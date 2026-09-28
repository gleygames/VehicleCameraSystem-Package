using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Gley.CameraSystem.Editor
{
    public class WatchMarkerListView
    {
        private const string DefaultMarkerName = "Marker";

        private readonly List<OrbitWatchMarker> rows = new List<OrbitWatchMarker>();
        private readonly ReorderableList list;
        private readonly VehicleCameraWindowContext context;
        private readonly OrbitEditOperations operations;

        private VehicleOrbit orbit;
        private int pendingRemovalMarkerId;
        private int lastIssueItemId;

        public int OrbitId { get; }

        public WatchMarkerListView(int orbitId, VehicleCameraWindowContext windowContext, OrbitEditOperations editOperations)
        {
            OrbitId = orbitId;
            context = windowContext;
            operations = editOperations;
            list = new ReorderableList(rows, typeof(OrbitWatchMarker), true, true, false, false);
            list.drawHeaderCallback = DrawHeader;
            list.drawElementCallback = DrawElement;
            list.onReorderCallbackWithDetails = HandleReorder;
            list.onSelectCallback = HandleSelect;
        }

        public void DrawMarkerList(VehicleOrbit targetOrbit)
        {
            orbit = targetOrbit;
            SceneHandleState state = context.SceneHandles;
            rows.Clear();

            for (int index = 0; index < orbit.WatchMarkers.Count; index++)
            {
                rows.Add(orbit.WatchMarkers[index]);
            }

            SelectIssueMarker(state);
            list.index = -1;
            if (state.SelectedOrbitId == orbit.Id)
            {
                list.index = FindRowIndex(state.SelectedMarkerId);
            }

            list.DoLayoutList();
            DrawSelectedMarker(state);

            if (GUILayout.Button("Add Watch Marker"))
            {
                AddMarker(state);
            }
        }

        private void DrawHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, "Watch markers (drag to reorder; the order is the next/previous point order)");
        }

        private void DrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            OrbitWatchMarker marker = rows[index];
            if (marker == null)
            {
                EditorGUI.LabelField(rect, "(missing marker)");
                return;
            }

            string kind = "marker";
            if (marker.IsPointOfInterest)
            {
                kind = "point of interest";
            }

            EditorGUI.LabelField(rect, $"{marker.Name}  ({kind}, position {marker.NormalizedOrbitPosition:0.###})");
        }

        private void HandleReorder(ReorderableList reorderedList, int oldIndex, int newIndex)
        {
            context.RecordProfileChange("Reorder Watch Markers");
            operations.ReorderMarkers(orbit, oldIndex, newIndex);
            context.CompleteProfileChange();
        }

        private void HandleSelect(ReorderableList selectedList)
        {
            int index = selectedList.index;
            if (index < 0 || index >= rows.Count || rows[index] == null)
            {
                return;
            }

            context.SceneHandles.SelectOrbit(orbit.Id);
            context.SceneHandles.SelectMarker(rows[index].Id);
        }

        private void SelectIssueMarker(SceneHandleState state)
        {
            int issueItemId = context.SelectedItemId;
            if (issueItemId == lastIssueItemId)
            {
                return;
            }

            lastIssueItemId = issueItemId;
            if (FindRowIndex(issueItemId) >= 0)
            {
                state.SelectOrbit(orbit.Id);
                state.SelectMarker(issueItemId);
            }
        }

        private int FindRowIndex(int markerId)
        {
            if (markerId == 0)
            {
                return -1;
            }

            for (int index = 0; index < rows.Count; index++)
            {
                if (rows[index] != null && rows[index].Id == markerId)
                {
                    return index;
                }
            }

            return -1;
        }

        private void DrawSelectedMarker(SceneHandleState state)
        {
            if (state.SelectedOrbitId != orbit.Id)
            {
                return;
            }

            int rowIndex = FindRowIndex(state.SelectedMarkerId);
            if (rowIndex < 0)
            {
                return;
            }

            OrbitWatchMarker marker = rows[rowIndex];
            pendingRemovalMarkerId = 0;
            context.BeginItem(marker.Id);
            EditorGUILayout.LabelField("Selected marker", EditorStyles.boldLabel);

            if (context.DrawNameField(marker.Name, out string newName))
            {
                string oldName = marker.Name;
                context.RecordProfileChange("Rename Watch Marker");
                marker.Rename(newName);
                context.CompleteRename(marker.Id, oldName);
            }

            context.DrawRenameNotice(marker.Id);

            float newPosition = EditorGUILayout.DelayedFloatField("Orbit position (0 to 1)", marker.NormalizedOrbitPosition);
            if (newPosition != marker.NormalizedOrbitPosition)
            {
                context.RecordProfileChange("Move Watch Marker");
                marker.Configure(WrapNormalizedPosition(newPosition), marker.WatchPointLocalPosition);
                context.CompleteProfileChange();
            }

            Vector3 newWatchPoint = EditorGUILayout.Vector3Field("Watch point (body local)", marker.WatchPointLocalPosition);
            if (newWatchPoint != marker.WatchPointLocalPosition)
            {
                context.RecordProfileChange("Move Watch Point");
                marker.Configure(marker.NormalizedOrbitPosition, newWatchPoint);
                context.CompleteProfileChange();
            }

            bool isPointOfInterest = EditorGUILayout.Toggle("Point of interest", marker.IsPointOfInterest);
            if (isPointOfInterest != marker.IsPointOfInterest)
            {
                context.RecordProfileChange("Change Point Of Interest");
                marker.ConfigurePointOfInterest(isPointOfInterest);
                context.CompleteProfileChange();
            }

            if (GUILayout.Button("Remove Watch Marker"))
            {
                pendingRemovalMarkerId = marker.Id;
            }

            context.EndItem();

            if (pendingRemovalMarkerId != 0)
            {
                context.RecordProfileChange("Remove Watch Marker");
                orbit.RemoveWatchMarker(pendingRemovalMarkerId);
                context.CompleteProfileChange();
                state.SelectMarker(0);
                pendingRemovalMarkerId = 0;
            }
        }

        private float WrapNormalizedPosition(float position)
        {
            float wrapped = Mathf.Repeat(position, 1f);
            if (wrapped >= 1f)
            {
                wrapped = 0f;
            }

            return wrapped;
        }

        private void AddMarker(SceneHandleState state)
        {
            VehicleProfile profile = context.Profile;
            context.RecordProfileChange("Add Watch Marker");
            OrbitWatchMarker marker = profile.AddWatchMarker(orbit, GetUniqueMarkerName(profile), operations.FindFreeMarkerPosition(orbit), operations.ToBodyLocalPoint(orbit, Vector3.zero), false);
            context.CompleteProfileChange();
            state.SelectOrbit(orbit.Id);
            state.SelectMarker(marker.Id);
        }

        private string GetUniqueMarkerName(VehicleProfile profile)
        {
            string markerName = DefaultMarkerName;
            int suffix = 2;

            while (IsMarkerNameUsed(profile, markerName))
            {
                markerName = $"{DefaultMarkerName} {suffix}";
                suffix++;
            }

            return markerName;
        }

        private bool IsMarkerNameUsed(VehicleProfile profile, string markerName)
        {
            for (int orbitIndex = 0; orbitIndex < profile.Orbits.Count; orbitIndex++)
            {
                VehicleOrbit profileOrbit = profile.Orbits[orbitIndex];
                if (profileOrbit == null)
                {
                    continue;
                }

                for (int markerIndex = 0; markerIndex < profileOrbit.WatchMarkers.Count; markerIndex++)
                {
                    OrbitWatchMarker marker = profileOrbit.WatchMarkers[markerIndex];
                    if (marker != null && marker.Name == markerName)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
