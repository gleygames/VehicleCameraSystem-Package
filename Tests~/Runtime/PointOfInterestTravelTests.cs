using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Gley.CameraSystem.Tests.PlayMode
{
    public class PointOfInterestTravelTests
    {
        private const float FrameTime = 0.02f;
        private const float DurationFrameTime = 0.03125f;
        private const float PositionTolerance = 0.001f;
        private const float DistanceTolerance = 0.0001f;
        private const float PathTolerance = 0.25f;
        private const float AimTolerance = 0.05f;
        private const float RectangleLength = 16f;
        private const float MaximumCornerRadius = 1.5f;
        private const float ArcHandleScale = 0.55228475f;
        private const float HitchGap = 0.5f;
        private const float SectionMargin = 0.5f;
        private const int RoundedKnotCount = 8;
        private const int MaximumFrames = 2000;
        private const int DenseSamplesPerSegment = 20000;

        private readonly List<CommandEndResult> endedResults = new List<CommandEndResult>();
        private readonly List<int> endedCommandIds = new List<int>();

        private CameraTestRig rig;
        private CameraSystemController controller;
        private Camera camera;

        [SetUp]
        public void SetUp()
        {
            rig = new CameraTestRig();
            endedResults.Clear();
            endedCommandIds.Clear();
            controller = null;
            camera = null;
        }

        [Test]
        public void WheelToPlateToEngine_StopsExactlyOnEach()
        {
            ActivatePointProfile();

            TravelAlongRectangle("wheel", new Vector3(-1f, 0f, -1.5f), 3.5f);
            AssertAimAt(new Vector3(-0.5f, 0.5f, -1.5f));
            TravelAlongRectangle("plate", new Vector3(0f, 0f, -3f), 1f);
            AssertAimAt(new Vector3(0f, 0.5f, -2.5f));
            TravelAlongRectangle("engine", new Vector3(0f, 0f, 3f), 9f);
            AssertAimAt(new Vector3(0f, 1f, 2f));
        }

        [Test]
        public void RequestNext_AtLastWithWrap_GoesToFirst()
        {
            ActivatePointProfile();
            TravelToPoint("engine", new Vector3(0f, 0f, 3f), 9f);

            int commandId;
            Assert.AreEqual(CameraCommandResult.Accepted, controller.RequestNextPoint(true, new TravelRequest(TravelDirection.Shortest), out commandId));
            Assert.AreEqual(CommandEndResult.Completed, RunUntilEnded(commandId));

            AssertArrived(new Vector3(1f, 0f, 0f), 13f);
        }

        [Test]
        public void RequestNext_AtLastWithoutWrap_NoNextPoint_StaysPut()
        {
            ActivatePointProfile();
            TravelToPoint("engine", new Vector3(0f, 0f, 3f), 9f);

            int commandId;
            Assert.AreEqual(CameraCommandResult.NoNextPoint, controller.RequestNextPoint(false, new TravelRequest(TravelDirection.Shortest), out commandId));
            Assert.AreEqual(0, commandId);
            RunFrames(20);

            AssertArrived(new Vector3(0f, 0f, 3f), 9f);
            Assert.AreEqual(1, endedCommandIds.Count);
        }

        [Test]
        public void RequestPrevious_AtFirstWithWrap_GoesToLast()
        {
            ActivatePointProfile();
            TravelToPoint("Right", new Vector3(1f, 0f, 0f), 13f);

            int commandId;
            Assert.AreEqual(CameraCommandResult.Accepted, controller.RequestPreviousPoint(true, new TravelRequest(TravelDirection.Shortest), out commandId));
            Assert.AreEqual(CommandEndResult.Completed, RunUntilEnded(commandId));

            AssertArrived(new Vector3(0f, 0f, 3f), 9f);
        }

        [Test]
        public void ShortestRoute_ChoosesShorterDirection()
        {
            ActivatePointProfile();

            float backwardAcrossWrap = TravelAlongRectangle("Right", new Vector3(1f, 0f, 0f), 13f);
            float forwardAcrossWrap = TravelAlongRectangle("wheel", new Vector3(-1f, 0f, -1.5f), 3.5f);

            Assert.AreEqual(4f, backwardAcrossWrap, PathTolerance);
            Assert.AreEqual(6.5f, forwardAcrossWrap, PathTolerance);
        }

        [Test]
        public void ForcedDirectionCrossingLimit_Unreachable_NoMovement()
        {
            rig.PresentationPreset.ConfigureAngleLimits(true, -60f, 100f);
            ActivatePointProfile();
            Vector3 start = camera.transform.position;

            int commandId;
            Assert.AreEqual(CameraCommandResult.Unreachable, controller.RequestPoint("wheel", new TravelRequest(TravelDirection.CounterClockwise), out commandId));
            Assert.AreEqual(0, commandId);
            Assert.AreEqual(CameraCommandResult.Unreachable, controller.RequestPoint("Right", new TravelRequest(TravelDirection.Clockwise), out commandId));
            Assert.AreEqual(0, commandId);
            RunFrames(20);

            AssertPosition(start, camera.transform.position);
            AssertPosition(new Vector3(0f, 0f, -3f), camera.transform.position);
            Assert.AreEqual(0, endedCommandIds.Count);

            Assert.AreEqual(CameraCommandResult.Accepted, controller.RequestPoint("wheel", new TravelRequest(TravelDirection.Clockwise), out commandId));
            Assert.AreEqual(CommandEndResult.Completed, RunUntilEnded(commandId));
            AssertArrived(new Vector3(-1f, 0f, -1.5f), 3.5f);
        }

        [Test]
        public void DestinationOutsideLimits_Unreachable()
        {
            rig.PresentationPreset.ConfigureAngleLimits(true, -60f, 100f);
            ActivatePointProfile();

            int commandId;
            Assert.AreEqual(CameraCommandResult.Unreachable, controller.RequestPoint("engine", new TravelRequest(TravelDirection.Shortest), out commandId));
            Assert.AreEqual(0, commandId);
            Assert.AreEqual(CameraCommandResult.Unreachable, controller.RequestPoint("engine", new TravelRequest(TravelDirection.CounterClockwise), out commandId));
            Assert.AreEqual(CameraCommandResult.Unreachable, controller.RequestPoint("engine", new TravelRequest(TravelDirection.Clockwise), out commandId));
            RunFrames(20);

            AssertArrived(new Vector3(0f, 0f, -3f), 1f);
            Assert.AreEqual(0, endedCommandIds.Count);
        }

        [Test]
        public void UnknownName_PointNotFound_NothingChanges()
        {
            ActivatePointProfile();
            int travelId;
            Assert.AreEqual(CameraCommandResult.Accepted, controller.RequestPoint("wheel", new TravelRequest(TravelDirection.Shortest), out travelId));
            RunFrames(5);
            Vector3 beforeRejection = camera.transform.position;

            int commandId;
            Assert.AreEqual(CameraCommandResult.PointNotFound, controller.RequestPoint("steering wheel", new TravelRequest(TravelDirection.Shortest), out commandId));
            Assert.AreEqual(0, commandId);
            Assert.AreEqual(CameraCommandResult.PointNotFound, controller.RequestPoint("Hidden", new TravelRequest(TravelDirection.Shortest), out commandId));
            Assert.AreEqual(0, commandId);
            Assert.AreEqual(CameraCommandResult.PointNotFound, controller.RequestPoint(string.Empty, new TravelRequest(TravelDirection.Shortest), out commandId));
            Assert.AreEqual(0, commandId);

            AssertPosition(beforeRejection, camera.transform.position);
            Assert.AreEqual(0, endedCommandIds.Count);
            Assert.AreEqual(CommandEndResult.Completed, RunUntilEnded(travelId));
            AssertArrived(new Vector3(-1f, 0f, -1.5f), 3.5f);
            Assert.AreEqual(1, endedCommandIds.Count);
        }

        [Test]
        public void NewRequestDuringTravel_OldEndsReplaced()
        {
            ActivatePointProfile();
            int firstId;
            Assert.AreEqual(CameraCommandResult.Accepted, controller.RequestPoint("engine", new TravelRequest(TravelDirection.Shortest), out firstId));
            RunFrames(20);
            Vector3 beforeReplacement = camera.transform.position;
            Assert.Greater(Vector3.Distance(new Vector3(0f, 0f, -3f), beforeReplacement), 0.5f);

            int secondId;
            Assert.AreEqual(CameraCommandResult.Accepted, controller.RequestPoint("plate", new TravelRequest(TravelDirection.Shortest), out secondId));

            Assert.AreNotEqual(firstId, secondId);
            Assert.AreEqual(1, endedCommandIds.Count);
            Assert.AreEqual(firstId, endedCommandIds[0]);
            Assert.AreEqual(CommandEndResult.Replaced, endedResults[0]);
            controller.UpdateCameraFrame(FrameTime);
            Assert.Less(Vector3.Distance(beforeReplacement, camera.transform.position), 0.05f);

            Assert.AreEqual(CommandEndResult.Completed, RunUntilEnded(secondId));
            AssertArrived(new Vector3(0f, 0f, -3f), 1f);
            Assert.AreEqual(2, endedCommandIds.Count);
        }

        [Test]
        public void Duration_ArrivesAtDuration()
        {
            ActivatePointProfile();
            int commandId;
            TravelRequest request = new TravelRequest(TravelDirection.Shortest, new TransitionOptions(TransitionMode.Duration, 1.5f));
            Assert.AreEqual(CameraCommandResult.Accepted, controller.RequestPoint("wheel", request, out commandId));

            for (int frame = 0; frame < 47; frame++)
            {
                controller.UpdateCameraFrame(DurationFrameTime);
            }

            Assert.AreEqual(0, endedCommandIds.Count);
            Assert.Greater(Mathf.Abs(3.5f - Mathf.Repeat(controller.OrbitDistance, RectangleLength)), PositionTolerance);

            controller.UpdateCameraFrame(DurationFrameTime);

            Assert.AreEqual(1, endedCommandIds.Count);
            Assert.AreEqual(commandId, endedCommandIds[0]);
            Assert.AreEqual(CommandEndResult.Completed, endedResults[0]);
            AssertArrived(new Vector3(-1f, 0f, -1.5f), 3.5f);
        }

        [Test]
        public void Arrival_RaisesCompletedWithSameCommandId()
        {
            ActivatePointProfile();
            int commandId;
            Assert.AreEqual(CameraCommandResult.Accepted, controller.RequestPoint("wheel", new TravelRequest(TravelDirection.Shortest), out commandId));
            Assert.Greater(commandId, 0);

            Assert.AreEqual(CommandEndResult.Completed, RunUntilEnded(commandId));
            RunFrames(20);

            Assert.AreEqual(1, endedCommandIds.Count);
            Assert.AreEqual(commandId, endedCommandIds[0]);
            Assert.AreEqual(CommandEndResult.Completed, endedResults[0]);
        }

        [Test]
        public void NextOrder_FollowsMarkerListOrder_NotOrbitPosition()
        {
            ActivatePointProfile();
            TravelToPoint("Right", new Vector3(1f, 0f, 0f), 13f);

            TravelToNext(new Vector3(-1f, 0f, -1.5f), 3.5f);
            TravelToNext(new Vector3(0f, 0f, -3f), 1f);
            TravelToNext(new Vector3(0f, 0f, 3f), 9f);
            TravelToNext(new Vector3(1f, 0f, 0f), 13f);
        }

        [Test]
        public void RequestNext_NoHistory_GoesToNearestPointAhead()
        {
            ActivatePointProfile(-90f);
            AssertArrived(new Vector3(-1f, 0f, 0f), 5f);

            TravelToNext(new Vector3(-1f, 0f, -1.5f), 3.5f);
        }

        [Test]
        public void RequestPrevious_NoHistory_GoesToNearestPointBehind()
        {
            ActivatePointProfile(-90f);

            int commandId;
            Assert.AreEqual(CameraCommandResult.Accepted, controller.RequestPreviousPoint(true, new TravelRequest(TravelDirection.Shortest), out commandId));
            Assert.AreEqual(CommandEndResult.Completed, RunUntilEnded(commandId));
            AssertArrived(new Vector3(0f, 0f, 3f), 9f);
        }

        [Test]
        public void RequestNext_NoHistoryOnPoint_GoesToFollowingPoint()
        {
            ActivatePointProfile();

            TravelToNext(new Vector3(0f, 0f, 3f), 9f);
        }

        [Test]
        public void LongThreeBodyChain_ExactStopWithinOneMillimetre()
        {
            float halfWidth = 2.25f;
            float truckLength = 4f;
            float trailerLength = 7.8f;
            VehicleProfile truckProfile = CreateRoundedProfile(halfWidth, truckLength, truckLength);
            VehicleProfile trailerProfile = CreateRoundedProfile(halfWidth, trailerLength, trailerLength);
            VehicleOrbit truckOrbit;
            VehicleOrbit trailerOrbit;
            Assert.IsTrue(truckProfile.TryGetOrbit(CameraTestRig.OrbitName, out truckOrbit));
            Assert.IsTrue(trailerProfile.TryGetOrbit(CameraTestRig.OrbitName, out trailerOrbit));
            float mirrorProgress = GetRoundedProgress(halfWidth, truckLength, truckLength, 2.5f, 1f, 1f, 0f);
            float plateProgress = GetRoundedProgress(halfWidth, trailerLength, trailerLength, 1f, 0.5f, 0f, 0f);
            truckProfile.AddWatchMarker(truckOrbit, "mirror", mirrorProgress, new Vector3(0f, 1f, 1f), true);
            trailerProfile.AddWatchMarker(trailerOrbit, "plate", plateProgress, new Vector3(0f, 1f, -6f), true);
            VehicleViewEntry view = truckProfile.AddView(CameraTestRig.PresentationView, rig.PresentationPreset, truckOrbit.Id);
            view.ConfigureDefaultPose(new OrbitPose(90f, 0f, 0f));
            float firstTrailerOffset = -truckLength - HitchGap - trailerLength - HitchGap;
            float secondTrailerOffset = firstTrailerOffset - trailerLength - HitchGap - trailerLength - HitchGap;
            Transform truck = rig.CreateBody("Truck");
            Transform firstTrailer = rig.CreateBody("First Trailer");
            firstTrailer.position = new Vector3(0f, 0f, firstTrailerOffset);
            Transform secondTrailer = rig.CreateBody("Second Trailer");
            secondTrailer.position = new Vector3(0f, 0f, secondTrailerOffset);
            VehicleCameraTarget target = rig.CreateTarget(truck, truckProfile);
            Assert.AreEqual(VehicleTargetResult.Accepted, target.Attach(firstTrailer, trailerProfile, ChainEnd.Rear));
            Assert.AreEqual(VehicleTargetResult.Accepted, target.Attach(secondTrailer, trailerProfile, ChainEnd.Rear));
            ChainOrbit chainOrbit = new ChainOrbitBuilder().Build(target, CameraTestRig.OrbitName);
            Assert.IsNotNull(chainOrbit);
            Assert.AreEqual(ChainOrbitAssemblyResult.Valid, chainOrbit.AssemblyResult);
            Assert.IsTrue(chainOrbit.HasValidWatchMarkers);
            Activate(target);

            int commandId;
            Assert.AreEqual(CameraCommandResult.Accepted, controller.RequestPoint("mirror", new TravelRequest(TravelDirection.Shortest), out commandId));
            Assert.AreEqual(CommandEndResult.Completed, RunUntilEnded(commandId));
            AssertPosition(EvaluateAtProgress(truckOrbit, mirrorProgress), camera.transform.position);

            Assert.AreEqual(CameraCommandResult.Accepted, controller.RequestPoint("plate", new TravelRequest(TravelDirection.Shortest), out commandId));
            Assert.AreEqual(CommandEndResult.Completed, RunUntilEnded(commandId));
            AssertPosition(EvaluateAtProgress(trailerOrbit, plateProgress) + new Vector3(0f, 0f, secondTrailerOffset), camera.transform.position);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (controller != null)
            {
                controller.CommandEnded -= RecordCommandEnded;
            }

            rig.Dispose();
            yield return null;
        }

        private void ActivatePointProfile()
        {
            ActivatePointProfile(0f);
            AssertArrived(new Vector3(0f, 0f, -3f), 1f);
        }

        private void ActivatePointProfile(float defaultBearing)
        {
            Transform root = rig.CreateBody("Truck");
            VehicleProfile profile = rig.CreateProfile(1f, 3f, 3f);
            VehicleOrbit orbit;
            Assert.IsTrue(profile.TryGetOrbit(CameraTestRig.OrbitName, out orbit));
            profile.AddWatchMarker(orbit, "wheel", 3.5f / RectangleLength, new Vector3(-0.5f, 0.5f, -1.5f), true);
            profile.AddWatchMarker(orbit, "plate", 1f / RectangleLength, new Vector3(0f, 0.5f, -2.5f), true);
            profile.AddWatchMarker(orbit, "engine", 9f / RectangleLength, new Vector3(0f, 1f, 2f), true);
            profile.AddWatchMarker(orbit, "Hidden", 6f / RectangleLength, new Vector3(0f, 1f, 1f), false);
            VehicleViewEntry view;
            Assert.IsTrue(profile.TryGetView(CameraTestRig.PresentationView, out view));
            view.ConfigureDefaultPose(new OrbitPose(defaultBearing, 0f, 0f));
            Activate(rig.CreateTarget(root, profile));
        }

        private void Activate(VehicleCameraTarget target)
        {
            camera = rig.CreateCamera("Camera");
            controller = rig.CreateController("Controller", camera, target, CameraTestRig.PresentationView);
            controller.CommandEnded += RecordCommandEnded;
            Assert.AreEqual(CameraCommandResult.Accepted, controller.Activate());
            controller.UpdateCameraFrame(FrameTime);
        }

        private void RecordCommandEnded(int commandId, CommandEndResult result)
        {
            endedCommandIds.Add(commandId);
            endedResults.Add(result);
        }

        private float TravelAlongRectangle(string pointName, Vector3 expectedPosition, float expectedDistance)
        {
            int commandId;
            Assert.AreEqual(CameraCommandResult.Accepted, controller.RequestPoint(pointName, new TravelRequest(TravelDirection.Shortest), out commandId));
            float pathLength = 0f;
            Vector3 previous = camera.transform.position;
            for (int frame = 0; frame < MaximumFrames && !endedCommandIds.Contains(commandId); frame++)
            {
                controller.UpdateCameraFrame(FrameTime);
                Vector3 current = camera.transform.position;
                Assert.Less(DistanceToRectangle(current), PositionTolerance, $"Camera left the orbit at {current:F4}");
                pathLength += Vector3.Distance(previous, current);
                previous = current;
            }

            AssertEndedWith(commandId, CommandEndResult.Completed);
            AssertArrived(expectedPosition, expectedDistance);
            return pathLength;
        }

        private void AssertAimAt(Vector3 watchPoint)
        {
            Assert.Less(Vector3.Angle(watchPoint - camera.transform.position, camera.transform.forward), AimTolerance);
        }

        private void TravelToPoint(string pointName, Vector3 expectedPosition, float expectedDistance)
        {
            int commandId;
            Assert.AreEqual(CameraCommandResult.Accepted, controller.RequestPoint(pointName, new TravelRequest(TravelDirection.Shortest), out commandId));
            Assert.AreEqual(CommandEndResult.Completed, RunUntilEnded(commandId));
            AssertArrived(expectedPosition, expectedDistance);
        }

        private CommandEndResult RunUntilEnded(int commandId)
        {
            Assert.Greater(commandId, 0);
            for (int frame = 0; frame < MaximumFrames && !endedCommandIds.Contains(commandId); frame++)
            {
                controller.UpdateCameraFrame(FrameTime);
            }

            int index = endedCommandIds.IndexOf(commandId);
            Assert.GreaterOrEqual(index, 0, $"Command {commandId} did not end within {MaximumFrames} frames.");
            return endedResults[index];
        }

        private void AssertArrived(Vector3 expectedPosition, float expectedDistance)
        {
            AssertPosition(expectedPosition, camera.transform.position);
            Assert.AreEqual(expectedDistance, Mathf.Repeat(controller.OrbitDistance, RectangleLength), DistanceTolerance);
        }

        private void AssertPosition(Vector3 expected, Vector3 actual)
        {
            Assert.Less(Vector3.Distance(expected, actual), PositionTolerance, $"Expected {expected:F4}, got {actual:F4}");
        }

        private void RunFrames(int frameCount)
        {
            for (int frame = 0; frame < frameCount; frame++)
            {
                controller.UpdateCameraFrame(FrameTime);
            }
        }

        private float DistanceToRectangle(Vector3 position)
        {
            float outsideX = Mathf.Abs(position.x) - 1f;
            float outsideZ = Mathf.Abs(position.z) - 3f;
            float planarDistance;
            if (outsideX <= 0f && outsideZ <= 0f)
            {
                planarDistance = Mathf.Min(-outsideX, -outsideZ);
            }
            else
            {
                planarDistance = new Vector2(Mathf.Max(outsideX, 0f), Mathf.Max(outsideZ, 0f)).magnitude;
            }

            return planarDistance + Mathf.Abs(position.y);
        }

        private void AssertEndedWith(int commandId, CommandEndResult expected)
        {
            int index = endedCommandIds.IndexOf(commandId);
            Assert.GreaterOrEqual(index, 0, $"Command {commandId} did not end within {MaximumFrames} frames.");
            Assert.AreEqual(expected, endedResults[index]);
        }

        private void TravelToNext(Vector3 expectedPosition, float expectedDistance)
        {
            int commandId;
            Assert.AreEqual(CameraCommandResult.Accepted, controller.RequestNextPoint(true, new TravelRequest(TravelDirection.Shortest), out commandId));
            Assert.AreEqual(CommandEndResult.Completed, RunUntilEnded(commandId));
            AssertArrived(expectedPosition, expectedDistance);
        }

        private VehicleProfile CreateRoundedProfile(float halfWidth, float front, float rear)
        {
            VehicleProfile profile = rig.CreateEmptyProfile();
            VehicleOrbit orbit = profile.AddOrbit(CameraTestRig.OrbitName);
            float radius = Mathf.Min(MaximumCornerRadius, halfWidth * 0.5f);
            Vector3[] corners =
            {
                new Vector3(halfWidth, 0f, -rear),
                new Vector3(-halfWidth, 0f, -rear),
                new Vector3(-halfWidth, 0f, front),
                new Vector3(halfWidth, 0f, front)
            };
            Vector3[] anchors =
            {
                new Vector3(halfWidth, 0f, -rear + radius),
                new Vector3(halfWidth - radius, 0f, -rear),
                new Vector3(-halfWidth + radius, 0f, -rear),
                new Vector3(-halfWidth, 0f, -rear + radius),
                new Vector3(-halfWidth, 0f, front - radius),
                new Vector3(-halfWidth + radius, 0f, front),
                new Vector3(halfWidth - radius, 0f, front),
                new Vector3(halfWidth, 0f, front - radius)
            };
            List<BezierOrbitKnot> knots = new List<BezierOrbitKnot>();
            for (int index = 0; index < RoundedKnotCount; index++)
            {
                Vector3 anchor = anchors[index];
                Vector3 corner = corners[index / 2];
                Vector3 previous = anchors[(index + RoundedKnotCount - 1) % RoundedKnotCount];
                Vector3 next = anchors[(index + 1) % RoundedKnotCount];
                BezierOrbitKnot knot = new BezierOrbitKnot();
                if (index % 2 == 0)
                {
                    knot.Configure(anchor, anchor + (previous - anchor) / 3f, anchor + (corner - anchor) * ArcHandleScale);
                }
                else
                {
                    knot.Configure(anchor, anchor + (corner - anchor) * ArcHandleScale, anchor + (next - anchor) / 3f);
                }

                knots.Add(knot);
            }

            orbit.Configure(knots, Quaternion.identity);
            orbit.ConfigureOffsetRanges(-1f, 4f, -4f, 1f);
            OrbitRemovableSection frontSection = new OrbitRemovableSection();
            frontSection.Configure(GetRoundedProgress(halfWidth, front, rear, 2f, 1f, 1f, -SectionMargin), GetRoundedProgress(halfWidth, front, rear, 4f, 2f, 1f, SectionMargin));
            OrbitRemovableSection rearSection = new OrbitRemovableSection();
            rearSection.Configure(0f, GetRoundedProgress(halfWidth, front, rear, 2f, 1f, 0f, SectionMargin));
            orbit.ConfigureRemovableSections(frontSection, rearSection);
            VehicleConnectorAnchors connectorAnchors = new VehicleConnectorAnchors();
            connectorAnchors.Configure(new Vector3(0f, 0f, front + HitchGap), new Vector3(0f, 0f, -rear - HitchGap));
            profile.ConfigureConnectorAnchors(connectorAnchors);
            return profile;
        }

        private float GetRoundedProgress(float halfWidth, float front, float rear, float arcCount, float edgeCount, float sideCount, float extraDistance)
        {
            float radius = Mathf.Min(MaximumCornerRadius, halfWidth * 0.5f);
            float edge = 2f * halfWidth - 2f * radius;
            float side = front + rear - 2f * radius;
            float arc = Mathf.PI * radius * 0.5f;
            float perimeter = 2f * (edge + side) + 4f * arc;
            return (arcCount * arc + edgeCount * edge + sideCount * side + extraDistance) / perimeter;
        }

        private Vector3 EvaluateAtProgress(VehicleOrbit orbit, float progress)
        {
            IReadOnlyList<BezierOrbitKnot> knots = orbit.Knots;
            Vector3 point;
            double totalLength = 0d;
            for (int index = 0; index < knots.Count; index++)
            {
                totalLength += WalkSegment(knots, index, double.MaxValue, out point);
            }

            double remaining = progress * totalLength;
            for (int index = 0; index < knots.Count; index++)
            {
                double segmentLength = WalkSegment(knots, index, remaining, out point);
                if (remaining <= segmentLength)
                {
                    return point;
                }

                remaining -= segmentLength;
            }

            return knots[0].Anchor;
        }

        private double WalkSegment(IReadOnlyList<BezierOrbitKnot> knots, int index, double stopDistance, out Vector3 point)
        {
            BezierOrbitKnot start = knots[index];
            BezierOrbitKnot end = knots[(index + 1) % knots.Count];
            double previousX = start.Anchor.x;
            double previousZ = start.Anchor.z;
            double length = 0d;
            point = end.Anchor;
            for (int sample = 1; sample <= DenseSamplesPerSegment; sample++)
            {
                double t = (double)sample / DenseSamplesPerSegment;
                double x = EvaluateCubic(start.Anchor.x, start.OutgoingControlPoint.x, end.IncomingControlPoint.x, end.Anchor.x, t);
                double z = EvaluateCubic(start.Anchor.z, start.OutgoingControlPoint.z, end.IncomingControlPoint.z, end.Anchor.z, t);
                double step = System.Math.Sqrt((x - previousX) * (x - previousX) + (z - previousZ) * (z - previousZ));
                if (step > 0d && length + step >= stopDistance)
                {
                    double fraction = (stopDistance - length) / step;
                    point = new Vector3((float)(previousX + (x - previousX) * fraction), 0f, (float)(previousZ + (z - previousZ) * fraction));
                    return length + step;
                }

                length += step;
                previousX = x;
                previousZ = z;
            }

            return length;
        }

        private double EvaluateCubic(double start, double startControl, double endControl, double end, double t)
        {
            double inverse = 1d - t;
            return inverse * inverse * inverse * start + 3d * inverse * inverse * t * startControl + 3d * inverse * t * t * endControl + t * t * t * end;
        }
    }
}
