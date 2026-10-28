using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Gley.CameraSystem.Tests.PlayMode
{
    public class PlayerPreferencesPlayModeTests
    {
        private const string DrivingView = "Driving";
        private const string RenamedDrivingView = "Chase";
        private const float FrameTime = 0.02f;
        private const float OrbitLength = 16f;
        private const float PositionTolerance = 0.001f;
        private const float OffsetTolerance = 0.0001f;
        private const float DistanceTolerance = 0.001f;
        private const float SpeedTolerance = 0.01f;
        private const float MovementThreshold = 0.01f;
        private const float MovingSpeed = 10f;
        private const float RecenterDelay = 4f;
        private const int JustBeforeDelayFrames = 190;
        private const int JustAfterDelayFrames = 20;
        private const int SettleFrames = 100;
        private const int MaximumFrames = 1000;

        private readonly List<CameraSystemController> subscribedControllers = new List<CameraSystemController>();
        private readonly List<int> endedCommandIds = new List<int>();
        private readonly List<CommandEndResult> endedResults = new List<CommandEndResult>();

        private CameraTestRig rig;
        private CameraSystemController controller;
        private VehicleProfile profile;
        private VehicleViewEntry drivingView;
        private CameraViewPreset drivingPreset;
        private VehicleCameraTarget target;
        private Camera camera;

        [SetUp]
        public void SetUp()
        {
            rig = new CameraTestRig();
            subscribedControllers.Clear();
            endedCommandIds.Clear();
            endedResults.Clear();
            controller = null;
            Transform root = rig.CreateBody("Truck");
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
        public void GetThenApplyOnNewCamera_RestoresRearAndFrontDefaults()
        {
            CreateAndActivate("Controller A", DrivingView, null);
            MoveToRightSide();
            AssertPosition(new Vector3(2f, 1f, -2f));
            Assert.AreEqual(CameraCommandResult.Accepted, controller.SaveCurrentPoseAsDefault(DefaultSlot.Rear));
            EnterReverseAndArrive();
            AssertPosition(new Vector3(0f, 0f, 3f));
            Assert.AreEqual(CameraCommandResult.Accepted, controller.AddDrag(new Vector2(0.1f, 0.5f)));
            Assert.AreEqual(CameraCommandResult.Accepted, controller.AddPinch(-0.4f));
            RunFrames(SettleFrames);
            AssertPosition(new Vector3(-3f, 2f, 2f));
            Assert.AreEqual(CameraCommandResult.Accepted, controller.SaveCurrentPoseAsDefault(DefaultSlot.Front));
            string json = JsonUtility.ToJson(controller.GetPlayerPreferences());
            controller.Deactivate();

            camera = rig.CreateCamera("Camera B");
            CreateAndActivate("Controller B", DrivingView, JsonUtility.FromJson<CameraPlayerPreferences>(json));
            RunFrames(SettleFrames);
            AssertPosition(new Vector3(0f, 0f, -3f));

            ResetAndArrive();
            AssertPosition(new Vector3(2f, 1f, -2f));
            Assert.AreEqual(-1f, controller.ZoomOffset, OffsetTolerance);
            Assert.AreEqual(1f, controller.HeightOffset, OffsetTolerance);

            EnterReverseAndArrive();
            AssertPosition(new Vector3(-3f, 2f, 2f));
            Assert.AreEqual(-2f, controller.ZoomOffset, OffsetTolerance);
            Assert.AreEqual(2f, controller.HeightOffset, OffsetTolerance);
        }

        [Test]
        public void Apply_UnknownViewId_SkippedNoError()
        {
            CameraPlayerPreferences preferences = new CameraPlayerPreferences();
            preferences.AddViewEntry(new ViewPreferenceEntry(profile.ProfileId, drivingView.Id + 1000, true, new OrbitPose(90f, -1f, 1f), true, new OrbitPose(180f, -2f, 2f)));
            preferences.AddViewEntry(new ViewPreferenceEntry("missing-profile", drivingView.Id, true, new OrbitPose(90f, -1f, 1f), false, default));

            CreateAndActivate("Controller", DrivingView, preferences);
            MoveToRightSide();
            AssertPosition(new Vector3(2f, 1f, -2f));
            ResetAndArrive();

            AssertPosition(new Vector3(0f, 0f, -3f));
            Assert.AreEqual(0f, controller.ZoomOffset, OffsetTolerance);
            Assert.AreEqual(0f, controller.HeightOffset, OffsetTolerance);
            Assert.AreEqual(2, controller.GetPlayerPreferences().ViewEntries.Count);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Apply_ViewRenamed_StillRestoredById()
        {
            CreateAndActivate("Controller A", DrivingView, null);
            MoveToRightSide();
            AssertPosition(new Vector3(2f, 1f, -2f));
            Assert.AreEqual(CameraCommandResult.Accepted, controller.SaveCurrentPoseAsDefault(DefaultSlot.Rear));
            string json = JsonUtility.ToJson(controller.GetPlayerPreferences());
            controller.Deactivate();

            JsonUtility.FromJsonOverwrite("{\"name\":\"" + RenamedDrivingView + "\"}", drivingView);
            VehicleViewEntry renamedView;
            Assert.IsFalse(profile.TryGetView(DrivingView, out renamedView));
            Assert.IsTrue(profile.TryGetView(RenamedDrivingView, out renamedView));
            Assert.AreSame(drivingView, renamedView);

            camera = rig.CreateCamera("Camera B");
            CreateAndActivate("Controller B", RenamedDrivingView, JsonUtility.FromJson<CameraPlayerPreferences>(json));
            RunFrames(SettleFrames);
            AssertPosition(new Vector3(0f, 0f, -3f));
            ResetAndArrive();

            AssertPosition(new Vector3(2f, 1f, -2f));
            Assert.AreEqual(-1f, controller.ZoomOffset, OffsetTolerance);
            Assert.AreEqual(1f, controller.HeightOffset, OffsetTolerance);
        }

        [Test]
        public void Apply_NewerFormat_RejectedWithWarning()
        {
            CameraPlayerPreferences preferences = new CameraPlayerPreferences();
            preferences.SetSensitivity(2f, 2f);
            preferences.SetCushionIntensity(0.5f);
            preferences.AddViewEntry(new ViewPreferenceEntry(profile.ProfileId, drivingView.Id, true, new OrbitPose(90f, -1f, 1f), false, default));
            string json = JsonUtility.ToJson(preferences);
            StringAssert.Contains("\"formatVersion\":1", json);
            CameraPlayerPreferences newer = JsonUtility.FromJson<CameraPlayerPreferences>(json.Replace("\"formatVersion\":1", "\"formatVersion\":2"));
            Assert.AreEqual(2, newer.FormatVersion);

            LogAssert.Expect(LogType.Warning, new Regex("format version 2 is newer"));
            CreateAndActivate("Controller", DrivingView, newer);

            Assert.AreEqual(1f, controller.OrbitSensitivity, OffsetTolerance);
            Assert.AreEqual(1f, controller.LookSensitivity, OffsetTolerance);
            Assert.AreEqual(1f, controller.CushionIntensity, OffsetTolerance);
            Assert.AreEqual(0, controller.GetPlayerPreferences().ViewEntries.Count);
            MoveToRightSide();
            AssertPosition(new Vector3(2f, 1f, -2f));
            ResetAndArrive();
            AssertPosition(new Vector3(0f, 0f, -3f));
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void RecenterOverride_PersistentStopsTimedRecenter()
        {
            drivingPreset.Recenter.Configure(RecenterMode.Timed, RecenterDelay, 0.25f, 0.5f);
            target.SetSuppliedSpeed(MovingSpeed);
            CreateAndActivate("Controller", DrivingView, null);
            float restDistance = controller.OrbitDistance;
            controller.SetRecenterOverride(RecenterMode.Persistent, 0f);
            Assert.AreEqual(RecenterModeOverride.Persistent, controller.GetPlayerPreferences().RecenterModeOverride);
            Assert.AreEqual(CameraCommandResult.Accepted, controller.AddDrag(new Vector2(0.1f, 0f)));
            float movedDistance = controller.OrbitDistance;
            Assert.AreEqual(2f, WrappedDifference(movedDistance, restDistance), DistanceTolerance);

            RunFrames(MaximumFrames);
            AssertAtDistance(movedDistance);
            Assert.AreEqual(0, endedCommandIds.Count);

            controller.ClearRecenterOverride();
            Assert.AreEqual(RecenterModeOverride.UsePreset, controller.GetPlayerPreferences().RecenterModeOverride);
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
        public void OrbitSensitivityTwo_DoublesManualTravelAndDrag()
        {
            CreateAndActivate("Controller", CameraTestRig.PresentationView, null);
            controller.SetSensitivity(2f, 1f);
            Assert.AreEqual(2f, controller.OrbitSensitivity, OffsetTolerance);
            Assert.AreEqual(1f, controller.LookSensitivity, OffsetTolerance);
            float startDistance = controller.OrbitDistance;

            Assert.AreEqual(CameraCommandResult.Accepted, controller.AddDrag(new Vector2(0.05f, 0.05f)));
            Assert.AreEqual(CameraCommandResult.Accepted, controller.AddPinch(-0.05f));
            Assert.AreEqual(2f, Mathf.Abs(controller.OrbitDistance - startDistance), DistanceTolerance);
            Assert.AreEqual(0.4f, controller.HeightOffset, OffsetTolerance);
            Assert.AreEqual(-0.5f, controller.ZoomOffset, OffsetTolerance);

            Assert.AreEqual(CameraCommandResult.Accepted, controller.SetHeldIntent(1f, 0f, 0f));
            RunFrames(50);
            Assert.AreEqual(12f, Mathf.Abs(controller.CurrentOrbitTravelSpeed), SpeedTolerance);
            float heldDistance = controller.OrbitDistance;
            RunFrames(10);
            Assert.AreEqual(2.4f, Mathf.Abs(controller.OrbitDistance - heldDistance), SpeedTolerance);

            Assert.AreEqual(CameraCommandResult.Accepted, controller.SetHeldIntent(0f, 1f, -1f));
            RunFrames(5);
            controller.SetHeldIntent(0f, 0f, 0f);
            Assert.AreEqual(0.8f, controller.HeightOffset, OffsetTolerance);
            Assert.AreEqual(-1.1f, controller.ZoomOffset, OffsetTolerance);

            controller.SetSensitivity(10f, 0.1f);
            Assert.AreEqual(4f, controller.OrbitSensitivity, OffsetTolerance);
            Assert.AreEqual(0.25f, controller.LookSensitivity, OffsetTolerance);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            for (int index = 0; index < subscribedControllers.Count; index++)
            {
                if (subscribedControllers[index] != null)
                {
                    subscribedControllers[index].CommandEnded -= RecordCommandEnded;
                }
            }

            subscribedControllers.Clear();
            rig.Dispose();
            yield return null;
        }

        private void CreateAndActivate(string controllerName, string viewName, CameraPlayerPreferences preferences)
        {
            controller = rig.CreateController(controllerName, camera, target, viewName);
            controller.CommandEnded += RecordCommandEnded;
            subscribedControllers.Add(controller);
            if (preferences != null)
            {
                controller.ApplyPlayerPreferences(preferences);
            }

            Assert.AreEqual(CameraCommandResult.Accepted, controller.Activate());
        }

        private void MoveToRightSide()
        {
            Assert.AreEqual(CameraCommandResult.Accepted, controller.AddDrag(new Vector2(0.1f, 0.25f)));
            Assert.AreEqual(CameraCommandResult.Accepted, controller.AddPinch(-0.2f));
            RunFrames(SettleFrames);
        }

        private void ResetAndArrive()
        {
            int commandId;
            Assert.AreEqual(CameraCommandResult.Accepted, controller.FullReset(CommandSource.Player, out commandId));
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

        private void AssertEndedWith(int commandId, CommandEndResult result)
        {
            int index = endedCommandIds.IndexOf(commandId);
            Assert.GreaterOrEqual(index, 0, $"Command {commandId} did not end.");
            Assert.AreEqual(result, endedResults[index]);
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

        private void AssertPosition(Vector3 expected)
        {
            Vector3 actual = camera.transform.position;
            Assert.Less(Vector3.Distance(expected, actual), PositionTolerance, $"Expected {expected:F4}, got {actual:F4}");
        }
    }
}
