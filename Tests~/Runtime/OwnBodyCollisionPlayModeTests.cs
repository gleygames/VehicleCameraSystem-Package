using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Gley.CameraSystem.Tests.PlayMode
{
    public class OwnBodyCollisionPlayModeTests
    {
        private const float FrameTime = 0.02f;
        private const float CameraRadius = 0.25f;
        private const float MoveInHalfLife = 0.05f;
        private const float EaseOutHalfLife = 0.3f;
        private const float DefaultZoom = -2f;
        private const float DesiredZ = -5f;
        private const float WallFrontFace = -4.4f;
        private const float SurfaceSkin = 0.01f;
        private const float PositionTolerance = 0.001f;
        private const float MinimalMoveTolerance = 0.02f;
        private const float MinimumVisibleMove = 0.1f;
        private const float WallTop = 0.9f;
        private const float WallShift = 0.25f;
        private const int CollisionCasts = 8;
        private const int SightLineOnlyCasts = 3;
        private const int TouchingBodyCasts = 5;
        private const int DefaultLayer = 0;
        private const int DefaultLayerMask = 1;
        private const int SettleFrames = 200;

        private readonly List<GameObject> obstacles = new List<GameObject>();
        private readonly Vector3 watchPoint = new Vector3(0f, 1f, 0f);
        private readonly Vector3 wallMinimum = new Vector3(-1f, -1f, -4.7f);
        private readonly Vector3 wallMaximum = new Vector3(1f, WallTop, WallFrontFace);
        private CameraTestRig rig;
        private Transform truck;
        private VehicleCameraTarget target;
        private Camera camera;
        private CameraSystemController controller;

        [SetUp]
        public void SetUp()
        {
            rig = new CameraTestRig();
            rig.PresentationPreset.Collision.Configure(true, CameraRadius, CollisionCasts, MoveInHalfLife, EaseOutHalfLife, DefaultLayerMask);
            truck = rig.CreateBody("Truck");
            VehicleProfile profile = rig.CreateProfile(1f, 3f, 3f);
            VehicleOrbit orbit;
            Assert.IsTrue(profile.TryGetOrbit(CameraTestRig.OrbitName, out orbit));
            orbit.ConfigureAttachmentMerge(false);
            VehicleViewEntry view;
            Assert.IsTrue(profile.TryGetView(CameraTestRig.PresentationView, out view));
            view.ConfigureDefaultPose(new OrbitPose(0f, DefaultZoom, 0f));
            target = rig.CreateTarget(truck, profile);
            camera = rig.CreateCamera("Camera");
            controller = rig.CreateController("Controller", camera, target, CameraTestRig.PresentationView);
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
        public void TrailerBoxBetweenCameraAndWatchPoint_MovesOutwardOrUp_NotInward()
        {
            Transform trailer = CreateTrailer();
            Assert.AreEqual(VehicleTargetResult.Accepted, target.Attach(trailer, rig.CreateProfile(1f, 3f, 3f), ChainEnd.Rear));
            Collider trailerWall = CreateBox(wallMinimum, wallMaximum, trailer);
            Assert.AreEqual(CameraCommandResult.Accepted, controller.Activate());
            Assert.IsTrue(controller.IsOwnBodyCollider(trailerWall));

            RunFrames(SettleFrames);

            Vector3 position = camera.transform.position;
            Assert.AreEqual(0f, position.x, PositionTolerance);
            Assert.LessOrEqual(position.z, DesiredZ + PositionTolerance);
            Assert.IsTrue(position.y > MinimumVisibleMove || position.z < DesiredZ - MinimumVisibleMove);
            Assert.IsFalse(Physics.Linecast(watchPoint, position, DefaultLayerMask, QueryTriggerInteraction.Ignore));
            Assert.IsFalse(Physics.CheckSphere(position, CameraRadius - PositionTolerance, DefaultLayerMask, QueryTriggerInteraction.Ignore));
            Assert.IsFalse(controller.HasNoClearPose);
        }

        [Test]
        public void OwnBody_NoProjectLayersRequired()
        {
            Collider wall = CreateBox(wallMinimum, wallMaximum, null);
            Assert.AreEqual(DefaultLayer, wall.gameObject.layer);
            Assert.AreEqual(DefaultLayer, truck.gameObject.layer);
            Assert.AreEqual(CameraCommandResult.Accepted, controller.Activate());
            Assert.IsFalse(controller.IsOwnBodyCollider(wall));

            RunFrames(SettleFrames);

            AssertMovedInwardToWall(camera.transform.position);

            controller.Deactivate();
            wall.transform.SetParent(truck, true);
            Assert.AreEqual(DefaultLayer, wall.gameObject.layer);
            Assert.AreEqual(CameraCommandResult.Accepted, controller.Activate());
            Assert.IsTrue(controller.IsOwnBodyCollider(wall));

            RunFrames(SettleFrames);

            Vector3 position = camera.transform.position;
            Assert.LessOrEqual(position.z, DesiredZ + PositionTolerance);
            Assert.IsTrue(position.y > MinimumVisibleMove || position.z < DesiredZ - MinimumVisibleMove);
            Assert.IsFalse(Physics.Linecast(watchPoint, position, DefaultLayerMask, QueryTriggerInteraction.Ignore));
        }

        [Test]
        public void ChainChange_OwnBodySetRebuilt()
        {
            Transform trailer = CreateTrailer();
            VehicleProfile trailerProfile = rig.CreateProfile(1f, 3f, 3f);
            Collider trailerWall = CreateBox(wallMinimum, wallMaximum, trailer);
            Assert.AreEqual(CameraCommandResult.Accepted, controller.Activate());
            Assert.IsFalse(controller.IsOwnBodyCollider(trailerWall));
            Assert.AreEqual(0, controller.OwnBodyColliderCount);

            RunFrames(SettleFrames);
            AssertMovedInwardToWall(camera.transform.position);

            Assert.AreEqual(VehicleTargetResult.Accepted, target.Attach(trailer, trailerProfile, ChainEnd.Rear));
            Assert.IsTrue(controller.IsOwnBodyCollider(trailerWall));
            Assert.AreEqual(1, controller.OwnBodyColliderCount);

            RunFrames(SettleFrames);
            Vector3 position = camera.transform.position;
            Assert.LessOrEqual(position.z, DesiredZ + PositionTolerance);
            Assert.IsFalse(Physics.Linecast(watchPoint, position, DefaultLayerMask, QueryTriggerInteraction.Ignore));

            Assert.AreEqual(VehicleTargetResult.Accepted, target.Detach(trailer));
            Assert.IsFalse(controller.IsOwnBodyCollider(trailerWall));
            Assert.AreEqual(0, controller.OwnBodyColliderCount);

            RunFrames(SettleFrames);
            AssertMovedInwardToWall(camera.transform.position);
        }

        [Test]
        public void OutwardCandidates_OnlyWhenCameraTouchesOwnBody()
        {
            Collider wall = CreateBox(wallMinimum, wallMaximum, truck);
            OwnBodyColliderSet ownBodyColliders = new OwnBodyColliderSet();
            ownBodyColliders.Rebuild(target);
            Assert.IsTrue(ownBodyColliders.Contains(wall));
            CollisionSettings settings = new CollisionSettings();
            settings.Configure(true, CameraRadius, CollisionCasts, MoveInHalfLife, EaseOutHalfLife, DefaultLayerMask);
            VehicleOrbit ranges = new VehicleOrbit();
            ranges.ConfigureOffsetRanges(-1f, 4f, -4f, 1f);
            Vector3 desiredPosition = new Vector3(0f, 0f, DesiredZ);
            CameraCollision collision = new CameraCollision();

            collision.UpdateCollisionCorrection(desiredPosition, watchPoint, Vector3.forward, Vector3.up, DefaultZoom, 0f, ranges, settings, ownBodyColliders, FrameTime);

            Assert.IsTrue(collision.HasClearPose);
            Assert.AreEqual(SightLineOnlyCasts, collision.CastsThisFrame);
            Assert.AreEqual(0f, collision.InwardCorrection, PositionTolerance);
            Assert.AreEqual(GetClearingUp(WallFrontFace + CameraRadius), collision.UpCorrection, PositionTolerance);

            wall.transform.position += Vector3.back * WallShift;
            wall.transform.localScale += Vector3.forward * WallShift;
            Physics.SyncTransforms();
            Assert.IsTrue(Physics.CheckSphere(desiredPosition, CameraRadius, DefaultLayerMask, QueryTriggerInteraction.Ignore));
            collision.Reset();

            collision.UpdateCollisionCorrection(desiredPosition, watchPoint, Vector3.forward, Vector3.up, DefaultZoom, 0f, ranges, settings, ownBodyColliders, FrameTime);

            Assert.IsTrue(collision.HasClearPose);
            Assert.AreEqual(TouchingBodyCasts, collision.CastsThisFrame);
            Assert.AreEqual(0f, collision.InwardCorrection, PositionTolerance);
            Assert.AreEqual(GetClearingUp(WallFrontFace + CameraRadius - WallShift * 0.5f), collision.UpCorrection, PositionTolerance);
        }

        private float GetClearingUp(float expandedFrontFace)
        {
            float lineFraction = expandedFrontFace / DesiredZ;
            float clearHeight = WallTop + CameraRadius;
            return watchPoint.y + (clearHeight - watchPoint.y) / lineFraction + SurfaceSkin;
        }

        private Transform CreateTrailer()
        {
            Transform trailer = rig.CreateBody("Trailer");
            trailer.position = new Vector3(0f, 0f, -7f);
            return trailer;
        }

        private Collider CreateBox(Vector3 minimum, Vector3 maximum, Transform parent)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = "Own Body Test Box";
            box.transform.position = (minimum + maximum) * 0.5f;
            box.transform.localScale = maximum - minimum;
            if (parent != null)
            {
                box.transform.SetParent(parent, true);
            }

            obstacles.Add(box);
            Physics.SyncTransforms();
            return box.GetComponent<Collider>();
        }

        private void RunFrames(int count)
        {
            for (int frame = 0; frame < count; frame++)
            {
                controller.UpdateCameraFrame(FrameTime);
            }
        }

        private void AssertMovedInwardToWall(Vector3 position)
        {
            float expectedZ = WallFrontFace + CameraRadius + SurfaceSkin;
            Assert.AreEqual(0f, position.x, PositionTolerance);
            Assert.AreEqual(0f, position.y, PositionTolerance);
            Assert.AreEqual(expectedZ, position.z, MinimalMoveTolerance);
        }
    }
}
