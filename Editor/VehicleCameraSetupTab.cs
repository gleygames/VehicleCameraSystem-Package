using System.Collections.Generic;
using Gley.Common.Editor;
using UnityEditor;
using UnityEngine;

namespace Gley.CameraSystem.Editor
{
    public class VehicleCameraSetupTab : IVehicleCameraTab
    {
        private const float SmallButtonWidth = 60f;
        private const float RemoveButtonWidth = 24f;

        private readonly List<Renderer> modelRenderers = new List<Renderer>();
        private readonly List<Renderer> selectedRenderers = new List<Renderer>();
        private readonly List<Collider> modelColliders = new List<Collider>();
        private readonly List<Collider> selectedColliders = new List<Collider>();
        private readonly List<Transform> exclusions = new List<Transform>();
        private readonly List<bool> rendererSelections = new List<bool>();
        private readonly List<bool> colliderSelections = new List<bool>();
        private readonly VehicleCameraWindowContext context;
        private readonly ProfileAssetSaver profileSaver;
        private readonly ProfileGenerator generator;
        private readonly VehicleCameraSetup setup;
        private readonly ProfileGenerationSettings generationSettings;
        private readonly SerializedObject windowObject;
        private readonly SerializedProperty generationSettingsProperty;
        private readonly IInputSystemInstallation inputSystemInstallation;

        private GameObject model;
        private CameraViewPreset drivingPreset;
        private CameraViewPreset presentationPreset;
        private CameraViewPreset interiorPreset;
        private CameraViewPreset fixedPreset;
        private string installStatus;
        private bool isInputSystemInstalled;
        private bool areRenderersExpanded;
        private bool areCollidersExpanded;

        public string Title => "Setup";

        public VehicleCameraSetupTab(VehicleCameraWindowContext windowContext, IInputSystemInstallation installation, ProfileAssetSaver saver, VehicleCameraSetup vehicleCameraSetup, ProfileGenerationSettings settings, SerializedObject serializedWindow, SerializedProperty settingsProperty)
        {
            context = windowContext;
            inputSystemInstallation = installation;
            isInputSystemInstalled = installation.IsInstalled;
            profileSaver = saver;
            generator = new ProfileGenerator(settings);
            setup = vehicleCameraSetup;
            generationSettings = settings;
            windowObject = serializedWindow;
            generationSettingsProperty = settingsProperty;
        }

        public void DrawTab()
        {
            DrawOneStepSetup();
            EditorGUILayout.Space();
            DrawProfileSummary();
            EditorGUILayout.Space();
            DrawGeneration();
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

        public void SetGeneratorPresets(GeneratorPresets generatorPresets)
        {
            drivingPreset = generatorPresets.Driving;
            presentationPreset = generatorPresets.Presentation;
            interiorPreset = generatorPresets.Interior;
            fixedPreset = generatorPresets.Fixed;
        }

        private void DrawOneStepSetup()
        {
            EditorGUILayout.LabelField("One-Step Setup", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Select a vehicle model above, then set up its camera in one step: generates a profile if needed, adds a Vehicle Camera Target and a Camera System Controller.", MessageType.None);

            EditorGUI.BeginDisabledGroup(model == null);
            if (GUILayout.Button("Set Up Vehicle Camera"))
            {
                EditorApplication.delayCall += RunSetup;
            }

            EditorGUI.EndDisabledGroup();
        }

        private void RunSetup()
        {
            EditorApplication.delayCall -= RunSetup;
            if (model == null)
            {
                return;
            }

            Camera camera = setup.ResolveCamera();
            if (camera == null)
            {
                return;
            }

            VehicleCameraSetupResult result = setup.RunInteractive(model.transform, camera, generationSettings, isInputSystemInstalled);
            string failureMessage = setup.DescribeFailure(result.Status);
            if (failureMessage != null)
            {
                EditorUtility.DisplayDialog("Set Up Vehicle Camera", failureMessage, "OK");
                return;
            }

            if (result.Status != VehicleCameraSetupStatus.Accepted)
            {
                return;
            }

            context.SetProfile(result.Profile);
            SetGeneratorPresets(new DefaultPresetBuilder().LoadShippedPresets());
            EditorGUIUtility.PingObject(result.Target.gameObject);
        }

        private void DrawProfileSummary()
        {
            EditorGUILayout.LabelField("Profile", EditorStyles.boldLabel);
            VehicleProfile profile = context.Profile;

            if (profile == null)
            {
                EditorGUILayout.HelpBox("No vehicle profile selected. Generate one from a vehicle model below, pick one above, select one in the Project window, or press New Profile.", MessageType.Info);
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

        private void DrawGeneration()
        {
            EditorGUILayout.LabelField("Generate From Model", EditorStyles.boldLabel);
            GameObject newModel = (GameObject)EditorGUILayout.ObjectField("Vehicle model", model, typeof(GameObject), true);
            if (newModel != model)
            {
                model = newModel;
                CollectModelComponents();
                if (model != null && model.scene.IsValid() && context.SceneHandles.PreviewRoot == null)
                {
                    context.SceneHandles.SetPreviewRoot(model.transform);
                }
            }

            if (model == null)
            {
                EditorGUILayout.HelpBox("Select the vehicle model (a scene object or a prefab). Generation only reads its renderers and colliders; the model is never changed.", MessageType.Info);
            }
            else
            {
                DrawRendererPicker();
                DrawColliderPicker();
                DrawExclusions();
            }

            DrawPresets();
            DrawGenerationSettings();

            string buttonText = "Generate Profile";
            if (context.Profile != null)
            {
                buttonText = "Regenerate Profile";
            }

            EditorGUI.BeginDisabledGroup(model == null);
            if (GUILayout.Button(buttonText))
            {
                EditorApplication.delayCall += GenerateProfile;
            }

            EditorGUI.EndDisabledGroup();
        }

        private void CollectModelComponents()
        {
            modelRenderers.Clear();
            rendererSelections.Clear();
            modelColliders.Clear();
            colliderSelections.Clear();
            exclusions.Clear();

            if (model == null)
            {
                return;
            }

            model.GetComponentsInChildren(true, modelRenderers);
            for (int index = 0; index < modelRenderers.Count; index++)
            {
                Renderer renderer = modelRenderers[index];
                rendererSelections.Add(renderer is MeshRenderer || renderer is SkinnedMeshRenderer);
            }

            model.GetComponentsInChildren(true, modelColliders);
            for (int index = 0; index < modelColliders.Count; index++)
            {
                colliderSelections.Add(!modelColliders[index].isTrigger);
            }
        }

        private void DrawRendererPicker()
        {
            areRenderersExpanded = EditorGUILayout.Foldout(areRenderersExpanded, $"Renderers ({CountSelected(rendererSelections)} of {modelRenderers.Count})", true);
            if (!areRenderersExpanded)
            {
                return;
            }

            EditorGUI.indentLevel++;
            DrawSelectionButtons(rendererSelections);

            for (int index = 0; index < modelRenderers.Count; index++)
            {
                Renderer renderer = modelRenderers[index];
                if (renderer == null)
                {
                    continue;
                }

                rendererSelections[index] = EditorGUILayout.ToggleLeft($"{GetRelativePath(renderer.transform)} ({renderer.GetType().Name})", rendererSelections[index]);
            }

            EditorGUI.indentLevel--;
        }

        private int CountSelected(List<bool> selections)
        {
            int count = 0;

            for (int index = 0; index < selections.Count; index++)
            {
                if (selections[index])
                {
                    count++;
                }
            }

            return count;
        }

        private void DrawSelectionButtons(List<bool> selections)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUI.indentLevel * 15f);

            if (GUILayout.Button("All", GUILayout.Width(SmallButtonWidth)))
            {
                SetSelections(selections, true);
            }

            if (GUILayout.Button("None", GUILayout.Width(SmallButtonWidth)))
            {
                SetSelections(selections, false);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void SetSelections(List<bool> selections, bool isSelected)
        {
            for (int index = 0; index < selections.Count; index++)
            {
                selections[index] = isSelected;
            }
        }

        private string GetRelativePath(Transform transform)
        {
            string path = transform.name;
            Transform current = transform;

            while (current != model.transform && current.parent != null)
            {
                current = current.parent;
                path = current.name + "/" + path;
            }

            return path;
        }

        private void DrawColliderPicker()
        {
            areCollidersExpanded = EditorGUILayout.Foldout(areCollidersExpanded, $"Colliders ({CountSelected(colliderSelections)} of {modelColliders.Count})", true);
            if (!areCollidersExpanded)
            {
                return;
            }

            EditorGUI.indentLevel++;
            DrawSelectionButtons(colliderSelections);

            for (int index = 0; index < modelColliders.Count; index++)
            {
                Collider collider = modelColliders[index];
                if (collider == null)
                {
                    continue;
                }

                colliderSelections[index] = EditorGUILayout.ToggleLeft($"{GetRelativePath(collider.transform)} ({collider.GetType().Name})", colliderSelections[index]);
            }

            EditorGUI.indentLevel--;
        }

        private void DrawExclusions()
        {
            EditorGUILayout.LabelField("Exclusions (with their children)");
            EditorGUI.indentLevel++;
            int removeIndex = -1;

            for (int index = 0; index < exclusions.Count; index++)
            {
                EditorGUILayout.BeginHorizontal();
                exclusions[index] = (Transform)EditorGUILayout.ObjectField(exclusions[index], typeof(Transform), true);
                if (GUILayout.Button("-", GUILayout.Width(RemoveButtonWidth)))
                {
                    removeIndex = index;
                }

                EditorGUILayout.EndHorizontal();
            }

            if (removeIndex >= 0)
            {
                exclusions.RemoveAt(removeIndex);
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUI.indentLevel * 15f);
            if (GUILayout.Button("Add Exclusion"))
            {
                exclusions.Add(null);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUI.indentLevel--;
        }

        private void DrawPresets()
        {
            EditorGUILayout.LabelField("View presets");
            EditorGUI.indentLevel++;
            drivingPreset = DrawPresetField(ProfileGenerator.DrivingName, drivingPreset, CameraViewType.Driving);
            presentationPreset = DrawPresetField(ProfileGenerator.PresentationName, presentationPreset, CameraViewType.Presentation);
            interiorPreset = DrawPresetField(ProfileGenerator.InteriorName, interiorPreset, CameraViewType.Interior);
            fixedPreset = DrawPresetField(ProfileGenerator.FixedName, fixedPreset, CameraViewType.Fixed);
            EditorGUI.indentLevel--;
        }

        private CameraViewPreset DrawPresetField(string viewName, CameraViewPreset preset, CameraViewType expectedType)
        {
            CameraViewPreset selectedPreset = (CameraViewPreset)EditorGUILayout.ObjectField(viewName, preset, typeof(CameraViewPreset), false);

            if (selectedPreset == null)
            {
                EditorGUILayout.HelpBox($"No preset: the {viewName} view is not generated (an existing {viewName} view is left unchanged).", MessageType.None);
            }
            else if (selectedPreset.ViewType != expectedType)
            {
                EditorGUILayout.HelpBox($"This preset is a {selectedPreset.ViewType} preset, not {expectedType}.", MessageType.Warning);
            }

            return selectedPreset;
        }

        private void DrawGenerationSettings()
        {
            windowObject.Update();
            EditorGUILayout.PropertyField(generationSettingsProperty, new GUIContent("Generation settings"), true);
            windowObject.ApplyModifiedProperties();
        }

        private void GenerateProfile()
        {
            EditorApplication.delayCall -= GenerateProfile;
            if (model == null)
            {
                return;
            }

            FillSelectedComponents();
            GeneratorPresets presets = new GeneratorPresets(drivingPreset, presentationPreset, interiorPreset, fixedPreset);
            VehicleProfile profile = context.Profile;

            if (profile == null)
            {
                GenerateNewProfile(presets);
                return;
            }

            string message = $"Regenerate '{profile.name}' from '{model.name}'? The Driving and Presentation orbits, their generated markers, the connector anchors, the seat, the Fixed pose and the views that have a preset are replaced. Other orbits, markers and views are kept. You can undo this.";
            if (!EditorUtility.DisplayDialog("Regenerate Profile", message, "Regenerate", "Cancel"))
            {
                return;
            }

            context.RecordProfileChange("Generate Vehicle Profile");
            ProfileGenerationResult result = generator.Generate(model.transform, selectedRenderers, selectedColliders, exclusions, presets, profile);
            context.CompleteProfileChange();

            if (result != ProfileGenerationResult.Generated)
            {
                ShowGenerationFailure(result);
            }
        }

        private void FillSelectedComponents()
        {
            selectedRenderers.Clear();
            selectedColliders.Clear();

            for (int index = 0; index < modelRenderers.Count; index++)
            {
                if (rendererSelections[index])
                {
                    selectedRenderers.Add(modelRenderers[index]);
                }
            }

            for (int index = 0; index < modelColliders.Count; index++)
            {
                if (colliderSelections[index])
                {
                    selectedColliders.Add(modelColliders[index]);
                }
            }
        }

        private void GenerateNewProfile(GeneratorPresets presets)
        {
            VehicleProfile newProfile = ScriptableObject.CreateInstance<VehicleProfile>();
            ProfileGenerationResult result = generator.Generate(model.transform, selectedRenderers, selectedColliders, exclusions, presets, newProfile);

            if (result != ProfileGenerationResult.Generated)
            {
                Object.DestroyImmediate(newProfile);
                ShowGenerationFailure(result);
                return;
            }

            VehicleProfile savedProfile = profileSaver.SaveNewProfile(newProfile, model.name + " Profile");
            if (savedProfile == null)
            {
                Object.DestroyImmediate(newProfile);
                return;
            }

            context.SetProfile(savedProfile);
            EditorGUIUtility.PingObject(savedProfile);
        }

        private void ShowGenerationFailure(ProfileGenerationResult result)
        {
            string message = "The profile could not be generated.";

            if (result == ProfileGenerationResult.NoGeometry)
            {
                message = "No selected renderer or collider has any size. Select at least one renderer or collider that is not excluded.";
            }
            else if (result == ProfileGenerationResult.MissingRoot)
            {
                message = "Select a vehicle model first.";
            }

            EditorUtility.DisplayDialog("Generate Profile", message, "OK");
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
