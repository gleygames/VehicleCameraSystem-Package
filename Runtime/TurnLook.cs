using UnityEngine;

namespace Gley.CameraSystem
{
    public class TurnLook
    {
        private const float RestThreshold = 0.0001f;

        private readonly HalfLifeSmoother smoother = new HalfLifeSmoother();

        private float offset;
        private bool isSuspended;

        public float Offset => offset;
        public bool IsSuspended => isSuspended;

        public float UpdateDrivingTurnLook(float deltaTime, float yawRate, bool hasHint, float hint, DrivingSettings settings, bool isReverseActive)
        {
            float targetOffset = 0f;
            if (settings.TurnLookEnabled && !isSuspended && !isReverseActive)
            {
                targetOffset = -Signal(yawRate, hasHint, hint, settings.TurnLookFullTurnRate, settings.TurnLookHintWeight) * settings.TurnLookMaximumOffset;
            }

            offset = smoother.Smooth(offset, targetOffset, deltaTime, settings.TurnLookHalfLife);
            if (targetOffset == 0f && Mathf.Abs(offset) < RestThreshold)
            {
                offset = 0f;
            }

            return offset;
        }

        public float Signal(float yawRate, bool hasHint, float hint, float fullTurnRate, float hintWeight)
        {
            float signal = 0f;
            if (fullTurnRate > 0f)
            {
                signal = yawRate / fullTurnRate;
            }

            if (hasHint)
            {
                signal += Mathf.Clamp(hint, -1f, 1f) * hintWeight;
            }

            return Mathf.Clamp(signal, -1f, 1f);
        }

        public void Suspend()
        {
            isSuspended = true;
        }

        public void Resume()
        {
            isSuspended = false;
        }

        public void Reset()
        {
            offset = 0f;
        }

        public void Clear()
        {
            offset = 0f;
            isSuspended = false;
        }
    }
}
