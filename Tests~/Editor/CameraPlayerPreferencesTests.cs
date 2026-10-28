using NUnit.Framework;
using UnityEngine;

namespace Gley.CameraSystem.Tests.EditMode
{
    public class CameraPlayerPreferencesTests
    {
        private const float Tolerance = 0.0001f;

        [Test]
        public void JsonRoundTrip_PreservesAllFields()
        {
            CameraPlayerPreferences preferences = new CameraPlayerPreferences();
            preferences.SetRecenterOverride(RecenterModeOverride.Timed, 2.5f);
            preferences.SetCushionIntensity(1.5f);
            preferences.SetSensitivity(2f, 0.5f);
            preferences.AddViewEntry(new ViewPreferenceEntry("profile-a", 3, true, new OrbitPose(30f, -1f, 0.5f), false, default));
            preferences.AddViewEntry(new ViewPreferenceEntry("profile-b", 7, true, new OrbitPose(-45f, 0.25f, -0.75f), true, new OrbitPose(170f, -2f, 1.25f)));

            string json = JsonUtility.ToJson(preferences);
            CameraPlayerPreferences restored = JsonUtility.FromJson<CameraPlayerPreferences>(json);

            Assert.AreEqual(1, restored.FormatVersion);
            Assert.AreEqual(RecenterModeOverride.Timed, restored.RecenterModeOverride);
            Assert.AreEqual(2.5f, restored.RecenterDelayOverride, Tolerance);
            Assert.AreEqual(1.5f, restored.CushionIntensity, Tolerance);
            Assert.AreEqual(2f, restored.OrbitSensitivity, Tolerance);
            Assert.AreEqual(0.5f, restored.LookSensitivity, Tolerance);
            Assert.AreEqual(2, restored.ViewEntries.Count);

            ViewPreferenceEntry first = restored.ViewEntries[0];
            Assert.AreEqual("profile-a", first.ProfileId);
            Assert.AreEqual(3, first.ViewId);
            Assert.IsTrue(first.HasRearDefault);
            AssertPose(new OrbitPose(30f, -1f, 0.5f), first.RearDefault);
            Assert.IsFalse(first.HasFrontDefault);

            ViewPreferenceEntry second = restored.ViewEntries[1];
            Assert.AreEqual("profile-b", second.ProfileId);
            Assert.AreEqual(7, second.ViewId);
            Assert.IsTrue(second.HasRearDefault);
            AssertPose(new OrbitPose(-45f, 0.25f, -0.75f), second.RearDefault);
            Assert.IsTrue(second.HasFrontDefault);
            AssertPose(new OrbitPose(170f, -2f, 1.25f), second.FrontDefault);
        }

        [Test]
        public void OlderJsonWithoutCushionField_DefaultsToOne()
        {
            string json = "{\"formatVersion\":1,\"orbitSensitivity\":2.0,\"viewEntries\":[{\"profileId\":\"profile-a\",\"viewId\":4,\"hasRearDefault\":true,\"rearDefault\":{\"bearing\":20.0,\"zoomOffset\":-1.0,\"heightOffset\":0.5}}]}";

            CameraPlayerPreferences restored = JsonUtility.FromJson<CameraPlayerPreferences>(json);

            Assert.AreEqual(1f, restored.CushionIntensity, Tolerance);
            Assert.AreEqual(1f, restored.LookSensitivity, Tolerance);
            Assert.AreEqual(2f, restored.OrbitSensitivity, Tolerance);
            Assert.AreEqual(RecenterModeOverride.UsePreset, restored.RecenterModeOverride);
            Assert.AreEqual(CameraPlayerPreferences.UsePresetRecenterDelay, restored.RecenterDelayOverride, Tolerance);
            Assert.AreEqual(1, restored.ViewEntries.Count);
            ViewPreferenceEntry entry = restored.ViewEntries[0];
            Assert.AreEqual("profile-a", entry.ProfileId);
            Assert.AreEqual(4, entry.ViewId);
            Assert.IsTrue(entry.HasRearDefault);
            AssertPose(new OrbitPose(20f, -1f, 0.5f), entry.RearDefault);
            Assert.IsFalse(entry.HasFrontDefault);
        }

        private void AssertPose(OrbitPose expected, OrbitPose actual)
        {
            Assert.AreEqual(expected.Bearing, actual.Bearing, Tolerance);
            Assert.AreEqual(expected.ZoomOffset, actual.ZoomOffset, Tolerance);
            Assert.AreEqual(expected.HeightOffset, actual.HeightOffset, Tolerance);
        }
    }
}
