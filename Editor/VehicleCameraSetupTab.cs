using System.Collections.Generic;
using Gley.Common.Editor;
using UnityEditor;
using UnityEngine;

namespace Gley.CameraSystem.Editor
{
    public class VehicleCameraSetupTab : IVehicleCameraTab
    {
        private readonly VehicleCameraWindowContext context;
        private readonly IInputSystemInstallation inputSystemInstallation;

        private string installStatus;
        private bool isInputSystemInstalled;

        public string Title => "Setup";

        public VehicleCameraSetupTab(VehicleCameraWindowContext windowContext, IInputSystemInstallation installation)
        {
            context = windowContext;
            inputSystemInstallation = installation;
            isInputSystemInstalled = installation.IsInstalled;
        }

        public void DrawTab()
        {
            DrawProfileSummary();
            EditorGUILayout.Space();
            DrawInputSystem();
        }

        public void CollectIssues(List<EditorIssue> issues)
        {
            isInputSystemInstalled = inputSystemInstallation.IsInstalled;
            context.Collector.CollectIssues(context.Profile, VehicleCameraTabKind.Setup, issues);
        }

        public void OnSceneGUI(SceneView sceneView)
        {
        }

        private void DrawProfileSummary()
        {
            EditorGUILayout.LabelField("Profile", EditorStyles.boldLabel);
            VehicleProfile profile = context.Profile;

            if (profile == null)
            {
                EditorGUILayout.HelpBox("No vehicle profile selected. Pick one above, select one in the Project window, or press New Profile.", MessageType.Info);
                return;
            }

            int markerCount = 0;
            int pointOfInterestCount = 0;

            for (int orbitIndex = 0; orbitIndex < profile.Orbits.Count; orbitIndex++)
            {
                VehicleOrbit orbit = profile.Orbits[orbitIndex];
                if (orbit == null)
                {
                    continue;
                }

                for (int markerIndex = 0; markerIndex < orbit.WatchMarkers.Count; markerIndex++)
                {
                    OrbitWatchMarker marker = orbit.WatchMarkers[markerIndex];
                    if (marker == null)
                    {
                        continue;
                    }

                    markerCount++;
                    if (marker.IsPointOfInterest)
                    {
                        pointOfInterestCount++;
                    }
                }
            }

            EditorGUILayout.LabelField("Asset", profile.name);
            EditorGUILayout.LabelField("Profile ID", profile.ProfileId);
            EditorGUILayout.LabelField("Format version", profile.FormatVersion.ToString());
            EditorGUILayout.LabelField("Orbits", profile.Orbits.Count.ToString());
            EditorGUILayout.LabelField("Watch markers", markerCount.ToString());
            EditorGUILayout.LabelField("Points of interest", pointOfInterestCount.ToString());
            EditorGUILayout.LabelField("Views", profile.Views.Count.ToString());
        }

        private void DrawInputSystem()
        {
            EditorGUILayout.LabelField("Input System", EditorStyles.boldLabel);

            if (isInputSystemInstalled)
            {
                EditorGUILayout.HelpBox("The Input System package is installed, so the input companion is available.", MessageType.Info);
                return;
            }

            EditorGUILayout.HelpBox("The input companion needs the Input System package.", MessageType.Warning);

            if (GUILayout.Button("Install"))
            {
                installStatus = "Requested";
                ImportRequiredPackages.ImportPackage(InputSystemInstallation.PackageName, UpdateInstallStatus);
            }

            if (!string.IsNullOrEmpty(installStatus))
            {
                EditorGUILayout.LabelField("Install status", installStatus);
            }

            EditorGUILayout.HelpBox("Without the Input System, drive the camera from your own input code through the CameraSystemController command API (SetHeldIntent, AddDrag, AddPinch, SelectView, OrbitReset and the other commands).", MessageType.None);
        }

        private void UpdateInstallStatus(string status)
        {
            installStatus = status;
            context.MarkIssuesDirty();
        }
    }
}
