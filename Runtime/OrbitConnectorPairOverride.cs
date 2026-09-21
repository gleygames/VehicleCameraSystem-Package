using UnityEngine;

namespace Gley.CameraSystem
{
    [CreateAssetMenu(fileName = "Orbit Connector Pair Override", menuName = "Gley/Vehicle Camera System/Orbit Connector Pair Override")]
    public class OrbitConnectorPairOverride : ScriptableObject
    {
        [SerializeField] private OrbitAttachmentEnd frontAttachmentEnd = OrbitAttachmentEnd.Rear;
        [SerializeField] private OrbitAttachmentEnd rearAttachmentEnd = OrbitAttachmentEnd.Front;
        [SerializeField] private OrbitConnectorGeometry leftConnector;
        [SerializeField] private OrbitConnectorGeometry rightConnector;
        [SerializeField] private VehicleProfile frontVehicleProfile;
        [SerializeField] private VehicleProfile rearVehicleProfile;

        public OrbitAttachmentEnd FrontAttachmentEnd => frontAttachmentEnd;
        public OrbitAttachmentEnd RearAttachmentEnd => rearAttachmentEnd;
        public OrbitConnectorGeometry LeftConnector => leftConnector;
        public OrbitConnectorGeometry RightConnector => rightConnector;
        public VehicleProfile FrontVehicleProfile => frontVehicleProfile;
        public VehicleProfile RearVehicleProfile => rearVehicleProfile;

        public GeneratedOrbitConnectorPair CreateConnectorPair()
        {
            if (leftConnector == null || rightConnector == null)
            {
                return null;
            }

            return new GeneratedOrbitConnectorPair(leftConnector.CreateConnector(), rightConnector.CreateConnector());
        }

        public bool Matches(VehicleProfile frontProfile, VehicleProfile rearProfile, OrbitAttachmentEnd expectedFrontAttachmentEnd, OrbitAttachmentEnd expectedRearAttachmentEnd)
        {
            return frontVehicleProfile == frontProfile
                && rearVehicleProfile == rearProfile
                && frontAttachmentEnd == expectedFrontAttachmentEnd
                && rearAttachmentEnd == expectedRearAttachmentEnd;
        }

        public void Configure(VehicleProfile frontProfile, VehicleProfile rearProfile, OrbitAttachmentEnd configuredFrontAttachmentEnd, OrbitAttachmentEnd configuredRearAttachmentEnd, OrbitConnectorGeometry configuredLeftConnector, OrbitConnectorGeometry configuredRightConnector)
        {
            frontVehicleProfile = frontProfile;
            rearVehicleProfile = rearProfile;
            frontAttachmentEnd = configuredFrontAttachmentEnd;
            rearAttachmentEnd = configuredRearAttachmentEnd;
            leftConnector = configuredLeftConnector;
            rightConnector = configuredRightConnector;
        }
    }
}
