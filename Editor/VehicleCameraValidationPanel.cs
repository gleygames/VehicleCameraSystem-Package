using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Gley.CameraSystem.Editor
{
    public class VehicleCameraValidationPanel
    {
        private const float SelectButtonWidth = 60f;

        private readonly VehicleCameraWindowContext context;

        public VehicleCameraValidationPanel(VehicleCameraWindowContext windowContext)
        {
            context = windowContext;
        }

        public void DrawIssues(List<EditorIssue> issues)
        {
            EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);

            if (issues.Count == 0)
            {
                EditorGUILayout.HelpBox("No issues on this tab.", MessageType.None);
                return;
            }

            for (int index = 0; index < issues.Count; index++)
            {
                DrawIssue(issues[index]);
            }
        }

        private void DrawIssue(EditorIssue issue)
        {
            MessageType messageType = MessageType.Warning;
            if (issue.Severity == EditorIssueSeverity.Error)
            {
                messageType = MessageType.Error;
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.HelpBox(issue.Message, messageType);

            if (issue.Target != null || issue.ItemId != 0)
            {
                if (GUILayout.Button("Select", GUILayout.Width(SelectButtonWidth)))
                {
                    context.SelectIssue(issue);
                }
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}
