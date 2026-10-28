using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Gley.CameraSystem.Tests.PlayMode
{
    public class RecenterTests
    {
        private const string DrivingView = "Driving";
        private const float FrameTime = 0.02f;
        private const float OrbitLength = 16f;
        private const float DistanceTolerance = 0.001f;
        private const float MovementThreshold = 0.01f;
        private const float MovingSpeed = 10f;
        private const float RecenterDelay = 4f;
        private const int JustBeforeDelayFrames = 190;
        private const int JustAfterDelayFrames = 20;
        private const int SettleFrames = 300;
        private const int MaximumFrames = 1000;

        private readonly List<int> endedCommandIds = new List<int>();
        private readonly List<CommandEndResult> endedResults = new List<CommandEndResult>();

        private CameraTestRig rig;
        private CameraSystemController controller;
        private CameraViewPreset drivingPreset;
        private VehicleCameraTarget target;
        private Camera camera;
        private float restDistance;
        private float originalTimeScale;

        [SetUp]
        public void SetUp()
        {
            originalTimeScale = Time.timeScale;
            rig = new CameraTestRig();
            endedCommandIds.Clear();
            endedResults.Clear();
            controller = null;
            Transform root = rig.CreateBody("Truck");
            VehicleProfile profile = rig.CreateProfile(1f, 3f, 3f);
            VehicleOrbit orbit;
            Assert.IsTrue(profile.TryGetOrbit(CameraTestRig.OrbitName, out orbit));
            drivingPreset = rig.CreatePreset(CameraViewType.Driving);
            drivingPreset.Driving.Configure(0.12f, 0.08f, 3f, false, 5f, 25f, 3f, 0.25f, false, false, 12f, 30f, 0.4f, 0.5f);
            drivingPreset.Recenter.Configure(RecenterMode.Timed, RecenterDelay, 0.25f, 0.5f);
            profile.AddView(DrivingView, drivingPreset, orbit.Id);
            target = rig.CreateTarget(root, profile);
            target.SetSuppliedSpeed(MovingSpeed);
            camera = rig.CreateCamera("Camera");
        }

        [Test]
        public void Timed_MovingNoInput_RecentersAfterDelay()
        {
            Activate();
            float movedDistance = MoveToSide();

            RunFrames(JustBeforeDelayFrames);
            AssertAtDistance(movedDistance);
            Assert.AreEqual(0, endedCommandIds.Count);

            RunFrames(JustAfterDelayFrames);
            Assert.Greater(WrappedDifference(controller.OrbitDistance, movedDistance), MovementThreshold);
            RunUntilEndedCount(1);
            Assert.AreEqual(CommandEndResult.Completed, endedResults[0]);
            AssertAtDistance(restDistance);

            RunFrames(SettleFrames);
            Assert.AreEqual(1, endedCommandIds.Count);
            AssertAtDistance(restDistance);
        }

        [Test]
        public void Timed_StopDuringCountdown_NoRecenter_RestartsFullDelayWhenMoving()
        {
            Activate();
            float movedDistance = MoveToSide();
            RunFrames(100);

            target.SetSuppliedSpeed(0f);
            RunFrames(500);
            AssertAtDistance(movedDistance);
            Assert.AreEqual(0, endedCommandIds.Count);

            target.SetSuppliedSpeed(MovingSpeed);
            RunFrames(JustBeforeDelayFrames);
            AssertAtDistance(movedDistance);
            Assert.AreEqual(0, endedCommandIds.Count);

            RunFrames(JustAfterDelayFrames);
            Assert.Greater(WrappedDifference(controller.OrbitDistance, movedDistance), MovementThreshold);
            RunUntilEndedCount(1);
            Assert.AreEqual(CommandEndResult.Completed, endedResults[0]);
            AssertAtDistance(restDistance);
        }

        [Test]
        public void Timed_InputResetsCountdown()
        {
            Activate();
            float movedDistance = MoveToSide();
            RunFrames(150);

            Assert.AreEqual(CameraCommandResult.Accepted, controller.SetHeldIntent(0f, 1f, 0f));
            RunFrames(50);
            controller.SetHeldIntent(0f, 0f, 0f);
            AssertAtDistance(movedDistance);
            Assert.AreEqual(0, endedCommandIds.Count);
            Assert.AreEqual(2f, controller.HeightOffset, DistanceTolerance);

            RunFrames(JustBeforeDelayFrames);
            AssertAtDistance(movedDistance);
            Assert.AreEqual(0, endedCommandIds.Count);

            RunFrames(JustAfterDelayFrames);
            Assert.Greater(WrappedDifference(controller.OrbitDistance, movedDistance), MovementThreshold);
            RunUntilEndedCount(1);
            Assert.AreEqual(CommandEndResult.Completed, endedResults[0]);
            AssertAtDistance(restDistance);
            Assert.AreEqual(2f, controller.HeightOffset, DistanceTolerance);
        }

        [Test]
        public void Timed_GamePaused_CountdownHolds()
        {
            Activate();
            float movedDistance = MoveToSide();
            RunFrames(100);

            Time.timeScale = 0f;
            RunFrames(500);
            AssertAtDistance(movedDistance);
            Assert.AreEqual(0, endedCommandIds.Count);

            Time.timeScale = 1f;
            RunFrames(90);
            AssertAtDistance(movedDistance);
            Assert.AreEqual(0, endedCommandIds.Count);

            RunFrames(JustAfterDelayFrames);
            Assert.Greater(WrappedDifference(controller.OrbitDistance, movedDistance), MovementThreshold);
            RunUntilEndedCount(1);
            Assert.AreEqual(CommandEndResult.Completed, endedResults[0]);
            AssertAtDistance(restDistance);
        }

        [Test]
        public void Persistent_NeverRecenters()
        {
            drivingPreset.Recenter.Configure(RecenterMode.Persistent, RecenterDelay, 0.25f, 0.5f);
            Activate();
            float movedDistance = MoveToSide();

            RunFrames(MaximumFrames);

            AssertAtDistance(movedDistance);
            Assert.AreEqual(0, endedCommandIds.Count);
        }

        [Test]
        public void ZeroDelay_SpringsBackOnRelease()
        {
            drivingPreset.Recenter.Configure(RecenterMode.Timed, 0f, 0.25f, 0.5f);
            Activate();

            Assert.AreEqual(CameraCommandResult.Accepted, controller.SetHeldIntent(1f, 0f, 0f));
            float previousDistance = controller.OrbitDistance;
            for (int frame = 0; frame < 25; frame++)
            {
                controller.UpdateCameraFrame(FrameTime);
                Assert.Greater(WrappedDifference(controller.OrbitDistance, restDistance), WrappedDifference(previousDistance, restDistance));
                previousDistance = controller.OrbitDistance;
            }

            Assert.Greater(WrappedDifference(controller.OrbitDistance, restDistance), 1f);
            Assert.AreEqual(0, endedCommandIds.Count);

            controller.SetHeldIntent(0f, 0f, 0f);
            float releasedDistance = controller.OrbitDistance;
            RunFrames(5);
            Assert.Greater(WrappedDifference(controller.OrbitDistance, releasedDistance), MovementThreshold);
            Assert.Less(WrappedDifference(controller.OrbitDistance, restDistance), WrappedDifference(releasedDistance, restDistance));
            RunUntilEndedCount(1);
            Assert.AreEqual(CommandEndResult.Completed, endedResults[0]);
            AssertAtDistance(restDistance);

            RunFrames(SettleFrames);
            Assert.AreEqual(1, endedCommandIds.Count);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = originalTimeScale;
            if (controller != null)
            {
                controller.CommandEnded -= RecordCommandEnded;
            }

            rig.Dispose();
            yield return null;
        }

        private void Activate()
        {
            controller = rig.CreateController("Controller", camera, target, DrivingView);
            controller.CommandEnded += RecordCommandEnded;
            Assert.AreEqual(CameraCommandResult.Accepted, controller.Activate());
            restDistance = controller.OrbitDistance;
        }

        private float MoveToSide()
        {
            Assert.AreEqual(CameraCommandResult.Accepted, controller.AddDrag(new Vector2(0.1f, 0f)));
            float movedDistance = controller.OrbitDistance;
            Assert.AreEqual(2f, WrappedDifference(movedDistance, restDistance), DistanceTolerance);
            return movedDistance;
        }

        private void RunFrames(int count)
        {
            for (int frame = 0; frame < count; frame++)
            {
                controller.UpdateCameraFrame(FrameTime);
            }
        }

        private void RunUntilEndedCount(int count)
        {
            for (int frame = 0; frame < MaximumFrames && endedCommandIds.Count < count; frame++)
            {
                controller.UpdateCameraFrame(FrameTime);
            }

            Assert.AreEqual(count, endedCommandIds.Count);
        }

        private void RecordCommandEnded(int commandId, CommandEndResult result)
        {
            endedCommandIds.Add(commandId);
            endedResults.Add(result);
        }

        private float WrappedDifference(float first, float second)
        {
            float difference = Mathf.Repeat(first - second, OrbitLength);
            return Mathf.Min(difference, OrbitLength - difference);
        }

        private void AssertAtDistance(float expected)
        {
            Assert.Less(WrappedDifference(controller.OrbitDistance, expected), DistanceTolerance, $"Expected orbit distance {expected:F4}, got {controller.OrbitDistance:F4}");
        }
    }
}
