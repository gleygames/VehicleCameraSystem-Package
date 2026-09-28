using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Gley.CameraSystem.Editor
{
    public class VehicleCameraConnectionsTab : IVehicleCameraTab
    {
        private const float ArticulationLimit = 90f;
        private const float RemoveButtonWidth = 70f;
        private const float EditInSceneButtonWidth = 100f;

        private readonly VehicleCameraWindowContext context;
        private readonly ConnectionChainSelection selection;
        private readonly VehicleCameraAssetSaver assetSaver;
        private readonly ConnectionChainPreview preview;
        private readonly ConnectionPreviewDrawer drawer;
        private readonly PairOverrideAuthoring overrideAuthoring;
        private readonly HandleDragUndo dragUndo;

        private VehicleProfile builtProfile;
        private Transform builtModel;
        private VehicleOrbit builtRootOrbit;
        private int builtRevision = -1;
        private int builtSelectionVersion = -1;
        private int selectedJointIndex = -1;
        private int pendingOverrideJointIndex = -1;
        private int pendingRemovalIndex = -1;
        private bool isRemovalFromFront;

        public string Title => "Connections";

        public VehicleCameraConnectionsTab(VehicleCameraWindowContext windowContext, ConnectionChainSelection chainSelection, VehicleCameraAssetSaver saver)
        {
            context = windowContext;
            selection = chainSelection;
            assetSaver = saver;
            preview = new ConnectionChainPreview();
            drawer = new ConnectionPreviewDrawer();
            overrideAuthoring = new PairOverrideAuthoring();
            dragUndo = new HandleDragUndo(context);
        }

        public void DrawTab()
        {
            VehicleProfile profile = context.Profile;
            if (profile == null)
            {
                EditorGUILayout.HelpBox("Select a vehicle profile to preview it connected to other bodies.", MessageType.Info);
                return;
            }

            EditorGUILayout.HelpBox("Preview this profile connected to partner bodies. Nothing in the scene changes: partners, articulation and aim lines are drawn around the preview root. A pair override is used at runtime when it is assigned to that joint on the Vehicle Camera Target.", MessageType.None);
            context.SceneHandles.DrawPreviewRootField();
            DrawRootOrbitPopup(profile);
            EditorGUILayout.Space();
            DrawPartners(ChainEnd.Front, "Front partners (nearest first)");
            DrawPartners(ChainEnd.Rear, "Rear partners (nearest first)");
            ApplyPendingRemoval();
            EnsurePreview();
            EditorGUILayout.Space();
            DrawJoints();
            DrawLegend();
        }

        public void CollectIssues(List<EditorIssue> issues)
        {
            context.Collector.CollectIssues(context.Profile, VehicleCameraTabKind.Connections, issues);
            if (context.Profile == null)
            {
                return;
            }

            EnsurePreview();
            context.Collector.CollectChainIssues(preview, issues);
        }

        public void OnSceneGUI(SceneView sceneView)
        {
            if (context.Profile == null)
            {
                return;
            }

            EnsurePreview();
            preview.ApplyArticulation(selection);
            dragUndo.ReleaseFinishedDrag();
            Matrix4x4 previousMatrix = Handles.matrix;
            Color previousColor = Handles.color;
            Matrix4x4 previewMatrix = context.SceneHandles.GetPreviewMatrix();

            drawer.DrawPreview(preview, previewMatrix);
            DrawSelectedOverrideHandles(previewMatrix);

            Handles.matrix = previousMatrix;
            Handles.color = previousColor;
        }

        private void DrawRootOrbitPopup(VehicleProfile profile)
        {
            VehicleOrbit rootOrbit = GetRootOrbit(profile);
            if (rootOrbit == null)
            {
                EditorGUILayout.HelpBox("This profile has no orbit to connect.", MessageType.Warning);
                return;
            }

            string[] options = new string[profile.Orbits.Count];
            int selectedIndex = 0;
            for (int index = 0; index < profile.Orbits.Count; index++)
            {
                VehicleOrbit orbit = profile.Orbits[index];
                options[index] = string.Empty;
                if (orbit != null)
                {
                    options[index] = orbit.Name;
                }

                if (orbit == rootOrbit)
                {
                    selectedIndex = index;
                }
            }

            int newIndex = EditorGUILayout.Popup("Orbit", selectedIndex, options);
            if (newIndex != selectedIndex && profile.Orbits[newIndex] != null)
            {
                selection.SetRootOrbitId(profile.Orbits[newIndex].Id);
                MarkSelectionChanged();
            }

            if (!rootOrbit.MergeWhenAttached)
            {
                EditorGUILayout.HelpBox($"Orbit '{rootOrbit.Name}' does not merge attached bodies, so at runtime it stays around this body alone. Partners are still drawn.", MessageType.Info);
            }
        }

        private VehicleOrbit GetRootOrbit(VehicleProfile profile)
        {
            if (profile == null)
            {
                return null;
            }

            if (profile.TryGetOrbit(selection.RootOrbitId, out VehicleOrbit selectedOrbit) && selectedOrbit != null)
            {
                return selectedOrbit;
            }

            if (profile.TryGetOrbit(context.SceneHandles.SelectedOrbitId, out VehicleOrbit handleOrbit) && handleOrbit != null)
            {
                return handleOrbit;
            }

            return profile.PrimaryOrbit;
        }

        private void MarkSelectionChanged()
        {
            selection.MarkChanged();
            context.MarkIssuesDirty();
            SceneView.RepaintAll();
            GUIUtility.ExitGUI();
        }

        private void DrawPartners(ChainEnd end, string title)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            IReadOnlyList<ConnectionPartner> partners = selection.RearPartners;
            if (end == ChainEnd.Front)
            {
                partners = selection.FrontPartners;
            }

            for (int index = 0; index < partners.Count; index++)
            {
                DrawPartner(end, partners[index], index);
            }

            if (GUILayout.Button($"Add {end} Partner"))
            {
                selection.AddPartner(end, null);
                MarkSelectionChanged();
            }
        }

        private void DrawPartner(ChainEnd end, ConnectionPartner partner, int index)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            VehicleProfile newProfile = (VehicleProfile)EditorGUILayout.ObjectField("Profile", partner.Profile, typeof(VehicleProfile), false);
            if (GUILayout.Button("Remove", GUILayout.Width(RemoveButtonWidth)))
            {
                pendingRemovalIndex = index;
                isRemovalFromFront = end == ChainEnd.Front;
            }

            EditorGUILayout.EndHorizontal();

            if (newProfile != partner.Profile)
            {
                partner.SetProfile(newProfile);
                MarkSelectionChanged();
            }

            GameObject newModel = (GameObject)EditorGUILayout.ObjectField("Model in scene", partner.Model, typeof(GameObject), true);
            if (newModel != null && !newModel.scene.IsValid())
            {
                newModel = partner.Model;
            }

            if (newModel != partner.Model)
            {
                partner.SetModel(newModel);
                MarkSelectionChanged();
            }

            EditorGUILayout.EndVertical();
        }

        private void ApplyPendingRemoval()
        {
            if (pendingRemovalIndex < 0)
            {
                return;
            }

            ChainEnd end = ChainEnd.Rear;
            if (isRemovalFromFront)
            {
                end = ChainEnd.Front;
            }

            selection.RemovePartner(end, pendingRemovalIndex);
            pendingRemovalIndex = -1;
            selectedJointIndex = -1;
            MarkSelectionChanged();
        }

        private void EnsurePreview()
        {
            VehicleProfile profile = context.Profile;
            Transform model = context.SceneHandles.PreviewRoot;
            VehicleOrbit rootOrbit = GetRootOrbit(profile);
            if (profile == builtProfile && model == builtModel && rootOrbit == builtRootOrbit && context.ProfileRevision == builtRevision && selection.Version == builtSelectionVersion)
            {
                return;
            }

            builtProfile = profile;
            builtModel = model;
            builtRootOrbit = rootOrbit;
            builtRevision = context.ProfileRevision;
            builtSelectionVersion = selection.Version;

            if (profile == null)
            {
                preview.Clear();
                return;
            }

            preview.Rebuild(profile, model, rootOrbit, selection);
            if (selectedJointIndex >= preview.BodyCount - 1)
            {
                selectedJointIndex = -1;
            }
        }

        private void DrawJoints()
        {
            if (preview.BodyCount < 2)
            {
                return;
            }

            EditorGUILayout.LabelField("Joints (front to back)", EditorStyles.boldLabel);
            for (int jointIndex = 0; jointIndex < preview.BodyCount - 1; jointIndex++)
            {
                ConnectionPartner partner = selection.GetJointPartner(jointIndex);
                if (partner != null)
                {
                    DrawJoint(jointIndex, partner);
                }
            }
        }

        private void DrawJoint(int jointIndex, ConnectionPartner partner)
        {
            VehicleProfile frontProfile = preview.Profiles[jointIndex];
            VehicleProfile rearProfile = preview.Profiles[jointIndex + 1];
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"{GetProfileName(frontProfile)} → {GetProfileName(rearProfile)}", EditorStyles.boldLabel);

            float articulation = EditorGUILayout.Slider("Articulation (°)", partner.Articulation, -ArticulationLimit, ArticulationLimit);
            if (articulation != partner.Articulation)
            {
                partner.SetArticulation(articulation);
                SceneView.RepaintAll();
            }

            OrbitConnectorPairOverride newOverride = (OrbitConnectorPairOverride)EditorGUILayout.ObjectField("Pair override", partner.JointOverride, typeof(OrbitConnectorPairOverride), false);
            if (newOverride != partner.JointOverride)
            {
                partner.SetJointOverride(newOverride);
                MarkSelectionChanged();
            }

            if (partner.JointOverride == null)
            {
                EditorGUILayout.LabelField("Connectors", "Generated");
                EditorGUI.BeginDisabledGroup(frontProfile == null || rearProfile == null);
                if (GUILayout.Button("Create Override"))
                {
                    pendingOverrideJointIndex = jointIndex;
                    EditorApplication.delayCall += CreatePendingOverride;
                }

                EditorGUI.EndDisabledGroup();
            }
            else
            {
                DrawOverrideStatus(jointIndex, partner.JointOverride, frontProfile, rearProfile);
            }

            EditorGUILayout.EndVertical();
        }

        private string GetProfileName(VehicleProfile profile)
        {
            if (profile == null)
            {
                return "(no profile)";
            }

            return profile.name;
        }

        private void CreatePendingOverride()
        {
            EditorApplication.delayCall -= CreatePendingOverride;
            int jointIndex = pendingOverrideJointIndex;
            pendingOverrideJointIndex = -1;
            if (jointIndex < 0 || jointIndex >= preview.BodyCount - 1)
            {
                return;
            }

            VehicleProfile frontProfile = preview.Profiles[jointIndex];
            VehicleProfile rearProfile = preview.Profiles[jointIndex + 1];
            string orbitName = null;
            if (preview.RootOrbit != null)
            {
                orbitName = preview.RootOrbit.Name;
            }

            OrbitConnectorPairOverride created = overrideAuthoring.CreateOverride(frontProfile, rearProfile, orbitName);
            if (created == null)
            {
                EditorUtility.DisplayDialog("Cannot create the override", "The generated connectors for this pair could not be built. Fix the errors listed for the Connections tab first.", "OK");
                return;
            }

            string path = assetSaver.RequestSavePath("Save Pair Override", overrideAuthoring.GetDefaultAssetName(frontProfile, rearProfile), VehicleCameraAssetSaver.ProfilesFolder);
            if (overrideAuthoring.SaveOverride(created, path, assetSaver) == null)
            {
                Object.DestroyImmediate(created);
                return;
            }

            ConnectionPartner partner = selection.GetJointPartner(jointIndex);
            if (partner != null)
            {
                partner.SetJointOverride(created);
                selection.MarkChanged();
            }

            selectedJointIndex = jointIndex;
            context.MarkIssuesDirty();
            EditorGUIUtility.PingObject(created);
            SceneView.RepaintAll();
        }

        private void DrawOverrideStatus(int jointIndex, OrbitConnectorPairOverride connectorOverride, VehicleProfile frontProfile, VehicleProfile rearProfile)
        {
            if (!connectorOverride.Matches(frontProfile, rearProfile, OrbitAttachmentEnd.Rear, OrbitAttachmentEnd.Front))
            {
                EditorGUILayout.HelpBox("This override belongs to another pair of profiles.", MessageType.Warning);
                return;
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Connectors", "Overridden");
            bool isSelected = selectedJointIndex == jointIndex;
            bool shouldSelect = GUILayout.Toggle(isSelected, "Edit in Scene", "Button", GUILayout.Width(EditInSceneButtonWidth));
            EditorGUILayout.EndHorizontal();

            if (shouldSelect != isSelected)
            {
                selectedJointIndex = -1;
                if (shouldSelect)
                {
                    selectedJointIndex = jointIndex;
                }

                SceneView.RepaintAll();
            }

            if (isSelected)
            {
                EditorGUILayout.HelpBox("Scene view: drag the orange square handles to reshape the left and right connectors. Their ends stay on the generated ends so the orbit stays closed.", MessageType.None);
            }
        }

        private void DrawLegend()
        {
            EditorGUILayout.HelpBox("Blue: assembled orbit in the straight reference arrangement (it never bends). Grey: this profile's own curve. Yellow: partner curves and model bounds at the preview articulation. Thick yellow / orange: generated / overridden connectors. White dots: joints. Green frustums: aim in the root frame. Magenta dotted lines: aim in the owner-body frame.", MessageType.None);
        }

        private void DrawSelectedOverrideHandles(Matrix4x4 previewMatrix)
        {
            if (selectedJointIndex < 0 || selectedJointIndex >= preview.BodyCount - 1 || selectedJointIndex >= preview.JointOverrides.Count)
            {
                return;
            }

            OrbitConnectorPairOverride connectorOverride = preview.JointOverrides[selectedJointIndex];
            if (connectorOverride == null || !connectorOverride.Matches(preview.Profiles[selectedJointIndex], preview.Profiles[selectedJointIndex + 1], OrbitAttachmentEnd.Rear, OrbitAttachmentEnd.Front))
            {
                return;
            }

            Handles.matrix = previewMatrix * preview.StraightBodyMatrices[selectedJointIndex];
            overrideAuthoring.DrawOverrideHandles(connectorOverride, preview.PlaneNormal, dragUndo);
        }
    }
}
