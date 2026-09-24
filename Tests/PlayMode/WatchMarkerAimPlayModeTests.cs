using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Gley.CameraSystem.Tests.PlayMode
{
    public class WatchMarkerAimPlayModeTests
    {
        [UnityTest]
        public IEnumerator PublicOrbitCommandsAimTheActualCameraAtExactAndIntermediateWatchTargets()
        {
            CameraViewPreset viewPreset = ScriptableObject.CreateInstance<CameraViewPreset>();
            viewPreset.Configure(CameraViewType.ExteriorPresentation);
            viewPreset.ConfigureOrbitTravel(20f, 0f);
            Vector3 firstWatchPoint = new Vector3(4f, 2f, 3f);
            Vector3 secondWatchPoint = new Vector3(8f, 4f, 6f);
            Vector3 thirdWatchPoint = new Vector3(2f, 6f, 9f);
            VehicleProfile vehicleProfile = CreateVehicleProfile(firstWatchPoint, secondWatchPoint, thirdWatchPoint);
            GameObject vehicleBodyObject = new GameObject("Vehicle Body");
            GameObject cameraObject = new GameObject("Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            CameraSystemController controller = cameraObject.AddComponent<CameraSystemController>();
            controller.AssignCamera(camera);
            controller.AssignVehicle(vehicleBodyObject.transform, vehicleProfile);
            controller.SelectViewPreset(viewPreset);

            Assert.AreEqual(CameraSystemActivationResult.Succeeded, controller.Activate());
            AssertCameraAim(camera, new Vector3(0f, 0f, 0f), firstWatchPoint);

            controller.SetHorizontalOrbitIntent(1f);
            controller.UpdateCameraVisuals(0.5f);
            AssertCameraAim(camera, new Vector3(10f, 0f, 0f), secondWatchPoint);

            controller.UpdateCameraVisuals(0.5f);
            AssertCameraAim(camera, new Vector3(10f, 0f, 10f), Vector3.Lerp(secondWatchPoint, thirdWatchPoint, 0.5f));

            controller.UpdateCameraVisuals(0.5f);
            AssertCameraAim(camera, new Vector3(0f, 0f, 10f), thirdWatchPoint);

            controller.SetHorizontalOrbitIntent(0f);
            controller.SetHeightIntent(1f);
            controller.UpdateCameraVisuals(0.5f);
            AssertCameraAim(camera, new Vector3(0f, 1f, 10f), thirdWatchPoint);

            Object.Destroy(cameraObject);
            Object.Destroy(vehicleBodyObject);
            Object.Destroy(vehicleProfile);
            Object.Destroy(viewPreset);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ProfilesSharingAPresetKeepIndependentWatchTracksAndSerializedData()
        {
            CameraViewPreset viewPreset = ScriptableObject.CreateInstance<CameraViewPreset>();
            viewPreset.Configure(CameraViewType.ExteriorPresentation);
            viewPreset.ConfigureOrbitTravel(20f, 0f);
            Vector3 firstProfileTarget = new Vector3(3f, 2f, 4f);
            Vector3 secondProfileTarget = new Vector3(7f, 5f, 6f);
            VehicleProfile firstProfile = CreateVehicleProfile(firstProfileTarget, Vector3.zero, Vector3.one);
            VehicleProfile secondProfile = CreateVehicleProfile(secondProfileTarget, Vector3.one, Vector3.zero);
            IReadOnlyList<OrbitWatchMarker> firstMarkers = firstProfile.VehicleOrbit.WatchMarkers;
            IReadOnlyList<OrbitWatchMarker> secondMarkers = secondProfile.VehicleOrbit.WatchMarkers;
            GameObject firstVehicleObject = new GameObject("First Vehicle");
            GameObject secondVehicleObject = new GameObject("Second Vehicle");
            GameObject firstCameraObject = new GameObject("First Camera");
            GameObject secondCameraObject = new GameObject("Second Camera");
            firstVehicleObject.transform.position = new Vector3(20f, 0f, 0f);
            secondVehicleObject.transform.position = new Vector3(-20f, 0f, 0f);
            Camera firstCamera = firstCameraObject.AddComponent<Camera>();
            Camera secondCamera = secondCameraObject.AddComponent<Camera>();
            CameraSystemController firstController = firstCameraObject.AddComponent<CameraSystemController>();
            CameraSystemController secondController = secondCameraObject.AddComponent<CameraSystemController>();
            firstController.AssignCamera(firstCamera);
            firstController.AssignVehicle(firstVehicleObject.transform, firstProfile);
            firstController.SelectViewPreset(viewPreset);
            secondController.AssignCamera(secondCamera);
            secondController.AssignVehicle(secondVehicleObject.transform, secondProfile);
            secondController.SelectViewPreset(viewPreset);

            Assert.AreEqual(CameraSystemActivationResult.Succeeded, firstController.Activate());
            Assert.AreEqual(CameraSystemActivationResult.Succeeded, secondController.Activate());
            AssertCameraAim(firstCamera, firstVehicleObject.transform.position, firstVehicleObject.transform.TransformPoint(firstProfileTarget));
            AssertCameraAim(secondCamera, secondVehicleObject.transform.position, secondVehicleObject.transform.TransformPoint(secondProfileTarget));
            Assert.AreEqual(3, firstMarkers.Count);
            Assert.AreEqual(3, secondMarkers.Count);
            Assert.AreSame(firstMarkers[0], firstProfile.VehicleOrbit.WatchMarkers[0]);
            Assert.AreSame(secondMarkers[0], secondProfile.VehicleOrbit.WatchMarkers[0]);
            Assert.AreEqual(firstProfileTarget, firstProfile.VehicleOrbit.WatchMarkers[0].WatchPointLocalPosition);
            Assert.AreEqual(secondProfileTarget, secondProfile.VehicleOrbit.WatchMarkers[0].WatchPointLocalPosition);

            Object.Destroy(firstCameraObject);
            Object.Destroy(secondCameraObject);
            Object.Destroy(firstVehicleObject);
            Object.Destroy(secondVehicleObject);
            Object.Destroy(firstProfile);
            Object.Destroy(secondProfile);
            Object.Destroy(viewPreset);
            yield return null;
        }

        private VehicleProfile CreateVehicleProfile(Vector3 firstWatchPoint, Vector3 secondWatchPoint, Vector3 thirdWatchPoint)
        {
            VehicleOrbit vehicleOrbit = CreateRectangleOrbit();
            vehicleOrbit.ConfigureWatchMarkers(CreateWatchMarkers(
                CreateWatchMarker(0f, firstWatchPoint),
                CreateWatchMarker(0.25f, secondWatchPoint),
                CreateWatchMarker(0.75f, thirdWatchPoint)));
            VehicleProfile vehicleProfile = ScriptableObject.CreateInstance<VehicleProfile>();
            vehicleProfile.ConfigureOrbit(vehicleOrbit);
            return vehicleProfile;
        }

        private VehicleOrbit CreateRectangleOrbit()
        {
            List<BezierOrbitKnot> knots = new List<BezierOrbitKnot>();
            knots.Add(CreateKnot(new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 10f), new Vector3(10f, 0f, 0f)));
            knots.Add(CreateKnot(new Vector3(10f, 0f, 0f), new Vector3(0f, 0f, 0f), new Vector3(10f, 0f, 10f)));
            knots.Add(CreateKnot(new Vector3(10f, 0f, 10f), new Vector3(10f, 0f, 0f), new Vector3(0f, 0f, 10f)));
            knots.Add(CreateKnot(new Vector3(0f, 0f, 10f), new Vector3(10f, 0f, 10f), new Vector3(0f, 0f, 0f)));
            VehicleOrbit vehicleOrbit = new VehicleOrbit();
            vehicleOrbit.Configure(knots, Quaternion.identity);
            vehicleOrbit.ConfigureOffsetRanges(-2f, 2f, -2f, 2f);
            return vehicleOrbit;
        }

        private BezierOrbitKnot CreateKnot(Vector3 anchor, Vector3 previousAnchor, Vector3 nextAnchor)
        {
            BezierOrbitKnot knot = new BezierOrbitKnot();
            knot.Configure(anchor, anchor + (previousAnchor - anchor) / 3f, anchor + (nextAnchor - anchor) / 3f);
            return knot;
        }

        private List<OrbitWatchMarker> CreateWatchMarkers(params OrbitWatchMarker[] markers)
        {
            List<OrbitWatchMarker> watchMarkers = new List<OrbitWatchMarker>();

            for (int markerIndex = 0; markerIndex < markers.Length; markerIndex++)
            {
                watchMarkers.Add(markers[markerIndex]);
            }

            return watchMarkers;
        }

        private OrbitWatchMarker CreateWatchMarker(float normalizedOrbitPosition, Vector3 watchPoint)
        {
            OrbitWatchMarker marker = new OrbitWatchMarker();
            marker.Configure(normalizedOrbitPosition, watchPoint);
            return marker;
        }

        private void AssertCameraAim(Camera camera, Vector3 expectedCameraPosition, Vector3 expectedWatchPoint)
        {
            Vector3 expectedForward = (expectedWatchPoint - expectedCameraPosition).normalized;
            Assert.Less(Vector3.Distance(expectedCameraPosition, camera.transform.position), 0.001f);
            Assert.Less(Vector3.Angle(expectedForward, camera.transform.forward), 0.01f);
        }
    }
}
