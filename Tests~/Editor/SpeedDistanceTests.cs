using NUnit.Framework;

namespace Gley.CameraSystem.Tests.EditMode
{
    public class SpeedDistanceTests
    {
        private const float Tolerance = 0.0001f;

        [Test]
        public void BelowLowSpeed_Zero()
        {
            SpeedDistance speedDistance = new SpeedDistance();
            DrivingSettings settings = new DrivingSettings();

            Assert.AreEqual(0f, speedDistance.Offset(0f, 0f, settings, false), Tolerance);
            Assert.AreEqual(0f, speedDistance.Offset(4.9f, 0f, settings, false), Tolerance);
            Assert.AreEqual(0f, speedDistance.Offset(5f, 0f, settings, false), Tolerance);
        }

        [Test]
        public void AboveHighSpeed_FullBehind()
        {
            SpeedDistance speedDistance = new SpeedDistance();
            DrivingSettings settings = new DrivingSettings();

            Assert.AreEqual(3f, speedDistance.Offset(25f, 0f, settings, false), Tolerance);
            Assert.AreEqual(3f, speedDistance.Offset(60f, 0f, settings, false), Tolerance);
        }

        [Test]
        public void Midway_Smoothstep()
        {
            SpeedDistance speedDistance = new SpeedDistance();
            DrivingSettings settings = new DrivingSettings();

            Assert.AreEqual(1.5f, speedDistance.Offset(15f, 0f, settings, false), Tolerance);
            Assert.AreEqual(0.46875f, speedDistance.Offset(10f, 0f, settings, false), Tolerance);
            Assert.AreEqual(2.53125f, speedDistance.Offset(20f, 0f, settings, false), Tolerance);
        }

        [Test]
        public void Side_EqualsSideMinimum()
        {
            SpeedDistance speedDistance = new SpeedDistance();
            DrivingSettings settings = new DrivingSettings();

            Assert.AreEqual(0.75f, speedDistance.Offset(30f, 90f, settings, false), Tolerance);
            Assert.AreEqual(0.75f, speedDistance.Offset(30f, -90f, settings, false), Tolerance);
            Assert.AreEqual(0.375f, speedDistance.Offset(15f, 90f, settings, false), Tolerance);
        }

        [Test]
        public void Front_EqualsSideMinimum()
        {
            SpeedDistance speedDistance = new SpeedDistance();
            DrivingSettings settings = new DrivingSettings();

            Assert.AreEqual(0.75f, speedDistance.Offset(30f, 180f, settings, false), Tolerance);
            Assert.AreEqual(0.75f, speedDistance.Offset(30f, -180f, settings, false), Tolerance);
            Assert.AreEqual(0.75f, speedDistance.Offset(30f, 135f, settings, false), Tolerance);
            Assert.AreEqual(0.75f, speedDistance.Offset(30f, -120f, settings, false), Tolerance);
        }

        [Test]
        public void Reverse_DefaultOff_Zero()
        {
            SpeedDistance speedDistance = new SpeedDistance();
            DrivingSettings settings = new DrivingSettings();

            Assert.IsFalse(settings.SpeedDistanceInReverse);
            Assert.AreEqual(0f, speedDistance.Offset(30f, 0f, settings, true), Tolerance);

            settings.Configure(0.12f, 0.08f, 3f, true, 5f, 25f, 3f, 0.25f, true, true, 12f, 30f, 0.4f, 0.5f);

            Assert.AreEqual(3f, speedDistance.Offset(30f, 0f, settings, true), Tolerance);
        }
    }
}
