using System.Collections.Generic;
using UnityEngine;

namespace Gley.CameraSystem.Tests.PlayMode
{
    public class WatchMarkerAimReviewFixture : MonoBehaviour
    {
        private readonly List<GameObject> watchTargetObjects = new List<GameObject>();

        private Camera reviewCamera;
        private CameraSystemController controller;
        private CameraViewPreset viewPreset;
        private ClosedBezierOrbit orbit;
        private VehicleProfile vehicleProfile;
        [SerializeField, Min(0f)] private float horizontalTravelSpeed = 10f;
        [SerializeField, Range(-1f, 1f)] private float horizontalIntent;
        [SerializeField, Range(-1f, 1f)] private float heightIntent;

        public void UpdateReviewVisuals(float deltaTime)
        {
            if (controller == null)
            {
                return;
            }

            controller.SetHorizontalOrbitIntent(horizontalIntent);
            controller.SetHeightIntent(heightIntent);
            controller.UpdateCameraVisuals(deltaTime);
        }

        private void Start()
        {
            viewPreset = ScriptableObject.CreateInstance<CameraViewPreset>();
            viewPreset.Configure(CameraViewType.ExteriorPresentation);
            viewPreset.ConfigureOrbitTravel(horizontalTravelSpeed, 0f);
            vehicleProfile = CreateVehicleProfile();
            Transform vehicleBody = CreateChild("Vehicle Body");
            CreateVehicleVisual(vehicleBody);
            CreateWatchTarget(vehicleBody, "Rear watch target", new Vector3(10f, 2f, 8f), Color.red);
            CreateWatchTarget(vehicleBody, "Side watch target", new Vector3(14f, 2f, 5f), Color.blue);
            CreateWatchTarget(vehicleBody, "Front watch target", new Vector3(10f, 2f, 2f), Color.green);
            reviewCamera = CreateChild("Review Camera").gameObject.AddComponent<Camera>();
            controller = CreateChild("Review Controller").gameObject.AddComponent<CameraSystemController>();
            controller.enabled = false;
            controller.AssignCamera(reviewCamera);
            controller.AssignVehicle(vehicleBody, vehicleProfile);
            controller.SelectViewPreset(viewPreset);
            controller.Activate();
            orbit = new ClosedBezierOrbit(vehicleProfile.VehicleOrbit);
        }

        private void LateUpdate()
        {
            UpdateReviewVisuals(Time.unscaledDeltaTime);
        }

        private void OnDrawGizmos()
        {
            if (orbit == null || controller == null || !controller.IsActive)
            {
                return;
            }

            Gizmos.color = Color.yellow;
            Vector3 previousPosition = orbit.EvaluateWorldPosition(controller.VehicleBody, 0f);

            for (int sampleIndex = 1; sampleIndex <= 256; sampleIndex++)
            {
                Vector3 currentPosition = orbit.EvaluateWorldPosition(controller.VehicleBody, orbit.Length * sampleIndex / 256f);
                Gizmos.DrawLine(previousPosition, currentPosition);
                previousPosition = currentPosition;
            }

            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(reviewCamera.transform.position, 0.25f);
        }

        private VehicleProfile CreateVehicleProfile()
        {
            List<BezierOrbitKnot> knots = new List<BezierOrbitKnot>();
            knots.Add(CreateKnot(new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 10f), new Vector3(20f, 0f, 0f)));
            knots.Add(CreateKnot(new Vector3(20f, 0f, 0f), new Vector3(0f, 0f, 0f), new Vector3(20f, 0f, 10f)));
            knots.Add(CreateKnot(new Vector3(20f, 0f, 10f), new Vector3(20f, 0f, 0f), new Vector3(0f, 0f, 10f)));
            knots.Add(CreateKnot(new Vector3(0f, 0f, 10f), new Vector3(20f, 0f, 10f), new Vector3(0f, 0f, 0f)));
            VehicleOrbit vehicleOrbit = new VehicleOrbit();
            vehicleOrbit.Configure(knots, Quaternion.identity);
            vehicleOrbit.ConfigureWatchMarkers(CreateWatchMarkers());
            vehicleOrbit.ConfigureOffsetRanges(-2f, 2f, -2f, 2f);
            VehicleProfile profile = ScriptableObject.CreateInstance<VehicleProfile>();
            profile.ConfigureOrbit(vehicleOrbit);
            return profile;
        }

        private void CreateVehicleVisual(Transform vehicleBody)
        {
            GameObject vehicleVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            vehicleVisual.name = "Temporary vehicle visual";
            vehicleVisual.transform.SetParent(vehicleBody, false);
            vehicleVisual.transform.localPosition = new Vector3(10f, 1f, 5f);
            vehicleVisual.transform.localScale = new Vector3(8f, 2f, 4f);
        }

        private void CreateWatchTarget(Transform vehicleBody, string targetName, Vector3 localPosition, Color color)
        {
            GameObject target = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            target.name = targetName;
            target.transform.SetParent(vehicleBody, false);
            target.transform.localPosition = localPosition;
            target.transform.localScale = Vector3.one * 0.8f;
            target.GetComponent<Renderer>().material.color = color;
            watchTargetObjects.Add(target);
        }

        private Transform CreateChild(string objectName)
        {
            GameObject child = new GameObject(objectName);
            child.transform.SetParent(transform, false);
            return child.transform;
        }

        private BezierOrbitKnot CreateKnot(Vector3 anchor, Vector3 previousAnchor, Vector3 nextAnchor)
        {
            BezierOrbitKnot knot = new BezierOrbitKnot();
            knot.Configure(anchor, anchor + (previousAnchor - anchor) / 3f, anchor + (nextAnchor - anchor) / 3f);
            return knot;
        }

        private List<OrbitWatchMarker> CreateWatchMarkers()
        {
            List<OrbitWatchMarker> markers = new List<OrbitWatchMarker>();
            markers.Add(CreateWatchMarker(0f, new Vector3(10f, 2f, 8f)));
            markers.Add(CreateWatchMarker(0.25f, new Vector3(14f, 2f, 5f)));
            markers.Add(CreateWatchMarker(0.75f, new Vector3(10f, 2f, 2f)));
            return markers;
        }

        private OrbitWatchMarker CreateWatchMarker(float normalizedOrbitPosition, Vector3 watchPoint)
        {
            OrbitWatchMarker marker = new OrbitWatchMarker();
            marker.Configure(normalizedOrbitPosition, watchPoint);
            return marker;
        }

        private void OnDestroy()
        {
            for (int targetIndex = 0; targetIndex < watchTargetObjects.Count; targetIndex++)
            {
                Destroy(watchTargetObjects[targetIndex]);
            }

            Destroy(vehicleProfile);
            Destroy(viewPreset);
        }
    }
}
