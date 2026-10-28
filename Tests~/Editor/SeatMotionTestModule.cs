using UnityEngine;

namespace Gley.CameraSystem.Tests.EditMode
{
    internal class SeatMotionTestModule : ISeatModule
    {
        private Vector3 offset;

        public bool IsEnabled => true;

        public SeatMotionTestModule(Vector3 value)
        {
            offset = value;
        }

        public Vector3 UpdateSeatModule(SeatMotionSignals signals, float deltaTime)
        {
            return offset;
        }

        public void Configure(Vector3 value)
        {
            offset = value;
        }
    }
}
