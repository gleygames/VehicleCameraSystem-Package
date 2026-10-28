using NUnit.Framework;
using UnityEngine;

namespace Gley.CameraSystem.Tests.EditMode
{
    public class InteriorViewTests
    {
        private const float Tolerance = 0.0001f;
        private const float FrameTime = 0.02f;

        [Test]
        public void Yaw_ClampedToLeftAndRightLimits_Independent()
        {
            InteriorView view = new InteriorView();
            view.Configure(CreateSettings(60f, 30f, 40f, 30f, false));

            view.SetHeldIntent(1f, 0f);
            RunFrames(view, 10);
            Assert.AreEqual(24f, view.Yaw, Tolerance);
            RunFrames(view, 50);
            Assert.AreEqual(30f, view.Yaw, Tolerance);

            view.SetHeldIntent(-1f, 0f);
            RunFrames(view, 100);
            Assert.AreEqual(-60f, view.Yaw, Tolerance);
            Assert.AreEqual(0f, view.Pitch, Tolerance);

            view.SetHeldIntent(0f, 0f);
            view.AddDrag(new Vector2(0.1f, 0f));
            Assert.AreEqual(-42f, view.Yaw, Tolerance);
            view.AddDrag(new Vector2(1f, 0f));
            Assert.AreEqual(30f, view.Yaw, Tolerance);
            view.AddDrag(new Vector2(-2f, 0f));
            Assert.AreEqual(-60f, view.Yaw, Tolerance);
        }

        [Test]
        public void Pitch_ClampedToUpAndDownLimits()
        {
            InteriorView view = new InteriorView();
            view.Configure(CreateSettings(140f, 140f, 40f, 30f, false));

            view.SetHeldIntent(0f, 1f);
            RunFrames(view, 10);
            Assert.AreEqual(24f, view.Pitch, Tolerance);
            RunFrames(view, 50);
            Assert.AreEqual(40f, view.Pitch, Tolerance);

            view.SetHeldIntent(0f, -1f);
            RunFrames(view, 100);
            Assert.AreEqual(-30f, view.Pitch, Tolerance);
            Assert.AreEqual(0f, view.Yaw, Tolerance);

            view.SetHeldIntent(0f, 0f);
            view.AddDrag(new Vector2(0f, 0.1f));
            Assert.AreEqual(-12f, view.Pitch, Tolerance);
            view.AddDrag(new Vector2(0f, 1f));
            Assert.AreEqual(40f, view.Pitch, Tolerance);
            view.AddDrag(new Vector2(0f, -1f));
            Assert.AreEqual(-30f, view.Pitch, Tolerance);
        }

        [Test]
        public void TurnLook_YawsTowardTurn_WithinLimits()
        {
            InteriorSettings settings = CreateSettings(140f, 15f, 40f, 30f, true);
            InteriorView view = new InteriorView();
            view.Configure(settings);
            TurnLook turnLook = new TurnLook();

            float rightTurnOffset = turnLook.UpdateInteriorTurnLook(FrameTime, 15f, false, 0f, settings, false);
            Assert.AreEqual(10f, rightTurnOffset, Tolerance);
            Assert.AreEqual(10f, view.GetLookYaw(rightTurnOffset), Tolerance);

            float hardRightOffset = turnLook.UpdateInteriorTurnLook(FrameTime, 90f, false, 0f, settings, false);
            Assert.AreEqual(20f, hardRightOffset, Tolerance);
            Assert.AreEqual(15f, view.GetLookYaw(hardRightOffset), Tolerance);

            float hintOffset = turnLook.UpdateInteriorTurnLook(FrameTime, 0f, true, -1f, settings, false);
            Assert.AreEqual(-10f, hintOffset, Tolerance);
            Assert.AreEqual(-10f, view.GetLookYaw(hintOffset), Tolerance);

            float hardLeftOffset = turnLook.UpdateInteriorTurnLook(FrameTime, -90f, false, 0f, settings, false);
            Assert.AreEqual(-20f, hardLeftOffset, Tolerance);
            Assert.AreEqual(-20f, view.GetLookYaw(hardLeftOffset), Tolerance);

            Quaternion rotation = view.ComputeRotation(Quaternion.identity, hardRightOffset, 1f);
            Vector3 expectedForward = new Vector3(Mathf.Sin(15f * Mathf.Deg2Rad), 0f, Mathf.Cos(15f * Mathf.Deg2Rad));
            Assert.Less(Vector3.Distance(expectedForward, rotation * Vector3.forward), Tolerance);

            Assert.AreEqual(0f, turnLook.UpdateInteriorTurnLook(FrameTime, 90f, false, 0f, settings, true), Tolerance);
            turnLook.Suspend();
            Assert.AreEqual(0f, turnLook.UpdateInteriorTurnLook(FrameTime, 90f, false, 0f, settings, false), Tolerance);
        }

        private InteriorSettings CreateSettings(float left, float right, float up, float down, bool turnLook)
        {
            InteriorSettings settings = new InteriorSettings();
            settings.Configure(left, right, up, down, 120f, 180f, turnLook, 20f, 0f, 30f, 0.5f, true, true, true, 0.02f, 0.03f, 0.015f, 0.1f, 1.5f, 0.5f, 0.05f);
            return settings;
        }

        private void RunFrames(InteriorView view, int count)
        {
            for (int frame = 0; frame < count; frame++)
            {
                view.UpdateInteriorHeadLook(FrameTime);
            }
        }
    }
}
