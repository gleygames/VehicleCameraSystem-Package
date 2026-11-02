using System.IO;
using UnityEditor;

namespace Gley.CameraSystem.Editor
{
    public class DataFolderPreparer
    {
        private const string MetaExtension = ".meta";

        private string createdFolder;

        public void Prepare(string folder)
        {
            createdFolder = null;

            string[] parts = folder.Split('/');
            string currentFolder = parts[0];

            for (int index = 1; index < parts.Length; index++)
            {
                string nextFolder = currentFolder + "/" + parts[index];
                if (!AssetDatabase.IsValidFolder(nextFolder))
                {
                    AssetDatabase.CreateFolder(currentFolder, parts[index]);
                    if (createdFolder == null)
                    {
                        createdFolder = nextFolder;
                    }
                }

                currentFolder = nextFolder;
            }
        }

        public void Release(string usedPath)
        {
            string folder = createdFolder;
            createdFolder = null;

            if (folder == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(usedPath))
            {
                string normalizedPath = usedPath.Replace('\\', '/');
                if (normalizedPath == folder || normalizedPath.StartsWith(folder + "/"))
                {
                    return;
                }
            }

            if (!AssetDatabase.IsValidFolder(folder) || ContainsFiles(folder))
            {
                return;
            }

            AssetDatabase.DeleteAsset(folder);
        }

        private bool ContainsFiles(string folder)
        {
            string[] files = Directory.GetFiles(folder, "*", SearchOption.AllDirectories);
            foreach (string file in files)
            {
                if (!file.EndsWith(MetaExtension))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
