using NUnit.Framework;
using UnityEngine;

namespace Gley.CameraSystem.Tests.EditMode
{
    public class CameraViewPresetTests
    {
        private CameraViewPreset preset;

        [SetUp]
        public void SetUp()
        {
            preset = ScriptableObject.CreateInstance<CameraViewPreset>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(preset);
        }

        [Test]
        public void Configure_Driving_UsesSection12Defaults()
        {
            preset.Configure(CameraViewType.Driving);

            Assert.AreEqual(6f, preset.OrbitMovement.ManualTravelSpeed);
            Assert.AreEqual(0.4f, preset.Travel.EaseTime);
            Assert.AreEqual(0.12f, preset.Driving.FollowHalfLife);
            Assert.AreEqual(140f, preset.Interior.HeadLimitLeft);
            Assert.AreEqual(4f, preset.Recenter.Delay);
            Assert.AreEqual(0.25f, preset.Collision.CameraRadius);
            Assert.AreEqual(0.3f, preset.Rendering.NearClip);
        }

        [Test]
        public void Configure_Presentation_SpeedDistanceOffAndOwnerBodyAim()
        {
            preset.Configure(CameraViewType.Presentation);

            Assert.IsFalse(preset.Driving.SpeedDistanceEnabled);
            Assert.AreEqual(AimFrame.OwnerBody, preset.AimFrame);
        }

        [Test]
        public void Configure_Interior_RollFollowOneAndTimedRecenterTwoSeconds()
        {
            preset.Configure(CameraViewType.Interior);

            Assert.AreEqual(1f, preset.OrbitMovement.ImageRollFollow);
            Assert.AreEqual(RecenterMode.Timed, preset.Recenter.Mode);
            Assert.AreEqual(2f, preset.Recenter.Delay);
        }

        [Test]
        public void Configure_Fixed_NoTurnLookNoCollision()
        {
            preset.Configure(CameraViewType.Fixed);

            Assert.IsFalse(preset.Driving.TurnLookEnabled);
            Assert.IsFalse(preset.Interior.TurnLookEnabled);
            Assert.IsFalse(preset.Collision.Enabled);
        }

        [Test]
        public void NewPreset_FormatVersionOne()
        {
            Assert.AreEqual(1, CameraViewPreset.CurrentFormatVersion);
            Assert.AreEqual(1, preset.FormatVersion);
        }
    }
}
