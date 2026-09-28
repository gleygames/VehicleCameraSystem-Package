using UnityEditor;
using UnityEngine;

namespace Gley.CameraSystem.Editor
{
    public class PairOverrideAuthoring
    {
        private const float EndpointTolerance = 0.0001f;
        private const float EndpointSize = 0.06f;
        private const float ControlPointSize = 0.05f;
        private const float MinimumAxisLength = 0.0001f;

        private readonly Vector3[] tangentLine = new Vector3[2];
        private readonly Color endpointColor = Color.white;
        private readonly Color controlPointColor = new Color(1f, 0.6f, 0.2f);
        private readonly Color tangentColor = new Color(1f, 0.6f, 0.2f, 0.6f);

        public OrbitConnectorPairOverride CreateOverride(VehicleProfile frontProfile, VehicleProfile rearProfile, string orbitName)
        {
            if (!TryGetGeneratedPair(frontProfile, rearProfile, orbitName, out GeneratedOrbitConnectorPair generatedPair))
            {
                return null;
            }

            OrbitConnectorGeometry leftConnector = CreateGeometry(generatedPair.LeftConnector);
            OrbitConnectorGeometry rightConnector = CreateGeometry(generatedPair.RightConnector);
            OrbitConnectorPairOverride connectorOverride = ScriptableObject.CreateInstance<OrbitConnectorPairOverride>();
            connectorOverride.Configure(frontProfile, rearProfile, OrbitAttachmentEnd.Rear, OrbitAttachmentEnd.Front, leftConnector, rightConnector);
            return connectorOverride;
        }

        public bool TryGetGeneratedPair(VehicleProfile frontProfile, VehicleProfile rearProfile, string orbitName, out GeneratedOrbitConnectorPair pair)
        {
            pair = null;
            if (frontProfile == null || rearProfile == null)
            {
                return false;
            }

            VehicleOrbit frontOrbit = null;
            if (!string.IsNullOrEmpty(orbitName))
            {
                frontProfile.TryGetOrbit(orbitName, out frontOrbit);
            }

            if (frontOrbit == null)
            {
                frontOrbit = frontProfile.PrimaryOrbit;
            }

            VehicleProfile[] profiles = { frontProfile, rearProfile };
            ChainLayout layout = new ChainLayout(profiles, 0, frontOrbit);
            if (layout.ConnectorPairs.Count != 1)
            {
                return false;
            }

            pair = layout.ConnectorPairs[0];
            return true;
        }

        public bool MatchesGeneratedEnds(OrbitConnectorPairOverride connectorOverride, GeneratedOrbitConnectorPair generatedPair)
        {
            if (connectorOverride == null || generatedPair == null)
            {
                return false;
            }

            GeneratedOrbitConnectorPair overridePair = connectorOverride.CreateConnectorPair();
            if (overridePair == null)
            {
                return false;
            }

            return AreEndsEqual(overridePair.LeftConnector, generatedPair.LeftConnector) && AreEndsEqual(overridePair.RightConnector, generatedPair.RightConnector);
        }

        public OrbitConnectorPairOverride SaveOverride(OrbitConnectorPairOverride connectorOverride, string path, VehicleCameraAssetSaver assetSaver)
        {
            if (connectorOverride == null || string.IsNullOrEmpty(path) || assetSaver == null || assetSaver.IsInsidePackageFolder(path))
            {
                return null;
            }

            AssetDatabase.CreateAsset(connectorOverride, path);
            AssetDatabase.SaveAssets();
            return connectorOverride;
        }

        public string GetDefaultAssetName(VehicleProfile frontProfile, VehicleProfile rearProfile)
        {
            return $"{GetProfileName(frontProfile)} to {GetProfileName(rearProfile)} Override";
        }

        public void DrawOverrideHandles(OrbitConnectorPairOverride connectorOverride, Vector3 planeNormal, HandleDragUndo dragUndo)
        {
            if (connectorOverride == null || connectorOverride.LeftConnector == null || connectorOverride.RightConnector == null)
            {
                return;
            }

            DrawConnectorHandles(connectorOverride, connectorOverride.LeftConnector, planeNormal, dragUndo);
            DrawConnectorHandles(connectorOverride, connectorOverride.RightConnector, planeNormal, dragUndo);
        }

        private OrbitConnectorGeometry CreateGeometry(GeneratedOrbitConnector connector)
        {
            OrbitConnectorGeometry geometry = new OrbitConnectorGeometry();
            geometry.Configure(connector.StartPosition, connector.StartControlPoint, connector.EndControlPoint, connector.EndPosition);
            return geometry;
        }

        private bool AreEndsEqual(GeneratedOrbitConnector first, GeneratedOrbitConnector second)
        {
            return Vector3.Distance(first.StartPosition, second.StartPosition) <= EndpointTolerance && Vector3.Distance(first.EndPosition, second.EndPosition) <= EndpointTolerance;
        }

        private string GetProfileName(VehicleProfile profile)
        {
            if (profile == null)
            {
                return "Missing";
            }

            return profile.name;
        }

        private void DrawConnectorHandles(OrbitConnectorPairOverride connectorOverride, OrbitConnectorGeometry connector, Vector3 planeNormal, HandleDragUndo dragUndo)
        {
            Vector3 start = connector.StartPosition;
            Vector3 end = connector.EndPosition;

            if (Event.current.type == EventType.Repaint)
            {
                Handles.color = tangentColor;
                DrawTangent(start, connector.StartControlPoint);
                DrawTangent(end, connector.EndControlPoint);
                Handles.color = endpointColor;
                Handles.DotHandleCap(0, start, Quaternion.identity, HandleUtility.GetHandleSize(start) * EndpointSize, EventType.Repaint);
                Handles.DotHandleCap(0, end, Quaternion.identity, HandleUtility.GetHandleSize(end) * EndpointSize, EventType.Repaint);
            }

            Handles.color = controlPointColor;
            if (DrawControlPoint(connector.StartControlPoint, start, planeNormal, out Vector3 newStartControlPoint))
            {
                dragUndo.RecordHandleChange(connectorOverride, "Move Connector Handle");
                connector.Configure(start, newStartControlPoint, connector.EndControlPoint, end);
                dragUndo.CompleteHandleChange(connectorOverride);
            }

            if (DrawControlPoint(connector.EndControlPoint, start, planeNormal, out Vector3 newEndControlPoint))
            {
                dragUndo.RecordHandleChange(connectorOverride, "Move Connector Handle");
                connector.Configure(start, connector.StartControlPoint, newEndControlPoint, end);
                dragUndo.CompleteHandleChange(connectorOverride);
            }
        }

        private void DrawTangent(Vector3 endpoint, Vector3 controlPoint)
        {
            tangentLine[0] = endpoint;
            tangentLine[1] = controlPoint;
            Handles.DrawAAPolyLine(2f, tangentLine);
        }

        private bool DrawControlPoint(Vector3 controlPoint, Vector3 planePoint, Vector3 planeNormal, out Vector3 newControlPoint)
        {
            Vector3 normal = planeNormal.normalized;
            Vector3 firstAxis = Vector3.Cross(normal, Vector3.forward);
            if (firstAxis.sqrMagnitude < MinimumAxisLength)
            {
                firstAxis = Vector3.Cross(normal, Vector3.right);
            }

            firstAxis.Normalize();
            Vector3 secondAxis = Vector3.Cross(firstAxis, normal);
            float size = HandleUtility.GetHandleSize(controlPoint) * ControlPointSize;
            int controlId = GUIUtility.GetControlID(FocusType.Passive);

            EditorGUI.BeginChangeCheck();
            Vector3 movedPoint = Handles.Slider2D(controlId, controlPoint, Vector3.zero, normal, firstAxis, secondAxis, size, Handles.RectangleHandleCap, Vector2.zero, false);
            if (!EditorGUI.EndChangeCheck())
            {
                newControlPoint = controlPoint;
                return false;
            }

            newControlPoint = movedPoint - normal * Vector3.Dot(movedPoint - planePoint, normal);
            return true;
        }
    }
}
