using System;
using UnityEngine;

namespace Gley.CameraSystem
{
    [Serializable]
    public class OrbitWatchMarker
    {
        [SerializeField] private Vector3 watchPointLocalPosition;
        [SerializeField] private float normalizedOrbitPosition;

        public Vector3 WatchPointLocalPosition => watchPointLocalPosition;
        public float NormalizedOrbitPosition => normalizedOrbitPosition;

        public void Configure(float orbitPosition, Vector3 watchPointPosition)
        {
            normalizedOrbitPosition = orbitPosition;
            watchPointLocalPosition = watchPointPosition;
        }
    }
}
