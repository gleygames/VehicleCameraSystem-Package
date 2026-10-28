using System.Collections.Generic;
using Gley.CameraSystem.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Gley.CameraSystem.Tests.EditMode
{
    public class VehicleCameraSetupTests
    {
        private readonly List<Object> objects = new List<Object>();

        private VehicleCameraSetup setup;
        private GeneratorPresets presets;
        private string createdProfilePath;

        [SetUp]
        public void SetUp()
        {
            VehicleCameraAssetSaver assetSaver = new VehicleCameraAssetSaver();
            ProfileAssetSaver profileSaver = new ProfileAssetSaver(assetSaver);
            ProfileGenerator generator = new ProfileGenerator(new ProfileGenerationSettings());
            setup = new VehicleCameraSetup(generator, profileSaver, assetSaver);
            presets = new GeneratorPresets(CreatePreset(CameraViewType.Driving), CreatePreset(CameraViewType.Presentation), CreatePreset(CameraViewType.Interior), CreatePreset(CameraViewType.Fixed));
            EnsureFolder("Assets/VehicleCameraSystemData");
            EnsureFolder("Assets/VehicleCameraSystemData/Profiles");
        }

        [TearDown]
        public void TearDown()
        {
            Undo.ClearAll();

            if (!string.IsNullOrEmpty(createdProfilePath))
            {
                AssetDatabase.DeleteAsset(createdProfilePath);
                createdProfilePath = null;
            }

            for (int index = objects.Count - 1; index >= 0; index--)
            {
                if (objects[index] != null)
                {
                    Object.DestroyImmediate(objects[index]);
                }
            }

            objects.Clear();
        }

        [Test]
        public void Setup_SceneVehicleAndCamera_CreatesTargetControllerAndProfile()
        {
            Transform vehicle = CreateBoxVehicle("Truck");
            Camera camera = CreateCamera();
            createdProfilePath = "Assets/VehicleCameraSystemData/Profiles/SetupTestProfile.asset";

            VehicleCameraSetupResult result = setup.Run(vehicle, camera, presets, new ProfileGenerationSettings(), createdProfilePath, false);
            RegisterTarget(result);

            Assert.AreEqual(VehicleCameraSetupStatus.Accepted, result.Status);
            Assert.IsTrue(result.GeneratedProfile);
            Assert.IsTrue(result.CreatedTarget);
            Assert.IsTrue(result.CreatedController);
            Assert.IsNotNull(result.Profile);
            Assert.AreEqual(4, result.Profile.Views.Count);
            Assert.IsNotNull(result.Target);
            Assert.AreEqual(vehicle, result.Target.GetBody(result.Target.RootIndex));
            Assert.AreEqual(result.Profile, result.Target.GetProfile(result.Target.RootIndex));
            Assert.AreEqual("Vehicle Camera Target (Truck)", result.Target.name);
            Assert.IsNotNull(result.Controller);
            Assert.AreEqual(camera, result.Controller.AssignedCamera);
            Assert.AreEqual(result.Target, result.Controller.Target);
            Assert.AreEqual(VehicleCameraSetup.InitialViewName, result.Controller.InitialViewName);
        }

        [Test]
        public void Setup_RunTwice_NoDuplicates()
        {
            Transform vehicle = CreateBoxVehicle("Truck");
            Camera camera = CreateCamera();
            createdProfilePath = "Assets/VehicleCameraSystemData/Profiles/SetupTestProfileTwice.asset";

            VehicleCameraSetupResult first = setup.Run(vehicle, camera, presets, new ProfileGenerationSettings(), createdProfilePath, false);
            RegisterTarget(first);
            VehicleCameraSetupResult second = setup.Run(vehicle, camera, presets, new ProfileGenerationSettings(), createdProfilePath, false);

            Assert.AreEqual(VehicleCameraSetupStatus.Accepted, first.Status);
            Assert.AreEqual(VehicleCameraSetupStatus.Accepted, second.Status);
            Assert.IsFalse(second.GeneratedProfile);
            Assert.IsFalse(second.CreatedTarget);
            Assert.IsFalse(second.CreatedController);
            Assert.AreSame(first.Profile, second.Profile);
            Assert.AreSame(first.Target, second.Target);
            Assert.AreSame(first.Controller, second.Controller);

            VehicleCameraTarget[] targets = Object.FindObjectsByType<VehicleCameraTarget>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int matchingTargets = 0;
            for (int index = 0; index < targets.Length; index++)
            {
                if (targets[index].GetBody(targets[index].RootIndex) == vehicle)
                {
                    matchingTargets++;
                }
            }

            Assert.AreEqual(1, matchingTargets);
            Assert.AreEqual(1, camera.GetComponents<CameraSystemController>().Length);
        }

        [Test]
        public void Setup_PrefabAssetNotModified()
        {
            Transform vehicle = CreateBoxVehicle("Truck");
            Transform[] transformsBefore = vehicle.GetComponentsInChildren<Transform>(true);
            List<Vector3> positions = new List<Vector3>();
            List<int> componentCounts = new List<int>();
            for (int index = 0; index < transformsBefore.Length; index++)
            {
                positions.Add(transformsBefore[index].localPosition);
                componentCounts.Add(transformsBefore[index].GetComponents<Component>().Length);
            }

            Camera camera = CreateCamera();
            createdProfilePath = "Assets/VehicleCameraSystemData/Profiles/SetupTestProfileUnchanged.asset";

            VehicleCameraSetupResult result = setup.Run(vehicle, camera, presets, new ProfileGenerationSettings(), createdProfilePath, false);
            RegisterTarget(result);

            Assert.AreEqual(VehicleCameraSetupStatus.Accepted, result.Status);
            Transform[] transformsAfter = vehicle.GetComponentsInChildren<Transform>(true);
            Assert.AreEqual(transformsBefore.Length, transformsAfter.Length);
            for (int index = 0; index < transformsBefore.Length; index++)
            {
                Assert.AreEqual(transformsBefore[index], transformsAfter[index]);
                Assert.AreEqual(positions[index], transformsAfter[index].localPosition);
                Assert.AreEqual(componentCounts[index], transformsAfter[index].GetComponents<Component>().Length);
            }

            Assert.AreNotEqual(vehicle, result.Target.transform.parent);
            Assert.IsNull(vehicle.GetComponent<VehicleCameraTarget>());
        }

        [Test]
        public void Setup_UndoGroup_RemovesEverything()
        {
            Transform vehicle = CreateBoxVehicle("Truck");
            Camera camera = CreateCamera();
            createdProfilePath = "Assets/VehicleCameraSystemData/Profiles/SetupTestProfileUndo.asset";

            int groupBefore = Undo.GetCurrentGroup();
            VehicleCameraSetupResult result = setup.Run(vehicle, camera, presets, new ProfileGenerationSettings(), createdProfilePath, false);
            RegisterTarget(result);

            Assert.AreEqual(VehicleCameraSetupStatus.Accepted, result.Status);
            Assert.GreaterOrEqual(Undo.GetCurrentGroup(), groupBefore + 1, "The setup should record at least one undo group.");
            Assert.AreEqual("Set Up Vehicle Camera", Undo.GetCurrentGroupName(), "Every created object should collapse into a single named undo group.");
        }

        private void RegisterTarget(VehicleCameraSetupResult result)
        {
            if (result.Target != null)
            {
                objects.Add(result.Target.gameObject);
            }
        }

        private CameraViewPreset CreatePreset(CameraViewType viewType)
        {
            CameraViewPreset preset = ScriptableObject.CreateInstance<CameraViewPreset>();
            preset.Configure(viewType);
            objects.Add(preset);
            return preset;
        }

        private Transform CreateBoxVehicle(string name)
        {
            GameObject root = new GameObject(name);
            objects.Add(root);
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.SetParent(root.transform, false);
            cube.transform.localScale = new Vector3(2f, 2f, 4f);
            objects.Add(cube);
            return root.transform;
        }

        private Camera CreateCamera()
        {
            GameObject cameraObject = new GameObject("Camera", typeof(Camera));
            objects.Add(cameraObject);
            return cameraObject.GetComponent<Camera>();
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
