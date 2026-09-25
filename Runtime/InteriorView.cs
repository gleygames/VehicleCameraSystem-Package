using UnityEngine;

namespace Gley.CameraSystem
{
    public class InteriorView
    {
        private const float NeutralTolerance = 0.05f;
        private const float MinimumUpCross = 0.000001f;

        private readonly HalfLifeSmoother smoother = new HalfLifeSmoother();

        private InteriorSettings settings;
        private float horizontalIntent;
        private float verticalIntent;
        private float yaw;
        private float pitch;
        private float sensitivity = 1f;

        public float Yaw => yaw;
        public float Pitch => pitch;
        public float Sensitivity => sensitivity;
        public bool IsNeutral => Mathf.Abs(yaw) < NeutralTolerance && Mathf.Abs(pitch) < NeutralTolerance;

        public void Configure(InteriorSettings interiorSettings)
        {
            settings = interiorSettings;
            horizontalIntent = 0f;
            verticalIntent = 0f;
            yaw = 0f;
            pitch = 0f;
        }

        public void UpdateInteriorHeadLook(float deltaTime)
        {
            if (settings == null)
            {
                return;
            }

            float step = settings.HeadLookRate * sensitivity * deltaTime;
            yaw = ClampYaw(yaw + horizontalIntent * step);
            pitch = ClampPitch(pitch + verticalIntent * step);
        }

        public bool UpdateInteriorHeadReturn(float deltaTime, float halfLife)
        {
            yaw = smoother.Smooth(yaw, 0f, deltaTime, halfLife);
            pitch = smoother.Smooth(pitch, 0f, deltaTime, halfLife);
            if (!IsNeutral)
            {
                return false;
            }

            yaw = 0f;
            pitch = 0f;
            return true;
        }

        public void SetHeldIntent(float horizontal, float vertical)
        {
            horizontalIntent = Mathf.Clamp(horizontal, -1f, 1f);
            verticalIntent = Mathf.Clamp(vertical, -1f, 1f);
        }

        public void AddDrag(Vector2 normalizedDelta)
        {
            if (settings == null)
            {
                return;
            }

            float degreesPerScreen = settings.HeadDrag * sensitivity;
            yaw = ClampYaw(yaw + normalizedDelta.x * degreesPerScreen);
            pitch = ClampPitch(pitch + normalizedDelta.y * degreesPerScreen);
        }

        public void SetSensitivity(float multiplier)
        {
            sensitivity = multiplier;
        }

        public float GetLookYaw(float turnLookOffset)
        {
            if (settings == null)
            {
                return yaw;
            }

            return ClampYaw(yaw + turnLookOffset);
        }

        public Vector3 ComputeEyePosition(Transform rootBody, SeatSettings seat)
        {
            return rootBody.TransformPoint(seat.EyeLocalPosition);
        }

        public Quaternion ComputeRotation(Quaternion rootRotation, float turnLookOffset, float imageRollFollow)
        {
            Quaternion headRotation = rootRotation * Quaternion.AngleAxis(GetLookYaw(turnLookOffset), Vector3.up) * Quaternion.AngleAxis(-pitch, Vector3.right);
            float rollFollow = Mathf.Clamp01(imageRollFollow);
            if (rollFollow >= 1f)
            {
                return headRotation;
            }

            Vector3 forward = headRotation * Vector3.forward;
            Vector3 up = Vector3.Slerp(Vector3.up, rootRotation * Vector3.up, rollFollow);
            if (Vector3.Cross(up, forward).sqrMagnitude < MinimumUpCross)
            {
                return headRotation;
            }

            return Quaternion.LookRotation(forward, up);
        }

        public void Clear()
        {
            settings = null;
            horizontalIntent = 0f;
            verticalIntent = 0f;
            yaw = 0f;
            pitch = 0f;
        }

        private float ClampYaw(float value)
        {
            return Mathf.Clamp(value, -settings.HeadLimitLeft, settings.HeadLimitRight);
        }

        private float ClampPitch(float value)
        {
            return Mathf.Clamp(value, -settings.HeadLimitDown, settings.HeadLimitUp);
        }
    }
}
