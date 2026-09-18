using UnityEngine;

namespace Gley.CameraSystem
{
    internal readonly struct OrbitSourceSegment
    {
        public AssembledBezierOrbitSegment Segment { get; }
        public float StartDistance { get; }
        public float EndDistance { get; }

        public OrbitSourceSegment(AssembledBezierOrbitSegment segment, float startDistance, float endDistance)
        {
            Segment = segment;
            StartDistance = startDistance;
            EndDistance = endDistance;
        }
    }
}
