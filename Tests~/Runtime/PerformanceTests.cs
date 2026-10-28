using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.TestTools;

namespace Gley.CameraSystem.Tests.PlayMode
{
    public class PerformanceTests
    {
        private const string DrivingView = "Driving";
        private const string GarbageSamplerName = "GC.Alloc";
        private const float FrameTime = 0.02f;
        private const float DrivingSpeed = 10f;
        private const float TurnHint = 0.6f;
        private const float InteriorCircleRadius = 30f;
        private const float InteriorStartSpeed = 8f;
        private const float InteriorAcceleration = 1.5f;
        private const float InteriorBumpHeight = 0.05f;
        private const float InteriorBumpRate = 12f;
        private const float PointTravelSpeed = 1f;
        private const float BarrierSpacing = 8f;
        private const float BarrierThickness = 0.3f;
        private const float BarrierTop = 0.6f;
        private const float TrailerSpacing = 7f;
        private const float RebuildBudgetMilliseconds = 1f;
        private const int WarmUpFrames = 30;
        private const int MeasuredFrames = 120;
        private const int DriveFrames = 300;
        private const int BarrierCount = 8;
        private const int ChainBodyCount = 5;
        private const int RebuildCount = 20;
        private const int ClearPoseCasts = 2;

        private readonly List<Collider> obstacles = new List<Collider>();
        private readonly PerformanceLogFile logFile = new PerformanceLogFile();
        private readonly Stopwatch operationStopwatch = new Stopwatch();

        private CameraTestRig rig;
        private CameraSystemController controller;
        private VehicleCameraTarget target;
        private Camera camera;
        private Transform root;
        private int endedCommandCount;
        private int frameIndex;
        private int maximumCasts;
        private int blockedFrames;

        [SetUp]
        public void SetUp()
        {
            rig = new CameraTestRig();
            endedCommandCount = 0;
            frameIndex = 0;
            maximumCasts = 0;
            blockedFrames = 0;
        }

        [Test]
        public void FixedView_SteadyState_NoGarbage()
        {
            CreateCamera(rig.CreateProfile(1f, 3f, 3f), CameraTestRig.FixedView);

            RunDrivingFrames(WarmUpFrames);
            int markerSamples;
            int garbageSamples = MeasureGarbage(out markerSamples);

            Assert.AreEqual(0, garbageSamples);
            Assert.AreEqual(MeasuredFrames, markerSamples);
        }

        [Test]
        public void InteriorView_AllModules_NoGarbage()
        {
            CreateCamera(rig.CreateProfile(1f, 3f, 3f), CameraTestRig.InteriorView);
            InteriorSettings interior = rig.InteriorPreset.Interior;
            Assert.IsTrue(interior.TurnLookEnabled);
            Assert.IsTrue(interior.AccelerationBrakingEnabled);
            Assert.IsTrue(interior.CorneringEnabled);
            Assert.IsTrue(interior.CushionEnabled);

            RunInteriorFrames(WarmUpFrames);
            int markerSamples;
            int garbageSamples = MeasureGarbage(out markerSamples);

            Assert.AreEqual(0, garbageSamples);
            Assert.AreEqual(MeasuredFrames, markerSamples);
            Assert.AreNotEqual(0f, controller.TurnLookOffset);
        }

        [Test]
        public void DrivingView_SpeedDistanceTurnLookAndCollision_NoGarbage()
        {
            CameraViewPreset drivingPreset = CreateDrivingCamera();
            Assert.IsTrue(drivingPreset.Driving.SpeedDistanceEnabled);
            Assert.IsTrue(drivingPreset.Driving.TurnLookEnabled);
            Assert.IsTrue(drivingPreset.Collision.Enabled);
            CreateBarriers();
            target.SetTurnHint(TurnHint);

            RunDrivingFrames(WarmUpFrames);
            blockedFrames = 0;
            int markerSamples;
            int garbageSamples = MeasureGarbage(out markerSamples);

            Assert.AreEqual(0, garbageSamples);
            Assert.AreEqual(MeasuredFrames, markerSamples);
            Assert.Greater(blockedFrames, 0);
            Assert.AreNotEqual(0f, controller.TurnLookOffset);
        }

        [Test]
        public void PresentationView_PointOfInterestTravel_NoGarbage()
        {
            rig.PresentationPreset.Travel.Configure(PointTravelSpeed, rig.PresentationPreset.Travel.StraightLineTransitionSpeed, rig.PresentationPreset.Travel.EaseTime);
            CreateCamera(rig.CreateProfile(1f, 3f, 3f), CameraTestRig.PresentationView);
            controller.CommandEnded += RecordCommandEnded;
            int commandId;
            Assert.AreEqual(CameraCommandResult.Accepted, controller.RequestPoint("Right", new TravelRequest(TravelDirection.Shortest), out commandId));
            Assert.AreNotEqual(0, commandId);

            RunStationaryFrames(WarmUpFrames);
            Vector3 startPosition = camera.transform.position;
            int markerSamples;
            int garbageSamples = MeasureGarbage(out markerSamples);

            Assert.AreEqual(0, garbageSamples);
            Assert.AreEqual(MeasuredFrames, markerSamples);
            Assert.AreEqual(0, endedCommandCount);
            Assert.Greater(Vector3.Distance(startPosition, camera.transform.position), 0.5f);
        }

        [Test]
        public void DrivePastObstacles_CastsNeverExceedPresetMaximum()
        {
            CameraViewPreset drivingPreset = CreateDrivingCamera();
            CreateBarriers();
            target.SetTurnHint(TurnHint);
            int presetMaximum = drivingPreset.Collision.MaximumCasts;

            for (int frame = 0; frame < DriveFrames; frame++)
            {
                AdvanceDriving();
                controller.UpdateCameraFrame(FrameTime);
                int casts = controller.CollisionCastsThisFrame;

                Assert.LessOrEqual(casts, presetMaximum);
                RecordCasts(casts);
            }

            Assert.Greater(blockedFrames, 0);
            Assert.Greater(maximumCasts, ClearPoseCasts);
        }

        [Test]
        [Category("Performance")]
        public void OrbitRebuild_FiveBodyChain_MedianUnderOneMillisecond()
        {
            VehicleProfile profile = rig.CreateProfile(1f, 3f, 3f);
            Transform truck = rig.CreateBody("Truck");
            VehicleCameraTarget chainTarget = rig.CreateTarget(truck, profile);
            for (int index = 1; index < ChainBodyCount; index++)
            {
                Transform trailer = rig.CreateBody($"Trailer {index}");
                trailer.position = new Vector3(0f, 0f, -TrailerSpacing * index);
                Assert.AreEqual(VehicleTargetResult.Accepted, chainTarget.Attach(trailer, rig.CreateProfile(1f, 3f, 3f), ChainEnd.Rear));
            }

            Assert.AreEqual(ChainBodyCount, chainTarget.BodyCount);
            ChainOrbitBuilder builder = new ChainOrbitBuilder();
            ChainOrbit warmUpOrbit = builder.Build(chainTarget, CameraTestRig.OrbitName);
            Assert.IsNotNull(warmUpOrbit);
            Assert.AreEqual(ChainOrbitAssemblyResult.Valid, warmUpOrbit.AssemblyResult);

            double[] milliseconds = new double[RebuildCount];
            Stopwatch stopwatch = new Stopwatch();
            for (int index = 0; index < RebuildCount; index++)
            {
                stopwatch.Restart();
                ChainOrbit rebuilt = builder.Build(chainTarget, CameraTestRig.OrbitName);
                stopwatch.Stop();
                milliseconds[index] = stopwatch.Elapsed.TotalMilliseconds;
                Assert.IsNotNull(rebuilt);
            }

            System.Array.Sort(milliseconds);
            double median = (milliseconds[RebuildCount / 2 - 1] + milliseconds[RebuildCount / 2]) * 0.5;
            logFile.Append($"PerformanceTests: five-body orbit rebuild median {median:F4} ms (min {milliseconds[0]:F4} ms, max {milliseconds[RebuildCount - 1]:F4} ms) over {RebuildCount} rebuilds.");

            Assert.Less(median, RebuildBudgetMilliseconds);
        }

        [Test]
        [Category("Performance")]
        public void Activate_FiveBodyChain_UnderOneMillisecond()
        {
            VehicleCameraTarget chainTarget = CreateChainTarget("Chain", ChainBodyCount);
            controller = rig.CreateController("Controller", rig.CreateCamera("Camera"), chainTarget, CameraTestRig.PresentationView);
            Assert.AreEqual(CameraCommandResult.Accepted, controller.Activate());
            controller.Deactivate();

            double[] milliseconds = new double[RebuildCount];
            for (int index = 0; index < RebuildCount; index++)
            {
                operationStopwatch.Restart();
                CameraCommandResult result = controller.Activate();
                operationStopwatch.Stop();
                milliseconds[index] = operationStopwatch.Elapsed.TotalMilliseconds;
                Assert.AreEqual(CameraCommandResult.Accepted, result);
                controller.Deactivate();
            }

            double median = LogMedian("five-body snap Activate (Presentation view)", milliseconds);

            Assert.Less(median, RebuildBudgetMilliseconds);
        }

        [Test]
        [Category("Performance")]
        public void SetTarget_FiveBodyChain_UnderOneMillisecond()
        {
            VehicleCameraTarget firstTarget = CreateChainTarget("First Chain", ChainBodyCount);
            VehicleCameraTarget secondTarget = CreateChainTarget("Second Chain", ChainBodyCount);
            controller = rig.CreateController("Controller", rig.CreateCamera("Camera"), firstTarget, CameraTestRig.PresentationView);
            Assert.AreEqual(CameraCommandResult.Accepted, controller.Activate());
            Assert.AreEqual(CameraCommandResult.Accepted, controller.SetTarget(secondTarget));

            double[] milliseconds = new double[RebuildCount];
            for (int index = 0; index < RebuildCount; index++)
            {
                VehicleCameraTarget nextTarget = firstTarget;
                if (index % 2 == 1)
                {
                    nextTarget = secondTarget;
                }

                operationStopwatch.Restart();
                CameraCommandResult result = controller.SetTarget(nextTarget);
                operationStopwatch.Stop();
                milliseconds[index] = operationStopwatch.Elapsed.TotalMilliseconds;
                Assert.AreEqual(CameraCommandResult.Accepted, result);
                Assert.AreSame(nextTarget, controller.Target);
            }

            double median = LogMedian("five-body snap SetTarget", milliseconds);

            Assert.Less(median, RebuildBudgetMilliseconds);
        }

        [Test]
        [Category("Performance")]
        public void AttachDetach_FiveBodyChain_UnderOneMillisecond()
        {
            VehicleCameraTarget chainTarget = CreateChainTarget("Chain", ChainBodyCount - 1);
            Transform lastTrailer = rig.CreateBody("Last Trailer");
            lastTrailer.position = new Vector3(0f, 0f, -TrailerSpacing * (ChainBodyCount - 1));
            VehicleProfile lastProfile = rig.CreateProfile(1f, 3f, 3f);
            controller = rig.CreateController("Controller", rig.CreateCamera("Camera"), chainTarget, CameraTestRig.PresentationView);
            Assert.AreEqual(CameraCommandResult.Accepted, controller.Activate());
            Assert.AreEqual(VehicleTargetResult.Accepted, chainTarget.Attach(lastTrailer, lastProfile, ChainEnd.Rear));
            Assert.AreEqual(VehicleTargetResult.Accepted, chainTarget.Detach(lastTrailer));

            double[] attachMilliseconds = new double[RebuildCount];
            double[] detachMilliseconds = new double[RebuildCount];
            for (int index = 0; index < RebuildCount; index++)
            {
                operationStopwatch.Restart();
                VehicleTargetResult attachResult = chainTarget.Attach(lastTrailer, lastProfile, ChainEnd.Rear);
                operationStopwatch.Stop();
                attachMilliseconds[index] = operationStopwatch.Elapsed.TotalMilliseconds;
                Assert.AreEqual(VehicleTargetResult.Accepted, attachResult);
                Assert.AreEqual(ChainBodyCount, chainTarget.BodyCount);

                operationStopwatch.Restart();
                VehicleTargetResult detachResult = chainTarget.Detach(lastTrailer);
                operationStopwatch.Stop();
                detachMilliseconds[index] = operationStopwatch.Elapsed.TotalMilliseconds;
                Assert.AreEqual(VehicleTargetResult.Accepted, detachResult);
                Assert.AreEqual(ChainBodyCount - 1, chainTarget.BodyCount);
            }

            double attachMedian = LogMedian("attach fifth body with active camera", attachMilliseconds);
            double detachMedian = LogMedian("detach fifth body with active camera", detachMilliseconds);

            Assert.IsTrue(controller.IsActive);
            Assert.Less(attachMedian, RebuildBudgetMilliseconds);
            Assert.Less(detachMedian, RebuildBudgetMilliseconds);
        }

        [Test]
        [Category("Performance")]
        public void SelectView_SameOrbit_UnderOneMillisecond()
        {
            VehicleCameraTarget chainTarget = CreateChainTarget("Chain", ChainBodyCount);
            AddDrivingView(chainTarget.GetProfile(chainTarget.RootIndex));
            controller = rig.CreateController("Controller", rig.CreateCamera("Camera"), chainTarget, CameraTestRig.PresentationView);
            Assert.AreEqual(CameraCommandResult.Accepted, controller.Activate());
            Assert.AreEqual(CameraCommandResult.Accepted, controller.SelectView(DrivingView));
            Assert.AreEqual(CameraCommandResult.Accepted, controller.SelectView(CameraTestRig.PresentationView));

            double[] milliseconds = new double[RebuildCount];
            for (int index = 0; index < RebuildCount; index++)
            {
                string viewName = DrivingView;
                if (index % 2 == 1)
                {
                    viewName = CameraTestRig.PresentationView;
                }

                operationStopwatch.Restart();
                CameraCommandResult result = controller.SelectView(viewName);
                operationStopwatch.Stop();
                milliseconds[index] = operationStopwatch.Elapsed.TotalMilliseconds;
                Assert.AreEqual(CameraCommandResult.Accepted, result);
                Assert.AreEqual(viewName, controller.ActiveView.Name);
            }

            double median = LogMedian("five-body snap SelectView between two views on the same orbit", milliseconds);

            Assert.Less(median, RebuildBudgetMilliseconds);
        }

        private void CreateCamera(VehicleProfile profile, string viewName)
        {
            root = rig.CreateBody("Truck");
            target = rig.CreateTarget(root, profile);
            camera = rig.CreateCamera("Camera");
            controller = rig.CreateController("Controller", camera, target, viewName);
            Assert.AreEqual(CameraCommandResult.Accepted, controller.Activate());
        }

        private void RunDrivingFrames(int count)
        {
            for (int frame = 0; frame < count; frame++)
            {
                AdvanceDriving();
                controller.UpdateCameraFrame(FrameTime);
                RecordCasts(controller.CollisionCastsThisFrame);
            }
        }

        private void AdvanceDriving()
        {
            frameIndex++;
            root.position = new Vector3(0f, 0f, DrivingSpeed * FrameTime * frameIndex);
        }

        private void RecordCasts(int casts)
        {
            if (casts > maximumCasts)
            {
                maximumCasts = casts;
            }

            if (casts > ClearPoseCasts)
            {
                blockedFrames++;
            }
        }

        private int MeasureGarbage(out int markerSamples)
        {
            CameraViewType viewType = controller.ActivePreset.ViewType;
            Recorder frameRecorder = Recorder.Get(CameraProfilerMarkers.UpdateCameraFrameName);
            Recorder garbageRecorder = Recorder.Get(GarbageSamplerName);
            frameRecorder.enabled = false;
            garbageRecorder.enabled = false;
            frameRecorder.FilterToCurrentThread();
            garbageRecorder.FilterToCurrentThread();
            frameRecorder.enabled = true;
            garbageRecorder.enabled = true;
            if (viewType == CameraViewType.Interior)
            {
                RunInteriorFrames(MeasuredFrames);
            }
            else if (viewType == CameraViewType.Presentation)
            {
                RunStationaryFrames(MeasuredFrames);
            }
            else
            {
                RunDrivingFrames(MeasuredFrames);
            }

            garbageRecorder.enabled = false;
            frameRecorder.enabled = false;
            garbageRecorder.CollectFromAllThreads();
            frameRecorder.CollectFromAllThreads();
            markerSamples = frameRecorder.sampleBlockCount;
            return garbageRecorder.sampleBlockCount;
        }

        private void RunInteriorFrames(int count)
        {
            for (int frame = 0; frame < count; frame++)
            {
                frameIndex++;
                float time = FrameTime * frameIndex;
                float distance = InteriorStartSpeed * time + 0.5f * InteriorAcceleration * time * time;
                float angle = distance / InteriorCircleRadius;
                float bump = InteriorBumpHeight * Mathf.Sin(InteriorBumpRate * time);
                Vector3 position = new Vector3(InteriorCircleRadius * (1f - Mathf.Cos(angle)), bump, InteriorCircleRadius * Mathf.Sin(angle));
                root.SetPositionAndRotation(position, Quaternion.Euler(0f, angle * Mathf.Rad2Deg, 0f));
                controller.UpdateCameraFrame(FrameTime);
            }
        }

        private void RunStationaryFrames(int count)
        {
            for (int frame = 0; frame < count; frame++)
            {
                controller.UpdateCameraFrame(FrameTime);
            }
        }

        private CameraViewPreset CreateDrivingCamera()
        {
            VehicleProfile profile = rig.CreateProfile(1f, 3f, 3f);
            VehicleOrbit orbit;
            Assert.IsTrue(profile.TryGetOrbit(CameraTestRig.OrbitName, out orbit));
            CameraViewPreset drivingPreset = rig.CreatePreset(CameraViewType.Driving);
            profile.AddView(DrivingView, drivingPreset, orbit.Id);
            CreateCamera(profile, DrivingView);
            return drivingPreset;
        }

        private void CreateBarriers()
        {
            for (int index = 1; index <= BarrierCount; index++)
            {
                float barrierStart = index * BarrierSpacing;
                CreateBox(new Vector3(-5f, -2f, barrierStart), new Vector3(5f, BarrierTop, barrierStart + BarrierThickness));
            }

            Physics.SyncTransforms();
        }

        private void CreateBox(Vector3 minimum, Vector3 maximum)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = "Performance Test Barrier";
            box.transform.position = (minimum + maximum) * 0.5f;
            box.transform.localScale = maximum - minimum;
            obstacles.Add(box.GetComponent<Collider>());
        }

        private void RecordCommandEnded(int commandId, CommandEndResult result)
        {
            endedCommandCount++;
        }

        private VehicleCameraTarget CreateChainTarget(string chainName, int bodyCount)
        {
            Transform truck = rig.CreateBody($"{chainName} Truck");
            VehicleCameraTarget chainTarget = rig.CreateTarget(truck, rig.CreateProfile(1f, 3f, 3f));
            for (int index = 1; index < bodyCount; index++)
            {
                Transform trailer = rig.CreateBody($"{chainName} Trailer {index}");
                trailer.position = new Vector3(0f, 0f, -TrailerSpacing * index);
                Assert.AreEqual(VehicleTargetResult.Accepted, chainTarget.Attach(trailer, rig.CreateProfile(1f, 3f, 3f), ChainEnd.Rear));
            }

            Assert.AreEqual(bodyCount, chainTarget.BodyCount);
            return chainTarget;
        }

        private double LogMedian(string label, double[] samples)
        {
            System.Array.Sort(samples);
            double median = (samples[samples.Length / 2 - 1] + samples[samples.Length / 2]) * 0.5;
            logFile.Append($"PerformanceTests: {label}: median {median:F4} ms (min {samples[0]:F4} ms, max {samples[samples.Length - 1]:F4} ms) over {samples.Length} runs.");
            return median;
        }

        private void AddDrivingView(VehicleProfile profile)
        {
            VehicleOrbit orbit;
            Assert.IsTrue(profile.TryGetOrbit(CameraTestRig.OrbitName, out orbit));
            profile.AddView(DrivingView, rig.CreatePreset(CameraViewType.Driving), orbit.Id);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (controller != null)
            {
                controller.CommandEnded -= RecordCommandEnded;
            }

            for (int index = 0; index < obstacles.Count; index++)
            {
                if (obstacles[index] != null)
                {
                    Object.Destroy(obstacles[index].gameObject);
                }
            }

            obstacles.Clear();
            rig.Dispose();
            yield return null;
            Physics.SyncTransforms();
        }
    }
}
