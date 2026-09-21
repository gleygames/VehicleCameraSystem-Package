using System;
using UnityEngine;

namespace Gley.CameraSystem
{
    [Serializable]
    public class OrbitConnectorGeometry
    {
        [SerializeField] private Vector3 startPosition;
        [SerializeField] private Vector3 startControlPoint;
        [SerializeField] private Vector3 endControlPoint;
        [SerializeField] private Vector3 endPosition;

        public Vector3 StartPosition => startPosition;
        public Vector3 StartControlPoint => startControlPoint;
        public Vector3 EndControlPoint => endControlPoint;
        public Vector3 EndPosition => endPosition;

        public GeneratedOrbitConnector CreateConnector()
        {
            return new GeneratedOrbitConnector(startPosition, startControlPoint, endControlPoint, endPosition);
        }

        public void Configure(Vector3 connectorStartPosition, Vector3 connectorStartControlPoint, Vector3 connectorEndControlPoint, Vector3 connectorEndPosition)
        {
            startPosition = connectorStartPosition;
            startControlPoint = connectorStartControlPoint;
            endControlPoint = connectorEndControlPoint;
            endPosition = connectorEndPosition;
        }
    }
}
