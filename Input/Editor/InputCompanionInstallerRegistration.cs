using Gley.CameraSystem.Editor;
using UnityEditor;

namespace Gley.CameraSystem.Input.Editor
{
    [InitializeOnLoad]
    public class InputCompanionInstallerRegistration
    {
        static InputCompanionInstallerRegistration()
        {
            VehicleCameraSetup.InputCompanionInstaller = new InputCompanionInstaller();
        }
    }
}
