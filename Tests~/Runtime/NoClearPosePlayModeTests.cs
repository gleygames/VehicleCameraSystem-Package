using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Gley.CameraSystem.Tests.PlayMode
{
    public class NoClearPosePlayModeTests
    {
        private const float FrameTime = 0.02f;
        private const float CameraRadius = 0.25f;
        private const float MoveInHalfLife = 0.05f;
        private const float EaseOutHalfLife = 0.3f;
        private const float DefaultHeight = 2f;
        private const float MaximumRigHeight = 4f;
        private const float MaximumRigZoom = 1f;
        private const float RearEdgeZ = -3f;
        private const float MaximumZoom = 2f;
        private const float PositionTolerance = 0.001f;
        private const int DefaultMaximumCasts = 8;
        private const int DefaultLayerMask = 1;

        private readonly List<GameObject> obstacles = new List<GameObject>();
        private readonly List<bool> noClearPoseEvents = new List<bool>();
        private CameraTestRig rig;
        private CameraSystemController controller;
        private Transform truck;
        private Camera camera;

        [SetUp]
        public void SetUp()
        {
            rig = new CameraTestRig();
        }

        [TearDown]
        public void TearDown()
        {
            if (controller != null)
            {
                controller.NoClearPoseChanged -= RecordNoClearPose;
            }

            for (int index = 0; index < obstacles.Count; index++)
            {
                if (obstacles[index] != null)
                {
                    Object.DestroyImmediate(obstacles[index]);
                }
            }

            obstacles.Clear();
            noClearPoseEvents.Clear();
            Physics.SyncTransforms();
            rig.Dispose();
        }

        [Test]
        public void Tunnel_NoClearPose_RaisesEventOnceAndStaysInRange()
        {
            CreateTunnel();
            CreateActiveController();

            Assert.AreEqual(1, noClearPoseEvents.Count);
            Assert.IsTrue(noClearPoseEvents[0]);
            Assert.IsTrue(controller.HasNoClearPose);

            for (int frame = 0; frame < 60; frame++)
            {
                controller.UpdateCameraFrame(FrameTime);

                Vector3 position = camera.transform.position;
                Assert.AreEqual(0f, position.x, PositionTolerance);
                Assert.GreaterOrEqual(position.y, DefaultHeight - PositionTolerance);
                Assert.LessOrEqual(position.y, MaximumRigHeight + PositionTolerance);
                Assert.GreaterOrEqual(position.z, RearEdgeZ - PositionTolerance);
                Assert.LessOrEqual(position.z, RearEdgeZ + MaximumRigZoom + PositionTolerance);
            }

            Assert.AreEqual(1, noClearPoseEvents.Count);
            Assert.IsTrue(controller.HasNoClearPose);
        }

        [Test]
        public void LeavingTunnel_RaisesFalseOnce()
        {
            CreateTunnel();
            CreateActiveController();
            RunFrames(10);
            Assert.AreEqual(1, noClearPoseEvents.Count);

            truck.position = new Vector3(0f, 0f, 30f);
            Physics.SyncTransforms();
            RunFrames(1);

            Assert.AreEqual(2, noClearPoseEvents.Count);
            Assert.IsFalse(noClearPoseEvents[1]);
            Assert.IsFalse(controller.HasNoClearPose);

            RunFrames(60);

            Assert.AreEqual(2, noClearPoseEvents.Count);
            Assert.IsFalse(controller.HasNoClearPose);
        }

        [Test]
        public void NoClearPose_PicksLeastBlockedCandidate()
        {
            CreateBox(new Vector3(-5f, -5f, -2.9f), new Vector3(5f, 10f, -0.3f));
            CameraCollision collision = new CameraCollision();
            CollisionSettings settings = new CollisionSettings();
            settings.Configure(true, CameraRadius, DefaultMaximumCasts, MoveInHalfLife, EaseOutHalfLife, DefaultLayerMask);
            VehicleOrbit ranges = new VehicleOrbit();
            ranges.ConfigureOffsetRanges(-1f, 4f, -4f, MaximumZoom);
            Vector3 desiredPosition = new Vector3(0f, 0f, RearEdgeZ);
            Vector3 watchPoint = new Vector3(0f, 1f, 0f);

            Vector3 position = collision.UpdateCollisionCorrection(desiredPosition, watchPoint, Vector3.forward, Vector3.up, 0f, 0f, ranges, settings, new OwnBodyColliderSet(), FrameTime);

            Assert.IsFalse(collision.HasClearPose);
            Assert.AreEqual(DefaultMaximumCasts, collision.CastsThisFrame);
            Assert.AreEqual(MaximumZoom, collision.InwardCorrection, PositionTolerance);
            Assert.AreEqual(0f, collision.UpCorrection, PositionTolerance);
            Assert.AreEqual(0f, position.x, PositionTolerance);
            Assert.AreEqual(0f, position.y, PositionTolerance);
            Assert.AreEqual(RearEdgeZ + MaximumZoom, position.z, PositionTolerance);
        }

        private void CreateTunnel()
        {
            CreateBox(new Vector3(-5f, 1.5f, -10f), new Vector3(5f, 8f, 10f));
            CreateBox(new Vector3(1.3f, -1f, -10f), new Vector3(5f, 1.5f, 10f));
            CreateBox(new Vector3(-5f, -1f, -10f), new Vector3(-1.3f, 1.5f, 10f));
        }

        private void CreateActiveController()
        {
            truck = rig.CreateBody("Truck");
            VehicleProfile profile = rig.CreateProfile(1f, 3f, 3f);
            VehicleViewEntry view;
            Assert.IsTrue(profile.TryGetView(CameraTestRig.PresentationView, out view));
            view.ConfigureDefaultPose(new OrbitPose(0f, 0f, DefaultHeight));
            VehicleCameraTarget target = rig.CreateTarget(truck, profile);
            camera = rig.CreateCamera("Camera");
            controller = rig.CreateController("Controller", camera, target, CameraTestRig.PresentationView);
            controller.NoClearPoseChanged += RecordNoClearPose;
            Assert.AreEqual(CameraCommandResult.Accepted, controller.Activate());
        }

        private void CreateBox(Vector3 minimum, Vector3 maximum)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = "No Clear Pose Test Box";
            box.transform.position = (minimum + maximum) * 0.5f;
            box.transform.localScale = maximum - minimum;
            obstacles.Add(box);
            Physics.SyncTransforms();
        }

        private void RunFrames(int count)
        {
            for (int frame = 0; frame < count; frame++)
            {
                controller.UpdateCameraFrame(FrameTime);
            }
        }

        private void RecordNoClearPose(bool hasNoClearPose)
        {
            noClearPoseEvents.Add(hasNoClearPose);
        }
    }
}
