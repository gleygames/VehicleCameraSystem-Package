using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Gley.CameraSystem.Tests.PlayMode
{
    public class ReversePlayModeTests
    {
        private const string DrivingView = "Driving";
        private const float FrameTime = 0.02f;
        private const float PositionTolerance = 0.001f;
        private const float OffsetTolerance = 0.0001f;
        private const float AimTolerance = 0.05f;
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
        public void ReverseInDriving_TravelsToFrontDefault()
        {
            Activate(DrivingView);
            AssertPosition(new Vector3(0f, 0f, -3f), camera.transform.position);

            int commandId;
            Assert.AreEqual(CameraCommandResult.Accepted, controller.SetReverse(true, out commandId));

            Assert.AreNotEqual(0, commandId);
            Assert.IsTrue(controller.IsReverseActive);
            RunFrames(5);
            Assert.Greater(Vector3.Distance(new Vector3(0f, 0f, -3f), camera.transform.position), 0.01f);
            RunUntilEnded(commandId);
            AssertEndedWith(commandId, CommandEndResult.Completed);
            RunFrames(SettleFrames);
            Vector3 frontPosition = camera.transform.position;
            AssertPosition(new Vector3(0f, 0f, 3f), frontPosition);
            Assert.Less(camera.transform.forward.z, 0f);
            Assert.Less(Vector3.Angle(new Vector3(0f, 1f, 0f) - frontPosition, camera.transform.forward), AimTolerance);

            target.SetSuppliedSpeed(30f);
            RunFrames(SettleFrames);
            AssertPosition(new Vector3(0f, 0f, 3f), camera.transform.position);

            drivingPreset.Driving.Configure(0.12f, 0.08f, 3f, true, 5f, 25f, 3f, 0.25f, true, true, 12f, 30f, 0.4f, 0.5f);
            RunFrames(SettleFrames);
            AssertPosition(new Vector3(0f, 0f, 3.75f), camera.transform.position);
        }

        [Test]
        public void ReverseInPresentationOrFixed_NoChange()
        {
            Activate(CameraTestRig.PresentationView);
            RunFrames(5);
            Vector3 presentationPosition = camera.transform.position;
            Quaternion presentationRotation = camera.transform.rotation;

            int commandId;
            Assert.AreEqual(CameraCommandResult.Accepted, controller.SetReverse(true, out commandId));

            Assert.AreEqual(0, commandId);
            RunFrames(SettleFrames);
            AssertPosition(presentationPosition, camera.transform.position);
            Assert.Less(Quaternion.Angle(presentationRotation, camera.transform.rotation), 0.01f);
            Assert.AreEqual(CameraCommandResult.Accepted, controller.SetReverse(false, out commandId));
            Assert.AreEqual(0, commandId);
            RunFrames(SettleFrames);
            AssertPosition(presentationPosition, camera.transform.position);

            Assert.AreEqual(CameraCommandResult.Accepted, controller.SelectView(CameraTestRig.FixedView));
            RunFrames(5);
            Vector3 fixedPosition = camera.transform.position;
            Quaternion fixedRotation = camera.transform.rotation;
            Assert.AreEqual(CameraCommandResult.Accepted, controller.SetReverse(true, out commandId));
            Assert.AreEqual(0, commandId);
            RunFrames(SettleFrames);
            AssertPosition(fixedPosition, camera.transform.position);
            Assert.Less(Quaternion.Angle(fixedRotation, camera.transform.rotation), 0.01f);
            Assert.AreEqual(CameraTestRig.FixedView, controller.ActiveView.Name);
            Assert.AreEqual(0, endedCommandIds.Count);
        }

        [Test]
        public void ClearReverse_NoPlayerInput_ReturnsToPreviousPose()
        {
            drivingView.ConfigureDefaultPose(new OrbitPose(45f, -1f, 0.5f));
            drivingView.ConfigureFrontDefaultPose(new OrbitPose(180f, -2f, 1f));
            Activate(DrivingView);
            Vector3 previousPosition = new Vector3(2f, 0.5f, -1f);
            AssertPosition(previousPosition, camera.transform.position);
            float previousDistance = controller.OrbitDistance;

            EnterReverseAndArrive();
            AssertPosition(new Vector3(0f, 1f, 5f), camera.transform.position);
            Assert.AreEqual(-2f, controller.ZoomOffset, OffsetTolerance);
            Assert.AreEqual(1f, controller.HeightOffset, OffsetTolerance);

            int returnId;
            Assert.AreEqual(CameraCommandResult.Accepted, controller.SetReverse(false, out returnId));

            Assert.AreNotEqual(0, returnId);
            Assert.IsFalse(controller.IsReverseActive);
            RunUntilEnded(returnId);
            AssertEndedWith(returnId, CommandEndResult.Completed);
            RunFrames(SettleFrames);
            AssertPosition(previousPosition, camera.transform.position);
            Assert.AreEqual(-1f, controller.ZoomOffset, OffsetTolerance);
            Assert.AreEqual(0.5f, controller.HeightOffset, OffsetTolerance);
            Assert.AreEqual(0f, Mathf.DeltaAngle(0f, (controller.OrbitDistance - previousDistance) * 360f / 16f), 0.01f);
        }

        [Test]
        public void ClearReverse_AfterPlayerMoved_Stays()
        {
            Activate(DrivingView);
            EnterReverseAndArrive();
            AssertPosition(new Vector3(0f, 0f, 3f), camera.transform.position);

            Assert.AreEqual(CameraCommandResult.Accepted, controller.SetHeldIntent(1f, 0f, 0f));
            RunFrames(10);
            controller.SetHeldIntent(0f, 0f, 0f);
            RunFrames(SettleFrames);
            Vector3 heldPosition = camera.transform.position;
            Assert.Greater(Vector3.Distance(new Vector3(0f, 0f, 3f), heldPosition), 0.1f);
            int endedBefore = endedCommandIds.Count;

            int returnId;
            Assert.AreEqual(CameraCommandResult.Accepted, controller.SetReverse(false, out returnId));

            Assert.AreEqual(0, returnId);
            RunFrames(SettleFrames);
            AssertPosition(heldPosition, camera.transform.position);
            Assert.AreEqual(endedBefore, endedCommandIds.Count);

            EnterReverseAndArrive();
            Assert.AreEqual(CameraCommandResult.Accepted, controller.AddDrag(new Vector2(0.01f, 0f)));
            RunFrames(SettleFrames);
            Vector3 draggedPosition = camera.transform.position;
            Assert.Greater(Vector3.Distance(new Vector3(0f, 0f, 3f), draggedPosition), 0.1f);
            Assert.AreEqual(CameraCommandResult.Accepted, controller.SetReverse(false, out returnId));
            Assert.AreEqual(0, returnId);
            RunFrames(SettleFrames);
            AssertPosition(draggedPosition, camera.transform.position);
        }

        [Test]
        public void ManualInputDuringReverseTravel_Interrupted()
        {
            Activate(DrivingView);
            int enterId;
            Assert.AreEqual(CameraCommandResult.Accepted, controller.SetReverse(true, out enterId));
            RunFrames(10);
            float distanceBeforeInput = controller.OrbitDistance;

            Assert.AreEqual(CameraCommandResult.Accepted, controller.SetHeldIntent(1f, 0f, 0f));

            AssertEndedWith(enterId, CommandEndResult.Interrupted);
            RunFrames(10);
            Assert.Greater(Mathf.Abs(controller.OrbitDistance - distanceBeforeInput), 0.1f);
            controller.SetHeldIntent(0f, 0f, 0f);
            RunFrames(SettleFrames);
            Vector3 leftPosition = camera.transform.position;
            int returnId;
            Assert.AreEqual(CameraCommandResult.Accepted, controller.SetReverse(false, out returnId));
            Assert.AreEqual(0, returnId);
            RunFrames(SettleFrames);
            AssertPosition(leftPosition, camera.transform.position);

            EnterReverseAndArrive();
            Assert.AreEqual(CameraCommandResult.Accepted, controller.SetReverse(false, out returnId));
            Assert.AreNotEqual(0, returnId);
            RunFrames(5);
            Assert.AreEqual(CameraCommandResult.Accepted, controller.AddDrag(new Vector2(0.01f, 0f)));
            AssertEndedWith(returnId, CommandEndResult.Interrupted);
        }

        [Test]
        public void FrontAndRearDefaults_DifferentZoom()
        {
            drivingView.ConfigureDefaultPose(new OrbitPose(0f, -1f, 0f));
            drivingView.ConfigureFrontDefaultPose(new OrbitPose(180f, -3f, 0f));
            Activate(DrivingView);
            AssertPosition(new Vector3(0f, 0f, -4f), camera.transform.position);
            Assert.AreEqual(-1f, controller.ZoomOffset, OffsetTolerance);

            EnterReverseAndArrive();
            AssertPosition(new Vector3(0f, 0f, 6f), camera.transform.position);
            Assert.AreEqual(-3f, controller.ZoomOffset, OffsetTolerance);

            int returnId;
            Assert.AreEqual(CameraCommandResult.Accepted, controller.SetReverse(false, out returnId));
            RunUntilEnded(returnId);
            RunFrames(SettleFrames);
            AssertPosition(new Vector3(0f, 0f, -4f), camera.transform.position);
            Assert.AreEqual(-1f, controller.ZoomOffset, OffsetTolerance);
        }

        [Test]
        public void TurnLook_SuspendedAfterManualInput()
        {
            DisableFollowSmoothing();
            Activate(DrivingView);
            RunTurningFrames(TurnSettleFrames);
            float turningOffset = controller.TurnLookOffset;
            Assert.Less(turningOffset, -11.5f);
            AssertLocalPosition(new Vector3(3f * Mathf.Tan(turningOffset * Mathf.Deg2Rad), 0f, -3f));

            Assert.AreEqual(CameraCommandResult.Accepted, controller.SetHeldIntent(0f, 1f, 0f));
            RunTurningFrames(5);
            controller.SetHeldIntent(0f, 0f, 0f);
            float height = controller.HeightOffset;
            RunTurningFrames(TurnDecayFrames);

            Assert.Less(Mathf.Abs(controller.TurnLookOffset), 0.05f);
            AssertLocalPosition(new Vector3(0f, height, -3f), 0.01f);
        }

        [Test]
        public void TurnLook_OffDuringReverse()
        {
            DisableFollowSmoothing();
            Activate(DrivingView);
            RunTurningFrames(TurnSettleFrames);
            Assert.Less(controller.TurnLookOffset, -11.5f);

            int enterId;
            Assert.AreEqual(CameraCommandResult.Accepted, controller.SetReverse(true, out enterId));
            RunTurningUntilEnded(enterId);
            RunTurningFrames(TurnDecayFrames);

            Assert.AreEqual(CommandEndResult.Completed, endedResults[endedCommandIds.IndexOf(enterId)]);
            Assert.Less(Mathf.Abs(controller.TurnLookOffset), 0.05f);
            AssertLocalPosition(new Vector3(0f, 0f, 3f), 0.01f);

            int returnId;
            Assert.AreEqual(CameraCommandResult.Accepted, controller.SetReverse(false, out returnId));
            RunTurningUntilEnded(returnId);
            RunTurningFrames(TurnSettleFrames);
            Assert.Less(controller.TurnLookOffset, -11.5f);
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

        private void DisableFollowSmoothing()
        {
            drivingPreset.Driving.Configure(0f, 0f, 3f, true, 5f, 25f, 3f, 0.25f, false, true, 12f, TurnRate, 0.4f, 0.5f);
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

        private void AssertLocalPosition(Vector3 expected)
        {
            AssertLocalPosition(expected, PositionTolerance);
        }

        private void AssertLocalPosition(Vector3 expected, float tolerance)
        {
            Vector3 actual = root.InverseTransformPoint(camera.transform.position);
            Assert.Less(Vector3.Distance(expected, actual), tolerance, $"Expected local {expected:F4}, got {actual:F4}");
        }

        private void AssertPosition(Vector3 expected, Vector3 actual)
        {
            Assert.Less(Vector3.Distance(expected, actual), PositionTolerance, $"Expected {expected:F4}, got {actual:F4}");
        }
    }
}
