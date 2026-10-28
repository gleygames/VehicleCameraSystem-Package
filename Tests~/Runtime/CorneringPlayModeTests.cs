using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Gley.CameraSystem.Tests.PlayMode
{
    public class CorneringPlayModeTests
    {
        private const float FrameTime = 0.02f;
        private const float PositionTolerance = 0.005f;
        private const float AngleTolerance = 0.01f;

        private CameraTestRig rig;
        private CameraSystemController controller;
        private VehicleCameraTarget target;
        private Camera camera;
        private Transform root;

        [SetUp]
        public void SetUp()
        {
            rig = new CameraTestRig();
            root = rig.CreateBody("Truck");
            VehicleProfile profile = rig.CreateProfile(1f, 3f, 3f);
            target = rig.CreateTarget(root, profile);
            camera = rig.CreateCamera("Camera");
            controller = rig.CreateController("Controller", camera, target, CameraTestRig.InteriorView);
        }

        [Test]
        public void ConstantRadiusTurn_SeatAheadOfOrigin_OffsetMatchesCentripetal()
        {
            const float radius = 2f;
            const float angularSpeed = 1f;
            const float seatLateralPosition = -0.5f;
            const float seatForwardPosition = 2f;
            const float strength = 0.015f;
            Assert.AreEqual(CameraCommandResult.Accepted, controller.Activate());
            Quaternion initialRotation = camera.transform.rotation;

            for (int frame = 1; frame <= 200; frame++)
            {
                float angle = frame * angularSpeed * FrameTime;
                Quaternion rotation = Quaternion.Euler(0f, angle * Mathf.Rad2Deg, 0f);
                Vector3 right = rotation * Vector3.right;
                root.SetPositionAndRotation(-radius * right, rotation);
                controller.UpdateCameraFrame(FrameTime);
            }

            Vector3 eye = root.InverseTransformPoint(camera.transform.position);
            float expectedLateralAcceleration = angularSpeed * angularSpeed * (radius - seatLateralPosition);
            float expectedOffset = -expectedLateralAcceleration * strength;
            Assert.AreEqual(seatLateralPosition + expectedOffset, eye.x, PositionTolerance);
            Assert.GreaterOrEqual(eye.z, seatForwardPosition - 0.08f - PositionTolerance);
            Assert.LessOrEqual(eye.z, seatForwardPosition + 0.08f + PositionTolerance);
            Assert.Greater(Quaternion.Angle(initialRotation, camera.transform.rotation), 1f);
        }

        [Test]
        public void CombinedWithBraking_StillWithinEnvelope()
        {
            Assert.AreEqual(CameraCommandResult.Accepted, controller.Activate());
            Quaternion rotation = camera.transform.rotation;
            target.SetSuppliedAcceleration(new Vector3(8f, 0f, -4f));

            for (int frame = 0; frame < 100; frame++)
            {
                controller.UpdateCameraFrame(FrameTime);
            }

            Vector3 eye = root.InverseTransformPoint(camera.transform.position);
            Assert.AreEqual(-0.56f, eye.x, PositionTolerance);
            Assert.AreEqual(1.8f, eye.y, PositionTolerance);
            Assert.AreEqual(2.08f, eye.z, PositionTolerance);
            Assert.Less(Quaternion.Angle(rotation, camera.transform.rotation), AngleTolerance);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            rig.Dispose();
            yield return null;
        }
    }
}
