using System.Collections;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Gley.CameraSystem.Tests.PlayMode
{
    public class CameraSystemControllerTests
    {
        private CameraTestRig rig;
        private int targetLostCount;
        private int cameraLostCount;
        private int interpolationWarningCount;

        [SetUp]
        public void SetUp()
        {
            rig = new CameraTestRig();
            targetLostCount = 0;
            cameraLostCount = 0;
            interpolationWarningCount = 0;
        }

        [Test]
        public void Activate_NoCamera_MissingCamera()
        {
            VehicleCameraTarget target = rig.CreateTarget(rig.CreateBody("Truck"), rig.CreateProfile(1f, 3f, 3f));
            CameraSystemController controller = rig.CreateController("Controller", null, target, CameraTestRig.FixedView);

            Assert.AreEqual(CameraCommandResult.MissingCamera, controller.Activate());
            Assert.IsFalse(controller.IsActive);
        }

        [Test]
        public void Activate_NoTarget_MissingTarget()
        {
            Camera camera = rig.CreateCamera("Camera");
            CameraSystemController controller = rig.CreateController("Controller", camera, null, CameraTestRig.FixedView);
            VehicleCameraTarget unconfigured = rig.CreateTarget(null, null);
            CameraSystemController secondController = rig.CreateController("Second Controller", camera, unconfigured, CameraTestRig.FixedView);

            Assert.AreEqual(CameraCommandResult.MissingTarget, controller.Activate());
            Assert.AreEqual(CameraCommandResult.MissingTarget, secondController.Activate());
            Assert.IsFalse(controller.IsActive);
            Assert.IsFalse(secondController.IsActive);
            Assert.IsNull(camera.GetComponent<CameraOwnershipMarker>());
        }

        [Test]
        public void Activate_SecondInstanceSameCamera_CameraInUse_FirstKeepsCamera()
        {
            Transform root = rig.CreateBody("Truck");
            root.SetPositionAndRotation(new Vector3(4f, 0f, 2f), Quaternion.Euler(0f, 30f, 0f));
            VehicleProfile profile = rig.CreateProfile(1f, 3f, 3f);
            VehicleCameraTarget target = rig.CreateTarget(root, profile);
            Camera camera = rig.CreateCamera("Camera");
            CameraSystemController first = rig.CreateController("First Controller", camera, target, CameraTestRig.FixedView);
            CameraSystemController second = rig.CreateController("Second Controller", camera, target, CameraTestRig.InteriorView);

            Assert.AreEqual(CameraCommandResult.Accepted, first.Activate());
            LogAssert.Expect(LogType.Warning, new Regex("camera in use.*First Controller"));
            Assert.AreEqual(CameraCommandResult.CameraInUse, second.Activate());
            first.UpdateCameraFrame(0.02f);

            Assert.IsTrue(first.IsActive);
            Assert.IsFalse(second.IsActive);
            AssertPosition(root.TransformPoint(profile.FixedViewPose.CameraLocalPosition), camera.transform.position);
        }

        [UnityTest]
        public IEnumerator Deactivate_ThenOtherInstanceActivates_Accepted()
        {
            Transform root = rig.CreateBody("Truck");
            VehicleProfile profile = rig.CreateProfile(1f, 3f, 3f);
            VehicleCameraTarget target = rig.CreateTarget(root, profile);
            Camera camera = rig.CreateCamera("Camera");
            CameraSystemController first = rig.CreateController("First Controller", camera, target, CameraTestRig.FixedView);
            CameraSystemController second = rig.CreateController("Second Controller", camera, target, CameraTestRig.InteriorView);

            Assert.AreEqual(CameraCommandResult.Accepted, first.Activate());
            first.Deactivate();
            Assert.AreEqual(CameraCommandResult.Accepted, second.Activate());
            yield return null;

            Assert.IsFalse(first.IsActive);
            Assert.IsTrue(second.IsActive);
            AssertPosition(root.TransformPoint(profile.Seat.EyeLocalPosition), camera.transform.position);
            CameraOwnershipMarker marker = camera.GetComponent<CameraOwnershipMarker>();
            Assert.IsNotNull(marker);
            Assert.AreSame(second, marker.Owner);
        }

        [Test]
        public void Deactivate_LeavesCameraTransformUnchanged()
        {
            Transform root = rig.CreateBody("Truck");
            VehicleCameraTarget target = rig.CreateTarget(root, rig.CreateProfile(1f, 3f, 3f));
            Camera camera = rig.CreateCamera("Camera");
            CameraSystemController controller = rig.CreateController("Controller", camera, target, CameraTestRig.FixedView);

            Assert.AreEqual(CameraCommandResult.Accepted, controller.Activate());
            controller.UpdateCameraFrame(0.02f);
            Vector3 position = camera.transform.position;
            Quaternion rotation = camera.transform.rotation;
            controller.Deactivate();
            root.position = new Vector3(10f, 0f, 10f);
            controller.UpdateCameraFrame(0.02f);

            Assert.IsFalse(controller.IsActive);
            Assert.AreEqual(position, camera.transform.position);
            Assert.AreEqual(rotation, camera.transform.rotation);
        }

        [Test]
        public void Activate_UnknownView_ViewNotAvailable_NothingChanges()
        {
            VehicleCameraTarget target = rig.CreateTarget(rig.CreateBody("Truck"), rig.CreateProfile(1f, 3f, 3f));
            Camera camera = rig.CreateCamera("Camera");
            camera.transform.SetPositionAndRotation(new Vector3(1f, 2f, 3f), Quaternion.Euler(10f, 20f, 0f));
            Vector3 position = camera.transform.position;
            Quaternion rotation = camera.transform.rotation;
            CameraSystemController controller = rig.CreateController("Controller", camera, target, "Missing View");

            Assert.AreEqual(CameraCommandResult.ViewNotAvailable, controller.Activate());
            Assert.IsFalse(controller.IsActive);
            Assert.IsNull(controller.ActiveView);
            Assert.IsNull(camera.GetComponent<CameraOwnershipMarker>());
            Assert.AreEqual(position, camera.transform.position);
            Assert.AreEqual(rotation, camera.transform.rotation);
        }

        [Test]
        public void Activate_ProfileFormatVersionTwo_AssetNeedsUpgrade()
        {
            VehicleProfile profile = rig.CreateProfile(1f, 3f, 3f);
            string json = JsonUtility.ToJson(profile);
            Assert.IsTrue(json.Contains("\"formatVersion\":1"));
            JsonUtility.FromJsonOverwrite(json.Replace("\"formatVersion\":1", "\"formatVersion\":2"), profile);
            Assert.AreEqual(2, profile.FormatVersion);
            VehicleCameraTarget target = rig.CreateTarget(rig.CreateBody("Truck"), profile);
            Camera camera = rig.CreateCamera("Camera");
            CameraSystemController controller = rig.CreateController("Controller", camera, target, CameraTestRig.FixedView);

            Assert.AreEqual(CameraCommandResult.AssetNeedsUpgrade, controller.Activate());
            Assert.IsFalse(controller.IsActive);
        }

        [Test]
        public void FixedView_CameraAtAuthoredPoseLookingAtWatchPoint()
        {
            Transform root = rig.CreateBody("Truck");
            root.SetPositionAndRotation(new Vector3(10f, 3f, -2f), Quaternion.Euler(8f, 90f, -5f));
            VehicleCameraTarget target = rig.CreateTarget(root, rig.CreateProfile(1f, 3f, 3f));
            Camera camera = rig.CreateCamera("Camera");
            CameraSystemController controller = rig.CreateController("Controller", camera, target, CameraTestRig.FixedView);

            Assert.AreEqual(CameraCommandResult.Accepted, controller.Activate());
            controller.UpdateCameraFrame(0.02f);

            Vector3 expectedPosition = root.position + root.rotation * new Vector3(2f, 3f, -8f);
            Vector3 expectedWatchPoint = root.position + root.rotation * new Vector3(0f, 1f, 2f);
            AssertPosition(expectedPosition, camera.transform.position);
            AssertRotation(Quaternion.LookRotation(expectedWatchPoint - expectedPosition, root.up), camera.transform.rotation);
        }

        [Test]
        public void OrbitView_DefaultPoseBearingZero_CameraBehindVehicle()
        {
            Transform root = rig.CreateBody("Truck");
            root.SetPositionAndRotation(new Vector3(5f, 0f, 2f), Quaternion.Euler(0f, 90f, 0f));
            VehicleCameraTarget target = rig.CreateTarget(root, rig.CreateProfile(1f, 3f, 3f));
            Camera camera = rig.CreateCamera("Camera");
            CameraSystemController controller = rig.CreateController("Controller", camera, target, CameraTestRig.PresentationView);

            Assert.AreEqual(CameraCommandResult.Accepted, controller.Activate());
            controller.UpdateCameraFrame(0.02f);

            Vector3 expectedPosition = new Vector3(2f, 0f, 2f);
            Vector3 expectedWatchPoint = new Vector3(5f, 1f, 2f);
            AssertPosition(expectedPosition, camera.transform.position);
            AssertRotation(Quaternion.LookRotation(expectedWatchPoint - expectedPosition, Vector3.up), camera.transform.rotation);
        }

        [UnityTest]
        public IEnumerator DestroyRoot_CameraHoldsPose_RaisesTargetLostOnce_NoException()
        {
            Transform root = rig.CreateBody("Truck");
            VehicleCameraTarget target = rig.CreateTarget(root, rig.CreateProfile(1f, 3f, 3f));
            Camera camera = rig.CreateCamera("Camera");
            CameraSystemController controller = rig.CreateController("Controller", camera, target, CameraTestRig.PresentationView);
            controller.TargetLost += CountTargetLost;

            Assert.AreEqual(CameraCommandResult.Accepted, controller.Activate());
            controller.UpdateCameraFrame(0.02f);
            Vector3 position = camera.transform.position;
            Quaternion rotation = camera.transform.rotation;
            Object.Destroy(root.gameObject);
            yield return null;

            LogAssert.Expect(LogType.Warning, new Regex("target lost"));
            controller.SetHorizontalOrbitIntent(1f);
            controller.UpdateCameraFrame(0.02f);
            controller.UpdateCameraFrame(0.02f);
            controller.UpdateCameraFrame(0.02f);

            Assert.AreEqual(1, targetLostCount);
            Assert.IsTrue(controller.IsActive);
            Assert.IsTrue(controller.IsTargetLost);
            Assert.AreEqual(position, camera.transform.position);
            Assert.AreEqual(rotation, camera.transform.rotation);
            controller.TargetLost -= CountTargetLost;
        }

        [Test]
        public void TwoCamerasOneTarget_AttachOnce_BothRebuild()
        {
            Transform root = rig.CreateBody("Truck");
            root.SetPositionAndRotation(new Vector3(3f, 0f, -1f), Quaternion.Euler(0f, -40f, 0f));
            Transform trailer = rig.CreateBody("Trailer");
            VehicleProfile truckProfile = rig.CreateProfile(1f, 3f, 3f);
            VehicleProfile trailerProfile = rig.CreateProfile(1f, 4f, 4f);
            VehicleCameraTarget target = rig.CreateTarget(root, truckProfile);
            Camera firstCamera = rig.CreateCamera("First Camera");
            Camera secondCamera = rig.CreateCamera("Second Camera");
            CameraSystemController first = rig.CreateController("First Controller", firstCamera, target, CameraTestRig.PresentationView);
            CameraSystemController second = rig.CreateController("Second Controller", secondCamera, target, CameraTestRig.PresentationView);

            Assert.AreEqual(CameraCommandResult.Accepted, first.Activate());
            Assert.AreEqual(CameraCommandResult.Accepted, second.Activate());
            AssertPosition(root.TransformPoint(new Vector3(0f, 0f, -3f)), firstCamera.transform.position);
            Assert.AreEqual(VehicleTargetResult.Accepted, target.Attach(trailer, trailerProfile, ChainEnd.Rear));
            first.UpdateCameraFrame(0.02f);
            second.UpdateCameraFrame(0.02f);

            Vector3 trailerRearCentre = root.TransformPoint(new Vector3(0f, 0f, -12f));
            AssertPosition(trailerRearCentre, firstCamera.transform.position);
            AssertPosition(trailerRearCentre, secondCamera.transform.position);
        }

        [Test]
        public void TwoInstances_IndependentViewsAndCameras()
        {
            Transform root = rig.CreateBody("Truck");
            VehicleProfile profile = rig.CreateProfile(1f, 3f, 3f);
            VehicleCameraTarget target = rig.CreateTarget(root, profile);
            Camera fixedCamera = rig.CreateCamera("Fixed Camera");
            Camera interiorCamera = rig.CreateCamera("Interior Camera");
            CameraSystemController fixedController = rig.CreateController("Fixed Controller", fixedCamera, target, CameraTestRig.FixedView);
            CameraSystemController interiorController = rig.CreateController("Interior Controller", interiorCamera, target, CameraTestRig.InteriorView);

            Assert.AreEqual(CameraCommandResult.Accepted, fixedController.Activate());
            Assert.AreEqual(CameraCommandResult.Accepted, interiorController.Activate());
            root.SetPositionAndRotation(new Vector3(-6f, 1f, 9f), Quaternion.Euler(0f, 120f, 0f));
            fixedController.UpdateCameraFrame(0.02f);
            interiorController.UpdateCameraFrame(0.02f);

            AssertPosition(root.position + root.rotation * new Vector3(2f, 3f, -8f), fixedCamera.transform.position);
            AssertPosition(root.position + root.rotation * new Vector3(-0.5f, 1.8f, 2f), interiorCamera.transform.position);
            AssertRotation(root.rotation, interiorCamera.transform.rotation);
            Assert.AreEqual(CameraTestRig.FixedView, fixedController.ActiveView.Name);
            Assert.AreEqual(CameraTestRig.InteriorView, interiorController.ActiveView.Name);

            fixedController.Deactivate();
            root.position = new Vector3(20f, 1f, 9f);
            interiorController.UpdateCameraFrame(0.02f);

            Assert.IsFalse(fixedController.IsActive);
            Assert.IsTrue(interiorController.IsActive);
            AssertPosition(root.position + root.rotation * new Vector3(-0.5f, 1.8f, 2f), interiorCamera.transform.position);
        }

        [UnityTest]
        public IEnumerator ManualMode_NoUpdateWithoutCall()
        {
            Transform root = rig.CreateBody("Truck");
            VehicleProfile profile = rig.CreateProfile(1f, 3f, 3f);
            VehicleCameraTarget target = rig.CreateTarget(root, profile);
            Camera camera = rig.CreateCamera("Camera");
            CameraSystemController controller = rig.CreateController("Controller", camera, target, CameraTestRig.FixedView);

            Assert.AreEqual(CameraCommandResult.Accepted, controller.Activate());
            Vector3 position = camera.transform.position;
            root.position = new Vector3(15f, 0f, 0f);
            yield return null;
            yield return null;

            Assert.AreEqual(CameraUpdateMode.Manual, controller.UpdateMode);
            Assert.AreEqual(position, camera.transform.position);
            controller.UpdateCameraFrame(0.02f);
            AssertPosition(root.TransformPoint(profile.FixedViewPose.CameraLocalPosition), camera.transform.position);
        }

        [UnityTest]
        public IEnumerator DestroyCamera_WhileActive_DeactivatesRaisesCameraLostOnce_NoException()
        {
            VehicleCameraTarget target = rig.CreateTarget(rig.CreateBody("Truck"), rig.CreateProfile(1f, 3f, 3f));
            Camera camera = rig.CreateCamera("Camera");
            CameraSystemController controller = rig.CreateController("Controller", camera, target, CameraTestRig.PresentationView);
            controller.CameraLost += CountCameraLost;

            Assert.AreEqual(CameraCommandResult.Accepted, controller.Activate());
            Object.Destroy(camera.gameObject);
            yield return null;

            LogAssert.Expect(LogType.Warning, new Regex("camera lost"));
            controller.UpdateCameraFrame(0.02f);
            controller.UpdateCameraFrame(0.02f);

            Assert.AreEqual(1, cameraLostCount);
            Assert.IsFalse(controller.IsActive);
            controller.CameraLost -= CountCameraLost;
        }

        [UnityTest]
        public IEnumerator DestroyCamera_ThenSetCameraAndActivate_Works()
        {
            Transform root = rig.CreateBody("Truck");
            VehicleProfile profile = rig.CreateProfile(1f, 3f, 3f);
            VehicleCameraTarget target = rig.CreateTarget(root, profile);
            Camera camera = rig.CreateCamera("Camera");
            CameraSystemController controller = rig.CreateController("Controller", camera, target, CameraTestRig.FixedView);

            Assert.AreEqual(CameraCommandResult.Accepted, controller.Activate());
            Object.Destroy(camera.gameObject);
            yield return null;

            LogAssert.Expect(LogType.Warning, new Regex("camera lost"));
            controller.UpdateCameraFrame(0.02f);
            Assert.AreEqual(CameraCommandResult.MissingCamera, controller.Activate());
            Camera replacement = rig.CreateCamera("Replacement Camera");

            Assert.AreEqual(CameraCommandResult.Accepted, controller.SetCamera(replacement));
            Assert.AreEqual(CameraCommandResult.Accepted, controller.Activate());
            controller.UpdateCameraFrame(0.02f);
            AssertPosition(root.TransformPoint(profile.FixedViewPose.CameraLocalPosition), replacement.transform.position);
        }

        [Test]
        public void RigidbodyWithoutInterpolation_LogsOneWarning()
        {
            Transform root = rig.CreateBody("Interpolation Truck");
            Rigidbody body = root.gameObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.None;
            VehicleCameraTarget target = rig.CreateTarget(root, rig.CreateProfile(1f, 3f, 3f));
            Camera camera = rig.CreateCamera("Camera");
            CameraSystemController controller = rig.CreateController("Controller", camera, target, CameraTestRig.FixedView);
            Application.logMessageReceived += CountInterpolationWarning;

            LogAssert.Expect(LogType.Warning, "Interpolation Truck has Rigidbody interpolation set to None; the camera may jitter. Set it to Interpolate.");
            Assert.AreEqual(CameraCommandResult.Accepted, controller.Activate());
            controller.Deactivate();
            Assert.AreEqual(CameraCommandResult.Accepted, controller.Activate());
            target.Configure(root, target.GetProfile(0));
            controller.UpdateCameraFrame(0.02f);
            Application.logMessageReceived -= CountInterpolationWarning;

            Assert.AreEqual(1, interpolationWarningCount);
        }

        [Test]
        public void RuntimeUse_ProfileAndPresetUnchanged()
        {
            Transform root = rig.CreateBody("Truck");
            Transform trailer = rig.CreateBody("Trailer");
            VehicleProfile truckProfile = rig.CreateProfile(1f, 3f, 3f);
            VehicleProfile trailerProfile = rig.CreateProfile(1f, 4f, 4f);
            VehicleCameraTarget target = rig.CreateTarget(root, truckProfile);
            Camera camera = rig.CreateCamera("Camera");
            CameraSystemController controller = rig.CreateController("Controller", camera, target, CameraTestRig.PresentationView);
            string truckBefore = JsonUtility.ToJson(truckProfile);
            string trailerBefore = JsonUtility.ToJson(trailerProfile);
            string presentationBefore = JsonUtility.ToJson(rig.PresentationPreset);
            string fixedBefore = JsonUtility.ToJson(rig.FixedPreset);

            Assert.AreEqual(CameraCommandResult.Accepted, controller.Activate());
            controller.SetHorizontalOrbitIntent(1f);
            controller.SetHeightIntent(1f);
            controller.SetZoomIntent(-1f);
            controller.UpdateCameraFrame(0.5f);
            controller.SetHorizontalOrbitIntent(0f);
            Assert.AreEqual(VehicleTargetResult.Accepted, target.Attach(trailer, trailerProfile, ChainEnd.Rear));
            controller.UpdateCameraFrame(0.02f);
            Assert.AreEqual(VehicleTargetResult.Accepted, target.Detach(trailer));
            controller.UpdateCameraFrame(0.02f);
            Assert.AreEqual(CameraCommandResult.Accepted, controller.SelectView(CameraTestRig.FixedView));
            controller.UpdateCameraFrame(0.02f);
            controller.Deactivate();

            Assert.AreEqual(truckBefore, JsonUtility.ToJson(truckProfile));
            Assert.AreEqual(trailerBefore, JsonUtility.ToJson(trailerProfile));
            Assert.AreEqual(presentationBefore, JsonUtility.ToJson(rig.PresentationPreset));
            Assert.AreEqual(fixedBefore, JsonUtility.ToJson(rig.FixedPreset));
        }

        [Test]
        public void CreatedAtRuntime_ConfigureThenActivate_Works()
        {
            Transform root = rig.CreateBody("Truck");
            root.position = new Vector3(0f, 0f, 30f);
            VehicleProfile profile = rig.CreateProfile(1f, 3f, 3f);
            VehicleCameraTarget target = rig.CreateTarget(root, profile);
            Camera camera = rig.CreateCamera("Camera");
            GameObject controllerObject = rig.CreateBody("Runtime Controller").gameObject;
            CameraSystemController controller = controllerObject.AddComponent<CameraSystemController>();

            controller.SetUpdateMode(CameraUpdateMode.Manual);
            controller.Configure(camera, target, CameraTestRig.FixedView);

            Assert.AreEqual(CameraCommandResult.Accepted, controller.Activate());
            Assert.AreSame(camera, controller.AssignedCamera);
            Assert.AreSame(target, controller.Target);
            AssertPosition(root.TransformPoint(profile.FixedViewPose.CameraLocalPosition), camera.transform.position);
        }

        [Test]
        public void SetCamera_WhileActive_InvalidWhileActive()
        {
            VehicleCameraTarget target = rig.CreateTarget(rig.CreateBody("Truck"), rig.CreateProfile(1f, 3f, 3f));
            Camera camera = rig.CreateCamera("Camera");
            Camera other = rig.CreateCamera("Other Camera");
            CameraSystemController controller = rig.CreateController("Controller", camera, target, CameraTestRig.FixedView);

            Assert.AreEqual(CameraCommandResult.Accepted, controller.Activate());
            Assert.AreEqual(CameraCommandResult.InvalidWhileActive, controller.SetCamera(other));
            Assert.AreSame(camera, controller.AssignedCamera);
            Assert.IsTrue(controller.IsActive);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Application.logMessageReceived -= CountInterpolationWarning;
            rig.Dispose();
            yield return null;
        }

        private void CountTargetLost()
        {
            targetLostCount++;
        }

        private void CountCameraLost()
        {
            cameraLostCount++;
        }

        private void CountInterpolationWarning(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Warning && condition.Contains("Rigidbody interpolation"))
            {
                interpolationWarningCount++;
            }
        }

        private void AssertPosition(Vector3 expected, Vector3 actual)
        {
            Assert.Less(Vector3.Distance(expected, actual), 0.001f, $"Expected {expected:F4}, got {actual:F4}");
        }

        private void AssertRotation(Quaternion expected, Quaternion actual)
        {
            Assert.Less(Quaternion.Angle(expected, actual), 0.01f);
        }
    }
}
