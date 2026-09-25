using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Gley.CameraSystem.Editor
{
    public class VehicleCameraViewsTab : IVehicleCameraTab
    {
        private const string DefaultViewName = "View";

        private readonly VehicleCameraWindowContext context;

        private int pendingRemovalViewId;

        public string Title => "Views";

        public VehicleCameraViewsTab(VehicleCameraWindowContext windowContext)
        {
            context = windowContext;
        }

        public void DrawTab()
        {
            VehicleProfile profile = context.Profile;

            if (profile == null)
            {
                EditorGUILayout.HelpBox("Select a vehicle profile to edit its views.", MessageType.Info);
                return;
            }

            pendingRemovalViewId = 0;

            for (int viewIndex = 0; viewIndex < profile.Views.Count; viewIndex++)
            {
                VehicleViewEntry view = profile.Views[viewIndex];
                if (view != null)
                {
                    DrawView(profile, view);
                }
            }

            if (pendingRemovalViewId != 0)
            {
                context.RecordProfileChange("Remove View");
                profile.RemoveView(pendingRemovalViewId);
                context.CompleteProfileChange();
                pendingRemovalViewId = 0;
            }

            if (GUILayout.Button("Add View"))
            {
                int orbitId = 0;
                if (profile.PrimaryOrbit != null)
                {
                    orbitId = profile.PrimaryOrbit.Id;
                }

                context.RecordProfileChange("Add View");
                profile.AddView(GetUniqueViewName(profile), null, orbitId);
                context.CompleteProfileChange();
            }
        }

        public void CollectIssues(List<EditorIssue> issues)
        {
            context.Collector.CollectIssues(context.Profile, VehicleCameraTabKind.Views, issues);
        }

        public void OnSceneGUI(SceneView sceneView)
        {
        }

        private void DrawView(VehicleProfile profile, VehicleViewEntry view)
        {
            context.BeginItem(view.Id);
            EditorGUILayout.LabelField("View", EditorStyles.boldLabel);

            if (context.DrawNameField(view.Name, out string newName))
            {
                string oldName = view.Name;
                context.RecordProfileChange("Rename View");
                view.Rename(newName);
                context.CompleteRename(view.Id, oldName);
            }

            context.DrawRenameNotice(view.Id);

            CameraViewPreset newPreset = (CameraViewPreset)EditorGUILayout.ObjectField("Preset", view.Preset, typeof(CameraViewPreset), false);
            if (newPreset != view.Preset)
            {
                context.RecordProfileChange("Change View Preset");
                view.ConfigurePreset(newPreset);
                context.CompleteProfileChange();
            }

            if (view.Preset != null)
            {
                DrawViewTypeFields(profile, view);
            }

            if (GUILayout.Button("Remove View"))
            {
                pendingRemovalViewId = view.Id;
            }

            context.EndItem();
        }

        private void DrawViewTypeFields(VehicleProfile profile, VehicleViewEntry view)
        {
            CameraViewType viewType = view.Preset.ViewType;

            if (viewType == CameraViewType.Fixed)
            {
                EditorGUILayout.LabelField("Uses the profile's fixed camera pose.");
                return;
            }

            if (viewType == CameraViewType.Interior)
            {
                EditorGUILayout.LabelField("Uses the profile's seat.");
                return;
            }

            DrawOrbitPopup(profile, view);

            if (DrawPose("Default pose", view.DefaultPose, out OrbitPose newDefaultPose))
            {
                context.RecordProfileChange("Change View Default Pose");
                view.ConfigureDefaultPose(newDefaultPose);
                context.CompleteProfileChange();
            }

            if (viewType == CameraViewType.Driving && DrawPose("Front default pose (reverse)", view.FrontDefaultPose, out OrbitPose newFrontPose))
            {
                context.RecordProfileChange("Change View Front Default Pose");
                view.ConfigureFrontDefaultPose(newFrontPose);
                context.CompleteProfileChange();
            }
        }

        private void DrawOrbitPopup(VehicleProfile profile, VehicleViewEntry view)
        {
            int orbitCount = profile.Orbits.Count;
            string[] options = new string[orbitCount + 1];
            int[] orbitIds = new int[orbitCount + 1];
            int selectedIndex = 0;
            options[0] = "(none)";

            for (int orbitIndex = 0; orbitIndex < orbitCount; orbitIndex++)
            {
                VehicleOrbit orbit = profile.Orbits[orbitIndex];
                options[orbitIndex + 1] = GetOrbitLabel(orbit);
                if (orbit != null)
                {
                    orbitIds[orbitIndex + 1] = orbit.Id;
                    if (orbit.Id == view.OrbitId)
                    {
                        selectedIndex = orbitIndex + 1;
                    }
                }
            }

            if (selectedIndex == 0 && view.OrbitId != 0)
            {
                options[0] = "(missing orbit)";
            }

            int newIndex = EditorGUILayout.Popup("Orbit", selectedIndex, options);
            if (newIndex != selectedIndex && orbitIds[newIndex] != view.OrbitId)
            {
                context.RecordProfileChange("Change View Orbit");
                view.ConfigureOrbit(orbitIds[newIndex]);
                context.CompleteProfileChange();
            }
        }

        private string GetOrbitLabel(VehicleOrbit orbit)
        {
            if (orbit == null || string.IsNullOrEmpty(orbit.Name))
            {
                return "(unnamed)";
            }

            return orbit.Name;
        }

        private bool DrawPose(string label, OrbitPose pose, out OrbitPose newPose)
        {
            EditorGUILayout.LabelField(label);
            EditorGUI.indentLevel++;
            float bearing = EditorGUILayout.FloatField("Bearing (degrees)", pose.Bearing);
            float zoom = EditorGUILayout.FloatField("Zoom (m)", pose.ZoomOffset);
            float height = EditorGUILayout.FloatField("Height (m)", pose.HeightOffset);
            EditorGUI.indentLevel--;
            newPose = new OrbitPose(bearing, zoom, height);
            return bearing != pose.Bearing || zoom != pose.ZoomOffset || height != pose.HeightOffset;
        }

        private string GetUniqueViewName(VehicleProfile profile)
        {
            string viewName = DefaultViewName;
            int suffix = 2;

            while (profile.TryGetView(viewName, out VehicleViewEntry _))
            {
                viewName = $"{DefaultViewName} {suffix}";
                suffix++;
            }

            return viewName;
        }
    }
}
