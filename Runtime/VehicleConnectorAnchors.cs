using System;
using UnityEngine;

namespace Gley.CameraSystem
{
    [Serializable]
    public class VehicleConnectorAnchors
    {
        [SerializeField] private Vector3 frontLocalPosition;
        [SerializeField] private Vector3 rearLocalPosition;

        public Vector3 FrontLocalPosition => frontLocalPosition;
        public Vector3 RearLocalPosition => rearLocalPosition;

        public void Configure(Vector3 frontPosition, Vector3 rearPosition)
        {
            frontLocalPosition = frontPosition;
            rearLocalPosition = rearPosition;
        }
    }
}
