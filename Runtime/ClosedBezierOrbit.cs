using System.Collections.Generic;
using UnityEngine;

namespace Gley.CameraSystem
{
    public class ClosedBezierOrbit
    {
        private const float MinimumLength = 0.0001f;
        private const float NormalSamplingDistance = 0.01f;
        private const float PlanarTolerance = 0.0001f;
        private const float RemovableSectionPositionTolerance = 0.0001f;
        private const int SamplesPerSegment = 32;
        private const float WatchMarkerPositionTolerance = 0.001f;
        private const float KnotSnapDistance = 0.001f;
        private const float MinimumHandleLength = 0.0001f;

        private readonly List<OrbitArcLengthSample> samples = new List<OrbitArcLengthSample>();
        private readonly List<OrbitWatchMarker> watchMarkers = new List<OrbitWatchMarker>();
        private readonly List<WatchPointKey> watchPointKeys = new List<WatchPointKey>();
        private readonly List<Vector3> bearingPositions = new List<Vector3>();
        private readonly List<float> bearingDistances = new List<float>();
        private readonly VehicleOrbit vehicleOrbit;
        private readonly WatchPointCurve watchPointCurve = new WatchPointCurve();
        private readonly InwardZoomAnalyzer inwardZoomAnalyzer = new InwardZoomAnalyzer();
        private readonly OrbitSelfIntersectionCheck selfIntersectionCheck = new OrbitSelfIntersectionCheck();
        private readonly OrbitBearing bearing = new OrbitBearing();

        private OrbitOffsetRangeValidationResult offsetRangeValidationResult;
        private Quaternion bearingOrientation;
        private float bearingBaseHeight;
        private float maximumZoomOffset;
        private float maximumSafeInwardZoom;
        private bool isBearingBuilt;
        private bool isInwardZoomMeasured;

        internal VehicleOrbit SourceOrbit => vehicleOrbit;
        public OrbitBearing Bearing
        {
            get
            {
                BuildBearingIfNeeded();
                return bearing;
            }
        }
        public OrbitOffsetRangeValidationResult OffsetRangeValidationResult
        {
            get
            {
                MeasureInwardZoomIfNeeded();
                return offsetRangeValidationResult;
            }
        }
        public OrbitWatchMarkerValidationResult WatchMarkerValidationResult { get; private set; }
        public OrbitValidationResult ValidationResult { get; private set; }
        public OrbitRemovableSectionValidationResult RemovableSectionValidationResult { get; private set; }
        public float Length { get; private set; }
        public float MaximumSafeInwardZoom
        {
            get
            {
                MeasureInwardZoomIfNeeded();
                return maximumSafeInwardZoom;
            }
        }
        public float MaximumCornerTurn { get; private set; }
        public int SharpestCornerKnotIndex { get; private set; }
        public bool IsClosed => ValidationResult == OrbitValidationResult.Valid;

        public ClosedBezierOrbit(VehicleOrbit orbit)
        {
            vehicleOrbit = orbit;
            Rebuild();
        }

        public Vector3 EvaluateBodyLocalPosition(float distance)
        {
            Vector3 orbitLocalPosition = EvaluateOrbitLocalPosition(distance);
            return vehicleOrbit.OrientationAdjustment * (orbitLocalPosition + Vector3.up * vehicleOrbit.BaseHeight);
        }

        public Vector3 EvaluateWorldPosition(Transform vehicleBody, float distance)
        {
            return vehicleBody.TransformPoint(EvaluateBodyLocalPosition(distance));
        }

        public Vector3 EvaluateBodyLocalWatchPoint(float distance)
        {
            if (WatchMarkerValidationResult != OrbitWatchMarkerValidationResult.Valid || Length <= 0f)
            {
                return Vector3.zero;
            }

            float normalizedOrbitPosition = Mathf.Repeat(distance, Length) / Length;
            return EvaluateWatchPoint(normalizedOrbitPosition);
        }

        public Vector3 EvaluateBodyLocalInwardNormal(float distance)
        {
            if (ValidationResult != OrbitValidationResult.Valid || Length <= 0f)
            {
                return Vector3.zero;
            }

            Vector3 previousPosition = EvaluateOrbitLocalPosition(distance - NormalSamplingDistance);
            Vector3 nextPosition = EvaluateOrbitLocalPosition(distance + NormalSamplingDistance);
            Vector3 tangent = nextPosition - previousPosition;

            if (tangent.sqrMagnitude < MinimumLength)
            {
                return Vector3.zero;
            }

            Vector3 inwardNormal = Vector3.Cross(tangent.normalized, Vector3.up);

            if (CalculateSignedArea() < 0f)
            {
                inwardNormal = Vector3.Cross(Vector3.up, tangent.normalized);
            }

            return vehicleOrbit.OrientationAdjustment * inwardNormal;
        }

        public Vector3 EvaluateBodyLocalRemovableSectionStart(OrbitAttachmentEnd attachmentEnd)
        {
            if (RemovableSectionValidationResult != OrbitRemovableSectionValidationResult.Valid)
            {
                return Vector3.zero;
            }

            OrbitRemovableSection section = vehicleOrbit.GetRemovableSection(attachmentEnd);
            return EvaluateBodyLocalPosition(SnapToKnot(Length * section.NormalizedStartPosition));
        }

        public Vector3 EvaluateBodyLocalRemovableSectionEnd(OrbitAttachmentEnd attachmentEnd)
        {
            if (RemovableSectionValidationResult != OrbitRemovableSectionValidationResult.Valid)
            {
                return Vector3.zero;
            }

            OrbitRemovableSection section = vehicleOrbit.GetRemovableSection(attachmentEnd);
            return EvaluateBodyLocalPosition(SnapToKnot(Length * section.NormalizedEndPosition));
        }

        public void Rebuild()
        {
            samples.Clear();
            watchMarkers.Clear();
            watchPointKeys.Clear();
            bearingPositions.Clear();
            bearingDistances.Clear();
            Length = 0f;
            maximumSafeInwardZoom = float.PositiveInfinity;
            MaximumCornerTurn = 0f;
            SharpestCornerKnotIndex = -1;
            isBearingBuilt = false;
            isInwardZoomMeasured = false;
            bearingOrientation = Quaternion.identity;
            bearingBaseHeight = 0f;
            maximumZoomOffset = 0f;
            if (vehicleOrbit != null)
            {
                bearingOrientation = vehicleOrbit.OrientationAdjustment;
                bearingBaseHeight = vehicleOrbit.BaseHeight;
                maximumZoomOffset = vehicleOrbit.MaximumZoomOffset;
            }

            ValidationResult = ValidateOrbit();
            WatchMarkerValidationResult = ValidateWatchMarkers();
            offsetRangeValidationResult = ValidateOffsetRanges();
            RemovableSectionValidationResult = ValidateRemovableSections();

            if (WatchMarkerValidationResult == OrbitWatchMarkerValidationResult.Valid)
            {
                for (int markerIndex = 0; markerIndex < watchMarkers.Count; markerIndex++)
                {
                    OrbitWatchMarker marker = watchMarkers[markerIndex];
                    watchPointKeys.Add(new WatchPointKey(marker.NormalizedOrbitPosition, marker.WatchPointLocalPosition, 0));
                }
            }

            watchPointCurve.Rebuild(watchPointKeys);

            if (ValidationResult == OrbitValidationResult.Valid)
            {
                MeasureCornerTurns();
            }

        }

        private void BuildBearingIfNeeded()
        {
            if (isBearingBuilt)
            {
                return;
            }

            isBearingBuilt = true;
            bearingPositions.Clear();
            bearingDistances.Clear();
            if (ValidationResult != OrbitValidationResult.Valid)
            {
                bearing.Rebuild(bearingPositions, bearingDistances, 0f);
                return;
            }

            if (bearingPositions.Capacity < samples.Count)
            {
                bearingPositions.Capacity = samples.Count;
                bearingDistances.Capacity = samples.Count;
            }

            for (int sampleIndex = 0; sampleIndex < samples.Count; sampleIndex++)
            {
                bearingPositions.Add(bearingOrientation * (samples[sampleIndex].Position + Vector3.up * bearingBaseHeight));
                bearingDistances.Add(samples[sampleIndex].Distance);
            }

            bearing.Rebuild(bearingPositions, bearingDistances, Length);
        }

        private void MeasureInwardZoomIfNeeded()
        {
            if (isInwardZoomMeasured)
            {
                return;
            }

            isInwardZoomMeasured = true;
            if (ValidationResult != OrbitValidationResult.Valid)
            {
                return;
            }

            maximumSafeInwardZoom = inwardZoomAnalyzer.MaximumSafeInwardZoom(this);
            if (offsetRangeValidationResult == OrbitOffsetRangeValidationResult.Valid && maximumZoomOffset > maximumSafeInwardZoom)
            {
                offsetRangeValidationResult = OrbitOffsetRangeValidationResult.InwardZoomExceedsCurvature;
            }
        }

        private float SnapToKnot(float distance)
        {
            for (int sampleIndex = 0; sampleIndex < samples.Count; sampleIndex += SamplesPerSegment)
            {
                float knotDistance = samples[sampleIndex].Distance;
                if (Mathf.Abs(distance - knotDistance) <= KnotSnapDistance)
                {
                    return knotDistance;
                }
            }

            return distance;
        }

        private OrbitRemovableSectionValidationResult ValidateRemovableSections()
        {
            if (vehicleOrbit == null)
            {
                return OrbitRemovableSectionValidationResult.NotConfigured;
            }

            OrbitRemovableSection frontSection = vehicleOrbit.FrontRemovableSection;
            OrbitRemovableSection rearSection = vehicleOrbit.RearRemovableSection;

            if (frontSection == null && rearSection == null)
            {
                return OrbitRemovableSectionValidationResult.NotConfigured;
            }

            if (frontSection == null)
            {
                return OrbitRemovableSectionValidationResult.MissingFrontSection;
            }

            if (rearSection == null)
            {
                return OrbitRemovableSectionValidationResult.MissingRearSection;
            }

            if (!IsRemovableSectionValid(frontSection))
            {
                return OrbitRemovableSectionValidationResult.InvalidFrontSection;
            }

            if (!IsRemovableSectionValid(rearSection))
            {
                return OrbitRemovableSectionValidationResult.InvalidRearSection;
            }

            if (DoRemovableSectionsOverlap(frontSection, rearSection))
            {
                return OrbitRemovableSectionValidationResult.OverlappingSections;
            }

            return OrbitRemovableSectionValidationResult.Valid;
        }

        private bool IsRemovableSectionValid(OrbitRemovableSection section)
        {
            if (section.NormalizedStartPosition < 0f || section.NormalizedStartPosition >= 1f)
            {
                return false;
            }

            if (section.NormalizedEndPosition < 0f || section.NormalizedEndPosition >= 1f)
            {
                return false;
            }

            return GetNormalizedSectionLength(section) > RemovableSectionPositionTolerance;
        }

        private bool DoRemovableSectionsOverlap(OrbitRemovableSection firstSection, OrbitRemovableSection secondSection)
        {
            return IsNormalizedPositionWithinSection(firstSection, secondSection.NormalizedStartPosition)
                || IsNormalizedPositionWithinSection(firstSection, secondSection.NormalizedEndPosition)
                || IsNormalizedPositionWithinSection(secondSection, firstSection.NormalizedStartPosition)
                || IsNormalizedPositionWithinSection(secondSection, firstSection.NormalizedEndPosition);
        }

        private bool IsNormalizedPositionWithinSection(OrbitRemovableSection section, float normalizedPosition)
        {
            float sectionLength = GetNormalizedSectionLength(section);

            float positionOffset = normalizedPosition - section.NormalizedStartPosition;

            if (positionOffset < 0f)
            {
                positionOffset += 1f;
            }

            return positionOffset <= sectionLength + RemovableSectionPositionTolerance;
        }

        private float GetNormalizedSectionLength(OrbitRemovableSection section)
        {
            float sectionLength = section.NormalizedEndPosition - section.NormalizedStartPosition;

            if (sectionLength < 0f)
            {
                sectionLength += 1f;
            }

            return sectionLength;
        }

        private OrbitOffsetRangeValidationResult ValidateOffsetRanges()
        {
            if (vehicleOrbit == null || vehicleOrbit.MinimumHeightOffset > vehicleOrbit.MaximumHeightOffset)
            {
                return OrbitOffsetRangeValidationResult.InvalidHeightRange;
            }

            if (vehicleOrbit.MinimumZoomOffset > vehicleOrbit.MaximumZoomOffset)
            {
                return OrbitOffsetRangeValidationResult.InvalidZoomRange;
            }

            return OrbitOffsetRangeValidationResult.Valid;
        }

        private OrbitWatchMarkerValidationResult ValidateWatchMarkers()
        {
            if (vehicleOrbit == null || vehicleOrbit.WatchMarkers == null || vehicleOrbit.WatchMarkers.Count == 0)
            {
                return OrbitWatchMarkerValidationResult.MissingMarkers;
            }

            for (int markerIndex = 0; markerIndex < vehicleOrbit.WatchMarkers.Count; markerIndex++)
            {
                OrbitWatchMarker marker = vehicleOrbit.WatchMarkers[markerIndex];

                if (marker == null || marker.NormalizedOrbitPosition < 0f || marker.NormalizedOrbitPosition >= 1f)
                {
                    return OrbitWatchMarkerValidationResult.InvalidMarkerPosition;
                }

                watchMarkers.Add(marker);
            }

            SortWatchMarkers();

            for (int markerIndex = 1; markerIndex < watchMarkers.Count; markerIndex++)
            {
                float previousPosition = watchMarkers[markerIndex - 1].NormalizedOrbitPosition;
                float currentPosition = watchMarkers[markerIndex].NormalizedOrbitPosition;

                if (Mathf.Abs(currentPosition - previousPosition) <= WatchMarkerPositionTolerance)
                {
                    return OrbitWatchMarkerValidationResult.DuplicateMarkerPosition;
                }
            }

            if (AreWatchMarkersDuplicateAcrossOrbitWrap())
            {
                return OrbitWatchMarkerValidationResult.DuplicateMarkerPosition;
            }

            return OrbitWatchMarkerValidationResult.Valid;
        }

        private void SortWatchMarkers()
        {
            for (int markerIndex = 1; markerIndex < watchMarkers.Count; markerIndex++)
            {
                OrbitWatchMarker marker = watchMarkers[markerIndex];
                int previousMarkerIndex = markerIndex - 1;

                while (previousMarkerIndex >= 0 && watchMarkers[previousMarkerIndex].NormalizedOrbitPosition > marker.NormalizedOrbitPosition)
                {
                    watchMarkers[previousMarkerIndex + 1] = watchMarkers[previousMarkerIndex];
                    previousMarkerIndex--;
                }

                watchMarkers[previousMarkerIndex + 1] = marker;
            }
        }

        private bool AreWatchMarkersDuplicateAcrossOrbitWrap()
        {
            if (watchMarkers.Count < 2)
            {
                return false;
            }

            float firstPosition = watchMarkers[0].NormalizedOrbitPosition;
            float lastPosition = watchMarkers[watchMarkers.Count - 1].NormalizedOrbitPosition;
            return firstPosition + 1f - lastPosition <= WatchMarkerPositionTolerance;
        }

        private OrbitValidationResult ValidateOrbit()
        {
            if (vehicleOrbit == null || vehicleOrbit.Knots.Count < 3)
            {
                return OrbitValidationResult.TooFewKnots;
            }

            for (int index = 0; index < vehicleOrbit.Knots.Count; index++)
            {
                BezierOrbitKnot knot = vehicleOrbit.Knots[index];

                if (knot == null)
                {
                    return OrbitValidationResult.TooFewKnots;
                }

                if (!IsPlanar(knot.Anchor) || !IsPlanar(knot.IncomingControlPoint) || !IsPlanar(knot.OutgoingControlPoint))
                {
                    return OrbitValidationResult.NonPlanar;
                }
            }

            BuildArcLengthSamples();

            if (Length < MinimumLength)
            {
                samples.Clear();
                return OrbitValidationResult.Degenerate;
            }

            if (HasSelfIntersection())
            {
                samples.Clear();
                return OrbitValidationResult.SelfIntersecting;
            }

            return OrbitValidationResult.Valid;
        }

        private bool IsPlanar(Vector3 point)
        {
            return Mathf.Abs(point.y) <= PlanarTolerance;
        }

        private float CalculateSignedArea()
        {
            float signedArea = 0f;

            for (int sampleIndex = 0; sampleIndex < samples.Count - 1; sampleIndex++)
            {
                Vector3 currentPosition = samples[sampleIndex].Position;
                Vector3 nextPosition = samples[sampleIndex + 1].Position;
                signedArea += currentPosition.x * nextPosition.z - nextPosition.x * currentPosition.z;
            }

            return signedArea;
        }

        private Vector3 EvaluateWatchPoint(float normalizedOrbitPosition)
        {
            return watchPointCurve.Evaluate(normalizedOrbitPosition);
        }

        private void BuildArcLengthSamples()
        {
            samples.Clear();
            Length = 0f;
            int sampleCount = vehicleOrbit.Knots.Count * SamplesPerSegment + 1;
            if (samples.Capacity < sampleCount)
            {
                samples.Capacity = sampleCount;
            }

            Vector3 firstPosition = EvaluateSegmentPosition(0, 0f);
            samples.Add(new OrbitArcLengthSample(firstPosition, 0f, 0, 0f));

            for (int segmentIndex = 0; segmentIndex < vehicleOrbit.Knots.Count; segmentIndex++)
            {
                Vector3 previousPosition = EvaluateSegmentPosition(segmentIndex, 0f);

                for (int sampleIndex = 1; sampleIndex <= SamplesPerSegment; sampleIndex++)
                {
                    float segmentT = (float)sampleIndex / SamplesPerSegment;
                    Vector3 currentPosition = EvaluateSegmentPosition(segmentIndex, segmentT);
                    Length += Vector3.Distance(previousPosition, currentPosition);
                    samples.Add(new OrbitArcLengthSample(currentPosition, Length, segmentIndex, segmentT));
                    previousPosition = currentPosition;
                }
            }
        }

        private void MeasureCornerTurns()
        {
            IReadOnlyList<BezierOrbitKnot> knots = vehicleOrbit.Knots;

            for (int knotIndex = 0; knotIndex < knots.Count; knotIndex++)
            {
                int previousKnotIndex = knotIndex - 1;
                if (previousKnotIndex < 0)
                {
                    previousKnotIndex = knots.Count - 1;
                }

                int nextKnotIndex = knotIndex + 1;
                if (nextKnotIndex == knots.Count)
                {
                    nextKnotIndex = 0;
                }

                BezierOrbitKnot previousKnot = knots[previousKnotIndex];
                BezierOrbitKnot knot = knots[knotIndex];
                BezierOrbitKnot nextKnot = knots[nextKnotIndex];

                if (!TryGetTangentDirection(knot.Anchor - knot.IncomingControlPoint, knot.Anchor - previousKnot.OutgoingControlPoint, knot.Anchor - previousKnot.Anchor, out Vector3 incomingDirection))
                {
                    continue;
                }

                if (!TryGetTangentDirection(knot.OutgoingControlPoint - knot.Anchor, nextKnot.IncomingControlPoint - knot.Anchor, nextKnot.Anchor - knot.Anchor, out Vector3 outgoingDirection))
                {
                    continue;
                }

                float turn = Vector3.Angle(incomingDirection, outgoingDirection);
                if (turn > MaximumCornerTurn)
                {
                    MaximumCornerTurn = turn;
                    SharpestCornerKnotIndex = knotIndex;
                }
            }
        }

        private bool TryGetTangentDirection(Vector3 handle, Vector3 nearControlChord, Vector3 anchorChord, out Vector3 direction)
        {
            float minimumLengthSquared = MinimumHandleLength * MinimumHandleLength;

            if (handle.sqrMagnitude > minimumLengthSquared)
            {
                direction = handle;
                return true;
            }

            if (nearControlChord.sqrMagnitude > minimumLengthSquared)
            {
                direction = nearControlChord;
                return true;
            }

            if (anchorChord.sqrMagnitude > minimumLengthSquared)
            {
                direction = anchorChord;
                return true;
            }

            direction = Vector3.zero;
            return false;
        }

        private bool HasSelfIntersection()
        {
            selfIntersectionCheck.Clear();
            selfIntersectionCheck.Reserve(samples.Count);
            for (int sampleIndex = 0; sampleIndex < samples.Count; sampleIndex++)
            {
                selfIntersectionCheck.AddPoint(ToPlanePosition(samples[sampleIndex].Position), samples[sampleIndex].SegmentIndex);
            }

            return selfIntersectionCheck.HasSelfIntersection();
        }

        private Vector2 ToPlanePosition(Vector3 position)
        {
            return new Vector2(position.x, position.z);
        }

        private Vector3 EvaluateOrbitLocalPosition(float distance)
        {
            if (ValidationResult != OrbitValidationResult.Valid || samples.Count == 0)
            {
                return Vector3.zero;
            }

            float wrappedDistance = distance % Length;

            if (wrappedDistance < 0f)
            {
                wrappedDistance += Length;
            }

            for (int sampleIndex = FindFirstSampleAtOrAfter(wrappedDistance); sampleIndex < samples.Count; sampleIndex++)
            {
                OrbitArcLengthSample previousSample = samples[sampleIndex - 1];
                OrbitArcLengthSample currentSample = samples[sampleIndex];

                if (wrappedDistance <= currentSample.Distance)
                {
                    float sampleDistance = currentSample.Distance - previousSample.Distance;
                    if (sampleDistance <= 0f)
                    {
                        continue;
                    }

                    float previousSegmentT = 0f;
                    if (previousSample.SegmentIndex == currentSample.SegmentIndex)
                    {
                        previousSegmentT = previousSample.SegmentT;
                    }

                    float sampleT = (wrappedDistance - previousSample.Distance) / sampleDistance;
                    float segmentT = Mathf.Lerp(previousSegmentT, currentSample.SegmentT, sampleT);
                    return EvaluateSegmentPosition(currentSample.SegmentIndex, segmentT);
                }
            }

            return samples[0].Position;
        }

        private int FindFirstSampleAtOrAfter(float distance)
        {
            int low = 1;
            int high = samples.Count - 1;
            if (high < low || samples[high].Distance < distance)
            {
                return samples.Count;
            }

            while (low < high)
            {
                int middle = (low + high) / 2;
                if (samples[middle].Distance >= distance)
                {
                    high = middle;
                }
                else
                {
                    low = middle + 1;
                }
            }

            return low;
        }

        private Vector3 EvaluateSegmentPosition(int segmentIndex, float segmentT)
        {
            int nextKnotIndex = segmentIndex + 1;

            if (nextKnotIndex == vehicleOrbit.Knots.Count)
            {
                nextKnotIndex = 0;
            }

            BezierOrbitKnot startKnot = vehicleOrbit.Knots[segmentIndex];
            BezierOrbitKnot endKnot = vehicleOrbit.Knots[nextKnotIndex];
            float inverseT = 1f - segmentT;
            float startWeight = inverseT * inverseT * inverseT;
            float startControlWeight = 3f * inverseT * inverseT * segmentT;
            float endControlWeight = 3f * inverseT * segmentT * segmentT;
            float endWeight = segmentT * segmentT * segmentT;

            return startWeight * startKnot.Anchor + startControlWeight * startKnot.OutgoingControlPoint + endControlWeight * endKnot.IncomingControlPoint + endWeight * endKnot.Anchor;
        }

        private readonly struct OrbitArcLengthSample
        {
            public Vector3 Position { get; }

            public float Distance { get; }

            public float SegmentT { get; }

            public int SegmentIndex { get; }

            public OrbitArcLengthSample(Vector3 position, float distance, int segmentIndex, float segmentT)
            {
                Position = position;
                Distance = distance;
                SegmentT = segmentT;
                SegmentIndex = segmentIndex;
            }
        }
    }
}
