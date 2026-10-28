using System.Collections.Generic;
using Gley.CameraSystem.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Gley.CameraSystem.Tests.Input
{
    internal class InputTestProfileBuilder
    {
        public const string OrbitName = "Main";
        public const string DrivingView = "Driving";
        public const string PointName = "Point";

        private readonly List<Object> objects = new List<Object>();

        public CameraViewPreset DrivingPreset { get; }

        public InputTestProfileBuilder()
        {
            DrivingPreset = CreatePreset(CameraViewType.Driving);
        }

        public CameraViewPreset CreatePreset(CameraViewType viewType)
        {
            CameraViewPreset preset = ScriptableObject.CreateInstance<CameraViewPreset>();
            preset.Configure(viewType);
            objects.Add(preset);
            return preset;
        }

        public VehicleProfile CreateProfile(float halfWidth, float front, float rear)
        {
            VehicleProfile profile = ScriptableObject.CreateInstance<VehicleProfile>();
            objects.Add(profile);
            profile.EnsureProfileId();
            VehicleOrbit orbit = profile.AddOrbit(OrbitName);
            Vector3 rearRight = new Vector3(halfWidth, 0f, -rear);
            Vector3 rearLeft = new Vector3(-halfWidth, 0f, -rear);
            Vector3 frontLeft = new Vector3(-halfWidth, 0f, front);
            Vector3 frontRight = new Vector3(halfWidth, 0f, front);
            List<BezierOrbitKnot> knots = new List<BezierOrbitKnot>();
            knots.Add(CreateKnot(rearRight, frontRight, rearLeft));
            knots.Add(CreateKnot(rearLeft, rearRight, frontLeft));
            knots.Add(CreateKnot(frontLeft, rearLeft, frontRight));
            knots.Add(CreateKnot(frontRight, frontLeft, rearRight));
            orbit.Configure(knots, Quaternion.identity);
            orbit.ConfigureOffsetRanges(-1f, 4f, -4f, 1f);
            float edge = 2f * halfWidth;
            float side = front + rear;
            float length = 2f * (edge + side);
            OrbitRemovableSection frontSection = new OrbitRemovableSection();
            frontSection.Configure((edge + side) / length, (2f * edge + side) / length);
            OrbitRemovableSection rearSection = new OrbitRemovableSection();
            rearSection.Configure(0f, edge / length);
            orbit.ConfigureRemovableSections(frontSection, rearSection);
            profile.AddWatchMarker(orbit, PointName, (2f * edge + side + side * 0.5f) / length, new Vector3(0f, 1f, 0f), true);
            profile.AddView(DrivingView, DrivingPreset, orbit.Id);
            return profile;
        }

        private BezierOrbitKnot CreateKnot(Vector3 anchor, Vector3 previous, Vector3 next)
        {
            BezierOrbitKnot knot = new BezierOrbitKnot();
            knot.Configure(anchor, anchor + (previous - anchor) / 3f, anchor + (next - anchor) / 3f);
            return knot;
        }

        public Transform CreateBody(string name)
        {
            GameObject body = new GameObject(name);
            objects.Add(body);
            return body.transform;
        }

        public VehicleCameraTarget CreateTarget(Transform root, VehicleProfile profile)
        {
            GameObject targetObject = new GameObject("Vehicle Target");
            objects.Add(targetObject);
            VehicleCameraTarget target = targetObject.AddComponent<VehicleCameraTarget>();
            target.Configure(root, profile);
            return target;
        }

        public Camera CreateCamera(string name)
        {
            GameObject cameraObject = new GameObject(name);
            objects.Add(cameraObject);
            return cameraObject.AddComponent<Camera>();
        }

        public CameraSystemController CreateController(string name, Camera camera, VehicleCameraTarget target)
        {
            GameObject controllerObject = new GameObject(name);
            objects.Add(controllerObject);
            CameraSystemController controller = controllerObject.AddComponent<CameraSystemController>();
            controller.SetUpdateMode(CameraUpdateMode.LateUpdate);
            controller.Configure(camera, target, DrivingView);
            return controller;
        }

        public VehicleCameraInput CreateCompanion(string name, CameraSystemController controller, InputActionAsset actions)
        {
            GameObject companionObject = new GameObject(name);
            objects.Add(companionObject);
            VehicleCameraInput companion = companionObject.AddComponent<VehicleCameraInput>();
            companion.Configure(controller, actions);
            return companion;
        }

        public void Dispose()
        {
            for (int index = objects.Count - 1; index >= 0; index--)
            {
                if (objects[index] != null)
                {
                    Object.Destroy(objects[index]);
                }
            }

            objects.Clear();
        }
    }
}
