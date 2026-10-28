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
        public IEnumerator PublicOrbitCommands_AtEachMarker_AimCameraAtExactWatchTarget()
        {
            CameraViewPreset viewPreset = ScriptableObject.CreateInstance<CameraViewPreset>();
            viewPreset.Configure(CameraViewType.Presentation);
            viewPreset.ConfigureOrbitTravel(20f, 0f);
            Vector3 firstWatchPoint = new Vector3(4f, 2f, 3f);
            Vector3 secondWatchPoint = new Vector3(8f, 4f, 6f);
            Vector3 thirdWatchPoint = new Vector3(2f, 6f, 9f);
            VehicleProfile vehicleProfile = CreateVehicleProfile(firstWatchPoint, secondWatchPoint, thirdWatchPoint);
            GameObject vehicleBodyObject = new GameObject("Vehicle Body");
            GameObject cameraObject = new GameObject("Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            CameraSystemController controller = CreateController(cameraObject, camera, vehicleBodyObject.transform, vehicleProfile, viewPreset);

            Assert.AreEqual(CameraCommandResult.Accepted, controller.Activate());
            ReturnToOrbitStart(controller, 20f);
            AssertCameraAim(camera, new Vector3(0f, 0f, 0f), firstWatchPoint);

            controller.SetHorizontalOrbitIntent(1f);
            controller.UpdateCameraFrame(0.5f);
            AssertCameraAim(camera, new Vector3(10f, 0f, 0f), secondWatchPoint);

            controller.UpdateCameraFrame(0.5f);
            Assert.Less(Vector3.Distance(new Vector3(10f, 0f, 10f), camera.transform.position), 0.001f);

            controller.UpdateCameraFrame(0.5f);
            AssertCameraAim(camera, new Vector3(0f, 0f, 10f), thirdWatchPoint);

            controller.SetHorizontalOrbitIntent(0f);
            controller.SetHeightIntent(1f);
            controller.UpdateCameraFrame(0.5f);
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
            viewPreset.Configure(CameraViewType.Presentation);
            viewPreset.ConfigureOrbitTravel(20f, 0f);
            Vector3 firstProfileTarget = new Vector3(3f, 2f, 4f);
            Vector3 secondProfileTarget = new Vector3(7f, 5f, 6f);
            VehicleProfile firstProfile = CreateVehicleProfile(firstProfileTarget, Vector3.zero, Vector3.one);
            VehicleProfile secondProfile = CreateVehicleProfile(secondProfileTarget, Vector3.one, Vector3.zero);
            IReadOnlyList<OrbitWatchMarker> firstMarkers = firstProfile.PrimaryOrbit.WatchMarkers;
            IReadOnlyList<OrbitWatchMarker> secondMarkers = secondProfile.PrimaryOrbit.WatchMarkers;
            GameObject firstVehicleObject = new GameObject("First Vehicle");
            GameObject secondVehicleObject = new GameObject("Second Vehicle");
            GameObject firstCameraObject = new GameObject("First Camera");
            GameObject secondCameraObject = new GameObject("Second Camera");
            firstVehicleObject.transform.position = new Vector3(20f, 0f, 0f);
            secondVehicleObject.transform.position = new Vector3(-20f, 0f, 0f);
            Camera firstCamera = firstCameraObject.AddComponent<Camera>();
            Camera secondCamera = secondCameraObject.AddComponent<Camera>();
            CameraSystemController firstController = CreateController(firstCameraObject, firstCamera, firstVehicleObject.transform, firstProfile, viewPreset);
            CameraSystemController secondController = CreateController(secondCameraObject, secondCamera, secondVehicleObject.transform, secondProfile, viewPreset);

            Assert.AreEqual(CameraCommandResult.Accepted, firstController.Activate());
            Assert.AreEqual(CameraCommandResult.Accepted, secondController.Activate());
            ReturnToOrbitStart(firstController, 20f);
            ReturnToOrbitStart(secondController, 20f);
            AssertCameraAim(firstCamera, firstVehicleObject.transform.position, firstVehicleObject.transform.TransformPoint(firstProfileTarget));
            AssertCameraAim(secondCamera, secondVehicleObject.transform.position, secondVehicleObject.transform.TransformPoint(secondProfileTarget));
            Assert.AreEqual(3, firstMarkers.Count);
            Assert.AreEqual(3, secondMarkers.Count);
            Assert.AreSame(firstMarkers[0], firstProfile.PrimaryOrbit.WatchMarkers[0]);
            Assert.AreSame(secondMarkers[0], secondProfile.PrimaryOrbit.WatchMarkers[0]);
            Assert.AreEqual(firstProfileTarget, firstProfile.PrimaryOrbit.WatchMarkers[0].WatchPointLocalPosition);
            Assert.AreEqual(secondProfileTarget, secondProfile.PrimaryOrbit.WatchMarkers[0].WatchPointLocalPosition);

            Object.Destroy(firstCameraObject);
            Object.Destroy(secondCameraObject);
            Object.Destroy(firstVehicleObject);
            Object.Destroy(secondVehicleObject);
            Object.Destroy(firstProfile);
            Object.Destroy(secondProfile);
            Object.Destroy(viewPreset);
            yield return null;
        }

        private CameraSystemController CreateController(GameObject controllerObject, Camera camera, Transform body, VehicleProfile profile, CameraViewPreset preset)
        {
            profile.AddView("Presentation", preset, profile.PrimaryOrbit.Id);
            VehicleCameraTarget target = body.gameObject.AddComponent<VehicleCameraTarget>();
            target.Configure(body, profile);
            CameraSystemController controller = controllerObject.AddComponent<CameraSystemController>();
            controller.SetUpdateMode(CameraUpdateMode.Manual);
            controller.Configure(camera, target, "Presentation");
            return controller;
        }

        private void ReturnToOrbitStart(CameraSystemController controller, float speed)
        {
            float distance = controller.OrbitDistance;
            controller.SetHorizontalOrbitIntent(-Mathf.Sign(distance));
            controller.UpdateCameraFrame(Mathf.Abs(distance) / speed);
            controller.SetHorizontalOrbitIntent(0f);
        }

        private VehicleProfile CreateVehicleProfile(Vector3 firstWatchPoint, Vector3 secondWatchPoint, Vector3 thirdWatchPoint)
        {
            VehicleProfile vehicleProfile = ScriptableObject.CreateInstance<VehicleProfile>();
            VehicleOrbit vehicleOrbit = vehicleProfile.AddOrbit("Main");
            vehicleOrbit.Configure(CreateRectangleKnots(), Quaternion.identity);
            vehicleOrbit.ConfigureOffsetRanges(-2f, 2f, -2f, 2f);
            vehicleProfile.AddWatchMarker(vehicleOrbit, "First", 0f, firstWatchPoint, false);
            vehicleProfile.AddWatchMarker(vehicleOrbit, "Second", 0.25f, secondWatchPoint, false);
            vehicleProfile.AddWatchMarker(vehicleOrbit, "Third", 0.75f, thirdWatchPoint, false);
            return vehicleProfile;
        }

        private List<BezierOrbitKnot> CreateRectangleKnots()
        {
            List<BezierOrbitKnot> knots = new List<BezierOrbitKnot>();
            knots.Add(CreateKnot(new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 10f), new Vector3(10f, 0f, 0f)));
            knots.Add(CreateKnot(new Vector3(10f, 0f, 0f), new Vector3(0f, 0f, 0f), new Vector3(10f, 0f, 10f)));
            knots.Add(CreateKnot(new Vector3(10f, 0f, 10f), new Vector3(10f, 0f, 0f), new Vector3(0f, 0f, 10f)));
            knots.Add(CreateKnot(new Vector3(0f, 0f, 10f), new Vector3(10f, 0f, 10f), new Vector3(0f, 0f, 0f)));
            return knots;
        }

        private BezierOrbitKnot CreateKnot(Vector3 anchor, Vector3 previousAnchor, Vector3 nextAnchor)
        {
            BezierOrbitKnot knot = new BezierOrbitKnot();
            knot.Configure(anchor, anchor + (previousAnchor - anchor) / 3f, anchor + (nextAnchor - anchor) / 3f);
            return knot;
        }

        private void AssertCameraAim(Camera camera, Vector3 expectedCameraPosition, Vector3 expectedWatchPoint)
        {
            Vector3 expectedForward = (expectedWatchPoint - expectedCameraPosition).normalized;
            Assert.Less(Vector3.Distance(expectedCameraPosition, camera.transform.position), 0.001f);
            Assert.Less(Vector3.Angle(expectedForward, camera.transform.forward), 0.01f);
        }
    }
}
