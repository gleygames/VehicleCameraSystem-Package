using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Gley.CameraSystem.Tests.EditMode
{
    public class InwardZoomAnalyzerTests
    {
        [Test]
        public void MaximumSafeInwardZoom_Circle_EqualsRadius()
        {
            const float radius = 5f;
            const float control = radius * 0.55228475f;
            List<BezierOrbitKnot> knots = new List<BezierOrbitKnot>
            {
                CreateKnot(new Vector3(radius, 0f, 0f), new Vector3(radius, 0f, -control), new Vector3(radius, 0f, control)),
                CreateKnot(new Vector3(0f, 0f, radius), new Vector3(control, 0f, radius), new Vector3(-control, 0f, radius)),
                CreateKnot(new Vector3(-radius, 0f, 0f), new Vector3(-radius, 0f, control), new Vector3(-radius, 0f, -control)),
                CreateKnot(new Vector3(0f, 0f, -radius), new Vector3(-control, 0f, -radius), new Vector3(control, 0f, -radius))
            };
            ClosedBezierOrbit orbit = CreateOrbit(knots, 2f);

            Assert.AreEqual(OrbitValidationResult.Valid, orbit.ValidationResult);
            Assert.AreEqual(radius, new InwardZoomAnalyzer().MaximumSafeInwardZoom(orbit), radius * 0.02f);
            Assert.AreEqual(radius, orbit.MaximumSafeInwardZoom, radius * 0.02f);
        }

        [Test]
        public void MaximumSafeInwardZoom_RoundedRectangle_EqualsCornerRadius()
        {
            ClosedBezierOrbit orbit = CreateRoundedRectangleOrbit(0.5f);

            Assert.AreEqual(OrbitValidationResult.Valid, orbit.ValidationResult);
            Assert.AreEqual(1f, new InwardZoomAnalyzer().MaximumSafeInwardZoom(orbit), 0.02f);
            Assert.AreEqual(OrbitOffsetRangeValidationResult.Valid, orbit.OffsetRangeValidationResult);
        }

        [Test]
        public void Validation_InwardRangeLargerThanCornerRadius_Warns()
        {
            ClosedBezierOrbit orbit = CreateRoundedRectangleOrbit(2f);

            Assert.AreEqual(OrbitValidationResult.Valid, orbit.ValidationResult);
            Assert.AreEqual(1f, orbit.MaximumSafeInwardZoom, 0.02f);
            Assert.AreEqual(OrbitOffsetRangeValidationResult.InwardZoomExceedsCurvature, orbit.OffsetRangeValidationResult);
        }

        private ClosedBezierOrbit CreateRoundedRectangleOrbit(float maximumZoom)
        {
            const float control = 0.55228475f;
            List<BezierOrbitKnot> knots = new List<BezierOrbitKnot>
            {
                CreateKnot(new Vector3(-3f, 0f, -6f), new Vector3(-3f - control, 0f, -6f), new Vector3(-1f, 0f, -6f)),
                CreateKnot(new Vector3(3f, 0f, -6f), new Vector3(1f, 0f, -6f), new Vector3(3f + control, 0f, -6f)),
                CreateKnot(new Vector3(4f, 0f, -5f), new Vector3(4f, 0f, -5f - control), new Vector3(4f, 0f, -5f + 10f / 3f)),
                CreateKnot(new Vector3(4f, 0f, 5f), new Vector3(4f, 0f, 5f - 10f / 3f), new Vector3(4f, 0f, 5f + control)),
                CreateKnot(new Vector3(3f, 0f, 6f), new Vector3(3f + control, 0f, 6f), new Vector3(1f, 0f, 6f)),
                CreateKnot(new Vector3(-3f, 0f, 6f), new Vector3(-1f, 0f, 6f), new Vector3(-3f - control, 0f, 6f)),
                CreateKnot(new Vector3(-4f, 0f, 5f), new Vector3(-4f, 0f, 5f + control), new Vector3(-4f, 0f, 5f - 10f / 3f)),
                CreateKnot(new Vector3(-4f, 0f, -5f), new Vector3(-4f, 0f, -5f + 10f / 3f), new Vector3(-4f, 0f, -5f - control))
            };
            return CreateOrbit(knots, maximumZoom);
        }

        private ClosedBezierOrbit CreateOrbit(List<BezierOrbitKnot> knots, float maximumZoom)
        {
            VehicleOrbit definition = new VehicleOrbit();
            definition.Configure(knots, Quaternion.identity);
            definition.ConfigureOffsetRanges(-1f, 1f, -4f, maximumZoom);
            return new ClosedBezierOrbit(definition);
        }

        private BezierOrbitKnot CreateKnot(Vector3 anchor, Vector3 incoming, Vector3 outgoing)
        {
            BezierOrbitKnot knot = new BezierOrbitKnot();
            knot.Configure(anchor, incoming, outgoing);
            return knot;
        }
    }
}
