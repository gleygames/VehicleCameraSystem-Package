using UnityEngine;

namespace Gley.CameraSystem.Editor
{
    public interface IInputCompanionInstaller
    {
        void InstallInputCompanion(GameObject cameraObject, CameraSystemController controller);
    }
}
