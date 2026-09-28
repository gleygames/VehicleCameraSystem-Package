using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Gley.CameraSystem.Editor
{
    public class VehicleCameraOrbitsTab : IVehicleCameraTab
    {
        private const string DefaultOrbitName = "Orbit";
        private const float EditInSceneButtonWidth = 100f;

        private readonly List<WatchMarkerListView> markerLists = new List<WatchMarkerListView>();
        private readonly VehicleCameraWindowContext context;
        private readonly OrbitEditOperations operations;
        private readonly HandleDragUndo dragUndo;
        private readonly OrbitPreviewCache previewCache;
        private readonly OrbitCurveHandles curveHandles;
        private readonly RemovableSectionHandles sectionHandles;
        private readonly WatchMarkerHandles markerHandles;
        private readonly EnvelopeHandles envelopeHandles;

        private int pendingRemovalOrbitId;

        public string Title => "Orbits";

        public VehicleCameraOrbitsTab(VehicleCameraWindowContext windowContext)
        {
            context = windowContext;
            operations = new OrbitEditOperations();
            dragUndo = new HandleDragUndo(context);
            previewCache = new OrbitPreviewCache(operations);
            curveHandles = new OrbitCurveHandles(operations, dragUndo, context.SceneHandles);
            sectionHandles = new RemovableSectionHandles(operations, dragUndo);
            markerHandles = new WatchMarkerHandles(operations, dragUndo, context.SceneHandles);
            envelopeHandles = new EnvelopeHandles(operations, dragUndo);
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
            context.SceneHandles.DrawPreviewRootField();
            EditorGUILayout.HelpBox("Scene view: drag knots on the orbit plane; select a knot to show its tangents (mirrored; hold Alt while dragging to break the mirror); Shift-click the curve to add a knot; Ctrl-click (Cmd on Mac) a knot to delete it. Drag markers along the curve and select one to move its watch point.", MessageType.None);
            EnsureSelectedOrbit(profile);

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
            VehicleProfile profile = context.Profile;
            if (profile == null)
            {
                return;
            }

            VehicleOrbit orbit = GetSelectedOrbit(profile);
            if (orbit == null)
            {
                return;
            }

            dragUndo.ReleaseFinishedDrag();
            previewCache.Refresh(orbit, context.ProfileRevision, false);
            Matrix4x4 previousMatrix = Handles.matrix;
            Color previousColor = Handles.color;
            Handles.matrix = context.SceneHandles.GetPreviewMatrix();

            if (!curveHandles.DrawCurveHandles(orbit, previewCache, sceneView))
            {
                envelopeHandles.DrawEnvelope(previewCache);
                sectionHandles.DrawSectionHandles(orbit, previewCache);
                markerHandles.DrawMarkerHandles(orbit, previewCache, sceneView);
                envelopeHandles.DrawRangeHandles(orbit, previewCache);
            }

            Handles.matrix = previousMatrix;
            Handles.color = previousColor;
        }

        private void EnsureSelectedOrbit(VehicleProfile profile)
        {
            VehicleOrbit orbit = GetSelectedOrbit(profile);
            if (orbit != null)
            {
                context.SceneHandles.SelectOrbit(orbit.Id);
            }
        }

        private VehicleOrbit GetSelectedOrbit(VehicleProfile profile)
        {
            if (profile.TryGetOrbit(context.SceneHandles.SelectedOrbitId, out VehicleOrbit selectedOrbit) && selectedOrbit != null)
            {
                return selectedOrbit;
            }

            return profile.PrimaryOrbit;
        }

        private void DrawOrbit(VehicleOrbit orbit)
        {
            context.BeginItem(orbit.Id);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Orbit", EditorStyles.boldLabel);
            bool isSelected = context.SceneHandles.SelectedOrbitId == orbit.Id;
            bool shouldSelect = GUILayout.Toggle(isSelected, "Edit in Scene", "Button", GUILayout.Width(EditInSceneButtonWidth));
            if (shouldSelect && !isSelected)
            {
                context.SceneHandles.SelectOrbit(orbit.Id);
            }

            EditorGUILayout.EndHorizontal();

            if (context.DrawNameField(orbit.Name, out string newName))
            {
                string oldName = orbit.Name;
                context.RecordProfileChange("Rename Orbit");
                orbit.Rename(newName);
                context.CompleteRename(orbit.Id, oldName);
            }

            context.DrawRenameNotice(orbit.Id);
            EditorGUILayout.LabelField("Knots", orbit.Knots.Count.ToString());
            float newBaseHeight = EditorGUILayout.DelayedFloatField("Base height (m)", orbit.BaseHeight);
            if (newBaseHeight != orbit.BaseHeight)
            {
                context.RecordProfileChange("Change Orbit Base Height");
                orbit.ConfigureBaseHeight(newBaseHeight);
                context.CompleteProfileChange();
            }

            EditorGUILayout.LabelField("Zoom range", $"{orbit.MinimumZoomOffset:0.##} to {orbit.MaximumZoomOffset:0.##} m");
            EditorGUILayout.LabelField("Height range", $"{orbit.MinimumHeightOffset:0.##} to {orbit.MaximumHeightOffset:0.##} m above the base height");

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
            GetMarkerList(orbit.Id).DrawMarkerList(orbit);
        }

        private WatchMarkerListView GetMarkerList(int orbitId)
        {
            for (int index = 0; index < markerLists.Count; index++)
            {
                if (markerLists[index].OrbitId == orbitId)
                {
                    return markerLists[index];
                }
            }

            WatchMarkerListView markerList = new WatchMarkerListView(orbitId, context, operations);
            markerLists.Add(markerList);
            return markerList;
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
