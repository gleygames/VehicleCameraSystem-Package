using System.Collections.Generic;
using UnityEditor;

namespace Gley.CameraSystem.Editor
{
    public class VehicleCameraConnectionsTab : IVehicleCameraTab
    {
        private readonly VehicleCameraWindowContext context;

        public string Title => "Connections";

        public VehicleCameraConnectionsTab(VehicleCameraWindowContext windowContext)
        {
            context = windowContext;
        }

        public void DrawTab()
        {
            EditorGUILayout.HelpBox("Partner profiles, generated connector previews and pair overrides will appear here.", MessageType.Info);
        }

        public void CollectIssues(List<EditorIssue> issues)
        {
            context.Collector.CollectIssues(context.Profile, VehicleCameraTabKind.Connections, issues);
        }

        public void OnSceneGUI(SceneView sceneView)
        {
        }
    }
}
