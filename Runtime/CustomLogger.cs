using System.Diagnostics;
using UnityEngine;
using UnityDebug = UnityEngine.Debug;

namespace Gley.CameraSystem
{
    internal static class CustomLogger
    {
        [Conditional("DEBUG"), Conditional("UNITY_EDITOR")]
        public static void LogWarning(object message, Object context = null)
        {
            UnityDebug.LogWarning(message, context);
        }
    }
}
