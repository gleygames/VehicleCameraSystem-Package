using UnityEngine;

namespace Gley.CameraSystem
{
    public class ThreeBodyOrbitReferenceLayout
    {
        private readonly VehicleProfile leadProfile;
        private readonly VehicleProfile middleProfile;
        private readonly VehicleProfile rearProfile;
        private readonly OrbitConnectorPairOverride leadMiddleConnectorOverride;
        private readonly OrbitConnectorPairOverride middleRearConnectorOverride;

        public GeneratedOrbitConnectorPair LeadMiddleConnectors { get; private set; }
        public GeneratedOrbitConnectorPair MiddleRearConnectors { get; private set; }
        public OrbitConnectorPairResolutionResult LeadMiddleConnectorResolutionResult { get; private set; }
        public OrbitConnectorPairResolutionResult MiddleRearConnectorResolutionResult { get; private set; }
        public ThreeBodyOrbitReferenceLayoutResult LayoutResult { get; private set; }
        public Vector3 MiddleBodyLocalPosition { get; private set; }
        public Vector3 RearBodyLocalPosition { get; private set; }

        public ThreeBodyOrbitReferenceLayout(VehicleProfile leadVehicleProfile, VehicleProfile middleVehicleProfile, VehicleProfile rearVehicleProfile)
            : this(leadVehicleProfile, middleVehicleProfile, rearVehicleProfile, null, null)
        {
        }

        public ThreeBodyOrbitReferenceLayout(VehicleProfile leadVehicleProfile, VehicleProfile middleVehicleProfile, VehicleProfile rearVehicleProfile, OrbitConnectorPairOverride leadMiddleOverride, OrbitConnectorPairOverride middleRearOverride)
        {
            leadProfile = leadVehicleProfile;
            middleProfile = middleVehicleProfile;
            rearProfile = rearVehicleProfile;
            leadMiddleConnectorOverride = leadMiddleOverride;
            middleRearConnectorOverride = middleRearOverride;
            BuildLayout();
        }

        public Vector3 EvaluateMiddleWorldPosition(Transform leadBody)
        {
            return leadBody.TransformPoint(MiddleBodyLocalPosition);
        }

        public Vector3 EvaluateRearWorldPosition(Transform leadBody)
        {
            return leadBody.TransformPoint(RearBodyLocalPosition);
        }

        private void BuildLayout()
        {
            OrbitConnectorPairResolver leadMiddleResolver = new OrbitConnectorPairResolver(leadProfile, middleProfile, leadMiddleConnectorOverride);
            LeadMiddleConnectorResolutionResult = leadMiddleResolver.ResolutionResult;

            if (leadMiddleResolver.ResolutionResult == OrbitConnectorPairResolutionResult.ConnectorGenerationFailed)
            {
                LayoutResult = ThreeBodyOrbitReferenceLayoutResult.LeadMiddleConnectorGenerationFailed;
                return;
            }

            if (leadMiddleResolver.ResolutionResult != OrbitConnectorPairResolutionResult.Generated && leadMiddleResolver.ResolutionResult != OrbitConnectorPairResolutionResult.Overridden)
            {
                LayoutResult = ThreeBodyOrbitReferenceLayoutResult.LeadMiddleConnectorOverrideInvalid;
                return;
            }

            OrbitConnectorPairResolver middleRearResolver = new OrbitConnectorPairResolver(middleProfile, rearProfile, middleRearConnectorOverride);
            MiddleRearConnectorResolutionResult = middleRearResolver.ResolutionResult;

            if (middleRearResolver.ResolutionResult == OrbitConnectorPairResolutionResult.ConnectorGenerationFailed)
            {
                LayoutResult = ThreeBodyOrbitReferenceLayoutResult.MiddleRearConnectorGenerationFailed;
                return;
            }

            if (middleRearResolver.ResolutionResult != OrbitConnectorPairResolutionResult.Generated && middleRearResolver.ResolutionResult != OrbitConnectorPairResolutionResult.Overridden)
            {
                LayoutResult = ThreeBodyOrbitReferenceLayoutResult.MiddleRearConnectorOverrideInvalid;
                return;
            }

            MiddleBodyLocalPosition = leadMiddleResolver.RearBodyLocalPosition;
            RearBodyLocalPosition = MiddleBodyLocalPosition + middleRearResolver.RearBodyLocalPosition;
            LeadMiddleConnectors = leadMiddleResolver.ConnectorPair;
            MiddleRearConnectors = CreateTranslatedConnectorPair(middleRearResolver.ConnectorPair, MiddleBodyLocalPosition);
            LayoutResult = ThreeBodyOrbitReferenceLayoutResult.Valid;
        }

        private GeneratedOrbitConnectorPair CreateTranslatedConnectorPair(GeneratedOrbitConnectorPair connectorPair, Vector3 offset)
        {
            GeneratedOrbitConnector leftConnector = CreateTranslatedConnector(connectorPair.LeftConnector, offset);
            GeneratedOrbitConnector rightConnector = CreateTranslatedConnector(connectorPair.RightConnector, offset);
            return new GeneratedOrbitConnectorPair(leftConnector, rightConnector);
        }

        private GeneratedOrbitConnector CreateTranslatedConnector(GeneratedOrbitConnector connector, Vector3 offset)
        {
            return new GeneratedOrbitConnector(
                connector.StartPosition + offset,
                connector.StartControlPoint + offset,
                connector.EndControlPoint + offset,
                connector.EndPosition + offset);
        }
    }
}
