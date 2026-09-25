using UnityEngine;

namespace Gley.CameraSystem
{
    public class CorneringModule : ISeatModule
    {
        private readonly HalfLifeSmoother smoother = new HalfLifeSmoother();

        private float strength;
        private float responseHalfLife;
        private float lateralOffset;
        private bool isEnabled;

        public bool IsEnabled => isEnabled;

        public Vector3 UpdateSeatModule(SeatMotionSignals signals, float deltaTime)
        {
            if (!isEnabled)
            {
                lateralOffset = 0f;
                return Vector3.zero;
            }

            float targetOffset = -signals.LocalAcceleration.x * strength;
            lateralOffset = smoother.Smooth(lateralOffset, targetOffset, deltaTime, responseHalfLife);
            return new Vector3(lateralOffset, 0f, 0f);
        }

        public void Configure(bool enabled, float corneringStrength, float halfLife)
        {
            isEnabled = enabled;
            strength = corneringStrength;
            responseHalfLife = halfLife;
            Reset();
        }

        public void Reset()
        {
            lateralOffset = 0f;
        }
    }
}
