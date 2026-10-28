using Gley.CameraSystem.Input;
using NUnit.Framework;
using UnityEngine;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace Gley.CameraSystem.Tests.Input
{
    public class TouchGestureRecognizerTests
    {
        private TouchGestureRecognizer recognizer;

        [SetUp]
        public void SetUp()
        {
            recognizer = new TouchGestureRecognizer();
        }

        [Test]
        public void TouchStartingOnUi_IgnoredUntilReleased()
        {
            Begin(0f);
            recognizer.FeedTouch(1, TouchPhase.Began, Vector2.zero, true, true);
            End();
            Begin(0.1f);
            recognizer.FeedTouch(1, TouchPhase.Moved, new Vector2(100f, 0f), false, true);
            End();
            Assert.AreEqual(Vector2.zero, recognizer.Drag);
            recognizer.FeedTouch(1, TouchPhase.Ended, new Vector2(100f, 0f), false, true);
            Begin(0.2f);
            recognizer.FeedTouch(2, TouchPhase.Began, Vector2.zero, false, true);
            End();
            Begin(0.3f);
            recognizer.FeedTouch(2, TouchPhase.Moved, new Vector2(100f, 0f), false, true);
            End();
            Assert.AreNotEqual(Vector2.zero, recognizer.Drag);
        }

        [Test]
        public void TouchOutsideGestureArea_Ignored()
        {
            Begin(0f);
            recognizer.FeedTouch(1, TouchPhase.Began, Vector2.zero, false, false);
            End();
            Begin(0.1f);
            recognizer.FeedTouch(1, TouchPhase.Moved, new Vector2(100f, 0f), false, true);
            End();
            Assert.AreEqual(Vector2.zero, recognizer.Drag);
        }

        [Test]
        public void SmallMovementBelowThreshold_NoDrag()
        {
            Begin(0f);
            recognizer.FeedTouch(1, TouchPhase.Began, Vector2.zero, false, true);
            End();
            Begin(0.1f);
            recognizer.FeedTouch(1, TouchPhase.Moved, new Vector2(9f, 0f), false, true);
            End();
            Assert.AreEqual(Vector2.zero, recognizer.Drag);
        }

        [Test]
        public void Drag_FullWidth_SendsNormalizedOne()
        {
            Begin(0f);
            recognizer.FeedTouch(1, TouchPhase.Began, Vector2.zero, false, true);
            End();
            Begin(0.1f);
            recognizer.FeedTouch(1, TouchPhase.Moved, new Vector2(1000f, 0f), false, true);
            End();
            Assert.AreEqual(1f, recognizer.Drag.x, 0.0001f);
        }

        [Test]
        public void Pinch_Spread_ZoomsIn()
        {
            Begin(0f);
            recognizer.FeedTouch(1, TouchPhase.Began, new Vector2(400f, 0f), false, true);
            recognizer.FeedTouch(2, TouchPhase.Began, new Vector2(600f, 0f), false, true);
            End();
            Begin(0.1f);
            recognizer.FeedTouch(1, TouchPhase.Moved, new Vector2(350f, 0f), false, true);
            recognizer.FeedTouch(2, TouchPhase.Moved, new Vector2(650f, 0f), false, true);
            End();
            Assert.AreEqual(0.1f, recognizer.Pinch, 0.0001f);
            Assert.AreEqual(Vector2.zero, recognizer.Drag);
        }

        [Test]
        public void DoubleTap_WithinLimits_TriggersReset()
        {
            Tap(1, 0f, Vector2.zero);
            Tap(2, 0.2f, new Vector2(10f, 0f));
            Assert.IsTrue(recognizer.DoubleTapped);
        }

        [Test]
        public void SlowSecondTap_NoReset()
        {
            Tap(1, 0f, Vector2.zero);
            Tap(2, 0.4f, Vector2.zero);
            Assert.IsFalse(recognizer.DoubleTapped);
        }

        [Test]
        public void MouseEmulation_LeftDrag_EqualsOneFingerDrag()
        {
            TouchGestureRecognizer mouse = new TouchGestureRecognizer();
            Begin(0f);
            recognizer.FeedTouch(1, TouchPhase.Began, Vector2.zero, false, true);
            mouse.BeginFrame(1000f, 500f, 0f);
            mouse.FeedMouse(true, Vector2.zero, false, true);
            End();
            mouse.EndFrame();
            Begin(0.1f);
            recognizer.FeedTouch(1, TouchPhase.Moved, new Vector2(100f, 50f), false, true);
            mouse.BeginFrame(1000f, 500f, 0.1f);
            mouse.FeedMouse(true, new Vector2(100f, 50f), false, true);
            End();
            mouse.EndFrame();
            Assert.AreEqual(recognizer.Drag, mouse.Drag);
        }

        private void Begin(float time)
        {
            recognizer.BeginFrame(1000f, 500f, time);
        }

        private void End()
        {
            recognizer.EndFrame();
        }

        private void Tap(int id, float time, Vector2 position)
        {
            Begin(time);
            recognizer.FeedTouch(id, TouchPhase.Began, position, false, true);
            End();
            Begin(time + 0.05f);
            recognizer.FeedTouch(id, TouchPhase.Ended, position, false, true);
            End();
        }
    }
}
