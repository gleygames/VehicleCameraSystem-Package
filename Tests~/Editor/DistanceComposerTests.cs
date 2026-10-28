using NUnit.Framework;

namespace Gley.CameraSystem.Tests.EditMode
{
    public class DistanceComposerTests
    {
        [Test]
        public void SpeedOffsetAtMaxZoomOut_NoEffect()
        {
            VehicleOrbit ranges = CreateRanges();
            DistanceComposer composer = new DistanceComposer();

            Assert.AreEqual(-4f, composer.ComposeZoom(-4f, 0f, ranges), 0.0001f);
            Assert.AreEqual(-4f, composer.ComposeZoom(-4f, 3f, ranges), 0.0001f);
        }

        [Test]
        public void SumOutsideRange_Clamped()
        {
            VehicleOrbit ranges = CreateRanges();
            DistanceComposer composer = new DistanceComposer();

            Assert.AreEqual(-0.5f, composer.ComposeZoom(0.5f, 1f, ranges), 0.0001f);
            Assert.AreEqual(-4f, composer.ComposeZoom(-2f, 3f, ranges), 0.0001f);
            Assert.AreEqual(1f, composer.ComposeZoom(3f, 0f, ranges), 0.0001f);
        }

        private VehicleOrbit CreateRanges()
        {
            VehicleOrbit ranges = new VehicleOrbit();
            ranges.ConfigureOffsetRanges(-1f, 4f, -4f, 1f);
            return ranges;
        }
    }
}
