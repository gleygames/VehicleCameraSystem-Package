using UnityEngine;

namespace Gley.CameraSystem
{
    public readonly struct SeatMotionSignals
    {
        public Vector3 LocalAcceleration { get; }
        public float Speed { get; }
        public float DeltaTime { get; }

        public SeatMotionSignals(Vector3 localAcceleration, float speed, float deltaTime)
        {
            LocalAcceleration = localAcceleration;
            Speed = speed;
            DeltaTime = deltaTime;
        }
    }
}
