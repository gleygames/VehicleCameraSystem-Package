using UnityEngine;

namespace Gley.CameraSystem
{
    public class CushionModule : ISeatModule
    {
        private readonly HalfLifeSmoother smoother = new HalfLifeSmoother();

        private float cutoff;
        private float gain;
        private float responseHalfLife;
        private float intensity = 1f;
        private float previousInput;
        private float filteredAcceleration;
        private float verticalOffset;
        private bool isEnabled;

        public bool IsEnabled => isEnabled;

        public Vector3 UpdateSeatModule(SeatMotionSignals signals, float deltaTime)
        {
            if (!isEnabled)
            {
                return Vector3.zero;
            }

            if (deltaTime > 0f)
            {
                float input = signals.LocalAcceleration.y;
                float alpha = 1f / (1f + 2f * Mathf.PI * cutoff * deltaTime);
                filteredAcceleration = alpha * (filteredAcceleration + input - previousInput);
                previousInput = input;
            }

            if (intensity <= 0f)
            {
                verticalOffset = 0f;
                return Vector3.zero;
            }

            float targetOffset = -filteredAcceleration * gain * intensity;
            verticalOffset = smoother.Smooth(verticalOffset, targetOffset, deltaTime, responseHalfLife);
            return new Vector3(0f, verticalOffset, 0f);
        }

        public void Configure(bool enabled, float highPassCutoff, float motionGain, float halfLife)
        {
            isEnabled = enabled;
            cutoff = Mathf.Max(0f, highPassCutoff);
            gain = motionGain;
            responseHalfLife = halfLife;
            Reset();
        }

        public void SetIntensity(float value)
        {
            intensity = Mathf.Clamp(value, CameraPlayerPreferences.MinimumCushionIntensity, CameraPlayerPreferences.MaximumCushionIntensity);
        }

        public void Reset()
        {
            previousInput = 0f;
            filteredAcceleration = 0f;
            verticalOffset = 0f;
        }
    }
}
