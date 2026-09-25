using UnityEngine;

namespace Gley.CameraSystem
{
    public class DrivingFollow
    {
        private const float MaximumStepTime = 1f / 120f;
        private const int MaximumStepCount = 16;

        private readonly HalfLifeSmoother smoother = new HalfLifeSmoother();

        private Vector3 laggedPosition;
        private Vector3 previousTargetPosition;
        private bool isSnapPending = true;

        public Vector3 UpdateDrivingFollow(Vector3 targetPosition, float deltaTime, DrivingSettings settings)
        {
            if (isSnapPending)
            {
                isSnapPending = false;
                laggedPosition = targetPosition;
                previousTargetPosition = targetPosition;
                return laggedPosition;
            }

            float maximumLag = Mathf.Max(0f, settings.MaximumLag);
            int stepCount = 1;
            if (deltaTime > MaximumStepTime)
            {
                stepCount = Mathf.Min(Mathf.CeilToInt(deltaTime / MaximumStepTime), MaximumStepCount);
            }

            float stepTime = deltaTime / stepCount;
            Vector3 stepStart = previousTargetPosition;
            for (int step = 1; step <= stepCount; step++)
            {
                Vector3 stepTarget = Vector3.Lerp(previousTargetPosition, targetPosition, (float)step / stepCount);
                laggedPosition = smoother.SmoothTowardMovingTarget(laggedPosition, stepStart, stepTarget, stepTime, settings.FollowHalfLife);
                ClampLag(stepTarget, maximumLag);
                stepStart = stepTarget;
            }

            previousTargetPosition = targetPosition;
            return laggedPosition;
        }

        public void Reset()
        {
            isSnapPending = true;
        }

        public void ShiftOrigin(Vector3 offset)
        {
            laggedPosition += offset;
            previousTargetPosition += offset;
        }

        private void ClampLag(Vector3 targetPosition, float maximumLag)
        {
            Vector3 lag = laggedPosition - targetPosition;
            if (lag.sqrMagnitude <= maximumLag * maximumLag)
            {
                return;
            }

            laggedPosition = targetPosition + lag.normalized * maximumLag;
        }
    }
}
