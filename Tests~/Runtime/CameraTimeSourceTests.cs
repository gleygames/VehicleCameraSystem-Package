using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Gley.CameraSystem.Tests.PlayMode
{
    public class CameraTimeSourceTests
    {
        private const int WaitedFrames = 10;

        private CameraTestRig rig;
        private float originalTimeScale;

        [SetUp]
        public void SetUp()
        {
            rig = new CameraTestRig();
            originalTimeScale = Time.timeScale;
        }

        [UnityTest]
        public IEnumerator Unscaled_TimeScaleZero_ManualTravelStillMoves()
        {
            rig.PresentationPreset.ConfigureTimeSource(CameraTimeSource.Unscaled);
            CameraSystemController controller = CreateLateUpdateController();

            Assert.AreEqual(CameraCommandResult.Accepted, controller.Activate());
            Time.timeScale = 0f;
            yield return null;
            float startDistance = controller.OrbitDistance;
            controller.SetHorizontalOrbitIntent(1f);
            for (int frame = 0; frame < WaitedFrames; frame++)
            {
                yield return null;
            }

            Assert.Greater(Mathf.Abs(controller.OrbitDistance - startDistance), 0f);
        }

        [UnityTest]
        public IEnumerator Scaled_TimeScaleZero_NoMovement()
        {
            rig.PresentationPreset.ConfigureTimeSource(CameraTimeSource.Scaled);
            CameraSystemController controller = CreateLateUpdateController();

            Assert.AreEqual(CameraCommandResult.Accepted, controller.Activate());
            Time.timeScale = 0f;
            yield return null;
            float startDistance = controller.OrbitDistance;
            controller.SetHorizontalOrbitIntent(1f);
            for (int frame = 0; frame < WaitedFrames; frame++)
            {
                yield return null;
            }

            Assert.AreEqual(startDistance, controller.OrbitDistance, 0.00001f);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = originalTimeScale;
            rig.Dispose();
            yield return null;
        }

        private CameraSystemController CreateLateUpdateController()
        {
            VehicleProfile profile = rig.CreateProfile(1f, 3f, 3f);
            VehicleCameraTarget target = rig.CreateTarget(rig.CreateBody("Truck"), profile);
            Camera camera = rig.CreateCamera("Camera");
            CameraSystemController controller = rig.CreateController("Controller", camera, target, CameraTestRig.PresentationView);
            controller.SetUpdateMode(CameraUpdateMode.LateUpdate);
            return controller;
        }
    }
}
