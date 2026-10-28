using NUnit.Framework;
using UnityEngine;

namespace Gley.CameraSystem.Tests.EditMode
{
    public class AccelerationBrakingModuleTests
    {
        private const float FrameTime = 0.02f;
        private const float Tolerance = 0.0001f;

        [Test]
        public void Accelerating_EyeMovesBack_ByStrengthTimesAcceleration()
        {
            AccelerationBrakingModule module = new AccelerationBrakingModule();
            module.Configure(true, 0.02f, 0.03f, 0.1f);
            SeatMotionSignals signals = new SeatMotionSignals(Vector3.forward * 2f, 5f, FrameTime);

            Vector3 offset = Vector3.zero;
            for (int frame = 0; frame < 100; frame++)
            {
                offset = module.UpdateSeatModule(signals, FrameTime);
            }

            Assert.AreEqual(-0.04f, offset.z, Tolerance);
            Assert.AreEqual(0f, offset.x, Tolerance);
            Assert.AreEqual(0f, offset.y, Tolerance);
        }

        [Test]
        public void Braking_UsesBrakingStrength_EyeMovesForward()
        {
            AccelerationBrakingModule module = new AccelerationBrakingModule();
            module.Configure(true, 0.02f, 0.03f, 0.1f);
            SeatMotionSignals signals = new SeatMotionSignals(Vector3.back * 2f, 5f, FrameTime);

            Vector3 offset = Vector3.zero;
            for (int frame = 0; frame < 100; frame++)
            {
                offset = module.UpdateSeatModule(signals, FrameTime);
            }

            Assert.AreEqual(0.06f, offset.z, Tolerance);
            Assert.AreEqual(0f, offset.x, Tolerance);
            Assert.AreEqual(0f, offset.y, Tolerance);
        }

        [Test]
        public void Disabled_ZeroOffset()
        {
            AccelerationBrakingModule module = new AccelerationBrakingModule();
            module.Configure(false, 0.02f, 0.03f, 0.1f);
            SeatMotionSignals signals = new SeatMotionSignals(Vector3.forward * 10f, 10f, FrameTime);

            Assert.IsFalse(module.IsEnabled);
            Assert.AreEqual(Vector3.zero, module.UpdateSeatModule(signals, FrameTime));
        }
    }
}
