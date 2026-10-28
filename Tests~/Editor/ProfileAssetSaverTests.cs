using Gley.CameraSystem.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Gley.CameraSystem.Tests.EditMode
{
    public class ProfileAssetSaverTests
    {
        private const string RefusedAssetName = "RefusedProfileTest.asset";

        private VehicleCameraAssetSaver assetSaver;
        private ProfileAssetSaver profileSaver;
        private VehicleProfile profile;
        private string packagePath;
        private string previousFolder;
        private bool hadPreviousFolder;

        [SetUp]
        public void SetUp()
        {
            packagePath = new VehicleCameraPackageLocator().AssetPath(RefusedAssetName);
            assetSaver = new VehicleCameraAssetSaver();
            profileSaver = new ProfileAssetSaver(assetSaver);
            profile = ScriptableObject.CreateInstance<VehicleProfile>();
            hadPreviousFolder = EditorPrefs.HasKey(VehicleCameraAssetSaver.LastFolderKey);
            previousFolder = EditorPrefs.GetString(VehicleCameraAssetSaver.LastFolderKey, string.Empty);
        }

        [TearDown]
        public void TearDown()
        {
            if (hadPreviousFolder)
            {
                EditorPrefs.SetString(VehicleCameraAssetSaver.LastFolderKey, previousFolder);
            }
            else
            {
                EditorPrefs.DeleteKey(VehicleCameraAssetSaver.LastFolderKey);
            }

            if (profile != null && !AssetDatabase.Contains(profile))
            {
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void PathInsidePackage_Refused()
        {
            VehicleProfile savedProfile = profileSaver.CreateProfileAsset(profile, packagePath);

            Assert.IsNull(savedProfile);
            Assert.IsFalse(AssetDatabase.Contains(profile));
            Assert.IsNull(AssetDatabase.LoadAssetAtPath<VehicleProfile>(packagePath));
            Assert.IsTrue(assetSaver.IsInsidePackageFolder(packagePath));
            Assert.IsFalse(assetSaver.IsInsidePackageFolder(VehicleCameraAssetSaver.ProfilesFolder + "/Allowed.asset"));
        }

        [Test]
        public void DefaultFolder_UsedWhenNoPreference()
        {
            EditorPrefs.DeleteKey(VehicleCameraAssetSaver.LastFolderKey);

            string startFolder = assetSaver.GetStartFolder(VehicleCameraAssetSaver.ProfilesFolder);

            Assert.AreEqual("Assets/VehicleCameraSystemData/Profiles", startFolder);
            Assert.IsTrue(AssetDatabase.IsValidFolder(startFolder));
        }
    }
}
