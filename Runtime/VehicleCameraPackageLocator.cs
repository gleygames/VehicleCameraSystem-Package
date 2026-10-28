#if UNITY_EDITOR
using UnityEditor.Compilation;

namespace Gley.CameraSystem
{
    public class VehicleCameraPackageLocator
    {
        private const string AssemblyName = "Gley.CameraSystem";

        public string PackageRoot()
        {
            string asmdefPath = CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(AssemblyName);
            if (string.IsNullOrEmpty(asmdefPath))
            {
                return string.Empty;
            }

            string runtimeFolder = GetFolder(asmdefPath.Replace('\\', '/'));
            return GetFolder(runtimeFolder);
        }

        public string AssetPath(string relativeToRoot)
        {
            return PackageRoot() + "/" + relativeToRoot;
        }

        private string GetFolder(string assetPath)
        {
            int separatorIndex = assetPath.LastIndexOf('/');
            if (separatorIndex <= 0)
            {
                return string.Empty;
            }

            return assetPath.Substring(0, separatorIndex);
        }
    }
}
#endif
