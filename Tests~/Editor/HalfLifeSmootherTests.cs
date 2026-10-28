using NUnit.Framework;
using UnityEngine;

namespace Gley.CameraSystem.Tests.EditMode
{
    public class HalfLifeSmootherTests
    {
        [Test]
        public void OneHalfLife_ClosesHalfTheGap()
        {
            HalfLifeSmoother smoother = new HalfLifeSmoother();

            Assert.AreEqual(0.5f, smoother.Factor(0.2f, 0.2f), 1e-6f);
            Assert.AreEqual(5f, smoother.Smooth(0f, 10f, 0.2f, 0.2f), 1e-5f);
            Assert.Less(Vector3.Distance(new Vector3(1f, 2f, 3f), smoother.Smooth(Vector3.zero, new Vector3(2f, 4f, 6f), 0.2f, 0.2f)), 1e-5f);
            Quaternion rotation = smoother.Smooth(Quaternion.identity, Quaternion.Euler(0f, 90f, 0f), 0.2f, 0.2f);
            Assert.Less(Quaternion.Angle(Quaternion.Euler(0f, 45f, 0f), rotation), 0.01f);
        }

        [Test]
        public void SplitFrames_SameResultAsOneFrame()
        {
            HalfLifeSmoother smoother = new HalfLifeSmoother();
            float deltaTime = 0.037f;
            float halfLife = 0.12f;

            float oneFrame = smoother.Smooth(2f, 9f, deltaTime, halfLife);
            float firstHalf = smoother.Smooth(2f, 9f, deltaTime * 0.5f, halfLife);
            float twoFrames = smoother.Smooth(firstHalf, 9f, deltaTime * 0.5f, halfLife);

            Assert.AreEqual(oneFrame, twoFrames, 1e-5f);
        }

        [Test]
        public void ZeroHalfLife_Snaps()
        {
            HalfLifeSmoother smoother = new HalfLifeSmoother();

            Assert.AreEqual(1f, smoother.Factor(0.016f, 0f));
            Assert.AreEqual(7f, smoother.Smooth(1f, 7f, 0.016f, 0f));
            Assert.AreEqual(new Vector3(4f, 5f, 6f), smoother.Smooth(Vector3.zero, new Vector3(4f, 5f, 6f), 0.016f, 0f));
            Assert.AreEqual(0f, smoother.Factor(0f, 0.1f));
        }
    }
}
