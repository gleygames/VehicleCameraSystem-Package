using UnityEditor;
using UnityEngine;

namespace Gley.CameraSystem.Editor
{
    [FilePath(VehicleCameraSystemDataPaths.UserSettingsFile, FilePathAttribute.Location.ProjectFolder)]
    public class VehicleCameraUserSettings : ScriptableSingleton<VehicleCameraUserSettings>
    {
        [SerializeField] private string lastSaveFolder = string.Empty;

        public string LastSaveFolder
        {
            get { return lastSaveFolder; }
            set
            {
                string newFolder = string.Empty;
                if (value != null)
                {
                    newFolder = value;
                }

                if (newFolder == lastSaveFolder)
                {
                    return;
                }

                lastSaveFolder = newFolder;
                Save(true);
            }
        }
    }
}
