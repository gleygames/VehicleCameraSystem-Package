using System;
using UnityEngine;

namespace Gley.CameraSystem
{
    [Serializable]
    public class SeatSettings
    {
        [SerializeField] private Vector3 eyeLocalPosition = Vector3.zero;
        [SerializeField] private Vector3 envelopeHalfExtents = new Vector3(0.06f, 0.04f, 0.08f);

        public Vector3 EyeLocalPosition => eyeLocalPosition;
        public Vector3 EnvelopeHalfExtents => envelopeHalfExtents;

        public void Configure(Vector3 eyePosition, Vector3 halfExtents)
        {
            eyeLocalPosition = eyePosition;
            envelopeHalfExtents = halfExtents;
        }
    }
}
