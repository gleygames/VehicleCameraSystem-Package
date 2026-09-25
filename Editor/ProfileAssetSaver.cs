using UnityEditor;

namespace Gley.CameraSystem.Editor
{
    public class ProfileAssetSaver
    {
        private readonly VehicleCameraAssetSaver assetSaver;

        public ProfileAssetSaver(VehicleCameraAssetSaver saver)
        {
            assetSaver = saver;
        }

        public VehicleProfile SaveNewProfile(VehicleProfile profile, string defaultName)
        {
            string path = assetSaver.RequestSavePath("Save Vehicle Profile", defaultName, VehicleCameraAssetSaver.ProfilesFolder);
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            return CreateProfileAsset(profile, path);
        }

        public VehicleProfile CreateProfileAsset(VehicleProfile profile, string path)
        {
            if (profile == null || string.IsNullOrEmpty(path) || assetSaver.IsInsidePackageFolder(path))
            {
                return null;
            }

            profile.EnsureProfileId();
            AssetDatabase.CreateAsset(profile, path);
            AssetDatabase.SaveAssets();
            return profile;
        }
    }
}
