using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Gley.CameraSystem.Tests.PlayMode
{
    public class CushionPlayModeTests
    {
        private const float FrameTime = 0.02f;
        private const float PositionTolerance = 0.0001f;

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
        public void PlayerIntensityFromPreferences_Applied()
        {
            controller.SetCushionIntensity(0f);
            float disabledOffset = MeasureBumpOffset();

            controller.SetCushionIntensity(1f);
            float normalOffset = MeasureBumpOffset();

            CameraPlayerPreferences preferences = controller.GetPlayerPreferences();
            preferences.SetCushionIntensity(2f);
            controller.ApplyPlayerPreferences(preferences);
            float intenseOffset = MeasureBumpOffset();

            Assert.AreEqual(0f, disabledOffset, PositionTolerance);
            Assert.Less(normalOffset, -0.001f);
            Assert.AreEqual(normalOffset * 2f, intenseOffset, PositionTolerance);
        }

        private float MeasureBumpOffset()
        {
            target.SetSuppliedAcceleration(Vector3.zero);
            controller.Deactivate();
            Assert.AreEqual(CameraCommandResult.Accepted, controller.Activate());
            controller.UpdateCameraFrame(FrameTime);
            float baselineHeight = root.InverseTransformPoint(camera.transform.position).y;
            target.SetSuppliedAcceleration(Vector3.up * 0.1f);
            controller.UpdateCameraFrame(FrameTime);
            return root.InverseTransformPoint(camera.transform.position).y - baselineHeight;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            rig.Dispose();
            yield return null;
        }
    }
}
