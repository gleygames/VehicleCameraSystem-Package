using UnityEditor;
using UnityEngine;

namespace Gley.CameraSystem.Editor
{
    public class OrbitCurveHandles
    {
        private const float CurveWidth = 3f;
        private const float AnchorSize = 0.07f;
        private const float ControlPointSize = 0.05f;
        private const float ButtonSize = 0.12f;
        private const float InsertPickDistance = 12f;
        private const float MinimumRayPlaneDot = 0.000001f;
        private const int MinimumKnotCount = 3;

        private readonly Color validCurveColor = new Color(0.3f, 0.85f, 1f);
        private readonly Color invalidCurveColor = new Color(1f, 0.2f, 0.2f);
        private readonly Color anchorColor = Color.white;
        private readonly Color selectedAnchorColor = new Color(1f, 0.85f, 0.2f);
        private readonly Color controlPointColor = new Color(0.55f, 0.9f, 1f);
        private readonly Color insertColor = new Color(0.3f, 1f, 0.3f);
        private readonly Color deleteColor = new Color(1f, 0.3f, 0.3f);
        private readonly OrbitEditOperations operations;
        private readonly HandleDragUndo dragUndo;
        private readonly SceneHandleState state;

        public OrbitCurveHandles(OrbitEditOperations editOperations, HandleDragUndo handleDragUndo, SceneHandleState handleState)
        {
            operations = editOperations;
            dragUndo = handleDragUndo;
            state = handleState;
        }

        public void DrawCurve(OrbitPreviewCache cache)
        {
            if (cache.KnotCurvePointCount < 2)
            {
                return;
            }

            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            Color curveColor = validCurveColor;
            if (!cache.IsCurveValid)
            {
                curveColor = invalidCurveColor;
            }

            Handles.color = curveColor;
            Handles.DrawAAPolyLine(CurveWidth, cache.KnotCurvePointCount, cache.KnotCurvePoints);

            if (!cache.IsCurveValid && cache.BuiltOrbit != null)
            {
                Handles.Label(cache.KnotCurvePoints[0], $"Invalid orbit: {cache.BuiltOrbit.ValidationResult}", EditorStyles.boldLabel);
            }
        }

        public bool DrawCurveHandles(VehicleOrbit orbit, OrbitPreviewCache cache, SceneView sceneView)
        {
            DrawCurve(cache);

            if (!AreKnotsUsable(orbit))
            {
                return false;
            }

            Event current = Event.current;
            bool isDragging = GUIUtility.hotControl != 0;
            if (current.shift && !isDragging)
            {
                DrawKnotDots(orbit);
                return DrawKnotInsertion(orbit);
            }

            if (EditorGUI.actionKey && !isDragging)
            {
                return DrawKnotDeletion(orbit, sceneView);
            }

            DrawKnotAnchors(orbit);
            DrawSelectedKnotTangents(orbit);
            return false;
        }

        private bool AreKnotsUsable(VehicleOrbit orbit)
        {
            if (orbit.Knots.Count < 2)
            {
                return false;
            }

            for (int index = 0; index < orbit.Knots.Count; index++)
            {
                if (orbit.Knots[index] == null)
                {
                    return false;
                }
            }

            return true;
        }

        private void DrawKnotDots(VehicleOrbit orbit)
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            Handles.color = anchorColor;
            for (int index = 0; index < orbit.Knots.Count; index++)
            {
                Vector3 position = operations.ToBodyLocalPoint(orbit, orbit.Knots[index].Anchor);
                Handles.DotHandleCap(0, position, Quaternion.identity, HandleUtility.GetHandleSize(position) * AnchorSize, EventType.Repaint);
            }
        }

        private bool DrawKnotInsertion(VehicleOrbit orbit)
        {
            Event current = Event.current;
            int controlId = GUIUtility.GetControlID(FocusType.Passive);

            if (current.type == EventType.Layout)
            {
                HandleUtility.AddDefaultControl(controlId);
                return false;
            }

            if (current.type == EventType.MouseMove)
            {
                HandleUtility.Repaint();
            }

            if (!TryFindInsertionPoint(orbit, current.mousePosition, out int segmentIndex, out float segmentT, out Vector3 insertionPoint))
            {
                return false;
            }

            if (current.type == EventType.Repaint)
            {
                Handles.color = insertColor;
                Handles.SphereHandleCap(0, insertionPoint, Quaternion.identity, HandleUtility.GetHandleSize(insertionPoint) * ButtonSize, EventType.Repaint);
                return false;
            }

            if (current.type == EventType.MouseDown && current.button == 0 && HandleUtility.nearestControl == controlId)
            {
                dragUndo.RecordHandleChange("Insert Orbit Knot");
                int newKnotIndex = operations.InsertKnot(orbit, segmentIndex, segmentT);
                dragUndo.CompleteHandleChange();
                state.SelectKnot(newKnotIndex);
                current.Use();
                return true;
            }

            return false;
        }

        private bool TryFindInsertionPoint(VehicleOrbit orbit, Vector2 mousePosition, out int segmentIndex, out float segmentT, out Vector3 insertionPoint)
        {
            segmentIndex = -1;
            segmentT = 0f;
            insertionPoint = Vector3.zero;

            Ray worldRay = HandleUtility.GUIPointToWorldRay(mousePosition);
            Matrix4x4 inverseMatrix = Handles.matrix.inverse;
            Quaternion inverseOrientation = Quaternion.Inverse(orbit.OrientationAdjustment);
            Vector3 origin = inverseOrientation * inverseMatrix.MultiplyPoint3x4(worldRay.origin) - Vector3.up * orbit.BaseHeight;
            Vector3 direction = inverseOrientation * inverseMatrix.MultiplyVector(worldRay.direction);

            if (Mathf.Abs(direction.y) < MinimumRayPlaneDot)
            {
                return false;
            }

            float rayDistance = -origin.y / direction.y;
            if (rayDistance < 0f)
            {
                return false;
            }

            Vector3 planePoint = origin + direction * rayDistance;
            operations.FindNearestSegmentParameter(orbit, planePoint, out segmentIndex, out segmentT);
            if (segmentIndex < 0)
            {
                return false;
            }

            insertionPoint = operations.ToBodyLocalPoint(orbit, operations.EvaluateSegmentPoint(orbit, segmentIndex, segmentT));
            return (HandleUtility.WorldToGUIPoint(insertionPoint) - mousePosition).magnitude <= InsertPickDistance;
        }

        private bool DrawKnotDeletion(VehicleOrbit orbit, SceneView sceneView)
        {
            Event current = Event.current;
            int controlId = GUIUtility.GetControlID(FocusType.Passive);
            if (current.type == EventType.Layout)
            {
                HandleUtility.AddDefaultControl(controlId);
            }

            if (current.type == EventType.MouseMove)
            {
                HandleUtility.Repaint();
            }

            for (int index = 0; index < orbit.Knots.Count; index++)
            {
                Vector3 position = operations.ToBodyLocalPoint(orbit, orbit.Knots[index].Anchor);
                float size = HandleUtility.GetHandleSize(position) * ButtonSize;
                int knotControlId = GUIUtility.GetControlID(FocusType.Passive);

                Handles.color = deleteColor;
                if (HandleUtility.nearestControl == knotControlId)
                {
                    Handles.color = selectedAnchorColor;
                }

                Handles.SphereHandleCap(knotControlId, position, Quaternion.identity, size, current.type);

                if (current.type != EventType.MouseDown || current.button != 0 || HandleUtility.nearestControl != knotControlId)
                {
                    continue;
                }

                current.Use();
                if (orbit.Knots.Count <= MinimumKnotCount)
                {
                    sceneView.ShowNotification(new GUIContent($"An orbit needs at least {MinimumKnotCount} knots."));
                    return false;
                }

                dragUndo.RecordHandleChange("Delete Orbit Knot");
                operations.DeleteKnot(orbit, index);
                dragUndo.CompleteHandleChange();
                state.SelectKnot(-1);
                return true;
            }

            return false;
        }

        private void DrawKnotAnchors(VehicleOrbit orbit)
        {
            Vector3 planeNormal = operations.GetBodyLocalPlaneNormal(orbit);
            Vector3 firstAxis = orbit.OrientationAdjustment * Vector3.right;
            Vector3 secondAxis = orbit.OrientationAdjustment * Vector3.forward;

            for (int index = 0; index < orbit.Knots.Count; index++)
            {
                Vector3 position = operations.ToBodyLocalPoint(orbit, orbit.Knots[index].Anchor);
                float size = HandleUtility.GetHandleSize(position) * AnchorSize;
                int controlId = GUIUtility.GetControlID(FocusType.Passive);

                Handles.color = anchorColor;
                if (index == state.SelectedKnotIndex)
                {
                    Handles.color = selectedAnchorColor;
                }

                EditorGUI.BeginChangeCheck();
                Vector3 newPosition = Handles.Slider2D(controlId, position, Vector3.zero, planeNormal, firstAxis, secondAxis, size, Handles.DotHandleCap, Vector2.zero, false);
                if (GUIUtility.hotControl == controlId)
                {
                    state.SelectKnot(index);
                }

                if (EditorGUI.EndChangeCheck())
                {
                    dragUndo.RecordHandleChange("Move Orbit Knot");
                    operations.MoveKnotAnchor(orbit, index, operations.ToOrbitLocalPoint(orbit, newPosition));
                    dragUndo.CompleteHandleChange();
                }
            }
        }

        private void DrawSelectedKnotTangents(VehicleOrbit orbit)
        {
            int knotIndex = state.SelectedKnotIndex;
            if (knotIndex < 0 || knotIndex >= orbit.Knots.Count)
            {
                return;
            }

            BezierOrbitKnot knot = orbit.Knots[knotIndex];
            Vector3 anchor = operations.ToBodyLocalPoint(orbit, knot.Anchor);
            DrawTangent(orbit, knotIndex, anchor, operations.ToBodyLocalPoint(orbit, knot.IncomingControlPoint), false);
            DrawTangent(orbit, knotIndex, anchor, operations.ToBodyLocalPoint(orbit, knot.OutgoingControlPoint), true);
        }

        private void DrawTangent(VehicleOrbit orbit, int knotIndex, Vector3 anchor, Vector3 controlPoint, bool isOutgoing)
        {
            Handles.color = controlPointColor;
            Handles.DrawLine(anchor, controlPoint);

            Vector3 planeNormal = operations.GetBodyLocalPlaneNormal(orbit);
            Vector3 firstAxis = orbit.OrientationAdjustment * Vector3.right;
            Vector3 secondAxis = orbit.OrientationAdjustment * Vector3.forward;
            float size = HandleUtility.GetHandleSize(controlPoint) * ControlPointSize;
            int controlId = GUIUtility.GetControlID(FocusType.Passive);

            EditorGUI.BeginChangeCheck();
            Vector3 newPosition = Handles.Slider2D(controlId, controlPoint, Vector3.zero, planeNormal, firstAxis, secondAxis, size, Handles.RectangleHandleCap, Vector2.zero, false);
            if (EditorGUI.EndChangeCheck())
            {
                dragUndo.RecordHandleChange("Move Orbit Tangent");
                operations.MoveControlPoint(orbit, knotIndex, isOutgoing, operations.ToOrbitLocalPoint(orbit, newPosition), !Event.current.alt);
                dragUndo.CompleteHandleChange();
            }
        }
    }
}
