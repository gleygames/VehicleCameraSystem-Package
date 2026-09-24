using System;
using UnityEngine;

namespace Gley.CameraSystem
{
    [Serializable]
    public class OrbitWatchMarker
    {
        [SerializeField] private Vector3 watchPointLocalPosition;
        [SerializeField] private string name;
        [SerializeField] private float normalizedOrbitPosition;
        [SerializeField] private int id;
        [SerializeField] private bool isPointOfInterest;

        public Vector3 WatchPointLocalPosition => watchPointLocalPosition;
        public string Name => name;
        public float NormalizedOrbitPosition => normalizedOrbitPosition;
        public int Id => id;
        public bool IsPointOfInterest => isPointOfInterest;

        public OrbitWatchMarker()
        {
        }

        public OrbitWatchMarker(int markerId, string markerName, float orbitPosition, Vector3 watchPointPosition, bool pointOfInterest)
        {
            id = markerId;
            name = markerName;
            normalizedOrbitPosition = orbitPosition;
            watchPointLocalPosition = watchPointPosition;
            isPointOfInterest = pointOfInterest;
        }

        public void Configure(float orbitPosition, Vector3 watchPointPosition)
        {
            normalizedOrbitPosition = orbitPosition;
            watchPointLocalPosition = watchPointPosition;
        }
    }
}
