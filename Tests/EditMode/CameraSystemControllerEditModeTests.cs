using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Gley.CameraSystem.Tests.EditMode
{
    public class CameraSystemControllerEditModeTests
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

        [Test]
        public void ActivateWithoutAssignedCameraReportsMissingCamera()
        {
            controller.AssignVehicle(vehicleBodyObject.transform, vehicleProfile);

            CameraSystemActivationResult activationResult = controller.Activate();

            Assert.AreEqual(CameraSystemActivationResult.MissingCamera, activationResult);
            Assert.IsFalse(controller.IsActive);
            Assert.IsNull(controller.AssignedCamera);
        }

        [Test]
        public void FixedViewUsesVehicleLocalPoseWithoutChangingTheProfile()
        {
            vehicleBodyObject.transform.SetPositionAndRotation(new Vector3(10f, 3f, -2f), Quaternion.Euler(0f, 90f, 0f));
            string serializedProfileBeforeActivation = EditorJsonUtility.ToJson(vehicleProfile);
            controller.AssignCamera(assignedCamera);
            controller.AssignVehicle(vehicleBodyObject.transform, vehicleProfile);

            CameraSystemActivationResult activationResult = controller.Activate();
            controller.UpdateCameraVisuals(0f);

            Vector3 expectedCameraPosition = vehicleBodyObject.transform.TransformPoint(vehicleProfile.FixedCameraLocalPosition);
            Vector3 expectedWatchPoint = vehicleBodyObject.transform.TransformPoint(vehicleProfile.FixedWatchPointLocalPosition);
            Quaternion expectedCameraRotation = Quaternion.LookRotation(expectedWatchPoint - expectedCameraPosition, vehicleBodyObject.transform.up);
            float rotationDifference = Quaternion.Angle(expectedCameraRotation, assignedCamera.transform.rotation);
            controller.Release();

            Assert.AreEqual(CameraSystemActivationResult.Succeeded, activationResult);
            Assert.AreSame(assignedCamera, controller.AssignedCamera);
            Assert.IsFalse(controller.IsActive);
            Assert.AreEqual(expectedCameraPosition, assignedCamera.transform.position);
            Assert.Less(rotationDifference, 0.01f);
            Assert.AreEqual(serializedProfileBeforeActivation, EditorJsonUtility.ToJson(vehicleProfile));
        }

        [Test]
        public void RearAttachmentReportsInvalidInputsAndKeepsProfilesUnchanged()
        {
            GameObject rearVehicleBodyObject = new GameObject("Rear Vehicle Body");
            VehicleProfile rearVehicleProfile = ScriptableObject.CreateInstance<VehicleProfile>();
            string rootProfileBeforeAttachment = EditorJsonUtility.ToJson(vehicleProfile);
            string rearProfileBeforeAttachment = EditorJsonUtility.ToJson(rearVehicleProfile);
            GameObject otherRearVehicleBodyObject = new GameObject("Other Rear Vehicle Body");

            CameraSystemAttachmentResult missingRootResult = controller.AttachRearBody(rearVehicleBodyObject.transform, rearVehicleProfile);
            controller.AssignVehicle(vehicleBodyObject.transform, vehicleProfile);
            CameraSystemAttachmentResult missingRearBodyResult = controller.AttachRearBody(null, rearVehicleProfile);
            CameraSystemAttachmentResult missingRearProfileResult = controller.AttachRearBody(rearVehicleBodyObject.transform, null);
            CameraSystemAttachmentResult matchingRootResult = controller.AttachRearBody(vehicleBodyObject.transform, rearVehicleProfile);
            CameraSystemAttachmentResult attachmentResult = controller.AttachRearBody(rearVehicleBodyObject.transform, rearVehicleProfile);
            CameraSystemAttachmentResult duplicateAttachmentResult = controller.AttachRearBody(otherRearVehicleBodyObject.transform, rearVehicleProfile);

            Assert.AreEqual(CameraSystemAttachmentResult.MissingRootVehicleBody, missingRootResult);
            Assert.AreEqual(CameraSystemAttachmentResult.MissingRearVehicleBody, missingRearBodyResult);
            Assert.AreEqual(CameraSystemAttachmentResult.MissingRearVehicleProfile, missingRearProfileResult);
            Assert.AreEqual(CameraSystemAttachmentResult.RearVehicleMatchesRoot, matchingRootResult);
            Assert.AreEqual(CameraSystemAttachmentResult.Attached, attachmentResult);
            Assert.AreSame(rearVehicleBodyObject.transform, controller.RearVehicleBody);
            Assert.AreSame(rearVehicleProfile, controller.RearVehicleProfile);
            Assert.IsTrue(controller.HasRearVehicleAttachment);
            Assert.AreEqual(CameraSystemAttachmentResult.RearVehicleAlreadyAttached, duplicateAttachmentResult);

            CameraSystemAttachmentResult detachmentResult = controller.DetachRearBody();
            CameraSystemAttachmentResult missingAttachmentResult = controller.DetachRearBody();

            Assert.AreEqual(CameraSystemAttachmentResult.Detached, detachmentResult);
            Assert.IsFalse(controller.HasRearVehicleAttachment);
            Assert.IsNull(controller.RearVehicleBody);
            Assert.IsNull(controller.RearVehicleProfile);
            Assert.AreEqual(CameraSystemAttachmentResult.NoRearVehicleAttached, missingAttachmentResult);
            Assert.AreEqual(rootProfileBeforeAttachment, EditorJsonUtility.ToJson(vehicleProfile));
            Assert.AreEqual(rearProfileBeforeAttachment, EditorJsonUtility.ToJson(rearVehicleProfile));

            Object.DestroyImmediate(rearVehicleBodyObject);
            Object.DestroyImmediate(otherRearVehicleBodyObject);
            Object.DestroyImmediate(rearVehicleProfile);
        }

        [Test]
        public void AttachedExteriorViewUsesTheComposedOrbitAtActivation()
        {
            GameObject rearVehicleBodyObject = new GameObject("Rear Vehicle Body");
            CameraViewPreset exteriorViewPreset = CreateExteriorViewPreset();
            VehicleProfile frontVehicleProfile = CreateConnectorProfile(20f, 10f, new Vector3(10f, 0f, 0f), new Vector3(10f, 0f, 10f));
            VehicleProfile rearVehicleProfile = CreateConnectorProfile(40f, 20f, new Vector3(20f, 0f, 0f), new Vector3(20f, 0f, 20f));
            rearVehicleProfile.VehicleOrbit.ConfigureRemovableSections(CreateRemovableSection(0f, 1f / 3f), CreateRemovableSection(0.4f, 0.6f));
            vehicleBodyObject.transform.SetPositionAndRotation(new Vector3(3f, 2f, -4f), Quaternion.Euler(10f, 25f, 5f));
            controller.AssignCamera(assignedCamera);
            controller.AssignVehicle(vehicleBodyObject.transform, frontVehicleProfile);
            controller.SelectViewPreset(exteriorViewPreset);
            CameraSystemAttachmentResult attachmentResult = controller.AttachRearBody(rearVehicleBodyObject.transform, rearVehicleProfile);
            TwoBodyClosedBezierOrbit expectedOrbit = new TwoBodyClosedBezierOrbit(frontVehicleProfile, rearVehicleProfile);

            CameraSystemActivationResult activationResult = controller.Activate();

            Vector3 expectedCameraPosition = expectedOrbit.EvaluateWorldPosition(vehicleBodyObject.transform, 0f);
            Vector3 expectedWatchPoint = vehicleBodyObject.transform.TransformPoint(expectedOrbit.EvaluateLeadBodyLocalWatchPoint(0f));
            Quaternion expectedCameraRotation = Quaternion.LookRotation(expectedWatchPoint - expectedCameraPosition, vehicleBodyObject.transform.up);

            Assert.AreEqual(CameraSystemAttachmentResult.Attached, attachmentResult);
            Assert.AreEqual(TwoBodyClosedBezierOrbitAssemblyResult.Valid, expectedOrbit.AssemblyResult);
            Assert.AreEqual(CameraSystemActivationResult.Succeeded, activationResult);
            Assert.IsTrue(controller.UsesAttachedOrbit);
            Assert.Less(Vector3.Distance(expectedCameraPosition, assignedCamera.transform.position), 0.0001f);
            Assert.Less(Quaternion.Angle(expectedCameraRotation, assignedCamera.transform.rotation), 0.01f);

            Object.DestroyImmediate(rearVehicleBodyObject);
            Object.DestroyImmediate(exteriorViewPreset);
            Object.DestroyImmediate(frontVehicleProfile);
            Object.DestroyImmediate(rearVehicleProfile);
        }

        [Test]
        public void RootOnlyExteriorViewDoesNotRequireTheAttachedProfileToHaveAnOrbit()
        {
            GameObject rearVehicleBodyObject = new GameObject("Rear Vehicle Body");
            CameraViewPreset exteriorViewPreset = CreateExteriorViewPreset();
            VehicleProfile frontVehicleProfile = CreateConnectorProfile(20f, 10f, new Vector3(10f, 0f, 0f), new Vector3(10f, 0f, 10f));
            VehicleProfile rearVehicleProfile = ScriptableObject.CreateInstance<VehicleProfile>();
            frontVehicleProfile.VehicleOrbit.ConfigureAttachmentMerge(false);
            controller.AssignCamera(assignedCamera);
            controller.AssignVehicle(vehicleBodyObject.transform, frontVehicleProfile);
            controller.SelectViewPreset(exteriorViewPreset);
            CameraSystemAttachmentResult attachmentResult = controller.AttachRearBody(rearVehicleBodyObject.transform, rearVehicleProfile);
            ClosedBezierOrbit expectedOrbit = new ClosedBezierOrbit(frontVehicleProfile.VehicleOrbit);

            CameraSystemActivationResult activationResult = controller.Activate();

            Vector3 expectedCameraPosition = expectedOrbit.EvaluateWorldPosition(vehicleBodyObject.transform, 0f);

            Assert.AreEqual(CameraSystemAttachmentResult.Attached, attachmentResult);
            Assert.AreEqual(CameraSystemActivationResult.Succeeded, activationResult);
            Assert.IsFalse(controller.UsesAttachedOrbit);
            Assert.Less(Vector3.Distance(expectedCameraPosition, assignedCamera.transform.position), 0.0001f);

            Object.DestroyImmediate(rearVehicleBodyObject);
            Object.DestroyImmediate(exteriorViewPreset);
            Object.DestroyImmediate(frontVehicleProfile);
            Object.DestroyImmediate(rearVehicleProfile);
        }

        [Test]
        public void AttachedExteriorViewRejectsAnInvalidComposedOrbit()
        {
            GameObject rearVehicleBodyObject = new GameObject("Rear Vehicle Body");
            CameraViewPreset exteriorViewPreset = CreateExteriorViewPreset();
            VehicleProfile frontVehicleProfile = CreateConnectorProfile(20f, 10f, new Vector3(10f, 0f, 0f), new Vector3(10f, 0f, 10f));
            VehicleProfile rearVehicleProfile = ScriptableObject.CreateInstance<VehicleProfile>();
            rearVehicleProfile.ConfigureOrbit(CreateRectangleOrbit(40f, 20f));
            rearVehicleProfile.VehicleOrbit.ConfigureWatchMarkers(CreateWatchMarkers(40f, 20f));
            controller.AssignCamera(assignedCamera);
            controller.AssignVehicle(vehicleBodyObject.transform, frontVehicleProfile);
            controller.SelectViewPreset(exteriorViewPreset);
            CameraSystemAttachmentResult attachmentResult = controller.AttachRearBody(rearVehicleBodyObject.transform, rearVehicleProfile);

            CameraSystemActivationResult activationResult = controller.Activate();

            Assert.AreEqual(CameraSystemAttachmentResult.Attached, attachmentResult);
            Assert.AreEqual(CameraSystemActivationResult.InvalidAttachedOrbit, activationResult);
            Assert.IsFalse(controller.IsActive);
            Assert.IsFalse(controller.UsesAttachedOrbit);

            Object.DestroyImmediate(rearVehicleBodyObject);
            Object.DestroyImmediate(exteriorViewPreset);
            Object.DestroyImmediate(frontVehicleProfile);
            Object.DestroyImmediate(rearVehicleProfile);
        }

        [Test]
        public void LiveAttachmentRemapsARetainedFrontPositionWithoutMovingTheCamera()
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
            TwoBodyOrbitAttachmentRemapResolver expectedRemapResolver = new TwoBodyOrbitAttachmentRemapResolver(frontVehicleProfile, rearVehicleProfile);
            OrbitAttachmentRemapResult expectedRemapResult = expectedRemapResolver.ResolveFrontOrbitDistance(orbitDistanceBeforeAttachment);

            CameraSystemAttachmentResult attachmentResult = controller.AttachRearBody(rearVehicleBodyObject.transform, rearVehicleProfile);

            Assert.AreEqual(CameraSystemActivationResult.Succeeded, activationResult);
            Assert.AreEqual(OrbitAttachmentRemapResult.RetainedFrontPosition, expectedRemapResult);
            Assert.AreEqual(CameraSystemAttachmentResult.Attached, attachmentResult);
            Assert.IsTrue(controller.HasRearVehicleAttachment);
            Assert.IsTrue(controller.UsesAttachedOrbit);
            Assert.AreEqual(expectedRemapResolver.RemappedOrbitDistance, controller.OrbitDistance);
            Assert.Less(Vector3.Distance(cameraPositionBeforeAttachment, assignedCamera.transform.position), 0.0001f);
            Assert.Less(Quaternion.Angle(cameraRotationBeforeAttachment, assignedCamera.transform.rotation), 0.01f);

            Object.DestroyImmediate(rearVehicleBodyObject);
            Object.DestroyImmediate(exteriorViewPreset);
            Object.DestroyImmediate(frontVehicleProfile);
            Object.DestroyImmediate(rearVehicleProfile);
        }

        [Test]
        public void InvalidLiveAttachmentLeavesTheActiveRootOnlyOrbitUnchanged()
        {
            GameObject rearVehicleBodyObject = new GameObject("Rear Vehicle Body");
            CameraViewPreset exteriorViewPreset = CreateExteriorViewPreset();
            VehicleProfile frontVehicleProfile = CreateConnectorProfile(20f, 10f, new Vector3(10f, 0f, 0f), new Vector3(10f, 0f, 10f));
            VehicleProfile invalidRearVehicleProfile = ScriptableObject.CreateInstance<VehicleProfile>();
            invalidRearVehicleProfile.ConfigureOrbit(CreateRectangleOrbit(40f, 20f));
            invalidRearVehicleProfile.VehicleOrbit.ConfigureWatchMarkers(CreateWatchMarkers(40f, 20f));
            controller.AssignCamera(assignedCamera);
            controller.AssignVehicle(vehicleBodyObject.transform, frontVehicleProfile);
            controller.SelectViewPreset(exteriorViewPreset);
            CameraSystemActivationResult activationResult = controller.Activate();
            Vector3 cameraPositionBeforeAttachment = assignedCamera.transform.position;
            Quaternion cameraRotationBeforeAttachment = assignedCamera.transform.rotation;
            float orbitDistanceBeforeAttachment = controller.OrbitDistance;

            CameraSystemAttachmentResult attachmentResult = controller.AttachRearBody(rearVehicleBodyObject.transform, invalidRearVehicleProfile);

            Assert.AreEqual(CameraSystemActivationResult.Succeeded, activationResult);
            Assert.AreEqual(CameraSystemAttachmentResult.InvalidActiveExteriorOrbit, attachmentResult);
            Assert.IsTrue(controller.IsActive);
            Assert.IsFalse(controller.HasRearVehicleAttachment);
            Assert.IsFalse(controller.UsesAttachedOrbit);
            Assert.AreEqual(orbitDistanceBeforeAttachment, controller.OrbitDistance);
            Assert.Less(Vector3.Distance(cameraPositionBeforeAttachment, assignedCamera.transform.position), 0.0001f);
            Assert.Less(Quaternion.Angle(cameraRotationBeforeAttachment, assignedCamera.transform.rotation), 0.01f);

            Object.DestroyImmediate(rearVehicleBodyObject);
            Object.DestroyImmediate(exteriorViewPreset);
            Object.DestroyImmediate(frontVehicleProfile);
            Object.DestroyImmediate(invalidRearVehicleProfile);
        }

        [Test]
        public void LiveAttachmentMapsARemovedFrontPositionToTheRearFallback()
        {
            GameObject rearVehicleBodyObject = new GameObject("Rear Vehicle Body");
            CameraViewPreset exteriorViewPreset = CreateExteriorViewPreset();
            exteriorViewPreset.ConfigureOrbitTravel(36f, 0f);
            VehicleProfile frontVehicleProfile = CreateConnectorProfile(20f, 10f, new Vector3(10f, 0f, 0f), new Vector3(10f, 0f, 10f));
            VehicleProfile rearVehicleProfile = CreateConnectorProfile(40f, 20f, new Vector3(20f, 0f, 0f), new Vector3(20f, 0f, 20f));
            rearVehicleProfile.VehicleOrbit.ConfigureRemovableSections(CreateRemovableSection(0f, 1f / 3f), CreateRemovableSection(0.4f, 0.6f));
            controller.AssignCamera(assignedCamera);
            controller.AssignVehicle(vehicleBodyObject.transform, frontVehicleProfile);
            controller.SelectViewPreset(exteriorViewPreset);
            CameraSystemActivationResult activationResult = controller.Activate();
            controller.SetHorizontalOrbitIntent(1f);
            controller.UpdateCameraVisuals(1f);
            float orbitDistanceBeforeAttachment = controller.OrbitDistance;
            TwoBodyOrbitAttachmentRemapResolver expectedRemapResolver = new TwoBodyOrbitAttachmentRemapResolver(frontVehicleProfile, rearVehicleProfile);
            OrbitAttachmentRemapResult expectedRemapResult = expectedRemapResolver.ResolveFrontOrbitDistance(orbitDistanceBeforeAttachment);

            CameraSystemAttachmentResult attachmentResult = controller.AttachRearBody(rearVehicleBodyObject.transform, rearVehicleProfile);

            Vector3 expectedCameraPosition = expectedRemapResolver.CombinedOrbit.EvaluateWorldPosition(vehicleBodyObject.transform, expectedRemapResolver.RemappedOrbitDistance);

            Assert.AreEqual(CameraSystemActivationResult.Succeeded, activationResult);
            Assert.AreEqual(OrbitAttachmentRemapResult.RemovedFrontPositionMappedToRear, expectedRemapResult);
            Assert.AreEqual(CameraSystemAttachmentResult.Attached, attachmentResult);
            Assert.AreEqual(expectedRemapResolver.RemappedOrbitDistance, controller.OrbitDistance);
            Assert.Less(Vector3.Distance(expectedCameraPosition, assignedCamera.transform.position), 0.0001f);

            Object.DestroyImmediate(rearVehicleBodyObject);
            Object.DestroyImmediate(exteriorViewPreset);
            Object.DestroyImmediate(frontVehicleProfile);
            Object.DestroyImmediate(rearVehicleProfile);
        }

        [Test]
        public void LiveDetachmentRemapsARetainedFrontPositionWithoutMovingTheCamera()
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

            Assert.AreEqual(OrbitDetachmentRemapResult.RetainedFrontPosition, expectedRemapResult);
            Assert.AreEqual(CameraSystemAttachmentResult.Detached, detachmentResult);
            Assert.IsFalse(controller.HasRearVehicleAttachment);
            Assert.IsFalse(controller.UsesAttachedOrbit);
            Assert.AreEqual(expectedRemapResolver.RemappedOrbitDistance, controller.OrbitDistance);
            Assert.Less(Vector3.Distance(cameraPositionBeforeDetachment, assignedCamera.transform.position), 0.0001f);
            Assert.Less(Quaternion.Angle(cameraRotationBeforeDetachment, assignedCamera.transform.rotation), 0.01f);

            Object.DestroyImmediate(rearVehicleBodyObject);
            Object.DestroyImmediate(exteriorViewPreset);
            Object.DestroyImmediate(frontVehicleProfile);
            Object.DestroyImmediate(rearVehicleProfile);
        }

        [Test]
        public void LiveDetachmentMapsANonFrontPositionToTheRemovedRearSectionMidpoint()
        {
            GameObject rearVehicleBodyObject = new GameObject("Rear Vehicle Body");
            CameraViewPreset exteriorViewPreset = CreateExteriorViewPreset();
            exteriorViewPreset.ConfigureOrbitTravel(36f, 0f);
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
            float orbitDistanceBeforeDetachment = controller.OrbitDistance;
            TwoBodyOrbitDetachmentRemapResolver expectedRemapResolver = new TwoBodyOrbitDetachmentRemapResolver(frontVehicleProfile, new TwoBodyClosedBezierOrbit(frontVehicleProfile, rearVehicleProfile));
            OrbitDetachmentRemapResult expectedRemapResult = expectedRemapResolver.ResolveCombinedOrbitDistance(orbitDistanceBeforeDetachment);

            CameraSystemAttachmentResult detachmentResult = controller.DetachRearBody();

            Vector3 expectedCameraPosition = expectedRemapResolver.RootOrbit.EvaluateWorldPosition(vehicleBodyObject.transform, expectedRemapResolver.RemappedOrbitDistance);

            Assert.AreEqual(OrbitDetachmentRemapResult.NonFrontPositionMappedToRemovedRear, expectedRemapResult);
            Assert.AreEqual(CameraSystemAttachmentResult.Detached, detachmentResult);
            Assert.AreEqual(expectedRemapResolver.RemappedOrbitDistance, controller.OrbitDistance);
            Assert.Less(Vector3.Distance(expectedCameraPosition, assignedCamera.transform.position), 0.0001f);

            Object.DestroyImmediate(rearVehicleBodyObject);
            Object.DestroyImmediate(exteriorViewPreset);
            Object.DestroyImmediate(frontVehicleProfile);
            Object.DestroyImmediate(rearVehicleProfile);
        }

        [Test]
        public void InvalidLiveDetachmentLeavesTheComposedOrbitUnchanged()
        {
            GameObject rearVehicleBodyObject = new GameObject("Rear Vehicle Body");
            CameraViewPreset exteriorViewPreset = CreateExteriorViewPreset();
            VehicleProfile frontVehicleProfile = CreateConnectorProfile(20f, 10f, new Vector3(10f, 0f, 0f), new Vector3(10f, 0f, 10f));
            VehicleProfile rearVehicleProfile = CreateConnectorProfile(40f, 20f, new Vector3(20f, 0f, 0f), new Vector3(20f, 0f, 20f));
            rearVehicleProfile.VehicleOrbit.ConfigureRemovableSections(CreateRemovableSection(0f, 1f / 3f), CreateRemovableSection(0.4f, 0.6f));
            controller.AssignCamera(assignedCamera);
            controller.AssignVehicle(vehicleBodyObject.transform, frontVehicleProfile);
            controller.SelectViewPreset(exteriorViewPreset);
            controller.Activate();
            controller.AttachRearBody(rearVehicleBodyObject.transform, rearVehicleProfile);
            frontVehicleProfile.VehicleOrbit.ConfigureWatchMarkers(new List<OrbitWatchMarker>());
            Vector3 cameraPositionBeforeDetachment = assignedCamera.transform.position;
            Quaternion cameraRotationBeforeDetachment = assignedCamera.transform.rotation;
            float orbitDistanceBeforeDetachment = controller.OrbitDistance;

            CameraSystemAttachmentResult detachmentResult = controller.DetachRearBody();

            Assert.AreEqual(CameraSystemAttachmentResult.InvalidActiveExteriorDetachment, detachmentResult);
            Assert.IsTrue(controller.HasRearVehicleAttachment);
            Assert.IsTrue(controller.UsesAttachedOrbit);
            Assert.AreEqual(orbitDistanceBeforeDetachment, controller.OrbitDistance);
            Assert.Less(Vector3.Distance(cameraPositionBeforeDetachment, assignedCamera.transform.position), 0.0001f);
            Assert.Less(Quaternion.Angle(cameraRotationBeforeDetachment, assignedCamera.transform.rotation), 0.01f);

            Object.DestroyImmediate(rearVehicleBodyObject);
            Object.DestroyImmediate(exteriorViewPreset);
            Object.DestroyImmediate(frontVehicleProfile);
            Object.DestroyImmediate(rearVehicleProfile);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(cameraObject);
            Object.DestroyImmediate(controllerObject);
            Object.DestroyImmediate(vehicleBodyObject);
            Object.DestroyImmediate(fixedViewPreset);
            Object.DestroyImmediate(vehicleProfile);
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
