using UnityEngine;

namespace Gley.CameraSystem
{
    public class AccelerationBrakingModule : ISeatModule
    {
        private readonly HalfLifeSmoother smoother = new HalfLifeSmoother();

        private float accelerationStrength;
        private float brakingStrength;
        private float responseHalfLife;
        private float foreAftOffset;
        private bool isEnabled;

        public bool IsEnabled => isEnabled;

        public Vector3 UpdateSeatModule(SeatMotionSignals signals, float deltaTime)
        {
            if (!isEnabled)
            {
                foreAftOffset = 0f;
                return Vector3.zero;
            }

            float longitudinalAcceleration = signals.LocalAcceleration.z;
            float strength = accelerationStrength;
            if (longitudinalAcceleration < 0f)
            {
                strength = brakingStrength;
            }

            float targetOffset = -longitudinalAcceleration * strength;
            foreAftOffset = smoother.Smooth(foreAftOffset, targetOffset, deltaTime, responseHalfLife);
            return new Vector3(0f, 0f, foreAftOffset);
        }

        public void Configure(bool enabled, float acceleration, float braking, float halfLife)
        {
            isEnabled = enabled;
            accelerationStrength = acceleration;
            brakingStrength = braking;
            responseHalfLife = halfLife;
            Reset();
        }

        public void Reset()
        {
            foreAftOffset = 0f;
        }
    }
}
