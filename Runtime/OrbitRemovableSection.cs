using System;
using UnityEngine;

namespace Gley.CameraSystem
{
    [Serializable]
    public class OrbitRemovableSection
    {
        [SerializeField] private float normalizedEndPosition;
        [SerializeField] private float normalizedStartPosition;

        public float NormalizedEndPosition => normalizedEndPosition;
        public float NormalizedStartPosition => normalizedStartPosition;

        public void Configure(float startPosition, float endPosition)
        {
            normalizedStartPosition = startPosition;
            normalizedEndPosition = endPosition;
        }
    }
}
