using UnityEngine;

namespace Gley.CameraSystem.Editor
{
    public class ClosedOrbitDistanceSampler : IOrbitDistanceSampler
    {
        private readonly ClosedBezierOrbit builtOrbit;

        public float Length => builtOrbit.Length;
        public bool CanSample => builtOrbit.IsClosed && builtOrbit.Length > 0f;

        public ClosedOrbitDistanceSampler(ClosedBezierOrbit orbit)
        {
            builtOrbit = orbit;
        }

        public Vector3 EvaluateBodyLocalPosition(float distance)
        {
            return builtOrbit.EvaluateBodyLocalPosition(distance);
        }
    }
}
