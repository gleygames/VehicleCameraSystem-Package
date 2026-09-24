using System;
using UnityEngine;

namespace Gley.CameraSystem
{
    [Serializable]
    public class CollisionSettings
    {
        [SerializeField] private LayerMask collisionMask = 1;
        [SerializeField] private float cameraRadius = 0.25f;
        [SerializeField] private float moveInHalfLife = 0.05f;
        [SerializeField] private float easeOutHalfLife = 0.3f;
        [SerializeField] private int maximumCasts = 8;
        [SerializeField] private bool enabled = true;

        public LayerMask CollisionMask => collisionMask;
        public float CameraRadius => cameraRadius;
        public float MoveInHalfLife => moveInHalfLife;
        public float EaseOutHalfLife => easeOutHalfLife;
        public int MaximumCasts => maximumCasts;
        public bool Enabled => enabled;

        public void Configure(bool collisionEnabled, float radius, int maxCasts, float moveInSmoothing, float easeOutSmoothing, LayerMask layers)
        {
            enabled = collisionEnabled;
            cameraRadius = radius;
            maximumCasts = maxCasts;
            moveInHalfLife = moveInSmoothing;
            easeOutHalfLife = easeOutSmoothing;
            collisionMask = layers;
        }
    }
}
