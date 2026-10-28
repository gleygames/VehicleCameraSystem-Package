using UnityEditor;
using UnityEngine;

namespace Gley.CameraSystem.Editor
{
    public class DefaultPresetBuilder
    {
        private const string FolderName = "DefaultPresets";

        private readonly string packageRoot;

        public string FolderPath { get; }
        public string DrivingAssetPath { get; }
        public string PresentationAssetPath { get; }
        public string InteriorAssetPath { get; }
        public string FixedAssetPath { get; }

        public DefaultPresetBuilder()
        {
            packageRoot = new VehicleCameraPackageLocator().PackageRoot();
            FolderPath = packageRoot + "/" + FolderName;
            DrivingAssetPath = FolderPath + "/Driving.asset";
            PresentationAssetPath = FolderPath + "/Presentation.asset";
            InteriorAssetPath = FolderPath + "/Interior.asset";
            FixedAssetPath = FolderPath + "/Fixed.asset";
        }

        public void RebuildDefaultPresets()
        {
            CreateOrUpdatePreset(DrivingAssetPath, CameraViewType.Driving);
            CreateOrUpdatePreset(PresentationAssetPath, CameraViewType.Presentation);
            CreateOrUpdatePreset(InteriorAssetPath, CameraViewType.Interior);
            CreateOrUpdatePreset(FixedAssetPath, CameraViewType.Fixed);
            AssetDatabase.SaveAssets();
        }

        public void ConfigurePreset(CameraViewPreset preset, CameraViewType type)
        {
            preset.Configure(type);
        }

        public GeneratorPresets LoadShippedPresets()
        {
            return new GeneratorPresets(
                AssetDatabase.LoadAssetAtPath<CameraViewPreset>(DrivingAssetPath),
                AssetDatabase.LoadAssetAtPath<CameraViewPreset>(PresentationAssetPath),
                AssetDatabase.LoadAssetAtPath<CameraViewPreset>(InteriorAssetPath),
                AssetDatabase.LoadAssetAtPath<CameraViewPreset>(FixedAssetPath));
        }

        private void CreateOrUpdatePreset(string path, CameraViewType type)
        {
            CameraViewPreset preset = AssetDatabase.LoadAssetAtPath<CameraViewPreset>(path);
            if (preset == null)
            {
                if (!AssetDatabase.IsValidFolder(FolderPath))
                {
                    AssetDatabase.CreateFolder(packageRoot, FolderName);
                }

                preset = ScriptableObject.CreateInstance<CameraViewPreset>();
                ConfigurePreset(preset, type);
                AssetDatabase.CreateAsset(preset, path);
                return;
            }

            ConfigurePreset(preset, type);
            EditorUtility.SetDirty(preset);
        }
    }
}
