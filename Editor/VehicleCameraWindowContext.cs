using UnityEditor;
using UnityEngine;

namespace Gley.CameraSystem.Editor
{
    public class VehicleCameraWindowContext
    {
        private readonly RenameNotice renameNotice = new RenameNotice();
        private readonly Color selectedItemColor = new Color(0.45f, 0.7f, 1f);

        public VehicleProfile Profile { get; private set; }
        public VehicleCameraValidationCollector Collector { get; }
        public SceneHandleState SceneHandles { get; }
        public int SelectedItemId { get; private set; }
        public int ProfileRevision { get; private set; }
        public bool AreIssuesDirty { get; private set; }

        public VehicleCameraWindowContext(VehicleCameraValidationCollector collector)
        {
            Collector = collector;
            SceneHandles = new SceneHandleState();
            AreIssuesDirty = true;
        }

        public void SetProfile(VehicleProfile profile)
        {
            if (profile == Profile)
            {
                return;
            }

            Profile = profile;
            SelectedItemId = 0;
            SceneHandles.ClearSelection();
            renameNotice.Clear();
            MarkIssuesDirty();
        }

        public void SelectIssue(EditorIssue issue)
        {
            SelectedItemId = issue.ItemId;

            if (issue.Target == null)
            {
                return;
            }

            GameObject targetObject = null;
            Component targetComponent = issue.Target as Component;
            if (targetComponent != null)
            {
                targetObject = targetComponent.gameObject;
            }
            else
            {
                targetObject = issue.Target as GameObject;
            }

            if (targetObject == null)
            {
                Selection.activeObject = issue.Target;
                EditorGUIUtility.PingObject(issue.Target);
                return;
            }

            Selection.activeGameObject = targetObject;
            if (!targetObject.scene.IsValid())
            {
                EditorGUIUtility.PingObject(targetObject);
                return;
            }

            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null)
            {
                sceneView.FrameSelected();
            }
        }

        public void BeginItem(int itemId)
        {
            Color previousColor = GUI.backgroundColor;
            if (itemId != 0 && itemId == SelectedItemId)
            {
                GUI.backgroundColor = selectedItemColor;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = previousColor;
        }

        public void EndItem()
        {
            EditorGUILayout.EndVertical();
        }

        public bool DrawNameField(string currentName, out string newName)
        {
            string shownName = currentName;
            if (shownName == null)
            {
                shownName = string.Empty;
            }

            newName = EditorGUILayout.DelayedTextField("Name", shownName);
            return newName != shownName;
        }

        public void RecordProfileChange(string undoName)
        {
            Undo.RecordObject(Profile, undoName);
        }

        public void CompleteProfileChange()
        {
            EditorUtility.SetDirty(Profile);
            MarkIssuesDirty();
        }

        public void CompleteRename(int itemId, string oldName)
        {
            CompleteProfileChange();
            renameNotice.Show(itemId, oldName);
        }

        public void DrawRenameNotice(int itemId)
        {
            renameNotice.Draw(itemId);
        }

        public void HandleUndoRedo()
        {
            renameNotice.Clear();
            MarkIssuesDirty();
        }

        public void MarkIssuesDirty()
        {
            AreIssuesDirty = true;
            ProfileRevision++;
        }

        public void ClearIssuesDirty()
        {
            AreIssuesDirty = false;
        }
    }
}
