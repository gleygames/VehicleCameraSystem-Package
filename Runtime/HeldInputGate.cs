using UnityEngine;

namespace Gley.CameraSystem
{
    public class HeldInputGate
    {
        private const float NeutralThreshold = 0.01f;

        private float horizontal;
        private float vertical;
        private float zoom;
        private float receivedHorizontal;
        private float receivedVertical;
        private float receivedZoom;
        private bool isHorizontalBlocked;
        private bool isVerticalBlocked;
        private bool isZoomBlocked;

        public float Horizontal => horizontal;
        public float Vertical => vertical;
        public float Zoom => zoom;
        public float ReceivedHorizontal => receivedHorizontal;
        public float ReceivedVertical => receivedVertical;
        public float ReceivedZoom => receivedZoom;
        public bool IsNeutral => IsNeutralValue(horizontal) && IsNeutralValue(vertical) && IsNeutralValue(zoom);
        public bool IsReceivedNeutral => IsNeutralValue(receivedHorizontal) && IsNeutralValue(receivedVertical) && IsNeutralValue(receivedZoom);

        public void ReceiveHeldInput(float horizontalValue, float verticalValue, float zoomValue)
        {
            receivedHorizontal = horizontalValue;
            receivedVertical = verticalValue;
            receivedZoom = zoomValue;
            horizontal = FilterAxis(horizontalValue, ref isHorizontalBlocked);
            vertical = FilterAxis(verticalValue, ref isVerticalBlocked);
            zoom = FilterAxis(zoomValue, ref isZoomBlocked);
        }

        public void Rearm()
        {
            isHorizontalBlocked = !IsNeutralValue(receivedHorizontal);
            isVerticalBlocked = !IsNeutralValue(receivedVertical);
            isZoomBlocked = !IsNeutralValue(receivedZoom);
            horizontal = 0f;
            vertical = 0f;
            zoom = 0f;
        }

        public void Clear()
        {
            horizontal = 0f;
            vertical = 0f;
            zoom = 0f;
            receivedHorizontal = 0f;
            receivedVertical = 0f;
            receivedZoom = 0f;
            isHorizontalBlocked = false;
            isVerticalBlocked = false;
            isZoomBlocked = false;
        }

        private float FilterAxis(float value, ref bool isBlocked)
        {
            if (isBlocked && IsNeutralValue(value))
            {
                isBlocked = false;
            }

            if (isBlocked)
            {
                return 0f;
            }

            return value;
        }

        private bool IsNeutralValue(float value)
        {
            return Mathf.Abs(value) < NeutralThreshold;
        }
    }
}
