using System;
using UnityEngine;

namespace Gley.CameraSystem
{
    [Serializable]
    public class FixedViewPose
    {
        [SerializeField] private Vector3 cameraLocalPosition = Vector3.zero;
        [SerializeField] private Vector3 watchPointLocalPosition = Vector3.forward;

        public Vector3 CameraLocalPosition => cameraLocalPosition;
        public Vector3 WatchPointLocalPosition => watchPointLocalPosition;

        public void Configure(Vector3 cameraPosition, Vector3 watchPointPosition)
        {
            cameraLocalPosition = cameraPosition;
            watchPointLocalPosition = watchPointPosition;
        }
    }
}
