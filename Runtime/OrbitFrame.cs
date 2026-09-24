using UnityEngine;

namespace Gley.CameraSystem
{
    public class OrbitFrame
    {
        private readonly Transform root;
        private readonly Quaternion orientationAdjustment;

        public Vector3 Up => root.rotation * orientationAdjustment * Vector3.up;

        public OrbitFrame(Transform root, Quaternion orientationAdjustment)
        {
            this.root = root;
            this.orientationAdjustment = orientationAdjustment;
        }

        public Vector3 ToWorldPosition(Vector3 rootLocalPosition)
        {
            return root.TransformPoint(rootLocalPosition);
        }

        public Vector3 ToWorldInwardNormal(Vector3 rootLocalInwardNormal)
        {
            return root.TransformDirection(rootLocalInwardNormal);
        }
    }
}
