using UnityEngine;

namespace Gley.CameraSystem.Editor
{
    public class OrbitPreviewCache : IOrbitDistanceSampler
    {
        private const int CurveSamplesPerSegment = 24;
        private const int ArcLengthSamplesPerSegment = 32;
        private const int MinimumSampleCount = 64;
        private const int MaximumSampleCount = 512;
        private const float SampleSpacing = 0.25f;
        private const float NormalSamplingDistance = 0.01f;
        private const float MinimumTangentLength = 0.0001f;
        private const int HeightBandCount = 16;
        private const int PointsPerHeightBand = 8;
        private const int GeometryHeaderValues = 5;
        private const int ValuesPerKnot = 10;
        private const int ValuesPerMarker = 5;
        private const int RangeValues = 4;

        private readonly Vector3[] heightBandLines = new Vector3[HeightBandCount * PointsPerHeightBand];
        private readonly float[] rangeSnapshot = new float[RangeValues];
        private readonly OrbitEditOperations operations;

        private Vector3[] knotCurvePoints = new Vector3[0];
        private Vector3[] basePoints = new Vector3[0];
        private Vector3[] inwardNormals = new Vector3[0];
        private Vector3[] innerLowPoints = new Vector3[0];
        private Vector3[] innerHighPoints = new Vector3[0];
        private Vector3[] outerLowPoints = new Vector3[0];
        private Vector3[] outerHighPoints = new Vector3[0];
        private float[] geometrySnapshot = new float[0];
        private float[] markerSnapshot = new float[0];
        private float[] tableDistances = new float[0];
        private float[] tableSegmentTs = new float[0];
        private int[] tableSegments = new int[0];
        private VehicleOrbit orbit;
        private ClosedBezierOrbit builtOrbit;
        private float signedArea;
        private int revision = -1;
        private int tableCount;
        private int geometrySnapshotCount;
        private int markerSnapshotCount;

        public Vector3[] KnotCurvePoints => knotCurvePoints;
        public Vector3[] InnerLowPoints => innerLowPoints;
        public Vector3[] InnerHighPoints => innerHighPoints;
        public Vector3[] OuterLowPoints => outerLowPoints;
        public Vector3[] OuterHighPoints => outerHighPoints;
        public Vector3[] HeightBandLines => heightBandLines;
        public ClosedBezierOrbit BuiltOrbit => builtOrbit;
        public Vector3 PlaneNormal { get; private set; }
        public Vector3 ReferencePoint { get; private set; }
        public Vector3 ReferenceInwardNormal { get; private set; }
        public float Length { get; private set; }
        public int KnotCurvePointCount { get; private set; }
        public int SampleCount { get; private set; }
        public bool IsCurveValid => builtOrbit != null && builtOrbit.IsClosed && SampleCount > 0;
        public bool CanSample => IsCurveValid;

        public OrbitPreviewCache(OrbitEditOperations editOperations)
        {
            operations = editOperations;
        }

        public void Refresh(VehicleOrbit targetOrbit, int profileRevision, bool needsWatchPoints)
        {
            if (targetOrbit == orbit && profileRevision == revision)
            {
                return;
            }

            bool isNewOrbit = targetOrbit != orbit || builtOrbit == null;
            orbit = targetOrbit;
            revision = profileRevision;

            if (orbit == null)
            {
                builtOrbit = null;
                ClearGeometry();
                return;
            }

            bool hasGeometryChanged = CaptureGeometry() || isNewOrbit;
            bool haveMarkersChanged = false;
            if (needsWatchPoints)
            {
                haveMarkersChanged = CaptureMarkers();
            }

            bool haveRangesChanged = CaptureRanges();

            if (hasGeometryChanged || haveMarkersChanged)
            {
                builtOrbit = new ClosedBezierOrbit(orbit);
            }

            if (hasGeometryChanged)
            {
                ClearGeometry();
                PlaneNormal = operations.GetBodyLocalPlaneNormal(orbit);
                BuildKnotCurve();
                BuildArcLengthTable();
                BuildSamples();
            }

            if (hasGeometryChanged || haveRangesChanged)
            {
                BuildEnvelope();
            }
        }

        public Vector3 EvaluateBodyLocalPosition(float distance)
        {
            return operations.ToBodyLocalPoint(orbit, EvaluateOrbitLocalPosition(distance));
        }

        public Vector3 EvaluateBodyLocalInwardNormal(float distance)
        {
            Vector3 tangent = EvaluateOrbitLocalPosition(distance + NormalSamplingDistance) - EvaluateOrbitLocalPosition(distance - NormalSamplingDistance);
            if (tangent.sqrMagnitude < MinimumTangentLength)
            {
                return Vector3.zero;
            }

            Vector3 inwardNormal = Vector3.Cross(tangent.normalized, Vector3.up);
            if (signedArea < 0f)
            {
                inwardNormal = Vector3.Cross(Vector3.up, tangent.normalized);
            }

            return orbit.OrientationAdjustment * inwardNormal;
        }

        public Vector3 EvaluateNormalizedPoint(float normalizedPosition)
        {
            return EvaluateBodyLocalPosition(Length * normalizedPosition);
        }

        public Vector3 EvaluateCachedPoint(float normalizedPosition)
        {
            float samplePosition = Mathf.Repeat(normalizedPosition, 1f) * SampleCount;
            int sampleIndex = Mathf.Min((int)samplePosition, SampleCount - 1);
            return Vector3.Lerp(basePoints[sampleIndex], basePoints[sampleIndex + 1], samplePosition - sampleIndex);
        }

        private void ClearGeometry()
        {
            KnotCurvePointCount = 0;
            SampleCount = 0;
            tableCount = 0;
            Length = 0f;
        }

        private bool CaptureGeometry()
        {
            int knotCount = orbit.Knots.Count;
            int count = GeometryHeaderValues + knotCount * ValuesPerKnot;
            bool hasChanged = count != geometrySnapshotCount;
            if (geometrySnapshot.Length < count)
            {
                geometrySnapshot = new float[count];
                hasChanged = true;
            }

            geometrySnapshotCount = count;
            Quaternion orientation = orbit.OrientationAdjustment;
            hasChanged |= StoreValue(geometrySnapshot, 0, orbit.BaseHeight);
            hasChanged |= StoreValue(geometrySnapshot, 1, orientation.x);
            hasChanged |= StoreValue(geometrySnapshot, 2, orientation.y);
            hasChanged |= StoreValue(geometrySnapshot, 3, orientation.z);
            hasChanged |= StoreValue(geometrySnapshot, 4, orientation.w);

            for (int knotIndex = 0; knotIndex < knotCount; knotIndex++)
            {
                int index = GeometryHeaderValues + knotIndex * ValuesPerKnot;
                BezierOrbitKnot knot = orbit.Knots[knotIndex];
                if (knot == null)
                {
                    hasChanged |= StoreValue(geometrySnapshot, index + 9, 1f);
                    continue;
                }

                hasChanged |= StoreVector(geometrySnapshot, index, knot.Anchor);
                hasChanged |= StoreVector(geometrySnapshot, index + 3, knot.IncomingControlPoint);
                hasChanged |= StoreVector(geometrySnapshot, index + 6, knot.OutgoingControlPoint);
                hasChanged |= StoreValue(geometrySnapshot, index + 9, 0f);
            }

            return hasChanged;
        }

        private bool StoreValue(float[] snapshot, int index, float value)
        {
            if (snapshot[index] == value)
            {
                return false;
            }

            snapshot[index] = value;
            return true;
        }

        private bool StoreVector(float[] snapshot, int index, Vector3 value)
        {
            bool hasChanged = StoreValue(snapshot, index, value.x);
            hasChanged |= StoreValue(snapshot, index + 1, value.y);
            hasChanged |= StoreValue(snapshot, index + 2, value.z);
            return hasChanged;
        }

        private bool CaptureMarkers()
        {
            int markerCount = orbit.WatchMarkers.Count;
            int count = markerCount * ValuesPerMarker;
            bool hasChanged = count != markerSnapshotCount;
            if (markerSnapshot.Length < count)
            {
                markerSnapshot = new float[count];
                hasChanged = true;
            }

            markerSnapshotCount = count;

            for (int markerIndex = 0; markerIndex < markerCount; markerIndex++)
            {
                int index = markerIndex * ValuesPerMarker;
                OrbitWatchMarker marker = orbit.WatchMarkers[markerIndex];
                if (marker == null)
                {
                    hasChanged |= StoreValue(markerSnapshot, index, -1f);
                    continue;
                }

                hasChanged |= StoreValue(markerSnapshot, index, marker.Id);
                hasChanged |= StoreValue(markerSnapshot, index + 1, marker.NormalizedOrbitPosition);
                hasChanged |= StoreVector(markerSnapshot, index + 2, marker.WatchPointLocalPosition);
            }

            return hasChanged;
        }

        private bool CaptureRanges()
        {
            bool hasChanged = StoreValue(rangeSnapshot, 0, orbit.MinimumHeightOffset);
            hasChanged |= StoreValue(rangeSnapshot, 1, orbit.MaximumHeightOffset);
            hasChanged |= StoreValue(rangeSnapshot, 2, orbit.MinimumZoomOffset);
            hasChanged |= StoreValue(rangeSnapshot, 3, orbit.MaximumZoomOffset);
            return hasChanged;
        }

        private void BuildKnotCurve()
        {
            int knotCount = orbit.Knots.Count;
            if (knotCount < 2)
            {
                return;
            }

            for (int index = 0; index < knotCount; index++)
            {
                if (orbit.Knots[index] == null)
                {
                    return;
                }
            }

            KnotCurvePointCount = knotCount * CurveSamplesPerSegment + 1;
            knotCurvePoints = EnsureSize(knotCurvePoints, KnotCurvePointCount);
            int pointIndex = 0;

            for (int segmentIndex = 0; segmentIndex < knotCount; segmentIndex++)
            {
                for (int sampleIndex = 0; sampleIndex < CurveSamplesPerSegment; sampleIndex++)
                {
                    float segmentT = (float)sampleIndex / CurveSamplesPerSegment;
                    knotCurvePoints[pointIndex] = operations.ToBodyLocalPoint(orbit, operations.EvaluateSegmentPoint(orbit, segmentIndex, segmentT));
                    pointIndex++;
                }
            }

            knotCurvePoints[pointIndex] = knotCurvePoints[0];
        }

        private Vector3[] EnsureSize(Vector3[] points, int size)
        {
            if (points.Length >= size)
            {
                return points;
            }

            return new Vector3[size];
        }

        private void BuildArcLengthTable()
        {
            signedArea = 0f;
            if (!builtOrbit.IsClosed)
            {
                return;
            }

            int knotCount = orbit.Knots.Count;
            int count = knotCount * ArcLengthSamplesPerSegment + 1;
            if (tableDistances.Length < count)
            {
                tableDistances = new float[count];
                tableSegmentTs = new float[count];
                tableSegments = new int[count];
            }

            tableDistances[0] = 0f;
            tableSegmentTs[0] = 0f;
            tableSegments[0] = 0;
            int tableIndex = 1;
            float length = 0f;

            for (int segmentIndex = 0; segmentIndex < knotCount; segmentIndex++)
            {
                Vector3 previousPosition = operations.EvaluateSegmentPoint(orbit, segmentIndex, 0f);

                for (int sampleIndex = 1; sampleIndex <= ArcLengthSamplesPerSegment; sampleIndex++)
                {
                    float segmentT = (float)sampleIndex / ArcLengthSamplesPerSegment;
                    Vector3 currentPosition = operations.EvaluateSegmentPoint(orbit, segmentIndex, segmentT);
                    length += Vector3.Distance(previousPosition, currentPosition);
                    signedArea += previousPosition.x * currentPosition.z - currentPosition.x * previousPosition.z;
                    tableDistances[tableIndex] = length;
                    tableSegmentTs[tableIndex] = segmentT;
                    tableSegments[tableIndex] = segmentIndex;
                    previousPosition = currentPosition;
                    tableIndex++;
                }
            }

            tableCount = count;
            Length = length;
        }

        private void BuildSamples()
        {
            if (tableCount < 2 || Length <= 0f)
            {
                return;
            }

            SampleCount = Mathf.Clamp(Mathf.CeilToInt(Length / SampleSpacing), MinimumSampleCount, MaximumSampleCount);
            basePoints = EnsureSize(basePoints, SampleCount + 1);
            inwardNormals = EnsureSize(inwardNormals, SampleCount + 1);

            for (int sampleIndex = 0; sampleIndex < SampleCount; sampleIndex++)
            {
                float distance = Length * sampleIndex / SampleCount;
                basePoints[sampleIndex] = EvaluateBodyLocalPosition(distance);
                inwardNormals[sampleIndex] = EvaluateBodyLocalInwardNormal(distance);
            }

            basePoints[SampleCount] = basePoints[0];
            inwardNormals[SampleCount] = inwardNormals[0];

            float referenceDistance;
            if (!builtOrbit.Bearing.TryGetDistanceAtBearing(0f, 0f, out referenceDistance))
            {
                referenceDistance = 0f;
            }

            ReferencePoint = EvaluateBodyLocalPosition(referenceDistance);
            ReferenceInwardNormal = EvaluateBodyLocalInwardNormal(referenceDistance);
        }

        private void BuildEnvelope()
        {
            if (SampleCount == 0)
            {
                return;
            }

            int pointCount = SampleCount + 1;
            innerLowPoints = EnsureSize(innerLowPoints, pointCount);
            innerHighPoints = EnsureSize(innerHighPoints, pointCount);
            outerLowPoints = EnsureSize(outerLowPoints, pointCount);
            outerHighPoints = EnsureSize(outerHighPoints, pointCount);
            Vector3 lowOffset = PlaneNormal * orbit.MinimumHeightOffset;
            Vector3 highOffset = PlaneNormal * orbit.MaximumHeightOffset;

            for (int index = 0; index < pointCount; index++)
            {
                Vector3 inner = basePoints[index] + inwardNormals[index] * orbit.MaximumZoomOffset;
                Vector3 outer = basePoints[index] + inwardNormals[index] * orbit.MinimumZoomOffset;
                innerLowPoints[index] = inner + lowOffset;
                innerHighPoints[index] = inner + highOffset;
                outerLowPoints[index] = outer + lowOffset;
                outerHighPoints[index] = outer + highOffset;
            }

            int lineIndex = 0;
            for (int bandIndex = 0; bandIndex < HeightBandCount; bandIndex++)
            {
                int sampleIndex = bandIndex * SampleCount / HeightBandCount;
                heightBandLines[lineIndex] = innerLowPoints[sampleIndex];
                heightBandLines[lineIndex + 1] = innerHighPoints[sampleIndex];
                heightBandLines[lineIndex + 2] = outerLowPoints[sampleIndex];
                heightBandLines[lineIndex + 3] = outerHighPoints[sampleIndex];
                heightBandLines[lineIndex + 4] = innerLowPoints[sampleIndex];
                heightBandLines[lineIndex + 5] = outerLowPoints[sampleIndex];
                heightBandLines[lineIndex + 6] = innerHighPoints[sampleIndex];
                heightBandLines[lineIndex + 7] = outerHighPoints[sampleIndex];
                lineIndex += PointsPerHeightBand;
            }
        }

        private Vector3 EvaluateOrbitLocalPosition(float distance)
        {
            if (tableCount < 2 || Length <= 0f)
            {
                return Vector3.zero;
            }

            float wrappedDistance = distance % Length;
            if (wrappedDistance < 0f)
            {
                wrappedDistance += Length;
            }

            int low = 1;
            int high = tableCount - 1;
            while (low < high)
            {
                int middle = (low + high) / 2;
                if (tableDistances[middle] >= wrappedDistance)
                {
                    high = middle;
                }
                else
                {
                    low = middle + 1;
                }
            }

            int index = low;
            while (index < tableCount - 1 && tableDistances[index] - tableDistances[index - 1] <= 0f)
            {
                index++;
            }

            float sampleDistance = tableDistances[index] - tableDistances[index - 1];
            if (sampleDistance <= 0f)
            {
                return operations.EvaluateSegmentPoint(orbit, 0, 0f);
            }

            float previousSegmentT = 0f;
            if (tableSegments[index - 1] == tableSegments[index])
            {
                previousSegmentT = tableSegmentTs[index - 1];
            }

            float sampleT = (wrappedDistance - tableDistances[index - 1]) / sampleDistance;
            return operations.EvaluateSegmentPoint(orbit, tableSegments[index], Mathf.Lerp(previousSegmentT, tableSegmentTs[index], sampleT));
        }
    }
}
