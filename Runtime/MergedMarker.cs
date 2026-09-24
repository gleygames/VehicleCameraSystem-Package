using UnityEngine;

namespace Gley.CameraSystem
{
    public readonly struct MergedMarker
    {
        public Transform OwnerBody { get; }
        public Vector3 OwnerLocalWatchPoint { get; }
        public Vector3 RootLocalWatchPoint { get; }
        public string Name { get; }
        public float OrbitDistance { get; }
        public float NormalizedProgress { get; }
        public int ChainIndex { get; }
        public int MarkerId { get; }
        public bool IsPointOfInterest { get; }

        public MergedMarker(Transform ownerBody, Vector3 ownerLocalWatchPoint, Vector3 rootLocalWatchPoint, string name, float orbitDistance, float normalizedProgress, int chainIndex, int markerId, bool isPointOfInterest)
        {
            OwnerBody = ownerBody;
            OwnerLocalWatchPoint = ownerLocalWatchPoint;
            RootLocalWatchPoint = rootLocalWatchPoint;
            Name = name;
            OrbitDistance = orbitDistance;
            NormalizedProgress = normalizedProgress;
            ChainIndex = chainIndex;
            MarkerId = markerId;
            IsPointOfInterest = isPointOfInterest;
        }
    }
}
