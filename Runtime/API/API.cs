using System.Collections.Generic;
using UnityEngine;

namespace Gley.CameraSystem
{
    public static class API
    {
        public static IReadOnlyList<CameraSystemController> GetCameraControllers()
        {
#if UNITY_6000_4_OR_NEWER
            return Object.FindObjectsByType<CameraSystemController>();
#else
            return Object.FindObjectsByType<CameraSystemController>(FindObjectsSortMode.None);
#endif
        }

        public static CameraSystemController GetCameraController(Camera camera)
        {
            if (camera == null)
            {
                return null;
            }

            IReadOnlyList<CameraSystemController> controllers = GetCameraControllers();
            for (int i = 0; i < controllers.Count; i++)
            {
                if (controllers[i].AssignedCamera == camera)
                {
                    return controllers[i];
                }
            }

            return null;
        }
    }
}
