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
            AssertPosition(expectedInterpolatedWatchPoint, firstInterpolatedWatchPoint);
            AssertPosition(expectedInterpolatedWatchPoint, secondInterpolatedWatchPoint);
        }

        [Test]
        public void InvalidWatchMarkerPositionBlocksPresentationActivation()
        {
            CameraViewPreset orbitViewPreset = ScriptableObject.CreateInstance<CameraViewPreset>();
            orbitViewPreset.Configure(CameraViewType.Presentation);
            VehicleProfile vehicleProfile = ScriptableObject.CreateInstance<VehicleProfile>();
            VehicleOrbit vehicleOrbit = vehicleProfile.AddOrbit("Main");
            vehicleOrbit.Configure(CreateRectangleKnots(20f, 10f), Quaternion.identity);
            AddWatchMarkers(vehicleProfile, vehicleOrbit, new Vector3(10f, 2f, 5f), new Vector3(10f, 4f, 5f));
            vehicleOrbit.WatchMarkers[1].Configure(1f, new Vector3(10f, 4f, 5f));
            vehicleProfile.AddView("Presentation", orbitViewPreset, vehicleOrbit.Id);
            GameObject vehicleBodyObject = new GameObject("Vehicle Body");
            GameObject cameraObject = new GameObject("Assigned Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            GameObject controllerObject = new GameObject("Camera System Controller");
            CameraSystemController controller = CreateController(controllerObject, camera, vehicleBodyObject.transform, vehicleProfile);

            CameraCommandResult activationResult = controller.Activate();

            Assert.AreEqual(CameraCommandResult.ViewInvalid, activationResult);
            Assert.IsFalse(controller.IsActive);

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
            orbitViewPreset.Configure(CameraViewType.Presentation);
            orbitViewPreset.ConfigureOffsetTravel(3f, 4f);
            VehicleProfile vehicleProfile = ScriptableObject.CreateInstance<VehicleProfile>();
            VehicleOrbit vehicleOrbit = vehicleProfile.AddOrbit("Main");
            vehicleOrbit.Configure(CreateRectangleKnots(20f, 10f), Quaternion.identity);
            AddWatchMarkers(vehicleProfile, vehicleOrbit, new Vector3(10f, 2f, 5f), new Vector3(10f, 4f, 5f));
            vehicleOrbit.ConfigureOffsetRanges(-1f, 1f, -2f, 2f);
            vehicleProfile.AddView("Presentation", orbitViewPreset, vehicleOrbit.Id);
            ClosedBezierOrbit orbit = new ClosedBezierOrbit(vehicleOrbit);
            VehicleOrbit invalidVehicleOrbit = CreateRectangleOrbit(20f, 10f, Quaternion.identity);
            invalidVehicleOrbit.ConfigureOffsetRanges(1f, -1f, -2f, 2f);
            ClosedBezierOrbit invalidOrbit = new ClosedBezierOrbit(invalidVehicleOrbit);
            VehicleOrbit invalidZoomVehicleOrbit = CreateRectangleOrbit(20f, 10f, Quaternion.identity);
            invalidZoomVehicleOrbit.ConfigureOffsetRanges(-1f, 1f, 2f, -2f);
            ClosedBezierOrbit invalidZoomOrbit = new ClosedBezierOrbit(invalidZoomVehicleOrbit);
            GameObject vehicleBodyObject = new GameObject("Vehicle Body");
            GameObject cameraObject = new GameObject("Assigned Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            GameObject controllerObject = new GameObject("Camera System Controller");
            CameraSystemController controller = CreateController(controllerObject, camera, vehicleBodyObject.transform, vehicleProfile);

            CameraCommandResult activationResult = controller.Activate();
            controller.SetHeightIntent(2f);
            controller.SetZoomIntent(2f);
            controller.UpdateCameraFrame(1f);
            float upperHeightOffset = controller.HeightOffset;
            float inwardZoomOffset = controller.ZoomOffset;
            controller.SetHeightIntent(-2f);
            controller.SetZoomIntent(-2f);
            controller.UpdateCameraFrame(1f);

            Assert.AreEqual(OrbitOffsetRangeValidationResult.Valid, orbit.OffsetRangeValidationResult);
            Assert.AreEqual(Vector3.forward, orbit.EvaluateBodyLocalInwardNormal(10f));
            Assert.AreEqual(OrbitOffsetRangeValidationResult.InvalidHeightRange, invalidOrbit.OffsetRangeValidationResult);
            Assert.AreEqual(OrbitOffsetRangeValidationResult.InvalidZoomRange, invalidZoomOrbit.OffsetRangeValidationResult);
            Assert.AreEqual(CameraCommandResult.Accepted, activationResult);
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
        public void AttachmentRemapPreservesRetainedFrontPositionsAndMapsRemovedPositionsToTheRear()
        {
            VehicleProfile frontProfile = CreateConnectorProfile(20f, 10f, new Vector3(10f, 0f, 0f), new Vector3(10f, 0f, 10f));
            VehicleProfile rearProfile = CreateConnectorProfile(40f, 20f, new Vector3(20f, 0f, 0f), new Vector3(20f, 0f, 20f));
            frontProfile.PrimaryOrbit.ConfigureRemovableSections(CreateRemovableSection(0f, 0.2f), CreateRemovableSection(0.5f, 5f / 6f));
            rearProfile.PrimaryOrbit.ConfigureRemovableSections(CreateRemovableSection(0f, 1f / 3f), CreateRemovableSection(0.4f, 0.6f));
            GameObject frontBody = new GameObject("Front Body");
            GameObject rearBody = new GameObject("Rear Body");
            ClosedBezierOrbit frontOrbit = new ClosedBezierOrbit(frontProfile.PrimaryOrbit);
            ClosedBezierOrbit rearOrbit = new ClosedBezierOrbit(rearProfile.PrimaryOrbit);
            VehicleProfile[] profiles = { frontProfile, rearProfile };
            ChainLayout layout = new ChainLayout(profiles, 0, frontProfile.PrimaryOrbit);
            ChainOrbit oldOrbit = new ChainOrbit(new ChainLayout(new VehicleProfile[] { frontProfile }, 0, frontProfile.PrimaryOrbit), new Transform[] { frontBody.transform }, 0);
            ChainOrbit combinedOrbit = new ChainOrbit(layout, new Transform[] { frontBody.transform, rearBody.transform }, 0);
            OrbitRemapper remapper = new OrbitRemapper();

            float retainedDistance;
            OrbitRemapResult retainedResult = remapper.Remap(oldOrbit, 10f, combinedOrbit, out retainedDistance);
            Vector3 retainedPosition = combinedOrbit.EvaluateLeadBodyLocalPosition(retainedDistance);
            float removedDistance;
            OrbitRemapResult removedResult = remapper.Remap(oldOrbit, 40f, combinedOrbit, out removedDistance);
            Vector3 removedPosition = combinedOrbit.EvaluateLeadBodyLocalPosition(removedDistance);
            Vector3 expectedRearPosition = rearOrbit.EvaluateBodyLocalPosition(rearOrbit.Length * 0.5f) + layout.Offsets[1];

            Assert.AreEqual(OrbitRemapResult.Survived, retainedResult);
            AssertPosition(frontOrbit.EvaluateBodyLocalPosition(10f), retainedPosition);
            Assert.AreEqual(OrbitRemapResult.MovedToRearFallback, removedResult);
            Assert.AreEqual(70f, removedDistance, 0.0001f);
            AssertPosition(expectedRearPosition, removedPosition);

            Object.DestroyImmediate(frontBody);
            Object.DestroyImmediate(rearBody);
            Object.DestroyImmediate(frontProfile);
            Object.DestroyImmediate(rearProfile);
        }

        [Test]
        public void AttachmentRemapPreservesCurvedRetainedFrontPositions()
        {
            VehicleProfile frontProfile = CreateCurvedConnectorProfile(20f, 10f, new Vector3(10f, 0f, 0f), new Vector3(10f, 0f, 10f));
            VehicleProfile rearProfile = CreateConnectorProfile(40f, 20f, new Vector3(20f, 0f, 0f), new Vector3(20f, 0f, 20f));
            frontProfile.PrimaryOrbit.ConfigureRemovableSections(CreateRemovableSection(0f, 0.2f), CreateRemovableSection(0.5f, 0.84f));
            rearProfile.PrimaryOrbit.ConfigureRemovableSections(CreateRemovableSection(0f, 1f / 3f), CreateRemovableSection(0.4f, 0.6f));
            ClosedBezierOrbit frontOrbit = new ClosedBezierOrbit(frontProfile.PrimaryOrbit);
            GameObject frontBody = new GameObject("Front Body");
            GameObject rearBody = new GameObject("Rear Body");
            ChainOrbit oldOrbit = new ChainOrbit(new ChainLayout(new VehicleProfile[] { frontProfile }, 0, frontProfile.PrimaryOrbit), new Transform[] { frontBody.transform }, 0);
            ChainOrbit combinedOrbit = new ChainOrbit(new ChainLayout(new VehicleProfile[] { frontProfile, rearProfile }, 0, frontProfile.PrimaryOrbit), new Transform[] { frontBody.transform, rearBody.transform }, 0);
            OrbitRemapper remapper = new OrbitRemapper();
            float frontDistance = frontOrbit.Length * 0.15f;

            float remappedDistance;
            OrbitRemapResult remapResult = remapper.Remap(oldOrbit, frontDistance, combinedOrbit, out remappedDistance);
            Vector3 expectedPosition = frontOrbit.EvaluateBodyLocalPosition(frontDistance);
            Vector3 remappedPosition = combinedOrbit.EvaluateLeadBodyLocalPosition(remappedDistance);

            Assert.AreEqual(ChainOrbitAssemblyResult.Valid, combinedOrbit.AssemblyResult);
            Assert.AreEqual(OrbitRemapResult.Survived, remapResult);
            AssertPosition(expectedPosition, remappedPosition);

            Object.DestroyImmediate(frontBody);
            Object.DestroyImmediate(rearBody);
            Object.DestroyImmediate(frontProfile);
            Object.DestroyImmediate(rearProfile);
        }

        [Test]
        public void OrbitMovementUsesPhysicalSpeedAndStopsImmediatelyOnRelease()
        {
            CameraViewPreset orbitViewPreset = ScriptableObject.CreateInstance<CameraViewPreset>();
            orbitViewPreset.Configure(CameraViewType.Presentation);
            orbitViewPreset.ConfigureOrbitTravel(10f, 0.2f);
            VehicleProfile vehicleProfile = ScriptableObject.CreateInstance<VehicleProfile>();
            VehicleOrbit vehicleOrbit = vehicleProfile.AddOrbit("Main");
            vehicleOrbit.Configure(CreateRectangleKnots(20f, 10f), Quaternion.identity);
            AddWatchMarkers(vehicleProfile, vehicleOrbit, new Vector3(10f, 2f, 5f), new Vector3(10f, 4f, 5f));
            vehicleProfile.AddView("Presentation", orbitViewPreset, vehicleOrbit.Id);
            GameObject vehicleBodyObject = new GameObject("Vehicle Body");
            GameObject cameraObject = new GameObject("Assigned Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            GameObject controllerObject = new GameObject("Camera System Controller");
            CameraSystemController controller = CreateController(controllerObject, camera, vehicleBodyObject.transform, vehicleProfile);

            CameraCommandResult activationResult = controller.Activate();
            float startDistance = controller.OrbitDistance;
            controller.SetHorizontalOrbitIntent(1f);
            controller.UpdateCameraFrame(0.1f);

            float distanceAfterStart = controller.OrbitDistance - startDistance;
            float speedAfterStart = controller.CurrentOrbitTravelSpeed;
            controller.SetHorizontalOrbitIntent(0f);
            controller.UpdateCameraFrame(0.1f);

            Assert.AreEqual(CameraCommandResult.Accepted, activationResult);
            float expectedSpeed = 10f * (1f - Mathf.Pow(0.5f, 0.1f / 0.2f));
            Assert.AreEqual(expectedSpeed * 0.1f, distanceAfterStart, 0.0001f);
            Assert.AreEqual(expectedSpeed, speedAfterStart, 0.0001f);
            Assert.AreEqual(distanceAfterStart, controller.OrbitDistance - startDistance, 0.0001f);
            Assert.AreEqual(0f, controller.CurrentOrbitTravelSpeed);

            Object.DestroyImmediate(cameraObject);
            Object.DestroyImmediate(controllerObject);
            Object.DestroyImmediate(vehicleBodyObject);
            Object.DestroyImmediate(orbitViewPreset);
            Object.DestroyImmediate(vehicleProfile);
        }

        private CameraSystemController CreateController(GameObject controllerObject, Camera camera, Transform body, VehicleProfile profile)
        {
            VehicleCameraTarget target = body.gameObject.AddComponent<VehicleCameraTarget>();
            target.Configure(body, profile);
            CameraSystemController controller = controllerObject.AddComponent<CameraSystemController>();
            controller.SetUpdateMode(CameraUpdateMode.Manual);
            controller.Configure(camera, target, "Presentation");
            return controller;
        }

        private VehicleOrbit CreateRectangleOrbit(float width, float depth, Quaternion orientationAdjustment)
        {
            VehicleOrbit vehicleOrbit = new VehicleOrbit();
            vehicleOrbit.Configure(CreateRectangleKnots(width, depth), orientationAdjustment);
            return vehicleOrbit;
        }

        private List<BezierOrbitKnot> CreateRectangleKnots(float width, float depth)
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
            return knots;
        }

        private VehicleProfile CreateConnectorProfile(float width, float depth, Vector3 frontAnchorPosition, Vector3 rearAnchorPosition)
        {
            VehicleProfile vehicleProfile = ScriptableObject.CreateInstance<VehicleProfile>();
            VehicleOrbit vehicleOrbit = vehicleProfile.AddOrbit("Main");
            vehicleOrbit.Configure(CreateRectangleKnots(width, depth), Quaternion.identity);
            vehicleOrbit.ConfigureRemovableSections(CreateRemovableSection(0f, 0.2f), CreateRemovableSection(0.4f, 0.6f));
            VehicleConnectorAnchors connectorAnchors = new VehicleConnectorAnchors();
            connectorAnchors.Configure(frontAnchorPosition, rearAnchorPosition);
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

            VehicleProfile vehicleProfile = ScriptableObject.CreateInstance<VehicleProfile>();
            VehicleOrbit vehicleOrbit = vehicleProfile.AddOrbit("Main");
            vehicleOrbit.Configure(knots, Quaternion.identity);
            VehicleConnectorAnchors connectorAnchors = new VehicleConnectorAnchors();
            connectorAnchors.Configure(frontAnchorPosition, rearAnchorPosition);
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

        private void AddWatchMarkers(VehicleProfile vehicleProfile, VehicleOrbit vehicleOrbit, Vector3 firstWatchPoint, Vector3 secondWatchPoint)
        {
            vehicleProfile.AddWatchMarker(vehicleOrbit, "First", 0f, firstWatchPoint, false);
            vehicleProfile.AddWatchMarker(vehicleOrbit, "Second", 0.5f, secondWatchPoint, false);
        }

        private void AssertPosition(Vector3 expectedPosition, Vector3 actualPosition)
        {
            Assert.Less(Vector3.Distance(expectedPosition, actualPosition), 0.0001f);
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
