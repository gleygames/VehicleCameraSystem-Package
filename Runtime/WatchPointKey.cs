using UnityEngine;

namespace Gley.CameraSystem
{
    public readonly struct WatchPointKey
    {
        public Vector3 Point { get; }
        public float NormalizedProgress { get; }
        public int OwnerIndex { get; }

        public WatchPointKey(float normalizedProgress, Vector3 point, int ownerIndex)
        {
            NormalizedProgress = normalizedProgress;
            Point = point;
            OwnerIndex = ownerIndex;
        }
    }
}
