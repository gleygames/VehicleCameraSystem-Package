using UnityEditor;
using UnityEngine;

namespace Gley.CameraSystem.Editor
{
    public class CameraGizmoDrawer
    {
        private const float GizmoScale = 0.6f;
        private const float FrustumHalfWidth = 0.4f;
        private const float FrustumHalfHeight = 0.25f;
        private const float MinimumDirectionLength = 0.0001f;

        private readonly Vector3[] lines = new Vector3[16];

        public void DrawCamera(Vector3 position, Vector3 watchPoint, Vector3 up, Color color)
        {
            Vector3 forward = watchPoint - position;
            if (forward.sqrMagnitude < MinimumDirectionLength)
            {
                forward = Vector3.forward;
            }

            Vector3 lookUp = up;
            if (Vector3.Cross(forward, lookUp).sqrMagnitude < MinimumDirectionLength)
            {
                lookUp = Vector3.forward;
            }

            Quaternion rotation = Quaternion.LookRotation(forward, lookUp);
            float size = HandleUtility.GetHandleSize(position) * GizmoScale;
            Vector3 centre = position + rotation * Vector3.forward * size;
            Vector3 right = rotation * Vector3.right * (size * FrustumHalfWidth);
            Vector3 top = rotation * Vector3.up * (size * FrustumHalfHeight);
            Vector3 topLeft = centre - right + top;
            Vector3 topRight = centre + right + top;
            Vector3 bottomLeft = centre - right - top;
            Vector3 bottomRight = centre + right - top;

            lines[0] = position;
            lines[1] = topLeft;
            lines[2] = position;
            lines[3] = topRight;
            lines[4] = position;
            lines[5] = bottomLeft;
            lines[6] = position;
            lines[7] = bottomRight;
            lines[8] = topLeft;
            lines[9] = topRight;
            lines[10] = topRight;
            lines[11] = bottomRight;
            lines[12] = bottomRight;
            lines[13] = bottomLeft;
            lines[14] = bottomLeft;
            lines[15] = topLeft;

            Color previousColor = Handles.color;
            Handles.color = color;
            Handles.DrawLines(lines);
            Handles.DrawDottedLine(position, watchPoint, 4f);
            Handles.color = previousColor;
        }
    }
}
