using UnityEditor;
using UnityEngine;

namespace Gley.CameraSystem.Editor
{
    [CustomEditor(typeof(CameraViewPreset))]
    public class DefaultPresetInspector : UnityEditor.Editor
    {
        private DefaultPresetDuplicator duplicator;

        public override void OnInspectorGUI()
        {
            if (duplicator == null)
            {
                duplicator = new DefaultPresetDuplicator(new VehicleCameraAssetSaver(new VehicleCameraWindowProperties()));
            }

            CameraViewPreset preset = (CameraViewPreset)target;
            if (!duplicator.IsDefaultPreset(preset))
            {
                DrawDefaultInspector();
                return;
            }

            EditorGUILayout.HelpBox("Default preset — duplicate to edit", MessageType.Info);
            EditorGUI.BeginDisabledGroup(true);
            DrawDefaultInspector();
            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button("Duplicate to Edit"))
            {
                DuplicateToEdit(preset);
            }
        }

        private void DuplicateToEdit(CameraViewPreset preset)
        {
            string path = duplicator.RequestDuplicatePath(preset);
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            CameraViewPreset duplicate = duplicator.CreateDuplicate(preset, path);
            if (duplicate == null)
            {
                return;
            }

            Selection.activeObject = duplicate;
            EditorGUIUtility.PingObject(duplicate);
        }
    }
}
