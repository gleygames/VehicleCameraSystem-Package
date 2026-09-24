using UnityEngine;

namespace Gley.CameraSystem
{
    public class StoredPoseResolver
    {
        public LivePose Resolve(OrbitPose stored, ChainOrbit orbit, VehicleOrbit rangeSource, float nearDistance)
        {
            float distance = nearDistance;
            if (orbit != null && orbit.IsClosed)
            {
                float resolvedDistance;
                if (orbit.Bearing.TryGetDistanceAtBearing(stored.Bearing, nearDistance, out resolvedDistance))
                {
                    distance = resolvedDistance;
                }
            }

            float zoom = stored.ZoomOffset;
            float height = stored.HeightOffset;
            if (rangeSource != null)
            {
                zoom = Mathf.Clamp(zoom, rangeSource.MinimumZoomOffset, rangeSource.MaximumZoomOffset);
                height = Mathf.Clamp(height, rangeSource.MinimumHeightOffset, rangeSource.MaximumHeightOffset);
            }

            return new LivePose(distance, zoom, height);
        }

        public LivePose CarryAdjustment(LivePose current, OrbitPose oldAuthoredDefault, OrbitPose newAuthoredDefault, VehicleOrbit newOrbit)
        {
            float zoom = newAuthoredDefault.ZoomOffset + current.ZoomOffset - oldAuthoredDefault.ZoomOffset;
            float height = newAuthoredDefault.HeightOffset + current.HeightOffset - oldAuthoredDefault.HeightOffset;
            if (newOrbit != null)
            {
                zoom = Mathf.Clamp(zoom, newOrbit.MinimumZoomOffset, newOrbit.MaximumZoomOffset);
                height = Mathf.Clamp(height, newOrbit.MinimumHeightOffset, newOrbit.MaximumHeightOffset);
            }

            return new LivePose(current.OrbitDistance, zoom, height);
        }
    }
}
