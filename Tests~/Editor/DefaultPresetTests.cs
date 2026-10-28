using Gley.CameraSystem.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Gley.CameraSystem.Tests.EditMode
{
    public class DefaultPresetTests
    {
        private const string SourcePresetPath = DefaultPresetDuplicator.PresetsFolder + "/DuplicateTestSource.asset";
        private const string DuplicatePresetPath = DefaultPresetDuplicator.PresetsFolder + "/DuplicateTestCopy.asset";

        private DefaultPresetBuilder builder;
        private DefaultPresetDuplicator duplicator;
        private VehicleCameraAssetSaver assetSaver;
        private CameraViewPreset built;
        private CameraViewPreset expected;
        private CameraViewPreset source;

        [SetUp]
        public void SetUp()
        {
            builder = new DefaultPresetBuilder();
            assetSaver = new VehicleCameraAssetSaver();
            duplicator = new DefaultPresetDuplicator(assetSaver);
        }

        [TearDown]
        public void TearDown()
        {
            if (built != null)
            {
                Object.DestroyImmediate(built);
            }

            if (expected != null)
            {
                Object.DestroyImmediate(expected);
            }

            if (AssetDatabase.LoadAssetAtPath<CameraViewPreset>(DuplicatePresetPath) != null)
            {
                AssetDatabase.DeleteAsset(DuplicatePresetPath);
            }

            if (AssetDatabase.LoadAssetAtPath<CameraViewPreset>(SourcePresetPath) != null)
            {
                AssetDatabase.DeleteAsset(SourcePresetPath);
            }
        }

        [TestCase(CameraViewType.Driving)]
        [TestCase(CameraViewType.Presentation)]
        [TestCase(CameraViewType.Interior)]
        [TestCase(CameraViewType.Fixed)]
        public void DefaultPresets_MatchConfigureDefaults(CameraViewType viewType)
        {
            built = ScriptableObject.CreateInstance<CameraViewPreset>();
            builder.ConfigurePreset(built, viewType);

            expected = ScriptableObject.CreateInstance<CameraViewPreset>();
            expected.Configure(viewType);

            Assert.AreEqual(expected.ViewType, built.ViewType);
            Assert.AreEqual(expected.OrbitMovement.ManualTravelSpeed, built.OrbitMovement.ManualTravelSpeed);
            Assert.AreEqual(expected.OrbitMovement.ImageRollFollow, built.OrbitMovement.ImageRollFollow);
            Assert.AreEqual(expected.Driving.SpeedDistanceEnabled, built.Driving.SpeedDistanceEnabled);
            Assert.AreEqual(expected.Driving.TurnLookEnabled, built.Driving.TurnLookEnabled);
            Assert.AreEqual(expected.Recenter.Mode, built.Recenter.Mode);
            Assert.AreEqual(expected.Collision.Enabled, built.Collision.Enabled);
            Assert.AreEqual(expected.AimFrame, built.AimFrame);
            Assert.AreEqual(expected.TimeSource, built.TimeSource);
        }

        [Test]
        public void IsDefaultPresetPath_InsideDefaultPresetsFolder_True()
        {
            Assert.IsTrue(duplicator.IsDefaultPresetPath(DefaultPresetBuilder.DrivingAssetPath));
            Assert.IsFalse(duplicator.IsDefaultPresetPath(DefaultPresetDuplicator.PresetsFolder + "/Driving.asset"));
            Assert.IsFalse(duplicator.IsDefaultPresetPath(string.Empty));
        }

        [Test]
        public void DuplicateToEdit_CopyOutsidePackage()
        {
            EnsureFolder("Assets/VehicleCameraSystemData");
            EnsureFolder(DefaultPresetDuplicator.PresetsFolder);

            source = ScriptableObject.CreateInstance<CameraViewPreset>();
            source.Configure(CameraViewType.Driving);
            AssetDatabase.CreateAsset(source, SourcePresetPath);

            CameraViewPreset duplicate = duplicator.CreateDuplicate(source, DuplicatePresetPath);

            Assert.IsNotNull(duplicate);
            Assert.AreNotSame(source, duplicate);
            Assert.AreEqual(CameraViewType.Driving, duplicate.ViewType);
            Assert.IsFalse(assetSaver.IsInsidePackageFolder(AssetDatabase.GetAssetPath(duplicate)));
            Assert.IsNull(duplicator.CreateDuplicate(source, DefaultPresetBuilder.FolderPath + "/Refused.asset"));
        }

        private void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            int separatorIndex = folder.LastIndexOf('/');
            string parent = folder.Substring(0, separatorIndex);
            string name = folder.Substring(separatorIndex + 1);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
