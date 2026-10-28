using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Gley.CameraSystem.Tests.EditMode
{
    public class OrbitMovementTests
    {
        private const float CircleRadius = 10f;
        private const float CircleTangentScale = 0.55228475f;

        private ChainTestProfileFactory factory;

        [SetUp]
        public void SetUp()
        {
            factory = new ChainTestProfileFactory();
        }

        [Test]
        public void HeldIntent_SpeedEasesUpWithHalfLife()
        {
            VehicleProfile profile = factory.CreateProfile("Main", 20f, 10f);
            OrbitMovement movement = CreateMovement(profile, CreateSettings(6f, 0.2f), 5f);

            movement.SetHeldIntent(1f, 0f, 0f);
            movement.UpdateOrbitMovement(0.1f);
            movement.UpdateOrbitMovement(0.1f);

            Assert.AreEqual(3f, movement.TravelSpeed, 0.0001f);

            movement.UpdateOrbitMovement(0.2f);

            Assert.AreEqual(4.5f, movement.TravelSpeed, 0.0001f);
        }

        [Test]
        public void IntentReleased_SpeedZeroSameFrame()
        {
            VehicleProfile profile = factory.CreateProfile("Main", 20f, 10f);
            OrbitMovement movement = CreateMovement(profile, CreateSettings(6f, 0.2f), 5f);
            movement.SetHeldIntent(1f, 0f, 0f);
            for (int step = 0; step < 10; step++)
            {
                movement.UpdateOrbitMovement(0.05f);
            }

            Assert.Greater(movement.TravelSpeed, 0f);
            float releasedDistance = movement.OrbitDistance;

            movement.SetHeldIntent(0f, 0f, 0f);
            movement.UpdateOrbitMovement(0.05f);

            Assert.AreEqual(0f, movement.TravelSpeed);
            Assert.AreEqual(releasedDistance, movement.OrbitDistance);
        }

        [Test]
        public void FiveTimesLongerOrbit_FiveTimesMoreTimeToCircle()
        {
            const float deltaTime = 0.01f;
            const float speed = 6f;
            const float halfLife = 0.06f;
            float startLoss = halfLife / Mathf.Log(2f);
            OrbitMovement shortMovement = CreateMovement(factory.CreateProfile("Main", 20f, 10f), CreateSettings(speed, halfLife), 5f);
            OrbitMovement longMovement = CreateMovement(factory.CreateProfile("Main", 100f, 50f), CreateSettings(speed, halfLife), 5f);

            float shortTime = MeasureTimeToCircle(shortMovement, 60f, deltaTime);
            float longTime = MeasureTimeToCircle(longMovement, 300f, deltaTime);

            Assert.AreEqual(60f / speed + startLoss, shortTime, 0.02f);
            Assert.AreEqual(300f / speed + startLoss, longTime, 0.02f);
            Assert.AreEqual(5f, (longTime - startLoss) / (shortTime - startLoss), 0.01f);
        }

        [Test]
        public void AngleLimits_TravelStopsAtLimit()
        {
            VehicleProfile profile = CreateCircleProfile();
            OrbitMovementSettings settings = new OrbitMovementSettings();
            settings.Configure(6f, 0f, 3f, 2f, 20f, 4f, 5f, true, -45f, 45f, 0f);
            ChainOrbit orbit = CreateOrbit(profile);
            OrbitMovement movement = new OrbitMovement();

            movement.Configure(orbit, profile.PrimaryOrbit, settings, new LivePose(DistanceAtBearing(orbit, 120f), 0f, 0f));

            Assert.IsTrue(movement.HasAngleLimits);
            Assert.AreEqual(45f, BearingOf(orbit, movement.OrbitDistance, Vector3.zero), 0.1f);

            movement.Configure(orbit, profile.PrimaryOrbit, settings, new LivePose(DistanceAtBearing(orbit, 0f), 0f, 0f));
            movement.SetHeldIntent(1f, 0f, 0f);
            for (int step = 0; step < 250; step++)
            {
                movement.UpdateOrbitMovement(0.02f);
            }

            Assert.AreEqual(45f, BearingOf(orbit, movement.OrbitDistance, Vector3.zero), 0.1f);
            Assert.AreEqual(0f, movement.TravelSpeed);

            movement.SetHeldIntent(-1f, 0f, 0f);
            for (int step = 0; step < 250; step++)
            {
                movement.UpdateOrbitMovement(0.02f);
            }

            Assert.AreEqual(-45f, BearingOf(orbit, movement.OrbitDistance, Vector3.zero), 0.1f);
        }

        [Test]
        public void ZoomAndHeight_ClampToOrbitRanges()
        {
            VehicleProfile profile = factory.CreateProfile("Main", 20f, 10f);
            profile.PrimaryOrbit.ConfigureOffsetRanges(-1f, 4f, -4f, 1f);
            OrbitMovement movement = CreateMovement(profile, CreateSettings(6f, 0.06f), 5f);

            movement.SetHeldIntent(0f, 1f, 1f);
            movement.UpdateOrbitMovement(10f);

            Assert.AreEqual(4f, movement.HeightOffset, 0.0001f);
            Assert.AreEqual(1f, movement.PlayerZoom, 0.0001f);

            movement.SetHeldIntent(0f, -1f, -1f);
            movement.UpdateOrbitMovement(10f);

            Assert.AreEqual(-1f, movement.HeightOffset, 0.0001f);
            Assert.AreEqual(-4f, movement.PlayerZoom, 0.0001f);

            movement.AddPinch(10f);
            movement.AddDrag(new Vector2(0f, 10f));

            Assert.AreEqual(1f, movement.PlayerZoom, 0.0001f);
            Assert.AreEqual(4f, movement.HeightOffset, 0.0001f);
        }

        [Test]
        public void Drag_FullWidth_MovesDragSensitivityMetres()
        {
            VehicleProfile profile = factory.CreateProfile("Main", 20f, 10f);
            profile.PrimaryOrbit.ConfigureOffsetRanges(-1f, 4f, -4f, 1f);
            ChainOrbit orbit = CreateOrbit(profile);
            OrbitMovement movement = new OrbitMovement();
            movement.Configure(orbit, profile.PrimaryOrbit, CreateSettings(6f, 0.06f), new LivePose(5f, 0f, 0f));

            movement.AddDrag(new Vector2(1f, 0.5f));

            Assert.AreEqual(20f, Mathf.Abs(movement.OrbitDistance - 5f), 0.0001f);
            Assert.AreEqual(2f, movement.HeightOffset, 0.0001f);

            Vector3 centre = new Vector3(10f, 0f, 5f);
            float bearingBefore = BearingOf(orbit, movement.OrbitDistance, centre);
            movement.AddDrag(new Vector2(0.01f, 0f));

            Assert.Greater(Mathf.DeltaAngle(bearingBefore, BearingOf(orbit, movement.OrbitDistance, centre)), 0f);
        }

        [TearDown]
        public void TearDown()
        {
            factory.Dispose();
        }

        private OrbitMovementSettings CreateSettings(float travelSpeed, float startHalfLife)
        {
            OrbitMovementSettings settings = new OrbitMovementSettings();
            settings.Configure(travelSpeed, startHalfLife, 3f, 2f, 20f, 4f, 5f, false, -180f, 180f, 0f);
            return settings;
        }

        private OrbitMovement CreateMovement(VehicleProfile profile, OrbitMovementSettings settings, float orbitDistance)
        {
            OrbitMovement movement = new OrbitMovement();
            movement.Configure(CreateOrbit(profile), profile.PrimaryOrbit, settings, new LivePose(orbitDistance, 0f, 0f));
            return movement;
        }

        private ChainOrbit CreateOrbit(VehicleProfile profile)
        {
            ChainLayout layout = new ChainLayout(new VehicleProfile[] { profile }, 0, profile.PrimaryOrbit);
            return new ChainOrbit(layout, new Transform[] { factory.CreateBody("Body") }, 0);
        }

        private float MeasureTimeToCircle(OrbitMovement movement, float orbitLength, float deltaTime)
        {
            float startDistance = movement.OrbitDistance;
            movement.SetHeldIntent(1f, 0f, 0f);
            for (int step = 1; step <= 10000; step++)
            {
                movement.UpdateOrbitMovement(deltaTime);
                if (Mathf.Abs(movement.OrbitDistance - startDistance) >= orbitLength)
                {
                    return step * deltaTime;
                }
            }

            return 0f;
        }

        private VehicleProfile CreateCircleProfile()
        {
            VehicleProfile profile = factory.CreateProfile("Main", 20f, 10f);
            profile.RemoveOrbit(profile.PrimaryOrbit.Id);
            VehicleOrbit orbit = profile.AddOrbit("Circle");
            Vector3[] anchors = { Vector3.right * CircleRadius, Vector3.back * CircleRadius, Vector3.left * CircleRadius, Vector3.forward * CircleRadius };
            List<BezierOrbitKnot> knots = new List<BezierOrbitKnot>();
            for (int index = 0; index < anchors.Length; index++)
            {
                Vector3 anchor = anchors[index];
                Vector3 tangent = new Vector3(anchor.z, 0f, -anchor.x) * CircleTangentScale;
                BezierOrbitKnot knot = new BezierOrbitKnot();
                knot.Configure(anchor, anchor - tangent, anchor + tangent);
                knots.Add(knot);
            }

            orbit.Configure(knots, Quaternion.identity);
            profile.AddWatchMarker(orbit, "Centre", 0f, Vector3.up, false);
            return profile;
        }

        private float DistanceAtBearing(ChainOrbit orbit, float bearing)
        {
            float distance;
            Assert.IsTrue(orbit.Bearing.TryGetDistanceAtBearing(bearing, 0f, out distance));
            return distance;
        }

        private float BearingOf(ChainOrbit orbit, float orbitDistance, Vector3 centre)
        {
            Vector3 position = orbit.EvaluateRootLocalPosition(orbitDistance);
            return Mathf.Atan2(position.x - centre.x, centre.z - position.z) * Mathf.Rad2Deg;
        }
    }
}
