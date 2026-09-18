using UnityEngine;

namespace Gley.CameraSystem
{
    public class GeneratedOrbitConnector
    {
        public Vector3 StartPosition { get; }
        public Vector3 StartControlPoint { get; }
        public Vector3 EndControlPoint { get; }
        public Vector3 EndPosition { get; }

        public GeneratedOrbitConnector(Vector3 startPosition, Vector3 startControlPoint, Vector3 endControlPoint, Vector3 endPosition)
        {
            StartPosition = startPosition;
            StartControlPoint = startControlPoint;
            EndControlPoint = endControlPoint;
            EndPosition = endPosition;
        }
    }
}
