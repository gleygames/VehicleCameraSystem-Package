using UnityEditor;
using UnityEngine;

namespace Gley.CameraSystem.Editor
{
    public class RemovableSectionHandles
    {
        private const float SectionWidth = 7f;
        private const float SliderSize = 0.06f;

        private readonly Color frontSectionColor = new Color(1f, 0.55f, 0.1f);
        private readonly Color rearSectionColor = new Color(1f, 0.3f, 0.75f);
        private readonly OrbitEditOperations operations;
        private readonly HandleDragUndo dragUndo;

        private Vector3[] sectionPoints = new Vector3[0];

        public RemovableSectionHandles(OrbitEditOperations editOperations, HandleDragUndo handleDragUndo)
        {
            operations = editOperations;
            dragUndo = handleDragUndo;
        }

        public void DrawSectionHandles(VehicleOrbit orbit, OrbitPreviewCache cache)
        {
            if (!cache.IsCurveValid)
            {
                return;
            }

            DrawSection(orbit, cache, orbit.FrontRemovableSection, "Front removable section", frontSectionColor);
            DrawSection(orbit, cache, orbit.RearRemovableSection, "Rear removable section", rearSectionColor);
        }

        private void DrawSection(VehicleOrbit orbit, OrbitPreviewCache cache, OrbitRemovableSection section, string label, Color color)
        {
            if (section == null)
            {
                return;
            }

            float start = section.NormalizedStartPosition;
            float end = section.NormalizedEndPosition;
            float sectionLength = end - start;
            if (sectionLength < 0f)
            {
                sectionLength += 1f;
            }

            if (Event.current.type == EventType.Repaint)
            {
                DrawSectionArc(cache, start, sectionLength, color);
                Handles.Label(cache.EvaluateCachedPoint(start + sectionLength * 0.5f), label, EditorStyles.boldLabel);
            }

            Handles.color = color;
            if (DrawEndSlider(orbit, cache, start, out float newStart))
            {
                dragUndo.RecordHandleChange("Move Removable Section");
                section.Configure(newStart, section.NormalizedEndPosition);
                dragUndo.CompleteHandleChange();
            }

            Handles.color = color;
            if (DrawEndSlider(orbit, cache, end, out float newEnd))
            {
                dragUndo.RecordHandleChange("Move Removable Section");
                section.Configure(section.NormalizedStartPosition, newEnd);
                dragUndo.CompleteHandleChange();
            }
        }

        private void DrawSectionArc(OrbitPreviewCache cache, float start, float sectionLength, Color color)
        {
            int stepCount = Mathf.Max(2, Mathf.CeilToInt(sectionLength * cache.SampleCount));
            if (sectionPoints.Length < stepCount + 1)
            {
                sectionPoints = new Vector3[stepCount + 1];
            }

            for (int stepIndex = 0; stepIndex <= stepCount; stepIndex++)
            {
                sectionPoints[stepIndex] = cache.EvaluateCachedPoint(start + sectionLength * stepIndex / stepCount);
            }

            Handles.color = color;
            Handles.DrawAAPolyLine(SectionWidth, stepCount + 1, sectionPoints);
        }

        private bool DrawEndSlider(VehicleOrbit orbit, OrbitPreviewCache cache, float normalizedPosition, out float newPosition)
        {
            newPosition = normalizedPosition;
            Vector3 position = cache.EvaluateNormalizedPoint(normalizedPosition);
            Vector3 firstAxis = orbit.OrientationAdjustment * Vector3.right;
            Vector3 secondAxis = orbit.OrientationAdjustment * Vector3.forward;
            float size = HandleUtility.GetHandleSize(position) * SliderSize;
            int controlId = GUIUtility.GetControlID(FocusType.Passive);

            EditorGUI.BeginChangeCheck();
            Vector3 newPoint = Handles.Slider2D(controlId, position, Vector3.zero, cache.PlaneNormal, firstAxis, secondAxis, size, Handles.CubeHandleCap, Vector2.zero, false);
            if (!EditorGUI.EndChangeCheck())
            {
                return false;
            }

            return operations.TryFindNearestNormalizedPosition(cache, newPoint, out newPosition);
        }
    }
}
