using UnityEditor;

namespace Gley.CameraSystem.Editor.Dev
{
    public class DevAssetPaths
    {
        private const string ScenesFolderGuid = "ec3f98ea444ab764ba420605a9d9eb97";

        public string ScenesFolder { get; }
        public string SampleTruckProfilePath { get; }

        public DevAssetPaths()
        {
            ScenesFolder = AssetDatabase.GUIDToAssetPath(ScenesFolderGuid);
            SampleTruckProfilePath = ScenesFolder + "/ReviewData/Sample Truck Profile.asset";
        }
    }
}
