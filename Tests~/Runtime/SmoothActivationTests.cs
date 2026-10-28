using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Gley.CameraSystem.Tests.PlayMode
{
    public class SmoothActivationTests
    {
        private CameraTestRig rig;
        private int endedCommandId;
        private int endedCount;
        private CommandEndResult endedResult;

        [SetUp]
        public void SetUp()
        {
            rig = new CameraTestRig();
            endedCommandId = 0;
            endedCount = 0;
            endedResult = CommandEndResult.Completed;
        }

        [Test]
        public void Activate_PresetSpeed_CameraMovesFromCurrentPoseInStraightLine()
        {
            Transform root = rig.CreateBody("Truck");
            VehicleProfile profile = rig.CreateProfile(1f, 3f, 3f);
            VehicleCameraTarget target = rig.CreateTarget(root, profile);
            Camera camera = rig.CreateCamera("Camera");
            Vector3 startPosition = new Vector3(20f, 12f, 22f);
            camera.transform.position = startPosition;
            CameraSystemController controller = rig.CreateController("Controller", camera, target, CameraTestRig.FixedView);
            int commandId;

            Assert.AreEqual(CameraCommandResult.Accepted, controller.Activate(new TransitionOptions(TransitionMode.PresetSpeed), out commandId));
            Assert.Greater(commandId, 0);
            AssertPosition(startPosition, camera.transform.position);
            controller.UpdateCameraFrame(0.2f);

            Vector3 destination = root.TransformPoint(profile.FixedViewPose.CameraLocalPosition);
            Vector3 segment = destination - startPosition;
            Vector3 relativePosition = camera.transform.position - startPosition;
            float progress = Vector3.Dot(relativePosition, segment) / segment.sqrMagnitude;
            Vector3 closestPoint = startPosition + segment * progress;
            Assert.Greater(progress, 0f);
            Assert.Less(progress, 1f);
            Assert.Less(Vector3.Distance(camera.transform.position, closestPoint), 0.01f);

            controller.Deactivate();
            camera.transform.position = startPosition;
            Assert.AreEqual(CameraCommandResult.Accepted, controller.Activate(new TransitionOptions(TransitionMode.Speed, 5f), out commandId));
            controller.UpdateCameraFrame(0.2f);
            Assert.AreEqual(0.25f, Vector3.Distance(startPosition, camera.transform.position), 0.001f);
        }

        [Test]
        public void Activate_Duration_CompletesAtDuration_RaisesCompleted()
        {
            Transform root = rig.CreateBody("Truck");
            VehicleProfile profile = rig.CreateProfile(1f, 3f, 3f);
            VehicleCameraTarget target = rig.CreateTarget(root, profile);
            Camera camera = rig.CreateCamera("Camera");
            Vector3 startPosition = new Vector3(30f, 10f, 20f);
            camera.transform.position = startPosition;
            CameraSystemController controller = rig.CreateController("Controller", camera, target, CameraTestRig.FixedView);
            controller.CommandEnded += HandleCommandEnded;
            Vector3 destination = root.TransformPoint(profile.FixedViewPose.CameraLocalPosition);
            int commandId;

            Assert.AreEqual(CameraCommandResult.Accepted, controller.Activate(new TransitionOptions(TransitionMode.Duration, 2f), out commandId));
            for (int frame = 0; frame < 50; frame++)
            {
                controller.UpdateCameraFrame(0.02f);
            }

            Assert.AreEqual(0, endedCount);
            Assert.Less(Vector3.Distance((startPosition + destination) * 0.5f, camera.transform.position), 0.01f);
            for (int frame = 50; frame < 100; frame++)
            {
                controller.UpdateCameraFrame(0.02f);
            }

            Assert.AreEqual(1, endedCount);
            Assert.AreEqual(commandId, endedCommandId);
            Assert.AreEqual(CommandEndResult.Completed, endedResult);
            AssertPosition(destination, camera.transform.position);
        }

        [Test]
        public void Activate_Snap_ArrivesSameFrame()
        {
            Transform root = rig.CreateBody("Truck");
            VehicleProfile profile = rig.CreateProfile(1f, 3f, 3f);
            VehicleCameraTarget target = rig.CreateTarget(root, profile);
            Camera camera = rig.CreateCamera("Camera");
            camera.transform.position = new Vector3(30f, 10f, 20f);
            CameraSystemController controller = rig.CreateController("Controller", camera, target, CameraTestRig.FixedView);

            Assert.AreEqual(CameraCommandResult.Accepted, controller.Activate());
            AssertPosition(root.TransformPoint(profile.FixedViewPose.CameraLocalPosition), camera.transform.position);
        }

        [Test]
        public void Deactivate_DuringTransition_RaisesInterrupted()
        {
            VehicleProfile profile = rig.CreateProfile(1f, 3f, 3f);
            VehicleCameraTarget target = rig.CreateTarget(rig.CreateBody("Truck"), profile);
            Camera camera = rig.CreateCamera("Camera");
            camera.transform.position = new Vector3(30f, 10f, 20f);
            CameraSystemController controller = rig.CreateController("Controller", camera, target, CameraTestRig.FixedView);
            controller.CommandEnded += HandleCommandEnded;
            int commandId;

            Assert.AreEqual(CameraCommandResult.Accepted, controller.Activate(new TransitionOptions(TransitionMode.Duration, 2f), out commandId));
            controller.UpdateCameraFrame(0.2f);
            controller.Deactivate();

            Assert.AreEqual(1, endedCount);
            Assert.AreEqual(commandId, endedCommandId);
            Assert.AreEqual(CommandEndResult.Interrupted, endedResult);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            rig.Dispose();
            yield return null;
        }

        private void HandleCommandEnded(int commandId, CommandEndResult result)
        {
            endedCommandId = commandId;
            endedResult = result;
            endedCount++;
        }

        private void AssertPosition(Vector3 expected, Vector3 actual)
        {
            Assert.Less(Vector3.Distance(expected, actual), 0.001f, $"Expected {expected:F4}, got {actual:F4}");
        }
    }
}
