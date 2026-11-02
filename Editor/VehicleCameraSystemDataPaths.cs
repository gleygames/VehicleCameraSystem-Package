namespace Gley.CameraSystem.Editor
{
    public class VehicleCameraSystemDataPaths
    {
        public const string UserSettingsFile = "UserSettings/Gley/VehicleCameraSystem.asset";

        private const string DataRootFolder = "Assets/GleyData";
        private const string PackageFolderName = "VehicleCameraSystem";
        private const string ProfilesFolderName = "Profiles";
        private const string PresetsFolderName = "Presets";

        public string Root { get; }
        public string ProfilesFolder { get; }
        public string PresetsFolder { get; }

        public VehicleCameraSystemDataPaths()
        {
            Root = DataRootFolder + "/" + PackageFolderName;
            ProfilesFolder = Root + "/" + ProfilesFolderName;
            PresetsFolder = Root + "/" + PresetsFolderName;
        }
    }
}
