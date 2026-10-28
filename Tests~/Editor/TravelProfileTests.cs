using NUnit.Framework;

namespace Gley.CameraSystem.Tests.EditMode
{
    public class TravelProfileTests
    {
        [Test]
        public void LongRoute_ReachesCruiseSpeed_EndsExactlyAtLength()
        {
            TravelProfile profile = new TravelProfile();
            profile.Begin(100f, 10f, 0.4f);

            Assert.AreEqual(10f, profile.PeakSpeed, 0.001f);
            Assert.AreEqual(2f, profile.AdvanceTravel(0.4f), 0.001f);
            Assert.AreEqual(50f, profile.AdvanceTravel(4.8f), 0.001f);
            Assert.AreEqual(100f, profile.AdvanceTravel(5.2f), 0f);
            Assert.IsTrue(profile.IsComplete);
        }

        [Test]
        public void ShortRoute_TrianglePeakBelowCruise_EndsExactly()
        {
            TravelProfile profile = new TravelProfile();
            profile.Begin(1f, 10f, 0.4f);

            Assert.AreEqual(5f, profile.PeakSpeed, 0.001f);
            Assert.AreEqual(0.5f, profile.AdvanceTravel(0.2f), 0.001f);
            Assert.AreEqual(1f, profile.AdvanceTravel(0.2f), 0f);
            Assert.IsTrue(profile.IsComplete);
        }

        [Test]
        public void Duration_TotalTimeMatchesWithinOneFrame()
        {
            TravelProfile profile = new TravelProfile();
            profile.BeginWithDuration(20f, 2f, 0.4f);
            float advancedTime = 0f;
            int frameCount = 0;

            while (!profile.IsComplete && frameCount < 200)
            {
                profile.AdvanceTravel(0.016f);
                advancedTime += 0.016f;
                frameCount++;
            }

            Assert.IsTrue(profile.IsComplete);
            Assert.GreaterOrEqual(advancedTime, 2f);
            Assert.LessOrEqual(advancedTime, 2.0161f);
            Assert.AreEqual(20f, profile.DistanceTravelled, 0f);
            Assert.AreEqual(12.5f, profile.PeakSpeed, 0.001f);

            TravelProfile frameAlignedProfile = new TravelProfile();
            frameAlignedProfile.BeginWithDuration(20f, 2f, 0.4f);
            for (int frame = 0; frame < 100; frame++)
            {
                frameAlignedProfile.AdvanceTravel(0.02f);
            }

            Assert.IsTrue(frameAlignedProfile.IsComplete);
            Assert.AreEqual(20f, frameAlignedProfile.DistanceTravelled, 0f);
        }

        [Test]
        public void NeverOvershoots_VariableDeltaTimes()
        {
            TravelProfile profile = new TravelProfile();
            profile.Begin(17f, 4f, 0.8f);
            float[] deltaTimes = { 0.01f, 0.17f, 0.03f, 0.4f, 0.11f };
            float previousDistance = 0f;
            int frameCount = 0;

            while (!profile.IsComplete && frameCount < 100)
            {
                float distance = profile.AdvanceTravel(deltaTimes[frameCount % deltaTimes.Length]);
                Assert.GreaterOrEqual(distance, previousDistance);
                Assert.LessOrEqual(distance, 17f);
                previousDistance = distance;
                frameCount++;
            }

            Assert.IsTrue(profile.IsComplete);
            Assert.AreEqual(17f, profile.DistanceTravelled, 0f);
        }

        [Test]
        public void ZeroLengthRoute_CompletesImmediately()
        {
            TravelProfile profile = new TravelProfile();
            profile.Begin(0f, 10f, 0.4f);

            Assert.IsTrue(profile.IsComplete);
            Assert.AreEqual(0f, profile.AdvanceTravel(1f), 0f);
        }
    }
}
