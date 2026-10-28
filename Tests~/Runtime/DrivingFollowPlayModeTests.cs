using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Gley.CameraSystem.Tests.PlayMode
{
    public class DrivingFollowPlayModeTests
    {
        private const string DrivingView = "Driving";
        private const string SideView = "Driving Side";
        private const float FrameTime = 0.02f;
        private const float PositionTolerance = 0.001f;
        private const float SettleSeconds = 3f;
        private const float FollowHalfLife = 0.12f;
        private const float MaximumLag = 3f;
        private const float CircleRadius = 40f;
        private const float CircleStartRate = 0.15f;
        private const float CircleAngularAcceleration = 0.1f;
        private const int ScriptedPathSeconds = 4;
        private const int SamplesPerSecond = 6;
        private const float UTurnSpeed = 20f;
        private const float UTurnRadius = 15f;
        private const float UTurnStraight = 30f;
        private const float UTurnDuration = 6f;
        private const float UTurnFrameTime = 1f / 60f;
        private const int TrailerSubsteps = 10;
        private const float TrailerOrbitHalfWidth = 2.25f;
        private const float TrailerOrbitFront = 1.5f;
        private const float TrailerOrbitRear = 9f;
        private const float TrailerHalfWidth = 1.25f;
        private const float TrailerFrontZ = 0.5f;
        private const float TrailerRearZ = -8f;
        private const float TrailerAxleZ = -6.5f;

        private CameraTestRig rig;

        [SetUp]
        public void SetUp()
        {
            rig = new CameraTestRig();
        }

        [Test]
        public void ConstantSpeed_LagBehindTargetButBelowMaxLag()
        {
            Transform root = rig.CreateBody("Truck");
            VehicleCameraTarget target = rig.CreateTarget(root, CreateDrivingProfile(1f, 3f, 3f));
            Camera camera = rig.CreateCamera("Camera");
            CameraSystemController controller = rig.CreateController("Controller", camera, target, DrivingView);
            Assert.AreEqual(CameraCommandResult.Accepted, controller.Activate());
            float speed = 10f;

            int frameCount = Mathf.RoundToInt(SettleSeconds / FrameTime);
            for (int frame = 0; frame < frameCount; frame++)
            {
                root.position += Vector3.forward * speed * FrameTime;
                controller.UpdateCameraFrame(FrameTime);
            }

            float speedOffset = 3f * 0.15625f;
            Vector3 targetPosition = root.TransformPoint(new Vector3(0f, 0f, -3f - speedOffset));
            Vector3 cameraPosition = camera.transform.position;
            float lag = targetPosition.z - cameraPosition.z;
            float expectedLag = speed * FollowHalfLife / Mathf.Log(2f);
            Assert.AreEqual(expectedLag, lag, 0.005f);
            Assert.Greater(lag, 0.5f);
            Assert.Less(lag, MaximumLag);
            Assert.AreEqual(0f, cameraPosition.x, PositionTolerance);
            Assert.AreEqual(0f, cameraPosition.y, PositionTolerance);
        }

        [Test]
        public void SuddenTeleportWithoutNotification_LagClampedToMaxLag()
        {
            Transform root = rig.CreateBody("Truck");
            VehicleCameraTarget target = rig.CreateTarget(root, CreateDrivingProfile(1f, 3f, 3f));
            target.SetSuppliedSpeed(0f);
            Camera camera = rig.CreateCamera("Camera");
            CameraSystemController controller = rig.CreateController("Controller", camera, target, DrivingView);
            Assert.AreEqual(CameraCommandResult.Accepted, controller.Activate());
            RunFrames(controller, 25);
            AssertPosition(new Vector3(0f, 0f, -3f), camera.transform.position);

            root.position = new Vector3(0f, 0f, 50f);
            controller.UpdateCameraFrame(FrameTime);

            Vector3 targetPosition = new Vector3(0f, 0f, 47f);
            Assert.AreEqual(MaximumLag, Vector3.Distance(targetPosition, camera.transform.position), PositionTolerance);
            AssertPosition(new Vector3(0f, 0f, 44f), camera.transform.position);
        }

        [Test]
        public void SpeedChange_OnlyDistanceChanges_FieldOfViewFixed()
        {
            Transform root = rig.CreateBody("Truck");
            VehicleCameraTarget target = rig.CreateTarget(root, CreateDrivingProfile(1f, 3f, 3f));
            target.SetSuppliedSpeed(0f);
            Camera camera = rig.CreateCamera("Camera");
            camera.fieldOfView = 47f;
            CameraSystemController controller = rig.CreateController("Controller", camera, target, DrivingView);
            Assert.AreEqual(CameraCommandResult.Accepted, controller.Activate());
            Vector3 watchPoint = new Vector3(0f, 1f, 0f);
            RunFrames(controller, 50);
            AssertPosition(new Vector3(0f, 0f, -3f), camera.transform.position);

            target.SetSuppliedSpeed(30f);
            RunFramesOnDistanceAxis(controller, camera);

            AssertPosition(new Vector3(0f, 0f, -6f), camera.transform.position);
            Assert.AreEqual(47f, camera.fieldOfView, 0.0001f);
            Assert.Less(Vector3.Angle(watchPoint - camera.transform.position, camera.transform.forward), 0.05f);

            target.SetSuppliedSpeed(0f);
            RunFramesOnDistanceAxis(controller, camera);

            AssertPosition(new Vector3(0f, 0f, -3f), camera.transform.position);
            Assert.AreEqual(47f, camera.fieldOfView, 0.0001f);
            Assert.Less(Vector3.Angle(watchPoint - camera.transform.position, camera.transform.forward), 0.05f);
        }

        [Test]
        public void LowSpeedCloserHighSpeedFarther_SideEffectSmallerButNonZero()
        {
            Transform root = rig.CreateBody("Truck");
            VehicleProfile profile = CreateDrivingProfile(1f, 3f, 3f);
            VehicleViewEntry drivingView;
            Assert.IsTrue(profile.TryGetView(DrivingView, out drivingView));
            VehicleViewEntry sideView = profile.AddView(SideView, drivingView.Preset, drivingView.OrbitId);
            sideView.ConfigureDefaultPose(new OrbitPose(90f, 0f, 0f));
            VehicleCameraTarget target = rig.CreateTarget(root, profile);
            Camera camera = rig.CreateCamera("Camera");
            CameraSystemController controller = rig.CreateController("Controller", camera, target, DrivingView);
            target.SetSuppliedSpeed(2f);
            Assert.AreEqual(CameraCommandResult.Accepted, controller.Activate());

            RunSeconds(controller, SettleSeconds);
            Vector3 behindSlow = camera.transform.position;
            target.SetSuppliedSpeed(30f);
            RunSeconds(controller, SettleSeconds);
            Vector3 behindFast = camera.transform.position;

            controller.Deactivate();
            controller.Configure(camera, target, SideView);
            target.SetSuppliedSpeed(2f);
            Assert.AreEqual(CameraCommandResult.Accepted, controller.Activate());
            RunSeconds(controller, SettleSeconds);
            Vector3 sideSlow = camera.transform.position;
            target.SetSuppliedSpeed(30f);
            RunSeconds(controller, SettleSeconds);
            Vector3 sideFast = camera.transform.position;

            AssertPosition(new Vector3(0f, 0f, -3f), behindSlow);
            AssertPosition(new Vector3(0f, 0f, -6f), behindFast);
            AssertPosition(new Vector3(1f, 0f, 0f), sideSlow);
            AssertPosition(new Vector3(1.75f, 0f, 0f), sideFast);
            float behindEffect = Vector3.Distance(behindSlow, behindFast);
            float sideEffect = Vector3.Distance(sideSlow, sideFast);
            Assert.Less(sideEffect, behindEffect);
            Assert.Greater(sideEffect, 0.5f);
            Assert.AreEqual(0.25f * behindEffect, sideEffect, 0.002f);
        }

        [Test]
        public void FrameRate30Vs144_SamePathWithinTwoCentimetres()
        {
            List<Vector3> path30 = new List<Vector3>();
            List<Vector3> path144 = new List<Vector3>();

            RunScriptedPath(30, path30);
            RunScriptedPath(144, path144);

            Assert.AreEqual(ScriptedPathSeconds * SamplesPerSecond, path30.Count);
            Assert.AreEqual(path30.Count, path144.Count);
            float maximumDifference = 0f;
            for (int index = 0; index < path30.Count; index++)
            {
                maximumDifference = Mathf.Max(maximumDifference, Vector3.Distance(path30[index], path144[index]));
            }

            Debug.Log($"Driving follow 30 vs 144 fps: maximum camera position difference {maximumDifference * 100f:F3} cm over {path30.Count} samples.");
            Assert.Less(maximumDifference, 0.02f);
        }

        [Test]
        public void HighSpeedUTurnWithTrailer_CameraNeverInsideTrailerBounds()
        {
            Transform truck = rig.CreateBody("Truck");
            Transform trailer = rig.CreateBody("Trailer");
            Vector3 hitchLocal = new Vector3(0f, 0f, -3.5f);
            Vector3 trailerFrontAnchor = new Vector3(0f, 0f, TrailerOrbitFront + 0.5f);
            trailer.position = hitchLocal - trailerFrontAnchor;
            VehicleProfile truckProfile = CreateDrivingProfile(1f, 3f, 3f);
            VehicleProfile trailerProfile = rig.CreateProfile(TrailerOrbitHalfWidth, TrailerOrbitFront, TrailerOrbitRear);
            VehicleCameraTarget target = rig.CreateTarget(truck, truckProfile);
            Assert.AreEqual(VehicleTargetResult.Accepted, target.Attach(trailer, trailerProfile, ChainEnd.Rear));
            target.SetSuppliedSpeed(UTurnSpeed);
            Camera camera = rig.CreateCamera("Camera");
            CameraSystemController controller = rig.CreateController("Controller", camera, target, DrivingView);
            Assert.AreEqual(CameraCommandResult.Accepted, controller.Activate());

            Vector3 axle = trailer.TransformPoint(new Vector3(0f, 0f, TrailerAxleZ));
            float minimumClearance = float.MaxValue;
            float minimumClearanceTime = 0f;
            float turnMinimumClearance = float.MaxValue;
            float turnMinimumClearanceTime = 0f;
            float maximumArticulation = 0f;
            int frameCount = Mathf.RoundToInt(UTurnDuration / UTurnFrameTime);
            for (int frame = 1; frame <= frameCount; frame++)
            {
                for (int substep = 1; substep <= TrailerSubsteps; substep++)
                {
                    float substepTime = (frame - 1 + (float)substep / TrailerSubsteps) * UTurnFrameTime;
                    SetUTurnPose(truck, substepTime);
                    axle = DragTrailer(truck, trailer, axle, hitchLocal, trailerFrontAnchor);
                }

                controller.UpdateCameraFrame(UTurnFrameTime);
                float time = frame * UTurnFrameTime;
                float clearance = GetFootprintClearance(trailer.InverseTransformPoint(camera.transform.position));
                maximumArticulation = Mathf.Max(maximumArticulation, Mathf.Abs(Vector3.SignedAngle(truck.forward, trailer.forward, Vector3.up)));
                if (clearance < minimumClearance)
                {
                    minimumClearance = clearance;
                    minimumClearanceTime = time;
                }

                if (UTurnSpeed * time >= UTurnStraight && clearance < turnMinimumClearance)
                {
                    turnMinimumClearance = clearance;
                    turnMinimumClearanceTime = time;
                }
            }

            Debug.Log($"High-speed U-turn ({UTurnSpeed:F0} m/s, radius {UTurnRadius:F0} m, trailer articulation up to {maximumArticulation:F1} degrees): minimum horizontal clearance from the trailer footprint {minimumClearance:F3} m at {minimumClearanceTime:F2} s; during and after the turn {turnMinimumClearance:F3} m at {turnMinimumClearanceTime:F2} s.");
            Assert.Greater(maximumArticulation, 20f);
            Assert.Greater(minimumClearance, 0f);
            Assert.Greater(turnMinimumClearance, 0f);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            rig.Dispose();
            yield return null;
        }

        private VehicleProfile CreateDrivingProfile(float halfWidth, float front, float rear)
        {
            VehicleProfile profile = rig.CreateProfile(halfWidth, front, rear);
            VehicleOrbit orbit;
            Assert.IsTrue(profile.TryGetOrbit(CameraTestRig.OrbitName, out orbit));
            profile.AddView(DrivingView, rig.CreatePreset(CameraViewType.Driving), orbit.Id);
            return profile;
        }

        private void RunFrames(CameraSystemController controller, int count)
        {
            for (int frame = 0; frame < count; frame++)
            {
                controller.UpdateCameraFrame(FrameTime);
            }
        }

        private void RunSeconds(CameraSystemController controller, float seconds)
        {
            RunFrames(controller, Mathf.RoundToInt(seconds / FrameTime));
        }

        private void RunFramesOnDistanceAxis(CameraSystemController controller, Camera camera)
        {
            int frameCount = Mathf.RoundToInt(SettleSeconds / FrameTime);
            for (int frame = 0; frame < frameCount; frame++)
            {
                controller.UpdateCameraFrame(FrameTime);
                Vector3 position = camera.transform.position;
                Assert.AreEqual(0f, position.x, PositionTolerance);
                Assert.AreEqual(0f, position.y, PositionTolerance);
                Assert.AreEqual(47f, camera.fieldOfView, 0.0001f);
            }
        }

        private void RunScriptedPath(int framesPerSecond, List<Vector3> samples)
        {
            Transform root = rig.CreateBody($"Truck {framesPerSecond}");
            VehicleCameraTarget target = rig.CreateTarget(root, CreateDrivingProfile(1f, 3f, 3f));
            Camera camera = rig.CreateCamera($"Camera {framesPerSecond}");
            CameraSystemController controller = rig.CreateController($"Controller {framesPerSecond}", camera, target, DrivingView);
            SetCirclePose(root, target, 0f);
            Assert.AreEqual(CameraCommandResult.Accepted, controller.Activate());
            float deltaTime = 1f / framesPerSecond;
            int framesPerSample = framesPerSecond / SamplesPerSecond;
            int frameCount = framesPerSecond * ScriptedPathSeconds;
            for (int frame = 1; frame <= frameCount; frame++)
            {
                SetCirclePose(root, target, (float)frame / framesPerSecond);
                controller.UpdateCameraFrame(deltaTime);
                if (frame % framesPerSample == 0)
                {
                    samples.Add(camera.transform.position);
                }
            }
        }

        private void SetCirclePose(Transform root, VehicleCameraTarget target, float time)
        {
            float angle = CircleStartRate * time + 0.5f * CircleAngularAcceleration * time * time;
            Vector3 position = new Vector3(CircleRadius - CircleRadius * Mathf.Cos(angle), 0f, CircleRadius * Mathf.Sin(angle));
            root.SetPositionAndRotation(position, Quaternion.Euler(0f, angle * Mathf.Rad2Deg, 0f));
            target.SetSuppliedSpeed(CircleRadius * (CircleStartRate + CircleAngularAcceleration * time));
        }

        private void SetUTurnPose(Transform truck, float time)
        {
            float distance = UTurnSpeed * time;
            float arcLength = Mathf.PI * UTurnRadius;
            Vector3 position;
            float yaw;
            if (distance <= UTurnStraight)
            {
                position = new Vector3(0f, 0f, distance);
                yaw = 0f;
            }
            else if (distance <= UTurnStraight + arcLength)
            {
                float angle = (distance - UTurnStraight) / UTurnRadius;
                position = new Vector3(UTurnRadius * Mathf.Cos(angle) - UTurnRadius, 0f, UTurnStraight + UTurnRadius * Mathf.Sin(angle));
                yaw = -angle * Mathf.Rad2Deg;
            }
            else
            {
                float after = distance - UTurnStraight - arcLength;
                position = new Vector3(-2f * UTurnRadius, 0f, UTurnStraight - after);
                yaw = -180f;
            }

            truck.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
        }

        private Vector3 DragTrailer(Transform truck, Transform trailer, Vector3 axle, Vector3 hitchLocal, Vector3 trailerFrontAnchor)
        {
            Vector3 hitch = truck.TransformPoint(hitchLocal);
            Vector3 towDirection = hitch - axle;
            towDirection.y = 0f;
            towDirection.Normalize();
            Quaternion rotation = Quaternion.LookRotation(towDirection, Vector3.up);
            trailer.SetPositionAndRotation(hitch - rotation * trailerFrontAnchor, rotation);
            return hitch - towDirection * (trailerFrontAnchor.z - TrailerAxleZ);
        }

        private float GetFootprintClearance(Vector3 trailerLocalPosition)
        {
            float outsideX = Mathf.Abs(trailerLocalPosition.x) - TrailerHalfWidth;
            float outsideZ = Mathf.Max(trailerLocalPosition.z - TrailerFrontZ, TrailerRearZ - trailerLocalPosition.z);
            if (outsideX <= 0f && outsideZ <= 0f)
            {
                return Mathf.Max(outsideX, outsideZ);
            }

            return new Vector2(Mathf.Max(outsideX, 0f), Mathf.Max(outsideZ, 0f)).magnitude;
        }

        private void AssertPosition(Vector3 expected, Vector3 actual)
        {
            Assert.Less(Vector3.Distance(expected, actual), PositionTolerance, $"Expected {expected:F4}, got {actual:F4}");
        }
    }
}
