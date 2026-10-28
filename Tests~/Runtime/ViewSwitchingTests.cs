using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Gley.CameraSystem.Tests.PlayMode
{
    public class ViewSwitchingTests
    {
        private const string DrivingView = "Driving";
        private const string WideView = "Wide Presentation";
        private const string FarView = "Far Presentation";
        private const string LimitedView = "Limited Presentation";
        private const string WideOrbitName = "Wide";
        private const string FarOrbitName = "Far";
        private const float FrameTime = 0.02f;
        private const float PositionTolerance = 0.001f;
        private const float OffsetTolerance = 0.0001f;
        private const float BearingTolerance = 0.1f;
        private const int FollowSettleFrames = 100;

        private readonly List<int> changedViewIds = new List<int>();
        private readonly List<int> endedCommandIds = new List<int>();
        private readonly List<CommandEndResult> endedResults = new List<CommandEndResult>();

        private CameraTestRig rig;
        private CameraSystemController controller;
        private VehicleProfile profile;
        private VehicleViewEntry drivingView;
        private CameraViewPreset drivingPreset;
        private CameraViewPreset widePreset;
        private CameraViewPreset limitedPreset;
        private Camera camera;
        private Transform root;

        [SetUp]
        public void SetUp()
        {
            rig = new CameraTestRig();
            changedViewIds.Clear();
            endedCommandIds.Clear();
            endedResults.Clear();
            controller = null;
            root = rig.CreateBody("Truck");
            profile = rig.CreateProfile(1f, 3f, 3f);
            VehicleOrbit mainOrbit;
            Assert.IsTrue(profile.TryGetOrbit(CameraTestRig.OrbitName, out mainOrbit));
            drivingPreset = rig.CreatePreset(CameraViewType.Driving);
            drivingView = profile.AddView(DrivingView, drivingPreset, mainOrbit.Id);
            widePreset = rig.CreatePreset(CameraViewType.Presentation);
            VehicleOrbit wideOrbit = AddRectangleOrbit(WideOrbitName, 2f, 4f, 4f);
            profile.AddView(WideView, widePreset, wideOrbit.Id);
            VehicleOrbit farOrbit = AddRectangleOrbit(FarOrbitName, 2f, 10f, 10f);
            profile.AddView(FarView, widePreset, farOrbit.Id);
            limitedPreset = rig.CreatePreset(CameraViewType.Presentation);
            limitedPreset.ConfigureAngleLimits(true, -90f, 90f);
            profile.AddView(LimitedView, limitedPreset, mainOrbit.Id);
            camera = rig.CreateCamera("Camera");
        }

        [Test]
        public void DrivingToPresentation_SameBearing()
        {
            drivingView.ConfigureDefaultPose(new OrbitPose(45f, 0f, 0f));
            ActivateDriving();
            controller.SetHeldIntent(1f, 0f, 0f);
            RunFrames(20);
            controller.SetHeldIntent(0f, 0f, 0f);
            RunFrames(FollowSettleFrames);
            Vector3 drivingPosition = camera.transform.position;
            Assert.Greater(Mathf.Abs(Mathf.DeltaAngle(45f, GetBearing(drivingPosition))), 5f);

            Assert.AreEqual(CameraCommandResult.Accepted, controller.SelectView(WideView));

            Vector3 presentationPosition = camera.transform.position;
            Assert.AreEqual(0f, Mathf.DeltaAngle(GetBearing(drivingPosition), GetBearing(presentationPosition)), BearingTolerance);
            Assert.AreEqual(1f, Mathf.Max(Mathf.Abs(presentationPosition.x) / 2f, Mathf.Abs(presentationPosition.z) / 4f), PositionTolerance);
            Assert.AreEqual(0f, presentationPosition.y, PositionTolerance);
        }

        [Test]
        public void DestinationRestricted_MapsToNearestAllowedBearing()
        {
            drivingView.ConfigureDefaultPose(new OrbitPose(150f, 0f, 0f));
            ActivateDriving();
            AssertPosition(new Vector3(1f, 0f, Mathf.Sqrt(3f)), camera.transform.position);

            Assert.AreEqual(CameraCommandResult.Accepted, controller.SelectView(LimitedView));
            AssertPosition(new Vector3(1f, 0f, 0f), camera.transform.position);

            controller.Deactivate();
            drivingView.ConfigureDefaultPose(new OrbitPose(-150f, 0f, 0f));
            Assert.AreEqual(CameraCommandResult.Accepted, controller.Activate());
            AssertPosition(new Vector3(-1f, 0f, Mathf.Sqrt(3f)), camera.transform.position);
            Assert.AreEqual(CameraCommandResult.Accepted, controller.SelectView(LimitedView));
            AssertPosition(new Vector3(-1f, 0f, 0f), camera.transform.position);

            controller.Deactivate();
            drivingView.ConfigureDefaultPose(new OrbitPose(45f, 0f, 0f));
            Assert.AreEqual(CameraCommandResult.Accepted, controller.Activate());
            Assert.AreEqual(CameraCommandResult.Accepted, controller.SelectView(LimitedView));
            AssertPosition(new Vector3(1f, 0f, -1f), camera.transform.position);
        }

        [Test]
        public void ReturnToDriving_RestoresPreviousZoomAndHeight()
        {
            drivingView.ConfigureDefaultPose(new OrbitPose(0f, -1f, 0.5f));
            ActivateDriving();
            controller.SetHeldIntent(0f, 1f, 1f);
            RunFrames(10);
            controller.SetHeldIntent(0f, 0f, 0f);
            float drivingZoom = controller.ZoomOffset;
            float drivingHeight = controller.HeightOffset;
            Assert.AreEqual(-0.4f, drivingZoom, 0.01f);
            Assert.AreEqual(0.9f, drivingHeight, 0.01f);

            Assert.AreEqual(CameraCommandResult.Accepted, controller.SelectView(CameraTestRig.PresentationView));
            Assert.AreEqual(0f, controller.ZoomOffset, OffsetTolerance);
            Assert.AreEqual(0f, controller.HeightOffset, OffsetTolerance);
            controller.SetHeldIntent(0f, -1f, -1f);
            RunFrames(10);
            controller.SetHeldIntent(0f, 0f, 0f);
            float presentationZoom = controller.ZoomOffset;
            float presentationHeight = controller.HeightOffset;

            Assert.AreEqual(CameraCommandResult.Accepted, controller.SelectView(DrivingView));
            Assert.AreEqual(drivingZoom, controller.ZoomOffset, OffsetTolerance);
            Assert.AreEqual(drivingHeight, controller.HeightOffset, OffsetTolerance);
            AssertPosition(new Vector3(0f, drivingHeight, -3f + drivingZoom), camera.transform.position);

            Assert.AreEqual(CameraCommandResult.Accepted, controller.SelectView(CameraTestRig.PresentationView));
            Assert.AreEqual(presentationZoom, controller.ZoomOffset, OffsetTolerance);
            Assert.AreEqual(presentationHeight, controller.HeightOffset, OffsetTolerance);
        }

        [Test]
        public void FirstVisit_UsesAuthoredDefaults()
        {
            VehicleViewEntry presentation;
            Assert.IsTrue(profile.TryGetView(CameraTestRig.PresentationView, out presentation));
            presentation.ConfigureDefaultPose(new OrbitPose(90f, -2f, 1.5f));
            VehicleViewEntry limited;
            Assert.IsTrue(profile.TryGetView(LimitedView, out limited));
            limited.ConfigureDefaultPose(new OrbitPose(0f, 3f, 6f));
            ActivateDriving();
            controller.SetHeldIntent(0f, 1f, 1f);
            RunFrames(10);
            controller.SetHeldIntent(0f, 0f, 0f);

            Assert.AreEqual(CameraCommandResult.Accepted, controller.SelectView(CameraTestRig.PresentationView));
            Assert.AreEqual(-2f, controller.ZoomOffset, OffsetTolerance);
            Assert.AreEqual(1.5f, controller.HeightOffset, OffsetTolerance);
            AssertPosition(new Vector3(0f, 1.5f, -5f), camera.transform.position);

            Assert.AreEqual(CameraCommandResult.Accepted, controller.SelectView(LimitedView));
            Assert.AreEqual(1f, controller.ZoomOffset, OffsetTolerance);
            Assert.AreEqual(4f, controller.HeightOffset, OffsetTolerance);
        }

        [Test]
        public void InteriorToDriving_UsesRearDefaultBearing_NotFront()
        {
            drivingView.ConfigureFrontDefaultPose(new OrbitPose(180f, 0f, 0f));
            controller = rig.CreateController("Controller", camera, rig.CreateTarget(root, profile), CameraTestRig.InteriorView);
            Assert.AreEqual(CameraCommandResult.Accepted, controller.Activate());
            AssertPosition(profile.Seat.EyeLocalPosition, camera.transform.position);

            Assert.AreEqual(CameraCommandResult.Accepted, controller.SelectView(DrivingView));
            AssertPosition(new Vector3(0f, 0f, -3f), camera.transform.position);

            controller.SetHeldIntent(1f, 0f, 0f);
            RunFrames(20);
            controller.SetHeldIntent(0f, 0f, 0f);
            Assert.Greater(Vector3.Distance(new Vector3(0f, 0f, -3f), camera.transform.position), 0.5f);
            Assert.AreEqual(CameraCommandResult.Accepted, controller.SelectView(CameraTestRig.FixedView));
            Assert.AreEqual(CameraCommandResult.Accepted, controller.SelectView(DrivingView));
            AssertPosition(new Vector3(0f, 0f, -3f), camera.transform.position);
        }

        [Test]
        public void SharedOrbit_TwoViews_SameOrbitDifferentPresets()
        {
            drivingView.ConfigureDefaultPose(new OrbitPose(45f, 0f, 0f));
            ActivateDriving();
            float drivingDistance = controller.OrbitDistance;
            Assert.AreEqual(AimFrame.Root, controller.AimFrame);
            AssertPosition(new Vector3(1f, 0f, -1f), camera.transform.position);

            Assert.AreEqual(CameraCommandResult.Accepted, controller.SelectView(CameraTestRig.PresentationView));

            AssertPosition(new Vector3(1f, 0f, -1f), camera.transform.position);
            Assert.AreEqual(drivingDistance, controller.OrbitDistance, 0.001f);
            Assert.AreSame(rig.PresentationPreset, controller.ActivePreset);
            Assert.AreEqual(AimFrame.OwnerBody, controller.AimFrame);
        }

        [Test]
        public void DifferentOrbits_CameraMovesToOtherCurve()
        {
            drivingView.ConfigureDefaultPose(new OrbitPose(90f, 0f, 0f));
            ActivateDriving();
            AssertPosition(new Vector3(1f, 0f, 0f), camera.transform.position);

            Assert.AreEqual(CameraCommandResult.Accepted, controller.SelectView(WideView));
            AssertPosition(new Vector3(2f, 0f, 0f), camera.transform.position);

            controller.SetHeldIntent(1f, 0f, 0f);
            RunFrames(40);
            Vector3 moved = camera.transform.position;
            Assert.Greater(Vector3.Distance(new Vector3(2f, 0f, 0f), moved), 1f);
            Assert.AreEqual(1f, Mathf.Max(Mathf.Abs(moved.x) / 2f, Mathf.Abs(moved.z) / 4f), PositionTolerance);
        }

        [Test]
        public void Snap_ArrivesSameFrame()
        {
            ActivateDriving();
            int commandId;

            Assert.AreEqual(CameraCommandResult.Accepted, controller.SelectView(WideView, new TransitionOptions(TransitionMode.Snap), out commandId));

            Assert.AreEqual(0, commandId);
            AssertPosition(new Vector3(0f, 0f, -4f), camera.transform.position);
            Assert.AreEqual(WideView, controller.ActiveView.Name);
            Assert.IsFalse(controller.IsSwitchingView);
            Assert.AreEqual(1, changedViewIds.Count);
            Assert.AreEqual(0, endedCommandIds.Count);
        }

        [Test]
        public void Duration_ArrivesOnTime()
        {
            ActivateDriving();
            int commandId;

            Assert.AreEqual(CameraCommandResult.Accepted, controller.SelectView(WideView, new TransitionOptions(TransitionMode.Duration, 1f), out commandId));
            Assert.Greater(commandId, 0);
            AssertPosition(new Vector3(0f, 0f, -3f), camera.transform.position);
            RunFrames(25);

            AssertPosition(new Vector3(0f, 0f, -3.5f), camera.transform.position);
            Assert.AreEqual(DrivingView, controller.ActiveView.Name);
            Assert.AreEqual(0, endedCommandIds.Count);
            RunFrames(25);

            AssertEndedWith(commandId, CommandEndResult.Completed);
            AssertPosition(new Vector3(0f, 0f, -4f), camera.transform.position);
            Assert.AreEqual(WideView, controller.ActiveView.Name);
        }

        [Test]
        public void PresetSpeed_UsesStraightLineSpeed()
        {
            drivingPreset.Travel.Configure(10f, 50f, 0.4f);
            widePreset.Travel.Configure(10f, 5f, 0.4f);
            ActivateDriving();
            int commandId;

            Assert.AreEqual(CameraCommandResult.Accepted, controller.SelectView(FarView, new TransitionOptions(TransitionMode.PresetSpeed), out commandId));
            RunFrames(45);

            AssertPosition(new Vector3(0f, 0f, -6.5f), camera.transform.position);
            Assert.AreEqual(0, endedCommandIds.Count);
            RunFrames(44);
            Assert.AreEqual(0, endedCommandIds.Count);
            RunFrames(1);
            AssertEndedWith(commandId, CommandEndResult.Completed);
            AssertPosition(new Vector3(0f, 0f, -10f), camera.transform.position);

            drivingPreset.Travel.Configure(10f, 10f, 0.4f);
            int returnId;
            Assert.AreEqual(CameraCommandResult.Accepted, controller.SelectView(DrivingView, new TransitionOptions(TransitionMode.Speed, 10f), out returnId));
            RunFrames(25);
            AssertPosition(new Vector3(0f, 0f, -7f), camera.transform.position);
        }

        [Test]
        public void UnknownView_ViewNotAvailable_NoChange()
        {
            ActivateDriving();
            profile.AddView("Missing Orbit", widePreset, 99999);
            VehicleOrbit crossedOrbit = profile.AddOrbit("Crossed");
            Vector3[] anchors = { new Vector3(-2f, 0f, -4f), new Vector3(2f, 0f, 4f), new Vector3(2f, 0f, -4f), new Vector3(-2f, 0f, 4f) };
            List<BezierOrbitKnot> crossedKnots = new List<BezierOrbitKnot>();
            for (int index = 0; index < anchors.Length; index++)
            {
                crossedKnots.Add(CreateKnot(anchors[index], anchors[(index + 3) % 4], anchors[(index + 1) % 4]));
            }

            crossedOrbit.Configure(crossedKnots, Quaternion.identity);
            crossedOrbit.ConfigureOffsetRanges(-1f, 4f, -4f, 1f);
            profile.AddWatchMarker(crossedOrbit, "Centre", 0.5f, new Vector3(0f, 1f, 0f), false);
            profile.AddView("Invalid Orbit", widePreset, crossedOrbit.Id);
            int commandId;
            Assert.AreEqual(CameraCommandResult.Accepted, controller.SelectView(WideView, new TransitionOptions(TransitionMode.Duration, 1f), out commandId));
            RunFrames(10);
            Vector3 position = camera.transform.position;
            Quaternion rotation = camera.transform.rotation;
            float distance = controller.OrbitDistance;
            int rejectedId;

            Assert.AreEqual(CameraCommandResult.ViewNotAvailable, controller.SelectView("Unknown", new TransitionOptions(TransitionMode.Snap), out rejectedId));
            Assert.AreEqual(0, rejectedId);
            Assert.AreEqual(CameraCommandResult.OrbitNotFound, controller.SelectView("Missing Orbit", new TransitionOptions(TransitionMode.Duration, 1f), out rejectedId));
            Assert.AreEqual(0, rejectedId);
            Assert.AreEqual(CameraCommandResult.ViewInvalid, controller.SelectView("Invalid Orbit",new TransitionOptions(TransitionMode.PresetSpeed), out rejectedId));
            Assert.AreEqual(0, rejectedId);

            AssertPosition(position, camera.transform.position);
            Assert.Less(Quaternion.Angle(rotation, camera.transform.rotation), 0.01f);
            Assert.AreEqual(distance, controller.OrbitDistance);
            Assert.AreEqual(DrivingView, controller.ActiveView.Name);
            Assert.IsTrue(controller.IsSwitchingView);
            Assert.AreEqual(0, changedViewIds.Count);
            Assert.AreEqual(0, endedCommandIds.Count);
            RunFrames(40);
            AssertEndedWith(commandId, CommandEndResult.Completed);
            AssertPosition(new Vector3(0f, 0f, -4f), camera.transform.position);
        }

        [Test]
        public void ViewChangedRaisedOnceWithNewViewId()
        {
            ActivateDriving();
            VehicleViewEntry wide;
            Assert.IsTrue(profile.TryGetView(WideView, out wide));
            int commandId;

            Assert.AreEqual(CameraCommandResult.Accepted, controller.SelectView(WideView, new TransitionOptions(TransitionMode.Duration, 0.5f), out commandId));
            RunFrames(10);
            Assert.AreEqual(0, changedViewIds.Count);
            RunFrames(30);

            Assert.AreEqual(1, changedViewIds.Count);
            Assert.AreEqual(wide.Id, changedViewIds[0]);
            AssertEndedWith(commandId, CommandEndResult.Completed);

            Assert.AreEqual(CameraCommandResult.Accepted, controller.SelectView(WideView));
            Assert.AreEqual(1, changedViewIds.Count);
            Assert.AreEqual(CameraCommandResult.Accepted, controller.SelectView(DrivingView));
            Assert.AreEqual(2, changedViewIds.Count);
            Assert.AreEqual(drivingView.Id, changedViewIds[1]);
        }

        [Test]
        public void MovingVehicle_TransitionStillEndsOnDestination()
        {
            ActivateDriving();
            int commandId;
            Assert.AreEqual(CameraCommandResult.Accepted, controller.SelectView(WideView, new TransitionOptions(TransitionMode.Duration, 1f), out commandId));

            for (int frame = 0; frame < 50; frame++)
            {
                root.rotation = Quaternion.Euler(0f, 30f * FrameTime, 0f) * root.rotation;
                root.position += root.forward * 20f * FrameTime;
                controller.UpdateCameraFrame(FrameTime);
                if (frame == 24)
                {
                    Vector3 local = root.InverseTransformPoint(camera.transform.position);
                    Assert.AreEqual(0f, local.x, PositionTolerance);
                    Assert.AreEqual(0f, local.y, PositionTolerance);
                    Assert.AreEqual(-3.5f, local.z, 0.01f);
                }
            }

            AssertEndedWith(commandId, CommandEndResult.Completed);
            AssertPosition(root.TransformPoint(new Vector3(0f, 0f, -4f)), camera.transform.position);
        }

        [Test]
        public void ManualInputDuringSwitch_Interrupted_DestinationViewActive()
        {
            ActivateDriving();
            VehicleViewEntry wide;
            Assert.IsTrue(profile.TryGetView(WideView, out wide));
            int commandId;
            Assert.AreEqual(CameraCommandResult.Accepted, controller.SelectView(WideView, new TransitionOptions(TransitionMode.Duration, 1f), out commandId));
            RunFrames(10);

            Assert.AreEqual(CameraCommandResult.Accepted, controller.SetHeldIntent(1f, 0f, 0f));

            AssertEndedWith(commandId, CommandEndResult.Interrupted);
            Assert.IsFalse(controller.IsSwitchingView);
            Assert.AreEqual(WideView, controller.ActiveView.Name);
            Assert.AreEqual(1, changedViewIds.Count);
            Assert.AreEqual(wide.Id, changedViewIds[0]);
            RunFrames(10);
            Vector3 moved = camera.transform.position;
            Assert.Greater(Vector3.Distance(new Vector3(0f, 0f, -4f), moved), 0.1f);
            Assert.AreEqual(1f, Mathf.Max(Mathf.Abs(moved.x) / 2f, Mathf.Abs(moved.z) / 4f), PositionTolerance);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (controller != null)
            {
                controller.ViewChanged -= RecordViewChanged;
                controller.CommandEnded -= RecordCommandEnded;
            }

            rig.Dispose();
            yield return null;
        }

        private VehicleOrbit AddRectangleOrbit(string orbitName, float halfWidth, float front, float rear)
        {
            VehicleOrbit orbit = profile.AddOrbit(orbitName);
            Vector3 rearRight = new Vector3(halfWidth, 0f, -rear);
            Vector3 rearLeft = new Vector3(-halfWidth, 0f, -rear);
            Vector3 frontLeft = new Vector3(-halfWidth, 0f, front);
            Vector3 frontRight = new Vector3(halfWidth, 0f, front);
            List<BezierOrbitKnot> knots = new List<BezierOrbitKnot>();
            knots.Add(CreateKnot(rearRight, frontRight, rearLeft));
            knots.Add(CreateKnot(rearLeft, rearRight, frontLeft));
            knots.Add(CreateKnot(frontLeft, rearLeft, frontRight));
            knots.Add(CreateKnot(frontRight, frontLeft, rearRight));
            orbit.Configure(knots, Quaternion.identity);
            orbit.ConfigureOffsetRanges(-1f, 4f, -4f, 1f);
            profile.AddWatchMarker(orbit, "Centre", 0.5f, new Vector3(0f, 1f, 0f), false);
            return orbit;
        }

        private BezierOrbitKnot CreateKnot(Vector3 anchor, Vector3 previous, Vector3 next)
        {
            BezierOrbitKnot knot = new BezierOrbitKnot();
            knot.Configure(anchor, anchor + (previous - anchor) / 3f, anchor + (next - anchor) / 3f);
            return knot;
        }

        private void ActivateDriving()
        {
            controller = rig.CreateController("Controller", camera, rig.CreateTarget(root, profile), DrivingView);
            controller.ViewChanged += RecordViewChanged;
            controller.CommandEnded += RecordCommandEnded;
            Assert.AreEqual(CameraCommandResult.Accepted, controller.Activate());
        }

        private void RunFrames(int count)
        {
            for (int frame = 0; frame < count; frame++)
            {
                controller.UpdateCameraFrame(FrameTime);
            }
        }

        private float GetBearing(Vector3 position)
        {
            return Mathf.Atan2(position.x, -position.z) * Mathf.Rad2Deg;
        }

        private void RecordViewChanged(int viewId)
        {
            changedViewIds.Add(viewId);
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
            Assert.AreEqual(1, endedCommandIds.Count);
        }

        private void AssertPosition(Vector3 expected, Vector3 actual)
        {
            Assert.Less(Vector3.Distance(expected, actual), PositionTolerance, $"Expected {expected:F4}, got {actual:F4}");
        }
    }
}
