using NUnit.Framework;
using UnityEngine;

namespace Gley.CameraSystem.Tests.EditMode
{
    public class CorneringModuleTests
    {
        private const float FrameTime = 0.02f;
        private const float Tolerance = 0.0001f;

        [Test]
        public void LeftTurn_EyeMovesRight()
        {
            CorneringModule module = new CorneringModule();
            module.Configure(true, 0.015f, 0f);
            SeatMotionSignals signals = new SeatMotionSignals(Vector3.left * 2f, 0f, FrameTime);

            Vector3 offset = module.UpdateSeatModule(signals, FrameTime);

            Assert.AreEqual(0.03f, offset.x, Tolerance);
            Assert.AreEqual(0f, offset.y, Tolerance);
            Assert.AreEqual(0f, offset.z, Tolerance);
        }

        [Test]
        public void OffsetEqualsStrengthTimesLateralAcceleration_AfterSettling()
        {
            CorneringModule module = new CorneringModule();
            module.Configure(true, 0.015f, 0.1f);
            SeatMotionSignals signals = new SeatMotionSignals(new Vector3(2f, 8f, -6f), 0f, FrameTime);

            Vector3 offset = Vector3.zero;
            for (int frame = 0; frame < 100; frame++)
            {
                offset = module.UpdateSeatModule(signals, FrameTime);
            }

            Assert.AreEqual(-0.03f, offset.x, Tolerance);
            Assert.AreEqual(0f, offset.y, Tolerance);
            Assert.AreEqual(0f, offset.z, Tolerance);
        }

        [Test]
        public void Disabled_ZeroOffset()
        {
            CorneringModule module = new CorneringModule();
            module.Configure(true, 0.015f, 0f);
            SeatMotionSignals signals = new SeatMotionSignals(Vector3.right * 2f, 0f, FrameTime);
            module.UpdateSeatModule(signals, FrameTime);
            module.Configure(false, 0.015f, 0.1f);

            Vector3 offset = module.UpdateSeatModule(signals, FrameTime);

            Assert.AreEqual(Vector3.zero, offset);
        }
    }
}
