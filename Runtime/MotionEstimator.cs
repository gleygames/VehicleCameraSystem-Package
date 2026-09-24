using UnityEngine;

namespace Gley.CameraSystem
{
    public class MotionEstimator
    {
        private VehicleCameraTarget target;
        private Transform rootBody;
        private Rigidbody rootRigidbody;
        private Vector3 rootLocalPoint;
        private Vector3 previousWorldPoint;
        private Vector3 previousPointVelocity;
        private Quaternion previousRotation;
        private bool hasHistory;
        private bool hasPreviousPointVelocity;
        private bool previousVelocityFromRigidbody;

        public Vector3 WorldVelocity { get; private set; }
        public Vector3 LocalAcceleration { get; private set; }
        public float Speed { get; private set; }
        public float YawRate { get; private set; }

        public void Configure(VehicleCameraTarget target, Vector3 rootLocalPoint)
        {
            this.target = target;
            this.rootLocalPoint = rootLocalPoint;
            rootBody = null;
            rootRigidbody = null;
            if (target != null && target.IsRootAlive && target.BodyCount > 0)
            {
                rootBody = target.GetBody(target.RootIndex);
                rootRigidbody = rootBody.GetComponent<Rigidbody>();
            }

            Reset();
        }

        public void UpdateMotionEstimate(float scaledDeltaTime)
        {
            if (scaledDeltaTime <= 0f)
            {
                return;
            }

            if (target == null || rootBody == null)
            {
                Reset();
                return;
            }

            Vector3 worldPoint = rootBody.TransformPoint(rootLocalPoint);
            bool fromRigidbody = rootRigidbody != null;
            Vector3 pointVelocity = Vector3.zero;
            float filterAmount = GetFilterAmount(scaledDeltaTime);

            if (fromRigidbody)
            {
                pointVelocity = rootRigidbody.GetPointVelocity(worldPoint);
                WorldVelocity = pointVelocity;
            }
            else if (hasHistory)
            {
                pointVelocity = (worldPoint - previousWorldPoint) / scaledDeltaTime;
                WorldVelocity = Vector3.LerpUnclamped(WorldVelocity, pointVelocity, filterAmount);
            }
            else
            {
                WorldVelocity = Vector3.zero;
            }

            if (target.HasSuppliedSpeed)
            {
                Speed = Mathf.Max(0f, target.SuppliedSpeed);
            }
            else
            {
                Speed = WorldVelocity.magnitude;
            }

            if (target.HasSuppliedAcceleration)
            {
                LocalAcceleration = target.SuppliedAcceleration;
            }
            else if (hasPreviousPointVelocity && previousVelocityFromRigidbody == fromRigidbody)
            {
                Vector3 worldAcceleration = (pointVelocity - previousPointVelocity) / scaledDeltaTime;
                Vector3 localAcceleration = rootBody.InverseTransformDirection(worldAcceleration);
                LocalAcceleration = Vector3.LerpUnclamped(LocalAcceleration, localAcceleration, filterAmount);
            }
            else
            {
                LocalAcceleration = Vector3.zero;
            }

            if (fromRigidbody)
            {
                YawRate = Vector3.Dot(rootRigidbody.angularVelocity, rootBody.up) * Mathf.Rad2Deg;
            }
            else if (hasHistory)
            {
                Quaternion rotationChange = rootBody.rotation * Quaternion.Inverse(previousRotation);
                rotationChange.ToAngleAxis(out float angle, out Vector3 axis);
                if (angle > 180f)
                {
                    angle -= 360f;
                }

                float rawYawRate = angle * Vector3.Dot(axis, rootBody.up) / scaledDeltaTime;
                YawRate = Mathf.LerpUnclamped(YawRate, rawYawRate, filterAmount);
            }
            else
            {
                YawRate = 0f;
            }

            previousWorldPoint = worldPoint;
            previousRotation = rootBody.rotation;
            previousPointVelocity = pointVelocity;
            hasPreviousPointVelocity = fromRigidbody || hasHistory;
            previousVelocityFromRigidbody = fromRigidbody;
            hasHistory = true;
        }

        public bool IsStationary(float threshold)
        {
            return Speed <= Mathf.Max(0f, threshold);
        }

        public void Reset()
        {
            WorldVelocity = Vector3.zero;
            LocalAcceleration = Vector3.zero;
            Speed = 0f;
            YawRate = 0f;
            previousWorldPoint = Vector3.zero;
            previousPointVelocity = Vector3.zero;
            previousRotation = Quaternion.identity;
            hasHistory = false;
            hasPreviousPointVelocity = false;
            previousVelocityFromRigidbody = false;
        }

        public void ShiftOrigin(Vector3 offset)
        {
            if (hasHistory)
            {
                previousWorldPoint += offset;
            }
        }

        private float GetFilterAmount(float scaledDeltaTime)
        {
            float halfLife = Mathf.Max(0f, target.EstimateFilterHalfLife);
            if (halfLife == 0f)
            {
                return 1f;
            }

            return 1f - Mathf.Pow(0.5f, scaledDeltaTime / halfLife);
        }
    }
}
