using UnityEditor;
using UnityEngine;

namespace Gley.CameraSystem.Editor.Dev
{
    public class ReviewSceneParts
    {
        private const string MaterialFolderName = "GenerationReviewMaterials";

        private readonly string scenesFolder;
        private readonly string materialFolder;

        private Material sourceMaterial;

        public ReviewSceneParts()
        {
            scenesFolder = new DevAssetPaths().ScenesFolder;
            materialFolder = scenesFolder + "/" + MaterialFolderName;
        }

        public void CreateLight()
        {
            GameObject lightObject = new GameObject("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        public void CreateGround(float scale)
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(scale, 1f, scale);
            sourceMaterial = ground.GetComponent<Renderer>().sharedMaterial;
            ApplyMaterial(ground, "Ground", new Color(0.33f, 0.45f, 0.3f));
        }

        public void CreateLaneMarks()
        {
            Transform lane = new GameObject("Lane Marks").transform;

            for (int index = -10; index <= 10; index++)
            {
                GameObject mark = CreatePart(PrimitiveType.Cube, "Lane Mark", lane, new Vector3(0f, 0.01f, index * 8f), Quaternion.identity, new Vector3(0.2f, 0.02f, 3f));
                Object.DestroyImmediate(mark.GetComponent<Collider>());
                ApplyMaterial(mark, "Lane Mark", Color.white);
            }
        }

        public Transform CreateSampleTruck(string name, Vector3 position, Quaternion rotation)
        {
            Transform truck = new GameObject(name).transform;
            truck.SetPositionAndRotation(position, rotation);
            ApplyMaterial(CreatePart(PrimitiveType.Cube, "Cab", truck, new Vector3(0f, 1.6f, 3.2f), Quaternion.identity, new Vector3(2.5f, 2.2f, 2.2f)), "Cab", new Color(0.8f, 0.15f, 0.1f));
            ApplyMaterial(CreatePart(PrimitiveType.Cube, "Cargo", truck, new Vector3(0f, 2.1f, -1.2f), Quaternion.identity, new Vector3(2.5f, 3.2f, 6.4f)), "Cargo", new Color(0.2f, 0.45f, 0.85f));
            ApplyMaterial(CreatePart(PrimitiveType.Cube, "Mirror Left", truck, new Vector3(-1.5f, 2.3f, 3.9f), Quaternion.identity, new Vector3(0.3f, 0.4f, 0.08f)), "Mirror", new Color(0.95f, 0.8f, 0.1f));
            ApplyMaterial(CreatePart(PrimitiveType.Cube, "Mirror Right", truck, new Vector3(1.5f, 2.3f, 3.9f), Quaternion.identity, new Vector3(0.3f, 0.4f, 0.08f)), "Mirror", new Color(0.95f, 0.8f, 0.1f));
            CreateWheel(truck, "Wheel Front Left", -1.15f, 3.2f);
            CreateWheel(truck, "Wheel Front Right", 1.15f, 3.2f);
            CreateWheel(truck, "Wheel Middle Left", -1.15f, -0.5f);
            CreateWheel(truck, "Wheel Middle Right", 1.15f, -0.5f);
            CreateWheel(truck, "Wheel Rear Left", -1.15f, -3.2f);
            CreateWheel(truck, "Wheel Rear Right", 1.15f, -3.2f);
            return truck;
        }

        public Camera CreateCamera(string name)
        {
            GameObject cameraObject = new GameObject(name);
            cameraObject.tag = "MainCamera";
            Camera reviewCamera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            cameraObject.transform.position = new Vector3(0f, 6f, -14f);
            cameraObject.transform.LookAt(new Vector3(0f, 2f, 0f));
            return reviewCamera;
        }

        public VehicleCameraTarget CreateTarget(string name, Transform root, VehicleProfile profile)
        {
            GameObject targetObject = new GameObject(name);
            VehicleCameraTarget target = targetObject.AddComponent<VehicleCameraTarget>();
            target.Configure(root, profile);
            return target;
        }

        private GameObject CreatePart(PrimitiveType primitiveType, string name, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
        {
            GameObject part = GameObject.CreatePrimitive(primitiveType);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localRotation = localRotation;
            part.transform.localScale = localScale;
            return part;
        }

        private void ApplyMaterial(GameObject target, string materialName, Color color)
        {
            if (!AssetDatabase.IsValidFolder(materialFolder))
            {
                AssetDatabase.CreateFolder(scenesFolder, MaterialFolderName);
            }

            string path = materialFolder + "/" + materialName + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(sourceMaterial);
                material.color = color;
                AssetDatabase.CreateAsset(material, path);
            }

            target.GetComponent<Renderer>().sharedMaterial = material;
        }

        private void CreateWheel(Transform truck, string name, float x, float z)
        {
            ApplyMaterial(CreatePart(PrimitiveType.Cylinder, name, truck, new Vector3(x, 0.5f, z), Quaternion.Euler(0f, 0f, 90f), new Vector3(1f, 0.2f, 1f)), "Wheel", new Color(0.08f, 0.08f, 0.08f));
        }
    }
}
