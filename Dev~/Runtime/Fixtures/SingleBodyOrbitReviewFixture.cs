using System.Collections.Generic;
using UnityEngine;

namespace Gley.CameraSystem.Dev
{
    public class SingleBodyOrbitReviewFixture : MonoBehaviour
    {
        private readonly List<VehicleProfile> profiles = new List<VehicleProfile>();

        private CameraSystemController controller;
        private CameraViewPreset preset;
        private ClosedBezierOrbit standaloneOrbit;
        private Transform vehicleBody;
        [SerializeField] private Vector3 orientationEuler;
        [SerializeField, Min(0.1f)] private float size = 1f;
        [SerializeField, Min(0f)] private float speed = 10f;
        [SerializeField, Min(0f)] private float startResponseSeconds = 0.2f;
        [SerializeField, Range(-1f, 1f)] private float horizontalIntent;
        [SerializeField, Range(-1f, 1f)] private float heightIntent;
        [SerializeField] private bool curved;

        public CameraSystemController Controller => controller;

        public void UpdateReviewVisuals(float deltaTime)
        {
            if (controller == null)
            {
                return;
            }
            preset.ConfigureOrbitTravel(speed, startResponseSeconds);
            controller.SetHorizontalOrbitIntent(horizontalIntent);
            controller.SetHeightIntent(heightIntent);
            controller.UpdateCameraFrame(deltaTime);
        }

        private void Start()
        {
            preset = ScriptableObject.CreateInstance<CameraViewPreset>();
            preset.Configure(CameraViewType.Presentation);
            preset.ConfigureOrbitTravel(speed, startResponseSeconds);
            VehicleProfile profile = CreateProfile(size, curved);
            Transform body = CreateChild("Lead body", transform);
            Camera camera = CreateChild("Review camera", transform).gameObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.1f, 0.14f);
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 2000f;
            vehicleBody = body;
            VehicleCameraTarget target = body.gameObject.AddComponent<VehicleCameraTarget>();
            target.Configure(body, profile);
            controller = CreateChild("Review controller", transform).gameObject.AddComponent<CameraSystemController>();
            controller.SetUpdateMode(CameraUpdateMode.Manual);
            controller.Configure(camera, target, "Presentation");
            CreateMarkers(body, profile);
            standaloneOrbit = new ClosedBezierOrbit(profile.PrimaryOrbit);
            CameraCommandResult activation = controller.Activate();
            if (activation != CameraCommandResult.Accepted)
            {
                Debug.LogError(activation, this);
            }
        }

        private void LateUpdate()
        {
            UpdateReviewVisuals(Time.unscaledDeltaTime);
        }

        private void OnDrawGizmos()
        {
            if (controller == null || !controller.IsActive)
            {
                return;
            }
            float length = standaloneOrbit.Length;
            Gizmos.color = Color.yellow;
            Vector3 previous = EvaluatePosition(0f);
            for (int index = 1; index <= 256; index++)
            {
                Vector3 current = EvaluatePosition(length * index / 256f);
                Gizmos.DrawLine(previous, current);
                previous = current;
            }
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(controller.AssignedCamera.transform.position, size * 0.25f);
        }

        private VehicleProfile CreateProfile(float profileSize, bool bend)
        {
            Vector3[] anchors = { Vector3.zero, new Vector3(20f, 0f, 0f), new Vector3(20f, 0f, 10f), new Vector3(0f, 0f, 10f) };
            List<BezierOrbitKnot> knots = new List<BezierOrbitKnot>();
            for (int index = 0; index < anchors.Length; index++)
            {
                Vector3 incoming = Vector3.Lerp(anchors[index], anchors[(index + 3) % 4], 1f / 3f);
                Vector3 outgoing = Vector3.Lerp(anchors[index], anchors[(index + 1) % 4], 1f / 3f);
                if (bend && index == 0)
                {
                    outgoing += Vector3.back / 3f;
                }
                if (bend && index == 1)
                {
                    incoming += Vector3.back / 3f;
                }
                BezierOrbitKnot knot = new BezierOrbitKnot();
                knot.Configure(anchors[index] * profileSize, incoming * profileSize, outgoing * profileSize);
                knots.Add(knot);
            }
            VehicleProfile profile = ScriptableObject.CreateInstance<VehicleProfile>();
            VehicleOrbit orbit = profile.AddOrbit("Main");
            orbit.Configure(knots, Quaternion.Euler(orientationEuler));
            profile.AddWatchMarker(orbit, "Centre", 0f, orbit.OrientationAdjustment * (new Vector3(10f, 2f, 5f) * profileSize), false);
            OrbitRemovableSection front = new OrbitRemovableSection();
            OrbitRemovableSection rear = new OrbitRemovableSection();
            front.Configure(0f, 0.2f);
            rear.Configure(0.49f, 0.85f);
            orbit.ConfigureRemovableSections(front, rear);
            VehicleConnectorAnchors connectors = new VehicleConnectorAnchors();
            connectors.Configure(orbit.OrientationAdjustment * (new Vector3(10f, 0f, 0f) * profileSize), orbit.OrientationAdjustment * (new Vector3(10f, 0f, 10f) * profileSize));
            profile.AddView("Presentation", preset, orbit.Id);
            profile.ConfigureConnectorAnchors(connectors);
            profiles.Add(profile);
            return profile;
        }

        private Transform CreateChild(string objectName, Transform parent)
        {
            GameObject child = new GameObject(objectName);
            child.transform.SetParent(parent, false);
            return child.transform;
        }

        private void CreateMarkers(Transform body, VehicleProfile profile)
        {
            for (int index = 0; index < profile.PrimaryOrbit.Knots.Count; index++)
            {
                GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                marker.name = "Knot " + index;
                marker.transform.SetParent(body, false);
                marker.transform.localPosition = profile.PrimaryOrbit.OrientationAdjustment * profile.PrimaryOrbit.Knots[index].Anchor;
                marker.transform.localScale = Vector3.one * size * 0.4f;
            }
            GameObject model = GameObject.CreatePrimitive(PrimitiveType.Cube);
            model.name = "Temporary body marker";
            model.transform.SetParent(body, false);
            model.transform.localPosition = profile.PrimaryOrbit.WatchMarkers[0].WatchPointLocalPosition;
            model.transform.localScale = new Vector3(4f, 2f, 3f) * size;
        }

        private Vector3 EvaluatePosition(float distance)
        {
            return standaloneOrbit.EvaluateWorldPosition(vehicleBody, distance);
        }

        private void OnDestroy()
        {
            foreach (VehicleProfile profile in profiles)
            {
                Destroy(profile);
            }
            Destroy(preset);
        }
    }
}
