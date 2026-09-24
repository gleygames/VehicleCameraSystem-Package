using System.Collections.Generic;
using UnityEngine;

namespace Gley.CameraSystem.Tests.PlayMode
{
    public class SingleBodyOrbitReviewFixture : MonoBehaviour
    {
        private readonly List<VehicleProfile> profiles = new List<VehicleProfile>();

        private CameraSystemController controller;
        private CameraViewPreset preset;
        private ClosedBezierOrbit standaloneOrbit;
        private TwoBodyClosedBezierOrbit attachedOrbit;
        [SerializeField] private Vector3 orientationEuler;
        [SerializeField, Min(0.1f)] private float size = 1f;
        [SerializeField, Min(0f)] private float speed = 10f;
        [SerializeField, Min(0f)] private float startResponseSeconds = 0.2f;
        [SerializeField, Range(-1f, 1f)] private float horizontalIntent;
        [SerializeField] private bool curved;
        [SerializeField] private bool attachRearBody;

        public CameraSystemController Controller => controller;

        public void UpdateReviewVisuals(float deltaTime)
        {
            if (controller == null)
            {
                return;
            }
            preset.ConfigureOrbitTravel(speed, startResponseSeconds);
            controller.SetHorizontalOrbitIntent(horizontalIntent);
            controller.UpdateCameraVisuals(deltaTime);
        }

        private void Start()
        {
            preset = ScriptableObject.CreateInstance<CameraViewPreset>();
            preset.Configure(CameraViewType.ExteriorPresentation);
            preset.ConfigureOrbitTravel(speed, startResponseSeconds);
            VehicleProfile profile = CreateProfile(size, curved);
            Transform body = CreateChild("Lead body", transform);
            Camera camera = CreateChild("Review camera", transform).gameObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.1f, 0.14f);
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 2000f;
            controller = CreateChild("Review controller", transform).gameObject.AddComponent<CameraSystemController>();
            controller.enabled = false;
            controller.AssignCamera(camera);
            controller.AssignVehicle(body, profile);
            controller.SelectViewPreset(preset);
            CreateMarkers(body, profile);
            if (attachRearBody)
            {
                VehicleProfile rearProfile = CreateProfile(size * 2f, false);
                rearProfile.VehicleOrbit.FrontRemovableSection.Configure(0f, 1f / 3f);
                Transform rearBody = CreateChild("Rear body", body);
                rearBody.localPosition = new Vector3(-10f, 0f, 10f) * size;
                CreateMarkers(rearBody, rearProfile);
                CameraSystemAttachmentResult attachment = controller.AttachRearBody(rearBody, rearProfile);
                if (attachment != CameraSystemAttachmentResult.Attached)
                {
                    Debug.LogError(attachment, this);
                    return;
                }
                attachedOrbit = new TwoBodyClosedBezierOrbit(profile, rearProfile);
            }
            else
            {
                standaloneOrbit = new ClosedBezierOrbit(profile.VehicleOrbit);
            }
            CameraSystemActivationResult activation = controller.Activate();
            if (activation != CameraSystemActivationResult.Succeeded)
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
            float length;
            if (attachedOrbit != null)
            {
                length = attachedOrbit.Length;
            }
            else
            {
                length = standaloneOrbit.Length;
            }
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
            if (attachedOrbit != null)
            {
                foreach (AssembledBezierOrbitSegment segment in attachedOrbit.Segments)
                {
                    Gizmos.DrawWireSphere(controller.VehicleBody.TransformPoint(segment.StartPosition), size * 0.35f);
                }
            }
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
            VehicleOrbit orbit = new VehicleOrbit();
            orbit.Configure(knots, Quaternion.Euler(orientationEuler));
            OrbitWatchMarker marker = new OrbitWatchMarker();
            marker.Configure(0f, new Vector3(10f, 2f, 5f) * profileSize);
            orbit.ConfigureWatchMarkers(new List<OrbitWatchMarker> { marker });
            OrbitRemovableSection front = new OrbitRemovableSection();
            OrbitRemovableSection rear = new OrbitRemovableSection();
            front.Configure(0f, 0.2f);
            rear.Configure(0.49f, 0.85f);
            orbit.ConfigureRemovableSections(front, rear);
            VehicleConnectorAnchors connectors = new VehicleConnectorAnchors();
            connectors.Configure(new Vector3(10f, 0f, 0f) * profileSize, new Vector3(10f, 0f, 10f) * profileSize);
            VehicleProfile profile = ScriptableObject.CreateInstance<VehicleProfile>();
            profile.ConfigureOrbit(orbit);
            profile.ConfigureOrbitView(preset);
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
            for (int index = 0; index < profile.VehicleOrbit.Knots.Count; index++)
            {
                GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                marker.name = "Knot " + index;
                marker.transform.SetParent(body, false);
                marker.transform.localPosition = profile.VehicleOrbit.OrientationAdjustment * profile.VehicleOrbit.Knots[index].Anchor;
                marker.transform.localScale = Vector3.one * size * 0.4f;
            }
            GameObject model = GameObject.CreatePrimitive(PrimitiveType.Cube);
            model.name = "Temporary body marker";
            model.transform.SetParent(body, false);
            model.transform.localPosition = profile.VehicleOrbit.WatchMarkers[0].WatchPointLocalPosition;
            model.transform.localScale = new Vector3(4f, 2f, 3f) * size;
        }

        private Vector3 EvaluatePosition(float distance)
        {
            if (attachedOrbit != null)
            {
                return attachedOrbit.EvaluateWorldPosition(controller.VehicleBody, distance);
            }
            return standaloneOrbit.EvaluateWorldPosition(controller.VehicleBody, distance);
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
