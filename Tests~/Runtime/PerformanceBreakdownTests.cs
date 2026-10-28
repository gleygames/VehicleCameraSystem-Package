using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Gley.CameraSystem.Tests.PlayMode
{
    [Category("Performance")]
    public class PerformanceBreakdownTests
    {
        private const string DrivingView = "Driving";
        private const float TrailerSpacing = 7f;
        private const int ChainBodyCount = 5;
        private const int SampleCount = 20;

        private readonly List<VehicleProfile> profiles = new List<VehicleProfile>();
        private readonly List<Transform> bodies = new List<Transform>();
        private readonly double[] milliseconds = new double[SampleCount];
        private readonly double[] secondMilliseconds = new double[SampleCount];
        private readonly Stopwatch stopwatch = new Stopwatch();
        private readonly PerformanceLogFile logFile = new PerformanceLogFile();

        private CameraTestRig rig;
        private CameraSystemController controller;

        [SetUp]
        public void SetUp()
        {
            rig = new CameraTestRig();
        }

        [Test]
        public void ClosedBezierOrbit_Construction_LogsMedian()
        {
            VehicleOrbit orbit;
            Assert.IsTrue(rig.CreateProfile(1f, 3f, 3f).TryGetOrbit(CameraTestRig.OrbitName, out orbit));
            Assert.IsTrue(new ClosedBezierOrbit(orbit).IsClosed);

            for (int index = 0; index < SampleCount; index++)
            {
                stopwatch.Restart();
                ClosedBezierOrbit built = new ClosedBezierOrbit(orbit);
                stopwatch.Stop();
                milliseconds[index] = stopwatch.Elapsed.TotalMilliseconds;
                Assert.IsTrue(built.IsClosed);
            }

            LogMedian("ClosedBezierOrbit construction (one body)", milliseconds);
        }

        [Test]
        public void ChainLayout_FiveBodies_LogsMedian()
        {
            VehicleCameraTarget target = CreateChainTarget("Chain", ChainBodyCount);
            CollectChain(target);
            VehicleOrbit rootOrbit = GetRootOrbit(target);
            Assert.AreEqual(ChainLayoutResult.Valid, new ChainLayout(profiles, target.RootIndex, rootOrbit).LayoutResult);

            for (int index = 0; index < SampleCount; index++)
            {
                stopwatch.Restart();
                ChainLayout layout = new ChainLayout(profiles, target.RootIndex, rootOrbit);
                stopwatch.Stop();
                milliseconds[index] = stopwatch.Elapsed.TotalMilliseconds;
                Assert.AreEqual(ChainLayoutResult.Valid, layout.LayoutResult);
            }

            LogMedian("ChainLayout construction (five bodies, includes its validation assembly)", milliseconds);
        }

        [Test]
        public void ChainOrbitAssembly_FiveBodies_LogsMedian()
        {
            VehicleCameraTarget target = CreateChainTarget("Chain", ChainBodyCount);
            CollectChain(target);
            ChainLayout layout = new ChainLayout(profiles, target.RootIndex, GetRootOrbit(target));
            Assert.AreEqual(ChainOrbitAssemblyResult.Valid, new ChainOrbit(layout, bodies, target.RootIndex).AssemblyResult);

            for (int index = 0; index < SampleCount; index++)
            {
                stopwatch.Restart();
                ChainOrbit orbit = new ChainOrbit(layout, bodies, target.RootIndex);
                stopwatch.Stop();
                milliseconds[index] = stopwatch.Elapsed.TotalMilliseconds;
                Assert.AreEqual(ChainOrbitAssemblyResult.Valid, orbit.AssemblyResult);
            }

            LogMedian("ChainOrbit assembly only (five bodies)", milliseconds);
        }

        [Test]
        public void ChainOrbitBuilder_OneToFiveBodies_LogsMedians()
        {
            ChainOrbitBuilder builder = new ChainOrbitBuilder();
            for (int bodyCount = 1; bodyCount <= ChainBodyCount; bodyCount++)
            {
                VehicleCameraTarget target = CreateChainTarget($"Chain {bodyCount}", bodyCount);
                Assert.AreEqual(ChainOrbitAssemblyResult.Valid, builder.Build(target, CameraTestRig.OrbitName).AssemblyResult);

                for (int index = 0; index < SampleCount; index++)
                {
                    stopwatch.Restart();
                    ChainOrbit orbit = builder.Build(target, CameraTestRig.OrbitName);
                    stopwatch.Stop();
                    milliseconds[index] = stopwatch.Elapsed.TotalMilliseconds;
                    Assert.AreEqual(ChainOrbitAssemblyResult.Valid, orbit.AssemblyResult);
                }

                LogMedian($"ChainOrbitBuilder.Build ({bodyCount} bodies, {builder.Build(target, CameraTestRig.OrbitName).Segments.Count} segments)", milliseconds);
            }
        }

        [Test]
        public void Activate_FiveBodyChain_LogsMedian()
        {
            VehicleCameraTarget target = CreateChainTarget("Chain", ChainBodyCount);
            Camera camera = rig.CreateCamera("Camera");
            controller = rig.CreateController("Controller", camera, target, CameraTestRig.PresentationView);
            Assert.AreEqual(CameraCommandResult.Accepted, controller.Activate());
            controller.Deactivate();

            for (int index = 0; index < SampleCount; index++)
            {
                stopwatch.Restart();
                CameraCommandResult result = controller.Activate();
                stopwatch.Stop();
                milliseconds[index] = stopwatch.Elapsed.TotalMilliseconds;
                Assert.AreEqual(CameraCommandResult.Accepted, result);
                controller.Deactivate();
            }

            LogMedian("Snap activation (five-body chain, Presentation view)", milliseconds);
        }

        [Test]
        public void SetTarget_FiveBodyChains_LogsMedian()
        {
            VehicleCameraTarget firstTarget = CreateChainTarget("First Chain", ChainBodyCount);
            VehicleCameraTarget secondTarget = CreateChainTarget("Second Chain", ChainBodyCount);
            Camera camera = rig.CreateCamera("Camera");
            controller = rig.CreateController("Controller", camera, firstTarget, CameraTestRig.PresentationView);
            Assert.AreEqual(CameraCommandResult.Accepted, controller.Activate());
            Assert.AreEqual(CameraCommandResult.Accepted, controller.SetTarget(secondTarget));

            for (int index = 0; index < SampleCount; index++)
            {
                VehicleCameraTarget nextTarget = firstTarget;
                if (index % 2 == 1)
                {
                    nextTarget = secondTarget;
                }

                stopwatch.Restart();
                CameraCommandResult result = controller.SetTarget(nextTarget);
                stopwatch.Stop();
                milliseconds[index] = stopwatch.Elapsed.TotalMilliseconds;
                Assert.AreEqual(CameraCommandResult.Accepted, result);
            }

            LogMedian("Snap SetTarget (five-body chain to five-body chain)", milliseconds);
        }

        [Test]
        public void AttachDetach_ActiveCamera_LogsMedians()
        {
            VehicleCameraTarget target = CreateChainTarget("Chain", ChainBodyCount - 1);
            Transform lastTrailer = rig.CreateBody("Last Trailer");
            lastTrailer.position = new Vector3(0f, 0f, -TrailerSpacing * (ChainBodyCount - 1));
            VehicleProfile lastProfile = rig.CreateProfile(1f, 3f, 3f);
            Camera camera = rig.CreateCamera("Camera");
            controller = rig.CreateController("Controller", camera, target, CameraTestRig.PresentationView);
            Assert.AreEqual(CameraCommandResult.Accepted, controller.Activate());
            Assert.AreEqual(VehicleTargetResult.Accepted, target.Attach(lastTrailer, lastProfile, ChainEnd.Rear));
            Assert.AreEqual(VehicleTargetResult.Accepted, target.Detach(lastTrailer));

            for (int index = 0; index < SampleCount; index++)
            {
                stopwatch.Restart();
                VehicleTargetResult attachResult = target.Attach(lastTrailer, lastProfile, ChainEnd.Rear);
                stopwatch.Stop();
                milliseconds[index] = stopwatch.Elapsed.TotalMilliseconds;
                Assert.AreEqual(VehicleTargetResult.Accepted, attachResult);
                Assert.AreEqual(ChainBodyCount, target.BodyCount);

                stopwatch.Restart();
                VehicleTargetResult detachResult = target.Detach(lastTrailer);
                stopwatch.Stop();
                secondMilliseconds[index] = stopwatch.Elapsed.TotalMilliseconds;
                Assert.AreEqual(VehicleTargetResult.Accepted, detachResult);
            }

            Assert.IsTrue(controller.IsActive);
            LogMedian("Attach fifth body with active camera (target work + orbit rebuild)", milliseconds);
            LogMedian("Detach fifth body with active camera (target work + orbit rebuild)", secondMilliseconds);
        }

        [Test]
        public void SelectView_FiveBodyChain_LogsMedian()
        {
            VehicleCameraTarget target = CreateChainTarget("Chain", ChainBodyCount);
            VehicleProfile rootProfile = target.GetProfile(target.RootIndex);
            rootProfile.AddView(DrivingView, rig.CreatePreset(CameraViewType.Driving), GetRootOrbit(target).Id);
            Camera camera = rig.CreateCamera("Camera");
            controller = rig.CreateController("Controller", camera, target, CameraTestRig.PresentationView);
            Assert.AreEqual(CameraCommandResult.Accepted, controller.Activate());
            Assert.AreEqual(CameraCommandResult.Accepted, controller.SelectView(DrivingView));
            Assert.AreEqual(CameraCommandResult.Accepted, controller.SelectView(CameraTestRig.PresentationView));

            for (int index = 0; index < SampleCount; index++)
            {
                string viewName = DrivingView;
                if (index % 2 == 1)
                {
                    viewName = CameraTestRig.PresentationView;
                }

                stopwatch.Restart();
                CameraCommandResult result = controller.SelectView(viewName);
                stopwatch.Stop();
                milliseconds[index] = stopwatch.Elapsed.TotalMilliseconds;
                Assert.AreEqual(CameraCommandResult.Accepted, result);
                Assert.AreEqual(viewName, controller.ActiveView.Name);
            }

            LogMedian("Snap SelectView between two views on the same orbit (five-body chain)", milliseconds);
        }

        private void LogMedian(string label, double[] samples)
        {
            System.Array.Sort(samples);
            double median = (samples[samples.Length / 2 - 1] + samples[samples.Length / 2]) * 0.5;
            logFile.Append($"PerformanceBreakdown: {label}: median {median:F4} ms (min {samples[0]:F4} ms, max {samples[samples.Length - 1]:F4} ms) over {samples.Length} runs.");
        }

        private VehicleCameraTarget CreateChainTarget(string name, int bodyCount)
        {
            Transform truck = rig.CreateBody($"{name} Truck");
            VehicleCameraTarget target = rig.CreateTarget(truck, rig.CreateProfile(1f, 3f, 3f));
            for (int index = 1; index < bodyCount; index++)
            {
                Transform trailer = rig.CreateBody($"{name} Trailer {index}");
                trailer.position = new Vector3(0f, 0f, -TrailerSpacing * index);
                Assert.AreEqual(VehicleTargetResult.Accepted, target.Attach(trailer, rig.CreateProfile(1f, 3f, 3f), ChainEnd.Rear));
            }

            Assert.AreEqual(bodyCount, target.BodyCount);
            return target;
        }

        private void CollectChain(VehicleCameraTarget target)
        {
            profiles.Clear();
            bodies.Clear();
            for (int index = 0; index < target.BodyCount; index++)
            {
                profiles.Add(target.GetProfile(index));
                bodies.Add(target.GetBody(index));
            }
        }

        private VehicleOrbit GetRootOrbit(VehicleCameraTarget target)
        {
            VehicleOrbit rootOrbit;
            Assert.IsTrue(target.GetProfile(target.RootIndex).TryGetOrbit(CameraTestRig.OrbitName, out rootOrbit));
            return rootOrbit;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (controller != null)
            {
                controller.Deactivate();
            }

            controller = null;
            profiles.Clear();
            bodies.Clear();
            rig.Dispose();
            yield return null;
        }
    }
}
