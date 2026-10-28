using Gley.CameraSystem.Dev;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Gley.CameraSystem.Editor.Dev
{
    public class PerformanceBenchmarkSceneBuilder
    {
        private const string MenuPath = "Tools/GleyDev/CameraSystem/Create Performance Benchmark Scene";
        private const string DataFolderName = "PerformanceBenchmarkData";
        private const string PresentationOrbitName = "Presentation";
        private const string InitialViewName = "Driving";
        private const float GroundScale = 200f;
        private const int TrailerCount = 4;

        private readonly ReviewSceneParts parts = new ReviewSceneParts();
        private readonly string scenesFolder;
        private readonly string scenePath;
        private readonly string dataFolder;
        private readonly string sourceProfilePath;
        private readonly string benchmarkProfilePath;

        public PerformanceBenchmarkSceneBuilder()
        {
            DevAssetPaths paths = new DevAssetPaths();
            scenesFolder = paths.ScenesFolder;
            scenePath = scenesFolder + "/PerformanceBenchmark.unity";
            dataFolder = scenesFolder + "/" + DataFolderName;
            sourceProfilePath = paths.SampleTruckProfilePath;
            benchmarkProfilePath = dataFolder + "/Benchmark Truck Profile.asset";
        }

        [MenuItem(MenuPath)]
        private static void CreateScene()
        {
            new PerformanceBenchmarkSceneBuilder().Build();
        }

        public void Build()
        {
            VehicleProfile profile = GetOrCreateBenchmarkProfile();
            if (profile == null)
            {
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) != null && !EditorUtility.DisplayDialog("Performance Benchmark Scene", $"{scenePath} already exists. Replace it?", "Replace", "Cancel"))
            {
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            parts.CreateLight();
            parts.CreateGround(GroundScale);
            Transform truck = parts.CreateSampleTruck("Benchmark Truck", Vector3.zero, Quaternion.identity);
            Transform[] trailers = new Transform[TrailerCount];
            for (int index = 0; index < TrailerCount; index++)
            {
                trailers[index] = parts.CreateSampleTruck($"Benchmark Trailer {index + 1}", new Vector3(-20f - index * 10f, 0f, 0f), Quaternion.identity);
            }

            Transform secondTruck = parts.CreateSampleTruck("Second Truck", new Vector3(10f, 0f, 0f), Quaternion.identity);
            VehicleCameraTarget mainTarget = parts.CreateTarget("Benchmark Truck Target", truck, profile);
            VehicleCameraTarget secondTarget = parts.CreateTarget("Second Truck Target", secondTruck, profile);
            Camera benchmarkCamera = parts.CreateCamera("Benchmark Camera");
            CameraSystemController controller = benchmarkCamera.gameObject.AddComponent<CameraSystemController>();
            controller.SetUpdateMode(CameraUpdateMode.Manual);
            controller.Configure(benchmarkCamera, mainTarget, InitialViewName);
            GameObject benchmarkObject = new GameObject("Performance Benchmark");
            PerformanceBenchmark benchmark = benchmarkObject.AddComponent<PerformanceBenchmark>();
            benchmark.Configure(controller, mainTarget, secondTarget, trailers, profile);
            EditorSceneManager.SaveScene(scene, scenePath);
            Selection.activeGameObject = benchmarkObject;
        }

        private VehicleProfile GetOrCreateBenchmarkProfile()
        {
            VehicleProfile existing = AssetDatabase.LoadAssetAtPath<VehicleProfile>(benchmarkProfilePath);
            if (existing != null)
            {
                return existing;
            }

            if (AssetDatabase.LoadAssetAtPath<VehicleProfile>(sourceProfilePath) == null)
            {
                Debug.LogError($"Performance Benchmark scene not built: {sourceProfilePath} is missing. Generate it from the Sample Truck with the Vehicle Camera window and move it there.");
                return null;
            }

            if (!AssetDatabase.IsValidFolder(dataFolder))
            {
                AssetDatabase.CreateFolder(scenesFolder, DataFolderName);
            }

            if (!AssetDatabase.CopyAsset(sourceProfilePath, benchmarkProfilePath))
            {
                Debug.LogError($"Performance Benchmark scene not built: could not copy {sourceProfilePath} to {benchmarkProfilePath}.");
                return null;
            }

            VehicleProfile profile = AssetDatabase.LoadAssetAtPath<VehicleProfile>(benchmarkProfilePath);
            VehicleOrbit presentationOrbit;
            if (!profile.TryGetOrbit(PresentationOrbitName, out presentationOrbit))
            {
                Debug.LogError($"Performance Benchmark scene not built: {benchmarkProfilePath} has no '{PresentationOrbitName}' orbit.");
                return null;
            }

            profile.AddWatchMarker(presentationOrbit, "Right Door", 0.05f, new Vector3(1.3f, 1.5f, 2.5f), true);
            profile.AddWatchMarker(presentationOrbit, "Left Cargo", 0.45f, new Vector3(-1.3f, 1.5f, -1f), true);
            profile.AddWatchMarker(presentationOrbit, "Left Door", 0.55f, new Vector3(-1.3f, 1.5f, 2.5f), true);
            profile.AddWatchMarker(presentationOrbit, "Right Cargo", 0.95f, new Vector3(1.3f, 1.5f, -1f), true);
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            return profile;
        }
    }
}
