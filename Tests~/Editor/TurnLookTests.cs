using NUnit.Framework;

namespace Gley.CameraSystem.Tests.EditMode
{
    public class TurnLookTests
    {
        private const float Tolerance = 0.0001f;
        private const float FrameTime = 0.02f;

        [Test]
        public void TurningRight_OffsetTowardLeft()
        {
            TurnLook turnLook = new TurnLook();
            DrivingSettings settings = CreateUnsmoothedSettings();

            Assert.AreEqual(0.5f, turnLook.Signal(15f, false, 0f, 30f, 0.5f), Tolerance);
            float rightTurnOffset = turnLook.UpdateDrivingTurnLook(FrameTime, 15f, false, 0f, settings, false);
            Assert.AreEqual(-6f, rightTurnOffset, Tolerance);
            Assert.Less(rightTurnOffset, 0f);

            float leftTurnOffset = turnLook.UpdateDrivingTurnLook(FrameTime, -15f, false, 0f, settings, false);
            Assert.AreEqual(6f, leftTurnOffset, Tolerance);
        }

        [Test]
        public void HintOnly_AnticipatesTurn()
        {
            TurnLook turnLook = new TurnLook();
            DrivingSettings settings = CreateUnsmoothedSettings();

            Assert.AreEqual(0.5f, turnLook.Signal(0f, true, 1f, 30f, 0.5f), Tolerance);
            Assert.AreEqual(-0.5f, turnLook.Signal(0f, true, -1f, 30f, 0.5f), Tolerance);
            Assert.AreEqual(0f, turnLook.Signal(0f, false, 1f, 30f, 0.5f), Tolerance);
            Assert.AreEqual(-6f, turnLook.UpdateDrivingTurnLook(FrameTime, 0f, true, 1f, settings, false), Tolerance);
            Assert.AreEqual(0.75f, turnLook.Signal(7.5f, true, 1f, 30f, 0.5f), Tolerance);
        }

        [Test]
        public void SignalClamped_MaxOffset()
        {
            TurnLook turnLook = new TurnLook();
            DrivingSettings settings = CreateUnsmoothedSettings();

            Assert.AreEqual(1f, turnLook.Signal(90f, true, 1f, 30f, 0.5f), Tolerance);
            Assert.AreEqual(-1f, turnLook.Signal(-90f, false, 0f, 30f, 0.5f), Tolerance);
            Assert.AreEqual(-12f, turnLook.UpdateDrivingTurnLook(FrameTime, 90f, true, 1f, settings, false), Tolerance);
            Assert.AreEqual(12f, turnLook.UpdateDrivingTurnLook(FrameTime, -90f, true, -1f, settings, false), Tolerance);
        }

        private DrivingSettings CreateUnsmoothedSettings()
        {
            DrivingSettings settings = new DrivingSettings();
            settings.Configure(0.12f, 0.08f, 3f, true, 5f, 25f, 3f, 0.25f, false, true, 12f, 30f, 0f, 0.5f);
            return settings;
        }
    }
}
