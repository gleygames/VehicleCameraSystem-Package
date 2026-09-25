using UnityEngine;

namespace Gley.CameraSystem
{
    public class SpeedDistance
    {
        public float Offset(float speed, float bearing, DrivingSettings settings, bool reverseActive)
        {
            if (!settings.SpeedDistanceEnabled)
            {
                return 0f;
            }

            if (reverseActive && !settings.SpeedDistanceInReverse)
            {
                return 0f;
            }

            float sideMinimum = settings.SpeedDistanceSideMinimum;
            float bearingFactor = sideMinimum + (1f - sideMinimum) * Mathf.Max(0f, Mathf.Cos(bearing * Mathf.Deg2Rad));
            return settings.MaximumSpeedDistanceOffset * GetSpeedFactor(speed, settings) * bearingFactor;
        }

        private float GetSpeedFactor(float speed, DrivingSettings settings)
        {
            float range = settings.HighSpeed - settings.LowSpeed;
            if (range <= 0f)
            {
                if (speed >= settings.HighSpeed)
                {
                    return 1f;
                }

                return 0f;
            }

            float progress = Mathf.Clamp01((speed - settings.LowSpeed) / range);
            return progress * progress * (3f - 2f * progress);
        }
    }
}
