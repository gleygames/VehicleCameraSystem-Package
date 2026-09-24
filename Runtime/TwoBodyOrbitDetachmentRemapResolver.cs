using UnityEngine;

namespace Gley.CameraSystem
{
    public class TwoBodyOrbitDetachmentRemapResolver
    {
        private readonly TwoBodyClosedBezierOrbit combinedOrbit;
        private readonly ClosedBezierOrbit rootOrbit;
        private readonly VehicleOrbit rootVehicleOrbit;

        public TwoBodyClosedBezierOrbit CombinedOrbit => combinedOrbit;
        public ClosedBezierOrbit RootOrbit => rootOrbit;
        public OrbitDetachmentRemapResult LastRemapResult { get; private set; }
        public float RemappedOrbitDistance { get; private set; }

        public TwoBodyOrbitDetachmentRemapResolver(VehicleProfile rootVehicleProfile, TwoBodyClosedBezierOrbit attachedOrbit)
        {
            combinedOrbit = attachedOrbit;

            if (rootVehicleProfile == null)
            {
                return;
            }

            rootVehicleOrbit = rootVehicleProfile.PrimaryOrbit;

            if (rootVehicleOrbit == null)
            {
                return;
            }

            rootOrbit = new ClosedBezierOrbit(rootVehicleOrbit);
        }

        public OrbitDetachmentRemapResult ResolveCombinedOrbitDistance(float combinedOrbitDistance)
        {
            RemappedOrbitDistance = 0f;

            if (combinedOrbit == null || !combinedOrbit.IsClosed || rootOrbit == null)
            {
                LastRemapResult = OrbitDetachmentRemapResult.InvalidRootOrbit;
                return LastRemapResult;
            }

            if (rootOrbit.ValidationResult != OrbitValidationResult.Valid
                || rootOrbit.WatchMarkerValidationResult != OrbitWatchMarkerValidationResult.Valid
                || rootOrbit.RemovableSectionValidationResult != OrbitRemovableSectionValidationResult.Valid)
            {
                LastRemapResult = OrbitDetachmentRemapResult.InvalidRootOrbit;
                return LastRemapResult;
            }

            float remappedDistance;

            if (combinedOrbit.TryGetRetainedFrontSourceOrbitDistance(combinedOrbitDistance, out remappedDistance))
            {
                RemappedOrbitDistance = remappedDistance;
                LastRemapResult = OrbitDetachmentRemapResult.RetainedFrontPosition;
                return LastRemapResult;
            }

            OrbitRemovableSection removedRearSection = rootVehicleOrbit.RearRemovableSection;
            float removedRearSectionLength = GetNormalizedSectionLength(removedRearSection);
            float fallbackNormalizedPosition = removedRearSection.NormalizedStartPosition + removedRearSectionLength * 0.5f;
            fallbackNormalizedPosition = Mathf.Repeat(fallbackNormalizedPosition, 1f);
            RemappedOrbitDistance = rootOrbit.Length * fallbackNormalizedPosition;
            LastRemapResult = OrbitDetachmentRemapResult.NonFrontPositionMappedToRemovedRear;
            return LastRemapResult;
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
    }
}
