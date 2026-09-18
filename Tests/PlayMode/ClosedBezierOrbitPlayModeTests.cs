using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Gley.CameraSystem.Tests.PlayMode
{
    public class ClosedBezierOrbitPlayModeTests
    {
        [UnityTest]
        public IEnumerator ClosedBezierOrbitFollowsVehiclePitchAndRoll()
        {
            GameObject vehicleBodyObject = new GameObject("Vehicle Body");
            vehicleBodyObject.transform.SetPositionAndRotation(new Vector3(4f, 2f, -3f), Quaternion.Euler(20f, 35f, 15f));
            Quaternion orientationAdjustment = Quaternion.Euler(0f, 90f, 0f);
            VehicleOrbit vehicleOrbit = CreateTriangleOrbit(orientationAdjustment);
            ClosedBezierOrbit orbit = new ClosedBezierOrbit(vehicleOrbit);
            Vector3 expectedWorldPosition = vehicleBodyObject.transform.TransformPoint(orientationAdjustment * vehicleOrbit.Knots[0].Anchor);

            yield return null;

            Vector3 orbitWorldPosition = orbit.EvaluateWorldPosition(vehicleBodyObject.transform, 0f);

            Assert.AreEqual(OrbitValidationResult.Valid, orbit.ValidationResult);
            Assert.AreEqual(expectedWorldPosition, orbitWorldPosition);

            Object.Destroy(vehicleBodyObject);
        }

        [UnityTest]
        public IEnumerator OrbitMovementFollowsWatchMarkersAndKeepsInstancesIndependent()
        {
            CameraViewPreset orbitViewPreset = ScriptableObject.CreateInstance<CameraViewPreset>();
            orbitViewPreset.Configure(CameraViewType.ExteriorPresentation);
            orbitViewPreset.ConfigureOrbitTravel(10f, 0f);
            VehicleProfile firstVehicleProfile = CreateVehicleProfile(orbitViewPreset, 20f, 10f);
            VehicleProfile secondVehicleProfile = CreateVehicleProfile(orbitViewPreset, 40f, 20f);
            GameObject firstVehicleBodyObject = new GameObject("First Vehicle Body");
            GameObject secondVehicleBodyObject = new GameObject("Second Vehicle Body");
            firstVehicleBodyObject.transform.SetPositionAndRotation(new Vector3(3f, 0f, 0f), Quaternion.Euler(10f, 20f, 5f));
            secondVehicleBodyObject.transform.SetPositionAndRotation(new Vector3(-6f, 0f, 4f), Quaternion.Euler(-5f, 30f, -10f));
            GameObject firstCameraObject = new GameObject("First Camera");
            Camera firstCamera = firstCameraObject.AddComponent<Camera>();
            GameObject secondCameraObject = new GameObject("Second Camera");
            Camera secondCamera = secondCameraObject.AddComponent<Camera>();
            GameObject firstControllerObject = new GameObject("First Camera System Controller");
            CameraSystemController firstController = firstControllerObject.AddComponent<CameraSystemController>();
            GameObject secondControllerObject = new GameObject("Second Camera System Controller");
            CameraSystemController secondController = secondControllerObject.AddComponent<CameraSystemController>();
            firstController.enabled = false;
            secondController.enabled = false;
            firstController.AssignCamera(firstCamera);
            firstController.AssignVehicle(firstVehicleBodyObject.transform, firstVehicleProfile);
            firstController.SelectViewPreset(orbitViewPreset);
            secondController.AssignCamera(secondCamera);
            secondController.AssignVehicle(secondVehicleBodyObject.transform, secondVehicleProfile);
            secondController.SelectViewPreset(orbitViewPreset);

            CameraSystemActivationResult firstActivationResult = firstController.Activate();
            CameraSystemActivationResult secondActivationResult = secondController.Activate();
            firstController.SetHorizontalOrbitIntent(1f);
            secondController.SetHorizontalOrbitIntent(-1f);
            firstController.UpdateCameraVisuals(1f);
            secondController.UpdateCameraVisuals(1f);
            yield return null;

            ClosedBezierOrbit firstOrbit = new ClosedBezierOrbit(firstVehicleProfile.VehicleOrbit);
            ClosedBezierOrbit secondOrbit = new ClosedBezierOrbit(secondVehicleProfile.VehicleOrbit);
            Vector3 expectedFirstPosition = firstOrbit.EvaluateWorldPosition(firstVehicleBodyObject.transform, 10f);
            Vector3 expectedSecondPosition = secondOrbit.EvaluateWorldPosition(secondVehicleBodyObject.transform, -10f);
            Vector3 expectedFirstWatchPoint = firstVehicleBodyObject.transform.TransformPoint(firstOrbit.EvaluateBodyLocalWatchPoint(10f));
            Vector3 expectedSecondWatchPoint = secondVehicleBodyObject.transform.TransformPoint(secondOrbit.EvaluateBodyLocalWatchPoint(-10f));
            Quaternion expectedFirstRotation = Quaternion.LookRotation(expectedFirstWatchPoint - expectedFirstPosition, firstVehicleBodyObject.transform.up);
            Quaternion expectedSecondRotation = Quaternion.LookRotation(expectedSecondWatchPoint - expectedSecondPosition, secondVehicleBodyObject.transform.up);

            Assert.AreEqual(CameraSystemActivationResult.Succeeded, firstActivationResult);
            Assert.AreEqual(CameraSystemActivationResult.Succeeded, secondActivationResult);
            Assert.AreEqual(expectedFirstPosition, firstCamera.transform.position);
            Assert.AreEqual(expectedSecondPosition, secondCamera.transform.position);
            Assert.Less(Quaternion.Angle(expectedFirstRotation, firstCamera.transform.rotation), 0.01f);
            Assert.Less(Quaternion.Angle(expectedSecondRotation, secondCamera.transform.rotation), 0.01f);
            Assert.AreEqual(10f, firstController.OrbitDistance, 0.0001f);
            Assert.AreEqual(-10f, secondController.OrbitDistance, 0.0001f);

            Object.Destroy(firstCameraObject);
            Object.Destroy(secondCameraObject);
            Object.Destroy(firstControllerObject);
            Object.Destroy(secondControllerObject);
            Object.Destroy(firstVehicleBodyObject);
            Object.Destroy(secondVehicleBodyObject);
            Object.Destroy(firstVehicleProfile);
            Object.Destroy(secondVehicleProfile);
            Object.Destroy(orbitViewPreset);
        }

        [UnityTest]
        public IEnumerator HeightAndZoomFollowVehicleFrameWithoutChangingFieldOfView()
        {
            CameraViewPreset orbitViewPreset = ScriptableObject.CreateInstance<CameraViewPreset>();
            orbitViewPreset.Configure(CameraViewType.ExteriorPresentation);
            orbitViewPreset.ConfigureOrbitTravel(10f, 0f);
            orbitViewPreset.ConfigureOffsetTravel(1f, 1f);
            VehicleProfile vehicleProfile = CreateVehicleProfile(orbitViewPreset, 20f, 10f);
            vehicleProfile.VehicleOrbit.ConfigureOffsetRanges(-1f, 1f, -1f, 1f);
            GameObject vehicleBodyObject = new GameObject("Vehicle Body");
            vehicleBodyObject.transform.SetPositionAndRotation(new Vector3(3f, 4f, 5f), Quaternion.Euler(20f, 30f, 15f));
            GameObject cameraObject = new GameObject("Assigned Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 47f;
            GameObject controllerObject = new GameObject("Camera System Controller");
            CameraSystemController controller = controllerObject.AddComponent<CameraSystemController>();
            controller.enabled = false;
            controller.AssignCamera(camera);
            controller.AssignVehicle(vehicleBodyObject.transform, vehicleProfile);
            controller.SelectViewPreset(orbitViewPreset);

            CameraSystemActivationResult activationResult = controller.Activate();
            controller.SetHorizontalOrbitIntent(1f);
            controller.SetHeightIntent(1f);
            controller.SetZoomIntent(1f);
            controller.UpdateCameraVisuals(1f);
            yield return null;

            ClosedBezierOrbit orbit = new ClosedBezierOrbit(vehicleProfile.VehicleOrbit);
            Vector3 baseCameraPosition = orbit.EvaluateWorldPosition(vehicleBodyObject.transform, 10f);
            Vector3 inwardNormal = orbit.EvaluateBodyLocalInwardNormal(10f);
            Vector3 expectedCameraPosition = baseCameraPosition + vehicleBodyObject.transform.up + vehicleBodyObject.transform.TransformDirection(inwardNormal);
            Vector3 watchPoint = vehicleBodyObject.transform.TransformPoint(orbit.EvaluateBodyLocalWatchPoint(10f));
            Quaternion expectedRotation = Quaternion.LookRotation(watchPoint - expectedCameraPosition, vehicleBodyObject.transform.up);

            Assert.AreEqual(CameraSystemActivationResult.Succeeded, activationResult);
            Assert.AreEqual(1f, controller.HeightOffset, 0.0001f);
            Assert.AreEqual(1f, controller.ZoomOffset, 0.0001f);
            Assert.AreEqual(expectedCameraPosition, camera.transform.position);
            Assert.Less(Quaternion.Angle(expectedRotation, camera.transform.rotation), 0.01f);
            Assert.AreEqual(47f, camera.fieldOfView, 0.0001f);

            Object.Destroy(cameraObject);
            Object.Destroy(controllerObject);
            Object.Destroy(vehicleBodyObject);
            Object.Destroy(vehicleProfile);
            Object.Destroy(orbitViewPreset);
        }

        [UnityTest]
        public IEnumerator RemovableSectionsRemainIndependentWhenBodiesMove()
        {
            VehicleOrbit firstVehicleOrbit = CreateRectangleOrbit(20f, 10f);
            firstVehicleOrbit.ConfigureRemovableSections(CreateRemovableSection(0.1f, 0.2f), CreateRemovableSection(0.6f, 0.7f));
            VehicleOrbit secondVehicleOrbit = CreateRectangleOrbit(40f, 20f);
            secondVehicleOrbit.ConfigureRemovableSections(CreateRemovableSection(0.2f, 0.3f), CreateRemovableSection(0.7f, 0.8f));
            ClosedBezierOrbit firstOrbit = new ClosedBezierOrbit(firstVehicleOrbit);
            ClosedBezierOrbit secondOrbit = new ClosedBezierOrbit(secondVehicleOrbit);
            GameObject firstVehicleBodyObject = new GameObject("First Vehicle Body");
            GameObject secondVehicleBodyObject = new GameObject("Second Vehicle Body");
            firstVehicleBodyObject.transform.SetPositionAndRotation(new Vector3(3f, 2f, 0f), Quaternion.Euler(0f, 90f, 0f));
            secondVehicleBodyObject.transform.SetPositionAndRotation(new Vector3(-4f, 1f, 5f), Quaternion.Euler(0f, -90f, 0f));

            yield return null;

            Vector3 firstBoundary = firstVehicleBodyObject.transform.TransformPoint(firstOrbit.EvaluateBodyLocalRemovableSectionStart(OrbitAttachmentEnd.Front));
            Vector3 secondBoundary = secondVehicleBodyObject.transform.TransformPoint(secondOrbit.EvaluateBodyLocalRemovableSectionStart(OrbitAttachmentEnd.Rear));

            Assert.AreEqual(OrbitRemovableSectionValidationResult.Valid, firstOrbit.RemovableSectionValidationResult);
            Assert.AreEqual(OrbitRemovableSectionValidationResult.Valid, secondOrbit.RemovableSectionValidationResult);
            Assert.Less(Vector3.Distance(new Vector3(3f, 2f, -6f), firstBoundary), 0.001f);
            Assert.Less(Vector3.Distance(new Vector3(-24f, 1f, 21f), secondBoundary), 0.001f);

            Object.Destroy(firstVehicleBodyObject);
            Object.Destroy(secondVehicleBodyObject);
        }

        [UnityTest]
        public IEnumerator GeneratedConnectorsUseStraightReferenceGeometryDuringArticulation()
        {
            VehicleProfile frontProfile = CreateConnectorProfile(20f, 10f, new Vector3(10f, 0f, 0f), new Vector3(10f, 0f, 10f));
            VehicleProfile rearProfile = CreateConnectorProfile(40f, 20f, new Vector3(20f, 0f, 0f), new Vector3(20f, 0f, 20f));
            TwoBodyOrbitConnectorGenerator firstGenerator = new TwoBodyOrbitConnectorGenerator(frontProfile, rearProfile);
            GameObject rearBodyObject = new GameObject("Rear Body");
            rearBodyObject.transform.SetPositionAndRotation(new Vector3(8f, 0f, 10f), Quaternion.Euler(0f, 35f, 0f));

            yield return null;

            rearBodyObject.transform.SetPositionAndRotation(new Vector3(-5f, 0f, 3f), Quaternion.Euler(0f, -55f, 0f));
            TwoBodyOrbitConnectorGenerator secondGenerator = new TwoBodyOrbitConnectorGenerator(frontProfile, rearProfile);

            Assert.AreEqual(TwoBodyOrbitConnectorGenerationResult.Generated, firstGenerator.GenerationResult);
            Assert.AreEqual(TwoBodyOrbitConnectorGenerationResult.Generated, secondGenerator.GenerationResult);
            Assert.Less(Vector3.Distance(firstGenerator.RearBodyLocalPosition, secondGenerator.RearBodyLocalPosition), 0.0001f);
            Assert.Less(Vector3.Distance(firstGenerator.ConnectorPair.LeftConnector.StartPosition, secondGenerator.ConnectorPair.LeftConnector.StartPosition), 0.0001f);
            Assert.Less(Vector3.Distance(firstGenerator.ConnectorPair.LeftConnector.EndPosition, secondGenerator.ConnectorPair.LeftConnector.EndPosition), 0.0001f);
            Assert.Less(Vector3.Distance(firstGenerator.ConnectorPair.RightConnector.StartPosition, secondGenerator.ConnectorPair.RightConnector.StartPosition), 0.0001f);
            Assert.Less(Vector3.Distance(firstGenerator.ConnectorPair.RightConnector.EndPosition, secondGenerator.ConnectorPair.RightConnector.EndPosition), 0.0001f);

            Object.Destroy(rearBodyObject);
            Object.Destroy(frontProfile);
            Object.Destroy(rearProfile);
        }

        [UnityTest]
        public IEnumerator OrbitAttachmentMergeRemainsIndependentAcrossProfileInstances()
        {
            VehicleProfile firstVehicleProfile = ScriptableObject.CreateInstance<VehicleProfile>();
            VehicleProfile secondVehicleProfile = ScriptableObject.CreateInstance<VehicleProfile>();
            VehicleOrbit firstVehicleOrbit = CreateRectangleOrbit(20f, 10f);
            VehicleOrbit secondVehicleOrbit = CreateRectangleOrbit(40f, 20f);
            secondVehicleOrbit.ConfigureAttachmentMerge(false);
            firstVehicleProfile.ConfigureOrbit(firstVehicleOrbit);
            secondVehicleProfile.ConfigureOrbit(secondVehicleOrbit);

            yield return null;

            Assert.IsTrue(firstVehicleProfile.VehicleOrbit.MergeWhenAttached);
            Assert.IsFalse(secondVehicleProfile.VehicleOrbit.MergeWhenAttached);

            Object.Destroy(firstVehicleProfile);
            Object.Destroy(secondVehicleProfile);
        }

        [UnityTest]
        public IEnumerator TwoBodyClosedBezierOrbitFollowsTheLeadBodyWithoutUsingRearArticulation()
        {
            VehicleProfile frontProfile = CreateConnectorProfile(20f, 10f, new Vector3(10f, 0f, 0f), new Vector3(10f, 0f, 10f));
            VehicleProfile rearProfile = CreateConnectorProfile(40f, 20f, new Vector3(20f, 0f, 0f), new Vector3(20f, 0f, 20f));
            frontProfile.VehicleOrbit.ConfigureRemovableSections(CreateRemovableSection(0f, 0.2f), CreateRemovableSection(0.5f, 5f / 6f));
            rearProfile.VehicleOrbit.ConfigureRemovableSections(CreateRemovableSection(0f, 1f / 3f), CreateRemovableSection(0.4f, 0.6f));
            TwoBodyClosedBezierOrbit orbit = new TwoBodyClosedBezierOrbit(frontProfile, rearProfile);
            GameObject leadBodyObject = new GameObject("Lead Body");
            GameObject rearBodyObject = new GameObject("Rear Body");
            leadBodyObject.transform.SetPositionAndRotation(new Vector3(3f, 4f, 5f), Quaternion.Euler(20f, 30f, 15f));
            rearBodyObject.transform.SetPositionAndRotation(new Vector3(-8f, 0f, 12f), Quaternion.Euler(0f, 35f, 0f));
            float distance = orbit.Length * 0.4f;
            Vector3 localPosition = orbit.EvaluateLeadBodyLocalPosition(distance);
            Vector3 firstWorldPosition = orbit.EvaluateWorldPosition(leadBodyObject.transform, distance);

            yield return null;

            rearBodyObject.transform.SetPositionAndRotation(new Vector3(7f, 0f, -4f), Quaternion.Euler(0f, -55f, 0f));
            Vector3 articulatedRearWorldPosition = orbit.EvaluateWorldPosition(leadBodyObject.transform, distance);
            leadBodyObject.transform.SetPositionAndRotation(new Vector3(-6f, 2f, 1f), Quaternion.Euler(-10f, 70f, 25f));
            Vector3 movedLeadWorldPosition = orbit.EvaluateWorldPosition(leadBodyObject.transform, distance);
            Vector3 expectedMovedLeadWorldPosition = leadBodyObject.transform.TransformPoint(localPosition);

            Assert.AreEqual(TwoBodyClosedBezierOrbitAssemblyResult.Valid, orbit.AssemblyResult);
            Assert.Less(Vector3.Distance(firstWorldPosition, articulatedRearWorldPosition), 0.0001f);
            Assert.Less(Vector3.Distance(expectedMovedLeadWorldPosition, movedLeadWorldPosition), 0.0001f);

            Object.Destroy(leadBodyObject);
            Object.Destroy(rearBodyObject);
            Object.Destroy(frontProfile);
            Object.Destroy(rearProfile);
        }

        [UnityTest]
        public IEnumerator TwoBodyClosedBezierOrbitNormalFollowsTheLeadBodyWithoutUsingRearArticulation()
        {
            VehicleProfile frontProfile = CreateConnectorProfile(20f, 10f, new Vector3(10f, 0f, 0f), new Vector3(10f, 0f, 10f));
            VehicleProfile rearProfile = CreateConnectorProfile(40f, 20f, new Vector3(20f, 0f, 0f), new Vector3(20f, 0f, 20f));
            frontProfile.VehicleOrbit.ConfigureRemovableSections(CreateRemovableSection(0f, 0.2f), CreateRemovableSection(0.5f, 5f / 6f));
            rearProfile.VehicleOrbit.ConfigureRemovableSections(CreateRemovableSection(0f, 1f / 3f), CreateRemovableSection(0.4f, 0.6f));
            TwoBodyClosedBezierOrbit orbit = new TwoBodyClosedBezierOrbit(frontProfile, rearProfile);
            GameObject leadBodyObject = new GameObject("Lead Body");
            GameObject rearBodyObject = new GameObject("Rear Body");
            leadBodyObject.transform.SetPositionAndRotation(new Vector3(3f, 4f, 5f), Quaternion.Euler(20f, 30f, 15f));
            rearBodyObject.transform.SetPositionAndRotation(new Vector3(-8f, 0f, 12f), Quaternion.Euler(0f, 35f, 0f));
            float distance = orbit.Length * 0.4f;
            Vector3 localNormal = orbit.EvaluateLeadBodyLocalInwardNormal(distance);
            Vector3 firstWorldNormal = leadBodyObject.transform.TransformDirection(localNormal);

            yield return null;

            rearBodyObject.transform.SetPositionAndRotation(new Vector3(7f, 0f, -4f), Quaternion.Euler(0f, -55f, 0f));
            Vector3 articulatedRearWorldNormal = leadBodyObject.transform.TransformDirection(orbit.EvaluateLeadBodyLocalInwardNormal(distance));
            leadBodyObject.transform.SetPositionAndRotation(new Vector3(-6f, 2f, 1f), Quaternion.Euler(-10f, 70f, 25f));
            Vector3 movedLeadWorldNormal = leadBodyObject.transform.TransformDirection(orbit.EvaluateLeadBodyLocalInwardNormal(distance));
            Vector3 expectedMovedLeadWorldNormal = leadBodyObject.transform.TransformDirection(localNormal);

            Assert.AreEqual(TwoBodyClosedBezierOrbitAssemblyResult.Valid, orbit.AssemblyResult);
            Assert.Greater(localNormal.sqrMagnitude, 0.99f);
            Assert.Less(Vector3.Distance(firstWorldNormal, articulatedRearWorldNormal), 0.0001f);
            Assert.Less(Vector3.Distance(expectedMovedLeadWorldNormal, movedLeadWorldNormal), 0.0001f);

            Object.Destroy(leadBodyObject);
            Object.Destroy(rearBodyObject);
            Object.Destroy(frontProfile);
            Object.Destroy(rearProfile);
        }

        [UnityTest]
        public IEnumerator TwoBodyClosedBezierOrbitWatchTrackFollowsTheLeadBodyWithoutUsingRearArticulation()
        {
            VehicleProfile frontProfile = CreateConnectorProfile(20f, 10f, new Vector3(10f, 0f, 0f), new Vector3(10f, 0f, 10f));
            VehicleProfile rearProfile = CreateConnectorProfile(40f, 20f, new Vector3(20f, 0f, 0f), new Vector3(20f, 0f, 20f));
            frontProfile.VehicleOrbit.ConfigureWatchMarkers(CreateWatchMarkers(20f, 10f));
            rearProfile.VehicleOrbit.ConfigureWatchMarkers(CreateWatchMarkers(40f, 20f));
            frontProfile.VehicleOrbit.ConfigureRemovableSections(CreateRemovableSection(0f, 0.2f), CreateRemovableSection(0.5f, 5f / 6f));
            rearProfile.VehicleOrbit.ConfigureRemovableSections(CreateRemovableSection(0f, 1f / 3f), CreateRemovableSection(0.4f, 0.6f));
            TwoBodyClosedBezierOrbit orbit = new TwoBodyClosedBezierOrbit(frontProfile, rearProfile);
            GameObject leadBodyObject = new GameObject("Lead Body");
            GameObject rearBodyObject = new GameObject("Rear Body");
            leadBodyObject.transform.SetPositionAndRotation(new Vector3(3f, 4f, 5f), Quaternion.Euler(20f, 30f, 15f));
            rearBodyObject.transform.SetPositionAndRotation(new Vector3(-8f, 0f, 12f), Quaternion.Euler(0f, 35f, 0f));
            float distance = orbit.Length * 0.5f;
            Vector3 localWatchPoint = orbit.EvaluateLeadBodyLocalWatchPoint(distance);
            Vector3 firstWorldWatchPoint = leadBodyObject.transform.TransformPoint(localWatchPoint);

            yield return null;

            rearBodyObject.transform.SetPositionAndRotation(new Vector3(7f, 0f, -4f), Quaternion.Euler(0f, -55f, 0f));
            Vector3 articulatedRearWorldWatchPoint = leadBodyObject.transform.TransformPoint(orbit.EvaluateLeadBodyLocalWatchPoint(distance));
            leadBodyObject.transform.SetPositionAndRotation(new Vector3(-6f, 2f, 1f), Quaternion.Euler(-10f, 70f, 25f));
            Vector3 movedLeadWorldWatchPoint = leadBodyObject.transform.TransformPoint(orbit.EvaluateLeadBodyLocalWatchPoint(distance));
            Vector3 expectedMovedLeadWorldWatchPoint = leadBodyObject.transform.TransformPoint(localWatchPoint);

            Assert.AreEqual(TwoBodyClosedBezierOrbitAssemblyResult.Valid, orbit.AssemblyResult);
            Assert.IsTrue(orbit.HasValidWatchMarkers);
            Assert.Less(Vector3.Distance(firstWorldWatchPoint, articulatedRearWorldWatchPoint), 0.0001f);
            Assert.Less(Vector3.Distance(expectedMovedLeadWorldWatchPoint, movedLeadWorldWatchPoint), 0.0001f);

            Object.Destroy(leadBodyObject);
            Object.Destroy(rearBodyObject);
            Object.Destroy(frontProfile);
            Object.Destroy(rearProfile);
        }

        [UnityTest]
        public IEnumerator AttachmentRemapUsesTheStraightReferenceOrbitAtRuntime()
        {
            VehicleProfile frontProfile = CreateConnectorProfile(20f, 10f, new Vector3(10f, 0f, 0f), new Vector3(10f, 0f, 10f));
            VehicleProfile rearProfile = CreateConnectorProfile(40f, 20f, new Vector3(20f, 0f, 0f), new Vector3(20f, 0f, 20f));
            frontProfile.VehicleOrbit.ConfigureRemovableSections(CreateRemovableSection(0f, 0.2f), CreateRemovableSection(0.5f, 5f / 6f));
            rearProfile.VehicleOrbit.ConfigureRemovableSections(CreateRemovableSection(0f, 1f / 3f), CreateRemovableSection(0.4f, 0.6f));
            TwoBodyOrbitAttachmentRemapResolver remapResolver = new TwoBodyOrbitAttachmentRemapResolver(frontProfile, rearProfile);
            GameObject rearBodyObject = new GameObject("Rear Body");
            rearBodyObject.transform.SetPositionAndRotation(new Vector3(-4f, 0f, 7f), Quaternion.Euler(0f, 40f, 0f));

            OrbitAttachmentRemapResult firstResult = remapResolver.ResolveFrontOrbitDistance(40f);
            float firstRemappedDistance = remapResolver.RemappedOrbitDistance;
            Vector3 firstRemappedPosition = remapResolver.CombinedOrbit.EvaluateLeadBodyLocalPosition(firstRemappedDistance);
            yield return null;

            rearBodyObject.transform.SetPositionAndRotation(new Vector3(9f, 0f, -3f), Quaternion.Euler(0f, -50f, 0f));
            OrbitAttachmentRemapResult secondResult = remapResolver.ResolveFrontOrbitDistance(40f);
            float secondRemappedDistance = remapResolver.RemappedOrbitDistance;
            Vector3 secondRemappedPosition = remapResolver.CombinedOrbit.EvaluateLeadBodyLocalPosition(secondRemappedDistance);

            Assert.AreEqual(OrbitAttachmentRemapResult.RemovedFrontPositionMappedToRear, firstResult);
            Assert.AreEqual(OrbitAttachmentRemapResult.RemovedFrontPositionMappedToRear, secondResult);
            Assert.AreEqual(firstRemappedDistance, secondRemappedDistance, 0.0001f);
            Assert.Less(Vector3.Distance(firstRemappedPosition, secondRemappedPosition), 0.0001f);

            Object.Destroy(rearBodyObject);
            Object.Destroy(frontProfile);
            Object.Destroy(rearProfile);
        }

        [UnityTest]
        public IEnumerator ThreeBodyClosedBezierOrbitFollowsOnlyTheLeadBodyReferenceFrame()
        {
            VehicleProfile leadProfile = CreateConnectorProfile(20f, 10f, new Vector3(10f, 0f, 0f), new Vector3(10f, 0f, 10f));
            VehicleProfile middleProfile = CreateConnectorProfile(20f, 10f, new Vector3(10f, 0f, 0f), new Vector3(10f, 0f, 10f));
            VehicleProfile rearProfile = CreateConnectorProfile(20f, 10f, new Vector3(10f, 0f, 0f), new Vector3(10f, 0f, 10f));
            leadProfile.VehicleOrbit.ConfigureWatchMarkers(CreateWatchMarkers(20f, 10f));
            middleProfile.VehicleOrbit.ConfigureWatchMarkers(CreateWatchMarkers(20f, 10f));
            rearProfile.VehicleOrbit.ConfigureWatchMarkers(CreateWatchMarkers(20f, 10f));
            leadProfile.VehicleOrbit.ConfigureRemovableSections(CreateRemovableSection(0f, 0.2f), CreateRemovableSection(0.5f, 5f / 6f));
            middleProfile.VehicleOrbit.ConfigureRemovableSections(CreateRemovableSection(0f, 0.2f), CreateRemovableSection(0.5f, 5f / 6f));
            rearProfile.VehicleOrbit.ConfigureRemovableSections(CreateRemovableSection(0f, 0.2f), CreateRemovableSection(0.5f, 5f / 6f));
            ThreeBodyClosedBezierOrbit orbit = new ThreeBodyClosedBezierOrbit(leadProfile, middleProfile, rearProfile);
            GameObject leadBodyObject = new GameObject("Lead Body");
            GameObject middleBodyObject = new GameObject("Middle Body");
            GameObject rearBodyObject = new GameObject("Rear Body");
            leadBodyObject.transform.SetPositionAndRotation(new Vector3(3f, 4f, 5f), Quaternion.Euler(20f, 30f, 15f));
            middleBodyObject.transform.SetPositionAndRotation(new Vector3(-8f, 0f, 12f), Quaternion.Euler(0f, 35f, 0f));
            rearBodyObject.transform.SetPositionAndRotation(new Vector3(6f, 1f, 17f), Quaternion.Euler(0f, -45f, 0f));
            float distance = orbit.Length * 0.4f;
            Vector3 localPosition = orbit.EvaluateLeadBodyLocalPosition(distance);
            Vector3 localNormal = orbit.EvaluateLeadBodyLocalInwardNormal(distance);
            Vector3 localWatchPoint = orbit.EvaluateLeadBodyLocalWatchPoint(distance);
            Vector3 firstWorldPosition = orbit.EvaluateWorldPosition(leadBodyObject.transform, distance);

            yield return null;

            middleBodyObject.transform.SetPositionAndRotation(new Vector3(11f, -2f, -3f), Quaternion.Euler(0f, -70f, 0f));
            rearBodyObject.transform.SetPositionAndRotation(new Vector3(-10f, 3f, 8f), Quaternion.Euler(0f, 80f, 0f));
            Vector3 articulatedWorldPosition = orbit.EvaluateWorldPosition(leadBodyObject.transform, distance);
            leadBodyObject.transform.SetPositionAndRotation(new Vector3(-6f, 2f, 1f), Quaternion.Euler(-10f, 70f, 25f));
            Vector3 expectedWorldPosition = leadBodyObject.transform.TransformPoint(localPosition);
            Vector3 expectedWorldNormal = leadBodyObject.transform.TransformDirection(localNormal);
            Vector3 expectedWorldWatchPoint = leadBodyObject.transform.TransformPoint(localWatchPoint);
            Vector3 actualWorldPosition = orbit.EvaluateWorldPosition(leadBodyObject.transform, distance);
            Vector3 actualWorldNormal = leadBodyObject.transform.TransformDirection(orbit.EvaluateLeadBodyLocalInwardNormal(distance));
            Vector3 actualWorldWatchPoint = leadBodyObject.transform.TransformPoint(orbit.EvaluateLeadBodyLocalWatchPoint(distance));

            Assert.AreEqual(ThreeBodyOrbitReferenceLayoutResult.Valid, orbit.ReferenceLayout.LayoutResult);
            Assert.AreEqual(ThreeBodyClosedBezierOrbitAssemblyResult.Valid, orbit.AssemblyResult);
            Assert.IsTrue(orbit.HasValidWatchMarkers);
            Assert.Less(Vector3.Distance(firstWorldPosition, articulatedWorldPosition), 0.0001f);
            Assert.Less(Vector3.Distance(expectedWorldPosition, actualWorldPosition), 0.0001f);
            Assert.Less(Vector3.Distance(expectedWorldNormal, actualWorldNormal), 0.0001f);
            Assert.Less(Vector3.Distance(expectedWorldWatchPoint, actualWorldWatchPoint), 0.0001f);

            Object.Destroy(leadBodyObject);
            Object.Destroy(middleBodyObject);
            Object.Destroy(rearBodyObject);
            Object.Destroy(leadProfile);
            Object.Destroy(middleProfile);
            Object.Destroy(rearProfile);
        }

        private VehicleProfile CreateVehicleProfile(CameraViewPreset orbitViewPreset, float width, float depth)
        {
            VehicleProfile vehicleProfile = ScriptableObject.CreateInstance<VehicleProfile>();
            VehicleOrbit vehicleOrbit = CreateRectangleOrbit(width, depth);
            vehicleOrbit.ConfigureWatchMarkers(CreateWatchMarkers(width, depth));
            vehicleProfile.ConfigureOrbit(vehicleOrbit);
            vehicleProfile.ConfigureOrbitView(orbitViewPreset);
            return vehicleProfile;
        }

        private VehicleProfile CreateConnectorProfile(float width, float depth, Vector3 frontAnchorPosition, Vector3 rearAnchorPosition)
        {
            VehicleOrbit vehicleOrbit = CreateRectangleOrbit(width, depth);
            vehicleOrbit.ConfigureRemovableSections(CreateRemovableSection(0f, 0.2f), CreateRemovableSection(0.4f, 0.6f));
            VehicleConnectorAnchors connectorAnchors = new VehicleConnectorAnchors();
            connectorAnchors.Configure(frontAnchorPosition, rearAnchorPosition);
            VehicleProfile vehicleProfile = ScriptableObject.CreateInstance<VehicleProfile>();
            vehicleProfile.ConfigureOrbit(vehicleOrbit);
            vehicleProfile.ConfigureConnectorAnchors(connectorAnchors);
            return vehicleProfile;
        }

        private VehicleOrbit CreateRectangleOrbit(float width, float depth)
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
            vehicleOrbit.Configure(knots, Quaternion.identity);
            return vehicleOrbit;
        }

        private OrbitRemovableSection CreateRemovableSection(float normalizedStartPosition, float normalizedEndPosition)
        {
            OrbitRemovableSection section = new OrbitRemovableSection();
            section.Configure(normalizedStartPosition, normalizedEndPosition);
            return section;
        }

        private List<OrbitWatchMarker> CreateWatchMarkers(float width, float depth)
        {
            List<OrbitWatchMarker> watchMarkers = new List<OrbitWatchMarker>();
            watchMarkers.Add(CreateWatchMarker(0f, new Vector3(width * 0.5f, 3f, depth * 0.5f)));
            watchMarkers.Add(CreateWatchMarker(0.5f, new Vector3(width * 0.5f, 4f, depth * 0.5f)));
            return watchMarkers;
        }

        private OrbitWatchMarker CreateWatchMarker(float normalizedOrbitPosition, Vector3 watchPoint)
        {
            OrbitWatchMarker marker = new OrbitWatchMarker();
            marker.Configure(normalizedOrbitPosition, watchPoint);
            return marker;
        }

        private VehicleOrbit CreateTriangleOrbit(Quaternion orientationAdjustment)
        {
            Vector3 firstAnchor = new Vector3(0f, 0f, -5f);
            Vector3 secondAnchor = new Vector3(5f, 0f, 5f);
            Vector3 thirdAnchor = new Vector3(-5f, 0f, 5f);
            List<BezierOrbitKnot> knots = new List<BezierOrbitKnot>();

            knots.Add(CreateKnot(firstAnchor, thirdAnchor, secondAnchor));
            knots.Add(CreateKnot(secondAnchor, firstAnchor, thirdAnchor));
            knots.Add(CreateKnot(thirdAnchor, secondAnchor, firstAnchor));

            VehicleOrbit vehicleOrbit = new VehicleOrbit();
            vehicleOrbit.Configure(knots, orientationAdjustment);
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
