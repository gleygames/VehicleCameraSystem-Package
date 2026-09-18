using UnityEngine;

namespace Gley.CameraSystem
{
    internal class AssembledOrbitSegmentSource
    {
        public ClosedBezierOrbit SourceOrbit { get; }
        public OrbitSourceSegment SourceSegment { get; }
        public Vector3 PositionOffset { get; }
        public float SourceSegmentStartDistance { get; }
        public float SourceStartT { get; }
        public float SourceEndT { get; }

        public AssembledOrbitSegmentSource(ClosedBezierOrbit sourceOrbit, OrbitSourceSegment sourceSegment, float sourceSegmentStartDistance, float sourceStartT, float sourceEndT, Vector3 positionOffset)
        {
            SourceOrbit = sourceOrbit;
            SourceSegment = sourceSegment;
            PositionOffset = positionOffset;
            SourceSegmentStartDistance = sourceSegmentStartDistance;
            SourceStartT = sourceStartT;
            SourceEndT = sourceEndT;
        }
    }
}
