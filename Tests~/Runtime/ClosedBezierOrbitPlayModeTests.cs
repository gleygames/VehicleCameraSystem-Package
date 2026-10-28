using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Gley.CameraSystem.Tests.PlayMode
{
    public class ClosedBezierOrbitPlayModeTests
    {
        [UnityTest]
        public IEnumerator ClosedBezierOrbitFollowsVehiclePitchAndRoll()
        {
            GameObject vehicleBodyObject = new GameObject("Vehicle Body");
            vehicleBodyObject.transform.SetPositionAndRotation(new Vector3(4f, 2f, -3f), Quaternion.Euler(20f, 35f, 15f));
            Quaternion orientationAdjustment = Quaternion.Euler(0f, 90f, 0f);
            VehicleOrbit vehicleOrbit = CreateTriangleOrbit(orientationAdjustment);
            ClosedBezierOrbit orbit = new ClosedBezierOrbit(vehicleOrbit);
            Vector3 expectedWorldPosition = vehicleBodyObject.transform.TransformPoint(orientationAdjustment * vehicleOrbit.Knots[0].Anchor);

            yield return null;

            Vector3 orbitWorldPosition = orbit.EvaluateWorldPosition(vehicleBodyObject.transform, 0f);

            Assert.AreEqual(OrbitValidationResult.Valid, orbit.ValidationResult);
            Assert.AreEqual(expectedWorldPosition, orbitWorldPosition);

            Object.Destroy(vehicleBodyObject);
        }

        [UnityTest]
        public IEnumerator OrbitMovementFollowsWatchMarkersAndKeepsInstancesIndependent()
        {
            CameraViewPreset orbitViewPreset = ScriptableObject.CreateInstance<CameraViewPreset>();
            orbitViewPreset.Configure(CameraViewType.Presentation);
            orbitViewPreset.ConfigureOrbitTravel(10f, 0f);
            VehicleProfile firstVehicleProfile = CreateVehicleProfile(orbitViewPreset, 20f, 10f);
            VehicleProfile secondVehicleProfile = CreateVehicleProfile(orbitViewPreset, 40f, 20f);
            GameObject firstVehicleBodyObject = new GameObject("First Vehicle Body");
            GameObject secondVehicleBodyObject = new GameObject("Second Vehicle Body");
            firstVehicleBodyObject.transform.SetPositionAndRotation(new Vector3(3f, 0f, 0f), Quaternion.Euler(10f, 20f, 5f));
            secondVehicleBodyObject.transform.SetPositionAndRotation(new Vector3(-6f, 0f, 4f), Quaternion.Euler(-5f, 30f, -10f));
            GameObject firstCameraObject = new GameObject("First Camera");
            Camera firstCamera = firstCameraObject.AddComponent<Camera>();
            GameObject secondCameraObject = new GameObject("Second Camera");
            Camera secondCamera = secondCameraObject.AddComponent<Camera>();
            GameObject firstControllerObject = new GameObject("First Camera System Controller");
            GameObject secondControllerObject = new GameObject("Second Camera System Controller");
            CameraSystemController firstController = CreateController(firstControllerObject, firstCamera, firstVehicleBodyObject.transform, firstVehicleProfile);
            CameraSystemController secondController = CreateController(secondControllerObject, secondCamera, secondVehicleBodyObject.transform, secondVehicleProfile);

            CameraCommandResult firstActivationResult = firstController.Activate();
            CameraCommandResult secondActivationResult = secondController.Activate();
            ReturnToOrbitStart(firstController, 10f);
            ReturnToOrbitStart(secondController, 10f);
            firstController.SetHorizontalOrbitIntent(1f);
            secondController.SetHorizontalOrbitIntent(-1f);
            firstController.UpdateCameraFrame(1f);
            secondController.UpdateCameraFrame(1f);
            yield return null;

            ClosedBezierOrbit firstOrbit = new ClosedBezierOrbit(firstVehicleProfile.PrimaryOrbit);
            ClosedBezierOrbit secondOrbit = new ClosedBezierOrbit(secondVehicleProfile.PrimaryOrbit);
            Vector3 expectedFirstPosition = firstOrbit.EvaluateWorldPosition(firstVehicleBodyObject.transform, 10f);
            Vector3 expectedSecondPosition = secondOrbit.EvaluateWorldPosition(secondVehicleBodyObject.transform, -10f);
            Vector3 expectedFirstWatchPoint = firstVehicleBodyObject.transform.TransformPoint(firstOrbit.EvaluateBodyLocalWatchPoint(10f));
            Vector3 expectedSecondWatchPoint = secondVehicleBodyObject.transform.TransformPoint(secondOrbit.EvaluateBodyLocalWatchPoint(-10f));
            Quaternion expectedFirstRotation = Quaternion.LookRotation(expectedFirstWatchPoint - expectedFirstPosition, Vector3.up);
            Quaternion expectedSecondRotation = Quaternion.LookRotation(expectedSecondWatchPoint - expectedSecondPosition, Vector3.up);

            Assert.AreEqual(CameraCommandResult.Accepted, firstActivationResult);
            Assert.AreEqual(CameraCommandResult.Accepted, secondActivationResult);
            AssertPosition(expectedFirstPosition, firstCamera.transform.position);
            AssertPosition(expectedSecondPosition, secondCamera.transform.position);
            Assert.Less(Quaternion.Angle(expectedFirstRotation, firstCamera.transform.rotation), 0.01f);
            Assert.Less(Quaternion.Angle(expectedSecondRotation, secondCamera.transform.rotation), 0.01f);
            Assert.AreEqual(10f, firstController.OrbitDistance, 0.0001f);
            Assert.AreEqual(-10f, secondController.OrbitDistance, 0.0001f);

            Object.Destroy(firstCameraObject);
            Object.Destroy(secondCameraObject);
            Object.Destroy(firstControllerObject);
            Object.Destroy(secondControllerObject);
            Object.Destroy(firstVehicleBodyObject);
            Object.Destroy(secondVehicleBodyObject);
            Object.Destroy(firstVehicleProfile);
            Object.Destroy(secondVehicleProfile);
            Object.Destroy(orbitViewPreset);
        }

        [UnityTest]
        public IEnumerator HeightAndZoomFollowVehicleFrameWithoutChangingFieldOfView()
        {
            CameraViewPreset orbitViewPreset = ScriptableObject.CreateInstance<CameraViewPreset>();
            orbitViewPreset.Configure(CameraViewType.Presentation);
            orbitViewPreset.ConfigureOrbitTravel(10f, 0f);
            orbitViewPreset.ConfigureOffsetTravel(1f, 1f);
            VehicleProfile vehicleProfile = CreateVehicleProfile(orbitViewPreset, 20f, 10f);
            vehicleProfile.PrimaryOrbit.ConfigureOffsetRanges(-1f, 1f, -1f, 1f);
            GameObject vehicleBodyObject = new GameObject("Vehicle Body");
            vehicleBodyObject.transform.SetPositionAndRotation(new Vector3(3f, 4f, 5f), Quaternion.Euler(20f, 30f, 15f));
            GameObject cameraObject = new GameObject("Assigned Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 47f;
            GameObject controllerObject = new GameObject("Camera System Controller");
            CameraSystemController controller = CreateController(controllerObject, camera, vehicleBodyObject.transform, vehicleProfile);

            CameraCommandResult activationResult = controller.Activate();
            ReturnToOrbitStart(controller, 10f);
            controller.SetHorizontalOrbitIntent(1f);
            controller.SetHeightIntent(1f);
            controller.SetZoomIntent(1f);
            controller.UpdateCameraFrame(1f);
            yield return null;

            ClosedBezierOrbit orbit = new ClosedBezierOrbit(vehicleProfile.PrimaryOrbit);
            Vector3 baseCameraPosition = orbit.EvaluateWorldPosition(vehicleBodyObject.transform, 10f);
            Vector3 inwardNormal = orbit.EvaluateBodyLocalInwardNormal(10f);
            Vector3 expectedCameraPosition = baseCameraPosition + vehicleBodyObject.transform.up + vehicleBodyObject.transform.TransformDirection(inwardNormal);
            Vector3 watchPoint = vehicleBodyObject.transform.TransformPoint(orbit.EvaluateBodyLocalWatchPoint(10f));
            Quaternion expectedRotation = Quaternion.LookRotation(watchPoint - expectedCameraPosition, Vector3.up);

            Assert.AreEqual(CameraCommandResult.Accepted, activationResult);
            Assert.AreEqual(1f, controller.HeightOffset, 0.0001f);
            Assert.AreEqual(1f, controller.ZoomOffset, 0.0001f);
            AssertPosition(expectedCameraPosition, camera.transform.position);
            Assert.Less(Quaternion.Angle(expectedRotation, camera.transform.rotation), 0.01f);
            Assert.AreEqual(47f, camera.fieldOfView, 0.0001f);

            Object.Destroy(cameraObject);
            Object.Destroy(controllerObject);
            Object.Destroy(vehicleBodyObject);
            Object.Destroy(vehicleProfile);
            Object.Destroy(orbitViewPreset);
        }

        [UnityTest]
        public IEnumerator RemovableSectionsRemainIndependentWhenBodiesMove()
        {
            VehicleOrbit firstVehicleOrbit = CreateRectangleOrbit(20f, 10f);
            firstVehicleOrbit.ConfigureRemovableSections(CreateRemovableSection(0.1f, 0.2f), CreateRemovableSection(0.6f, 0.7f));
            VehicleOrbit secondVehicleOrbit = CreateRectangleOrbit(40f, 20f);
            secondVehicleOrbit.ConfigureRemovableSections(CreateRemovableSection(0.2f, 0.3f), CreateRemovableSection(0.7f, 0.8f));
            ClosedBezierOrbit firstOrbit = new ClosedBezierOrbit(firstVehicleOrbit);
            ClosedBezierOrbit secondOrbit = new ClosedBezierOrbit(secondVehicleOrbit);
            GameObject firstVehicleBodyObject = new GameObject("First Vehicle Body");
            GameObject secondVehicleBodyObject = new GameObject("Second Vehicle Body");
            firstVehicleBodyObject.transform.SetPositionAndRotation(new Vector3(3f, 2f, 0f), Quaternion.Euler(0f, 90f, 0f));
            secondVehicleBodyObject.transform.SetPositionAndRotation(new Vector3(-4f, 1f, 5f), Quaternion.Euler(0f, -90f, 0f));

            yield return null;

            Vector3 firstBoundary = firstVehicleBodyObject.transform.TransformPoint(firstOrbit.EvaluateBodyLocalRemovableSectionStart(OrbitAttachmentEnd.Front));
            Vector3 secondBoundary = secondVehicleBodyObject.transform.TransformPoint(secondOrbit.EvaluateBodyLocalRemovableSectionStart(OrbitAttachmentEnd.Rear));

            Assert.AreEqual(OrbitRemovableSectionValidationResult.Valid, firstOrbit.RemovableSectionValidationResult);
            Assert.AreEqual(OrbitRemovableSectionValidationResult.Valid, secondOrbit.RemovableSectionValidationResult);
            Assert.Less(Vector3.Distance(new Vector3(3f, 2f, -6f), firstBoundary), 0.001f);
            Assert.Less(Vector3.Distance(new Vector3(-24f, 1f, 21f), secondBoundary), 0.001f);

            Object.Destroy(firstVehicleBodyObject);
            Object.Destroy(secondVehicleBodyObject);
        }

        [UnityTest]
        public IEnumerator OrbitAttachmentMergeRemainsIndependentAcrossProfileInstances()
        {
            VehicleProfile firstVehicleProfile = ScriptableObject.CreateInstance<VehicleProfile>();
            VehicleProfile secondVehicleProfile = ScriptableObject.CreateInstance<VehicleProfile>();
            VehicleOrbit firstVehicleOrbit = firstVehicleProfile.AddOrbit("Main");
            VehicleOrbit secondVehicleOrbit = secondVehicleProfile.AddOrbit("Main");
            firstVehicleOrbit.Configure(CreateRectangleKnots(20f, 10f), Quaternion.identity);
            secondVehicleOrbit.Configure(CreateRectangleKnots(40f, 20f), Quaternion.identity);
            secondVehicleOrbit.ConfigureAttachmentMerge(false);

            yield return null;

            Assert.IsTrue(firstVehicleProfile.PrimaryOrbit.MergeWhenAttached);
            Assert.IsFalse(secondVehicleProfile.PrimaryOrbit.MergeWhenAttached);

            Object.Destroy(firstVehicleProfile);
            Object.Destroy(secondVehicleProfile);
        }

        [UnityTest]
        public IEnumerator AttachmentRemapUsesTheStraightReferenceOrbitAtRuntime()
        {
            VehicleProfile frontProfile = CreateConnectorProfile(20f, 10f, new Vector3(10f, 0f, 0f), new Vector3(10f, 0f, 10f));
            VehicleProfile rearProfile = CreateConnectorProfile(40f, 20f, new Vector3(20f, 0f, 0f), new Vector3(20f, 0f, 20f));
            frontProfile.PrimaryOrbit.ConfigureRemovableSections(CreateRemovableSection(0f, 0.2f), CreateRemovableSection(0.5f, 5f / 6f));
            rearProfile.PrimaryOrbit.ConfigureRemovableSections(CreateRemovableSection(0f, 1f / 3f), CreateRemovableSection(0.4f, 0.6f));
            GameObject frontBodyObject = new GameObject("Front Body");
            GameObject rearBodyObject = new GameObject("Rear Body");
            rearBodyObject.transform.SetPositionAndRotation(new Vector3(-4f, 0f, 7f), Quaternion.Euler(0f, 40f, 0f));
            ChainOrbit oldOrbit = new ChainOrbit(new ChainLayout(new VehicleProfile[] { frontProfile }, 0, frontProfile.PrimaryOrbit), new Transform[] { frontBodyObject.transform }, 0);
            ChainOrbit combinedOrbit = new ChainOrbit(new ChainLayout(new VehicleProfile[] { frontProfile, rearProfile }, 0, frontProfile.PrimaryOrbit), new Transform[] { frontBodyObject.transform, rearBodyObject.transform }, 0);
            OrbitRemapper remapper = new OrbitRemapper();

            float firstRemappedDistance;
            OrbitRemapResult firstResult = remapper.Remap(oldOrbit, 40f, combinedOrbit, out firstRemappedDistance);
            Vector3 firstRemappedPosition = combinedOrbit.EvaluateLeadBodyLocalPosition(firstRemappedDistance);
            yield return null;

            rearBodyObject.transform.SetPositionAndRotation(new Vector3(9f, 0f, -3f), Quaternion.Euler(0f, -50f, 0f));
            float secondRemappedDistance;
            OrbitRemapResult secondResult = remapper.Remap(oldOrbit, 40f, combinedOrbit, out secondRemappedDistance);
            Vector3 secondRemappedPosition = combinedOrbit.EvaluateLeadBodyLocalPosition(secondRemappedDistance);

            Assert.AreEqual(OrbitRemapResult.MovedToRearFallback, firstResult);
            Assert.AreEqual(OrbitRemapResult.MovedToRearFallback, secondResult);
            Assert.AreEqual(firstRemappedDistance, secondRemappedDistance, 0.0001f);
            Assert.Less(Vector3.Distance(firstRemappedPosition, secondRemappedPosition), 0.0001f);

            Object.Destroy(frontBodyObject);
            Object.Destroy(rearBodyObject);
            Object.Destroy(frontProfile);
            Object.Destroy(rearProfile);
        }

        private CameraSystemController CreateController(GameObject controllerObject, Camera camera, Transform body, VehicleProfile profile)
        {
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

        private VehicleProfile CreateVehicleProfile(CameraViewPreset orbitViewPreset, float width, float depth)
        {
            VehicleProfile vehicleProfile = ScriptableObject.CreateInstance<VehicleProfile>();
            VehicleOrbit vehicleOrbit = vehicleProfile.AddOrbit("Main");
            vehicleOrbit.Configure(CreateRectangleKnots(width, depth), Quaternion.identity);
            AddWatchMarkers(vehicleProfile, vehicleOrbit, width, depth);
            vehicleProfile.AddView("Presentation", orbitViewPreset, vehicleOrbit.Id);
            return vehicleProfile;
        }

        private VehicleProfile CreateConnectorProfile(float width, float depth, Vector3 frontAnchorPosition, Vector3 rearAnchorPosition)
        {
            VehicleProfile vehicleProfile = ScriptableObject.CreateInstance<VehicleProfile>();
            VehicleOrbit vehicleOrbit = vehicleProfile.AddOrbit("Main");
            vehicleOrbit.Configure(CreateRectangleKnots(width, depth), Quaternion.identity);
            vehicleOrbit.ConfigureRemovableSections(CreateRemovableSection(0f, 0.2f), CreateRemovableSection(0.4f, 0.6f));
            VehicleConnectorAnchors connectorAnchors = new VehicleConnectorAnchors();
            connectorAnchors.Configure(frontAnchorPosition, rearAnchorPosition);
            vehicleProfile.ConfigureConnectorAnchors(connectorAnchors);
            return vehicleProfile;
        }

        private VehicleOrbit CreateRectangleOrbit(float width, float depth)
        {
            VehicleOrbit vehicleOrbit = new VehicleOrbit();
            vehicleOrbit.Configure(CreateRectangleKnots(width, depth), Quaternion.identity);
            return vehicleOrbit;
        }

        private List<BezierOrbitKnot> CreateRectangleKnots(float width, float depth)
        {
            Vector3 firstAnchor = Vector3.zero;
            Vector3 secondAnchor = new Vector3(width, 0f, 0f);
            Vector3 thirdAnchor = new Vector3(width, 0f, depth);
            Vector3 fourthAnchor = new Vector3(0f, 0f, depth);
            List<BezierOrbitKnot> knots = new List<BezierOrbitKnot>();

            knots.Add(CreateKnot(firstAnchor, fourthAnchor, secondAnchor));
            knots.Add(CreateKnot(secondAnchor, firstAnchor, thirdAnchor));
            knots.Add(CreateKnot(thirdAnchor, secondAnchor, fourthAnchor));
            knots.Add(CreateKnot(fourthAnchor, thirdAnchor, firstAnchor));
            return knots;
        }

        private OrbitRemovableSection CreateRemovableSection(float normalizedStartPosition, float normalizedEndPosition)
        {
            OrbitRemovableSection section = new OrbitRemovableSection();
            section.Configure(normalizedStartPosition, normalizedEndPosition);
            return section;
        }

        private void AddWatchMarkers(VehicleProfile vehicleProfile, VehicleOrbit vehicleOrbit, float width, float depth)
        {
            vehicleProfile.AddWatchMarker(vehicleOrbit, "First", 0f, new Vector3(width * 0.5f, 3f, depth * 0.5f), false);
            vehicleProfile.AddWatchMarker(vehicleOrbit, "Second", 0.5f, new Vector3(width * 0.5f, 4f, depth * 0.5f), false);
        }

        private float GetSegmentStartDistance(IReadOnlyList<AssembledBezierOrbitSegment> segments, int segmentIndex)
        {
            float distance = 0f;

            for (int index = 0; index < segmentIndex; index++)
            {
                distance += GetSegmentLength(segments[index]);
            }

            return distance;
        }

        private float GetSegmentLength(AssembledBezierOrbitSegment segment)
        {
            float length = 0f;
            Vector3 previousPosition = segment.EvaluatePosition(0f);

            for (int sampleIndex = 1; sampleIndex <= 32; sampleIndex++)
            {
                float segmentT = (float)sampleIndex / 32f;
                Vector3 currentPosition = segment.EvaluatePosition(segmentT);
                length += Vector3.Distance(previousPosition, currentPosition);
                previousPosition = currentPosition;
            }

            return length;
        }

        private void AssertPosition(Vector3 expectedPosition, Vector3 actualPosition)
        {
            Assert.Less(Vector3.Distance(expectedPosition, actualPosition), 0.0001f);
        }

        private VehicleOrbit CreateTriangleOrbit(Quaternion orientationAdjustment)
        {
            Vector3 firstAnchor = new Vector3(0f, 0f, -5f);
            Vector3 secondAnchor = new Vector3(5f, 0f, 5f);
            Vector3 thirdAnchor = new Vector3(-5f, 0f, 5f);
            List<BezierOrbitKnot> knots = new List<BezierOrbitKnot>();

            knots.Add(CreateKnot(firstAnchor, thirdAnchor, secondAnchor));
            knots.Add(CreateKnot(secondAnchor, firstAnchor, thirdAnchor));
            knots.Add(CreateKnot(thirdAnchor, secondAnchor, firstAnchor));

            VehicleOrbit vehicleOrbit = new VehicleOrbit();
            vehicleOrbit.Configure(knots, orientationAdjustment);
            return vehicleOrbit;
        }

        private BezierOrbitKnot CreateKnot(Vector3 anchor, Vector3 previousAnchor, Vector3 nextAnchor)
        {
            Vector3 incomingControlPoint = anchor + (previousAnchor - anchor) / 3f;
            Vector3 outgoingControlPoint = anchor + (nextAnchor - anchor) / 3f;
            BezierOrbitKnot knot = new BezierOrbitKnot();
            knot.Configure(anchor, incomingControlPoint, outgoingControlPoint);
            return knot;
        }
    }
}
