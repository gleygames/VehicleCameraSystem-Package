using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gley.CameraSystem
{
    [Serializable]
    public class CameraPlayerPreferences
    {
        public const int CurrentFormatVersion = 1;
        public const float UsePresetRecenterDelay = -1f;
        public const float MinimumCushionIntensity = 0f;
        public const float MaximumCushionIntensity = 2f;
        public const float MinimumSensitivity = 0.25f;
        public const float MaximumSensitivity = 4f;

        [SerializeField] private List<ViewPreferenceEntry> viewEntries = new List<ViewPreferenceEntry>();
        [SerializeField] private RecenterModeOverride recenterModeOverride = RecenterModeOverride.UsePreset;
        [SerializeField] private float recenterDelayOverride = UsePresetRecenterDelay;
        [SerializeField] private float cushionIntensity = 1f;
        [SerializeField] private float orbitSensitivity = 1f;
        [SerializeField] private float lookSensitivity = 1f;
        [SerializeField] private int formatVersion = CurrentFormatVersion;

        public IReadOnlyList<ViewPreferenceEntry> ViewEntries => viewEntries;
        public RecenterModeOverride RecenterModeOverride => recenterModeOverride;
        public float RecenterDelayOverride => recenterDelayOverride;
        public float CushionIntensity => cushionIntensity;
        public float OrbitSensitivity => orbitSensitivity;
        public float LookSensitivity => lookSensitivity;
        public int FormatVersion => formatVersion;

        public void SetRecenterOverride(RecenterModeOverride mode, float delay)
        {
            recenterModeOverride = mode;
            if (delay < 0f)
            {
                recenterDelayOverride = UsePresetRecenterDelay;
            }
            else
            {
                recenterDelayOverride = delay;
            }
        }

        public void ClearRecenterOverride()
        {
            recenterModeOverride = RecenterModeOverride.UsePreset;
            recenterDelayOverride = UsePresetRecenterDelay;
        }

        public void SetCushionIntensity(float value)
        {
            cushionIntensity = Mathf.Clamp(value, MinimumCushionIntensity, MaximumCushionIntensity);
        }

        public void SetSensitivity(float orbit, float look)
        {
            orbitSensitivity = Mathf.Clamp(orbit, MinimumSensitivity, MaximumSensitivity);
            lookSensitivity = Mathf.Clamp(look, MinimumSensitivity, MaximumSensitivity);
        }

        public void CopyGlobalSettings(CameraPlayerPreferences source)
        {
            SetRecenterOverride(source.RecenterModeOverride, source.RecenterDelayOverride);
            SetCushionIntensity(source.CushionIntensity);
            SetSensitivity(source.OrbitSensitivity, source.LookSensitivity);
        }

        public void AddViewEntry(ViewPreferenceEntry entry)
        {
            if (viewEntries == null)
            {
                viewEntries = new List<ViewPreferenceEntry>();
            }

            viewEntries.Add(entry);
        }

        public void ClearViewEntries()
        {
            if (viewEntries != null)
            {
                viewEntries.Clear();
            }
        }

        public RecenterMode GetRecenterMode(RecenterSettings presetSettings)
        {
            if (recenterModeOverride == RecenterModeOverride.Persistent)
            {
                return RecenterMode.Persistent;
            }

            if (recenterModeOverride == RecenterModeOverride.Timed)
            {
                return RecenterMode.Timed;
            }

            return presetSettings.Mode;
        }

        public float GetRecenterDelay(RecenterSettings presetSettings)
        {
            if (recenterDelayOverride < 0f)
            {
                return presetSettings.Delay;
            }

            return recenterDelayOverride;
        }
    }
}
