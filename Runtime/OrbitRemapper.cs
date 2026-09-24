using UnityEngine;

namespace Gley.CameraSystem
{
    public class OrbitRemapper
    {
        public OrbitRemapResult Remap(ChainOrbit oldOrbit, float oldDistance, ChainOrbit newOrbit, out float newDistance)
        {
            newDistance = oldDistance;
            if (newOrbit == null || !newOrbit.IsClosed)
            {
                return OrbitRemapResult.InvalidNewOrbit;
            }

            Transform body;
            VehicleOrbit sourceOrbit;
            float sourceDistance;
            if (oldOrbit != null && oldOrbit.TryGetSourcePosition(oldDistance, out body, out sourceOrbit, out sourceDistance)
                && newOrbit.TryGetRetainedSourceDistance(body, sourceOrbit, sourceDistance, out newDistance))
            {
                return OrbitRemapResult.Survived;
            }

            if (newOrbit.TryGetRearFallbackDistance(out newDistance))
            {
                return OrbitRemapResult.MovedToRearFallback;
            }

            newDistance = oldDistance;
            return OrbitRemapResult.InvalidNewOrbit;
        }
    }
}
