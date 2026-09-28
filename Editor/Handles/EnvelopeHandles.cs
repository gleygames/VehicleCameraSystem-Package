using UnityEditor;
using UnityEngine;

namespace Gley.CameraSystem.Editor
{
    public class EnvelopeHandles
    {
        private const float EnvelopeWidth = 1.5f;
        private const float RangeHandleSize = 0.08f;
        private const float PoseHandleSize = 0.09f;
        private const float MinimumDirectionLength = 0.0001f;

        private readonly Color envelopeColor = new Color(0.45f, 0.6f, 1f, 0.8f);
        private readonly Color heightBandColor = new Color(0.45f, 0.6f, 1f, 0.35f);
        private readonly Color zoomHandleColor = new Color(0.4f, 0.75f, 1f);
        private readonly Color heightHandleColor = new Color(0.4f, 1f, 0.6f);
        private readonly Color defaultPoseColor = new Color(1f, 0.85f, 0.2f);
        private readonly Color frontPoseColor = new Color(1f, 0.45f, 0.2f);
        private readonly OrbitEditOperations operations;
        private readonly HandleDragUndo dragUndo;
        private readonly CameraGizmoDrawer cameraGizmo = new CameraGizmoDrawer();

        public EnvelopeHandles(OrbitEditOperations editOperations, HandleDragUndo handleDragUndo)
        {
            operations = editOperations;
            dragUndo = handleDragUndo;
        }

        public void DrawEnvelope(OrbitPreviewCache cache)
        {
            if (!cache.IsCurveValid || Event.current.type != EventType.Repaint)
            {
                return;
            }

            int pointCount = cache.SampleCount + 1;
            Handles.color = envelopeColor;
            Handles.DrawAAPolyLine(EnvelopeWidth, pointCount, cache.InnerLowPoints);
            Handles.DrawAAPolyLine(EnvelopeWidth, pointCount, cache.InnerHighPoints);
            Handles.DrawAAPolyLine(EnvelopeWidth, pointCount, cache.OuterLowPoints);
            Handles.DrawAAPolyLine(EnvelopeWidth, pointCount, cache.OuterHighPoints);
            Handles.color = heightBandColor;
            Handles.DrawLines(cache.HeightBandLines);
        }

        public void DrawRangeHandles(VehicleOrbit orbit, OrbitPreviewCache cache)
        {
            Vector3 inward = cache.ReferenceInwardNormal;
            if (!cache.IsCurveValid || inward.sqrMagnitude < MinimumDirectionLength)
            {
                return;
            }

            Vector3 origin = cache.ReferencePoint;
            Vector3 up = cache.PlaneNormal;

            if (Event.current.type == EventType.Repaint)
            {
                Handles.color = zoomHandleColor;
                Handles.DrawLine(origin + inward * orbit.MinimumZoomOffset, origin + inward * orbit.MaximumZoomOffset);
                Handles.Label(origin + inward * orbit.MaximumZoomOffset, $"Inward zoom {orbit.MaximumZoomOffset:+0.00;-0.00} m");
                Handles.Label(origin + inward * orbit.MinimumZoomOffset, $"Outward zoom {orbit.MinimumZoomOffset:+0.00;-0.00} m");
                Handles.color = heightHandleColor;
                Handles.DrawLine(origin + up * orbit.MinimumHeightOffset, origin + up * orbit.MaximumHeightOffset);
                Handles.Label(origin + up * orbit.MaximumHeightOffset, $"Maximum height {orbit.MaximumHeightOffset:+0.00;-0.00} m");
                Handles.Label(origin + up * orbit.MinimumHeightOffset, $"Minimum height {orbit.MinimumHeightOffset:+0.00;-0.00} m");
            }

            Handles.color = zoomHandleColor;
            if (DrawLinearSlider(origin, inward, orbit.MaximumZoomOffset, RangeHandleSize, out float maximumZoom))
            {
                ApplyRanges(orbit, orbit.MinimumHeightOffset, orbit.MaximumHeightOffset, orbit.MinimumZoomOffset, Mathf.Max(maximumZoom, orbit.MinimumZoomOffset), "Change Zoom Range");
                return;
            }

            if (DrawLinearSlider(origin, inward, orbit.MinimumZoomOffset, RangeHandleSize, out float minimumZoom))
            {
                ApplyRanges(orbit, orbit.MinimumHeightOffset, orbit.MaximumHeightOffset, Mathf.Min(minimumZoom, orbit.MaximumZoomOffset), orbit.MaximumZoomOffset, "Change Zoom Range");
                return;
            }

            Handles.color = heightHandleColor;
            if (DrawLinearSlider(origin, up, orbit.MaximumHeightOffset, RangeHandleSize, out float maximumHeight))
            {
                ApplyRanges(orbit, orbit.MinimumHeightOffset, Mathf.Max(maximumHeight, orbit.MinimumHeightOffset), orbit.MinimumZoomOffset, orbit.MaximumZoomOffset, "Change Height Range");
                return;
            }

            if (DrawLinearSlider(origin, up, orbit.MinimumHeightOffset, RangeHandleSize, out float minimumHeight))
            {
                ApplyRanges(orbit, Mathf.Min(minimumHeight, orbit.MaximumHeightOffset), orbit.MaximumHeightOffset, orbit.MinimumZoomOffset, orbit.MaximumZoomOffset, "Change Height Range");
            }
        }

        public void DrawDefaultPoseHandles(VehicleViewEntry view, VehicleOrbit orbit, OrbitPreviewCache cache)
        {
            if (!cache.IsCurveValid)
            {
                return;
            }

            DrawPose(view, orbit, cache, view.DefaultPose, false, defaultPoseColor, "Default pose");

            if (view.Preset != null && view.Preset.ViewType == CameraViewType.Driving)
            {
                DrawPose(view, orbit, cache, view.FrontDefaultPose, true, frontPoseColor, "Front default pose (reverse)");
            }
        }

        private bool DrawLinearSlider(Vector3 origin, Vector3 direction, float value, float handleScale, out float newValue)
        {
            newValue = value;
            Vector3 position = origin + direction * value;
            float size = HandleUtility.GetHandleSize(position) * handleScale;

            EditorGUI.BeginChangeCheck();
            Vector3 newPosition = Handles.Slider(position, direction, size, Handles.CubeHandleCap, 0f);
            if (!EditorGUI.EndChangeCheck())
            {
                return false;
            }

            newValue = Vector3.Dot(newPosition - origin, direction);
            return true;
        }

        private void ApplyRanges(VehicleOrbit orbit, float minimumHeight, float maximumHeight, float minimumZoom, float maximumZoom, string undoName)
        {
            dragUndo.RecordHandleChange(undoName);
            orbit.ConfigureOffsetRanges(minimumHeight, maximumHeight, minimumZoom, maximumZoom);
            dragUndo.CompleteHandleChange();
        }

        private void DrawPose(VehicleViewEntry view, VehicleOrbit orbit, OrbitPreviewCache cache, OrbitPose pose, bool isFrontPose, Color color, string label)
        {
            ClosedBezierOrbit builtOrbit = cache.BuiltOrbit;
            if (!builtOrbit.Bearing.TryGetDistanceAtBearing(pose.Bearing, 0f, out float distance))
            {
                return;
            }

            Vector3 basePoint = cache.EvaluateBodyLocalPosition(distance);
            Vector3 inward = cache.EvaluateBodyLocalInwardNormal(distance);
            Vector3 up = cache.PlaneNormal;
            Vector3 heightPoint = basePoint + up * pose.HeightOffset;
            Vector3 cameraPosition = heightPoint + inward * pose.ZoomOffset;

            if (Event.current.type == EventType.Repaint)
            {
                cameraGizmo.DrawCamera(cameraPosition, builtOrbit.EvaluateBodyLocalWatchPoint(distance), up, color);
                Handles.color = color;
                Handles.DrawDottedLine(basePoint, heightPoint, 3f);
                Handles.DrawDottedLine(heightPoint, cameraPosition, 3f);
                Handles.Label(cameraPosition, $"{label}: bearing {pose.Bearing:0.#} degrees, zoom {pose.ZoomOffset:0.##} m, height {pose.HeightOffset:0.##} m");
            }

            Handles.color = color;
            if (DrawBearingSlider(orbit, cache, basePoint, out float bearing))
            {
                ApplyPose(view, isFrontPose, new OrbitPose(bearing, pose.ZoomOffset, pose.HeightOffset));
                return;
            }

            if (inward.sqrMagnitude >= MinimumDirectionLength && DrawLinearSlider(heightPoint, inward, pose.ZoomOffset, PoseHandleSize, out float zoom))
            {
                zoom = Mathf.Clamp(zoom, orbit.MinimumZoomOffset, orbit.MaximumZoomOffset);
                ApplyPose(view, isFrontPose, new OrbitPose(pose.Bearing, zoom, pose.HeightOffset));
                return;
            }

            Vector3 zoomPoint = basePoint + inward * pose.ZoomOffset;
            if (DrawLinearSlider(zoomPoint, up, pose.HeightOffset, PoseHandleSize, out float height))
            {
                height = Mathf.Clamp(height, orbit.MinimumHeightOffset, orbit.MaximumHeightOffset);
                ApplyPose(view, isFrontPose, new OrbitPose(pose.Bearing, pose.ZoomOffset, height));
            }
        }

        private bool DrawBearingSlider(VehicleOrbit orbit, OrbitPreviewCache cache, Vector3 basePoint, out float bearing)
        {
            bearing = 0f;
            Vector3 firstAxis = orbit.OrientationAdjustment * Vector3.right;
            Vector3 secondAxis = orbit.OrientationAdjustment * Vector3.forward;
            float size = HandleUtility.GetHandleSize(basePoint) * PoseHandleSize;
            int controlId = GUIUtility.GetControlID(FocusType.Passive);

            EditorGUI.BeginChangeCheck();
            Vector3 newPoint = Handles.Slider2D(controlId, basePoint, Vector3.zero, cache.PlaneNormal, firstAxis, secondAxis, size, Handles.SphereHandleCap, Vector2.zero, false);
            if (!EditorGUI.EndChangeCheck())
            {
                return false;
            }

            if (!operations.TryFindNearestNormalizedPosition(cache, newPoint, out float normalizedPosition))
            {
                return false;
            }

            bearing = cache.BuiltOrbit.Bearing.BearingOfLocalPoint(cache.EvaluateNormalizedPoint(normalizedPosition));
            return true;
        }

        private void ApplyPose(VehicleViewEntry view, bool isFrontPose, OrbitPose pose)
        {
            if (isFrontPose)
            {
                dragUndo.RecordHandleChange("Change View Front Default Pose");
                view.ConfigureFrontDefaultPose(pose);
            }
            else
            {
                dragUndo.RecordHandleChange("Change View Default Pose");
                view.ConfigureDefaultPose(pose);
            }

            dragUndo.CompleteHandleChange();
        }
    }
}
