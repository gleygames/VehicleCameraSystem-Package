using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Gley.CameraSystem.Editor
{
    public class SeatHandles
    {
        private const int AxisCount = 3;

        private readonly Color eyeColor = new Color(1f, 0.85f, 0.2f);
        private readonly Color envelopeColor = new Color(0.4f, 1f, 0.6f);
        private readonly Color fixedCameraColor = new Color(0.55f, 0.9f, 1f);
        private readonly BoxBoundsHandle envelopeHandle = new BoxBoundsHandle();
        private readonly HandleDragUndo dragUndo;
        private readonly CameraGizmoDrawer cameraGizmo = new CameraGizmoDrawer();

        public SeatHandles(HandleDragUndo handleDragUndo)
        {
            dragUndo = handleDragUndo;
        }

        public void DrawSeatHandles(VehicleProfile profile)
        {
            SeatSettings seat = profile.Seat;
            if (seat == null)
            {
                return;
            }

            Vector3 eye = seat.EyeLocalPosition;
            Vector3 halfExtents = seat.EnvelopeHalfExtents;

            if (Event.current.type == EventType.Repaint)
            {
                cameraGizmo.DrawCamera(eye, eye + Vector3.forward, Vector3.up, eyeColor);
                Handles.color = eyeColor;
                Handles.Label(eye, "Seat eye");
            }

            EditorGUI.BeginChangeCheck();
            Vector3 newEye = Handles.PositionHandle(eye, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                dragUndo.RecordHandleChange("Move Seat Eye");
                seat.Configure(newEye, halfExtents);
                dragUndo.CompleteHandleChange();
                return;
            }

            envelopeHandle.center = eye;
            envelopeHandle.size = halfExtents * 2f;
            envelopeHandle.handleColor = envelopeColor;
            envelopeHandle.wireframeColor = envelopeColor;

            EditorGUI.BeginChangeCheck();
            envelopeHandle.DrawHandle();
            if (EditorGUI.EndChangeCheck())
            {
                Vector3 newHalfExtents = ResolveHalfExtents(eye, halfExtents, envelopeHandle.center, envelopeHandle.size);
                dragUndo.RecordHandleChange("Change Seat Envelope");
                seat.Configure(eye, newHalfExtents);
                dragUndo.CompleteHandleChange();
            }
        }

        public void DrawFixedPoseHandles(VehicleProfile profile)
        {
            FixedViewPose pose = profile.FixedViewPose;
            if (pose == null)
            {
                return;
            }

            Vector3 cameraPosition = pose.CameraLocalPosition;
            Vector3 watchPoint = pose.WatchPointLocalPosition;

            if (Event.current.type == EventType.Repaint)
            {
                cameraGizmo.DrawCamera(cameraPosition, watchPoint, Vector3.up, fixedCameraColor);
                Handles.color = fixedCameraColor;
                Handles.Label(cameraPosition, "Fixed camera");
                Handles.Label(watchPoint, "Fixed watch point");
            }

            EditorGUI.BeginChangeCheck();
            Vector3 newCameraPosition = Handles.PositionHandle(cameraPosition, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                dragUndo.RecordHandleChange("Move Fixed Camera");
                pose.Configure(newCameraPosition, watchPoint);
                dragUndo.CompleteHandleChange();
                return;
            }

            EditorGUI.BeginChangeCheck();
            Vector3 newWatchPoint = Handles.PositionHandle(watchPoint, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                dragUndo.RecordHandleChange("Move Fixed Watch Point");
                pose.Configure(cameraPosition, newWatchPoint);
                dragUndo.CompleteHandleChange();
            }
        }

        private Vector3 ResolveHalfExtents(Vector3 eye, Vector3 halfExtents, Vector3 newCentre, Vector3 newSize)
        {
            Vector3 newMinimum = newCentre - newSize * 0.5f;
            Vector3 newMaximum = newCentre + newSize * 0.5f;
            Vector3 oldMinimum = eye - halfExtents;
            Vector3 oldMaximum = eye + halfExtents;
            Vector3 result = halfExtents;

            for (int axis = 0; axis < AxisCount; axis++)
            {
                float maximumChange = Mathf.Abs(newMaximum[axis] - oldMaximum[axis]);
                float minimumChange = Mathf.Abs(newMinimum[axis] - oldMinimum[axis]);
                if (maximumChange > minimumChange)
                {
                    result[axis] = Mathf.Max(0f, newMaximum[axis] - eye[axis]);
                }
                else if (minimumChange > 0f)
                {
                    result[axis] = Mathf.Max(0f, eye[axis] - newMinimum[axis]);
                }
            }

            return result;
        }
    }
}
