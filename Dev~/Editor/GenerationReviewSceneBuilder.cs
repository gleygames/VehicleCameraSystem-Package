using Gley.CameraSystem.Dev;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Gley.CameraSystem.Editor.Dev
{
    public class GenerationReviewSceneBuilder
    {
        private const string MenuPath = "Tools/GleyDev/CameraSystem/Create Generation Review Scene";
        private const string InitialViewName = "Driving";
        private const float GroundScale = 20f;

        private readonly ReviewSceneParts parts = new ReviewSceneParts();
        private readonly string scenePath;
        private readonly string profilePath;

        public GenerationReviewSceneBuilder()
        {
            DevAssetPaths paths = new DevAssetPaths();
            scenePath = paths.ScenesFolder + "/GenerationReview.unity";
            profilePath = paths.SampleTruckProfilePath;
        }

        [MenuItem(MenuPath)]
        private static void CreateScene()
        {
            new GenerationReviewSceneBuilder().Build();
        }

        public void Build()
        {
            VehicleProfile profile = AssetDatabase.LoadAssetAtPath<VehicleProfile>(profilePath);
            if (profile == null)
            {
                Debug.LogError($"Generation Review scene not built: {profilePath} is missing. Generate it from the Sample Truck with the Vehicle Camera window and move it there.");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) != null && !EditorUtility.DisplayDialog("Generation Review Scene", $"{scenePath} already exists. Replace it?", "Replace", "Cancel"))
            {
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            parts.CreateLight();
            parts.CreateGround(GroundScale);
            parts.CreateLaneMarks();
            Transform truck = parts.CreateSampleTruck("Sample Truck", Vector3.zero, Quaternion.identity);
            VehicleCameraTarget target = parts.CreateTarget("Vehicle Camera Target", truck, profile);
            Camera reviewCamera = parts.CreateCamera("Review Camera");
            CameraSystemController controller = reviewCamera.gameObject.AddComponent<CameraSystemController>();
            controller.SetUpdateMode(CameraUpdateMode.LateUpdate);
            controller.Configure(reviewCamera, target, InitialViewName);
            InstallInputCompanion(reviewCamera, controller);
            GameObject fixtureObject = new GameObject("Generation Review");
            GenerationReviewFixture fixture = fixtureObject.AddComponent<GenerationReviewFixture>();
            fixture.ConfigureReview(truck, controller);
            EditorSceneManager.SaveScene(scene, scenePath);
            Selection.activeGameObject = truck.gameObject;
        }

        private void InstallInputCompanion(Camera reviewCamera, CameraSystemController controller)
        {
            IInputCompanionInstaller installer = VehicleCameraSetup.InputCompanionInstaller;
            if (installer == null)
            {
                Debug.LogWarning("Generation Review scene: the Input System companion is not available, so camera input keys will not work. Install the Input System package and rebuild the scene.");
                return;
            }

            installer.InstallInputCompanion(reviewCamera.gameObject, controller);
        }
    }
}
