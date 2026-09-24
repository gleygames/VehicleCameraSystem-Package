using UnityEngine;

namespace Gley.CameraSystem
{
    internal class OrbitConnectorPairResolver
    {
        public GeneratedOrbitConnectorPair ConnectorPair { get; private set; }
        public OrbitConnectorPairResolutionResult ResolutionResult { get; private set; }
        public Vector3 RearBodyLocalPosition { get; private set; }

        public OrbitConnectorPairResolver(VehicleProfile frontProfile, VehicleProfile rearProfile, OrbitConnectorPairOverride connectorOverride)
        {
            VehicleProfile[] profiles = { frontProfile, rearProfile };
            OrbitConnectorPairOverride[] overrides = { connectorOverride };
            VehicleOrbit rootOrbit = null;
            if (frontProfile != null)
            {
                rootOrbit = frontProfile.PrimaryOrbit;
            }

            ChainLayout layout = new ChainLayout(profiles, 0, rootOrbit, overrides);
            if (layout.LayoutResult != ChainLayoutResult.Valid)
            {
                ResolutionResult = OrbitConnectorPairResolutionResult.ConnectorGenerationFailed;
                return;
            }

            ConnectorPair = layout.ConnectorPairs[0];
            RearBodyLocalPosition = layout.Offsets[1];
            ResolutionResult = OrbitConnectorPairResolutionResult.Generated;
            if (connectorOverride != null && connectorOverride.Matches(frontProfile, rearProfile, OrbitAttachmentEnd.Rear, OrbitAttachmentEnd.Front))
            {
                ResolutionResult = OrbitConnectorPairResolutionResult.Overridden;
            }
        }
    }
}
