using UnityEngine;

namespace Gley.CameraSystem
{
    public class HalfLifeSmoother
    {
        public float Factor(float deltaTime, float halfLife)
        {
            if (halfLife <= 0f)
            {
                return 1f;
            }

            if (deltaTime <= 0f)
            {
                return 0f;
            }

            return 1f - Mathf.Pow(0.5f, deltaTime / halfLife);
        }

        public float Smooth(float current, float target, float deltaTime, float halfLife)
        {
            return Mathf.Lerp(current, target, Factor(deltaTime, halfLife));
        }

        public Vector3 Smooth(Vector3 current, Vector3 target, float deltaTime, float halfLife)
        {
            return Vector3.Lerp(current, target, Factor(deltaTime, halfLife));
        }

        public Quaternion Smooth(Quaternion current, Quaternion target, float deltaTime, float halfLife)
        {
            return Quaternion.Slerp(current, target, Factor(deltaTime, halfLife));
        }
    }
}
