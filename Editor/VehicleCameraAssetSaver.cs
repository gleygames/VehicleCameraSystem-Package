using System;
using UnityEditor;

namespace Gley.CameraSystem.Editor
{
    public class VehicleCameraAssetSaver
    {
        private readonly string packageFolder;
        private readonly DataFolderPreparer folderPreparer;

        public VehicleCameraSystemDataPaths DataPaths { get; }

        public VehicleCameraAssetSaver()
        {
            packageFolder = new VehicleCameraPackageLocator().PackageRoot();
            folderPreparer = new DataFolderPreparer();
            DataPaths = new VehicleCameraSystemDataPaths();
        }

        public string RequestSavePath(string title, string defaultName, string suggestedFolder)
        {
            while (true)
            {
                string startFolder = GetStartFolder(suggestedFolder);
                if (startFolder == suggestedFolder)
                {
                    folderPreparer.Prepare(suggestedFolder);
                }

                string path = EditorUtility.SaveFilePanelInProject(title, defaultName, "asset", "Choose where to save the asset.", startFolder);
                if (string.IsNullOrEmpty(path))
                {
                    folderPreparer.Release(null);
                    return null;
                }

                if (!IsInsidePackageFolder(path))
                {
                    folderPreparer.Release(path);
                    VehicleCameraUserSettings.instance.LastSaveFolder = GetFolder(path);
                    return path;
                }

                folderPreparer.Release(null);
                EditorUtility.DisplayDialog("Choose another folder", "Assets cannot be saved inside the Vehicle Camera System package folder, because package updates overwrite that folder. Choose a folder outside it.", "OK");
            }
        }

        public bool IsInsidePackageFolder(string assetPath)
        {
            if (string.IsNullOrEmpty(packageFolder) || string.IsNullOrEmpty(assetPath))
            {
                return false;
            }

            string normalizedPath = assetPath.Replace('\\', '/');
            return normalizedPath.StartsWith(packageFolder + "/", StringComparison.OrdinalIgnoreCase) || string.Equals(normalizedPath, packageFolder, StringComparison.OrdinalIgnoreCase);
        }

        public string GetStartFolder(string suggestedFolder)
        {
            string lastFolder = VehicleCameraUserSettings.instance.LastSaveFolder;
            if (!string.IsNullOrEmpty(lastFolder) && AssetDatabase.IsValidFolder(lastFolder) && !IsInsidePackageFolder(lastFolder))
            {
                return lastFolder;
            }

            return suggestedFolder;
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
