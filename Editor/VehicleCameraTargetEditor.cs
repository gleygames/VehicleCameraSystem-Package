using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Gley.CameraSystem.Editor
{
    [CustomEditor(typeof(VehicleCameraTarget))]
    public class VehicleCameraTargetEditor : UnityEditor.Editor
    {
        private readonly List<EditorIssue> issues = new List<EditorIssue>();

        private VehicleCameraValidationCollector collector;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (collector == null)
            {
                collector = new VehicleCameraValidationCollector(new InputSystemInstallation());
            }

            issues.Clear();
            collector.CollectTargetIssues((VehicleCameraTarget)target, issues);

            for (int index = 0; index < issues.Count; index++)
            {
                DrawIssue(issues[index]);
            }

            issues.Clear();
        }

        private void DrawIssue(EditorIssue issue)
        {
            EditorGUILayout.HelpBox(issue.Message, MessageType.Warning);

            Rigidbody bodyRigidbody = issue.Target as Rigidbody;
            if (bodyRigidbody == null)
            {
                return;
            }

            if (GUILayout.Button("Set to Interpolate"))
            {
                Undo.RecordObject(bodyRigidbody, "Set Rigidbody Interpolation");
                bodyRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
                PrefabUtility.RecordPrefabInstancePropertyModifications(bodyRigidbody);
                EditorUtility.SetDirty(bodyRigidbody);
            }
        }
    }
}
