using UnityEngine;

namespace Gley.CameraSystem
{
    public class AssembledBezierOrbitSegment
    {
        public Vector3 StartPosition { get; }
        public Vector3 StartControlPoint { get; }
        public Vector3 EndControlPoint { get; }
        public Vector3 EndPosition { get; }

        public AssembledBezierOrbitSegment(Vector3 startPosition, Vector3 startControlPoint, Vector3 endControlPoint, Vector3 endPosition)
        {
            StartPosition = startPosition;
            StartControlPoint = startControlPoint;
            EndControlPoint = endControlPoint;
            EndPosition = endPosition;
        }

        public Vector3 EvaluatePosition(float segmentT)
        {
            float inverseT = 1f - segmentT;
            float startWeight = inverseT * inverseT * inverseT;
            float startControlWeight = 3f * inverseT * inverseT * segmentT;
            float endControlWeight = 3f * inverseT * segmentT * segmentT;
            float endWeight = segmentT * segmentT * segmentT;
            return startWeight * StartPosition + startControlWeight * StartControlPoint + endControlWeight * EndControlPoint + endWeight * EndPosition;
        }

        public AssembledBezierOrbitSegment CreateReversedSegment()
        {
            return new AssembledBezierOrbitSegment(EndPosition, EndControlPoint, StartControlPoint, StartPosition);
        }
    }
}
