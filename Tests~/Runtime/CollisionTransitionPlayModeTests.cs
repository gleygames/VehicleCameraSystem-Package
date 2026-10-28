using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Gley.CameraSystem.Tests.PlayMode
{
    public class CollisionTransitionPlayModeTests
    {
        private const string PointName = "Right";
        private const float FrameTime = 0.02f;
        private const float CameraRadius = 0.25f;
        private const float MoveInHalfLife = 0.05f;
        private const float SurfaceSkin = 0.01f;
        private const float SwitchDuration = 0.5f;
        private const float WallFace = -2.8f;
        private const float SideWallFace = 1.2f;
        private const float OrbitSideX = 1f;
        private const float PositionTolerance = 0.001f;
        private const float EaseTolerance = 0.005f;
        private const float MinimumSideCorrection = 0.03f;
        private const int MaximumFrames = 200;

        private readonly List<GameObject> obstacles = new List<GameObject>();
        private readonly List<int> endedCommandIds = new List<int>();
        private readonly List<CommandEndResult> endedResults = new List<CommandEndResult>();
        private readonly Vector3 fixedCameraPosition = new Vector3(2f, 3f, -8f);
        private readonly Vector3 presentationPosition = new Vector3(0f, 0f, -3f);
        private CameraTestRig rig;
        private CameraSystemController controller;
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
                controller.CommandEnded -= RecordCommandEnded;
            }

            for (int index = 0; index < obstacles.Count; index++)
            {
                if (obstacles[index] != null)
                {
                    Object.DestroyImmediate(obstacles[index]);
                }
            }

            obstacles.Clear();
            endedCommandIds.Clear();
            endedResults.Clear();
            Physics.SyncTransforms();
            rig.Dispose();
        }

        [Test]
        public void SmoothViewSwitch_CollisionSuspended_ThenEasesIn()
        {
            CreateBox(new Vector3(-5f, -5f, -4f), new Vector3(5f, 5f, WallFace));
            CreateActiveController(CameraTestRig.FixedView);
            AssertPosition(fixedCameraPosition, camera.transform.position);

            int commandId;
            Assert.AreEqual(CameraCommandResult.Accepted, controller.SelectView(CameraTestRig.PresentationView, new TransitionOptions(TransitionMode.Duration, SwitchDuration), out commandId));
            Assert.Greater(commandId, 0);

            int switchFrames = 0;
            while (!HasEnded(commandId) && switchFrames < MaximumFrames)
            {
                Assert.Less(DistanceFromLine(camera.transform.position, fixedCameraPosition, presentationPosition), PositionTolerance);
                controller.UpdateCameraFrame(FrameTime);
                switchFrames++;
            }

            Assert.IsTrue(HasEnded(commandId));
            Assert.AreEqual(CommandEndResult.Completed, endedResults[endedResults.Count - 1]);
            Assert.Greater(switchFrames, 10);
            AssertPosition(presentationPosition, camera.transform.position);

            float fullCorrection = WallFace + CameraRadius + SurfaceSkin - presentationPosition.z;
            float moveInStep = 1f - Mathf.Pow(0.5f, FrameTime / MoveInHalfLife);
            controller.UpdateCameraFrame(FrameTime);

            Vector3 position = camera.transform.position;
            Assert.AreEqual(0f, position.x, PositionTolerance);
            Assert.AreEqual(0f, position.y, PositionTolerance);
            Assert.AreEqual(fullCorrection * moveInStep, position.z - presentationPosition.z, EaseTolerance);

            RunFrames(40);

            Assert.AreEqual(presentationPosition.z + fullCorrection, camera.transform.position.z, EaseTolerance);
        }

        [Test]
        public void PointOfInterestTravel_CollisionApplied()
        {
            CreateBox(new Vector3(SideWallFace, -5f, -2f), new Vector3(5f, 5f, 10f));
            CreateBox(new Vector3(-5f, -5f, -2f), new Vector3(-SideWallFace, 5f, 10f));
            CreateActiveController(CameraTestRig.PresentationView);
            RunFrames(5);
            AssertPosition(presentationPosition, camera.transform.position);

            int commandId;
            Assert.AreEqual(CameraCommandResult.Accepted, controller.RequestPoint(PointName, new TravelRequest(TravelDirection.Shortest), out commandId));
            Assert.Greater(commandId, 0);

            int checkedFrames = 0;
            int frames = 0;
            while (!HasEnded(commandId) && frames < MaximumFrames)
            {
                controller.UpdateCameraFrame(FrameTime);
                frames++;
                Vector3 position = camera.transform.position;
                if (!HasEnded(commandId) && position.z > -1f)
                {
                    Assert.Less(Mathf.Abs(position.x), OrbitSideX - MinimumSideCorrection);
                    checkedFrames++;
                }
            }

            Assert.IsTrue(HasEnded(commandId));
            Assert.AreEqual(CommandEndResult.Completed, endedResults[endedResults.Count - 1]);
            Assert.Greater(checkedFrames, 0);

            RunFrames(30);

            Vector3 arrival = camera.transform.position;
            Assert.AreEqual(SideWallFace - CameraRadius - SurfaceSkin, Mathf.Abs(arrival.x), EaseTolerance);
            Assert.AreEqual(0f, arrival.z, PositionTolerance);
            Assert.IsFalse(Physics.CheckSphere(arrival, CameraRadius - PositionTolerance));
        }

        private void CreateActiveController(string viewName)
        {
            Transform truck = rig.CreateBody("Truck");
            VehicleCameraTarget target = rig.CreateTarget(truck, rig.CreateProfile(1f, 3f, 3f));
            camera = rig.CreateCamera("Camera");
            controller = rig.CreateController("Controller", camera, target, viewName);
            controller.CommandEnded += RecordCommandEnded;
            Assert.AreEqual(CameraCommandResult.Accepted, controller.Activate());
        }

        private void CreateBox(Vector3 minimum, Vector3 maximum)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = "Collision Transition Test Box";
            box.transform.position = (minimum + maximum) * 0.5f;
            box.transform.localScale = maximum - minimum;
            obstacles.Add(box);
            Physics.SyncTransforms();
        }

        private bool HasEnded(int commandId)
        {
            return endedCommandIds.Contains(commandId);
        }

        private float DistanceFromLine(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
        {
            Vector3 direction = (lineEnd - lineStart).normalized;
            Vector3 offset = point - lineStart;
            return (offset - direction * Vector3.Dot(offset, direction)).magnitude;
        }

        private void RunFrames(int count)
        {
            for (int frame = 0; frame < count; frame++)
            {
                controller.UpdateCameraFrame(FrameTime);
            }
        }

        private void RecordCommandEnded(int commandId, CommandEndResult result)
        {
            endedCommandIds.Add(commandId);
            endedResults.Add(result);
        }

        private void AssertPosition(Vector3 expected, Vector3 actual)
        {
            Assert.AreEqual(expected.x, actual.x, PositionTolerance);
            Assert.AreEqual(expected.y, actual.y, PositionTolerance);
            Assert.AreEqual(expected.z, actual.z, PositionTolerance);
        }
    }
}
