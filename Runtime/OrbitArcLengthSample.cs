using UnityEngine;

namespace Gley.CameraSystem
{
    internal readonly struct OrbitArcLengthSample
    {
        public Vector3 Position { get; }
        public float Distance { get; }
        public int SegmentIndex { get; }
        public float SegmentT { get; }

        public OrbitArcLengthSample(Vector3 position, float distance, int segmentIndex, float segmentT)
        {
            Position = position;
            Distance = distance;
            SegmentIndex = segmentIndex;
            SegmentT = segmentT;
        }
    }
}
