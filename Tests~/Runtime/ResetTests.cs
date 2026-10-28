using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Gley.CameraSystem.Tests.PlayMode
{
    public class ResetTests
    {
        private const string DrivingView = "Driving";
        private const float FrameTime = 0.02f;
        private const float PositionTolerance = 0.001f;
        private const float OffsetTolerance = 0.0001f;
        private const float MovementThreshold = 0.01f;
        private const float TurnRate = 30f;
        private const int SettleFrames = 100;
        private const int TurnSettleFrames = 150;
        private const int TurnDecayFrames = 200;
        private const int MaximumFrames = 1000;

        private readonly List<int> endedCommandIds = new List<int>();
        private readonly List<CommandEndResult> endedResults = new List<CommandEndResult>();

        private CameraTestRig rig;
        private CameraSystemController controller;
        private VehicleProfile profile;
        private VehicleViewEntry drivingView;
        private CameraViewPreset drivingPreset;
        private VehicleCameraTarget target;
        private Camera camera;
        private Transform root;

        [SetUp]
        public void SetUp()
        {
            rig = new CameraTestRig();
            endedCommandIds.Clear();
            endedResults.Clear();
            controller = null;
            root = rig.CreateBody("Truck");
            profile = rig.CreateProfile(1f, 3f, 3f);
            VehicleOrbit orbit;
            Assert.IsTrue(profile.TryGetOrbit(CameraTestRig.OrbitName, out orbit));
            drivingPreset = rig.CreatePreset(CameraViewType.Driving);
            drivingView = profile.AddView(DrivingView, drivingPreset, orbit.Id);
            target = rig.CreateTarget(root, profile);
            target.SetSuppliedSpeed(0f);
            camera = rig.CreateCamera("Camera");
        }

        [Test]
        public void OrbitReset_Driving_ReturnsToRearDefault_ZoomHeightKept()
        {
            Activate(DrivingView);
            AssertPosition(new Vector3(0f, 0f, -3f));
            MoveToRightSide();
            AssertPosition(new Vector3(2f, 1f, -2f));

            int commandId;
            Assert.AreEqual(CameraCommandResult.Accepted, controller.OrbitReset(CommandSource.Player, out commandId));

            Assert.AreNotEqual(0, commandId);
            RunFrames(5);
            Assert.Greater(Vector3.Distance(new Vector3(2f, 1f, -2f), camera.transform.position), MovementThreshold);
            Assert.Greater(Vector3.Distance(new Vector3(0f, 1f, -4f), camera.transform.position), MovementThreshold);
            RunUntilEnded(commandId);
            AssertEndedWith(commandId, CommandEndResult.Completed);
            RunFrames(SettleFrames);
            AssertPosition(new Vector3(0f, 1f, -4f));
            Assert.AreEqual(-1f, controller.ZoomOffset, OffsetTolerance);
            Assert.AreEqual(1f, controller.HeightOffset, OffsetTolerance);
        }

        [Test]
        public void FullReset_ResetsZoomAndHeightToo()
        {
            drivingView.ConfigureDefaultPose(new OrbitPose(0f, -0.5f, 0.5f));
            Activate(DrivingView);
            AssertPosition(new Vector3(0f, 0.5f, -3.5f));
            MoveToRightSide();
            AssertPosition(new Vector3(2.5f, 1.5f, -2f));

            ResetAndArrive(false);
            AssertPosition(new Vector3(0f, 1.5f, -4.5f));
            Assert.AreEqual(-1.5f, controller.ZoomOffset, OffsetTolerance);
            Assert.AreEqual(1.5f, controller.HeightOffset, OffsetTolerance);

            MoveToRightSide();
            AssertPosition(new Vector3(3.5f, 2.5f, -2f));
            ResetAndArrive(true);
            AssertPosition(new Vector3(0f, 0.5f, -3.5f));
            Assert.AreEqual(-0.5f, controller.ZoomOffset, OffsetTolerance);
            Assert.AreEqual(0.5f, controller.HeightOffset, OffsetTolerance);
        }

        [Test]
        public void OrbitReset_DuringReverse_GoesToFrontDefault()
        {
            drivingView.ConfigureFrontDefaultPose(new OrbitPose(180f, -2f, 1f));
            Activate(DrivingView);
            EnterReverseAndArrive();
            AssertPosition(new Vector3(0f, 1f, 5f));

            Assert.AreEqual(CameraCommandResult.Accepted, controller.AddDrag(new Vector2(0.1f, 0f)));
            Assert.AreEqual(CameraCommandResult.Accepted, controller.AddPinch(0.2f));
            RunFrames(SettleFrames);
            AssertPosition(new Vector3(-2f, 1f, 2f));

            ResetAndArrive(false);
            AssertPosition(new Vector3(0f, 1f, 4f));
            Assert.AreEqual(-1f, controller.ZoomOffset, OffsetTolerance);
            Assert.AreEqual(1f, controller.HeightOffset, OffsetTolerance);
            Assert.IsTrue(controller.IsReverseActive);
        }

        [Test]
        public void SaveRearDefault_ThenReset_ReturnsToSavedPose()
        {
            Activate(DrivingView);
            MoveToRightSide();
            AssertPosition(new Vector3(2f, 1f, -2f));
            Assert.AreEqual(CameraCommandResult.Accepted, controller.SaveCurrentPoseAsDefault(DefaultSlot.Rear));

            Assert.AreEqual(CameraCommandResult.Accepted, controller.AddDrag(new Vector2(-0.2f, -0.25f)));
            Assert.AreEqual(CameraCommandResult.Accepted, controller.AddPinch(0.2f));
            RunFrames(SettleFrames);
            AssertPosition(new Vector3(-1f, 0f, -2f));

            ResetAndArrive(true);
            AssertPosition(new Vector3(2f, 1f, -2f));
            Assert.AreEqual(-1f, controller.ZoomOffset, OffsetTolerance);
            Assert.AreEqual(1f, controller.HeightOffset, OffsetTolerance);
        }

        [Test]
        public void FrontAndRearDefaults_IndependentPositionZoomHeight()
        {
            Activate(DrivingView);
            MoveToRightSide();
            Assert.AreEqual(CameraCommandResult.Accepted, controller.SaveCurrentPoseAsDefault(DefaultSlot.Rear));

            EnterReverseAndArrive();
            AssertPosition(new Vector3(0f, 0f, 3f));
            Assert.AreEqual(CameraCommandResult.Accepted, controller.AddDrag(new Vector2(0.1f, 0.5f)));
            Assert.AreEqual(CameraCommandResult.Accepted, controller.AddPinch(-0.4f));
            RunFrames(SettleFrames);
            AssertPosition(new Vector3(-3f, 2f, 2f));
            Assert.AreEqual(CameraCommandResult.Accepted, controller.SaveCurrentPoseAsDefault(DefaultSlot.Front));

            int commandId;
            Assert.AreEqual(CameraCommandResult.Accepted, controller.SetReverse(false, out commandId));
            Assert.AreEqual(0, commandId);
            ResetAndArrive(true);
            AssertPosition(new Vector3(2f, 1f, -2f));
            Assert.AreEqual(-1f, controller.ZoomOffset, OffsetTolerance);
            Assert.AreEqual(1f, controller.HeightOffset, OffsetTolerance);

            EnterReverseAndArrive();
            AssertPosition(new Vector3(-3f, 2f, 2f));
            Assert.AreEqual(-2f, controller.ZoomOffset, OffsetTolerance);
            Assert.AreEqual(2f, controller.HeightOffset, OffsetTolerance);
        }

        [Test]
        public void SavedDefault_AfterTrailerAttach_StillBehindWholeChain()
        {
            Activate(DrivingView);
            Assert.AreEqual(CameraCommandResult.Accepted, controller.AddDrag(new Vector2(0f, 0.25f)));
            Assert.AreEqual(CameraCommandResult.Accepted, controller.AddPinch(-0.4f));
            RunFrames(SettleFrames);
            AssertPosition(new Vector3(0f, 1f, -5f));
            Assert.AreEqual(CameraCommandResult.Accepted, controller.SaveCurrentPoseAsDefault(DefaultSlot.Rear));
            Assert.AreEqual(CameraCommandResult.Accepted, controller.AddDrag(new Vector2(0.1f, 0f)));
            Assert.AreEqual(CameraCommandResult.Accepted, controller.AddPinch(0.2f));
            RunFrames(SettleFrames);
            AssertPosition(new Vector3(2f, 1f, -2f));

            Transform trailer = rig.CreateBody("Trailer");
            trailer.position = new Vector3(0f, 0f, -7f);
            VehicleProfile trailerProfile = rig.CreateProfile(1f, 3f, 3f);
            Assert.AreEqual(VehicleTargetResult.Accepted, target.Attach(trailer, trailerProfile, ChainEnd.Rear));
            RunFrames(SettleFrames);
            AssertPosition(new Vector3(2f, 1f, -2f));

            ResetAndArrive(true);
            AssertPosition(new Vector3(0f, 1f, -12f));
            Assert.Less(camera.transform.position.z, -10f);
            Assert.AreEqual(-2f, controller.ZoomOffset, OffsetTolerance);
            Assert.AreEqual(1f, controller.HeightOffset, OffsetTolerance);
        }

        [Test]
        public void Reset_ResumesTurnLook()
        {
            drivingPreset.Driving.Configure(0f, 0f, 3f, true, 5f, 25f, 3f, 0.25f, false, true, 12f, TurnRate, 0.4f, 0.5f);
            Activate(DrivingView);
            RunTurningFrames(TurnSettleFrames);
            Assert.Less(controller.TurnLookOffset, -11.5f);

            Assert.AreEqual(CameraCommandResult.Accepted, controller.SetHeldIntent(0f, 1f, 0f));
            RunTurningFrames(5);
            controller.SetHeldIntent(0f, 0f, 0f);
            RunTurningFrames(TurnDecayFrames);
            Assert.Less(Mathf.Abs(controller.TurnLookOffset), 0.05f);

            int commandId;
            Assert.AreEqual(CameraCommandResult.Accepted, controller.OrbitReset(CommandSource.Player, out commandId));
            RunTurningUntilEnded(commandId);
            AssertEndedWith(commandId, CommandEndResult.Completed);
            RunTurningFrames(TurnSettleFrames);

            Assert.Less(controller.TurnLookOffset, -11.5f);
        }

        [Test]
        public void PlayerReset_DuringExplicitLock_PlayerControlLocked()
        {
            Activate(DrivingView);
            MoveToRightSide();
            controller.SetPlayerControlLocked(true);

            int commandId;
            Assert.AreEqual(CameraCommandResult.PlayerControlLocked, controller.OrbitReset(CommandSource.Player, out commandId));
            Assert.AreEqual(0, commandId);
            Assert.AreEqual(CameraCommandResult.PlayerControlLocked, controller.FullReset(CommandSource.Player, out commandId));
            Assert.AreEqual(0, commandId);
            RunFrames(SettleFrames);
            AssertPosition(new Vector3(2f, 1f, -2f));
            Assert.AreEqual(0, endedCommandIds.Count);

            Assert.AreEqual(CameraCommandResult.Accepted, controller.OrbitReset(CommandSource.Game, out commandId));
            Assert.AreNotEqual(0, commandId);
            RunUntilEnded(commandId);
            AssertEndedWith(commandId, CommandEndResult.Completed);
            RunFrames(SettleFrames);
            AssertPosition(new Vector3(0f, 1f, -4f));
            Assert.IsTrue(controller.IsPlayerControlLocked);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (controller != null)
            {
                controller.CommandEnded -= RecordCommandEnded;
            }

            rig.Dispose();
            yield return null;
        }

        private void Activate(string viewName)
        {
            controller = rig.CreateController("Controller", camera, target, viewName);
            controller.CommandEnded += RecordCommandEnded;
            Assert.AreEqual(CameraCommandResult.Accepted, controller.Activate());
        }

        private void MoveToRightSide()
        {
            Assert.AreEqual(CameraCommandResult.Accepted, controller.AddDrag(new Vector2(0.1f, 0.25f)));
            Assert.AreEqual(CameraCommandResult.Accepted, controller.AddPinch(-0.2f));
            RunFrames(SettleFrames);
        }

        private void ResetAndArrive(bool isFullReset)
        {
            int commandId;
            CameraCommandResult result;
            if (isFullReset)
            {
                result = controller.FullReset(CommandSource.Player, out commandId);
            }
            else
            {
                result = controller.OrbitReset(CommandSource.Player, out commandId);
            }

            Assert.AreEqual(CameraCommandResult.Accepted, result);
            Assert.AreNotEqual(0, commandId);
            RunUntilEnded(commandId);
            AssertEndedWith(commandId, CommandEndResult.Completed);
            RunFrames(SettleFrames);
        }

        private void EnterReverseAndArrive()
        {
            int commandId;
            Assert.AreEqual(CameraCommandResult.Accepted, controller.SetReverse(true, out commandId));
            Assert.AreNotEqual(0, commandId);
            RunUntilEnded(commandId);
            AssertEndedWith(commandId, CommandEndResult.Completed);
            RunFrames(SettleFrames);
        }

        private void RunFrames(int count)
        {
            for (int frame = 0; frame < count; frame++)
            {
                controller.UpdateCameraFrame(FrameTime);
            }
        }

        private void RunUntilEnded(int commandId)
        {
            for (int frame = 0; frame < MaximumFrames && !endedCommandIds.Contains(commandId); frame++)
            {
                controller.UpdateCameraFrame(FrameTime);
            }

            Assert.IsTrue(endedCommandIds.Contains(commandId), $"Command {commandId} did not end.");
        }

        private void RunTurningFrames(int count)
        {
            for (int frame = 0; frame < count; frame++)
            {
                TurnAndUpdate();
            }
        }

        private void RunTurningUntilEnded(int commandId)
        {
            for (int frame = 0; frame < MaximumFrames && !endedCommandIds.Contains(commandId); frame++)
            {
                TurnAndUpdate();
            }

            Assert.IsTrue(endedCommandIds.Contains(commandId), $"Command {commandId} did not end.");
        }

        private void TurnAndUpdate()
        {
            root.rotation = Quaternion.Euler(0f, TurnRate * FrameTime, 0f) * root.rotation;
            controller.UpdateCameraFrame(FrameTime);
        }

        private void RecordCommandEnded(int commandId, CommandEndResult result)
        {
            endedCommandIds.Add(commandId);
            endedResults.Add(result);
        }

        private void AssertEndedWith(int commandId, CommandEndResult result)
        {
            int index = endedCommandIds.IndexOf(commandId);
            Assert.GreaterOrEqual(index, 0, $"Command {commandId} did not end.");
            Assert.AreEqual(result, endedResults[index]);
        }

        private void AssertPosition(Vector3 expected)
        {
            Vector3 actual = camera.transform.position;
            Assert.Less(Vector3.Distance(expected, actual), PositionTolerance, $"Expected {expected:F4}, got {actual:F4}");
        }
    }
}
