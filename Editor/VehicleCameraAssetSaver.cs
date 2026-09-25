using System;
using Gley.Common.Editor;
using UnityEditor;

namespace Gley.CameraSystem.Editor
{
    public class VehicleCameraAssetSaver
    {
        public const string ProfilesFolder = "Assets/VehicleCameraSystemData/Profiles";
        private const string LastFolderKey = "Gley.VehicleCameraSystem.LastSaveFolder";

        private readonly ISettingsWindowProperties windowProperties;

        public VehicleCameraAssetSaver(ISettingsWindowProperties properties)
        {
            windowProperties = properties;
        }

        public string RequestSavePath(string title, string defaultName, string suggestedFolder)
        {
            string startFolder = GetStartFolder(suggestedFolder);

            while (true)
            {
                string path = EditorUtility.SaveFilePanelInProject(title, defaultName, "asset", "Choose where to save the asset.", startFolder);
                if (string.IsNullOrEmpty(path))
                {
                    return null;
                }

                if (!IsInsidePackageFolder(path))
                {
                    EditorPrefs.SetString(LastFolderKey, GetFolder(path));
                    return path;
                }

                EditorUtility.DisplayDialog("Choose another folder", "Assets cannot be saved inside the Vehicle Camera System package folder, because package updates overwrite that folder. Choose a folder outside it.", "OK");
                startFolder = GetStartFolder(suggestedFolder);
            }
        }

        public bool IsInsidePackageFolder(string assetPath)
        {
            string packageFolder = WindowLoader.GetRootFolder(windowProperties);
            if (string.IsNullOrEmpty(packageFolder) || string.IsNullOrEmpty(assetPath))
            {
                return false;
            }

            string normalizedPath = assetPath.Replace('\\', '/');
            return normalizedPath.StartsWith(packageFolder + "/", StringComparison.OrdinalIgnoreCase) || string.Equals(normalizedPath, packageFolder, StringComparison.OrdinalIgnoreCase);
        }

        private string GetStartFolder(string suggestedFolder)
        {
            string lastFolder = EditorPrefs.GetString(LastFolderKey, string.Empty);
            if (!string.IsNullOrEmpty(lastFolder) && AssetDatabase.IsValidFolder(lastFolder) && !IsInsidePackageFolder(lastFolder))
            {
                return lastFolder;
            }

            CreateFolder(suggestedFolder);
            return suggestedFolder;
        }

        private void CreateFolder(string folder)
        {
            string[] parts = folder.Split('/');
            string currentFolder = parts[0];

            for (int index = 1; index < parts.Length; index++)
            {
                string nextFolder = currentFolder + "/" + parts[index];
                if (!AssetDatabase.IsValidFolder(nextFolder))
                {
                    AssetDatabase.CreateFolder(currentFolder, parts[index]);
                }

                currentFolder = nextFolder;
            }
        }

        private string GetFolder(string assetPath)
        {
            string normalizedPath = assetPath.Replace('\\', '/');
            int separatorIndex = normalizedPath.LastIndexOf('/');
            if (separatorIndex <= 0)
            {
                return "Assets";
            }

            return normalizedPath.Substring(0, separatorIndex);
        }
    }
}
