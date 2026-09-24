using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Gley.CameraSystem.Tests.EditMode
{
    public class OrbitDistanceSamplingEditModeTests
    {
        private readonly List<VehicleProfile> profiles = new List<VehicleProfile>();

        [TestCase(19.99f)]
        [TestCase(20f)]
        [TestCase(20.01f)]
        [TestCase(20.1f)]
        [TestCase(20.3125f)]
        [TestCase(20.32f)]
        public void RectangleRegressionUsesPhysicalDistance(float distance)
        {
            ClosedBezierOrbit orbit = new ClosedBezierOrbit(CreateRectangle());
            Assert.AreEqual(OrbitValidationResult.Valid, orbit.ValidationResult);
            Assert.AreEqual(60f, orbit.Length, 0.0001f);
            AssertPosition(RectanglePosition(distance), orbit.EvaluateBodyLocalPosition(distance), 0.0001f);
        }

        [Test]
        public void EveryRectangleKnotAndWrapPreservesForwardProgress()
        {
            ClosedBezierOrbit orbit = new ClosedBezierOrbit(CreateRectangle());
            float[] boundaries = { 0f, 20f, 30f, 50f, 60f };
            float[] offsets = { -0.01f, 0f, 0.01f, 0.1f, 0.3125f, 0.32f };
            for (int wrap = -3; wrap <= 3; wrap++)
            {
                foreach (float boundary in boundaries)
                {
                    foreach (float offset in offsets)
                    {
                        float distance = boundary + offset + wrap * 60f;
                        AssertPosition(RectanglePosition(distance), orbit.EvaluateBodyLocalPosition(distance), 0.0001f);
                    }
                }
            }
        }

        [Test]
        public void CurvedOrbitMatchesIndependentDenseReferenceIncludingEveryKnot()
        {
            VehicleOrbit definition = CreateRectangle();
            BezierOrbitKnot first = definition.Knots[0];
            BezierOrbitKnot second = definition.Knots[1];
            first.Configure(first.Anchor, first.IncomingControlPoint, new Vector3(2f, 0f, -8f));
            second.Configure(second.Anchor, new Vector3(17f, 0f, -5f), second.OutgoingControlPoint);
            ClosedBezierOrbit orbit = new ClosedBezierOrbit(definition);
            List<AssembledBezierOrbitSegment> segments = new List<AssembledBezierOrbitSegment>();
            for (int index = 0; index < definition.Knots.Count; index++)
            {
                BezierOrbitKnot start = definition.Knots[index];
                BezierOrbitKnot end = definition.Knots[(index + 1) % definition.Knots.Count];
                segments.Add(new AssembledBezierOrbitSegment(start.Anchor, start.OutgoingControlPoint, end.IncomingControlPoint, end.Anchor));
            }

            DenseReference reference = new DenseReference(segments);
            Assert.AreEqual(OrbitValidationResult.Valid, orbit.ValidationResult);
            Assert.AreEqual(reference.Length, orbit.Length, 0.01f);
            for (int index = 0; index <= 300; index++)
            {
                float distance = reference.Length * index / 300f;
                AssertPosition(reference.Position(distance), orbit.EvaluateBodyLocalPosition(distance), 0.015f);
            }

            foreach (float boundary in reference.Boundaries)
            {
                for (int wrap = -2; wrap <= 2; wrap++)
                {
                    float distance = boundary + wrap * orbit.Length;
                    Vector3 before = orbit.EvaluateBodyLocalPosition(distance - 0.02f);
                    Vector3 after = orbit.EvaluateBodyLocalPosition(distance + 0.02f);
                    AssertPosition(reference.Position(boundary), orbit.EvaluateBodyLocalPosition(distance), 0.015f);
                    AssertPosition(reference.Position(boundary - 0.02f), before, 0.015f);
                    AssertPosition(reference.Position(boundary + 0.02f), after, 0.015f);
                    Assert.Less(Vector3.Distance(before, after), 0.041f);
                    Assert.Greater(Vector3.Dot(after - before, reference.Position(boundary + 0.02f) - reference.Position(boundary - 0.02f)), 0f);
                }
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ComposedSamplerMatchesReferenceAtEveryRetainedAndConnectorBoundary(bool curved)
        {
            VehicleProfile front = CreateProfile(20f, 10f, 0.5f, 5f / 6f);
            VehicleProfile rear = CreateProfile(40f, 20f, 0.4f, 0.6f);
            rear.VehicleOrbit.FrontRemovableSection.Configure(0f, 1f / 3f);
            if (curved)
            {
                front.VehicleOrbit.RearRemovableSection.Configure(0.49f, 0.85f);
                BezierOrbitKnot first = front.VehicleOrbit.Knots[0];
                BezierOrbitKnot second = front.VehicleOrbit.Knots[1];
                first.Configure(first.Anchor, first.IncomingControlPoint, first.OutgoingControlPoint + Vector3.back);
                second.Configure(second.Anchor, second.IncomingControlPoint + Vector3.back, second.OutgoingControlPoint);
            }

            TwoBodyClosedBezierOrbit orbit = new TwoBodyClosedBezierOrbit(front, rear);
            Assert.AreEqual(TwoBodyClosedBezierOrbitAssemblyResult.Valid, orbit.AssemblyResult);
            DenseReference reference = new DenseReference(orbit.Segments);
            Assert.AreEqual(reference.Length, orbit.Length, 0.01f);
            float[] offsets = { -0.01f, 0f, 0.01f, 0.1f, 0.32f };
            foreach (float boundary in reference.Boundaries)
            {
                for (int wrap = -2; wrap <= 2; wrap++)
                {
                    foreach (float offset in offsets)
                    {
                        float distance = boundary + offset + wrap * orbit.Length;
                        AssertPosition(reference.Position(boundary + offset), orbit.EvaluateLeadBodyLocalPosition(distance), 0.015f);
                        Vector3 before = orbit.EvaluateLeadBodyLocalPosition(distance - 0.005f);
                        Vector3 after = orbit.EvaluateLeadBodyLocalPosition(distance + 0.005f);
                        Assert.Less(Vector3.Distance(before, after), 0.0105f);
                        Assert.Greater(Vector3.Dot(after - before, reference.Position(boundary + offset + 0.005f) - reference.Position(boundary + offset - 0.005f)), 0f);
                    }
                }
            }
        }

        [Test]
        public void OrientationAdjustmentRotatesTheEntireOrbitPlane()
        {
            VehicleOrbit definition = CreateRectangle();
            Quaternion adjustment = Quaternion.Euler(31f, 73f, -26f);
            definition.Configure(new List<BezierOrbitKnot>(definition.Knots), adjustment);
            ClosedBezierOrbit orbit = new ClosedBezierOrbit(definition);
            for (float distance = 0f; distance < 60f; distance += 0.37f)
            {
                Vector3 actual = orbit.EvaluateBodyLocalPosition(distance);
                AssertPosition(adjustment * RectanglePosition(distance), actual, 0.0001f);
                Assert.AreEqual(0f, Vector3.Dot(actual, adjustment * Vector3.up), 0.0001f);
            }
        }

        [Test]
        public void InvalidTopologyBlocksOrbitButLeavesFixedViewAvailable()
        {
            VehicleProfile profile = CreateProfile(20f, 10f, 0.5f, 5f / 6f);
            VehicleOrbit definition = profile.VehicleOrbit;
            List<BezierOrbitKnot> crossed = new List<BezierOrbitKnot>();
            Vector3[] anchors = { Vector3.zero, new Vector3(20f, 0f, 10f), new Vector3(20f, 0f, 0f), new Vector3(0f, 0f, 10f) };
            for (int index = 0; index < anchors.Length; index++)
            {
                crossed.Add(CreateKnot(anchors[index], anchors[(index + 3) % 4], anchors[(index + 1) % 4]));
            }
            definition.Configure(crossed, Quaternion.identity);
            Assert.AreEqual(OrbitValidationResult.SelfIntersecting, new ClosedBezierOrbit(definition).ValidationResult);
            GameObject rig = new GameObject("Topology test");
            CameraViewPreset preset = ScriptableObject.CreateInstance<CameraViewPreset>();
            try
            {
                CameraSystemController controller = rig.AddComponent<CameraSystemController>();
                controller.AssignCamera(rig.AddComponent<Camera>());
                controller.AssignVehicle(rig.transform, profile);
                preset.Configure(CameraViewType.ExteriorPresentation);
                controller.SelectViewPreset(preset);
                Assert.AreEqual(CameraSystemActivationResult.InvalidOrbit, controller.Activate());
                preset.Configure(CameraViewType.Fixed);
                profile.ConfigureFixedView(preset, Vector3.back, Vector3.forward);
                Assert.AreEqual(CameraSystemActivationResult.Succeeded, controller.Activate());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(rig);
                UnityEngine.Object.DestroyImmediate(preset);
            }
            definition.Configure(new List<BezierOrbitKnot>(), Quaternion.identity);
            Assert.AreEqual(OrbitValidationResult.TooFewKnots, new ClosedBezierOrbit(definition).ValidationResult);
            definition = CreateRectangle();
            definition.Knots[0].Configure(Vector3.up, Vector3.zero, Vector3.right);
            Assert.AreEqual(OrbitValidationResult.NonPlanar, new ClosedBezierOrbit(definition).ValidationResult);
        }

        private VehicleOrbit CreateRectangle(float width = 20f, float depth = 10f)
        {
            Vector3[] anchors = { Vector3.zero, new Vector3(width, 0f, 0f), new Vector3(width, 0f, depth), new Vector3(0f, 0f, depth) };
            List<BezierOrbitKnot> knots = new List<BezierOrbitKnot>();
            for (int index = 0; index < anchors.Length; index++)
            {
                knots.Add(CreateKnot(anchors[index], anchors[(index + 3) % 4], anchors[(index + 1) % 4]));
            }
            VehicleOrbit orbit = new VehicleOrbit();
            orbit.Configure(knots, Quaternion.identity);
            return orbit;
        }

        private BezierOrbitKnot CreateKnot(Vector3 anchor, Vector3 previous, Vector3 next)
        {
            BezierOrbitKnot knot = new BezierOrbitKnot();
            knot.Configure(anchor, Vector3.Lerp(anchor, previous, 1f / 3f), Vector3.Lerp(anchor, next, 1f / 3f));
            return knot;
        }

        private Vector3 RectanglePosition(float distance)
        {
            distance = Mathf.Repeat(distance, 60f);
            if (distance <= 20f)
            {
                return new Vector3(distance, 0f, 0f);
            }
            if (distance <= 30f)
            {
                return new Vector3(20f, 0f, distance - 20f);
            }
            if (distance <= 50f)
            {
                return new Vector3(50f - distance, 0f, 10f);
            }
            return new Vector3(0f, 0f, 60f - distance);
        }

        private void AssertPosition(Vector3 expected, Vector3 actual, float tolerance)
        {
            Assert.LessOrEqual(Vector3.Distance(expected, actual), tolerance, $"Expected {expected:F5}, got {actual:F5}");
        }

        private VehicleProfile CreateProfile(float width, float depth, float rearStart, float rearEnd)
        {
            VehicleOrbit orbit = CreateRectangle(width, depth);
            OrbitRemovableSection front = new OrbitRemovableSection();
            OrbitRemovableSection rear = new OrbitRemovableSection();
            front.Configure(0f, 0.2f);
            rear.Configure(rearStart, rearEnd);
            orbit.ConfigureRemovableSections(front, rear);
            VehicleConnectorAnchors anchors = new VehicleConnectorAnchors();
            anchors.Configure(new Vector3(width / 2f, 0f, 0f), new Vector3(width / 2f, 0f, depth));
            VehicleProfile profile = ScriptableObject.CreateInstance<VehicleProfile>();
            profile.ConfigureOrbit(orbit);
            profile.ConfigureConnectorAnchors(anchors);
            profiles.Add(profile);
            return profile;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (VehicleProfile profile in profiles)
            {
                UnityEngine.Object.DestroyImmediate(profile);
            }
            profiles.Clear();
        }

        private class DenseReference
        {
            private readonly List<Vector3> positions = new List<Vector3>();
            private readonly List<double> distances = new List<double>();

            public List<float> Boundaries { get; } = new List<float>();
            public float Length => (float)distances[distances.Count - 1];

            public DenseReference(IReadOnlyList<AssembledBezierOrbitSegment> segments)
            {
                positions.Add(segments[0].StartPosition);
                distances.Add(0d);
                double length = 0d;
                foreach (AssembledBezierOrbitSegment segment in segments)
                {
                    Boundaries.Add((float)length);
                    for (int sample = 1; sample <= 4096; sample++)
                    {
                        float t = sample / 4096f;
                        Vector3 first = Vector3.Lerp(segment.StartPosition, segment.StartControlPoint, t);
                        Vector3 middle = Vector3.Lerp(segment.StartControlPoint, segment.EndControlPoint, t);
                        Vector3 last = Vector3.Lerp(segment.EndControlPoint, segment.EndPosition, t);
                        Vector3 point = Vector3.Lerp(Vector3.Lerp(first, middle, t), Vector3.Lerp(middle, last, t), t);
                        Vector3 difference = point - positions[positions.Count - 1];
                        length += Math.Sqrt((double)difference.x * difference.x + (double)difference.y * difference.y + (double)difference.z * difference.z);
                        positions.Add(point);
                        distances.Add(length);
                    }
                }
                Boundaries.Add((float)length);
            }

            public Vector3 Position(float distance)
            {
                double wrapped = Mathf.Repeat(distance, Length);
                int index = distances.BinarySearch(wrapped);
                if (index >= 0)
                {
                    return positions[index];
                }
                index = ~index;
                if (index >= positions.Count)
                {
                    return positions[0];
                }
                float fraction = (float)((wrapped - distances[index - 1]) / (distances[index] - distances[index - 1]));
                return Vector3.Lerp(positions[index - 1], positions[index], fraction);
            }
        }
    }
}
