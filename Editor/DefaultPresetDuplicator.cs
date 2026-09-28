using System;
using UnityEditor;

namespace Gley.CameraSystem.Editor
{
    public class DefaultPresetDuplicator
    {
        public const string PresetsFolder = "Assets/VehicleCameraSystemData/Presets";

        private readonly VehicleCameraAssetSaver assetSaver;

        public DefaultPresetDuplicator(VehicleCameraAssetSaver saver)
        {
            assetSaver = saver;
        }

        public bool IsDefaultPresetPath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return false;
            }

            string normalizedPath = assetPath.Replace('\\', '/');
            return normalizedPath.StartsWith(DefaultPresetBuilder.FolderPath + "/", StringComparison.OrdinalIgnoreCase);
        }

        public bool IsDefaultPreset(CameraViewPreset preset)
        {
            if (preset == null)
            {
                return false;
            }

            return IsDefaultPresetPath(AssetDatabase.GetAssetPath(preset));
        }

        public string RequestDuplicatePath(CameraViewPreset preset)
        {
            return assetSaver.RequestSavePath("Duplicate Camera View Preset", preset.name, PresetsFolder);
        }

        public CameraViewPreset CreateDuplicate(CameraViewPreset source, string path)
        {
            if (source == null || string.IsNullOrEmpty(path) || assetSaver.IsInsidePackageFolder(path))
            {
                return null;
            }

            string sourcePath = AssetDatabase.GetAssetPath(source);
            if (string.IsNullOrEmpty(sourcePath) || !AssetDatabase.CopyAsset(sourcePath, path))
            {
                return null;
            }

            return AssetDatabase.LoadAssetAtPath<CameraViewPreset>(path);
        }
    }
}
