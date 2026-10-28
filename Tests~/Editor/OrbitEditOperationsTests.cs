using System.Collections.Generic;
using Gley.CameraSystem.Editor;
using NUnit.Framework;
using UnityEngine;

namespace Gley.CameraSystem.Tests.EditMode
{
    public class OrbitEditOperationsTests
    {
        private const float Tolerance = 0.0001f;
        private const float ProjectionTolerance = 0.001f;
        private const float CircleHandleFactor = 0.5522848f;
        private const float HalfSize = 2f;

        private readonly List<Object> createdObjects = new List<Object>();

        private OrbitEditOperations operations;

        [SetUp]
        public void SetUp()
        {
            operations = new OrbitEditOperations();
        }

        [TearDown]
        public void TearDown()
        {
            for (int index = 0; index < createdObjects.Count; index++)
            {
                if (createdObjects[index] != null)
                {
                    Object.DestroyImmediate(createdObjects[index]);
                }
            }

            createdObjects.Clear();
        }

        [Test]
        public void InsertKnotAtCurveParameter_ShapeUnchangedAtInsertion()
        {
            VehicleOrbit orbit = CreateCircleProfile().PrimaryOrbit;
            Vector3 p0 = orbit.Knots[1].Anchor;
            Vector3 p1 = orbit.Knots[1].OutgoingControlPoint;
            Vector3 p2 = orbit.Knots[2].IncomingControlPoint;
            Vector3 p3 = orbit.Knots[2].Anchor;
            const float SplitT = 0.3f;

            int insertedIndex = operations.InsertKnot(orbit, 1, SplitT);

            Assert.AreEqual(2, insertedIndex);
            Assert.AreEqual(5, orbit.Knots.Count);
            AssertVector(EvaluateCubic(p0, p1, p2, p3, SplitT), orbit.Knots[2].Anchor);
            AssertVector(p0, orbit.Knots[1].Anchor);
            AssertVector(p3, orbit.Knots[3].Anchor);

            for (int sampleIndex = 0; sampleIndex <= 10; sampleIndex++)
            {
                float u = sampleIndex / 10f;
                Vector3 firstHalf = EvaluateCubic(orbit.Knots[1].Anchor, orbit.Knots[1].OutgoingControlPoint, orbit.Knots[2].IncomingControlPoint, orbit.Knots[2].Anchor, u);
                Vector3 secondHalf = EvaluateCubic(orbit.Knots[2].Anchor, orbit.Knots[2].OutgoingControlPoint, orbit.Knots[3].IncomingControlPoint, orbit.Knots[3].Anchor, u);
                AssertVector(EvaluateCubic(p0, p1, p2, p3, SplitT * u), firstHalf);
                AssertVector(EvaluateCubic(p0, p1, p2, p3, SplitT + (1f - SplitT) * u), secondHalf);
            }

            ClosedBezierOrbit builtOrbit = new ClosedBezierOrbit(orbit);
            Assert.AreEqual(OrbitValidationResult.Valid, builtOrbit.ValidationResult);
        }

        [Test]
        public void DeleteKnot_CurveStaysClosed()
        {
            VehicleOrbit orbit = CreateSquareProfile().PrimaryOrbit;
            operations.InsertKnot(orbit, 0, 0.5f);
            BezierOrbitKnot first = orbit.Knots[0];
            BezierOrbitKnot third = orbit.Knots[2];
            BezierOrbitKnot fourth = orbit.Knots[3];
            BezierOrbitKnot fifth = orbit.Knots[4];

            bool deleted = operations.DeleteKnot(orbit, 1);

            Assert.IsTrue(deleted);
            Assert.AreEqual(4, orbit.Knots.Count);
            Assert.AreSame(first, orbit.Knots[0]);
            Assert.AreSame(third, orbit.Knots[1]);
            Assert.AreSame(fourth, orbit.Knots[2]);
            Assert.AreSame(fifth, orbit.Knots[3]);

            ClosedBezierOrbit builtOrbit = new ClosedBezierOrbit(orbit);
            Assert.IsTrue(builtOrbit.IsClosed);
            Assert.AreEqual(8f * HalfSize, builtOrbit.Length, ProjectionTolerance);
            AssertVector(builtOrbit.EvaluateBodyLocalPosition(0f), builtOrbit.EvaluateBodyLocalPosition(builtOrbit.Length));

            VehicleOrbit triangle = CreateSquareProfile().PrimaryOrbit;
            Assert.IsTrue(operations.DeleteKnot(triangle, 3));
            Assert.IsFalse(operations.DeleteKnot(triangle, 0));
            Assert.AreEqual(3, triangle.Knots.Count);
        }

        [Test]
        public void MoveMarkerAlongCurve_UpdatesNormalizedPosition()
        {
            VehicleProfile profile = CreateSquareProfile();
            VehicleOrbit orbit = profile.PrimaryOrbit;
            Vector3 watchPoint = new Vector3(0.5f, 1.2f, -0.3f);
            OrbitWatchMarker marker = profile.AddWatchMarker(orbit, "Marker", 0.1f, watchPoint, true);
            ClosedBezierOrbit builtOrbit = new ClosedBezierOrbit(orbit);

            bool moved = operations.MoveMarkerAlongCurve(marker, builtOrbit, new Vector3(-1f, 0.7f, -2.4f));

            Assert.IsTrue(moved);
            Assert.AreEqual(3f / 16f, marker.NormalizedOrbitPosition, ProjectionTolerance);
            AssertVector(watchPoint, marker.WatchPointLocalPosition);
            Assert.IsTrue(marker.IsPointOfInterest);

            operations.MoveMarkerAlongCurve(marker, builtOrbit, new Vector3(2.3f, 0f, 1f));

            Assert.AreEqual(13f / 16f, marker.NormalizedOrbitPosition, ProjectionTolerance);
        }

        [Test]
        public void MirroredTangentEdit_OppositeTangentMirrors()
        {
            VehicleOrbit orbit = CreateSquareProfile().PrimaryOrbit;
            BezierOrbitKnot knot = orbit.Knots[1];
            Vector3 anchor = new Vector3(-HalfSize, 0f, -HalfSize);
            Vector3 originalIncoming = knot.IncomingControlPoint;
            float incomingLength = 4f * HalfSize / 6f;
            Vector3 newOutgoing = new Vector3(-3f, 0.4f, -1f);

            operations.MoveControlPoint(orbit, 1, true, newOutgoing, true);

            Vector3 expectedOutgoing = new Vector3(-3f, 0f, -1f);
            Vector3 expectedIncoming = anchor + new Vector3(1f, 0f, -1f).normalized * incomingLength;
            AssertVector(anchor, knot.Anchor);
            AssertVector(expectedOutgoing, knot.OutgoingControlPoint);
            AssertVector(expectedIncoming, knot.IncomingControlPoint);
            Assert.AreEqual(incomingLength, (originalIncoming - anchor).magnitude, Tolerance);

            Vector3 brokenIncoming = new Vector3(-1f, 0f, -3f);
            operations.MoveControlPoint(orbit, 1, false, brokenIncoming, false);

            AssertVector(brokenIncoming, knot.IncomingControlPoint);
            AssertVector(expectedOutgoing, knot.OutgoingControlPoint);
        }

        [Test]
        public void ReorderMarkers_ChangesNextPointOrderOnly()
        {
            VehicleProfile profile = CreateSquareProfile();
            VehicleOrbit orbit = profile.PrimaryOrbit;
            OrbitWatchMarker first = profile.AddWatchMarker(orbit, "wheel", 0.1f, new Vector3(1f, 0.5f, 0f), true);
            OrbitWatchMarker second = profile.AddWatchMarker(orbit, "plate", 0.4f, new Vector3(0f, 0.5f, -1f), true);
            OrbitWatchMarker third = profile.AddWatchMarker(orbit, "engine", 0.7f, new Vector3(0f, 1f, 1f), true);
            ClosedBezierOrbit before = new ClosedBezierOrbit(orbit);
            Vector3 watchBefore = before.EvaluateBodyLocalWatchPoint(before.Length * 0.25f);
            Transform body = CreateBody();
            ChainOrbit chainBefore = CreateChainOrbit(profile, body);
            float thirdProgressBefore = FindMergedMarker(chainBefore, third.Id).NormalizedProgress;
            Assert.AreEqual(2, FindMergedMarker(chainBefore, third.Id).AuthoredIndex);

            bool reordered = operations.ReorderMarkers(orbit, 2, 0);

            Assert.IsTrue(reordered);
            Assert.AreSame(third, orbit.WatchMarkers[0]);
            Assert.AreSame(first, orbit.WatchMarkers[1]);
            Assert.AreSame(second, orbit.WatchMarkers[2]);
            Assert.AreEqual(0.1f, first.NormalizedOrbitPosition, Tolerance);
            Assert.AreEqual(0.4f, second.NormalizedOrbitPosition, Tolerance);
            Assert.AreEqual(0.7f, third.NormalizedOrbitPosition, Tolerance);
            AssertVector(new Vector3(0f, 1f, 1f), third.WatchPointLocalPosition);

            ClosedBezierOrbit after = new ClosedBezierOrbit(orbit);
            AssertVector(watchBefore, after.EvaluateBodyLocalWatchPoint(after.Length * 0.25f));

            ChainOrbit chainOrbit = CreateChainOrbit(profile, body);
            Assert.IsTrue(chainOrbit.IsClosed);
            Assert.AreEqual(0, FindMergedMarker(chainOrbit, third.Id).AuthoredIndex);
            Assert.AreEqual(1, FindMergedMarker(chainOrbit, first.Id).AuthoredIndex);
            Assert.AreEqual(2, FindMergedMarker(chainOrbit, second.Id).AuthoredIndex);
            Assert.AreEqual(thirdProgressBefore, FindMergedMarker(chainOrbit, third.Id).NormalizedProgress, Tolerance);
        }

        private VehicleProfile CreateSquareProfile()
        {
            Vector3[] anchors =
            {
                new Vector3(HalfSize, 0f, -HalfSize),
                new Vector3(-HalfSize, 0f, -HalfSize),
                new Vector3(-HalfSize, 0f, HalfSize),
                new Vector3(HalfSize, 0f, HalfSize)
            };
            List<BezierOrbitKnot> knots = new List<BezierOrbitKnot>();

            for (int index = 0; index < anchors.Length; index++)
            {
                Vector3 anchor = anchors[index];
                Vector3 previous = anchors[(index + anchors.Length - 1) % anchors.Length];
                Vector3 next = anchors[(index + 1) % anchors.Length];
                BezierOrbitKnot knot = new BezierOrbitKnot();
                knot.Configure(anchor, anchor + (previous - anchor) / 3f, anchor + (next - anchor) / 3f);
                knots.Add(knot);
            }

            return CreateProfile(knots);
        }

        private VehicleProfile CreateCircleProfile()
        {
            Vector3[] anchors =
            {
                new Vector3(0f, 0f, -HalfSize),
                new Vector3(-HalfSize, 0f, 0f),
                new Vector3(0f, 0f, HalfSize),
                new Vector3(HalfSize, 0f, 0f)
            };
            Vector3[] tangents =
            {
                Vector3.left,
                Vector3.forward,
                Vector3.right,
                Vector3.back
            };
            List<BezierOrbitKnot> knots = new List<BezierOrbitKnot>();

            for (int index = 0; index < anchors.Length; index++)
            {
                Vector3 handle = tangents[index] * (HalfSize * CircleHandleFactor);
                BezierOrbitKnot knot = new BezierOrbitKnot();
                knot.Configure(anchors[index], anchors[index] - handle, anchors[index] + handle);
                knots.Add(knot);
            }

            return CreateProfile(knots);
        }

        private VehicleProfile CreateProfile(List<BezierOrbitKnot> knots)
        {
            VehicleProfile profile = ScriptableObject.CreateInstance<VehicleProfile>();
            createdObjects.Add(profile);
            VehicleOrbit orbit = profile.AddOrbit("Main");
            orbit.Configure(knots, Quaternion.identity);
            return profile;
        }

        private Transform CreateBody()
        {
            GameObject body = new GameObject("Orbit Edit Body");
            createdObjects.Add(body);
            return body.transform;
        }

        private ChainOrbit CreateChainOrbit(VehicleProfile profile, Transform body)
        {
            ChainLayout layout = new ChainLayout(new List<VehicleProfile> { profile }, 0, profile.PrimaryOrbit);
            return new ChainOrbit(layout, new List<Transform> { body }, 0);
        }

        private Vector3 EvaluateCubic(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float inverse = 1f - t;
            return inverse * inverse * inverse * p0 + 3f * inverse * inverse * t * p1 + 3f * inverse * t * t * p2 + t * t * t * p3;
        }

        private MergedMarker FindMergedMarker(ChainOrbit chainOrbit, int markerId)
        {
            for (int index = 0; index < chainOrbit.MergedMarkers.Count; index++)
            {
                if (chainOrbit.MergedMarkers[index].MarkerId == markerId)
                {
                    return chainOrbit.MergedMarkers[index];
                }
            }

            Assert.Fail($"Merged marker {markerId} not found.");
            return default;
        }

        private void AssertVector(Vector3 expected, Vector3 actual)
        {
            Assert.AreEqual(expected.x, actual.x, Tolerance);
            Assert.AreEqual(expected.y, actual.y, Tolerance);
            Assert.AreEqual(expected.z, actual.z, Tolerance);
        }
    }
}
