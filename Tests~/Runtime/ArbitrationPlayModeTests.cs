using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Gley.CameraSystem.Tests.PlayMode
{
    public class ArbitrationPlayModeTests
    {
        private const string SecondView = "Presentation Two";
        private const float FrameTime = 0.02f;
        private const float PositionTolerance = 0.001f;
        private const float DistanceTolerance = 0.0001f;
        private const float AimTolerance = 0.05f;
        private const float RectangleLength = 16f;
        private const int MaximumFrames = 2000;

        private readonly List<CommandEndResult> endedResults = new List<CommandEndResult>();
        private readonly List<int> endedCommandIds = new List<int>();

        private CameraTestRig rig;
        private CameraSystemController controller;
        private VehicleCameraTarget target;
        private Camera camera;
        private Transform root;

        [SetUp]
        public void SetUp()
        {
            rig = new CameraTestRig();
            endedResults.Clear();
            endedCommandIds.Clear();
            controller = null;
            target = null;
            camera = null;
            root = null;
        }

        [Test]
        public void ManualInputDuringPointTravel_Interrupted()
        {
            ActivatePointProfile();
            int travelId;
            Assert.AreEqual(CameraCommandResult.Accepted, controller.RequestPoint("engine", new TravelRequest(TravelDirection.Shortest), out travelId));
            RunFrames(10);
            float distanceBeforeInput = controller.OrbitDistance;

            Assert.AreEqual(CameraCommandResult.Accepted, controller.SetHeldIntent(1f, 0f, 0f));

            Assert.AreEqual(1, endedCommandIds.Count);
            Assert.AreEqual(travelId, endedCommandIds[0]);
            Assert.AreEqual(CommandEndResult.Interrupted, endedResults[0]);
            RunFrames(10);
            Assert.Greater(Mathf.Abs(controller.OrbitDistance - distanceBeforeInput), 0.1f);
            controller.SetHeldIntent(0f, 0f, 0f);
            RunFrames(200);
            Assert.AreEqual(1, endedCommandIds.Count);

            int secondTravelId;
            Assert.AreEqual(CameraCommandResult.Accepted, controller.RequestPoint("wheel", new TravelRequest(TravelDirection.Shortest), out secondTravelId));
            RunFrames(5);
            Assert.AreEqual(CameraCommandResult.Accepted, controller.AddDrag(new Vector2(0.01f, 0f)));

            Assert.AreEqual(2, endedCommandIds.Count);
            Assert.AreEqual(secondTravelId, endedCommandIds[1]);
            Assert.AreEqual(CommandEndResult.Interrupted, endedResults[1]);
            controller.UpdateCameraFrame(FrameTime);
            Vector3 afterDrag = camera.transform.position;
            RunFrames(20);
            AssertPosition(afterDrag, camera.transform.position);
        }

        [Test]
        public void LockedPointTravel_ManualInputIgnored_GameViewSwitchStillWorks()
        {
            ActivatePointProfile();
            int lockedId;
            Assert.AreEqual(CameraCommandResult.Accepted, controller.RequestPoint("engine", new TravelRequest(TravelDirection.Shortest, true), out lockedId));
            Assert.IsTrue(controller.IsPlayerControlLocked);

            for (int frame = 0; frame < MaximumFrames && !endedCommandIds.Contains(lockedId); frame++)
            {
                Assert.AreEqual(CameraCommandResult.PlayerControlLocked, controller.SetHeldIntent(-1f, 1f, 1f));
                Assert.AreEqual(CameraCommandResult.PlayerControlLocked, controller.AddDrag(new Vector2(0.2f, 0.1f)));
                Assert.AreEqual(CameraCommandResult.PlayerControlLocked, controller.AddPinch(0.5f));
                controller.UpdateCameraFrame(FrameTime);
            }

            AssertEndedWith(lockedId, CommandEndResult.Completed);
            AssertArrived(new Vector3(0f, 0f, 3f), 9f);
            Assert.AreEqual(0f, controller.ZoomOffset);
            Assert.AreEqual(0f, controller.HeightOffset);
            Assert.IsFalse(controller.IsPlayerControlLocked);
            controller.SetHeldIntent(0f, 0f, 0f);

            int secondLockedId;
            Assert.AreEqual(CameraCommandResult.Accepted, controller.RequestPoint("plate", new TravelRequest(TravelDirection.Shortest, true), out secondLockedId));
            RunFrames(5);
            Assert.AreEqual(CameraCommandResult.PlayerControlLocked, controller.SelectView(SecondView, CommandSource.Player));
            Assert.AreEqual(CameraTestRig.PresentationView, controller.ActiveView.Name);
            Assert.AreEqual(1, endedCommandIds.Count);

            Assert.AreEqual(CameraCommandResult.Accepted, controller.SelectView(SecondView));

            Assert.AreEqual(SecondView, controller.ActiveView.Name);
            AssertEndedWith(secondLockedId, CommandEndResult.Replaced);
            Assert.IsFalse(controller.IsPlayerControlLocked);
        }

        [Test]
        public void SwitchViewWhileHoldingInput_OldInputDiscardedUntilNeutral()
        {
            ActivatePointProfile();
            float startDistance = controller.OrbitDistance;
            controller.SetHeldIntent(1f, 0f, 0f);
            RunFrames(10);
            Assert.Greater(Mathf.Abs(controller.OrbitDistance - startDistance), 0.1f);

            Assert.AreEqual(CameraCommandResult.Accepted, controller.SelectView(SecondView));
            Vector3 afterSwitch = camera.transform.position;
            float distanceAfterSwitch = controller.OrbitDistance;
            for (int frame = 0; frame < 20; frame++)
            {
                controller.SetHeldIntent(1f, 0f, 0f);
                controller.UpdateCameraFrame(FrameTime);
            }

            AssertPosition(afterSwitch, camera.transform.position);
            Assert.AreEqual(distanceAfterSwitch, controller.OrbitDistance);

            controller.SetHeldIntent(0f, 0f, 0f);
            controller.UpdateCameraFrame(FrameTime);
            controller.SetHeldIntent(1f, 0f, 0f);
            RunFrames(10);

            Assert.Greater(Mathf.Abs(controller.OrbitDistance - distanceAfterSwitch), 0.1f);
        }

        [Test]
        public void AttachDuringPointTravel_ContinuesToSameMarker()
        {
            ActivatePointProfile();
            Transform trailer = rig.CreateBody("Trailer");
            trailer.position = new Vector3(0f, 0f, -8f);
            VehicleProfile trailerProfile = rig.CreateProfile(1f, 4f, 4f);
            int travelId;
            Assert.AreEqual(CameraCommandResult.Accepted, controller.RequestPoint("engine", new TravelRequest(TravelDirection.Shortest), out travelId));
            RunFrames(25);
            Vector3 beforeAttach = camera.transform.position;
            Assert.AreEqual(-1f, beforeAttach.x, PositionTolerance);

            Assert.AreEqual(VehicleTargetResult.Accepted, target.Attach(trailer, trailerProfile, ChainEnd.Rear));
            Assert.AreEqual(0, endedCommandIds.Count);
            controller.UpdateCameraFrame(FrameTime);
            Assert.Less(Vector3.Distance(beforeAttach, camera.transform.position), 0.25f);

            Assert.AreEqual(CommandEndResult.Completed, RunUntilEnded(travelId));
            Assert.AreEqual(1, endedCommandIds.Count);
            AssertPosition(new Vector3(0f, 0f, 3f), camera.transform.position);
            AssertAimAt(new Vector3(0f, 1f, 2f));
        }

        [Test]
        public void DetachOwnerDuringPointTravel_TargetDisconnected()
        {
            root = rig.CreateBody("Truck");
            VehicleProfile truckProfile = rig.CreateProfile(1f, 3f, 3f);
            VehicleProfile trailerProfile = rig.CreateProfile(1f, 4f, 4f);
            VehicleOrbit trailerOrbit;
            Assert.IsTrue(trailerProfile.TryGetOrbit(CameraTestRig.OrbitName, out trailerOrbit));
            trailerProfile.AddWatchMarker(trailerOrbit, "tail", 1f / 20f, new Vector3(0f, 0.5f, -4f), true);
            VehicleViewEntry view;
            Assert.IsTrue(truckProfile.TryGetView(CameraTestRig.PresentationView, out view));
            view.ConfigureDefaultPose(new OrbitPose(90f, 0f, 0f));
            Transform trailer = rig.CreateBody("Trailer");
            trailer.position = new Vector3(0f, 0f, -8f);
            target = rig.CreateTarget(root, truckProfile);
            Assert.AreEqual(VehicleTargetResult.Accepted, target.Attach(trailer, trailerProfile, ChainEnd.Rear));
            Activate();
            int travelId;
            Assert.AreEqual(CameraCommandResult.Accepted, controller.RequestPoint("tail", new TravelRequest(TravelDirection.Shortest), out travelId));
            RunFrames(20);
            Assert.AreEqual(0, endedCommandIds.Count);

            Assert.AreEqual(VehicleTargetResult.Accepted, target.Detach(trailer));

            Assert.AreEqual(1, endedCommandIds.Count);
            AssertEndedWith(travelId, CommandEndResult.TargetDisconnected);
            controller.UpdateCameraFrame(FrameTime);
            Vector3 afterDetach = camera.transform.position;
            RunFrames(20);
            AssertPosition(afterDetach, camera.transform.position);
            Assert.AreEqual(1, endedCommandIds.Count);
            int rejectedId;
            Assert.AreEqual(CameraCommandResult.PointNotFound, controller.RequestPoint("tail", new TravelRequest(TravelDirection.Shortest), out rejectedId));
        }

        [Test]
        public void TeleportDuringTravel_SnapsAndContinues()
        {
            ActivatePointProfile();
            int travelId;
            Assert.AreEqual(CameraCommandResult.Accepted, controller.RequestPoint("engine", new TravelRequest(TravelDirection.Shortest), out travelId));
            RunFrames(25);
            Vector3 localBefore = root.InverseTransformPoint(camera.transform.position);
            float distanceBefore = controller.OrbitDistance;

            root.SetPositionAndRotation(new Vector3(40f, 0f, 25f), Quaternion.Euler(0f, 120f, 0f));
            target.Teleport();

            AssertPosition(root.TransformPoint(localBefore), camera.transform.position);
            Assert.AreEqual(distanceBefore, controller.OrbitDistance);
            Assert.AreEqual(0, endedCommandIds.Count);
            Assert.AreEqual(CommandEndResult.Completed, RunUntilEnded(travelId));
            AssertPosition(root.TransformPoint(new Vector3(0f, 0f, 3f)), camera.transform.position);
            AssertAimAt(root.TransformPoint(new Vector3(0f, 1f, 2f)));
        }

        [Test]
        public void TeleportDuringSmoothActivation_TransitionMovesWithVehicle()
        {
            root = rig.CreateBody("Truck");
            VehicleProfile profile = rig.CreateProfile(1f, 3f, 3f);
            target = rig.CreateTarget(root, profile);
            camera = rig.CreateCamera("Camera");
            camera.transform.position = new Vector3(30f, 10f, 20f);
            controller = rig.CreateController("Controller", camera, target, CameraTestRig.FixedView);
            controller.CommandEnded += RecordCommandEnded;
            int commandId;
            Assert.AreEqual(CameraCommandResult.Accepted, controller.Activate(new TransitionOptions(TransitionMode.Duration, 2f), out commandId));
            RunFrames(50);
            Vector3 localBefore = root.InverseTransformPoint(camera.transform.position);
            Quaternion localRotationBefore = Quaternion.Inverse(root.rotation) * camera.transform.rotation;

            root.SetPositionAndRotation(new Vector3(40f, 0f, 25f), Quaternion.Euler(0f, 120f, 0f));
            target.Teleport();

            AssertPosition(root.TransformPoint(localBefore), camera.transform.position);
            Assert.Less(Quaternion.Angle(root.rotation * localRotationBefore, camera.transform.rotation), 0.05f);
            Assert.AreEqual(0, endedCommandIds.Count);
            Assert.AreEqual(CommandEndResult.Completed, RunUntilEnded(commandId));
            AssertPosition(root.TransformPoint(profile.FixedViewPose.CameraLocalPosition), camera.transform.position);
        }

        [Test]
        public void PlayerViewSwitchDuringExplicitLock_PlayerControlLocked()
        {
            ActivatePointProfile();
            controller.SetPlayerControlLocked(true);
            Assert.IsTrue(controller.IsPlayerControlLocked);

            Assert.AreEqual(CameraCommandResult.PlayerControlLocked, controller.SelectView(SecondView, CommandSource.Player));
            Assert.AreEqual(CameraTestRig.PresentationView, controller.ActiveView.Name);
            int rejectedId;
            Assert.AreEqual(CameraCommandResult.PlayerControlLocked, controller.RequestPoint("engine", new TravelRequest(TravelDirection.Shortest), CommandSource.Player, out rejectedId));
            Assert.AreEqual(0, rejectedId);
            Assert.AreEqual(CameraCommandResult.PlayerControlLocked, controller.SetHeldIntent(1f, 0f, 0f));

            Assert.AreEqual(CameraCommandResult.Accepted, controller.SelectView(SecondView));
            Assert.AreEqual(SecondView, controller.ActiveView.Name);
            Assert.IsTrue(controller.IsPlayerControlLocked);
            int travelId;
            Assert.AreEqual(CameraCommandResult.Accepted, controller.RequestPoint("engine", new TravelRequest(TravelDirection.Shortest), out travelId));
            Assert.AreEqual(CommandEndResult.Completed, RunUntilEnded(travelId));
            Assert.IsTrue(controller.IsPlayerControlLocked);

            controller.SetPlayerControlLocked(false);
            Assert.IsFalse(controller.IsPlayerControlLocked);
            Assert.AreEqual(CameraCommandResult.Accepted, controller.SelectView(CameraTestRig.PresentationView, CommandSource.Player));
            Assert.AreEqual(CameraTestRig.PresentationView, controller.ActiveView.Name);
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

        private void ActivatePointProfile()
        {
            root = rig.CreateBody("Truck");
            VehicleProfile profile = rig.CreateProfile(1f, 3f, 3f);
            VehicleOrbit orbit;
            Assert.IsTrue(profile.TryGetOrbit(CameraTestRig.OrbitName, out orbit));
            profile.AddWatchMarker(orbit, "wheel", 3.5f / RectangleLength, new Vector3(-0.5f, 0.5f, -1.5f), true);
            profile.AddWatchMarker(orbit, "plate", 1f / RectangleLength, new Vector3(0f, 0.5f, -2.5f), true);
            profile.AddWatchMarker(orbit, "engine", 9f / RectangleLength, new Vector3(0f, 1f, 2f), true);
            profile.AddView(SecondView, rig.PresentationPreset, orbit.Id);
            target = rig.CreateTarget(root, profile);
            Activate();
            AssertArrived(new Vector3(0f, 0f, -3f), 1f);
        }

        private void Activate()
        {
            camera = rig.CreateCamera("Camera");
            controller = rig.CreateController("Controller", camera, target, CameraTestRig.PresentationView);
            controller.CommandEnded += RecordCommandEnded;
            Assert.AreEqual(CameraCommandResult.Accepted, controller.Activate());
            controller.UpdateCameraFrame(FrameTime);
        }

        private void RecordCommandEnded(int commandId, CommandEndResult result)
        {
            endedCommandIds.Add(commandId);
            endedResults.Add(result);
        }

        private void RunFrames(int frameCount)
        {
            for (int frame = 0; frame < frameCount; frame++)
            {
                controller.UpdateCameraFrame(FrameTime);
            }
        }

        private void AssertPosition(Vector3 expected, Vector3 actual)
        {
            Assert.Less(Vector3.Distance(expected, actual), PositionTolerance, $"Expected {expected:F4}, got {actual:F4}");
        }

        private void AssertEndedWith(int commandId, CommandEndResult expected)
        {
            int index = endedCommandIds.IndexOf(commandId);
            Assert.GreaterOrEqual(index, 0, $"Command {commandId} did not end.");
            Assert.AreEqual(expected, endedResults[index]);
        }

        private void AssertArrived(Vector3 expectedPosition, float expectedDistance)
        {
            AssertPosition(expectedPosition, camera.transform.position);
            Assert.AreEqual(expectedDistance, Mathf.Repeat(controller.OrbitDistance, RectangleLength), DistanceTolerance);
        }

        private CommandEndResult RunUntilEnded(int commandId)
        {
            Assert.Greater(commandId, 0);
            for (int frame = 0; frame < MaximumFrames && !endedCommandIds.Contains(commandId); frame++)
            {
                controller.UpdateCameraFrame(FrameTime);
            }

            int index = endedCommandIds.IndexOf(commandId);
            Assert.GreaterOrEqual(index, 0, $"Command {commandId} did not end within {MaximumFrames} frames.");
            return endedResults[index];
        }

        private void AssertAimAt(Vector3 watchPoint)
        {
            Assert.Less(Vector3.Angle(watchPoint - camera.transform.position, camera.transform.forward), AimTolerance);
        }
    }
}
