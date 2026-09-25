using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Gley.CameraSystem.Editor
{
    public class VehicleCameraOrbitsTab : IVehicleCameraTab
    {
        private const string DefaultOrbitName = "Orbit";

        private readonly VehicleCameraWindowContext context;

        private int pendingRemovalOrbitId;

        public string Title => "Orbits";

        public VehicleCameraOrbitsTab(VehicleCameraWindowContext windowContext)
        {
            context = windowContext;
        }

        public void DrawTab()
        {
            VehicleProfile profile = context.Profile;

            if (profile == null)
            {
                EditorGUILayout.HelpBox("Select a vehicle profile to edit its orbits.", MessageType.Info);
                return;
            }

            pendingRemovalOrbitId = 0;

            for (int orbitIndex = 0; orbitIndex < profile.Orbits.Count; orbitIndex++)
            {
                VehicleOrbit orbit = profile.Orbits[orbitIndex];
                if (orbit != null)
                {
                    DrawOrbit(orbit);
                }
            }

            if (pendingRemovalOrbitId != 0)
            {
                context.RecordProfileChange("Remove Orbit");
                profile.RemoveOrbit(pendingRemovalOrbitId);
                context.CompleteProfileChange();
                pendingRemovalOrbitId = 0;
            }

            if (GUILayout.Button("Add Orbit"))
            {
                context.RecordProfileChange("Add Orbit");
                profile.AddOrbit(GetUniqueOrbitName(profile));
                context.CompleteProfileChange();
            }
        }

        public void CollectIssues(List<EditorIssue> issues)
        {
            context.Collector.CollectIssues(context.Profile, VehicleCameraTabKind.Orbits, issues);
        }

        public void OnSceneGUI(SceneView sceneView)
        {
        }

        private void DrawOrbit(VehicleOrbit orbit)
        {
            context.BeginItem(orbit.Id);
            EditorGUILayout.LabelField("Orbit", EditorStyles.boldLabel);

            if (context.DrawNameField(orbit.Name, out string newName))
            {
                string oldName = orbit.Name;
                context.RecordProfileChange("Rename Orbit");
                orbit.Rename(newName);
                context.CompleteRename(orbit.Id, oldName);
            }

            context.DrawRenameNotice(orbit.Id);
            EditorGUILayout.LabelField("Knots", orbit.Knots.Count.ToString());
            EditorGUILayout.LabelField("Zoom range", $"{orbit.MinimumZoomOffset:0.##} to {orbit.MaximumZoomOffset:0.##} m");
            EditorGUILayout.LabelField("Height range", $"{orbit.MinimumHeightOffset:0.##} to {orbit.MaximumHeightOffset:0.##} m");

            string mergeText = "No";
            if (orbit.MergeWhenAttached)
            {
                mergeText = "Yes";
            }

            EditorGUILayout.LabelField("Merge attached bodies", mergeText);
            DrawMarkers(orbit);

            if (GUILayout.Button("Remove Orbit"))
            {
                pendingRemovalOrbitId = orbit.Id;
            }

            context.EndItem();
        }

        private void DrawMarkers(VehicleOrbit orbit)
        {
            EditorGUILayout.LabelField("Watch markers", orbit.WatchMarkers.Count.ToString());
            EditorGUI.indentLevel++;

            for (int markerIndex = 0; markerIndex < orbit.WatchMarkers.Count; markerIndex++)
            {
                OrbitWatchMarker marker = orbit.WatchMarkers[markerIndex];
                if (marker != null)
                {
                    DrawMarker(marker);
                }
            }

            EditorGUI.indentLevel--;
        }

        private void DrawMarker(OrbitWatchMarker marker)
        {
            context.BeginItem(marker.Id);

            if (context.DrawNameField(marker.Name, out string newName))
            {
                string oldName = marker.Name;
                context.RecordProfileChange("Rename Watch Marker");
                marker.Rename(newName);
                context.CompleteRename(marker.Id, oldName);
            }

            context.DrawRenameNotice(marker.Id);
            EditorGUILayout.LabelField("Orbit position", $"{marker.NormalizedOrbitPosition:0.###}");

            if (marker.IsPointOfInterest)
            {
                EditorGUILayout.LabelField("Point of interest", "Yes");
            }

            context.EndItem();
        }

        private string GetUniqueOrbitName(VehicleProfile profile)
        {
            string orbitName = DefaultOrbitName;
            int suffix = 2;

            while (profile.TryGetOrbit(orbitName, out VehicleOrbit _))
            {
                orbitName = $"{DefaultOrbitName} {suffix}";
                suffix++;
            }

            return orbitName;
        }
    }
}
