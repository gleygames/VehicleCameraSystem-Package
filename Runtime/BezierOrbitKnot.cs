using System;
using UnityEngine;

namespace Gley.CameraSystem
{
    [Serializable]
    public class BezierOrbitKnot
    {
        [SerializeField] private Vector3 anchor;
        [SerializeField] private Vector3 incomingControlPoint;
        [SerializeField] private Vector3 outgoingControlPoint;

        public Vector3 Anchor => anchor;

        public Vector3 IncomingControlPoint => incomingControlPoint;

        public Vector3 OutgoingControlPoint => outgoingControlPoint;

        public void Configure(Vector3 anchorPosition, Vector3 incomingControlPointPosition, Vector3 outgoingControlPointPosition)
        {
            anchor = anchorPosition;
            incomingControlPoint = incomingControlPointPosition;
            outgoingControlPoint = outgoingControlPointPosition;
        }
    }
}
