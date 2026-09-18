using UnityEngine;

namespace Gley.CameraSystem
{
    public class ThreeBodyOrbitReferenceLayout
    {
        private readonly VehicleProfile leadProfile;
        private readonly VehicleProfile middleProfile;
        private readonly VehicleProfile rearProfile;

        public GeneratedOrbitConnectorPair LeadMiddleConnectors { get; private set; }
        public GeneratedOrbitConnectorPair MiddleRearConnectors { get; private set; }
        public ThreeBodyOrbitReferenceLayoutResult LayoutResult { get; private set; }
        public Vector3 MiddleBodyLocalPosition { get; private set; }
        public Vector3 RearBodyLocalPosition { get; private set; }

        public ThreeBodyOrbitReferenceLayout(VehicleProfile leadVehicleProfile, VehicleProfile middleVehicleProfile, VehicleProfile rearVehicleProfile)
        {
            leadProfile = leadVehicleProfile;
            middleProfile = middleVehicleProfile;
            rearProfile = rearVehicleProfile;
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
            TwoBodyOrbitConnectorGenerator leadMiddleGenerator = new TwoBodyOrbitConnectorGenerator(leadProfile, middleProfile);

            if (leadMiddleGenerator.GenerationResult != TwoBodyOrbitConnectorGenerationResult.Generated)
            {
                LayoutResult = ThreeBodyOrbitReferenceLayoutResult.LeadMiddleConnectorGenerationFailed;
                return;
            }

            TwoBodyOrbitConnectorGenerator middleRearGenerator = new TwoBodyOrbitConnectorGenerator(middleProfile, rearProfile);

            if (middleRearGenerator.GenerationResult != TwoBodyOrbitConnectorGenerationResult.Generated)
            {
                LayoutResult = ThreeBodyOrbitReferenceLayoutResult.MiddleRearConnectorGenerationFailed;
                return;
            }

            MiddleBodyLocalPosition = leadMiddleGenerator.RearBodyLocalPosition;
            RearBodyLocalPosition = MiddleBodyLocalPosition + middleRearGenerator.RearBodyLocalPosition;
            LeadMiddleConnectors = leadMiddleGenerator.ConnectorPair;
            MiddleRearConnectors = CreateTranslatedConnectorPair(middleRearGenerator.ConnectorPair, MiddleBodyLocalPosition);
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
