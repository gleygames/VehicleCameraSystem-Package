using UnityEngine;

namespace Gley.CameraSystem
{
    public class Recenter
    {
        private float remainingTime;
        private bool isPending;
        private bool isCountingDown;
        private bool hasGestureInput;

        public float RemainingTime => remainingTime;
        public bool IsPending => isPending;
        public bool IsCountingDown => isCountingDown;

        public bool UpdateRecenterCountdown(float scaledDeltaTime, RecenterMode mode, float delay, float stationaryThreshold, float speed, bool canRecenter, bool isHoldingInput)
        {
            bool isReceivingInput = isHoldingInput || hasGestureInput;
            hasGestureInput = false;
            if (!isPending || !canRecenter || isReceivingInput || mode != RecenterMode.Timed || speed <= stationaryThreshold)
            {
                isCountingDown = false;
                return false;
            }

            if (!isCountingDown)
            {
                remainingTime = Mathf.Max(0f, delay);
                isCountingDown = true;
            }

            remainingTime -= scaledDeltaTime;
            if (remainingTime > 0f)
            {
                return false;
            }

            isCountingDown = false;
            return true;
        }

        public void NoteGestureInput()
        {
            hasGestureInput = true;
        }

        public void MarkPending()
        {
            isPending = true;
            isCountingDown = false;
        }

        public void ClearPending()
        {
            isPending = false;
            isCountingDown = false;
        }

        public void Clear()
        {
            remainingTime = 0f;
            isPending = false;
            isCountingDown = false;
            hasGestureInput = false;
        }
    }
}
