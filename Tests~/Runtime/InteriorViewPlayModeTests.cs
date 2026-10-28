using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Gley.CameraSystem.Tests.PlayMode
{
    public class InteriorViewPlayModeTests
    {
        private const float FrameTime = 0.02f;
        private const float AngleTolerance = 0.01f;
        private const float PositionTolerance = 0.001f;
        private const float DirectionTolerance = 0.0005f;
        private const float MovingSpeed = 10f;
        private const float SettledTurnLook = 9.5f;
        private const int TurnSettleFrames = 150;
        private const int JustBeforeDelayFrames = 90;
        private const int MaximumFrames = 1000;

        private readonly List<int> endedCommandIds = new List<int>();
        private readonly List<CommandEndResult> endedResults = new List<CommandEndResult>();

        private CameraTestRig rig;
        private CameraSystemController controller;
        private VehicleProfile profile;
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
            root.SetPositionAndRotation(new Vector3(5f, 1f, -3f), Quaternion.Euler(0f, 40f, 0f));
            profile = rig.CreateProfile(1f, 3f, 3f);
            target = rig.CreateTarget(root, profile);
            target.SetSuppliedSpeed(0f);
            camera = rig.CreateCamera("Camera");
        }

        [Test]
        public void Activate_EyeAtSeatPosition_LookingForward()
        {
            Activate(CameraTestRig.InteriorView);

            Vector3 expectedEye = root.position + root.rotation * new Vector3(-0.5f, 1.8f, 2f);
            AssertPosition(expectedEye, camera.transform.position);
            AssertLook(0f, 0f);
            Assert.AreEqual(0f, controller.HeadYaw, AngleTolerance);
            Assert.AreEqual(0f, controller.HeadPitch, AngleTolerance);

            RunFrames(20);
            AssertPosition(expectedEye, camera.transform.position);
            AssertLook(0f, 0f);
        }

        [Test]
        public void HeldInput_TurnsHeadAtLookRate()
        {
            Activate(CameraTestRig.InteriorView);

            Assert.AreEqual(CameraCommandResult.Accepted, controller.SetHeldIntent(1f, 0f, 0f));
            RunFrames(25);
            Assert.AreEqual(60f, controller.HeadYaw, AngleTolerance);
            Assert.AreEqual(0f, controller.HeadPitch, AngleTolerance);
            AssertLook(60f, 0f);

            controller.SetHeldIntent(0f, 1f, 0f);
            RunFrames(10);
            Assert.AreEqual(60f, controller.HeadYaw, AngleTolerance);
            Assert.AreEqual(24f, controller.HeadPitch, AngleTolerance);
            AssertLook(60f, 24f);

            controller.SetHeldIntent(0f, 0f, 1f);
            RunFrames(10);
            Assert.AreEqual(60f, controller.HeadYaw, AngleTolerance);
            Assert.AreEqual(24f, controller.HeadPitch, AngleTolerance);
            AssertPosition(root.position + root.rotation * new Vector3(-0.5f, 1.8f, 2f), camera.transform.position);

            controller.SetSensitivity(1f, 2f);
            controller.SetHeldIntent(-1f, 0f, 0f);
            RunFrames(5);
            Assert.AreEqual(36f, controller.HeadYaw, AngleTolerance);
            AssertLook(36f, 24f);
        }

        [Test]
        public void ManualLook_SuspendsTurnLook_UntilRecenterToNeutral()
        {
            target.SetSuppliedSpeed(MovingSpeed);
            target.SetTurnHint(1f);
            Activate(CameraTestRig.InteriorView);

            RunFrames(TurnSettleFrames);
            float settledOffset = controller.TurnLookOffset;
            Assert.Greater(settledOffset, SettledTurnLook);
            Assert.Less(settledOffset, 10f + AngleTolerance);
            AssertLook(settledOffset, 0f);

            Assert.AreEqual(CameraCommandResult.Accepted, controller.SetHeldIntent(-1f, 0f, 0f));
            RunFrames(5);
            controller.SetHeldIntent(0f, 0f, 0f);
            Assert.AreEqual(-12f, controller.HeadYaw, AngleTolerance);
            Assert.Less(controller.TurnLookOffset, settledOffset);

            RunFrames(JustBeforeDelayFrames);
            Assert.AreEqual(-12f, controller.HeadYaw, AngleTolerance);
            Assert.Less(controller.TurnLookOffset, 1f);
            Assert.AreEqual(0, endedCommandIds.Count);
            AssertLook(-12f + controller.TurnLookOffset, 0f);

            RunUntilEndedCount(1);
            Assert.AreEqual(CommandEndResult.Completed, endedResults[0]);
            Assert.AreEqual(0f, controller.HeadYaw, AngleTolerance);
            Assert.Less(controller.TurnLookOffset, 1f);

            RunFrames(TurnSettleFrames);
            Assert.Greater(controller.TurnLookOffset, SettledTurnLook);
            AssertLook(controller.TurnLookOffset, 0f);
            Assert.AreEqual(1, endedCommandIds.Count);
        }

        [Test]
        public void TimedRecenter_ReturnsHeadToNeutral_OnlyWhileMoving()
        {
            DisableInteriorTurnLook();
            Activate(CameraTestRig.InteriorView);
            Assert.AreEqual(CameraCommandResult.Accepted, controller.SetHeldIntent(1f, 1f, 0f));
            RunFrames(10);
            controller.SetHeldIntent(0f, 0f, 0f);
            Assert.AreEqual(24f, controller.HeadYaw, AngleTolerance);
            Assert.AreEqual(24f, controller.HeadPitch, AngleTolerance);

            RunFrames(300);
            Assert.AreEqual(24f, controller.HeadYaw, AngleTolerance);
            Assert.AreEqual(24f, controller.HeadPitch, AngleTolerance);
            Assert.AreEqual(0, endedCommandIds.Count);

            target.SetSuppliedSpeed(MovingSpeed);
            RunFrames(JustBeforeDelayFrames);
            Assert.AreEqual(24f, controller.HeadYaw, AngleTolerance);
            Assert.AreEqual(0, endedCommandIds.Count);

            target.SetSuppliedSpeed(0f);
            RunFrames(300);
            Assert.AreEqual(24f, controller.HeadYaw, AngleTolerance);
            Assert.AreEqual(0, endedCommandIds.Count);

            target.SetSuppliedSpeed(MovingSpeed);
            RunFrames(JustBeforeDelayFrames);
            Assert.AreEqual(24f, controller.HeadYaw, AngleTolerance);
            Assert.AreEqual(0, endedCommandIds.Count);

            RunFrames(15);
            Assert.Greater(controller.HeadYaw, AngleTolerance);
            Assert.Less(controller.HeadYaw, 24f - AngleTolerance);
            Assert.Greater(controller.HeadPitch, AngleTolerance);
            Assert.Less(controller.HeadPitch, 24f - AngleTolerance);

            RunUntilEndedCount(1);
            Assert.AreEqual(CommandEndResult.Completed, endedResults[0]);
            Assert.AreEqual(0f, controller.HeadYaw, AngleTolerance);
            Assert.AreEqual(0f, controller.HeadPitch, AngleTolerance);
            AssertLook(0f, 0f);
        }

        [Test]
        public void FullReset_HeadNeutral()
        {
            DisableInteriorTurnLook();
            Activate(CameraTestRig.InteriorView);
            LookLeftAndUp();

            int commandId;
            Assert.AreEqual(CameraCommandResult.Accepted, controller.FullReset(CommandSource.Player, out commandId));
            Assert.AreNotEqual(0, commandId);
            RunFrames(1);
            Assert.Less(controller.HeadYaw, -AngleTolerance);
            Assert.Greater(controller.HeadYaw, -30f + AngleTolerance);
            Assert.Greater(controller.HeadPitch, AngleTolerance);
            Assert.Less(controller.HeadPitch, 30f - AngleTolerance);

            RunUntilEndedCount(1);
            Assert.AreEqual(commandId, endedCommandIds[0]);
            Assert.AreEqual(CommandEndResult.Completed, endedResults[0]);
            Assert.AreEqual(0f, controller.HeadYaw, AngleTolerance);
            Assert.AreEqual(0f, controller.HeadPitch, AngleTolerance);
            AssertLook(0f, 0f);

            LookLeftAndUp();
            Assert.AreEqual(CameraCommandResult.Accepted, controller.OrbitReset(CommandSource.Player, out commandId));
            Assert.AreNotEqual(0, commandId);
            RunUntilEndedCount(2);
            Assert.AreEqual(CommandEndResult.Completed, endedResults[1]);
            Assert.AreEqual(0f, controller.HeadYaw, AngleTolerance);
            Assert.AreEqual(0f, controller.HeadPitch, AngleTolerance);
            AssertLook(0f, 0f);
        }

        [Test]
        public void ReverseRequest_InInterior_NoChange()
        {
            Activate(CameraTestRig.InteriorView);
            Assert.AreEqual(CameraCommandResult.Accepted, controller.SetHeldIntent(-1f, 0f, 0f));
            RunFrames(10);
            controller.SetHeldIntent(0f, 0f, 0f);
            Vector3 position = camera.transform.position;
            Quaternion rotation = camera.transform.rotation;

            int commandId;
            Assert.AreEqual(CameraCommandResult.Accepted, controller.SetReverse(true, out commandId));
            Assert.AreEqual(0, commandId);
            Assert.IsTrue(controller.IsReverseActive);
            RunFrames(50);
            Assert.AreEqual(CameraViewType.Interior, controller.ActivePreset.ViewType);
            Assert.AreEqual(-24f, controller.HeadYaw, AngleTolerance);
            AssertPosition(position, camera.transform.position);
            Assert.Less(Quaternion.Angle(rotation, camera.transform.rotation), AngleTolerance);

            Assert.AreEqual(CameraCommandResult.Accepted, controller.SetReverse(false, out commandId));
            Assert.AreEqual(0, commandId);
            RunFrames(50);
            Assert.AreEqual(-24f, controller.HeadYaw, AngleTolerance);
            AssertPosition(position, camera.transform.position);
            Assert.Less(Quaternion.Angle(rotation, camera.transform.rotation), AngleTolerance);
            Assert.AreEqual(0, endedCommandIds.Count);
        }

        [Test]
        public void TurnLook_NotInFixedOrPresentation()
        {
            EnableAllTurnLook(rig.FixedPreset);
            EnableAllTurnLook(rig.PresentationPreset);
            target.SetSuppliedSpeed(MovingSpeed);
            target.SetTurnHint(1f);
            Activate(CameraTestRig.FixedView);

            Vector3 position = camera.transform.position;
            Quaternion rotation = camera.transform.rotation;
            RunFrames(TurnSettleFrames);
            Assert.AreEqual(0f, controller.TurnLookOffset);
            AssertPosition(position, camera.transform.position);
            Assert.Less(Quaternion.Angle(rotation, camera.transform.rotation), AngleTolerance);

            Assert.AreEqual(CameraCommandResult.Accepted, controller.SelectView(CameraTestRig.PresentationView));
            position = camera.transform.position;
            rotation = camera.transform.rotation;
            RunFrames(TurnSettleFrames);
            Assert.AreEqual(0f, controller.TurnLookOffset);
            AssertPosition(position, camera.transform.position);
            Assert.Less(Quaternion.Angle(rotation, camera.transform.rotation), AngleTolerance);

            Assert.AreEqual(CameraCommandResult.Accepted, controller.SelectView(CameraTestRig.InteriorView));
            RunFrames(TurnSettleFrames);
            Assert.Greater(controller.TurnLookOffset, SettledTurnLook);
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

        private void DisableInteriorTurnLook()
        {
            rig.InteriorPreset.Interior.Configure(140f, 140f, 40f, 30f, 120f, 180f, false, 20f, 0.4f, 30f, 0.5f, true, true, true, 0.02f, 0.03f, 0.015f, 0.1f, 1.5f, 0.5f, 0.05f);
        }

        private void EnableAllTurnLook(CameraViewPreset preset)
        {
            preset.Driving.Configure(0.12f, 0.08f, 3f, false, 5f, 25f, 3f, 0.25f, false, true, 12f, 30f, 0.4f, 0.5f);
            preset.Interior.Configure(140f, 140f, 40f, 30f, 120f, 180f, true, 20f, 0.4f, 30f, 0.5f, true, true, true, 0.02f, 0.03f, 0.015f, 0.1f, 1.5f, 0.5f, 0.05f);
        }

        private void LookLeftAndUp()
        {
            Assert.AreEqual(CameraCommandResult.Accepted, controller.SetHeldIntent(-1f, 1f, 0f));
            RunFrames(12);
            controller.SetHeldIntent(0f, 0f, 0f);
            Assert.AreEqual(-28.8f, controller.HeadYaw, AngleTolerance);
            Assert.AreEqual(28.8f, controller.HeadPitch, AngleTolerance);
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

        private void AssertLook(float yaw, float pitch)
        {
            float yawRadians = yaw * Mathf.Deg2Rad;
            float pitchRadians = pitch * Mathf.Deg2Rad;
            Vector3 localForward = new Vector3(Mathf.Sin(yawRadians) * Mathf.Cos(pitchRadians), Mathf.Sin(pitchRadians), Mathf.Cos(yawRadians) * Mathf.Cos(pitchRadians));
            Vector3 localRight = new Vector3(Mathf.Cos(yawRadians), 0f, -Mathf.Sin(yawRadians));
            Vector3 expectedForward = root.rotation * localForward;
            Vector3 expectedRight = root.rotation * localRight;
            Assert.Less(Vector3.Distance(expectedForward, camera.transform.forward), DirectionTolerance, $"Expected forward {expectedForward:F4}, got {camera.transform.forward:F4}");
            Assert.Less(Vector3.Distance(expectedRight, camera.transform.right), DirectionTolerance, $"Expected right {expectedRight:F4}, got {camera.transform.right:F4}");
        }

        private void AssertPosition(Vector3 expected, Vector3 actual)
        {
            Assert.Less(Vector3.Distance(expected, actual), PositionTolerance, $"Expected {expected:F4}, got {actual:F4}");
        }
    }
}
