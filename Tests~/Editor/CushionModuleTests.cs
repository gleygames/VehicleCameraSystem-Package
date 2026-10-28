using NUnit.Framework;
using UnityEngine;

namespace Gley.CameraSystem.Tests.EditMode
{
    public class CushionModuleTests
    {
        private const float FrameTime = 0.02f;
        private const float Tolerance = 0.0001f;

        [Test]
        public void ShortBump_ProducesVerticalOffset()
        {
            CushionModule module = new CushionModule();
            module.Configure(true, 1.5f, 0.5f, 0.05f);
            SeatMotionSignals bump = new SeatMotionSignals(Vector3.up * 2f, 20f, FrameTime);

            Vector3 offset = module.UpdateSeatModule(bump, FrameTime);

            float alpha = 1f / (1f + 2f * Mathf.PI * 1.5f * FrameTime);
            float response = 1f - Mathf.Pow(0.5f, FrameTime / 0.05f);
            Assert.AreEqual(-alpha * response, offset.y, Tolerance);
            Assert.AreEqual(0f, offset.x, Tolerance);
            Assert.AreEqual(0f, offset.z, Tolerance);
        }

        [Test]
        public void SlowHillClimb_ConstantSlope_NearZero()
        {
            CushionModule hill = new CushionModule();
            hill.Configure(true, 1.5f, 0.5f, 0.05f);
            float verticalSpeed = 20f * 0.05f;
            float approachTime = 30f;
            float approachAcceleration = verticalSpeed / approachTime;
            SeatMotionSignals approach = new SeatMotionSignals(Vector3.up * approachAcceleration, 20f, FrameTime);
            float hillPeak = 0f;
            for (int frame = 0; frame < 1500; frame++)
            {
                Vector3 offset = hill.UpdateSeatModule(approach, FrameTime);
                hillPeak = Mathf.Max(hillPeak, Mathf.Abs(offset.y));
            }

            SeatMotionSignals constantSlope = new SeatMotionSignals(Vector3.zero, 20f, FrameTime);
            Vector3 slopeOffset = Vector3.zero;
            for (int frame = 0; frame < 100; frame++)
            {
                slopeOffset = hill.UpdateSeatModule(constantSlope, FrameTime);
            }

            CushionModule bump = new CushionModule();
            bump.Configure(true, 1.5f, 0.5f, 0.05f);
            float bumpResponse = Mathf.Abs(bump.UpdateSeatModule(new SeatMotionSignals(Vector3.up * 2f, 20f, FrameTime), FrameTime).y);
            Assert.Less(hillPeak, bumpResponse * 0.05f);
            Assert.Less(Mathf.Abs(slopeOffset.y), bumpResponse * 0.05f);
        }

        [Test]
        public void IntensityZero_NoMotion()
        {
            CushionModule module = new CushionModule();
            module.Configure(true, 1.5f, 0.5f, 0.05f);
            SeatMotionSignals bump = new SeatMotionSignals(Vector3.up * 2f, 0f, FrameTime);

            Assert.Less(module.UpdateSeatModule(bump, FrameTime).y, 0f);
            module.SetIntensity(0f);
            Assert.AreEqual(Vector3.zero, module.UpdateSeatModule(bump, FrameTime));
            Assert.AreEqual(Vector3.zero, module.UpdateSeatModule(bump, FrameTime));
        }

        [Test]
        public void IntensityTwo_DoublesResponse()
        {
            CushionModule normal = new CushionModule();
            normal.Configure(true, 1.5f, 0.5f, 0.05f);
            CushionModule intense = new CushionModule();
            intense.Configure(true, 1.5f, 0.5f, 0.05f);
            intense.SetIntensity(2f);
            SeatMotionSignals bump = new SeatMotionSignals(Vector3.up * 0.1f, 0f, FrameTime);

            Vector3 normalOffset = normal.UpdateSeatModule(bump, FrameTime);
            Vector3 intenseOffset = intense.UpdateSeatModule(bump, FrameTime);

            Assert.AreEqual(normalOffset.y * 2f, intenseOffset.y, Tolerance);
        }

        [Test]
        public void VariableFrameRate_StableNoNaN()
        {
            CushionModule module = new CushionModule();
            module.Configure(true, 1.5f, 0.5f, 0.05f);
            float[] frameTimes = { 0.02f, 0f, 0.005f, 0.033f, 0.25f, 1f, 10f };
            float previousOffset = 0f;
            for (int frame = 0; frame < 140; frame++)
            {
                float deltaTime = frameTimes[frame % frameTimes.Length];
                float acceleration = 2f;
                if (frame % 2 != 0)
                {
                    acceleration = -2f;
                }
                SeatMotionSignals signals = new SeatMotionSignals(Vector3.up * acceleration, 0f, deltaTime);
                Vector3 offset = module.UpdateSeatModule(signals, deltaTime);
                Assert.IsFalse(float.IsNaN(offset.y));
                Assert.IsFalse(float.IsInfinity(offset.y));
                Assert.LessOrEqual(Mathf.Abs(offset.y), 1f);
                if (deltaTime == 0f)
                {
                    Assert.AreEqual(previousOffset, offset.y, Tolerance);
                }

                previousOffset = offset.y;
            }
        }

        [Test]
        public void Disabled_OtherModulesUnaffected()
        {
            SeatSettings seat = new SeatSettings();
            seat.Configure(Vector3.zero, new Vector3(0.06f, 0.04f, 0.08f));
            InteriorSettings settings = new InteriorSettings();
            SeatMotion motion = new SeatMotion();
            motion.Configure(null, seat, settings);
            motion.SetCushionIntensity(0f);
            SeatMotionSignals signals = new SeatMotionSignals(new Vector3(2f, 2f, 0f), 0f, FrameTime);

            Vector3 offset = Vector3.zero;
            for (int frame = 0; frame < 100; frame++)
            {
                offset = motion.UpdateSeatMotion(signals);
            }

            Assert.AreEqual(-0.03f, offset.x, Tolerance);
            Assert.AreEqual(0f, offset.y, Tolerance);
            Assert.AreEqual(0f, offset.z, Tolerance);
        }
    }
}
