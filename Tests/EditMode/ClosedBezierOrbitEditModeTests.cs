using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Gley.CameraSystem.Tests.EditMode
{
    public class ClosedBezierOrbitEditModeTests
    {
        [Test]
        public void ClosedBezierOrbitSamplesStraightSegmentsByPhysicalDistance()
        {
            VehicleOrbit vehicleOrbit = CreateRectangleOrbit(20f, 10f, Quaternion.identity);
            ClosedBezierOrbit orbit = new ClosedBezierOrbit(vehicleOrbit);

            Vector3 firstQuarterPosition = orbit.EvaluateBodyLocalPosition(15f);
            Vector3 secondQuarterPosition = orbit.EvaluateBodyLocalPosition(25f);

            Assert.AreEqual(OrbitValidationResult.Valid, orbit.ValidationResult);
            Assert.IsTrue(orbit.IsClosed);
            Assert.AreEqual(60f, orbit.Length, 0.01f);
            Assert.AreEqual(new Vector3(15f, 0f, 0f), firstQuarterPosition);
            Assert.AreEqual(new Vector3(20f, 0f, 5f), secondQuarterPosition);
        }

        [Test]
        public void SelfIntersectingOrbitIsRejected()
        {
            VehicleOrbit vehicleOrbit = CreateBowTieOrbit();
            ClosedBezierOrbit orbit = new ClosedBezierOrbit(vehicleOrbit);

            Assert.AreEqual(OrbitValidationResult.SelfIntersecting, orbit.ValidationResult);
            Assert.IsFalse(orbit.IsClosed);
        }

        [Test]
        public void NonPlanarOrbitIsRejected()
        {
            VehicleOrbit vehicleOrbit = CreateRectangleOrbit(20f, 10f, Quaternion.identity);
            vehicleOrbit.Knots[0].Configure(new Vector3(0f, 1f, 0f), new Vector3(0f, 1f, 0f), new Vector3(20f / 3f, 1f, 0f));
            ClosedBezierOrbit orbit = new ClosedBezierOrbit(vehicleOrbit);

            Assert.AreEqual(OrbitValidationResult.NonPlanar, orbit.ValidationResult);
        }

        [Test]
        public void WatchMarkersInterpolateContinuouslyAndWrapAtTheOrbitStart()
        {
            Vector3 firstWatchPoint = new Vector3(2f, 4f, 6f);
            Vector3 secondWatchPoint = new Vector3(10f, 8f, 2f);
            VehicleOrbit vehicleOrbit = CreateRectangleOrbit(20f, 10f, Quaternion.identity);
            vehicleOrbit.ConfigureWatchMarkers(CreateWatchMarkers(firstWatchPoint, secondWatchPoint));
            ClosedBezierOrbit orbit = new ClosedBezierOrbit(vehicleOrbit);
            Vector3 firstInterpolatedWatchPoint = orbit.EvaluateBodyLocalWatchPoint(orbit.Length * 0.25f);
            Vector3 secondInterpolatedWatchPoint = orbit.EvaluateBodyLocalWatchPoint(orbit.Length * 0.75f);
            Vector3 expectedInterpolatedWatchPoint = new Vector3(6f, 6f, 4f);

            Assert.AreEqual(OrbitWatchMarkerValidationResult.Valid, orbit.WatchMarkerValidationResult);
            Assert.AreEqual(firstWatchPoint, orbit.EvaluateBodyLocalWatchPoint(0f));
            Assert.AreEqual(secondWatchPoint, orbit.EvaluateBodyLocalWatchPoint(orbit.Length * 0.5f));
            Assert.AreEqual(expectedInterpolatedWatchPoint, firstInterpolatedWatchPoint);
            Assert.AreEqual(expectedInterpolatedWatchPoint, secondInterpolatedWatchPoint);
        }

        [Test]
        public void InvalidWatchMarkerPositionBlocksPresentationActivation()
        {
            CameraViewPreset orbitViewPreset = ScriptableObject.CreateInstance<CameraViewPreset>();
            orbitViewPreset.Configure(CameraViewType.ExteriorPresentation);
            VehicleOrbit vehicleOrbit = CreateRectangleOrbit(20f, 10f, Quaternion.identity);
            vehicleOrbit.ConfigureWatchMarkers(CreateWatchMarkers(new Vector3(10f, 2f, 5f), new Vector3(10f, 4f, 5f)));
            vehicleOrbit.WatchMarkers[1].Configure(1f, new Vector3(10f, 4f, 5f));
            VehicleProfile vehicleProfile = ScriptableObject.CreateInstance<VehicleProfile>();
            vehicleProfile.ConfigureOrbit(vehicleOrbit);
            vehicleProfile.ConfigureOrbitView(orbitViewPreset);
            GameObject vehicleBodyObject = new GameObject("Vehicle Body");
            GameObject cameraObject = new GameObject("Assigned Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            GameObject controllerObject = new GameObject("Camera System Controller");
            CameraSystemController controller = controllerObject.AddComponent<CameraSystemController>();
            controller.AssignCamera(camera);
            controller.AssignVehicle(vehicleBodyObject.transform, vehicleProfile);
            controller.SelectViewPreset(orbitViewPreset);

            CameraSystemActivationResult activationResult = controller.Activate();

            Assert.AreEqual(CameraSystemActivationResult.InvalidWatchMarkers, activationResult);

            Object.DestroyImmediate(cameraObject);
            Object.DestroyImmediate(controllerObject);
            Object.DestroyImmediate(vehicleBodyObject);
            Object.DestroyImmediate(orbitViewPreset);
            Object.DestroyImmediate(vehicleProfile);
        }

        [Test]
        public void OrbitOffsetRangesValidateAndClampManualInput()
        {
            CameraViewPreset orbitViewPreset = ScriptableObject.CreateInstance<CameraViewPreset>();
            orbitViewPreset.Configure(CameraViewType.ExteriorPresentation);
            orbitViewPreset.ConfigureOffsetTravel(3f, 4f);
            VehicleOrbit vehicleOrbit = CreateRectangleOrbit(20f, 10f, Quaternion.identity);
            vehicleOrbit.ConfigureWatchMarkers(CreateWatchMarkers(new Vector3(10f, 2f, 5f), new Vector3(10f, 4f, 5f)));
            vehicleOrbit.ConfigureOffsetRanges(-1f, 1f, -2f, 2f);
            ClosedBezierOrbit orbit = new ClosedBezierOrbit(vehicleOrbit);
            VehicleOrbit invalidVehicleOrbit = CreateRectangleOrbit(20f, 10f, Quaternion.identity);
            invalidVehicleOrbit.ConfigureOffsetRanges(1f, -1f, -2f, 2f);
            ClosedBezierOrbit invalidOrbit = new ClosedBezierOrbit(invalidVehicleOrbit);
            VehicleOrbit invalidZoomVehicleOrbit = CreateRectangleOrbit(20f, 10f, Quaternion.identity);
            invalidZoomVehicleOrbit.ConfigureOffsetRanges(-1f, 1f, 2f, -2f);
            ClosedBezierOrbit invalidZoomOrbit = new ClosedBezierOrbit(invalidZoomVehicleOrbit);
            VehicleProfile vehicleProfile = ScriptableObject.CreateInstance<VehicleProfile>();
            vehicleProfile.ConfigureOrbit(vehicleOrbit);
            vehicleProfile.ConfigureOrbitView(orbitViewPreset);
            GameObject vehicleBodyObject = new GameObject("Vehicle Body");
            GameObject cameraObject = new GameObject("Assigned Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            GameObject controllerObject = new GameObject("Camera System Controller");
            CameraSystemController controller = controllerObject.AddComponent<CameraSystemController>();
            controller.AssignCamera(camera);
            controller.AssignVehicle(vehicleBodyObject.transform, vehicleProfile);
            controller.SelectViewPreset(orbitViewPreset);

            CameraSystemActivationResult activationResult = controller.Activate();
            controller.SetHeightIntent(2f);
            controller.SetZoomIntent(2f);
            controller.UpdateCameraVisuals(1f);
            float upperHeightOffset = controller.HeightOffset;
            float inwardZoomOffset = controller.ZoomOffset;
            controller.SetHeightIntent(-2f);
            controller.SetZoomIntent(-2f);
            controller.UpdateCameraVisuals(1f);

            Assert.AreEqual(OrbitOffsetRangeValidationResult.Valid, orbit.OffsetRangeValidationResult);
            Assert.AreEqual(Vector3.forward, orbit.EvaluateBodyLocalInwardNormal(10f));
            Assert.AreEqual(OrbitOffsetRangeValidationResult.InvalidHeightRange, invalidOrbit.OffsetRangeValidationResult);
            Assert.AreEqual(OrbitOffsetRangeValidationResult.InvalidZoomRange, invalidZoomOrbit.OffsetRangeValidationResult);
            Assert.AreEqual(CameraSystemActivationResult.Succeeded, activationResult);
            Assert.AreEqual(1f, upperHeightOffset, 0.0001f);
            Assert.AreEqual(2f, inwardZoomOffset, 0.0001f);
            Assert.AreEqual(-1f, controller.HeightOffset, 0.0001f);
            Assert.AreEqual(-2f, controller.ZoomOffset, 0.0001f);

            Object.DestroyImmediate(cameraObject);
            Object.DestroyImmediate(controllerObject);
            Object.DestroyImmediate(vehicleBodyObject);
            Object.DestroyImmediate(orbitViewPreset);
            Object.DestroyImmediate(vehicleProfile);
        }

        [Test]
        public void RemovableSectionsValidateCircularRangesAndExposeExactBoundaries()
        {
            VehicleOrbit vehicleOrbit = CreateRectangleOrbit(20f, 10f, Quaternion.identity);
            OrbitRemovableSection frontSection = CreateRemovableSection(0.8f, 0.1f);
            OrbitRemovableSection rearSection = CreateRemovableSection(0.3f, 0.5f);
            vehicleOrbit.ConfigureRemovableSections(frontSection, rearSection);
            ClosedBezierOrbit orbit = new ClosedBezierOrbit(vehicleOrbit);
            VehicleOrbit overlappingVehicleOrbit = CreateRectangleOrbit(20f, 10f, Quaternion.identity);
            overlappingVehicleOrbit.ConfigureRemovableSections(CreateRemovableSection(0.1f, 0.4f), CreateRemovableSection(0.3f, 0.6f));
            ClosedBezierOrbit overlappingOrbit = new ClosedBezierOrbit(overlappingVehicleOrbit);

            Assert.AreEqual(OrbitRemovableSectionValidationResult.Valid, orbit.RemovableSectionValidationResult);
            AssertPosition(new Vector3(2f, 0f, 10f), orbit.EvaluateBodyLocalRemovableSectionStart(OrbitAttachmentEnd.Front));
            AssertPosition(new Vector3(6f, 0f, 0f), orbit.EvaluateBodyLocalRemovableSectionEnd(OrbitAttachmentEnd.Front));
            AssertPosition(new Vector3(18f, 0f, 0f), orbit.EvaluateBodyLocalRemovableSectionStart(OrbitAttachmentEnd.Rear));
            AssertPosition(new Vector3(20f, 0f, 10f), orbit.EvaluateBodyLocalRemovableSectionEnd(OrbitAttachmentEnd.Rear));
            Assert.AreEqual(OrbitRemovableSectionValidationResult.OverlappingSections, overlappingOrbit.RemovableSectionValidationResult);
        }

        [Test]
        public void OrbitAttachmentMergeDefaultsEnabledAndStaysIndependentPerOrbit()
        {
            VehicleOrbit firstVehicleOrbit = CreateRectangleOrbit(20f, 10f, Quaternion.identity);
            VehicleOrbit secondVehicleOrbit = CreateRectangleOrbit(40f, 20f, Quaternion.identity);
            secondVehicleOrbit.ConfigureAttachmentMerge(false);

            Assert.IsTrue(firstVehicleOrbit.MergeWhenAttached);
            Assert.IsFalse(secondVehicleOrbit.MergeWhenAttached);
        }

        [Test]
        public void GeneratedConnectorsUseProfileAnchorsAndStraightCubicControls()
        {
            VehicleProfile frontProfile = CreateConnectorProfile(20f, 10f, new Vector3(10f, 0f, 0f), new Vector3(10f, 0f, 10f));
            VehicleProfile rearProfile = CreateConnectorProfile(40f, 20f, new Vector3(20f, 0f, 0f), new Vector3(20f, 0f, 20f));
            TwoBodyOrbitConnectorGenerator connectorGenerator = new TwoBodyOrbitConnectorGenerator(frontProfile, rearProfile);
            GeneratedOrbitConnector leftConnector = connectorGenerator.ConnectorPair.LeftConnector;
            GeneratedOrbitConnector rightConnector = connectorGenerator.ConnectorPair.RightConnector;

            Assert.AreEqual(TwoBodyOrbitConnectorGenerationResult.Generated, connectorGenerator.GenerationResult);
            AssertPosition(new Vector3(-10f, 0f, 10f), connectorGenerator.RearBodyLocalPosition);
            AssertPosition(new Vector3(14f, 0f, 10f), leftConnector.StartPosition);
            AssertPosition(new Vector3(-10f, 0f, 10f), leftConnector.EndPosition);
            AssertPosition(new Vector3(6f, 0f, 10f), leftConnector.StartControlPoint);
            AssertPosition(new Vector3(-2f, 0f, 10f), leftConnector.EndControlPoint);
            AssertPosition(new Vector3(20f, 0f, 4f), rightConnector.StartPosition);
            AssertPosition(new Vector3(14f, 0f, 10f), rightConnector.EndPosition);
            AssertPosition(new Vector3(18f, 0f, 6f), rightConnector.StartControlPoint);
            AssertPosition(new Vector3(16f, 0f, 8f), rightConnector.EndControlPoint);

            Object.DestroyImmediate(frontProfile);
            Object.DestroyImmediate(rearProfile);
        }

        [Test]
        public void TwoBodyClosedBezierOrbitAssemblesAContinuousLoop()
        {
            VehicleProfile frontProfile = CreateConnectorProfile(20f, 10f, new Vector3(10f, 0f, 0f), new Vector3(10f, 0f, 10f));
            VehicleProfile rearProfile = CreateConnectorProfile(40f, 20f, new Vector3(20f, 0f, 0f), new Vector3(20f, 0f, 20f));
            frontProfile.VehicleOrbit.ConfigureRemovableSections(CreateRemovableSection(0f, 0.2f), CreateRemovableSection(0.5f, 5f / 6f));
            rearProfile.VehicleOrbit.ConfigureRemovableSections(CreateRemovableSection(0f, 1f / 3f), CreateRemovableSection(0.4f, 0.6f));
            TwoBodyClosedBezierOrbit orbit = new TwoBodyClosedBezierOrbit(frontProfile, rearProfile);

            Assert.AreEqual(TwoBodyClosedBezierOrbitAssemblyResult.Valid, orbit.AssemblyResult);
            Assert.IsTrue(orbit.IsClosed);
            Assert.AreEqual(8, orbit.Segments.Count);

            AssembledBezierOrbitSegment rightConnector = orbit.Segments[3];
            AssembledBezierOrbitSegment leftConnector = orbit.Segments[7];

            AssertPosition(new Vector3(0f, 0f, 10f), orbit.Segments[0].StartPosition);
            AssertPosition(new Vector3(20f, 0f, 10f), rightConnector.StartPosition);
            AssertPosition(new Vector3(30f, 0f, 10f), rightConnector.EndPosition);
            AssertPosition(new Vector3(-10f, 0f, 10f), leftConnector.StartPosition);
            AssertPosition(orbit.Segments[0].StartPosition, leftConnector.EndPosition);
            AssertPosition(orbit.Segments[0].StartPosition, orbit.EvaluateLeadBodyLocalPosition(orbit.Length));

            Object.DestroyImmediate(frontProfile);
            Object.DestroyImmediate(rearProfile);
        }

        [Test]
        public void TwoBodyClosedBezierOrbitRejectsOverlappingPairGeometry()
        {
            VehicleProfile frontProfile = CreateConnectorProfile(20f, 10f, new Vector3(10f, 0f, 0f), new Vector3(10f, 0f, 10f));
            VehicleProfile rearProfile = CreateConnectorProfile(40f, 20f, new Vector3(20f, 0f, 0f), new Vector3(20f, 0f, 20f));
            TwoBodyClosedBezierOrbit orbit = new TwoBodyClosedBezierOrbit(frontProfile, rearProfile);

            Assert.AreEqual(TwoBodyClosedBezierOrbitAssemblyResult.SelfIntersecting, orbit.AssemblyResult);
            Assert.IsFalse(orbit.IsClosed);

            Object.DestroyImmediate(frontProfile);
            Object.DestroyImmediate(rearProfile);
        }

        [Test]
        public void AttachmentRemapPreservesRetainedFrontPositionsAndMapsRemovedPositionsToTheRear()
        {
            VehicleProfile frontProfile = CreateConnectorProfile(20f, 10f, new Vector3(10f, 0f, 0f), new Vector3(10f, 0f, 10f));
            VehicleProfile rearProfile = CreateConnectorProfile(40f, 20f, new Vector3(20f, 0f, 0f), new Vector3(20f, 0f, 20f));
            frontProfile.VehicleOrbit.ConfigureRemovableSections(CreateRemovableSection(0f, 0.2f), CreateRemovableSection(0.5f, 5f / 6f));
            rearProfile.VehicleOrbit.ConfigureRemovableSections(CreateRemovableSection(0f, 1f / 3f), CreateRemovableSection(0.4f, 0.6f));
            TwoBodyOrbitAttachmentRemapResolver remapResolver = new TwoBodyOrbitAttachmentRemapResolver(frontProfile, rearProfile);
            ClosedBezierOrbit frontOrbit = new ClosedBezierOrbit(frontProfile.VehicleOrbit);
            ClosedBezierOrbit rearOrbit = new ClosedBezierOrbit(rearProfile.VehicleOrbit);
            TwoBodyOrbitConnectorGenerator connectorGenerator = new TwoBodyOrbitConnectorGenerator(frontProfile, rearProfile);

            OrbitAttachmentRemapResult retainedResult = remapResolver.ResolveFrontOrbitDistance(10f);
            float retainedDistance = remapResolver.RemappedOrbitDistance;
            Vector3 retainedPosition = remapResolver.CombinedOrbit.EvaluateLeadBodyLocalPosition(retainedDistance);
            OrbitAttachmentRemapResult removedResult = remapResolver.ResolveFrontOrbitDistance(40f);
            float removedDistance = remapResolver.RemappedOrbitDistance;
            Vector3 removedPosition = remapResolver.CombinedOrbit.EvaluateLeadBodyLocalPosition(removedDistance);
            Vector3 expectedRearPosition = rearOrbit.EvaluateBodyLocalPosition(rearOrbit.Length * 0.5f) + connectorGenerator.RearBodyLocalPosition;

            Assert.AreEqual(OrbitAttachmentRemapResult.RetainedFrontPosition, retainedResult);
            AssertPosition(frontOrbit.EvaluateBodyLocalPosition(10f), retainedPosition);
            Assert.AreEqual(OrbitAttachmentRemapResult.RemovedFrontPositionMappedToRear, removedResult);
            Assert.AreEqual(70f, removedDistance, 0.0001f);
            AssertPosition(expectedRearPosition, removedPosition);

            Object.DestroyImmediate(frontProfile);
            Object.DestroyImmediate(rearProfile);
        }

        [Test]
        public void AttachmentRemapPreservesCurvedRetainedFrontPositions()
        {
            VehicleProfile frontProfile = CreateCurvedConnectorProfile(20f, 10f, new Vector3(10f, 0f, 0f), new Vector3(10f, 0f, 10f));
            VehicleProfile rearProfile = CreateConnectorProfile(40f, 20f, new Vector3(20f, 0f, 0f), new Vector3(20f, 0f, 20f));
            frontProfile.VehicleOrbit.ConfigureRemovableSections(CreateRemovableSection(0f, 0.2f), CreateRemovableSection(0.5f, 0.84f));
            rearProfile.VehicleOrbit.ConfigureRemovableSections(CreateRemovableSection(0f, 1f / 3f), CreateRemovableSection(0.4f, 0.6f));
            ClosedBezierOrbit frontOrbit = new ClosedBezierOrbit(frontProfile.VehicleOrbit);
            TwoBodyOrbitAttachmentRemapResolver remapResolver = new TwoBodyOrbitAttachmentRemapResolver(frontProfile, rearProfile);
            float frontDistance = frontOrbit.Length * 0.15f;

            OrbitAttachmentRemapResult remapResult = remapResolver.ResolveFrontOrbitDistance(frontDistance);
            Vector3 expectedPosition = frontOrbit.EvaluateBodyLocalPosition(frontDistance);
            Vector3 remappedPosition = remapResolver.CombinedOrbit.EvaluateLeadBodyLocalPosition(remapResolver.RemappedOrbitDistance);

            Assert.AreEqual(TwoBodyClosedBezierOrbitAssemblyResult.Valid, remapResolver.CombinedOrbit.AssemblyResult);
            Assert.AreEqual(OrbitAttachmentRemapResult.RetainedFrontPosition, remapResult);
            AssertPosition(expectedPosition, remappedPosition);

            Object.DestroyImmediate(frontProfile);
            Object.DestroyImmediate(rearProfile);
        }

        [Test]
        public void TwoBodyClosedBezierOrbitPreservesBodyWatchTracksAndInterpolatesAcrossConnectors()
        {
            VehicleProfile frontProfile = CreateConnectorProfile(20f, 10f, new Vector3(10f, 0f, 0f), new Vector3(10f, 0f, 10f));
            VehicleProfile rearProfile = CreateConnectorProfile(40f, 20f, new Vector3(20f, 0f, 0f), new Vector3(20f, 0f, 20f));
            frontProfile.VehicleOrbit.ConfigureWatchMarkers(CreateWatchMarkers(new Vector3(2f, 3f, 4f), new Vector3(8f, 5f, 6f)));
            rearProfile.VehicleOrbit.ConfigureWatchMarkers(CreateWatchMarkers(new Vector3(12f, 4f, 8f), new Vector3(24f, 6f, 16f)));
            frontProfile.VehicleOrbit.ConfigureRemovableSections(CreateRemovableSection(0f, 0.2f), CreateRemovableSection(0.5f, 5f / 6f));
            rearProfile.VehicleOrbit.ConfigureRemovableSections(CreateRemovableSection(0f, 1f / 3f), CreateRemovableSection(0.4f, 0.6f));
            ClosedBezierOrbit frontOrbit = new ClosedBezierOrbit(frontProfile.VehicleOrbit);
            ClosedBezierOrbit rearOrbit = new ClosedBezierOrbit(rearProfile.VehicleOrbit);
            TwoBodyOrbitConnectorGenerator connectorGenerator = new TwoBodyOrbitConnectorGenerator(frontProfile, rearProfile);
            TwoBodyClosedBezierOrbit orbit = new TwoBodyClosedBezierOrbit(frontProfile, rearProfile);
            float rightConnectorStartDistance = GetAssembledSegmentStartDistance(orbit.Segments, 3);
            float rightConnectorLength = GetAssembledSegmentLength(orbit.Segments[3]);
            Vector3 expectedFrontWatchPoint = frontOrbit.EvaluateBodyLocalWatchPoint(frontOrbit.Length * (5f / 6f));
            Vector3 expectedRearWatchPoint = rearOrbit.EvaluateBodyLocalWatchPoint(rearOrbit.Length / 3f) + connectorGenerator.RearBodyLocalPosition;
            Vector3 expectedConnectorWatchPoint = Vector3.Lerp(
                frontOrbit.EvaluateBodyLocalWatchPoint(frontOrbit.Length * 0.5f),
                expectedRearWatchPoint,
                0.5f);

            Assert.AreEqual(TwoBodyClosedBezierOrbitAssemblyResult.Valid, orbit.AssemblyResult);
            Assert.AreEqual(OrbitWatchMarkerValidationResult.Valid, orbit.FrontWatchMarkerValidationResult);
            Assert.AreEqual(OrbitWatchMarkerValidationResult.Valid, orbit.RearWatchMarkerValidationResult);
            Assert.IsTrue(orbit.HasValidWatchMarkers);
            AssertPosition(expectedFrontWatchPoint, orbit.EvaluateLeadBodyLocalWatchPoint(0f));
            AssertPosition(expectedConnectorWatchPoint, orbit.EvaluateLeadBodyLocalWatchPoint(rightConnectorStartDistance + rightConnectorLength * 0.5f));
            AssertPosition(expectedRearWatchPoint, orbit.EvaluateLeadBodyLocalWatchPoint(rightConnectorStartDistance + rightConnectorLength));

            Object.DestroyImmediate(frontProfile);
            Object.DestroyImmediate(rearProfile);
        }

        [Test]
        public void TwoBodyFacadeRetainsTheEstablishedAssembledSegmentOrder()
        {
            VehicleProfile frontProfile = CreateConnectorProfile(20f, 10f, new Vector3(10f, 0f, 0f), new Vector3(10f, 0f, 10f));
            VehicleProfile rearProfile = CreateConnectorProfile(40f, 20f, new Vector3(20f, 0f, 0f), new Vector3(20f, 0f, 20f));
            frontProfile.VehicleOrbit.ConfigureRemovableSections(CreateRemovableSection(0f, 0.2f), CreateRemovableSection(0.5f, 5f / 6f));
            rearProfile.VehicleOrbit.ConfigureRemovableSections(CreateRemovableSection(0f, 1f / 3f), CreateRemovableSection(0.4f, 0.6f));
            TwoBodyClosedBezierOrbit orbit = new TwoBodyClosedBezierOrbit(frontProfile, rearProfile);

            Assert.AreEqual(TwoBodyClosedBezierOrbitAssemblyResult.Valid, orbit.AssemblyResult);
            Assert.AreEqual(8, orbit.Segments.Count);
            AssertPosition(new Vector3(0f, 0f, 10f), orbit.Segments[0].StartPosition);
            AssertPosition(new Vector3(20f, 0f, 10f), orbit.Segments[3].StartPosition);
            AssertPosition(new Vector3(30f, 0f, 10f), orbit.Segments[3].EndPosition);
            AssertPosition(new Vector3(-10f, 0f, 10f), orbit.Segments[7].StartPosition);
            AssertPosition(orbit.Segments[0].StartPosition, orbit.Segments[7].EndPosition);

            Object.DestroyImmediate(frontProfile);
            Object.DestroyImmediate(rearProfile);
        }

        [Test]
        public void TwoBodyRebuildRefreshesProfileGeometryAndWatchMarkers()
        {
            VehicleProfile frontProfile = CreateConnectorProfile(20f, 10f, new Vector3(10f, 0f, 0f), new Vector3(10f, 0f, 12f));
            VehicleProfile rearProfile = CreateConnectorProfile(20f, 10f, new Vector3(10f, 0f, 0f), new Vector3(10f, 0f, 12f));
            frontProfile.VehicleOrbit.ConfigureWatchMarkers(CreateWatchMarkers(new Vector3(2f, 3f, 4f), new Vector3(8f, 5f, 6f)));
            rearProfile.VehicleOrbit.ConfigureWatchMarkers(CreateWatchMarkers(new Vector3(12f, 4f, 8f), new Vector3(16f, 6f, 10f)));
            frontProfile.VehicleOrbit.ConfigureRemovableSections(CreateRemovableSection(0f, 1f / 3f), CreateRemovableSection(0.5f, 5f / 6f));
            rearProfile.VehicleOrbit.ConfigureRemovableSections(CreateRemovableSection(0f, 1f / 3f), CreateRemovableSection(0.5f, 5f / 6f));
            TwoBodyClosedBezierOrbit orbit = new TwoBodyClosedBezierOrbit(frontProfile, rearProfile);
            Vector3 initialWatchPoint = orbit.EvaluateLeadBodyLocalWatchPoint(0f);
            VehicleOrbit replacementOrbit = CreateRectangleOrbit(20f, 10f, Quaternion.identity);
            replacementOrbit.ConfigureWatchMarkers(CreateWatchMarkers(new Vector3(20f, 3f, 4f), new Vector3(30f, 5f, 6f)));
            replacementOrbit.ConfigureRemovableSections(CreateRemovableSection(0f, 1f / 3f), CreateRemovableSection(0.5f, 5f / 6f));
            frontProfile.ConfigureOrbit(replacementOrbit);
            VehicleConnectorAnchors rearAnchors = new VehicleConnectorAnchors();
            rearAnchors.Configure(new Vector3(10f, 0f, -2f), new Vector3(10f, 0f, 12f));
            rearProfile.ConfigureConnectorAnchors(rearAnchors);

            orbit.Rebuild();
            ClosedBezierOrbit expectedFrontOrbit = new ClosedBezierOrbit(replacementOrbit);
            Vector3 expectedWatchPoint = expectedFrontOrbit.EvaluateBodyLocalWatchPoint(expectedFrontOrbit.Length * (5f / 6f));

            Assert.AreEqual(TwoBodyClosedBezierOrbitAssemblyResult.Valid, orbit.AssemblyResult);
            AssertPosition(new Vector3(20f, 0f, 14f), orbit.Segments[3].EndPosition);
            Assert.Greater(Vector3.Distance(initialWatchPoint, orbit.EvaluateLeadBodyLocalWatchPoint(0f)), 1f);
            AssertPosition(expectedWatchPoint, orbit.EvaluateLeadBodyLocalWatchPoint(0f));

            replacementOrbit.WatchMarkers[1].Configure(1f, new Vector3(30f, 5f, 6f));
            orbit.Rebuild();

            Assert.AreEqual(OrbitWatchMarkerValidationResult.InvalidMarkerPosition, orbit.FrontWatchMarkerValidationResult);
            Assert.IsFalse(orbit.HasValidWatchMarkers);

            Object.DestroyImmediate(frontProfile);
            Object.DestroyImmediate(rearProfile);
        }

        [Test]
        public void ThreeBodyClosedBezierOrbitBuildsAClosedReferenceLoopWithGeneratedPairs()
        {
            VehicleProfile leadProfile = CreateConnectorProfile(20f, 10f, new Vector3(10f, 0f, 0f), new Vector3(10f, 0f, 12f));
            VehicleProfile middleProfile = CreateConnectorProfile(20f, 10f, new Vector3(10f, 0f, 0f), new Vector3(10f, 0f, 12f));
            VehicleProfile rearProfile = CreateConnectorProfile(20f, 10f, new Vector3(10f, 0f, 0f), new Vector3(10f, 0f, 12f));
            leadProfile.VehicleOrbit.ConfigureWatchMarkers(CreateWatchMarkers(new Vector3(10f, 3f, 5f), new Vector3(10f, 4f, 5f)));
            middleProfile.VehicleOrbit.ConfigureWatchMarkers(CreateWatchMarkers(new Vector3(10f, 5f, 5f), new Vector3(10f, 6f, 5f)));
            rearProfile.VehicleOrbit.ConfigureWatchMarkers(CreateWatchMarkers(new Vector3(10f, 7f, 5f), new Vector3(10f, 8f, 5f)));
            leadProfile.VehicleOrbit.ConfigureRemovableSections(CreateRemovableSection(0f, 1f / 3f), CreateRemovableSection(0.5f, 5f / 6f));
            middleProfile.VehicleOrbit.ConfigureRemovableSections(CreateRemovableSection(0f, 1f / 3f), CreateRemovableSection(0.5f, 5f / 6f));
            rearProfile.VehicleOrbit.ConfigureRemovableSections(CreateRemovableSection(0f, 1f / 3f), CreateRemovableSection(0.5f, 5f / 6f));
            ThreeBodyClosedBezierOrbit orbit = new ThreeBodyClosedBezierOrbit(leadProfile, middleProfile, rearProfile);

            Assert.AreEqual(ThreeBodyOrbitReferenceLayoutResult.Valid, orbit.ReferenceLayout.LayoutResult);
            Assert.AreEqual(ThreeBodyClosedBezierOrbitAssemblyResult.Valid, orbit.AssemblyResult);
            Assert.IsTrue(orbit.IsClosed);
            Assert.IsTrue(orbit.HasValidWatchMarkers);
            Assert.AreEqual(12, orbit.Segments.Count);
            Assert.AreEqual(108f, orbit.Length, 0.001f);
            AssertPosition(new Vector3(0f, 0f, 12f), orbit.ReferenceLayout.MiddleBodyLocalPosition);
            AssertPosition(new Vector3(0f, 0f, 24f), orbit.ReferenceLayout.RearBodyLocalPosition);
            AssertPosition(new Vector3(0f, 0f, 10f), orbit.Segments[0].StartPosition);
            AssertPosition(new Vector3(20f, 0f, 10f), orbit.ReferenceLayout.LeadMiddleConnectors.RightConnector.StartPosition);
            AssertPosition(new Vector3(20f, 0f, 12f), orbit.ReferenceLayout.LeadMiddleConnectors.RightConnector.EndPosition);
            AssertPosition(new Vector3(20f, 0f, 22f), orbit.ReferenceLayout.MiddleRearConnectors.RightConnector.StartPosition);
            AssertPosition(new Vector3(20f, 0f, 24f), orbit.ReferenceLayout.MiddleRearConnectors.RightConnector.EndPosition);

            for (int segmentIndex = 0; segmentIndex < orbit.Segments.Count; segmentIndex++)
            {
                int nextSegmentIndex = (segmentIndex + 1) % orbit.Segments.Count;
                AssertPosition(orbit.Segments[segmentIndex].EndPosition, orbit.Segments[nextSegmentIndex].StartPosition);
                Assert.Greater(Vector3.Distance(orbit.Segments[segmentIndex].StartPosition, orbit.Segments[segmentIndex].EndPosition), 1.99f);
            }

            AssertPosition(new Vector3(20f, 0f, 11f), orbit.EvaluateLeadBodyLocalPosition(41f));
            AssertPosition(new Vector3(20f, 0f, 23f), orbit.EvaluateLeadBodyLocalPosition(53f));
            AssertPosition(new Vector3(0f, 0f, 23f), orbit.EvaluateLeadBodyLocalPosition(95f));
            AssertPosition(new Vector3(0f, 0f, 11f), orbit.EvaluateLeadBodyLocalPosition(107f));
            AssertPosition(orbit.Segments[0].StartPosition, orbit.EvaluateLeadBodyLocalPosition(orbit.Length));

            Object.DestroyImmediate(leadProfile);
            Object.DestroyImmediate(middleProfile);
            Object.DestroyImmediate(rearProfile);
        }

        [Test]
        public void ThreeBodyClosedBezierOrbitRejectsConnectorsThatRetraceRetainedEdges()
        {
            VehicleProfile leadProfile = CreateConnectorProfile(20f, 10f, new Vector3(10f, 0f, 0f), new Vector3(10f, 0f, 10f));
            VehicleProfile middleProfile = CreateConnectorProfile(20f, 10f, new Vector3(10f, 0f, 0f), new Vector3(10f, 0f, 10f));
            VehicleProfile rearProfile = CreateConnectorProfile(20f, 10f, new Vector3(10f, 0f, 0f), new Vector3(10f, 0f, 10f));
            leadProfile.VehicleOrbit.ConfigureRemovableSections(CreateRemovableSection(0f, 0.2f), CreateRemovableSection(0.5f, 5f / 6f));
            middleProfile.VehicleOrbit.ConfigureRemovableSections(CreateRemovableSection(0f, 0.2f), CreateRemovableSection(0.5f, 5f / 6f));
            rearProfile.VehicleOrbit.ConfigureRemovableSections(CreateRemovableSection(0f, 0.2f), CreateRemovableSection(0.5f, 5f / 6f));
            ThreeBodyClosedBezierOrbit orbit = new ThreeBodyClosedBezierOrbit(leadProfile, middleProfile, rearProfile);

            Assert.AreEqual(ThreeBodyOrbitReferenceLayoutResult.Valid, orbit.ReferenceLayout.LayoutResult);
            AssertPosition(new Vector3(20f, 0f, 10f), orbit.ReferenceLayout.LeadMiddleConnectors.RightConnector.StartPosition);
            AssertPosition(new Vector3(12f, 0f, 10f), orbit.ReferenceLayout.LeadMiddleConnectors.RightConnector.EndPosition);
            Assert.AreEqual(ThreeBodyClosedBezierOrbitAssemblyResult.SelfIntersecting, orbit.AssemblyResult);
            Assert.IsFalse(orbit.IsClosed);

            Object.DestroyImmediate(leadProfile);
            Object.DestroyImmediate(middleProfile);
            Object.DestroyImmediate(rearProfile);
        }

        [Test]
        public void ThreeBodyClosedBezierOrbitResolvesOrderedPairOverridesAndRejectsMismatchedPairs()
        {
            VehicleProfile leadProfile = CreateConnectorProfile(20f, 10f, new Vector3(10f, 0f, 0f), new Vector3(10f, 0f, 12f));
            VehicleProfile middleProfile = CreateConnectorProfile(20f, 10f, new Vector3(10f, 0f, 0f), new Vector3(10f, 0f, 12f));
            VehicleProfile rearProfile = CreateConnectorProfile(20f, 10f, new Vector3(10f, 0f, 0f), new Vector3(10f, 0f, 12f));
            leadProfile.VehicleOrbit.ConfigureWatchMarkers(CreateWatchMarkers(new Vector3(10f, 3f, 5f), new Vector3(10f, 4f, 5f)));
            middleProfile.VehicleOrbit.ConfigureWatchMarkers(CreateWatchMarkers(new Vector3(10f, 3f, 5f), new Vector3(10f, 4f, 5f)));
            rearProfile.VehicleOrbit.ConfigureWatchMarkers(CreateWatchMarkers(new Vector3(10f, 3f, 5f), new Vector3(10f, 4f, 5f)));
            leadProfile.VehicleOrbit.ConfigureRemovableSections(CreateRemovableSection(0f, 1f / 3f), CreateRemovableSection(0.5f, 5f / 6f));
            middleProfile.VehicleOrbit.ConfigureRemovableSections(CreateRemovableSection(0f, 1f / 3f), CreateRemovableSection(0.5f, 5f / 6f));
            rearProfile.VehicleOrbit.ConfigureRemovableSections(CreateRemovableSection(0f, 1f / 3f), CreateRemovableSection(0.5f, 5f / 6f));
            TwoBodyOrbitConnectorGenerator leadMiddleGenerator = new TwoBodyOrbitConnectorGenerator(leadProfile, middleProfile);
            TwoBodyOrbitConnectorGenerator middleRearGenerator = new TwoBodyOrbitConnectorGenerator(middleProfile, rearProfile);
            OrbitConnectorPairOverride leadMiddleOverride = CreateConnectorOverride(leadProfile, middleProfile, leadMiddleGenerator.ConnectorPair, Vector3.left, Vector3.right);
            OrbitConnectorPairOverride middleRearOverride = CreateConnectorOverride(middleProfile, rearProfile, middleRearGenerator.ConnectorPair, Vector3.left, Vector3.right);
            ThreeBodyClosedBezierOrbit orbit = new ThreeBodyClosedBezierOrbit(leadProfile, middleProfile, rearProfile, leadMiddleOverride, middleRearOverride);
            OrbitConnectorPairOverride mismatchedOverride = CreateConnectorOverride(rearProfile, middleProfile, leadMiddleGenerator.ConnectorPair, Vector3.left, Vector3.right);
            ThreeBodyClosedBezierOrbit invalidOrbit = new ThreeBodyClosedBezierOrbit(leadProfile, middleProfile, rearProfile, mismatchedOverride, null);
            Vector3 expectedLeadMiddleRightControl = leadMiddleGenerator.ConnectorPair.RightConnector.StartControlPoint + Vector3.right;
            Vector3 expectedMiddleRearRightControl = middleRearGenerator.ConnectorPair.RightConnector.StartControlPoint + new Vector3(1f, 0f, 12f);

            Assert.AreEqual(TwoBodyOrbitConnectorGenerationResult.Generated, leadMiddleGenerator.GenerationResult);
            Assert.AreEqual(TwoBodyOrbitConnectorGenerationResult.Generated, middleRearGenerator.GenerationResult);
            Assert.AreEqual(ThreeBodyClosedBezierOrbitAssemblyResult.Valid, orbit.AssemblyResult);
            Assert.AreEqual(OrbitConnectorPairResolutionResult.Overridden, orbit.ReferenceLayout.LeadMiddleConnectorResolutionResult);
            Assert.AreEqual(OrbitConnectorPairResolutionResult.Overridden, orbit.ReferenceLayout.MiddleRearConnectorResolutionResult);
            AssertPosition(expectedLeadMiddleRightControl, orbit.Segments[3].StartControlPoint);
            AssertPosition(expectedMiddleRearRightControl, orbit.Segments[5].StartControlPoint);
            Assert.AreEqual(ThreeBodyClosedBezierOrbitAssemblyResult.LeadMiddleConnectorOverrideInvalid, invalidOrbit.AssemblyResult);
            Assert.AreEqual(OrbitConnectorPairResolutionResult.OverrideDoesNotMatchPair, invalidOrbit.ReferenceLayout.LeadMiddleConnectorResolutionResult);

            Object.DestroyImmediate(leadMiddleOverride);
            Object.DestroyImmediate(middleRearOverride);
            Object.DestroyImmediate(mismatchedOverride);
            Object.DestroyImmediate(leadProfile);
            Object.DestroyImmediate(middleProfile);
            Object.DestroyImmediate(rearProfile);
        }

        private void AssertPosition(Vector3 expectedPosition, Vector3 actualPosition)
        {
            Assert.Less(Vector3.Distance(expectedPosition, actualPosition), 0.0001f);
        }

        private float GetAssembledSegmentStartDistance(IReadOnlyList<AssembledBezierOrbitSegment> segments, int segmentIndex)
        {
            float distance = 0f;

            for (int index = 0; index < segmentIndex; index++)
            {
                distance += GetAssembledSegmentLength(segments[index]);
            }

            return distance;
        }

        private float GetAssembledSegmentLength(AssembledBezierOrbitSegment segment)
        {
            float distance = 0f;
            Vector3 previousPosition = segment.EvaluatePosition(0f);

            for (int sampleIndex = 1; sampleIndex <= 32; sampleIndex++)
            {
                Vector3 currentPosition = segment.EvaluatePosition((float)sampleIndex / 32f);
                distance += Vector3.Distance(previousPosition, currentPosition);
                previousPosition = currentPosition;
            }

            return distance;
        }

        [Test]
        public void OrbitMovementUsesPhysicalSpeedAndStopsImmediatelyOnRelease()
        {
            CameraViewPreset orbitViewPreset = ScriptableObject.CreateInstance<CameraViewPreset>();
            orbitViewPreset.Configure(CameraViewType.ExteriorPresentation);
            orbitViewPreset.ConfigureOrbitTravel(10f, 0.2f);
            VehicleProfile vehicleProfile = ScriptableObject.CreateInstance<VehicleProfile>();
            VehicleOrbit vehicleOrbit = CreateRectangleOrbit(20f, 10f, Quaternion.identity);
            vehicleOrbit.ConfigureWatchMarkers(CreateWatchMarkers(new Vector3(10f, 2f, 5f), new Vector3(10f, 4f, 5f)));
            vehicleProfile.ConfigureOrbit(vehicleOrbit);
            vehicleProfile.ConfigureOrbitView(orbitViewPreset);
            GameObject vehicleBodyObject = new GameObject("Vehicle Body");
            GameObject cameraObject = new GameObject("Assigned Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            GameObject controllerObject = new GameObject("Camera System Controller");
            CameraSystemController controller = controllerObject.AddComponent<CameraSystemController>();
            controller.AssignCamera(camera);
            controller.AssignVehicle(vehicleBodyObject.transform, vehicleProfile);
            controller.SelectViewPreset(orbitViewPreset);

            CameraSystemActivationResult activationResult = controller.Activate();
            controller.SetHorizontalOrbitIntent(1f);
            controller.UpdateCameraVisuals(0.1f);

            float distanceAfterStart = controller.OrbitDistance;
            float speedAfterStart = controller.CurrentOrbitTravelSpeed;
            controller.SetHorizontalOrbitIntent(0f);
            controller.UpdateCameraVisuals(0.1f);

            Assert.AreEqual(CameraSystemActivationResult.Succeeded, activationResult);
            Assert.AreEqual(0.5f, distanceAfterStart, 0.0001f);
            Assert.AreEqual(5f, speedAfterStart, 0.0001f);
            Assert.AreEqual(distanceAfterStart, controller.OrbitDistance, 0.0001f);
            Assert.AreEqual(0f, controller.CurrentOrbitTravelSpeed);

            Object.DestroyImmediate(cameraObject);
            Object.DestroyImmediate(controllerObject);
            Object.DestroyImmediate(vehicleBodyObject);
            Object.DestroyImmediate(orbitViewPreset);
            Object.DestroyImmediate(vehicleProfile);
        }

        private VehicleOrbit CreateRectangleOrbit(float width, float depth, Quaternion orientationAdjustment)
        {
            Vector3 firstAnchor = Vector3.zero;
            Vector3 secondAnchor = new Vector3(width, 0f, 0f);
            Vector3 thirdAnchor = new Vector3(width, 0f, depth);
            Vector3 fourthAnchor = new Vector3(0f, 0f, depth);
            List<BezierOrbitKnot> knots = new List<BezierOrbitKnot>();

            knots.Add(CreateKnot(firstAnchor, fourthAnchor, secondAnchor));
            knots.Add(CreateKnot(secondAnchor, firstAnchor, thirdAnchor));
            knots.Add(CreateKnot(thirdAnchor, secondAnchor, fourthAnchor));
            knots.Add(CreateKnot(fourthAnchor, thirdAnchor, firstAnchor));

            VehicleOrbit vehicleOrbit = new VehicleOrbit();
            vehicleOrbit.Configure(knots, orientationAdjustment);
            return vehicleOrbit;
        }

        private VehicleProfile CreateConnectorProfile(float width, float depth, Vector3 frontAnchorPosition, Vector3 rearAnchorPosition)
        {
            VehicleOrbit vehicleOrbit = CreateRectangleOrbit(width, depth, Quaternion.identity);
            vehicleOrbit.ConfigureRemovableSections(CreateRemovableSection(0f, 0.2f), CreateRemovableSection(0.4f, 0.6f));
            VehicleConnectorAnchors connectorAnchors = new VehicleConnectorAnchors();
            connectorAnchors.Configure(frontAnchorPosition, rearAnchorPosition);
            VehicleProfile vehicleProfile = ScriptableObject.CreateInstance<VehicleProfile>();
            vehicleProfile.ConfigureOrbit(vehicleOrbit);
            vehicleProfile.ConfigureConnectorAnchors(connectorAnchors);
            return vehicleProfile;
        }

        private VehicleProfile CreateCurvedConnectorProfile(float width, float depth, Vector3 frontAnchorPosition, Vector3 rearAnchorPosition)
        {
            Vector3 firstAnchor = Vector3.zero;
            Vector3 secondAnchor = new Vector3(width, 0f, 0f);
            Vector3 thirdAnchor = new Vector3(width, 0f, depth);
            Vector3 fourthAnchor = new Vector3(0f, 0f, depth);
            List<BezierOrbitKnot> knots = new List<BezierOrbitKnot>();

            knots.Add(CreateKnot(firstAnchor, fourthAnchor, secondAnchor + new Vector3(0f, 0f, -1f)));
            knots.Add(CreateKnot(secondAnchor, firstAnchor + new Vector3(0f, 0f, -1f), thirdAnchor));
            knots.Add(CreateKnot(thirdAnchor, secondAnchor, fourthAnchor));
            knots.Add(CreateKnot(fourthAnchor, thirdAnchor, firstAnchor));

            VehicleOrbit vehicleOrbit = new VehicleOrbit();
            vehicleOrbit.Configure(knots, Quaternion.identity);
            VehicleConnectorAnchors connectorAnchors = new VehicleConnectorAnchors();
            connectorAnchors.Configure(frontAnchorPosition, rearAnchorPosition);
            VehicleProfile vehicleProfile = ScriptableObject.CreateInstance<VehicleProfile>();
            vehicleProfile.ConfigureOrbit(vehicleOrbit);
            vehicleProfile.ConfigureConnectorAnchors(connectorAnchors);
            return vehicleProfile;
        }

        private OrbitRemovableSection CreateRemovableSection(float normalizedStartPosition, float normalizedEndPosition)
        {
            OrbitRemovableSection section = new OrbitRemovableSection();
            section.Configure(normalizedStartPosition, normalizedEndPosition);
            return section;
        }

        private List<OrbitWatchMarker> CreateWatchMarkers(Vector3 firstWatchPoint, Vector3 secondWatchPoint)
        {
            List<OrbitWatchMarker> watchMarkers = new List<OrbitWatchMarker>();
            watchMarkers.Add(CreateWatchMarker(0f, firstWatchPoint));
            watchMarkers.Add(CreateWatchMarker(0.5f, secondWatchPoint));
            return watchMarkers;
        }

        private OrbitConnectorPairOverride CreateConnectorOverride(VehicleProfile frontProfile, VehicleProfile rearProfile, GeneratedOrbitConnectorPair generatedConnectorPair, Vector3 leftControlPointOffset, Vector3 rightControlPointOffset)
        {
            OrbitConnectorGeometry leftConnector = CreateConnectorGeometry(generatedConnectorPair.LeftConnector, leftControlPointOffset);
            OrbitConnectorGeometry rightConnector = CreateConnectorGeometry(generatedConnectorPair.RightConnector, rightControlPointOffset);
            OrbitConnectorPairOverride connectorOverride = ScriptableObject.CreateInstance<OrbitConnectorPairOverride>();
            connectorOverride.Configure(frontProfile, rearProfile, OrbitAttachmentEnd.Rear, OrbitAttachmentEnd.Front, leftConnector, rightConnector);
            return connectorOverride;
        }

        private OrbitConnectorGeometry CreateConnectorGeometry(GeneratedOrbitConnector generatedConnector, Vector3 controlPointOffset)
        {
            OrbitConnectorGeometry connectorGeometry = new OrbitConnectorGeometry();
            connectorGeometry.Configure(
                generatedConnector.StartPosition,
                generatedConnector.StartControlPoint + controlPointOffset,
                generatedConnector.EndControlPoint + controlPointOffset,
                generatedConnector.EndPosition);
            return connectorGeometry;
        }

        private OrbitWatchMarker CreateWatchMarker(float normalizedOrbitPosition, Vector3 watchPoint)
        {
            OrbitWatchMarker marker = new OrbitWatchMarker();
            marker.Configure(normalizedOrbitPosition, watchPoint);
            return marker;
        }

        private VehicleOrbit CreateBowTieOrbit()
        {
            Vector3 firstAnchor = Vector3.zero;
            Vector3 secondAnchor = new Vector3(10f, 0f, 10f);
            Vector3 thirdAnchor = new Vector3(0f, 0f, 10f);
            Vector3 fourthAnchor = new Vector3(10f, 0f, 0f);
            List<BezierOrbitKnot> knots = new List<BezierOrbitKnot>();

            knots.Add(CreateKnot(firstAnchor, fourthAnchor, secondAnchor));
            knots.Add(CreateKnot(secondAnchor, firstAnchor, thirdAnchor));
            knots.Add(CreateKnot(thirdAnchor, secondAnchor, fourthAnchor));
            knots.Add(CreateKnot(fourthAnchor, thirdAnchor, firstAnchor));

            VehicleOrbit vehicleOrbit = new VehicleOrbit();
            vehicleOrbit.Configure(knots, Quaternion.identity);
            return vehicleOrbit;
        }

        private BezierOrbitKnot CreateKnot(Vector3 anchor, Vector3 previousAnchor, Vector3 nextAnchor)
        {
            Vector3 incomingControlPoint = anchor + (previousAnchor - anchor) / 3f;
            Vector3 outgoingControlPoint = anchor + (nextAnchor - anchor) / 3f;
            BezierOrbitKnot knot = new BezierOrbitKnot();
            knot.Configure(anchor, incomingControlPoint, outgoingControlPoint);
            return knot;
        }
    }
}
