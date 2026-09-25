using Gley.Common.Editor;

namespace Gley.CameraSystem.Editor
{
    public class VehicleCameraWindowProperties : ISettingsWindowProperties
    {
        public const string MenuItem = "Tools/Gley/Vehicle Camera System/Editor Window";

        public string VersionFilePath => string.Empty;

        public string WindowName => "Vehicle Camera System - v.";

        public int MinWidth => 420;

        public int MinHeight => 480;

        public string FolderName => "VehicleCameraSystem";

        public string ParentFolder => "Gley";
    }
}
