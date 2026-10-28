using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Profiling;

namespace Gley.CameraSystem.Tests.PlayMode
{
    public class CameraCollisionPlayModeTests
    {
        private const string DrivingView = "Driving";
        private const float FrameTime = 0.02f;
        private const float CameraRadius = 0.25f;
        private const float MoveInHalfLife = 0.05f;
        private const float EaseOutHalfLife = 0.3f;
        private const float MinimumHeight = -1f;
        private const float MaximumHeight = 4f;
        private const float MinimumZoom = -4f;
        private const float MaximumZoom = 2f;
        private const float UpStep = 0.8f;
        private const float PositionTolerance = 0.001f;
        private const float ContactTolerance = 0.005f;
        private const float MinimalMoveTolerance = 0.02f;
        private const int DefaultMaximumCasts = 8;
        private const int DefaultLayerMask = 1;
        private const int TransparentFxLayer = 1;

        private readonly List<GameObject> obstacles = new List<GameObject>();
        private readonly OwnBodyColliderSet ownBodyColliders = new OwnBodyColliderSet();
        private readonly Vector3 watchPoint = new Vector3(0f, 1f, 0f);
        private readonly Vector3 desiredPosition = new Vector3(0f, 0f, -3f);
        private CameraTestRig rig;
        private CameraCollision collision;
        private CollisionSettings settings;
        private VehicleOrbit ranges;

        [SetUp]
        public void SetUp()
        {
            rig = new CameraTestRig();
            collision = new CameraCollision();
            settings = new CollisionSettings();
            settings.Configure(true, CameraRadius, DefaultMaximumCasts, MoveInHalfLife, EaseOutHalfLife, DefaultLayerMask);
            ranges = new VehicleOrbit();
            ranges.ConfigureOffsetRanges(MinimumHeight, MaximumHeight, MinimumZoom, MaximumZoom);
        }

        [TearDown]
        public void TearDown()
        {
            for (int index = 0; index < obstacles.Count; index++)
            {
                if (obstacles[index] != null)
                {
                    Object.DestroyImmediate(obstacles[index]);
                }
            }

            obstacles.Clear();
            Physics.SyncTransforms();
            rig.Dispose();
        }

        [Test]
        public void WallBehindCamera_MovesInward_WithinZoomRange()
        {
            float wallFace = -2.8f;
            CreateBox(new Vector3(-5f, -5f, -4f), new Vector3(5f, 5f, wallFace));
            Camera camera;
            CameraSystemController controller = CreateActiveController(CameraTestRig.PresentationView, out camera);

            RunFrames(controller, 10);

            Vector3 position = camera.transform.position;
            Assert.AreEqual(0f, position.x, PositionTolerance);
            Assert.AreEqual(0f, position.y, PositionTolerance);
            Assert.GreaterOrEqual(position.z, wallFace + CameraRadius - PositionTolerance);
            Assert.LessOrEqual(position.z, wallFace + CameraRadius + MinimalMoveTolerance);
            Assert.LessOrEqual(position.z - desiredPosition.z, 1f + PositionTolerance);
            Assert.AreEqual(0f, controller.ZoomOffset, PositionTolerance);

            ClearObstacles();
            CreateBox(new Vector3(-5f, -5f, -4f), new Vector3(5f, 5f, -1.5f));
            RunFrames(controller, 100);

            position = camera.transform.position;
            Assert.LessOrEqual(position.z - desiredPosition.z, 1f + PositionTolerance);
            Assert.GreaterOrEqual(position.z - desiredPosition.z, -PositionTolerance);
        }

        [Test]
        public void LowCeilingAndWall_ChoosesSmallestDisplacement()
        {
            float wallFace = -2f;
            float wallTop = 0.2f;
            CreateBox(new Vector3(-5f, -5f, -6f), new Vector3(5f, wallTop, wallFace));

            Vector3 position = RunCollision(0f, 0f, FrameTime);

            Assert.IsTrue(collision.HasClearPose);
            Assert.AreEqual(0f, collision.InwardCorrection, PositionTolerance);
            Assert.GreaterOrEqual(position.y, wallTop + CameraRadius - PositionTolerance);
            Assert.LessOrEqual(position.y, wallTop + CameraRadius + MinimalMoveTolerance);
            Assert.AreEqual(0f, position.x, PositionTolerance);
            Assert.AreEqual(desiredPosition.z, position.z, PositionTolerance);

            CreateBox(new Vector3(-5f, 0.6f, -6f), new Vector3(5f, 6f, -2.2f));
            collision.Reset();
            position = RunCollision(0f, 0f, FrameTime);

            Assert.IsTrue(collision.HasClearPose);
            Assert.AreEqual(0f, collision.UpCorrection, PositionTolerance);
            Assert.GreaterOrEqual(position.z, wallFace + CameraRadius - PositionTolerance);
            Assert.LessOrEqual(position.z, wallFace + CameraRadius + MinimalMoveTolerance);
            Assert.AreEqual(0f, position.x, PositionTolerance);
            Assert.AreEqual(0f, position.y, PositionTolerance);
        }

        [Test]
        public void TieInwardVersusUp_PrefersInward()
        {
            float wallFace = -2.46f;
            float wallTop = 0.54f;
            CreateBox(new Vector3(-5f, -5f, -4f), new Vector3(5f, wallTop, wallFace));
            float expectedInward = wallFace + CameraRadius - desiredPosition.z;
            Assert.AreEqual(UpStep, expectedInward, MinimalMoveTolerance);
            Assert.AreEqual(UpStep, wallTop + CameraRadius + 0.01f, PositionTolerance);

            Vector3 position = RunCollision(0f, 0f, FrameTime);

            Assert.IsTrue(collision.HasClearPose);
            Assert.AreEqual(expectedInward, collision.InwardCorrection, MinimalMoveTolerance);
            Assert.AreEqual(0f, collision.UpCorrection, PositionTolerance);
            Assert.GreaterOrEqual(position.z, wallFace + CameraRadius - PositionTolerance);

            collision.Reset();
            position = RunCollision(MaximumZoom, 0f, FrameTime);

            Assert.IsTrue(collision.HasClearPose);
            Assert.AreEqual(0f, collision.InwardCorrection, PositionTolerance);
            Assert.AreEqual(UpStep, collision.UpCorrection, PositionTolerance);
            AssertPosition(desiredPosition + Vector3.up * UpStep, position);
        }

        [Test]
        public void CorrectionNeverLeavesZoomOrHeightRange()
        {
            CreateBox(new Vector3(-5f, -5f, -6f), new Vector3(5f, 5f, -1.5f));
            float[] zooms = { 0f, 1.7f, 1.95f, MaximumZoom, MinimumZoom, 0.5f };
            float[] heights = { 0f, 3.5f, 3.9f, MaximumHeight, MinimumHeight, 2f };

            for (int pair = 0; pair < zooms.Length; pair++)
            {
                for (int frame = 0; frame < 20; frame++)
                {
                    Vector3 position = RunCollision(zooms[pair], heights[pair], FrameTime);

                    Assert.GreaterOrEqual(collision.InwardCorrection, 0f);
                    Assert.GreaterOrEqual(collision.UpCorrection, 0f);
                    Assert.LessOrEqual(zooms[pair] + collision.InwardCorrection, MaximumZoom + PositionTolerance);
                    Assert.LessOrEqual(heights[pair] + collision.UpCorrection, MaximumHeight + PositionTolerance);
                    AssertPosition(desiredPosition + Vector3.forward * collision.InwardCorrection + Vector3.up * collision.UpCorrection, position);
                }
            }
        }

        [Test]
        public void CastsPerFrame_NeverExceedsMaximum()
        {
            RunCollision(0f, 0f, FrameTime);
            Assert.AreEqual(2, collision.CastsThisFrame);

            CreateBox(new Vector3(-5f, -5f, -6f), new Vector3(5f, 5f, -0.5f));
            int[] budgets = { 0, 1, 2, 3, 4, 5, 8, 12 };
            for (int index = 0; index < budgets.Length; index++)
            {
                settings.Configure(true, CameraRadius, budgets[index], MoveInHalfLife, EaseOutHalfLife, DefaultLayerMask);
                collision.Reset();
                for (int frame = 0; frame < 5; frame++)
                {
                    RunCollision(0f, 0f, FrameTime);

                    Assert.LessOrEqual(collision.CastsThisFrame, budgets[index]);
                }
            }

            settings.Configure(true, CameraRadius, 3, MoveInHalfLife, EaseOutHalfLife, DefaultLayerMask);
            RunCollision(0f, 0f, FrameTime);
            Assert.AreEqual(3, collision.CastsThisFrame);
            Assert.IsFalse(collision.HasClearPose);
        }

        [Test]
        public void ObstacleRemoved_EasesOutWithEaseOutHalfLife_NoJump()
        {
            float wallFace = -2.8f;
            GameObject wall = CreateBox(new Vector3(-5f, -5f, -4f), new Vector3(5f, 5f, wallFace));
            RunCollision(0f, 0f, FrameTime);
            float fullCorrection = collision.InwardCorrection;
            Assert.AreEqual(wallFace + CameraRadius - desiredPosition.z, fullCorrection, MinimalMoveTolerance);
            for (int frame = 0; frame < 5; frame++)
            {
                RunCollision(0f, 0f, FrameTime);
                Assert.AreEqual(fullCorrection, collision.InwardCorrection, PositionTolerance);
            }

            wall.SetActive(false);
            Physics.SyncTransforms();
            float easeOutStep = 1f - Mathf.Pow(0.5f, FrameTime / EaseOutHalfLife);
            float previous = fullCorrection;
            for (int frame = 1; frame <= 30; frame++)
            {
                RunCollision(0f, 0f, FrameTime);

                float expected = fullCorrection * Mathf.Pow(0.5f, frame * FrameTime / EaseOutHalfLife);
                Assert.AreEqual(expected, collision.InwardCorrection, 0.0001f);
                Assert.LessOrEqual(previous - collision.InwardCorrection, fullCorrection * easeOutStep + 0.00001f);
                Assert.Less(collision.InwardCorrection, previous);
                previous = collision.InwardCorrection;
            }

            wall.SetActive(true);
            Physics.SyncTransforms();
            RunCollision(0f, 0f, FrameTime);

            float moveInStep = 1f - Mathf.Pow(0.5f, FrameTime / MoveInHalfLife);
            Assert.AreEqual(previous + (fullCorrection - previous) * moveInStep, collision.InwardCorrection, 0.0005f);
        }

        [Test]
        public void TriggerColliders_Ignored()
        {
            GameObject wall = CreateBox(new Vector3(-5f, -5f, -4f), new Vector3(5f, 5f, -2.8f));
            wall.GetComponent<Collider>().isTrigger = true;
            Physics.SyncTransforms();

            Vector3 position = RunCollision(0f, 0f, FrameTime);

            Assert.IsTrue(collision.HasClearPose);
            Assert.AreEqual(0f, collision.InwardCorrection, PositionTolerance);
            Assert.AreEqual(0f, collision.UpCorrection, PositionTolerance);
            AssertPosition(desiredPosition, position);
        }

        [Test]
        public void CollisionMaskExcludesLayer_Ignored()
        {
            float wallFace = -2.8f;
            GameObject wall = CreateBox(new Vector3(-5f, -5f, -4f), new Vector3(5f, 5f, wallFace));
            wall.layer = TransparentFxLayer;
            Physics.SyncTransforms();

            Vector3 position = RunCollision(0f, 0f, FrameTime);

            Assert.IsTrue(collision.HasClearPose);
            AssertPosition(desiredPosition, position);

            settings.Configure(true, CameraRadius, DefaultMaximumCasts, MoveInHalfLife, EaseOutHalfLife, DefaultLayerMask | (1 << TransparentFxLayer));
            collision.Reset();
            RunCollision(0f, 0f, FrameTime);

            Assert.AreEqual(wallFace + CameraRadius - desiredPosition.z, collision.InwardCorrection, MinimalMoveTolerance);
        }

        [Test]
        public void WatchPointUnchanged_ByCollision()
        {
            Camera camera;
            CameraSystemController controller = CreateActiveController(CameraTestRig.PresentationView, out camera);
            RunFrames(controller, 5);
            AssertPosition(desiredPosition, camera.transform.position);
            Assert.Less(Vector3.Angle(watchPoint - camera.transform.position, camera.transform.forward), 0.05f);

            CreateBox(new Vector3(-5f, -5f, -4f), new Vector3(5f, 5f, -2.8f));
            RunFrames(controller, 30);

            Vector3 position = camera.transform.position;
            Assert.Greater(position.z, desiredPosition.z + 0.4f);
            Assert.Less(Vector3.Angle(watchPoint - position, camera.transform.forward), 0.05f);
        }

        [Test]
        public void NoGarbage_WithCollision()
        {
            CreateBox(new Vector3(-5f, -5f, -4f), new Vector3(5f, 5f, -2.8f));
            Transform root = rig.CreateBody("Truck");
            VehicleProfile profile = rig.CreateProfile(1f, 3f, 3f);
            VehicleOrbit orbit;
            Assert.IsTrue(profile.TryGetOrbit(CameraTestRig.OrbitName, out orbit));
            profile.AddView(DrivingView, rig.CreatePreset(CameraViewType.Driving), orbit.Id);
            VehicleCameraTarget target = rig.CreateTarget(root, profile);
            target.SetSuppliedSpeed(0f);
            Camera camera = rig.CreateCamera("Camera");
            CameraSystemController controller = rig.CreateController("Controller", camera, target, DrivingView);
            Assert.AreEqual(CameraCommandResult.Accepted, controller.Activate());
            RunFrames(controller, 30);

            Recorder recorder = Recorder.Get("GC.Alloc");
            recorder.enabled = false;
            recorder.FilterToCurrentThread();
            recorder.enabled = true;
            RunFrames(controller, 60);
            recorder.enabled = false;
            recorder.CollectFromAllThreads();

            Assert.AreEqual(0, recorder.sampleBlockCount);
            Assert.Greater(camera.transform.position.z, desiredPosition.z + 0.4f);
        }

        private GameObject CreateBox(Vector3 minimum, Vector3 maximum)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = "Collision Test Box";
            box.transform.position = (minimum + maximum) * 0.5f;
            box.transform.localScale = maximum - minimum;
            obstacles.Add(box);
            Physics.SyncTransforms();
            return box;
        }

        private CameraSystemController CreateActiveController(string viewName, out Camera camera)
        {
            Transform root = rig.CreateBody("Truck");
            VehicleCameraTarget target = rig.CreateTarget(root, rig.CreateProfile(1f, 3f, 3f));
            camera = rig.CreateCamera("Camera");
            CameraSystemController controller = rig.CreateController("Controller", camera, target, viewName);
            Assert.AreEqual(CameraCommandResult.Accepted, controller.Activate());
            return controller;
        }

        private void RunFrames(CameraSystemController controller, int count)
        {
            for (int frame = 0; frame < count; frame++)
            {
                controller.UpdateCameraFrame(FrameTime);
            }
        }

        private Vector3 RunCollision(float zoom, float height, float deltaTime)
        {
            return collision.UpdateCollisionCorrection(desiredPosition, watchPoint, Vector3.forward, Vector3.up, zoom, height, ranges, settings, ownBodyColliders, deltaTime);
        }

        private void ClearObstacles()
        {
            for (int index = 0; index < obstacles.Count; index++)
            {
                Object.DestroyImmediate(obstacles[index]);
            }

            obstacles.Clear();
            Physics.SyncTransforms();
        }

        private void AssertPosition(Vector3 expected, Vector3 actual)
        {
            Assert.AreEqual(expected.x, actual.x, PositionTolerance);
            Assert.AreEqual(expected.y, actual.y, PositionTolerance);
            Assert.AreEqual(expected.z, actual.z, PositionTolerance);
        }
    }
}
