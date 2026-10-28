using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Gley.CameraSystem.Tests.PlayMode
{
    public class OrbitViewPlayModeTests
    {
        private const float RollDegrees = 20f;
        private const float SwingDegrees = 25f;

        private CameraTestRig rig;

        [SetUp]
        public void SetUp()
        {
            rig = new CameraTestRig();
        }

        [Test]
        public void VerticalIntent_MovesPositionOnly_AimStillOnWatchPoint()
        {
            Transform root = rig.CreateBody("Truck");
            root.SetPositionAndRotation(new Vector3(3f, 0f, -2f), Quaternion.Euler(0f, 30f, 0f));
            VehicleCameraTarget target = rig.CreateTarget(root, rig.CreateProfile(1f, 3f, 3f));
            Camera camera = rig.CreateCamera("Camera");
            CameraSystemController controller = rig.CreateController("Controller", camera, target, CameraTestRig.PresentationView);

            Assert.AreEqual(CameraCommandResult.Accepted, controller.Activate());
            controller.UpdateCameraFrame(0.02f);
            Vector3 before = camera.transform.position;
            controller.SetHeldIntent(0f, 1f, 0f);
            controller.UpdateCameraFrame(0.5f);

            Vector3 watchPoint = root.TransformPoint(new Vector3(0f, 1f, 0f));
            AssertPosition(root.TransformPoint(new Vector3(0f, 1f, -3f)), camera.transform.position);
            AssertPosition(before + root.up, camera.transform.position);
            Assert.AreEqual(1f, controller.CurrentPose.HeightOffset, 0.0001f);
            Assert.Less(Vector3.Angle(watchPoint - camera.transform.position, camera.transform.forward), 0.01f);
        }

        [Test]
        public void RollFollowZero_RolledTruck_HorizonLevel()
        {
            Camera camera = ActivateOnRolledTruck(0f);

            Assert.Less(Vector3.Angle(Vector3.forward, camera.transform.forward), 0.01f);
            Assert.Less(Vector3.Angle(Vector3.up, camera.transform.up), 0.01f);
            Assert.AreEqual(0f, camera.transform.right.y, 0.0001f);
        }

        [Test]
        public void RollFollowOne_RolledTruck_CameraRollsWithTruck()
        {
            Camera camera = ActivateOnRolledTruck(1f);
            float roll = RollDegrees * Mathf.Deg2Rad;
            Vector3 expectedUp = new Vector3(-Mathf.Sin(roll), Mathf.Cos(roll), 0f);

            Assert.Less(Vector3.Angle(Vector3.forward, camera.transform.forward), 0.01f);
            Assert.Less(Vector3.Angle(expectedUp, camera.transform.up), 0.01f);
        }

        [Test]
        public void RootAimFrame_ArticulatedTrailer_AimUnchanged()
        {
            Transform trailer;
            CameraSystemController controller = ActivateAtTrailerMarker(out trailer);
            Camera camera = controller.AssignedCamera;

            Assert.AreEqual(CameraCommandResult.Accepted, controller.SetAimFrame(AimFrame.Root));
            controller.UpdateCameraFrame(0.02f);
            Quaternion before = camera.transform.rotation;

            AssertPosition(new Vector3(1f, 0f, -8f), camera.transform.position);
            Assert.Less(Vector3.Angle(new Vector3(0f, 1f, -8f) - camera.transform.position, camera.transform.forward), 0.05f);

            SwingTrailer(trailer);
            controller.UpdateCameraFrame(0.02f);

            AssertPosition(new Vector3(1f, 0f, -8f), camera.transform.position);
            Assert.Less(Quaternion.Angle(before, camera.transform.rotation), 0.01f);
        }

        [Test]
        public void OwnerAimFrame_ArticulatedTrailer_AimFollowsTrailerDetail()
        {
            Transform trailer;
            CameraSystemController controller = ActivateAtTrailerMarker(out trailer);
            Camera camera = controller.AssignedCamera;

            Assert.AreEqual(AimFrame.OwnerBody, controller.AimFrame);
            controller.UpdateCameraFrame(0.02f);
            Quaternion before = camera.transform.rotation;

            AssertPosition(new Vector3(1f, 0f, -8f), camera.transform.position);
            Assert.Less(Vector3.Angle(new Vector3(0f, 1f, -8f) - camera.transform.position, camera.transform.forward), 0.05f);

            SwingTrailer(trailer);
            controller.UpdateCameraFrame(0.02f);

            Vector3 trailerDetail = trailer.TransformPoint(new Vector3(0f, 1f, 0f));
            AssertPosition(new Vector3(1f, 0f, -8f), camera.transform.position);
            Assert.Less(Vector3.Angle(trailerDetail - camera.transform.position, camera.transform.forward), 0.05f);
            Assert.Greater(Quaternion.Angle(before, camera.transform.rotation), 1f);
        }

        [Test]
        public void ZoomChanges_FieldOfViewUnchanged()
        {
            Transform root = rig.CreateBody("Truck");
            VehicleCameraTarget target = rig.CreateTarget(root, rig.CreateProfile(1f, 3f, 3f));
            Camera camera = rig.CreateCamera("Camera");
            camera.fieldOfView = 47f;
            CameraSystemController controller = rig.CreateController("Controller", camera, target, CameraTestRig.PresentationView);

            Assert.AreEqual(CameraCommandResult.Accepted, controller.Activate());
            controller.SetHeldIntent(0f, 0f, 1f);
            controller.UpdateCameraFrame(0.2f);

            AssertPosition(new Vector3(0f, 0f, -2.4f), camera.transform.position);
            Assert.AreEqual(47f, camera.fieldOfView, 0.0001f);

            controller.SetHeldIntent(0f, 0f, -1f);
            controller.UpdateCameraFrame(1f);

            AssertPosition(new Vector3(0f, 0f, -5.4f), camera.transform.position);
            Assert.AreEqual(47f, camera.fieldOfView, 0.0001f);
        }

        [Test]
        public void FixedView_IgnoresPlayerInput()
        {
            Transform root = rig.CreateBody("Truck");
            root.SetPositionAndRotation(new Vector3(-4f, 1f, 6f), Quaternion.Euler(0f, 70f, 0f));
            VehicleCameraTarget target = rig.CreateTarget(root, rig.CreateProfile(1f, 3f, 3f));
            Camera camera = rig.CreateCamera("Camera");
            CameraSystemController controller = rig.CreateController("Controller", camera, target, CameraTestRig.FixedView);

            Assert.AreEqual(CameraCommandResult.Accepted, controller.Activate());
            controller.UpdateCameraFrame(0.02f);
            Quaternion rotation = camera.transform.rotation;
            controller.SetHeldIntent(1f, 1f, 1f);
            controller.AddDrag(new Vector2(0.5f, 0.5f));
            controller.AddPinch(1f);
            controller.UpdateCameraFrame(1f);

            AssertPosition(root.TransformPoint(new Vector3(2f, 3f, -8f)), camera.transform.position);
            Assert.Less(Quaternion.Angle(rotation, camera.transform.rotation), 0.01f);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            rig.Dispose();
            yield return null;
        }

        private Camera ActivateOnRolledTruck(float imageRollFollow)
        {
            Transform root = rig.CreateBody("Truck");
            root.rotation = Quaternion.Euler(0f, 0f, RollDegrees);
            VehicleProfile profile = rig.CreateProfile(1f, 3f, 3f);
            VehicleViewEntry view;
            Assert.IsTrue(profile.TryGetView(CameraTestRig.PresentationView, out view));
            view.ConfigureDefaultPose(new OrbitPose(0f, 0f, 1f));
            rig.PresentationPreset.ConfigureImageRollFollow(imageRollFollow);
            VehicleCameraTarget target = rig.CreateTarget(root, profile);
            Camera camera = rig.CreateCamera("Camera");
            CameraSystemController controller = rig.CreateController("Controller", camera, target, CameraTestRig.PresentationView);

            Assert.AreEqual(CameraCommandResult.Accepted, controller.Activate());
            controller.UpdateCameraFrame(0.02f);
            AssertPosition(root.TransformPoint(new Vector3(0f, 1f, -3f)), camera.transform.position);
            return camera;
        }

        private CameraSystemController ActivateAtTrailerMarker(out Transform trailer)
        {
            Transform root = rig.CreateBody("Truck");
            trailer = rig.CreateBody("Trailer");
            trailer.position = new Vector3(0f, 0f, -8f);
            VehicleProfile truckProfile = rig.CreateProfile(1f, 3f, 3f);
            VehicleProfile trailerProfile = rig.CreateProfile(1f, 4f, 4f);
            VehicleViewEntry view;
            Assert.IsTrue(truckProfile.TryGetView(CameraTestRig.PresentationView, out view));
            view.ConfigureDefaultPose(new OrbitPose(Mathf.Atan2(1f, 3.5f) * Mathf.Rad2Deg, 0f, 0f));
            VehicleCameraTarget target = rig.CreateTarget(root, truckProfile);
            Assert.AreEqual(VehicleTargetResult.Accepted, target.Attach(trailer, trailerProfile, ChainEnd.Rear));
            Camera camera = rig.CreateCamera("Camera");
            CameraSystemController controller = rig.CreateController("Controller", camera, target, CameraTestRig.PresentationView);

            Assert.AreEqual(CameraCommandResult.Accepted, controller.Activate());
            return controller;
        }

        private void SwingTrailer(Transform trailer)
        {
            Vector3 hitch = new Vector3(0f, 0f, -3.5f);
            Quaternion swing = Quaternion.Euler(0f, SwingDegrees, 0f);
            trailer.SetPositionAndRotation(hitch + swing * new Vector3(0f, 0f, -4.5f), swing);
        }

        private void AssertPosition(Vector3 expected, Vector3 actual)
        {
            Assert.Less(Vector3.Distance(expected, actual), 0.001f, $"Expected {expected:F4}, got {actual:F4}");
        }
    }
}
