using UnityEngine;

namespace Gley.CameraSystem.Editor
{
    public interface IOrbitDistanceSampler
    {
        float Length { get; }
        bool CanSample { get; }

        Vector3 EvaluateBodyLocalPosition(float distance);
    }
}
