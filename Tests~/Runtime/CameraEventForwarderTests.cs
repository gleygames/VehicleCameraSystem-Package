using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Gley.CameraSystem.Tests.PlayMode
{
    public class CameraEventForwarderTests
    {
        private CameraTestRig rig;
        private CommandEndResult directResult;
        private CommandEndResult forwardedResult;
        private int directCommandId;
        private int directCount;
        private int forwardedCommandId;
        private int forwardedCount;

        [SetUp]
        public void SetUp()
        {
            rig = new CameraTestRig();
            directResult = CommandEndResult.Completed;
            forwardedResult = CommandEndResult.Completed;
            directCommandId = 0;
            directCount = 0;
            forwardedCommandId = 0;
            forwardedCount = 0;
        }

        [Test]
        public void CommandEnded_InvokesUnityEventWithSameArguments()
        {
            CameraSystemController controller = CreateController();
            CameraEventForwarder forwarder = controller.gameObject.AddComponent<CameraEventForwarder>();
            forwarder.SetController(controller);
            forwarder.CommandEnded.AddListener(HandleForwardedCommandEnded);
            controller.CommandEnded += HandleDirectCommandEnded;
            int commandId;

            Assert.AreEqual(CameraCommandResult.Accepted, controller.Activate(new TransitionOptions(TransitionMode.Duration, 2f), out commandId));
            controller.UpdateCameraFrame(0.2f);
            controller.Deactivate();

            Assert.AreEqual(1, directCount);
            Assert.AreEqual(1, forwardedCount);
            Assert.AreEqual(commandId, forwardedCommandId);
            Assert.AreEqual(directCommandId, forwardedCommandId);
            Assert.AreEqual(CommandEndResult.Interrupted, forwardedResult);
            Assert.AreEqual(directResult, forwardedResult);
        }

        [Test]
        public void DisabledForwarder_DoesNotInvoke()
        {
            CameraSystemController controller = CreateController();
            CameraEventForwarder forwarder = controller.gameObject.AddComponent<CameraEventForwarder>();
            forwarder.SetController(controller);
            forwarder.CommandEnded.AddListener(HandleForwardedCommandEnded);
            controller.CommandEnded += HandleDirectCommandEnded;
            forwarder.enabled = false;
            int commandId;

            Assert.AreEqual(CameraCommandResult.Accepted, controller.Activate(new TransitionOptions(TransitionMode.Duration, 2f), out commandId));
            controller.UpdateCameraFrame(0.2f);
            controller.Deactivate();

            Assert.AreEqual(1, directCount);
            Assert.AreEqual(0, forwardedCount);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            rig.Dispose();
            yield return null;
        }

        private CameraSystemController CreateController()
        {
            VehicleProfile profile = rig.CreateProfile(1f, 3f, 3f);
            VehicleCameraTarget target = rig.CreateTarget(rig.CreateBody("Truck"), profile);
            Camera camera = rig.CreateCamera("Camera");
            camera.transform.position = new Vector3(30f, 10f, 20f);
            return rig.CreateController("Controller", camera, target, CameraTestRig.FixedView);
        }

        private void HandleForwardedCommandEnded(int commandId, CommandEndResult result)
        {
            forwardedCommandId = commandId;
            forwardedResult = result;
            forwardedCount++;
        }

        private void HandleDirectCommandEnded(int commandId, CommandEndResult result)
        {
            directCommandId = commandId;
            directResult = result;
            directCount++;
        }
    }
}
