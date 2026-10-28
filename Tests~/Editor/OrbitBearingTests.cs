using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Gley.CameraSystem.Tests.EditMode
{
    public class OrbitBearingTests
    {
        [Test]
        public void Centre_RectangleOffsetFromOrigin_IsBoundsCentre()
        {
            Vector3[] corners =
            {
                new Vector3(8f, 2f, -9f),
                new Vector3(12f, 2f, -9f),
                new Vector3(12f, 2f, 1f),
                new Vector3(8f, 2f, 1f)
            };
            OrbitBearing bearing = CreatePolygonBearing(corners);

            Assert.AreEqual(new Vector3(10f, 0f, -4f), bearing.Centre);
        }

        [Test]
        public void BearingOfLocalPoint_BehindRightFrontLeft_0_90_180_Minus90()
        {
            OrbitBearing bearing = CreateRectangleBearing();

            Assert.AreEqual(0f, bearing.BearingOfLocalPoint(new Vector3(0f, 0f, -5f)), 0.0001f);
            Assert.AreEqual(90f, bearing.BearingOfLocalPoint(new Vector3(2f, 0f, 0f)), 0.0001f);
            Assert.AreEqual(180f, bearing.BearingOfLocalPoint(new Vector3(0f, 0f, 5f)), 0.0001f);
            Assert.AreEqual(-90f, bearing.BearingOfLocalPoint(new Vector3(-2f, 0f, 0f)), 0.0001f);
        }

        [Test]
        public void TryGetDistanceAtBearing_Rectangle_ReturnsAnalyticPoint()
        {
            OrbitBearing bearing = CreateRectangleBearing();

            Assert.IsTrue(bearing.TryGetDistanceAtBearing(0f, 0f, out float rearDistance));
            Assert.IsTrue(bearing.TryGetDistanceAtBearing(90f, 0f, out float rightDistance));
            Assert.AreEqual(2f, rearDistance, 0.0001f);
            Assert.AreEqual(9f, rightDistance, 0.0001f);
        }

        [Test]
        public void RoundTrip_DistanceToBearingToDistance_WithinOneCentimetre()
        {
            ClosedBezierOrbit orbit = CreateRoundedRectangleOrbit();

            Assert.AreEqual(OrbitValidationResult.Valid, orbit.ValidationResult);
            for (float sourceDistance = 0f; sourceDistance < orbit.Length; sourceDistance += 1f)
            {
                Vector3 point = orbit.EvaluateBodyLocalPosition(sourceDistance);
                float angle = orbit.Bearing.BearingOfLocalPoint(point);
                Assert.IsTrue(orbit.Bearing.TryGetDistanceAtBearing(angle, sourceDistance, out float recoveredDistance));
                float difference = Mathf.Abs(recoveredDistance - sourceDistance);
                difference = Mathf.Min(difference, orbit.Length - difference);
                Assert.LessOrEqual(difference, 0.01f);
            }
        }

        [Test]
        public void LShape_AmbiguousBearing_UsesCrossingNearestToNearDistance()
        {
            OrbitBearing bearing = CreateLShapeBearing();

            Assert.IsTrue(bearing.TryGetDistanceAtBearing(0f, 3f, out float outerDistance));
            Assert.IsTrue(bearing.TryGetDistanceAtBearing(0f, 15f, out float innerDistance));
            Assert.IsTrue(bearing.TryGetDistanceAtBearing(0f, 31f, out float wrappedNearestDistance));
            Assert.AreEqual(4f, outerDistance, 0.0001f);
            Assert.AreEqual(14f, innerDistance, 0.0001f);
            Assert.AreEqual(4f, wrappedNearestDistance, 0.0001f);
            Assert.IsFalse(bearing.TryGetDistanceAtBearing(90f, 0f, out _));
        }

        [Test]
        public void CountCrossings_LShape_ReturnsTwoForKnownBearing()
        {
            OrbitBearing bearing = CreateLShapeBearing();

            Assert.AreEqual(2, bearing.CountCrossings(0f));
        }

        [Test]
        public void WrapBearing_Minus190_Is170()
        {
            OrbitBearing bearing = new OrbitBearing();

            Assert.AreEqual(170f, bearing.WrapBearing(-190f), 0.0001f);
        }

        private OrbitBearing CreatePolygonBearing(IReadOnlyList<Vector3> corners)
        {
            List<Vector3> positions = new List<Vector3>();
            List<float> distances = new List<float>();
            float length = 0f;

            for (int index = 0; index < corners.Count; index++)
            {
                positions.Add(corners[index]);
                distances.Add(length);
                length += Vector3.Distance(corners[index], corners[(index + 1) % corners.Count]);
            }

            positions.Add(corners[0]);
            distances.Add(length);
            OrbitBearing bearing = new OrbitBearing();
            bearing.Rebuild(positions, distances, length);
            return bearing;
        }

        private OrbitBearing CreateRectangleBearing()
        {
            Vector3[] corners =
            {
                new Vector3(-2f, 0f, -5f),
                new Vector3(2f, 0f, -5f),
                new Vector3(2f, 0f, 5f),
                new Vector3(-2f, 0f, 5f)
            };
            return CreatePolygonBearing(corners);
        }

        private ClosedBezierOrbit CreateRoundedRectangleOrbit()
        {
            const float cornerControl = 0.55228475f;
            List<BezierOrbitKnot> knots = new List<BezierOrbitKnot>
            {
                CreateKnot(new Vector3(-3f, 0f, -6f), new Vector3(-3f - cornerControl, 0f, -6f), new Vector3(-1f, 0f, -6f)),
                CreateKnot(new Vector3(3f, 0f, -6f), new Vector3(1f, 0f, -6f), new Vector3(3f + cornerControl, 0f, -6f)),
                CreateKnot(new Vector3(4f, 0f, -5f), new Vector3(4f, 0f, -5f - cornerControl), new Vector3(4f, 0f, -5f + 10f / 3f)),
                CreateKnot(new Vector3(4f, 0f, 5f), new Vector3(4f, 0f, 5f - 10f / 3f), new Vector3(4f, 0f, 5f + cornerControl)),
                CreateKnot(new Vector3(3f, 0f, 6f), new Vector3(3f + cornerControl, 0f, 6f), new Vector3(1f, 0f, 6f)),
                CreateKnot(new Vector3(-3f, 0f, 6f), new Vector3(-1f, 0f, 6f), new Vector3(-3f - cornerControl, 0f, 6f)),
                CreateKnot(new Vector3(-4f, 0f, 5f), new Vector3(-4f, 0f, 5f + cornerControl), new Vector3(-4f, 0f, 5f - 10f / 3f)),
                CreateKnot(new Vector3(-4f, 0f, -5f), new Vector3(-4f, 0f, -5f + 10f / 3f), new Vector3(-4f, 0f, -5f - cornerControl))
            };
            VehicleOrbit definition = new VehicleOrbit();
            definition.Configure(knots, Quaternion.identity);
            return new ClosedBezierOrbit(definition);
        }

        private OrbitBearing CreateLShapeBearing()
        {
            Vector3[] corners =
            {
                new Vector3(-4f, 0f, -4f),
                new Vector3(4f, 0f, -4f),
                new Vector3(4f, 0f, -2f),
                new Vector3(-2f, 0f, -2f),
                new Vector3(-2f, 0f, 4f),
                new Vector3(-4f, 0f, 4f)
            };
            return CreatePolygonBearing(corners);
        }

        private BezierOrbitKnot CreateKnot(Vector3 anchor, Vector3 incoming, Vector3 outgoing)
        {
            BezierOrbitKnot knot = new BezierOrbitKnot();
            knot.Configure(anchor, incoming, outgoing);
            return knot;
        }
    }
}
