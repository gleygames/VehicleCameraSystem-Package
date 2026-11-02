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

        [SetUp]
        public void SetUp()
        {
            packagePath = new VehicleCameraPackageLocator().AssetPath(RefusedAssetName);
            assetSaver = new VehicleCameraAssetSaver();
            profileSaver = new ProfileAssetSaver(assetSaver);
            profile = ScriptableObject.CreateInstance<VehicleProfile>();
            previousFolder = VehicleCameraUserSettings.instance.LastSaveFolder;
        }

        [TearDown]
        public void TearDown()
        {
            VehicleCameraUserSettings.instance.LastSaveFolder = previousFolder;

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
            Assert.IsFalse(assetSaver.IsInsidePackageFolder(assetSaver.DataPaths.ProfilesFolder + "/Allowed.asset"));
        }

        [Test]
        public void DefaultFolder_UsedWhenNoPreference()
        {
            VehicleCameraUserSettings.instance.LastSaveFolder = string.Empty;

            string startFolder = assetSaver.GetStartFolder(assetSaver.DataPaths.ProfilesFolder);

            Assert.AreEqual("Assets/GleyData/VehicleCameraSystem/Profiles", startFolder);
        }
    }
}
