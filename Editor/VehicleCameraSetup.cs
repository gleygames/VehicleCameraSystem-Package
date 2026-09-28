using System.Collections.Generic;
using Gley.Common.Editor;
using UnityEditor;
using UnityEngine;

namespace Gley.CameraSystem.Editor
{
    public class VehicleCameraSetup
    {
        public const string TargetNamePrefix = "Vehicle Camera Target (";
        public const string TargetNameSuffix = ")";
        public const string InitialViewName = ProfileGenerator.DrivingName;

        private readonly ProfileGenerator generator;
        private readonly ProfileAssetSaver profileSaver;
        private readonly VehicleCameraAssetSaver assetSaver;

        public static IInputCompanionInstaller InputCompanionInstaller { get; set; }

        public VehicleCameraSetup(ProfileGenerator profileGenerator, ProfileAssetSaver saver, VehicleCameraAssetSaver cameraAssetSaver)
        {
            generator = profileGenerator;
            profileSaver = saver;
            assetSaver = cameraAssetSaver;
        }

        public Camera ResolveCamera()
        {
            GameObject[] selectedObjects = Selection.gameObjects;
            for (int index = 0; index < selectedObjects.Length; index++)
            {
                Camera selectedCamera = selectedObjects[index].GetComponent<Camera>();
                if (selectedCamera != null)
                {
                    return selectedCamera;
                }
            }

            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                EditorUtility.DisplayDialog("Set Up Vehicle Camera", "Select a Camera, or tag one MainCamera, first.", "OK");
                return null;
            }

            if (!EditorUtility.DisplayDialog("Set Up Vehicle Camera", $"Use '{mainCamera.name}' (Camera.main)?", "Use This Camera", "Cancel"))
            {
                return null;
            }

            return mainCamera;
        }

        public string DescribeFailure(VehicleCameraSetupStatus status)
        {
            if (status == VehicleCameraSetupStatus.Accepted || status == VehicleCameraSetupStatus.ProfileSaveCancelled)
            {
                return null;
            }

            if (status == VehicleCameraSetupStatus.MissingVehicle)
            {
                return "Select the vehicle model first.";
            }

            if (status == VehicleCameraSetupStatus.MissingCamera)
            {
                return "Select a Camera first.";
            }

            if (status == VehicleCameraSetupStatus.GenerationFailed)
            {
                return "No selected renderer or collider on the vehicle has any size.";
            }

            return "The vehicle camera could not be set up.";
        }

        public VehicleCameraSetupResult RunInteractive(Transform vehicleRoot, Camera camera, ProfileGenerationSettings generationSettings, bool isInputSystemInstalled, bool addTouchButtons = false)
        {
            if (vehicleRoot == null)
            {
                return new VehicleCameraSetupResult(VehicleCameraSetupStatus.MissingVehicle);
            }

            if (camera == null)
            {
                return new VehicleCameraSetupResult(VehicleCameraSetupStatus.MissingCamera);
            }

            VehicleCameraTarget existingTarget = FindExistingTarget(vehicleRoot);
            string savePath = null;
            if (existingTarget == null)
            {
                savePath = assetSaver.RequestSavePath("Save Vehicle Profile", vehicleRoot.name + " Profile", VehicleCameraAssetSaver.ProfilesFolder);
                if (string.IsNullOrEmpty(savePath))
                {
                    return new VehicleCameraSetupResult(VehicleCameraSetupStatus.ProfileSaveCancelled);
                }
            }

            GeneratorPresets defaultPresets = new DefaultPresetBuilder().LoadShippedPresets();
            return Run(vehicleRoot, camera, defaultPresets, generationSettings, savePath, isInputSystemInstalled, addTouchButtons);
        }

        public VehicleCameraSetupResult Run(Transform vehicleRoot, Camera camera, GeneratorPresets defaultPresets, ProfileGenerationSettings generationSettings, string newProfileSavePath, bool isInputSystemInstalled, bool addTouchButtons = false)
        {
            if (vehicleRoot == null)
            {
                return new VehicleCameraSetupResult(VehicleCameraSetupStatus.MissingVehicle);
            }

            if (camera == null)
            {
                return new VehicleCameraSetupResult(VehicleCameraSetupStatus.MissingCamera);
            }

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Set Up Vehicle Camera");

            VehicleCameraSetupResult result = RunSteps(vehicleRoot, camera, defaultPresets, newProfileSavePath, isInputSystemInstalled, addTouchButtons);

            Undo.CollapseUndoOperations(undoGroup);
            return result;
        }

        public Canvas FindCanvas()
        {
            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int index = 0; index < canvases.Length; index++)
            {
                if (canvases[index].isRootCanvas && canvases[index].gameObject.scene.IsValid())
                {
                    return canvases[index];
                }
            }

            return null;
        }

        public VehicleCameraTarget FindExistingTarget(Transform vehicleRoot)
        {
            VehicleCameraTarget[] targets = Object.FindObjectsByType<VehicleCameraTarget>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int index = 0; index < targets.Length; index++)
            {
                VehicleCameraTarget candidate = targets[index];
                if (candidate.BodyCount > 0 && candidate.GetBody(candidate.RootIndex) == vehicleRoot)
                {
                    return candidate;
                }
            }

            return null;
        }

        private VehicleCameraSetupResult RunSteps(Transform vehicleRoot, Camera camera, GeneratorPresets defaultPresets, string newProfileSavePath, bool isInputSystemInstalled, bool addTouchButtons)
        {
            VehicleCameraTarget target = FindExistingTarget(vehicleRoot);
            VehicleProfile profile = null;
            if (target != null)
            {
                profile = target.GetProfile(target.RootIndex);
            }

            bool generatedProfile = false;
            if (profile == null)
            {
                profile = GenerateAndSaveProfile(vehicleRoot, defaultPresets, newProfileSavePath, out VehicleCameraSetupStatus generationStatus);
                if (profile == null)
                {
                    return new VehicleCameraSetupResult(generationStatus);
                }

                generatedProfile = true;
            }

            bool createdTarget = false;
            if (target == null)
            {
                target = CreateTarget(vehicleRoot, profile);
                createdTarget = true;
            }

            CameraSystemController controller = camera.GetComponent<CameraSystemController>();
            bool createdController = false;
            if (controller == null)
            {
                controller = Undo.AddComponent<CameraSystemController>(camera.gameObject);
                createdController = true;
            }

            if (controller.Target != target)
            {
                Undo.RecordObject(controller, "Configure Vehicle Camera");
                controller.Configure(camera, target, InitialViewName);
                EditorUtility.SetDirty(controller);
            }

            if (isInputSystemInstalled && InputCompanionInstaller != null)
            {
                InputCompanionInstaller.InstallInputCompanion(camera.gameObject, controller);
                if (addTouchButtons)
                {
                    Canvas canvas = FindCanvas();
                    if (canvas != null)
                    {
                        InputCompanionInstaller.InstallTouchButtons(camera.gameObject, canvas);
                    }
                }
            }

            return new VehicleCameraSetupResult(VehicleCameraSetupStatus.Accepted, profile, target, controller, generatedProfile, createdTarget, createdController, isInputSystemInstalled);
        }

        private VehicleProfile GenerateAndSaveProfile(Transform vehicleRoot, GeneratorPresets defaultPresets, string newProfileSavePath, out VehicleCameraSetupStatus status)
        {
            if (string.IsNullOrEmpty(newProfileSavePath))
            {
                status = VehicleCameraSetupStatus.ProfileSaveCancelled;
                return null;
            }

            List<Renderer> renderers = new List<Renderer>();
            vehicleRoot.GetComponentsInChildren(true, renderers);
            FilterDefaultRenderers(renderers);

            List<Collider> colliders = new List<Collider>();
            vehicleRoot.GetComponentsInChildren(true, colliders);
            FilterDefaultColliders(colliders);

            VehicleProfile newProfile = ScriptableObject.CreateInstance<VehicleProfile>();
            ProfileGenerationResult generationResult = generator.Generate(vehicleRoot, renderers, colliders, null, defaultPresets, newProfile);
            if (generationResult != ProfileGenerationResult.Generated)
            {
                Object.DestroyImmediate(newProfile);
                status = VehicleCameraSetupStatus.GenerationFailed;
                return null;
            }

            VehicleProfile savedProfile = profileSaver.CreateProfileAsset(newProfile, newProfileSavePath);
            if (savedProfile == null)
            {
                Object.DestroyImmediate(newProfile);
                status = VehicleCameraSetupStatus.ProfileSaveCancelled;
                return null;
            }

            Undo.RegisterCreatedObjectUndo(savedProfile, "Generate Vehicle Profile");
            status = VehicleCameraSetupStatus.Accepted;
            return savedProfile;
        }

        private void FilterDefaultRenderers(List<Renderer> renderers)
        {
            for (int index = renderers.Count - 1; index >= 0; index--)
            {
                Renderer candidate = renderers[index];
                bool isMeshRenderer = candidate is MeshRenderer;
                bool isSkinnedMeshRenderer = candidate is SkinnedMeshRenderer;
                if (!isMeshRenderer && !isSkinnedMeshRenderer)
                {
                    renderers.RemoveAt(index);
                }
            }
        }

        private void FilterDefaultColliders(List<Collider> colliders)
        {
            for (int index = colliders.Count - 1; index >= 0; index--)
            {
                if (colliders[index].isTrigger)
                {
                    colliders.RemoveAt(index);
                }
            }
        }

        private VehicleCameraTarget CreateTarget(Transform vehicleRoot, VehicleProfile profile)
        {
            GameObject targetObject = new GameObject(TargetNamePrefix + vehicleRoot.name + TargetNameSuffix);
            Undo.RegisterCreatedObjectUndo(targetObject, "Create Vehicle Camera Target");
            VehicleCameraTarget target = targetObject.AddComponent<VehicleCameraTarget>();
            target.Configure(vehicleRoot, profile);
            EditorUtility.SetDirty(target);
            return target;
        }

        [MenuItem("Tools/Gley/Vehicle Camera System/Set Up Vehicle Camera")]
        private static void SetUpVehicleCameraMenuItem()
        {
            GameObject selectedVehicle = Selection.activeGameObject;
            if (selectedVehicle == null)
            {
                EditorUtility.DisplayDialog("Set Up Vehicle Camera", "Select the vehicle model in the Hierarchy first.", "OK");
                return;
            }

            VehicleCameraAssetSaver menuAssetSaver = new VehicleCameraAssetSaver(new VehicleCameraWindowProperties());
            VehicleCameraSetup setup = new VehicleCameraSetup(new ProfileGenerator(new ProfileGenerationSettings()), new ProfileAssetSaver(menuAssetSaver), menuAssetSaver);

            Camera camera = setup.ResolveCamera();
            if (camera == null)
            {
                return;
            }

            bool isInputSystemInstalled = new InputSystemInstallation().IsInstalled;
            bool addTouchButtons = false;
            if (isInputSystemInstalled && setup.FindCanvas() != null)
            {
                addTouchButtons = EditorUtility.DisplayDialog("Add Touch Buttons", "Add touch buttons to the scene Canvas?", "Add Touch Buttons", "Skip");
            }

            VehicleCameraSetupResult result = setup.RunInteractive(selectedVehicle.transform, camera, new ProfileGenerationSettings(), isInputSystemInstalled, addTouchButtons);
            string failureMessage = setup.DescribeFailure(result.Status);
            if (failureMessage != null)
            {
                EditorUtility.DisplayDialog("Set Up Vehicle Camera", failureMessage, "OK");
            }

            VehicleCameraWindow window = WindowLoader.LoadWindow<VehicleCameraWindow>(new VehicleCameraWindowProperties(), new VehicleCameraSystemVersion(), out string _);
            window.ShowSetupTab(result.Profile);
        }
    }
}
