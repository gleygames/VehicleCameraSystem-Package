namespace Gley.CameraSystem.Editor
{
    public class InputSystemInstallation : IInputSystemInstallation
    {
        public const string PackageName = "com.unity.inputsystem";

        public bool IsInstalled => UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/" + PackageName) != null;
    }
}
