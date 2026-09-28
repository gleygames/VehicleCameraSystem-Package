using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Gley.CameraSystem.Editor
{
    public class ConnectionPreviewDrawer
    {
        private const float OrbitWidth = 3f;
        private const float BodyCurveWidth = 1.5f;
        private const float ConnectorWidth = 5f;
        private const float PivotSize = 0.08f;
        private const float WatchPointSize = 0.04f;
        private const float DottedLineSpace = 4f;
        private const int FramingPositionCount = 8;

        private readonly List<Renderer> renderers = new List<Renderer>();
        private readonly Color orbitColor = new Color(0.3f, 0.85f, 1f);
        private readonly Color rootCurveColor = new Color(0.6f, 0.6f, 0.6f, 0.8f);
        private readonly Color partnerCurveColor = new Color(0.85f, 0.85f, 0.4f, 0.8f);
        private readonly Color boundsColor = new Color(0.85f, 0.85f, 0.4f, 0.5f);
        private readonly Color generatedConnectorColor = new Color(1f, 0.9f, 0.2f);
        private readonly Color overriddenConnectorColor = new Color(1f, 0.5f, 0.1f);
        private readonly Color pivotColor = Color.white;
        private readonly Color rootAimColor = new Color(0.4f, 1f, 0.4f);
        private readonly Color ownerAimColor = new Color(1f, 0.4f, 1f);
        private readonly CameraGizmoDrawer cameraDrawer = new CameraGizmoDrawer();

        public void DrawPreview(ConnectionChainPreview preview, Matrix4x4 previewMatrix)
        {
            if (Event.current.type != EventType.Repaint || preview.BodyCount == 0 || preview.BodyMatrices.Count != preview.BodyCount)
            {
                return;
            }

            DrawBodies(preview, previewMatrix);
            Handles.matrix = previewMatrix;
            DrawOrbit(preview);
            DrawConnectors(preview);
            DrawPivots(preview);
            DrawFraming(preview);
        }

        private void DrawBodies(ConnectionChainPreview preview, Matrix4x4 previewMatrix)
        {
            for (int bodyIndex = 0; bodyIndex < preview.BodyCount; bodyIndex++)
            {
                Matrix4x4 bodyMatrix = previewMatrix * preview.BodyMatrices[bodyIndex];
                Vector3[] curvePoints = preview.GetBodyCurvePoints(bodyIndex);
                bool isRoot = bodyIndex == preview.RootIndex;
                if (curvePoints.Length > 1)
                {
                    Handles.matrix = bodyMatrix;
                    Handles.color = partnerCurveColor;
                    if (isRoot)
                    {
                        Handles.color = rootCurveColor;
                    }

                    Handles.DrawAAPolyLine(BodyCurveWidth, curvePoints);
                }

                if (!isRoot)
                {
                    DrawModelBounds(preview.Models[bodyIndex], bodyMatrix);
                }
            }
        }

        private void DrawModelBounds(Transform model, Matrix4x4 bodyMatrix)
        {
            if (model == null)
            {
                return;
            }

            Matrix4x4 modelToPreview = bodyMatrix * model.worldToLocalMatrix;
            model.GetComponentsInChildren(false, renderers);
            Handles.color = boundsColor;
            for (int index = 0; index < renderers.Count; index++)
            {
                Renderer bodyRenderer = renderers[index];
                if (!(bodyRenderer is MeshRenderer) && !(bodyRenderer is SkinnedMeshRenderer))
                {
                    continue;
                }

                Bounds localBounds = bodyRenderer.localBounds;
                Handles.matrix = modelToPreview * bodyRenderer.transform.localToWorldMatrix;
                Handles.DrawWireCube(localBounds.center, localBounds.size);
            }

            renderers.Clear();
        }

        private void DrawOrbit(ConnectionChainPreview preview)
        {
            if (!preview.IsOrbitValid || preview.OrbitPointCount < 2)
            {
                Vector3 labelPosition = preview.StraightBodyMatrices[preview.RootIndex].MultiplyPoint3x4(Vector3.zero);
                Handles.Label(labelPosition, $"Chain orbit invalid: {preview.LayoutResult}", EditorStyles.boldLabel);
                return;
            }

            Handles.color = orbitColor;
            Handles.DrawAAPolyLine(OrbitWidth, preview.OrbitPointCount, preview.OrbitPoints);
        }

        private void DrawConnectors(ConnectionChainPreview preview)
        {
            if (!preview.IsMerged || preview.Layout == null)
            {
                return;
            }

            IReadOnlyList<GeneratedOrbitConnectorPair> pairs = preview.Layout.ConnectorPairs;
            for (int jointIndex = 0; jointIndex < pairs.Count; jointIndex++)
            {
                Color connectorColor = generatedConnectorColor;
                if (jointIndex < preview.JointOverrides.Count && preview.JointOverrides[jointIndex] != null)
                {
                    connectorColor = overriddenConnectorColor;
                }

                DrawConnector(pairs[jointIndex].LeftConnector, connectorColor);
                DrawConnector(pairs[jointIndex].RightConnector, connectorColor);
            }
        }

        private void DrawConnector(GeneratedOrbitConnector connector, Color connectorColor)
        {
            Handles.DrawBezier(connector.StartPosition, connector.EndPosition, connector.StartControlPoint, connector.EndControlPoint, connectorColor, null, ConnectorWidth);
        }

        private void DrawPivots(ConnectionChainPreview preview)
        {
            Handles.color = pivotColor;
            IReadOnlyList<Vector3> pivots = preview.ArticulatedJointPivots;
            for (int index = 0; index < pivots.Count; index++)
            {
                Vector3 pivot = pivots[index];
                Handles.DotHandleCap(0, pivot, Quaternion.identity, HandleUtility.GetHandleSize(pivot) * PivotSize, EventType.Repaint);
            }
        }

        private void DrawFraming(ConnectionChainPreview preview)
        {
            ChainOrbit orbit = preview.Orbit;
            if (!preview.IsOrbitValid || !orbit.HasValidWatchMarkers)
            {
                return;
            }

            for (int index = 0; index < FramingPositionCount; index++)
            {
                float distance = orbit.Length * index / FramingPositionCount;
                Vector3 cameraPosition = orbit.EvaluateRootLocalPosition(distance);
                cameraDrawer.DrawCamera(cameraPosition, preview.EvaluateRootWatchPoint(distance), preview.PlaneNormal, rootAimColor);

                if (preview.HasOwnerWatchPoints)
                {
                    Vector3 ownerWatchPoint = preview.EvaluateOwnerWatchPoint(distance);
                    Handles.color = ownerAimColor;
                    Handles.DrawDottedLine(cameraPosition, ownerWatchPoint, DottedLineSpace);
                    Handles.DotHandleCap(0, ownerWatchPoint, Quaternion.identity, HandleUtility.GetHandleSize(ownerWatchPoint) * WatchPointSize, EventType.Repaint);
                }
            }
        }
    }
}
