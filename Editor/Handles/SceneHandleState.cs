using UnityEditor;
using UnityEngine;

namespace Gley.CameraSystem.Editor
{
    public class SceneHandleState
    {
        private const string DrawingPreferenceKey = "Gley.VehicleCameraSystem.SceneHandlesVisible";

        public Transform PreviewRoot { get; private set; }
        public int SelectedOrbitId { get; private set; }
        public int SelectedKnotIndex { get; private set; }
        public int SelectedMarkerId { get; private set; }
        public int SelectedViewId { get; private set; }
        public int SelectionVersion { get; private set; }
        public bool IsDrawingEnabled { get; private set; }

        public SceneHandleState()
        {
            IsDrawingEnabled = EditorPrefs.GetBool(DrawingPreferenceKey, true);
            SelectedKnotIndex = -1;
        }

        public Matrix4x4 GetPreviewMatrix()
        {
            if (PreviewRoot == null)
            {
                return Matrix4x4.identity;
            }

            return PreviewRoot.localToWorldMatrix;
        }

        public void SetDrawingEnabled(bool isEnabled)
        {
            if (isEnabled == IsDrawingEnabled)
            {
                return;
            }

            IsDrawingEnabled = isEnabled;
            EditorPrefs.SetBool(DrawingPreferenceKey, isEnabled);
            SceneView.RepaintAll();
        }

        public void SetPreviewRoot(Transform root)
        {
            if (root == PreviewRoot)
            {
                return;
            }

            PreviewRoot = root;
            SceneView.RepaintAll();
        }

        public void DrawPreviewRootField()
        {
            Transform newRoot = (Transform)EditorGUILayout.ObjectField("Preview root", PreviewRoot, typeof(Transform), true);
            if (newRoot != null && !newRoot.gameObject.scene.IsValid())
            {
                newRoot = PreviewRoot;
            }

            SetPreviewRoot(newRoot);

            if (PreviewRoot == null)
            {
                EditorGUILayout.HelpBox("Scene handles are drawn around the world origin. Assign the vehicle in the scene (or pick the model in Setup) to draw them around it.", MessageType.None);
            }
        }

        public void SelectOrbit(int orbitId)
        {
            if (orbitId == SelectedOrbitId)
            {
                return;
            }

            SelectedOrbitId = orbitId;
            SelectedKnotIndex = -1;
            SelectedMarkerId = 0;
            SelectionVersion++;
            SceneView.RepaintAll();
        }

        public void SelectKnot(int knotIndex)
        {
            SelectedKnotIndex = knotIndex;
        }

        public void SelectMarker(int markerId)
        {
            if (markerId == SelectedMarkerId)
            {
                return;
            }

            SelectedMarkerId = markerId;
            SelectionVersion++;
            SceneView.RepaintAll();
        }

        public void SelectView(int viewId)
        {
            if (viewId == SelectedViewId)
            {
                return;
            }

            SelectedViewId = viewId;
            SelectionVersion++;
            SceneView.RepaintAll();
        }

        public void ClearSelection()
        {
            SelectedOrbitId = 0;
            SelectedKnotIndex = -1;
            SelectedMarkerId = 0;
            SelectedViewId = 0;
            SelectionVersion++;
            SceneView.RepaintAll();
        }
    }
}
