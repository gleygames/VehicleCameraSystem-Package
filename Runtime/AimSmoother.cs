using UnityEngine;

namespace Gley.CameraSystem
{
    public class AimSmoother
    {
        private readonly HalfLifeSmoother smoother = new HalfLifeSmoother();

        private Quaternion smoothedRotation = Quaternion.identity;
        private bool isSnapPending = true;

        public Quaternion UpdateAimSmoothing(Quaternion aimRotation, float deltaTime, DrivingSettings settings)
        {
            if (isSnapPending)
            {
                isSnapPending = false;
                smoothedRotation = aimRotation;
                return smoothedRotation;
            }

            smoothedRotation = smoother.Smooth(smoothedRotation, aimRotation, deltaTime, settings.AimHalfLife);
            return smoothedRotation;
        }

        public void Reset()
        {
            isSnapPending = true;
        }
    }
}
