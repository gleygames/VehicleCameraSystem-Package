using UnityEngine;

namespace Gley.CameraSystem
{
    public class TwoBodyOrbitConnectorGenerator
    {
        private const float PlanarTolerance = 0.0001f;

        private readonly VehicleProfile frontProfile;
        private readonly VehicleProfile rearProfile;

        public GeneratedOrbitConnectorPair ConnectorPair { get; private set; }
        public TwoBodyOrbitConnectorGenerationResult GenerationResult { get; private set; }
        public Vector3 RearBodyLocalPosition { get; private set; }

        public TwoBodyOrbitConnectorGenerator(VehicleProfile frontVehicleProfile, VehicleProfile rearVehicleProfile)
        {
            frontProfile = frontVehicleProfile;
            rearProfile = rearVehicleProfile;
            GenerateConnectors();
        }

        private void GenerateConnectors()
        {
            if (frontProfile == null)
            {
                GenerationResult = TwoBodyOrbitConnectorGenerationResult.MissingFrontProfile;
                return;
            }

            if (rearProfile == null)
            {
                GenerationResult = TwoBodyOrbitConnectorGenerationResult.MissingRearProfile;
                return;
            }

            if (frontProfile.VehicleOrbit == null)
            {
                GenerationResult = TwoBodyOrbitConnectorGenerationResult.MissingFrontOrbit;
                return;
            }

            if (rearProfile.VehicleOrbit == null)
            {
                GenerationResult = TwoBodyOrbitConnectorGenerationResult.MissingRearOrbit;
                return;
            }

            ClosedBezierOrbit frontOrbit = new ClosedBezierOrbit(frontProfile.VehicleOrbit);
            ClosedBezierOrbit rearOrbit = new ClosedBezierOrbit(rearProfile.VehicleOrbit);

            if (frontOrbit.ValidationResult != OrbitValidationResult.Valid)
            {
                GenerationResult = TwoBodyOrbitConnectorGenerationResult.InvalidFrontOrbit;
                return;
            }

            if (rearOrbit.ValidationResult != OrbitValidationResult.Valid)
            {
                GenerationResult = TwoBodyOrbitConnectorGenerationResult.InvalidRearOrbit;
                return;
            }

            if (frontOrbit.RemovableSectionValidationResult != OrbitRemovableSectionValidationResult.Valid)
            {
                GenerationResult = TwoBodyOrbitConnectorGenerationResult.InvalidFrontRemovableSections;
                return;
            }

            if (rearOrbit.RemovableSectionValidationResult != OrbitRemovableSectionValidationResult.Valid)
            {
                GenerationResult = TwoBodyOrbitConnectorGenerationResult.InvalidRearRemovableSections;
                return;
            }

            if (frontProfile.ConnectorAnchors == null)
            {
                GenerationResult = TwoBodyOrbitConnectorGenerationResult.MissingFrontConnectorAnchors;
                return;
            }

            if (rearProfile.ConnectorAnchors == null)
            {
                GenerationResult = TwoBodyOrbitConnectorGenerationResult.MissingRearConnectorAnchors;
                return;
            }

            if (!AreConnectorAnchorsPlanar(frontProfile.ConnectorAnchors))
            {
                GenerationResult = TwoBodyOrbitConnectorGenerationResult.InvalidFrontConnectorAnchors;
                return;
            }

            if (!AreConnectorAnchorsPlanar(rearProfile.ConnectorAnchors))
            {
                GenerationResult = TwoBodyOrbitConnectorGenerationResult.InvalidRearConnectorAnchors;
                return;
            }

            RearBodyLocalPosition = frontProfile.ConnectorAnchors.RearLocalPosition - rearProfile.ConnectorAnchors.FrontLocalPosition;
            Vector3 frontSectionStart = frontOrbit.EvaluateBodyLocalRemovableSectionStart(OrbitAttachmentEnd.Rear);
            Vector3 frontSectionEnd = frontOrbit.EvaluateBodyLocalRemovableSectionEnd(OrbitAttachmentEnd.Rear);
            Vector3 rearSectionStart = rearOrbit.EvaluateBodyLocalRemovableSectionStart(OrbitAttachmentEnd.Front) + RearBodyLocalPosition;
            Vector3 rearSectionEnd = rearOrbit.EvaluateBodyLocalRemovableSectionEnd(OrbitAttachmentEnd.Front) + RearBodyLocalPosition;
            Vector3 frontLeftBoundary = frontSectionStart;
            Vector3 frontRightBoundary = frontSectionEnd;
            Vector3 rearLeftBoundary = rearSectionStart;
            Vector3 rearRightBoundary = rearSectionEnd;

            if (frontLeftBoundary.x > frontRightBoundary.x)
            {
                frontLeftBoundary = frontSectionEnd;
                frontRightBoundary = frontSectionStart;
            }

            if (rearLeftBoundary.x > rearRightBoundary.x)
            {
                rearLeftBoundary = rearSectionEnd;
                rearRightBoundary = rearSectionStart;
            }

            GeneratedOrbitConnector leftConnector = CreateStraightConnector(frontLeftBoundary, rearLeftBoundary);
            GeneratedOrbitConnector rightConnector = CreateStraightConnector(frontRightBoundary, rearRightBoundary);
            ConnectorPair = new GeneratedOrbitConnectorPair(leftConnector, rightConnector);
            GenerationResult = TwoBodyOrbitConnectorGenerationResult.Generated;
        }

        private bool AreConnectorAnchorsPlanar(VehicleConnectorAnchors connectorAnchors)
        {
            return Mathf.Abs(connectorAnchors.FrontLocalPosition.y) <= PlanarTolerance
                && Mathf.Abs(connectorAnchors.RearLocalPosition.y) <= PlanarTolerance;
        }

        private GeneratedOrbitConnector CreateStraightConnector(Vector3 startPosition, Vector3 endPosition)
        {
            Vector3 connectorOffset = endPosition - startPosition;
            Vector3 startControlPoint = startPosition + connectorOffset / 3f;
            Vector3 endControlPoint = startPosition + connectorOffset * (2f / 3f);
            return new GeneratedOrbitConnector(startPosition, startControlPoint, endControlPoint, endPosition);
        }
    }
}
