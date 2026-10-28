using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Gley.CameraSystem.Tests.PlayMode
{
    public class CameraRenderingStateTests
    {
        private CameraTestRig rig;

        [SetUp]
        public void SetUp()
        {
            rig = new CameraTestRig();
        }

        [Test]
        public void Replace_ThenRelease_RestoresOriginalMask()
        {
            rig.PresentationPreset.Rendering.Configure(CullingMaskMode.Replace, 8, 0, 0, false, 0.3f, false, 1000f);
            Camera camera = rig.CreateCamera("Camera");
            camera.cullingMask = 33;
            CameraSystemController controller = CreateController(camera, CameraTestRig.PresentationView);

            Assert.AreEqual(CameraCommandResult.Accepted, controller.Activate());
            Assert.AreEqual(8, camera.cullingMask);

            controller.Deactivate();
            Assert.AreEqual(33, camera.cullingMask);
        }

        [Test]
        public void AddRemove_CycleViewsThreeTimes_NoAccumulation()
        {
            rig.PresentationPreset.Rendering.Configure(CullingMaskMode.AddRemove, 0, 64, 1, false, 0.3f, false, 1000f);
            rig.InteriorPreset.Rendering.Configure(CullingMaskMode.AddRemove, 0, 256, 16, false, 0.3f, false, 1000f);
            Camera camera = rig.CreateCamera("Camera");
            camera.cullingMask = 17;
            CameraSystemController controller = CreateController(camera, CameraTestRig.PresentationView);

            Assert.AreEqual(CameraCommandResult.Accepted, controller.Activate());
            Assert.AreEqual(80, camera.cullingMask);
            for (int cycle = 0; cycle < 3; cycle++)
            {
                Assert.AreEqual(CameraCommandResult.Accepted, controller.SelectView(CameraTestRig.InteriorView));
                Assert.AreEqual(257, camera.cullingMask);
                Assert.AreEqual(CameraCommandResult.Accepted, controller.SelectView(CameraTestRig.FixedView));
                Assert.AreEqual(17, camera.cullingMask);
                Assert.AreEqual(CameraCommandResult.Accepted, controller.SelectView(CameraTestRig.PresentationView));
                Assert.AreEqual(80, camera.cullingMask);
            }

            controller.Deactivate();
            Assert.AreEqual(17, camera.cullingMask);
        }

        [Test]
        public void ClipOverrideDisabled_ClipUnchanged()
        {
            rig.PresentationPreset.Rendering.Configure(CullingMaskMode.Unchanged, 0, 0, 0, false, 2f, false, 50f);
            rig.FixedPreset.Rendering.Configure(CullingMaskMode.Unchanged, 0, 0, 0, true, 1f, true, 300f);
            Camera camera = rig.CreateCamera("Camera");
            camera.nearClipPlane = 0.5f;
            camera.farClipPlane = 500f;
            CameraSystemController controller = CreateController(camera, CameraTestRig.PresentationView);

            Assert.AreEqual(CameraCommandResult.Accepted, controller.Activate());
            Assert.AreEqual(0.5f, camera.nearClipPlane, 0.0001f);
            Assert.AreEqual(500f, camera.farClipPlane, 0.0001f);

            Assert.AreEqual(CameraCommandResult.Accepted, controller.SelectView(CameraTestRig.FixedView));
            Assert.AreEqual(1f, camera.nearClipPlane, 0.0001f);
            Assert.AreEqual(300f, camera.farClipPlane, 0.0001f);

            Assert.AreEqual(CameraCommandResult.Accepted, controller.SelectView(CameraTestRig.PresentationView));
            Assert.AreEqual(0.5f, camera.nearClipPlane, 0.0001f);
            Assert.AreEqual(500f, camera.farClipPlane, 0.0001f);

            controller.Deactivate();
            Assert.AreEqual(0.5f, camera.nearClipPlane, 0.0001f);
            Assert.AreEqual(500f, camera.farClipPlane, 0.0001f);
        }

        [Test]
        public void FieldOfView_NeverChanged()
        {
            rig.PresentationPreset.Rendering.Configure(CullingMaskMode.Replace, 8, 0, 0, true, 1f, true, 300f);
            rig.FixedPreset.Rendering.Configure(CullingMaskMode.AddRemove, 0, 64, 1, true, 0.2f, true, 800f);
            Camera camera = rig.CreateCamera("Camera");
            camera.fieldOfView = 47f;
            CameraSystemController controller = CreateController(camera, CameraTestRig.PresentationView);

            Assert.AreEqual(CameraCommandResult.Accepted, controller.Activate());
            Assert.AreEqual(47f, camera.fieldOfView, 0.0001f);
            controller.UpdateCameraFrame(0.02f);
            Assert.AreEqual(CameraCommandResult.Accepted, controller.SelectView(CameraTestRig.FixedView));
            controller.UpdateCameraFrame(0.02f);
            Assert.AreEqual(47f, camera.fieldOfView, 0.0001f);

            controller.Deactivate();
            Assert.AreEqual(47f, camera.fieldOfView, 0.0001f);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            rig.Dispose();
            yield return null;
        }

        private CameraSystemController CreateController(Camera camera, string viewName)
        {
            VehicleProfile profile = rig.CreateProfile(1f, 3f, 3f);
            VehicleCameraTarget target = rig.CreateTarget(rig.CreateBody("Truck"), profile);
            return rig.CreateController("Controller", camera, target, viewName);
        }
    }
}
