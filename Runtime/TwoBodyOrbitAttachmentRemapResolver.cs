using UnityEngine;

namespace Gley.CameraSystem
{
    public class TwoBodyOrbitAttachmentRemapResolver
    {
        private readonly ClosedBezierOrbit frontOrbit;
        private readonly ClosedBezierOrbit rearOrbit;
        private readonly TwoBodyClosedBezierOrbit combinedOrbit;
        private readonly VehicleOrbit frontVehicleOrbit;
        private readonly VehicleOrbit rearVehicleOrbit;
        private readonly VehicleProfile frontVehicleProfile;
        private readonly VehicleProfile rearVehicleProfile;

        public TwoBodyClosedBezierOrbit CombinedOrbit => combinedOrbit;

        public OrbitAttachmentRemapResult LastRemapResult { get; private set; }
        public float RemappedOrbitDistance { get; private set; }

        public TwoBodyOrbitAttachmentRemapResolver(VehicleProfile frontVehicleProfile, VehicleProfile rearVehicleProfile)
        {
            this.frontVehicleProfile = frontVehicleProfile;
            this.rearVehicleProfile = rearVehicleProfile;
            combinedOrbit = new TwoBodyClosedBezierOrbit(frontVehicleProfile, rearVehicleProfile);

            if (frontVehicleProfile == null || rearVehicleProfile == null)
            {
                return;
            }

            frontVehicleOrbit = frontVehicleProfile.VehicleOrbit;
            rearVehicleOrbit = rearVehicleProfile.VehicleOrbit;

            if (frontVehicleOrbit == null || rearVehicleOrbit == null)
            {
                return;
            }

            frontOrbit = new ClosedBezierOrbit(frontVehicleOrbit);
            rearOrbit = new ClosedBezierOrbit(rearVehicleOrbit);
        }

        public OrbitAttachmentRemapResult ResolveFrontOrbitDistance(float frontOrbitDistance)
        {
            RemappedOrbitDistance = 0f;

            if (!combinedOrbit.IsClosed)
            {
                LastRemapResult = OrbitAttachmentRemapResult.InvalidCombinedOrbit;
                return LastRemapResult;
            }

            OrbitRemovableSection removedFrontRearSection = frontVehicleOrbit.RearRemovableSection;
            float normalizedFrontOrbitPosition = Mathf.Repeat(frontOrbitDistance, frontOrbit.Length) / frontOrbit.Length;

            if (IsNormalizedPositionWithinSection(normalizedFrontOrbitPosition, removedFrontRearSection))
            {
                RemappedOrbitDistance = GetRearTargetCombinedDistance(removedFrontRearSection);
                LastRemapResult = OrbitAttachmentRemapResult.RemovedFrontPositionMappedToRear;
                return LastRemapResult;
            }

            float remappedDistance;

            if (!combinedOrbit.TryGetRetainedFrontOrbitDistance(frontOrbitDistance, out remappedDistance))
            {
                LastRemapResult = OrbitAttachmentRemapResult.InvalidCombinedOrbit;
                return LastRemapResult;
            }

            RemappedOrbitDistance = remappedDistance;
            LastRemapResult = OrbitAttachmentRemapResult.RetainedFrontPosition;
            return LastRemapResult;
        }

        private float GetRearTargetCombinedDistance(OrbitRemovableSection removedFrontRearSection)
        {
            OrbitRemovableSection rearFrontSection = rearVehicleOrbit.FrontRemovableSection;
            OrbitRemovableSection rearRearSection = rearVehicleOrbit.RearRemovableSection;
            float rearSectionLength = GetNormalizedSectionLength(rearRearSection);
            float rearTargetNormalizedPosition = rearRearSection.NormalizedStartPosition + rearSectionLength * 0.5f;
            rearTargetNormalizedPosition = Mathf.Repeat(rearTargetNormalizedPosition, 1f);
            float frontRetainedLength = frontOrbit.Length * (1f - GetNormalizedSectionLength(removedFrontRearSection));
            TwoBodyOrbitConnectorGenerator connectorGenerator = new TwoBodyOrbitConnectorGenerator(frontVehicleProfile, rearVehicleProfile);
            float rightConnectorLength = Vector3.Distance(connectorGenerator.ConnectorPair.RightConnector.StartPosition, connectorGenerator.ConnectorPair.RightConnector.EndPosition);
            float rearTargetDistance = rearOrbit.Length * rearTargetNormalizedPosition;
            float rearRetainedStartDistance = rearOrbit.Length * rearFrontSection.NormalizedEndPosition;
            float rearDistanceFromRetainedStart = rearTargetDistance - rearRetainedStartDistance;

            if (rearDistanceFromRetainedStart < 0f)
            {
                rearDistanceFromRetainedStart += rearOrbit.Length;
            }

            return frontRetainedLength + rightConnectorLength + rearDistanceFromRetainedStart;
        }

        private bool IsNormalizedPositionWithinSection(float normalizedPosition, OrbitRemovableSection section)
        {
            float sectionLength = GetNormalizedSectionLength(section);
            float positionOffset = normalizedPosition - section.NormalizedStartPosition;

            if (positionOffset < 0f)
            {
                positionOffset += 1f;
            }

            return positionOffset > 0f && positionOffset < sectionLength;
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
