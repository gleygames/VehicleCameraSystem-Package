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
        private const float WatchMarkerPositionTolerance = 0.0001f;

        private readonly List<OrbitArcLengthSample> samples = new List<OrbitArcLengthSample>();
        private readonly List<OrbitWatchMarker> watchMarkers = new List<OrbitWatchMarker>();
        private readonly VehicleOrbit vehicleOrbit;

        public OrbitOffsetRangeValidationResult OffsetRangeValidationResult { get; private set; }
        public OrbitWatchMarkerValidationResult WatchMarkerValidationResult { get; private set; }
        public OrbitValidationResult ValidationResult { get; private set; }
        public OrbitRemovableSectionValidationResult RemovableSectionValidationResult { get; private set; }
        public float Length { get; private set; }
        public bool IsClosed => ValidationResult == OrbitValidationResult.Valid;

        public ClosedBezierOrbit(VehicleOrbit orbit)
        {
            vehicleOrbit = orbit;
            Rebuild();
        }

        public Vector3 EvaluateBodyLocalPosition(float distance)
        {
            Vector3 orbitLocalPosition = EvaluateOrbitLocalPosition(distance);
            return vehicleOrbit.OrientationAdjustment * orbitLocalPosition;
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
            return EvaluateBodyLocalPosition(Length * section.NormalizedStartPosition);
        }

        public Vector3 EvaluateBodyLocalRemovableSectionEnd(OrbitAttachmentEnd attachmentEnd)
        {
            if (RemovableSectionValidationResult != OrbitRemovableSectionValidationResult.Valid)
            {
                return Vector3.zero;
            }

            OrbitRemovableSection section = vehicleOrbit.GetRemovableSection(attachmentEnd);
            return EvaluateBodyLocalPosition(Length * section.NormalizedEndPosition);
        }

        public void Rebuild()
        {
            samples.Clear();
            watchMarkers.Clear();
            Length = 0f;
            ValidationResult = ValidateOrbit();
            WatchMarkerValidationResult = ValidateWatchMarkers();
            OffsetRangeValidationResult = ValidateOffsetRanges();
            RemovableSectionValidationResult = ValidateRemovableSections();

            if (ValidationResult != OrbitValidationResult.Valid)
            {
                return;
            }

            BuildArcLengthSamples();

            if (Length < MinimumLength)
            {
                ValidationResult = OrbitValidationResult.Degenerate;
                samples.Clear();
            }
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

            samples.Clear();
            Length = 0f;
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
            int previousMarkerIndex = watchMarkers.Count - 1;

            for (int markerIndex = 0; markerIndex < watchMarkers.Count; markerIndex++)
            {
                if (normalizedOrbitPosition < watchMarkers[markerIndex].NormalizedOrbitPosition)
                {
                    break;
                }

                previousMarkerIndex = markerIndex;
            }

            int nextMarkerIndex = previousMarkerIndex + 1;

            if (nextMarkerIndex == watchMarkers.Count)
            {
                nextMarkerIndex = 0;
            }

            OrbitWatchMarker previousMarker = watchMarkers[previousMarkerIndex];
            OrbitWatchMarker nextMarker = watchMarkers[nextMarkerIndex];
            float previousMarkerPosition = previousMarker.NormalizedOrbitPosition;
            float nextMarkerPosition = nextMarker.NormalizedOrbitPosition;
            float interpolationPosition = normalizedOrbitPosition;

            if (nextMarkerIndex == 0)
            {
                nextMarkerPosition += 1f;
            }

            if (interpolationPosition < previousMarkerPosition)
            {
                interpolationPosition += 1f;
            }

            float interpolation = Mathf.InverseLerp(previousMarkerPosition, nextMarkerPosition, interpolationPosition);
            return Vector3.Lerp(previousMarker.WatchPointLocalPosition, nextMarker.WatchPointLocalPosition, interpolation);
        }

        private void BuildArcLengthSamples()
        {
            samples.Clear();
            Length = 0f;

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

        private bool HasSelfIntersection()
        {
            int segmentCount = samples.Count - 1;

            for (int firstSegmentIndex = 0; firstSegmentIndex < segmentCount; firstSegmentIndex++)
            {
                Vector2 firstStart = ToPlanePosition(samples[firstSegmentIndex].Position);
                Vector2 firstEnd = ToPlanePosition(samples[firstSegmentIndex + 1].Position);

                for (int secondSegmentIndex = firstSegmentIndex + 1; secondSegmentIndex < segmentCount; secondSegmentIndex++)
                {
                    if (secondSegmentIndex == firstSegmentIndex + 1)
                    {
                        continue;
                    }

                    if (firstSegmentIndex == 0 && secondSegmentIndex == segmentCount - 1)
                    {
                        continue;
                    }

                    Vector2 secondStart = ToPlanePosition(samples[secondSegmentIndex].Position);
                    Vector2 secondEnd = ToPlanePosition(samples[secondSegmentIndex + 1].Position);

                    if (DoSegmentsIntersect(firstStart, firstEnd, secondStart, secondEnd))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private Vector2 ToPlanePosition(Vector3 position)
        {
            return new Vector2(position.x, position.z);
        }

        private bool DoSegmentsIntersect(Vector2 firstStart, Vector2 firstEnd, Vector2 secondStart, Vector2 secondEnd)
        {
            float firstStartSide = CalculateCross(firstStart, firstEnd, secondStart);
            float firstEndSide = CalculateCross(firstStart, firstEnd, secondEnd);
            float secondStartSide = CalculateCross(secondStart, secondEnd, firstStart);
            float secondEndSide = CalculateCross(secondStart, secondEnd, firstEnd);

            if (firstStartSide == 0f && IsPointOnSegment(firstStart, firstEnd, secondStart))
            {
                return true;
            }

            if (firstEndSide == 0f && IsPointOnSegment(firstStart, firstEnd, secondEnd))
            {
                return true;
            }

            if (secondStartSide == 0f && IsPointOnSegment(secondStart, secondEnd, firstStart))
            {
                return true;
            }

            if (secondEndSide == 0f && IsPointOnSegment(secondStart, secondEnd, firstEnd))
            {
                return true;
            }

            bool firstSegmentSeparatesSecondSegment = (firstStartSide > 0f && firstEndSide < 0f) || (firstStartSide < 0f && firstEndSide > 0f);
            bool secondSegmentSeparatesFirstSegment = (secondStartSide > 0f && secondEndSide < 0f) || (secondStartSide < 0f && secondEndSide > 0f);
            return firstSegmentSeparatesSecondSegment && secondSegmentSeparatesFirstSegment;
        }

        private float CalculateCross(Vector2 lineStart, Vector2 lineEnd, Vector2 point)
        {
            Vector2 line = lineEnd - lineStart;
            Vector2 offset = point - lineStart;
            return line.x * offset.y - line.y * offset.x;
        }

        private bool IsPointOnSegment(Vector2 segmentStart, Vector2 segmentEnd, Vector2 point)
        {
            float minimumX = Mathf.Min(segmentStart.x, segmentEnd.x);
            float maximumX = Mathf.Max(segmentStart.x, segmentEnd.x);
            float minimumY = Mathf.Min(segmentStart.y, segmentEnd.y);
            float maximumY = Mathf.Max(segmentStart.y, segmentEnd.y);

            return point.x >= minimumX && point.x <= maximumX && point.y >= minimumY && point.y <= maximumY;
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

            for (int sampleIndex = 1; sampleIndex < samples.Count; sampleIndex++)
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
