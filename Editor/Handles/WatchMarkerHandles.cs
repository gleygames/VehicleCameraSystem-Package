using UnityEditor;
using UnityEngine;

namespace Gley.CameraSystem.Editor
{
    public class WatchMarkerHandles
    {
        private const float MarkerSize = 0.1f;
        private const float WatchPointSize = 0.1f;
        private const float MaximumDrawDistance = 200f;

        private readonly Color markerColor = new Color(1f, 0.9f, 0.2f);
        private readonly Color pointOfInterestColor = new Color(0.3f, 1f, 0.45f);
        private readonly Color selectedColor = Color.white;
        private readonly OrbitEditOperations operations;
        private readonly HandleDragUndo dragUndo;
        private readonly SceneHandleState state;

        public WatchMarkerHandles(OrbitEditOperations editOperations, HandleDragUndo handleDragUndo, SceneHandleState handleState)
        {
            operations = editOperations;
            dragUndo = handleDragUndo;
            state = handleState;
        }

        public void DrawMarkerHandles(VehicleOrbit orbit, OrbitPreviewCache cache, SceneView sceneView)
        {
            if (!cache.IsCurveValid || sceneView.camera == null)
            {
                return;
            }

            Vector3 cameraPosition = sceneView.camera.transform.position;
            Matrix4x4 matrix = Handles.matrix;
            float maximumDistanceSquared = MaximumDrawDistance * MaximumDrawDistance;

            for (int index = 0; index < orbit.WatchMarkers.Count; index++)
            {
                OrbitWatchMarker marker = orbit.WatchMarkers[index];
                if (marker == null)
                {
                    continue;
                }

                Vector3 markerPoint = cache.EvaluateCachedPoint(marker.NormalizedOrbitPosition);
                if ((matrix.MultiplyPoint3x4(markerPoint) - cameraPosition).sqrMagnitude > maximumDistanceSquared)
                {
                    continue;
                }

                DrawMarker(orbit, cache, marker, markerPoint);
            }
        }

        private void DrawMarker(VehicleOrbit orbit, OrbitPreviewCache cache, OrbitWatchMarker marker, Vector3 markerPoint)
        {
            bool isSelected = marker.Id == state.SelectedMarkerId;
            Color color = markerColor;
            if (marker.IsPointOfInterest)
            {
                color = pointOfInterestColor;
            }

            Vector3 watchPoint = marker.WatchPointLocalPosition;
            if (Event.current.type == EventType.Repaint)
            {
                Handles.color = color;
                Handles.DrawLine(markerPoint, watchPoint);
                if (marker.IsPointOfInterest || isSelected)
                {
                    Handles.Label(markerPoint, marker.Name, EditorStyles.boldLabel);
                }
            }

            Handles.color = color;
            if (isSelected)
            {
                Handles.color = selectedColor;
            }

            Vector3 firstAxis = orbit.OrientationAdjustment * Vector3.right;
            Vector3 secondAxis = orbit.OrientationAdjustment * Vector3.forward;
            float markerSize = HandleUtility.GetHandleSize(markerPoint) * MarkerSize;
            int controlId = GUIUtility.GetControlID(FocusType.Passive);

            EditorGUI.BeginChangeCheck();
            Vector3 newMarkerPoint = Handles.Slider2D(controlId, markerPoint, Vector3.zero, cache.PlaneNormal, firstAxis, secondAxis, markerSize, Handles.SphereHandleCap, Vector2.zero, false);
            if (GUIUtility.hotControl == controlId)
            {
                state.SelectMarker(marker.Id);
            }

            if (EditorGUI.EndChangeCheck())
            {
                dragUndo.RecordHandleChange("Move Watch Marker");
                operations.MoveMarkerAlongCurve(marker, cache, newMarkerPoint);
                dragUndo.CompleteHandleChange();
            }

            if (isSelected)
            {
                EditorGUI.BeginChangeCheck();
                Vector3 newWatchPoint = Handles.PositionHandle(watchPoint, Quaternion.identity);
                if (EditorGUI.EndChangeCheck())
                {
                    dragUndo.RecordHandleChange("Move Watch Point");
                    marker.Configure(marker.NormalizedOrbitPosition, newWatchPoint);
                    dragUndo.CompleteHandleChange();
                }

                return;
            }

            Handles.color = color;
            float watchPointSize = HandleUtility.GetHandleSize(watchPoint) * WatchPointSize;
            if (Handles.Button(watchPoint, Quaternion.identity, watchPointSize, watchPointSize, Handles.SphereHandleCap))
            {
                state.SelectMarker(marker.Id);
            }
        }
    }
}
