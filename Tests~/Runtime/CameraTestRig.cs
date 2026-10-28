using System.Collections.Generic;
using UnityEngine;

namespace Gley.CameraSystem.Tests.PlayMode
{
    internal class CameraTestRig
    {
        public const string OrbitName = "Main";
        public const string PresentationView = "Presentation";
        public const string FixedView = "Fixed";
        public const string InteriorView = "Interior";

        private readonly List<Object> objects = new List<Object>();

        public CameraViewPreset PresentationPreset { get; }
        public CameraViewPreset FixedPreset { get; }
        public CameraViewPreset InteriorPreset { get; }

        public CameraTestRig()
        {
            PresentationPreset = CreatePreset(CameraViewType.Presentation);
            FixedPreset = CreatePreset(CameraViewType.Fixed);
            InteriorPreset = CreatePreset(CameraViewType.Interior);
        }

        public CameraViewPreset CreatePreset(CameraViewType viewType)
        {
            CameraViewPreset preset = ScriptableObject.CreateInstance<CameraViewPreset>();
            preset.Configure(viewType);
            objects.Add(preset);
            return preset;
        }

        public VehicleProfile CreateEmptyProfile()
        {
            VehicleProfile profile = ScriptableObject.CreateInstance<VehicleProfile>();
            objects.Add(profile);
            profile.EnsureProfileId();
            return profile;
        }

        public VehicleProfile CreateProfile(float halfWidth, float front, float rear)
        {
            VehicleProfile profile = CreateEmptyProfile();
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
            profile.AddWatchMarker(orbit, "Right", (2f * edge + side + side * 0.5f) / length, new Vector3(0f, 1f, 0f), true);
            VehicleConnectorAnchors anchors = new VehicleConnectorAnchors();
            anchors.Configure(new Vector3(0f, 0f, front + 0.5f), new Vector3(0f, 0f, -rear - 0.5f));
            profile.ConfigureConnectorAnchors(anchors);
            profile.FixedViewPose.Configure(new Vector3(2f, 3f, -8f), new Vector3(0f, 1f, 2f));
            profile.Seat.Configure(new Vector3(-0.5f, 1.8f, front - 1f), new Vector3(0.06f, 0.04f, 0.08f));
            profile.AddView(PresentationView, PresentationPreset, orbit.Id);
            profile.AddView(FixedView, FixedPreset, orbit.Id);
            profile.AddView(InteriorView, InteriorPreset, orbit.Id);
            return profile;
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

        public CameraSystemController CreateController(string name, Camera camera, VehicleCameraTarget target, string viewName)
        {
            GameObject controllerObject = new GameObject(name);
            objects.Add(controllerObject);
            CameraSystemController controller = controllerObject.AddComponent<CameraSystemController>();
            controller.SetUpdateMode(CameraUpdateMode.Manual);
            controller.Configure(camera, target, viewName);
            return controller;
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

        private BezierOrbitKnot CreateKnot(Vector3 anchor, Vector3 previous, Vector3 next)
        {
            BezierOrbitKnot knot = new BezierOrbitKnot();
            knot.Configure(anchor, anchor + (previous - anchor) / 3f, anchor + (next - anchor) / 3f);
            return knot;
        }
    }
}
