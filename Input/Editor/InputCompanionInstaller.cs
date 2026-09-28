using Gley.CameraSystem.Editor;
using UnityEditor;
using UnityEngine;

namespace Gley.CameraSystem.Input.Editor
{
    public class InputCompanionInstaller : IInputCompanionInstaller
    {
        public void InstallInputCompanion(GameObject cameraObject, CameraSystemController controller)
        {
            VehicleCameraInput existingCompanion = cameraObject.GetComponent<VehicleCameraInput>();
            if (existingCompanion != null)
            {
                return;
            }

            VehicleCameraInput companion = Undo.AddComponent<VehicleCameraInput>(cameraObject);
            EditorUtility.SetDirty(companion);
        }
    }
}
