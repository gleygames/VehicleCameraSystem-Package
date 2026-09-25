using System.Collections.Generic;
using UnityEditor;

namespace Gley.CameraSystem.Editor
{
    public interface IVehicleCameraTab
    {
        string Title { get; }

        void DrawTab();

        void CollectIssues(List<EditorIssue> issues);

        void OnSceneGUI(SceneView sceneView);
    }
}
