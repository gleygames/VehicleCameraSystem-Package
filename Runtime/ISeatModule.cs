using UnityEngine;

namespace Gley.CameraSystem
{
    public interface ISeatModule
    {
        bool IsEnabled { get; }

        Vector3 UpdateSeatModule(SeatMotionSignals signals, float deltaTime);
    }
}
