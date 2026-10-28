using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Gley.CameraSystem.Tests.PlayMode
{
    public class SeatMotionPlayModeTests
    {
        private const float FrameTime = 0.02f;
        private const float PositionTolerance = 0.001f;
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
        public void SuppliedAcceleration_NoRigidbody_Works()
        {
            Assert.IsNull(root.GetComponent<Rigidbody>());
            Assert.AreEqual(CameraCommandResult.Accepted, controller.Activate());
            Quaternion rotation = camera.transform.rotation;
            target.SetSuppliedAcceleration(Vector3.forward * 2f);

            for (int frame = 0; frame < 100; frame++)
            {
                controller.UpdateCameraFrame(FrameTime);
            }

            Vector3 seatLocalEye = root.InverseTransformPoint(camera.transform.position);
            Assert.AreEqual(1.96f, seatLocalEye.z, PositionTolerance);
            Assert.AreEqual(-0.5f, seatLocalEye.x, PositionTolerance);
            Assert.AreEqual(1.8f, seatLocalEye.y, PositionTolerance);
            Assert.Less(Quaternion.Angle(rotation, camera.transform.rotation), AngleTolerance);

            target.SetSuppliedAcceleration(Vector3.back * 2f);
            for (int frame = 0; frame < 100; frame++)
            {
                controller.UpdateCameraFrame(FrameTime);
            }

            seatLocalEye = root.InverseTransformPoint(camera.transform.position);
            Assert.AreEqual(2.06f, seatLocalEye.z, PositionTolerance);
            Assert.Less(Quaternion.Angle(rotation, camera.transform.rotation), AngleTolerance);
        }

        [Test]
        public void RigidbodyAcceleration_UsedWhenNothingSupplied()
        {
            Rigidbody body = root.gameObject.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.velocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            Assert.IsFalse(target.HasSuppliedAcceleration);
            Assert.AreEqual(CameraCommandResult.Accepted, controller.Activate());
            controller.UpdateCameraFrame(FrameTime);

            body.angularVelocity = Vector3.up * 2f;
            controller.UpdateCameraFrame(FrameTime);

            Vector3 seatLocalEye = root.InverseTransformPoint(camera.transform.position);
            Assert.Less(seatLocalEye.z, 1.995f);
            Assert.GreaterOrEqual(seatLocalEye.z, 1.92f - PositionTolerance);
            Assert.AreEqual(-0.56f, seatLocalEye.x, PositionTolerance);
            Assert.AreEqual(1.8f, seatLocalEye.y, PositionTolerance);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            rig.Dispose();
            yield return null;
        }
    }
}
