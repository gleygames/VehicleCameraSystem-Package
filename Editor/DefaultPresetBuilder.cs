using UnityEditor;
using UnityEngine;

namespace Gley.CameraSystem.Editor
{
    public class DefaultPresetBuilder
    {
        public const string FolderPath = "Assets/Gley/VehicleCameraSystem/DefaultPresets";
        public const string DrivingAssetPath = FolderPath + "/Driving.asset";
        public const string PresentationAssetPath = FolderPath + "/Presentation.asset";
        public const string InteriorAssetPath = FolderPath + "/Interior.asset";
        public const string FixedAssetPath = FolderPath + "/Fixed.asset";

        [MenuItem("Tools/Gley/Vehicle Camera System/Internal/Rebuild Default Presets")]
        private static void RebuildDefaultPresetsMenuItem()
        {
            new DefaultPresetBuilder().RebuildDefaultPresets();
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
                    AssetDatabase.CreateFolder("Assets/Gley/VehicleCameraSystem", "DefaultPresets");
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
