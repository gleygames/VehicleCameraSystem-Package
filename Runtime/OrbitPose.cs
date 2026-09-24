using System;
using UnityEngine;

namespace Gley.CameraSystem
{
    [Serializable]
    public struct OrbitPose
    {
        [SerializeField] private float bearing;
        [SerializeField] private float zoomOffset;
        [SerializeField] private float heightOffset;

        public float Bearing => bearing;
        public float ZoomOffset => zoomOffset;
        public float HeightOffset => heightOffset;

        public OrbitPose(float bearing, float zoomOffset, float heightOffset)
        {
            this.bearing = bearing;
            this.zoomOffset = zoomOffset;
            this.heightOffset = heightOffset;
        }
    }
}
