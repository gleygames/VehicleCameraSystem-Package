using UnityEngine;

namespace Gley.CameraSystem
{
    public class DistanceComposer
    {
        public float ComposeZoom(float playerZoom, float speedOffset, VehicleOrbit ranges)
        {
            return Mathf.Clamp(playerZoom - speedOffset, ranges.MinimumZoomOffset, ranges.MaximumZoomOffset);
        }
    }
}
