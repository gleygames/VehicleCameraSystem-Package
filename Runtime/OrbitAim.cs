using System.Collections.Generic;
using UnityEngine;

namespace Gley.CameraSystem
{
    public class OrbitAim
    {
        private const float MinimumAimDistance = 0.0001f;

        public Vector3 EvaluateWatchPoint(ChainOrbit orbit, float orbitDistance, AimFrame aimFrame, Transform rootBody, IReadOnlyList<Transform> chainBodies)
        {
            if (aimFrame == AimFrame.OwnerBody)
            {
                return orbit.EvaluateWorldWatchPointOwnerFrame(orbitDistance, chainBodies);
            }

            return rootBody.TransformPoint(orbit.EvaluateRootLocalWatchPoint(orbitDistance));
        }

        public Quaternion ComputeAim(Vector3 cameraPosition, Vector3 watchPoint, Vector3 orbitUp, float imageRollFollow, Quaternion fallbackRotation)
        {
            Vector3 watchDirection = watchPoint - cameraPosition;
            if (watchDirection.sqrMagnitude < MinimumAimDistance)
            {
                return fallbackRotation;
            }

            Vector3 up = Vector3.Slerp(Vector3.up, orbitUp.normalized, Mathf.Clamp01(imageRollFollow));
            return Quaternion.LookRotation(watchDirection, up);
        }
    }
}
