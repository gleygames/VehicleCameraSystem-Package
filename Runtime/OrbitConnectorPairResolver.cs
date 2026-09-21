using UnityEngine;

namespace Gley.CameraSystem
{
    internal class OrbitConnectorPairResolver
    {
        private const float PositionTolerance = 0.0001f;

        public GeneratedOrbitConnectorPair ConnectorPair { get; private set; }
        public OrbitConnectorPairResolutionResult ResolutionResult { get; private set; }
        public Vector3 RearBodyLocalPosition { get; private set; }

        public OrbitConnectorPairResolver(VehicleProfile frontProfile, VehicleProfile rearProfile, OrbitConnectorPairOverride connectorOverride)
        {
            TwoBodyOrbitConnectorGenerator connectorGenerator = new TwoBodyOrbitConnectorGenerator(frontProfile, rearProfile);

            if (connectorGenerator.GenerationResult != TwoBodyOrbitConnectorGenerationResult.Generated)
            {
                ResolutionResult = OrbitConnectorPairResolutionResult.ConnectorGenerationFailed;
                return;
            }

            RearBodyLocalPosition = connectorGenerator.RearBodyLocalPosition;

            if (connectorOverride == null)
            {
                ConnectorPair = connectorGenerator.ConnectorPair;
                ResolutionResult = OrbitConnectorPairResolutionResult.Generated;
                return;
            }

            if (!connectorOverride.Matches(frontProfile, rearProfile, OrbitAttachmentEnd.Rear, OrbitAttachmentEnd.Front))
            {
                ResolutionResult = OrbitConnectorPairResolutionResult.OverrideDoesNotMatchPair;
                return;
            }

            GeneratedOrbitConnectorPair overrideConnectorPair = connectorOverride.CreateConnectorPair();

            if (!HasMatchingConnectionEndpoints(connectorGenerator.ConnectorPair, overrideConnectorPair))
            {
                ResolutionResult = OrbitConnectorPairResolutionResult.OverrideDoesNotMatchConnection;
                return;
            }

            ConnectorPair = overrideConnectorPair;
            ResolutionResult = OrbitConnectorPairResolutionResult.Overridden;
        }

        private bool HasMatchingConnectionEndpoints(GeneratedOrbitConnectorPair generatedConnectorPair, GeneratedOrbitConnectorPair overrideConnectorPair)
        {
            if (overrideConnectorPair == null)
            {
                return false;
            }

            return ArePositionsEqual(generatedConnectorPair.LeftConnector.StartPosition, overrideConnectorPair.LeftConnector.StartPosition)
                && ArePositionsEqual(generatedConnectorPair.LeftConnector.EndPosition, overrideConnectorPair.LeftConnector.EndPosition)
                && ArePositionsEqual(generatedConnectorPair.RightConnector.StartPosition, overrideConnectorPair.RightConnector.StartPosition)
                && ArePositionsEqual(generatedConnectorPair.RightConnector.EndPosition, overrideConnectorPair.RightConnector.EndPosition);
        }

        private bool ArePositionsEqual(Vector3 firstPosition, Vector3 secondPosition)
        {
            return Vector3.Distance(firstPosition, secondPosition) <= PositionTolerance;
        }
    }
}
