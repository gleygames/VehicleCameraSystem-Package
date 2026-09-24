using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Gley.CameraSystem.Tests.PlayMode
{
    public class CameraSystemControllerPlayModeTests
    {
        private GameObject cameraObject;
        private GameObject controllerObject;
        private GameObject vehicleBodyObject;
        private Camera assignedCamera;
        private CameraSystemController controller;
        private CameraViewPreset fixedViewPreset;
        private VehicleProfile vehicleProfile;

        [SetUp]
        public void SetUp()
        {
            cameraObject = new GameObject("Assigned Camera");
            assignedCamera = cameraObject.AddComponent<Camera>();
            controllerObject = new GameObject("Camera System Controller");
            controller = controllerObject.AddComponent<CameraSystemController>();
            vehicleBodyObject = new GameObject("Vehicle Body");
            fixedViewPreset = ScriptableObject.CreateInstance<CameraViewPreset>();
            fixedViewPreset.Configure(CameraViewType.Fixed);
            vehicleProfile = ScriptableObject.CreateInstance<VehicleProfile>();
            vehicleProfile.ConfigureFixedView(fixedViewPreset, new Vector3(0f, 2f, -6f), new Vector3(0f, 1f, 4f));
        }

        [UnityTest]
        public IEnumerator TwoFixedViewInstancesFollowOnlyTheirAssignedVehicleBodies()
        {
            GameObject secondCameraObject = new GameObject("Second Assigned Camera");
            Camera secondAssignedCamera = secondCameraObject.AddComponent<Camera>();
            GameObject secondControllerObject = new GameObject("Second Camera System Controller");
            CameraSystemController secondController = secondControllerObject.AddComponent<CameraSystemController>();
            GameObject secondVehicleBodyObject = new GameObject("Second Vehicle Body");
            CameraViewPreset secondFixedViewPreset = ScriptableObject.CreateInstance<CameraViewPreset>();
            secondFixedViewPreset.Configure(CameraViewType.Fixed);
            VehicleProfile secondVehicleProfile = ScriptableObject.CreateInstance<VehicleProfile>();
            secondVehicleProfile.ConfigureFixedView(secondFixedViewPreset, new Vector3(0f, 3f, -8f), new Vector3(0f, 1f, 5f));

            vehicleBodyObject.transform.position = new Vector3(3f, 0f, 0f);
            secondVehicleBodyObject.transform.position = new Vector3(-5f, 0f, 4f);
            controller.AssignCamera(assignedCamera);
            controller.AssignVehicle(vehicleBodyObject.transform, vehicleProfile);
            secondController.AssignCamera(secondAssignedCamera);
            secondController.AssignVehicle(secondVehicleBodyObject.transform, secondVehicleProfile);

            CameraSystemActivationResult firstActivationResult = controller.Activate();
            CameraSystemActivationResult secondActivationResult = secondController.Activate();
            yield return null;

            Assert.AreEqual(CameraSystemActivationResult.Succeeded, firstActivationResult);
            Assert.AreEqual(CameraSystemActivationResult.Succeeded, secondActivationResult);
            Assert.IsTrue(controller.IsActive);
            Assert.IsTrue(secondController.IsActive);
            Assert.AreSame(assignedCamera, controller.AssignedCamera);
            Assert.AreSame(secondAssignedCamera, secondController.AssignedCamera);

            vehicleBodyObject.transform.position = new Vector3(8f, 0f, -2f);
            secondVehicleBodyObject.transform.position = new Vector3(-7f, 0f, 9f);
            yield return null;

            Vector3 expectedFirstCameraPosition = vehicleBodyObject.transform.TransformPoint(vehicleProfile.FixedCameraLocalPosition);
            Vector3 expectedSecondCameraPosition = secondVehicleBodyObject.transform.TransformPoint(secondVehicleProfile.FixedCameraLocalPosition);

            Assert.AreEqual(expectedFirstCameraPosition, assignedCamera.transform.position);
            Assert.AreEqual(expectedSecondCameraPosition, secondAssignedCamera.transform.position);
            Assert.AreNotEqual(assignedCamera.transform.position, secondAssignedCamera.transform.position);

            controller.Release();
            secondController.Release();

            Assert.IsFalse(controller.IsActive);
            Assert.IsFalse(secondController.IsActive);

            Object.Destroy(secondCameraObject);
            Object.Destroy(secondControllerObject);
            Object.Destroy(secondVehicleBodyObject);
            Object.Destroy(secondFixedViewPreset);
            Object.Destroy(secondVehicleProfile);
        }

        [UnityTest]
        public IEnumerator RearAttachmentLeavesAnActiveFixedViewUnchanged()
        {
            GameObject rearVehicleBodyObject = new GameObject("Rear Vehicle Body");
            VehicleProfile rearVehicleProfile = ScriptableObject.CreateInstance<VehicleProfile>();
            vehicleBodyObject.transform.SetPositionAndRotation(new Vector3(3f, 2f, -4f), Quaternion.Euler(10f, 25f, 5f));
            controller.AssignCamera(assignedCamera);
            controller.AssignVehicle(vehicleBodyObject.transform, vehicleProfile);
            CameraSystemActivationResult activationResult = controller.Activate();
            controller.UpdateCameraVisuals(0f);
            Vector3 cameraPositionBeforeAttachment = assignedCamera.transform.position;
            Quaternion cameraRotationBeforeAttachment = assignedCamera.transform.rotation;

            CameraSystemAttachmentResult attachmentResult = controller.AttachRearBody(rearVehicleBodyObject.transform, rearVehicleProfile);
            controller.UpdateCameraVisuals(0f);
            yield return null;

            Assert.AreEqual(CameraSystemActivationResult.Succeeded, activationResult);
            Assert.AreEqual(CameraSystemAttachmentResult.Attached, attachmentResult);
            Assert.IsTrue(controller.HasRearVehicleAttachment);
            Assert.AreEqual(cameraPositionBeforeAttachment, assignedCamera.transform.position);
            Assert.Less(Quaternion.Angle(cameraRotationBeforeAttachment, assignedCamera.transform.rotation), 0.01f);

            CameraSystemAttachmentResult detachmentResult = controller.DetachRearBody();

            Assert.AreEqual(CameraSystemAttachmentResult.Detached, detachmentResult);
            Assert.IsFalse(controller.HasRearVehicleAttachment);

            Object.Destroy(rearVehicleBodyObject);
            Object.Destroy(rearVehicleProfile);
        }

        [UnityTest]
        public IEnumerator AttachedExteriorViewFollowsTheLeadBodyWithoutUsingTheRearBodyPose()
        {
            GameObject rearVehicleBodyObject = new GameObject("Rear Vehicle Body");
            CameraViewPreset exteriorViewPreset = CreateExteriorViewPreset();
            VehicleProfile frontVehicleProfile = CreateConnectorProfile(20f, 10f, new Vector3(10f, 0f, 0f), new Vector3(10f, 0f, 10f));
            VehicleProfile rearVehicleProfile = CreateConnectorProfile(40f, 20f, new Vector3(20f, 0f, 0f), new Vector3(20f, 0f, 20f));
            rearVehicleProfile.VehicleOrbit.ConfigureRemovableSections(CreateRemovableSection(0f, 1f / 3f), CreateRemovableSection(0.4f, 0.6f));
            TwoBodyClosedBezierOrbit expectedOrbit = new TwoBodyClosedBezierOrbit(frontVehicleProfile, rearVehicleProfile);
            vehicleBodyObject.transform.SetPositionAndRotation(new Vector3(3f, 2f, -4f), Quaternion.Euler(10f, 25f, 5f));
            rearVehicleBodyObject.transform.SetPositionAndRotation(new Vector3(-20f, 1f, 7f), Quaternion.Euler(0f, 40f, 0f));
            controller.AssignCamera(assignedCamera);
            controller.AssignVehicle(vehicleBodyObject.transform, frontVehicleProfile);
            controller.SelectViewPreset(exteriorViewPreset);
            CameraSystemAttachmentResult attachmentResult = controller.AttachRearBody(rearVehicleBodyObject.transform, rearVehicleProfile);
            CameraSystemActivationResult activationResult = controller.Activate();
            controller.UpdateCameraVisuals(0f);
            Vector3 expectedInitialPosition = expectedOrbit.EvaluateWorldPosition(vehicleBodyObject.transform, 0f);

            yield return null;

            rearVehicleBodyObject.transform.SetPositionAndRotation(new Vector3(100f, -5f, -30f), Quaternion.Euler(45f, 90f, 20f));
            controller.UpdateCameraVisuals(0f);

            Assert.AreEqual(TwoBodyClosedBezierOrbitAssemblyResult.Valid, expectedOrbit.AssemblyResult);
            Assert.AreEqual(CameraSystemAttachmentResult.Attached, attachmentResult);
            Assert.AreEqual(CameraSystemActivationResult.Succeeded, activationResult);
            Assert.IsTrue(controller.UsesAttachedOrbit);
            Assert.Less(Vector3.Distance(expectedInitialPosition, assignedCamera.transform.position), 0.0001f);

            vehicleBodyObject.transform.SetPositionAndRotation(new Vector3(-7f, 5f, 11f), Quaternion.Euler(5f, 60f, 0f));
            controller.UpdateCameraVisuals(0f);
            Vector3 expectedMovedPosition = expectedOrbit.EvaluateWorldPosition(vehicleBodyObject.transform, 0f);
            Vector3 expectedWatchPoint = vehicleBodyObject.transform.TransformPoint(expectedOrbit.EvaluateLeadBodyLocalWatchPoint(0f));
            Quaternion expectedRotation = Quaternion.LookRotation(expectedWatchPoint - expectedMovedPosition, vehicleBodyObject.transform.up);

            Assert.Less(Vector3.Distance(expectedMovedPosition, assignedCamera.transform.position), 0.0001f);
            Assert.Less(Quaternion.Angle(expectedRotation, assignedCamera.transform.rotation), 0.01f);

            Object.Destroy(rearVehicleBodyObject);
            Object.Destroy(exteriorViewPreset);
            Object.Destroy(frontVehicleProfile);
            Object.Destroy(rearVehicleProfile);
        }

        [UnityTest]
        public IEnumerator LiveAttachmentRemapsTheActiveExteriorCameraOntoTheComposedOrbit()
        {
            GameObject rearVehicleBodyObject = new GameObject("Rear Vehicle Body");
            CameraViewPreset exteriorViewPreset = CreateExteriorViewPreset();
            exteriorViewPreset.ConfigureOrbitTravel(15f, 0f);
            VehicleProfile frontVehicleProfile = CreateConnectorProfile(20f, 10f, new Vector3(10f, 0f, 0f), new Vector3(10f, 0f, 10f));
            VehicleProfile rearVehicleProfile = CreateConnectorProfile(40f, 20f, new Vector3(20f, 0f, 0f), new Vector3(20f, 0f, 20f));
            rearVehicleProfile.VehicleOrbit.ConfigureRemovableSections(CreateRemovableSection(0f, 1f / 3f), CreateRemovableSection(0.4f, 0.6f));
            controller.AssignCamera(assignedCamera);
            controller.AssignVehicle(vehicleBodyObject.transform, frontVehicleProfile);
            controller.SelectViewPreset(exteriorViewPreset);
            CameraSystemActivationResult activationResult = controller.Activate();
            controller.SetHorizontalOrbitIntent(1f);
            controller.UpdateCameraVisuals(1f);
            Vector3 cameraPositionBeforeAttachment = assignedCamera.transform.position;
            Quaternion cameraRotationBeforeAttachment = assignedCamera.transform.rotation;
            float orbitDistanceBeforeAttachment = controller.OrbitDistance;
            controller.SetHorizontalOrbitIntent(0f);
            TwoBodyOrbitAttachmentRemapResolver expectedRemapResolver = new TwoBodyOrbitAttachmentRemapResolver(frontVehicleProfile, rearVehicleProfile);
            OrbitAttachmentRemapResult expectedRemapResult = expectedRemapResolver.ResolveFrontOrbitDistance(orbitDistanceBeforeAttachment);

            CameraSystemAttachmentResult attachmentResult = controller.AttachRearBody(rearVehicleBodyObject.transform, rearVehicleProfile);
            yield return null;

            Assert.AreEqual(CameraSystemActivationResult.Succeeded, activationResult);
            Assert.AreEqual(OrbitAttachmentRemapResult.RetainedFrontPosition, expectedRemapResult);
            Assert.AreEqual(CameraSystemAttachmentResult.Attached, attachmentResult);
            Assert.IsTrue(controller.UsesAttachedOrbit);
            Assert.AreEqual(expectedRemapResolver.RemappedOrbitDistance, controller.OrbitDistance);
            Assert.Less(Vector3.Distance(cameraPositionBeforeAttachment, assignedCamera.transform.position), 0.0001f);
            Assert.Less(Quaternion.Angle(cameraRotationBeforeAttachment, assignedCamera.transform.rotation), 0.01f);

            Object.Destroy(rearVehicleBodyObject);
            Object.Destroy(exteriorViewPreset);
            Object.Destroy(frontVehicleProfile);
            Object.Destroy(rearVehicleProfile);
        }

        [UnityTest]
        public IEnumerator LiveDetachmentRemapsTheActiveComposedCameraOntoTheRootOrbit()
        {
            GameObject rearVehicleBodyObject = new GameObject("Rear Vehicle Body");
            CameraViewPreset exteriorViewPreset = CreateExteriorViewPreset();
            exteriorViewPreset.ConfigureOrbitTravel(15f, 0f);
            VehicleProfile frontVehicleProfile = CreateConnectorProfile(20f, 10f, new Vector3(10f, 0f, 0f), new Vector3(10f, 0f, 10f));
            VehicleProfile rearVehicleProfile = CreateConnectorProfile(40f, 20f, new Vector3(20f, 0f, 0f), new Vector3(20f, 0f, 20f));
            rearVehicleProfile.VehicleOrbit.ConfigureRemovableSections(CreateRemovableSection(0f, 1f / 3f), CreateRemovableSection(0.4f, 0.6f));
            controller.AssignCamera(assignedCamera);
            controller.AssignVehicle(vehicleBodyObject.transform, frontVehicleProfile);
            controller.SelectViewPreset(exteriorViewPreset);
            controller.Activate();
            controller.SetHorizontalOrbitIntent(1f);
            controller.UpdateCameraVisuals(1f);
            controller.SetHorizontalOrbitIntent(0f);
            controller.AttachRearBody(rearVehicleBodyObject.transform, rearVehicleProfile);
            Vector3 cameraPositionBeforeDetachment = assignedCamera.transform.position;
            Quaternion cameraRotationBeforeDetachment = assignedCamera.transform.rotation;
            float orbitDistanceBeforeDetachment = controller.OrbitDistance;
            TwoBodyOrbitDetachmentRemapResolver expectedRemapResolver = new TwoBodyOrbitDetachmentRemapResolver(frontVehicleProfile, new TwoBodyClosedBezierOrbit(frontVehicleProfile, rearVehicleProfile));
            OrbitDetachmentRemapResult expectedRemapResult = expectedRemapResolver.ResolveCombinedOrbitDistance(orbitDistanceBeforeDetachment);

            CameraSystemAttachmentResult detachmentResult = controller.DetachRearBody();
            yield return null;

            Assert.AreEqual(OrbitDetachmentRemapResult.RetainedFrontPosition, expectedRemapResult);
            Assert.AreEqual(CameraSystemAttachmentResult.Detached, detachmentResult);
            Assert.IsFalse(controller.HasRearVehicleAttachment);
            Assert.IsFalse(controller.UsesAttachedOrbit);
            Assert.AreEqual(expectedRemapResolver.RemappedOrbitDistance, controller.OrbitDistance);
            Assert.Less(Vector3.Distance(cameraPositionBeforeDetachment, assignedCamera.transform.position), 0.0001f);
            Assert.Less(Quaternion.Angle(cameraRotationBeforeDetachment, assignedCamera.transform.rotation), 0.01f);

            Object.Destroy(rearVehicleBodyObject);
            Object.Destroy(exteriorViewPreset);
            Object.Destroy(frontVehicleProfile);
            Object.Destroy(rearVehicleProfile);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(cameraObject);
            Object.Destroy(controllerObject);
            Object.Destroy(vehicleBodyObject);
            Object.Destroy(fixedViewPreset);
            Object.Destroy(vehicleProfile);
            yield return null;
        }

        private CameraViewPreset CreateExteriorViewPreset()
        {
            CameraViewPreset exteriorViewPreset = ScriptableObject.CreateInstance<CameraViewPreset>();
            exteriorViewPreset.Configure(CameraViewType.ExteriorPresentation);
            return exteriorViewPreset;
        }

        private VehicleProfile CreateConnectorProfile(float width, float depth, Vector3 frontAnchorPosition, Vector3 rearAnchorPosition)
        {
            VehicleOrbit vehicleOrbit = CreateRectangleOrbit(width, depth);
            vehicleOrbit.ConfigureWatchMarkers(CreateWatchMarkers(width, depth));
            vehicleOrbit.ConfigureRemovableSections(CreateRemovableSection(0f, 0.2f), CreateRemovableSection(0.5f, 5f / 6f));
            VehicleConnectorAnchors connectorAnchors = new VehicleConnectorAnchors();
            connectorAnchors.Configure(frontAnchorPosition, rearAnchorPosition);
            VehicleProfile connectorProfile = ScriptableObject.CreateInstance<VehicleProfile>();
            connectorProfile.ConfigureOrbit(vehicleOrbit);
            connectorProfile.ConfigureConnectorAnchors(connectorAnchors);
            return connectorProfile;
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

        private List<OrbitWatchMarker> CreateWatchMarkers(float width, float depth)
        {
            List<OrbitWatchMarker> watchMarkers = new List<OrbitWatchMarker>();
            OrbitWatchMarker firstWatchMarker = new OrbitWatchMarker();
            OrbitWatchMarker secondWatchMarker = new OrbitWatchMarker();
            firstWatchMarker.Configure(0f, new Vector3(width * 0.5f, 2f, depth * 0.5f));
            secondWatchMarker.Configure(0.5f, new Vector3(width * 0.5f, 3f, depth * 0.5f));
            watchMarkers.Add(firstWatchMarker);
            watchMarkers.Add(secondWatchMarker);
            return watchMarkers;
        }

        private OrbitRemovableSection CreateRemovableSection(float startPosition, float endPosition)
        {
            OrbitRemovableSection removableSection = new OrbitRemovableSection();
            removableSection.Configure(startPosition, endPosition);
            return removableSection;
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
