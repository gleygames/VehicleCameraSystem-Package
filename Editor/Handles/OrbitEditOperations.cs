using System.Collections.Generic;
using UnityEngine;

namespace Gley.CameraSystem.Editor
{
    public class OrbitEditOperations
    {
        private const int MinimumKnotCount = 3;
        private const int SegmentSearchSamples = 32;
        private const int RefineIterations = 24;
        private const int MinimumCurveSearchSamples = 256;
        private const float CurveSearchSpacing = 0.1f;
        private const float MinimumHandleLength = 0.0001f;

        public Vector3 ToBodyLocalPoint(VehicleOrbit orbit, Vector3 orbitLocalPoint)
        {
            return orbit.OrientationAdjustment * (orbitLocalPoint + Vector3.up * orbit.BaseHeight);
        }

        public Vector3 ToOrbitLocalPoint(VehicleOrbit orbit, Vector3 bodyLocalPoint)
        {
            Vector3 orbitLocalPoint = Quaternion.Inverse(orbit.OrientationAdjustment) * bodyLocalPoint;
            orbitLocalPoint.y = 0f;
            return orbitLocalPoint;
        }

        public Vector3 GetBodyLocalPlaneNormal(VehicleOrbit orbit)
        {
            return orbit.OrientationAdjustment * Vector3.up;
        }

        public Vector3 EvaluateSegmentPoint(VehicleOrbit orbit, int segmentIndex, float segmentT)
        {
            BezierOrbitKnot startKnot = orbit.Knots[segmentIndex];
            BezierOrbitKnot endKnot = orbit.Knots[GetNextKnotIndex(orbit, segmentIndex)];
            float inverseT = 1f - segmentT;
            float startWeight = inverseT * inverseT * inverseT;
            float startControlWeight = 3f * inverseT * inverseT * segmentT;
            float endControlWeight = 3f * inverseT * segmentT * segmentT;
            float endWeight = segmentT * segmentT * segmentT;

            return startWeight * startKnot.Anchor + startControlWeight * startKnot.OutgoingControlPoint + endControlWeight * endKnot.IncomingControlPoint + endWeight * endKnot.Anchor;
        }

        public int InsertKnot(VehicleOrbit orbit, int segmentIndex, float segmentT)
        {
            int knotCount = orbit.Knots.Count;
            if (knotCount < 2 || segmentIndex < 0 || segmentIndex >= knotCount)
            {
                return -1;
            }

            float t = Mathf.Clamp01(segmentT);
            BezierOrbitKnot startKnot = orbit.Knots[segmentIndex];
            BezierOrbitKnot endKnot = orbit.Knots[GetNextKnotIndex(orbit, segmentIndex)];
            Vector3 firstLeft = Vector3.Lerp(startKnot.Anchor, startKnot.OutgoingControlPoint, t);
            Vector3 firstMiddle = Vector3.Lerp(startKnot.OutgoingControlPoint, endKnot.IncomingControlPoint, t);
            Vector3 firstRight = Vector3.Lerp(endKnot.IncomingControlPoint, endKnot.Anchor, t);
            Vector3 secondLeft = Vector3.Lerp(firstLeft, firstMiddle, t);
            Vector3 secondRight = Vector3.Lerp(firstMiddle, firstRight, t);
            Vector3 splitPoint = Vector3.Lerp(secondLeft, secondRight, t);

            startKnot.Configure(startKnot.Anchor, startKnot.IncomingControlPoint, firstLeft);
            endKnot.Configure(endKnot.Anchor, firstRight, endKnot.OutgoingControlPoint);
            BezierOrbitKnot insertedKnot = new BezierOrbitKnot();
            insertedKnot.Configure(splitPoint, secondLeft, secondRight);

            List<BezierOrbitKnot> knots = CopyKnots(orbit);
            knots.Insert(segmentIndex + 1, insertedKnot);
            orbit.Configure(knots, orbit.OrientationAdjustment);
            return segmentIndex + 1;
        }

        public bool DeleteKnot(VehicleOrbit orbit, int knotIndex)
        {
            if (orbit.Knots.Count <= MinimumKnotCount || knotIndex < 0 || knotIndex >= orbit.Knots.Count)
            {
                return false;
            }

            List<Vector3> placements = null;
            if (knotIndex == 0)
            {
                placements = CapturePlacements(orbit);
            }

            List<BezierOrbitKnot> knots = CopyKnots(orbit);
            knots.RemoveAt(knotIndex);
            orbit.Configure(knots, orbit.OrientationAdjustment);

            if (placements != null)
            {
                RestorePlacements(orbit, placements);
            }

            return true;
        }

        public void MoveKnotAnchor(VehicleOrbit orbit, int knotIndex, Vector3 orbitLocalAnchor)
        {
            BezierOrbitKnot knot = orbit.Knots[knotIndex];
            orbitLocalAnchor.y = 0f;
            Vector3 delta = orbitLocalAnchor - knot.Anchor;
            knot.Configure(orbitLocalAnchor, knot.IncomingControlPoint + delta, knot.OutgoingControlPoint + delta);
        }

        public void MoveControlPoint(VehicleOrbit orbit, int knotIndex, bool isOutgoing, Vector3 orbitLocalPosition, bool isMirrored)
        {
            BezierOrbitKnot knot = orbit.Knots[knotIndex];
            orbitLocalPosition.y = 0f;
            Vector3 incoming = knot.IncomingControlPoint;
            Vector3 outgoing = knot.OutgoingControlPoint;

            if (isOutgoing)
            {
                outgoing = orbitLocalPosition;
                if (isMirrored)
                {
                    incoming = MirrorHandle(knot.Anchor, outgoing, incoming);
                }
            }
            else
            {
                incoming = orbitLocalPosition;
                if (isMirrored)
                {
                    outgoing = MirrorHandle(knot.Anchor, incoming, outgoing);
                }
            }

            knot.Configure(knot.Anchor, incoming, outgoing);
        }

        public float FindNearestSegmentParameter(VehicleOrbit orbit, Vector3 orbitLocalPoint, out int segmentIndex, out float segmentT)
        {
            orbitLocalPoint.y = 0f;
            segmentIndex = -1;
            segmentT = 0f;
            float bestDistance = float.PositiveInfinity;

            for (int index = 0; index < orbit.Knots.Count; index++)
            {
                for (int sampleIndex = 0; sampleIndex <= SegmentSearchSamples; sampleIndex++)
                {
                    float t = (float)sampleIndex / SegmentSearchSamples;
                    float distance = (EvaluateSegmentPoint(orbit, index, t) - orbitLocalPoint).sqrMagnitude;
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        segmentIndex = index;
                        segmentT = t;
                    }
                }
            }

            if (segmentIndex < 0)
            {
                return bestDistance;
            }

            float step = 1f / SegmentSearchSamples;
            float low = Mathf.Max(0f, segmentT - step);
            float high = Mathf.Min(1f, segmentT + step);

            for (int iteration = 0; iteration < RefineIterations; iteration++)
            {
                float first = low + (high - low) / 3f;
                float second = high - (high - low) / 3f;
                float firstDistance = (EvaluateSegmentPoint(orbit, segmentIndex, first) - orbitLocalPoint).sqrMagnitude;
                float secondDistance = (EvaluateSegmentPoint(orbit, segmentIndex, second) - orbitLocalPoint).sqrMagnitude;
                if (firstDistance < secondDistance)
                {
                    high = second;
                }
                else
                {
                    low = first;
                }
            }

            float refinedT = (low + high) * 0.5f;
            float refinedDistance = (EvaluateSegmentPoint(orbit, segmentIndex, refinedT) - orbitLocalPoint).sqrMagnitude;
            if (refinedDistance < bestDistance)
            {
                segmentT = refinedT;
                bestDistance = refinedDistance;
            }

            return bestDistance;
        }

        public bool TryFindNearestNormalizedPosition(IOrbitDistanceSampler sampler, Vector3 bodyLocalPoint, out float normalizedPosition)
        {
            normalizedPosition = 0f;
            if (sampler == null || !sampler.CanSample || sampler.Length <= 0f)
            {
                return false;
            }

            float length = sampler.Length;
            int sampleCount = Mathf.Max(MinimumCurveSearchSamples, Mathf.CeilToInt(length / CurveSearchSpacing));
            float step = length / sampleCount;
            float bestOrbitDistance = 0f;
            float bestDistance = float.PositiveInfinity;

            for (int sampleIndex = 0; sampleIndex < sampleCount; sampleIndex++)
            {
                float orbitDistance = sampleIndex * step;
                float distance = (sampler.EvaluateBodyLocalPosition(orbitDistance) - bodyLocalPoint).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestOrbitDistance = orbitDistance;
                }
            }

            float low = bestOrbitDistance - step;
            float high = bestOrbitDistance + step;

            for (int iteration = 0; iteration < RefineIterations; iteration++)
            {
                float first = low + (high - low) / 3f;
                float second = high - (high - low) / 3f;
                float firstDistance = (sampler.EvaluateBodyLocalPosition(first) - bodyLocalPoint).sqrMagnitude;
                float secondDistance = (sampler.EvaluateBodyLocalPosition(second) - bodyLocalPoint).sqrMagnitude;
                if (firstDistance < secondDistance)
                {
                    high = second;
                }
                else
                {
                    low = first;
                }
            }

            normalizedPosition = Mathf.Repeat((low + high) * 0.5f, length) / length;
            if (normalizedPosition >= 1f)
            {
                normalizedPosition = 0f;
            }

            return true;
        }

        public bool MoveMarkerAlongCurve(OrbitWatchMarker marker, ClosedBezierOrbit builtOrbit, Vector3 bodyLocalPoint)
        {
            return MoveMarkerAlongCurve(marker, new ClosedOrbitDistanceSampler(builtOrbit), bodyLocalPoint);
        }

        public bool MoveMarkerAlongCurve(OrbitWatchMarker marker, IOrbitDistanceSampler sampler, Vector3 bodyLocalPoint)
        {
            if (!TryFindNearestNormalizedPosition(sampler, bodyLocalPoint, out float normalizedPosition))
            {
                return false;
            }

            marker.Configure(normalizedPosition, marker.WatchPointLocalPosition);
            return true;
        }

        public bool ReorderMarkers(VehicleOrbit orbit, int fromIndex, int toIndex)
        {
            int markerCount = orbit.WatchMarkers.Count;
            if (fromIndex < 0 || fromIndex >= markerCount || toIndex < 0 || toIndex >= markerCount || fromIndex == toIndex)
            {
                return false;
            }

            List<OrbitWatchMarker> markers = new List<OrbitWatchMarker>(markerCount);
            for (int index = 0; index < markerCount; index++)
            {
                markers.Add(orbit.WatchMarkers[index]);
            }

            OrbitWatchMarker movedMarker = markers[fromIndex];
            markers.RemoveAt(fromIndex);
            markers.Insert(toIndex, movedMarker);
            orbit.ConfigureWatchMarkers(markers);
            return true;
        }

        public float FindFreeMarkerPosition(VehicleOrbit orbit)
        {
            int markerCount = orbit.WatchMarkers.Count;
            if (markerCount == 0)
            {
                return 0f;
            }

            List<float> positions = new List<float>(markerCount);
            for (int index = 0; index < markerCount; index++)
            {
                OrbitWatchMarker marker = orbit.WatchMarkers[index];
                if (marker != null)
                {
                    positions.Add(Mathf.Repeat(marker.NormalizedOrbitPosition, 1f));
                }
            }

            if (positions.Count == 0)
            {
                return 0f;
            }

            positions.Sort();
            float bestStart = positions[positions.Count - 1];
            float bestGap = positions[0] + 1f - bestStart;

            for (int index = 1; index < positions.Count; index++)
            {
                float gap = positions[index] - positions[index - 1];
                if (gap > bestGap)
                {
                    bestGap = gap;
                    bestStart = positions[index - 1];
                }
            }

            float position = Mathf.Repeat(bestStart + bestGap * 0.5f, 1f);
            if (position >= 1f)
            {
                position = 0f;
            }

            return position;
        }

        private int GetNextKnotIndex(VehicleOrbit orbit, int knotIndex)
        {
            int nextIndex = knotIndex + 1;
            if (nextIndex >= orbit.Knots.Count)
            {
                nextIndex = 0;
            }

            return nextIndex;
        }

        private List<BezierOrbitKnot> CopyKnots(VehicleOrbit orbit)
        {
            List<BezierOrbitKnot> knots = new List<BezierOrbitKnot>(orbit.Knots.Count + 1);
            for (int index = 0; index < orbit.Knots.Count; index++)
            {
                knots.Add(orbit.Knots[index]);
            }

            return knots;
        }

        private List<Vector3> CapturePlacements(VehicleOrbit orbit)
        {
            ClosedBezierOrbit builtOrbit = new ClosedBezierOrbit(orbit);
            if (!builtOrbit.IsClosed)
            {
                return null;
            }

            List<Vector3> placements = new List<Vector3>();
            for (int index = 0; index < orbit.WatchMarkers.Count; index++)
            {
                OrbitWatchMarker marker = orbit.WatchMarkers[index];
                float position = 0f;
                if (marker != null)
                {
                    position = marker.NormalizedOrbitPosition;
                }

                placements.Add(EvaluateNormalized(builtOrbit, position));
            }

            AddSectionPlacements(builtOrbit, orbit.FrontRemovableSection, placements);
            AddSectionPlacements(builtOrbit, orbit.RearRemovableSection, placements);
            return placements;
        }

        private Vector3 EvaluateNormalized(ClosedBezierOrbit builtOrbit, float normalizedPosition)
        {
            return builtOrbit.EvaluateBodyLocalPosition(builtOrbit.Length * normalizedPosition);
        }

        private void AddSectionPlacements(ClosedBezierOrbit builtOrbit, OrbitRemovableSection section, List<Vector3> placements)
        {
            if (section == null)
            {
                return;
            }

            placements.Add(EvaluateNormalized(builtOrbit, section.NormalizedStartPosition));
            placements.Add(EvaluateNormalized(builtOrbit, section.NormalizedEndPosition));
        }

        private void RestorePlacements(VehicleOrbit orbit, List<Vector3> placements)
        {
            ClosedOrbitDistanceSampler sampler = new ClosedOrbitDistanceSampler(new ClosedBezierOrbit(orbit));
            if (!sampler.CanSample)
            {
                return;
            }

            int placementIndex = 0;
            for (int index = 0; index < orbit.WatchMarkers.Count; index++)
            {
                OrbitWatchMarker marker = orbit.WatchMarkers[index];
                if (marker != null)
                {
                    MoveMarkerAlongCurve(marker, sampler, placements[placementIndex]);
                }

                placementIndex++;
            }

            placementIndex = RestoreSectionPlacement(sampler, orbit.FrontRemovableSection, placements, placementIndex);
            RestoreSectionPlacement(sampler, orbit.RearRemovableSection, placements, placementIndex);
        }

        private int RestoreSectionPlacement(IOrbitDistanceSampler sampler, OrbitRemovableSection section, List<Vector3> placements, int placementIndex)
        {
            if (section == null)
            {
                return placementIndex;
            }

            if (TryFindNearestNormalizedPosition(sampler, placements[placementIndex], out float start) && TryFindNearestNormalizedPosition(sampler, placements[placementIndex + 1], out float end))
            {
                section.Configure(start, end);
            }

            return placementIndex + 2;
        }

        private Vector3 MirrorHandle(Vector3 anchor, Vector3 movedHandle, Vector3 oppositeHandle)
        {
            Vector3 movedOffset = movedHandle - anchor;
            float movedLength = movedOffset.magnitude;
            if (movedLength < MinimumHandleLength)
            {
                return oppositeHandle;
            }

            float oppositeLength = (oppositeHandle - anchor).magnitude;
            if (oppositeLength < MinimumHandleLength)
            {
                oppositeLength = movedLength;
            }

            return anchor - movedOffset / movedLength * oppositeLength;
        }
    }
}
