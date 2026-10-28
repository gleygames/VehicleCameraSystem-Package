using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Gley.CameraSystem.Tests.PlayMode
{
    public class TargetChangePlayModeTests
    {
        private const string DrivingView = "Driving";
        private const float FrameTime = 0.02f;
        private const float PositionTolerance = 0.01f;
        private const float ShiftTolerance = 0.005f;
        private const float RightSideBearing = 90f;
        private const float SettleSeconds = 3f;
        private const float DrivingSpeed = 10f;
        private const int MaximumFrames = 2000;

        private readonly List<CommandEndResult> endedResults = new List<CommandEndResult>();
        private readonly List<int> endedCommandIds = new List<int>();
        private readonly List<int> changedViewIds = new List<int>();

        private CameraTestRig rig;
        private CameraSystemController controller;
        private VehicleCameraTarget firstTarget;
        private VehicleCameraTarget secondTarget;
        private VehicleProfile firstProfile;
        private VehicleProfile secondProfile;
        private Camera camera;
        private Transform firstRoot;
        private Transform secondRoot;
        private int targetLostCount;

        [SetUp]
        public void SetUp()
        {
            rig = new CameraTestRig();
            endedResults.Clear();
            endedCommandIds.Clear();
            changedViewIds.Clear();
            controller = null;
            firstTarget = null;
            secondTarget = null;
            firstProfile = null;
            secondProfile = null;
            camera = null;
            firstRoot = null;
            secondRoot = null;
            targetLostCount = 0;
        }

        [TearDown]
        public void TearDown()
        {
            rig.Dispose();
        }

        [Test]
        public void ChangeTarget_Smooth_EndsAtNewVehicleDefault()
        {
            ActivateOnFirstVehicle(RightSideBearing);
            Vector3 startPosition = camera.transform.position;
            Vector3 destination = secondRoot.TransformPoint(new Vector3(0f, 0f, -3f));
            int commandId;

            Assert.AreEqual(CameraCommandResult.Accepted, controller.ChangeTarget(secondTarget, new TransitionOptions(TransitionMode.Duration, 1.5f), false, out commandId));

            Assert.AreNotEqual(0, commandId);
            Assert.AreSame(secondTarget, controller.Target);
            RunFrames(37);
            Assert.AreEqual(0, endedCommandIds.Count);
            Assert.Greater(Vector3.Distance(startPosition, camera.transform.position), 1f);
            Assert.Greater(Vector3.Distance(destination, camera.transform.position), 1f);
            Assert.AreEqual(CommandEndResult.Completed, RunUntilEnded(commandId));
            AssertPosition(destination, camera.transform.position);
        }

        [Test]
        public void ChangeTarget_PreserveBearing_KeepsBearing()
        {
            ActivateOnFirstVehicle(RightSideBearing);
            int commandId;

            Assert.AreEqual(CameraCommandResult.Accepted, controller.ChangeTarget(secondTarget, new TransitionOptions(TransitionMode.Duration, 1.5f), true, out commandId));

            Assert.AreEqual(CommandEndResult.Completed, RunUntilEnded(commandId));
            AssertPosition(secondRoot.TransformPoint(new Vector3(1f, 0f, 0f)), camera.transform.position);
        }

        [Test]
        public void ChangeTarget_OldTargetEventsNoLongerAffectCamera()
        {
            ActivateOnFirstVehicle(0f);
            controller.TargetLost += RecordTargetLost;
            int commandId;
            Assert.AreEqual(CameraCommandResult.Accepted, controller.ChangeTarget(secondTarget, new TransitionOptions(TransitionMode.Snap), false, out commandId));
            RunFrames(5);
            Vector3 positionBefore = camera.transform.position;

            Object.DestroyImmediate(firstTarget.gameObject);
            RunFrames(5);

            Assert.AreEqual(0, targetLostCount);
            Assert.IsFalse(controller.IsTargetLost);
            AssertPosition(positionBefore, camera.transform.position);
        }

        [Test]
        public void ChangeTarget_ViewMissingInNewProfile_ViewNotAvailable_NothingChanges()
        {
            ActivateOnFirstVehicle(RightSideBearing);
            secondTarget.Configure(secondRoot, rig.CreateEmptyProfile());
            RunFrames(5);
            Vector3 positionBefore = camera.transform.position;
            Quaternion rotationBefore = camera.transform.rotation;
            int commandId;

            Assert.AreEqual(CameraCommandResult.ViewNotAvailable, controller.ChangeTarget(secondTarget, new TransitionOptions(TransitionMode.Duration, 1.5f), false, out commandId));

            Assert.AreEqual(0, commandId);
            Assert.AreSame(firstTarget, controller.Target);
            Assert.AreEqual(CameraTestRig.PresentationView, controller.ActiveView.Name);
            RunFrames(5);
            AssertPosition(positionBefore, camera.transform.position);
            Assert.Less(Quaternion.Angle(rotationBefore, camera.transform.rotation), 0.05f);
            Assert.AreEqual(0, endedCommandIds.Count);
        }

        [Test]
        public void ChangeTarget_WithViewName_SwitchesViewAndRaisesViewChanged()
        {
            ActivateOnFirstVehicle(0f);
            VehicleViewEntry fixedView;
            Assert.IsTrue(secondProfile.TryGetView(CameraTestRig.FixedView, out fixedView));
            int commandId;

            Assert.AreEqual(CameraCommandResult.Accepted, controller.ChangeTarget(secondTarget, CameraTestRig.FixedView, new TransitionOptions(TransitionMode.Snap), false, out commandId));

            Assert.AreEqual(CameraTestRig.FixedView, controller.ActiveView.Name);
            Assert.AreEqual(1, changedViewIds.Count);
            Assert.AreEqual(fixedView.Id, changedViewIds[0]);
            AssertPosition(secondRoot.TransformPoint(secondProfile.FixedViewPose.CameraLocalPosition), camera.transform.position);
        }

        [Test]
        public void ChangeTarget_SameViewName_DoesNotRaiseViewChanged()
        {
            ActivateOnFirstVehicle(0f);
            int commandId;

            Assert.AreEqual(CameraCommandResult.Accepted, controller.ChangeTarget(secondTarget, new TransitionOptions(TransitionMode.Snap), false, out commandId));

            Assert.AreEqual(0, changedViewIds.Count);
            AssertPosition(secondRoot.TransformPoint(new Vector3(0f, 0f, -3f)), camera.transform.position);
        }

        [Test]
        public void Teleport_SnapsKeepingDistanceZoomHeight()
        {
            ActivateOnFirstVehicle(0f);
            controller.SetHeldIntent(0.5f, 1f, 1f);
            RunFrames(20);
            controller.SetHeldIntent(0f, 0f, 0f);
            RunFrames(5);
            float distanceBefore = controller.OrbitDistance;
            float zoomBefore = controller.ZoomOffset;
            float heightBefore = controller.HeightOffset;
            Assert.Greater(Mathf.Abs(zoomBefore) + Mathf.Abs(heightBefore), 0.01f);
            Vector3 localBefore = firstRoot.InverseTransformPoint(camera.transform.position);

            firstRoot.SetPositionAndRotation(new Vector3(60f, 0f, -15f), Quaternion.Euler(0f, 200f, 0f));
            firstTarget.Teleport();

            AssertPosition(firstRoot.TransformPoint(localBefore), camera.transform.position);
            Assert.AreEqual(distanceBefore, controller.OrbitDistance, 0.0001f);
            Assert.AreEqual(zoomBefore, controller.ZoomOffset, 0.0001f);
            Assert.AreEqual(heightBefore, controller.HeightOffset, 0.0001f);
        }

        [Test]
        public void OriginShift_CameraPoseRelativeToVehicleUnchanged_NoSpeedSpike()
        {
            ActivateOnFirstVehicle(0f, DrivingView);
            int settleFrames = Mathf.RoundToInt(SettleSeconds / FrameTime);
            DriveFrames(settleFrames);
            Vector3 relativeBefore = camera.transform.position - firstRoot.position;
            Quaternion rotationBefore = Quaternion.Inverse(firstRoot.rotation) * camera.transform.rotation;
            Vector3 offset = new Vector3(-1000f, 0f, 0f);

            firstRoot.position += offset;
            firstTarget.ShiftOrigin(offset);
            DriveFrames(1);

            Vector3 relativeAfter = camera.transform.position - firstRoot.position;
            Assert.Less(Vector3.Distance(relativeBefore, relativeAfter), ShiftTolerance, $"Expected {relativeBefore:F4}, got {relativeAfter:F4}");
            Assert.Less(Quaternion.Angle(rotationBefore, Quaternion.Inverse(firstRoot.rotation) * camera.transform.rotation), 0.05f);
            DriveFrames(settleFrames);
            Vector3 relativeLater = camera.transform.position - firstRoot.position;
            Assert.Less(Vector3.Distance(relativeBefore, relativeLater), ShiftTolerance, $"Expected {relativeBefore:F4}, got {relativeLater:F4}");
        }

        [Test]
        public void OriginShift_OnLostTargetCamera_ShiftsHeldPose()
        {
            ActivateOnFirstVehicle(0f);
            RunFrames(5);
            Object.DestroyImmediate(firstRoot.gameObject);
            RunFrames(1);
            Assert.IsTrue(controller.IsTargetLost);
            Vector3 positionBefore = camera.transform.position;
            Quaternion rotationBefore = camera.transform.rotation;
            Vector3 offset = new Vector3(-1000f, 5f, 250f);

            controller.ShiftOrigin(offset);

            AssertPosition(positionBefore + offset, camera.transform.position);
            Assert.Less(Quaternion.Angle(rotationBefore, camera.transform.rotation), 0.001f);
            RunFrames(3);
            AssertPosition(positionBefore + offset, camera.transform.position);
        }

        private void ActivateOnFirstVehicle(float firstDefaultBearing)
        {
            ActivateOnFirstVehicle(firstDefaultBearing, CameraTestRig.PresentationView);
        }

        private void ActivateOnFirstVehicle(float firstDefaultBearing, string viewName)
        {
            firstRoot = rig.CreateBody("First Truck");
            secondRoot = rig.CreateBody("Second Truck");
            secondRoot.SetPositionAndRotation(new Vector3(30f, 0f, 10f), Quaternion.Euler(0f, 90f, 0f));
            firstProfile = CreateProfile();
            secondProfile = CreateProfile();
            VehicleViewEntry presentation;
            Assert.IsTrue(firstProfile.TryGetView(CameraTestRig.PresentationView, out presentation));
            presentation.ConfigureDefaultPose(new OrbitPose(firstDefaultBearing, 0f, 0f));
            firstTarget = rig.CreateTarget(firstRoot, firstProfile);
            secondTarget = rig.CreateTarget(secondRoot, secondProfile);
            firstTarget.SetSuppliedSpeed(0f);
            secondTarget.SetSuppliedSpeed(0f);
            camera = rig.CreateCamera("Camera");
            controller = rig.CreateController("Controller", camera, firstTarget, viewName);
            controller.CommandEnded += RecordCommandEnded;
            controller.ViewChanged += RecordViewChanged;
            Assert.AreEqual(CameraCommandResult.Accepted, controller.Activate());
        }

        private VehicleProfile CreateProfile()
        {
            VehicleProfile profile = rig.CreateProfile(1f, 3f, 3f);
            VehicleOrbit orbit;
            Assert.IsTrue(profile.TryGetOrbit(CameraTestRig.OrbitName, out orbit));
            profile.AddView(DrivingView, rig.CreatePreset(CameraViewType.Driving), orbit.Id);
            return profile;
        }

        private void RecordCommandEnded(int commandId, CommandEndResult result)
        {
            endedCommandIds.Add(commandId);
            endedResults.Add(result);
        }

        private void RecordViewChanged(int viewId)
        {
            changedViewIds.Add(viewId);
        }

        private void RecordTargetLost()
        {
            targetLostCount++;
        }

        private void RunFrames(int count)
        {
            for (int frame = 0; frame < count; frame++)
            {
                controller.UpdateCameraFrame(FrameTime);
            }
        }

        private void DriveFrames(int count)
        {
            firstTarget.ClearSuppliedSpeed();
            for (int frame = 0; frame < count; frame++)
            {
                firstRoot.position += Vector3.forward * DrivingSpeed * FrameTime;
                controller.UpdateCameraFrame(FrameTime);
            }
        }

        private CommandEndResult RunUntilEnded(int commandId)
        {
            for (int frame = 0; frame < MaximumFrames; frame++)
            {
                controller.UpdateCameraFrame(FrameTime);
                int index = endedCommandIds.IndexOf(commandId);
                if (index >= 0)
                {
                    return endedResults[index];
                }
            }

            Assert.Fail($"Command {commandId} did not end.");
            return CommandEndResult.Interrupted;
        }

        private void AssertPosition(Vector3 expected, Vector3 actual)
        {
            Assert.Less(Vector3.Distance(expected, actual), PositionTolerance, $"Expected {expected:F4}, got {actual:F4}");
        }
    }
}
