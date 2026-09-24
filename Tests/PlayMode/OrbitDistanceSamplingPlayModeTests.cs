using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Gley.CameraSystem.Tests.PlayMode
{
    public class OrbitDistanceSamplingPlayModeTests
    {
        private readonly List<Object> objects = new List<Object>();

        [TestCase(false)]
        [TestCase(true)]
        public void PublicCameraCrossesEveryKnotConnectorAndWrapWithoutJumps(bool attached)
        {
            CameraSystemController controller = CreateController(1f, 1f, 0f);
            List<Vector3> corners = new List<Vector3>();
            if (attached)
            {
                VehicleProfile rear = CreateProfile(2f);
                rear.VehicleOrbit.FrontRemovableSection.Configure(0f, 1f / 3f);
                GameObject rearBody = CreateObject("Rear body");
                Assert.AreEqual(CameraSystemAttachmentResult.Attached, controller.AttachRearBody(rearBody.transform, rear));
                TwoBodyClosedBezierOrbit geometry = new TwoBodyClosedBezierOrbit(controller.VehicleProfile, rear);
                foreach (AssembledBezierOrbitSegment segment in geometry.Segments)
                {
                    corners.Add(segment.StartPosition);
                }
                Assert.IsTrue(controller.UsesAttachedOrbit);
            }
            else
            {
                corners.Add(Vector3.zero);
                corners.Add(new Vector3(20f, 0f, 0f));
                corners.Add(new Vector3(20f, 0f, 10f));
                corners.Add(new Vector3(0f, 0f, 10f));
            }

            float length = PolygonLength(corners);
            float boundary = 0f;
            for (int index = 0; index <= corners.Count; index++)
            {
                for (int wrap = -2; wrap <= 2; wrap++)
                {
                    float distance = boundary + wrap * length;
                    MoveTo(controller, distance - 0.01f);
                    Vector3 before = controller.AssignedCamera.transform.position;
                    AssertPosition(PolygonPosition(corners, distance - 0.01f), before, 0.001f);
                    controller.SetHorizontalOrbitIntent(1f);
                    controller.UpdateCameraVisuals(0.01f);
                    AssertPosition(PolygonPosition(corners, distance), controller.AssignedCamera.transform.position, 0.001f);
                    controller.UpdateCameraVisuals(0.01f);
                    Vector3 after = controller.AssignedCamera.transform.position;
                    AssertPosition(PolygonPosition(corners, distance + 0.01f), after, 0.001f);
                    Assert.Less(Vector3.Distance(before, after), 0.0205f);
                    Assert.Greater(Vector3.Dot(after - before, PolygonPosition(corners, distance + 0.01f) - PolygonPosition(corners, distance - 0.01f)), 0f);
                    controller.SetHorizontalOrbitIntent(0f);
                    controller.UpdateCameraVisuals(0.25f);
                    Assert.AreEqual(after, controller.AssignedCamera.transform.position);
                }
                if (index < corners.Count)
                {
                    boundary += Vector3.Distance(corners[index], corners[(index + 1) % corners.Count]);
                }
            }
        }

        [UnityTest]
        public IEnumerator CurvedCameraTravelRemainsContinuousAndForwardThroughTwoLoops()
        {
            CameraSystemController controller = CreateController(1f, 10f, 0f);
            controller.Release();
            controller.VehicleProfile.ConfigureOrbit(CreateCircle());
            Assert.AreEqual(CameraSystemActivationResult.Succeeded, controller.Activate());
            controller.SetHorizontalOrbitIntent(1f);
            Vector3 before = controller.AssignedCamera.transform.position;
            for (int step = 0; step < 1300; step++)
            {
                controller.UpdateCameraVisuals(0.01f);
                Vector3 after = controller.AssignedCamera.transform.position;
                Assert.AreEqual(10f, after.magnitude, 0.004f);
                Assert.AreEqual(0.1f, Vector3.Distance(before, after), 0.001f);
                Assert.Greater(Vector3.Dot(after - before, new Vector3(-before.z, 0f, before.x)), 0f);
                before = after;
                if (step % 100 == 0)
                {
                    yield return null;
                }
            }
        }

        [TestCase(0.01f)]
        [TestCase(0.02f)]
        [TestCase(0.05f)]
        public void GentleStartReleaseAndRestartReachTheActualCamera(float deltaTime)
        {
            CameraSystemController controller = CreateController(1f, 10f, 0.2f);
            controller.SetHorizontalOrbitIntent(1f);
            controller.UpdateCameraVisuals(deltaTime);
            float firstDisplacement = controller.AssignedCamera.transform.position.x;
            Assert.Greater(firstDisplacement, 0f);
            Assert.Less(firstDisplacement, 10f * deltaTime);
            float previousSpeed = controller.CurrentOrbitTravelSpeed;
            for (int step = 0; step < Mathf.CeilToInt(0.2f / deltaTime); step++)
            {
                Vector3 previousPosition = controller.AssignedCamera.transform.position;
                controller.UpdateCameraVisuals(deltaTime);
                Assert.GreaterOrEqual(controller.CurrentOrbitTravelSpeed, previousSpeed);
                Assert.LessOrEqual(controller.CurrentOrbitTravelSpeed, 10f);
                Assert.Greater(controller.AssignedCamera.transform.position.x, previousPosition.x);
                previousSpeed = controller.CurrentOrbitTravelSpeed;
            }
            Assert.AreEqual(10f, controller.CurrentOrbitTravelSpeed, 0.0001f);
            Vector3 releasedPosition = controller.AssignedCamera.transform.position;
            float releasedDistance = controller.OrbitDistance;
            controller.SetHorizontalOrbitIntent(0f);
            Assert.AreEqual(0f, controller.CurrentOrbitTravelSpeed);
            for (int step = 0; step < 10; step++)
            {
                controller.UpdateCameraVisuals(deltaTime);
                Assert.AreEqual(releasedPosition, controller.AssignedCamera.transform.position);
                Assert.AreEqual(releasedDistance, controller.OrbitDistance);
            }
            controller.SetHorizontalOrbitIntent(-1f);
            controller.UpdateCameraVisuals(deltaTime);
            Assert.AreEqual(firstDisplacement, releasedPosition.x - controller.AssignedCamera.transform.position.x, 0.0001f);
        }

        [TestCase(0.01f)]
        [TestCase(0.02f)]
        [TestCase(0.05f)]
        public void FiveTimesLongerOrbitTakesFiveTimesTheSteadyTravelTime(float deltaTime)
        {
            CameraSystemController shortController = CreateController(1f, 10f, 0.2f);
            CameraSystemController longController = CreateController(5f, 10f, 0.2f);
            shortController.SetHorizontalOrbitIntent(1f);
            longController.SetHorizontalOrbitIntent(1f);
            float shortTime = 0f;
            float longTime = 0f;
            float startLoss = 0f;
            int maximumSteps = Mathf.CeilToInt(31f / deltaTime);
            for (int step = 1; step <= maximumSteps; step++)
            {
                shortController.UpdateCameraVisuals(deltaTime);
                longController.UpdateCameraVisuals(deltaTime);
                if (step * deltaTime <= 0.3f)
                {
                    AssertPosition(shortController.AssignedCamera.transform.position, longController.AssignedCamera.transform.position, 0.0001f);
                    startLoss = step * deltaTime - shortController.OrbitDistance / 10f;
                }
                if (shortTime == 0f && shortController.OrbitDistance >= 60f)
                {
                    shortTime = step * deltaTime;
                    Assert.Less(shortController.AssignedCamera.transform.position.magnitude, 10f * deltaTime + 0.001f);
                }
                if (longController.OrbitDistance >= 300f)
                {
                    longTime = step * deltaTime;
                    Assert.Less(longController.AssignedCamera.transform.position.magnitude, 10f * deltaTime + 0.02f);
                    break;
                }
            }
            Assert.Greater(shortTime, 0f);
            Assert.Greater(longTime, 0f);
            Assert.AreEqual(6f, shortTime - startLoss, deltaTime + 0.003f);
            Assert.AreEqual(30f, longTime - startLoss, deltaTime + 0.003f);
            Assert.AreEqual(5f, (longTime - startLoss) / (shortTime - startLoss), deltaTime + 0.003f);
        }

        [Test]
        public void SeparateRatesAndSharedAssetsRemainIndependentWhileBodyPitchAndRollChange()
        {
            CameraSystemController first = CreateController(1f, 3f, 0f);
            CameraSystemController second = CreateController(1f, 7f, 0f);
            VehicleProfile shared = first.VehicleProfile;
            Quaternion adjustment = Quaternion.Euler(27f, 68f, -19f);
            shared.VehicleOrbit.Configure(new List<BezierOrbitKnot>(shared.VehicleOrbit.Knots), adjustment);
            CameraViewPreset secondPreset = second.ActiveViewPreset;
            second.AssignVehicle(second.VehicleBody, shared);
            second.SelectViewPreset(secondPreset);
            Assert.AreEqual(CameraSystemActivationResult.Succeeded, second.Activate());
            first.Release();
            Assert.AreEqual(CameraSystemActivationResult.Succeeded, first.Activate());
            string beforeProfile = JsonUtility.ToJson(shared);
            string beforeFirstPreset = JsonUtility.ToJson(first.ActiveViewPreset);
            string beforeSecondPreset = JsonUtility.ToJson(second.ActiveViewPreset);
            first.SetHorizontalOrbitIntent(1f);
            second.SetHorizontalOrbitIntent(1f);
            first.UpdateCameraVisuals(1f);
            second.UpdateCameraVisuals(1f);
            AssertPosition(adjustment * new Vector3(3f, 0f, 0f), first.AssignedCamera.transform.position, 0.0001f);
            AssertPosition(adjustment * new Vector3(7f, 0f, 0f), second.AssignedCamera.transform.position, 0.0001f);
            first.SetHorizontalOrbitIntent(0f);
            second.SetHorizontalOrbitIntent(0f);
            first.VehicleBody.SetPositionAndRotation(new Vector3(5f, 7f, -3f), Quaternion.Euler(37f, 23f, -41f));
            second.VehicleBody.SetPositionAndRotation(new Vector3(-5f, 2f, 8f), Quaternion.Euler(-22f, 53f, 32f));
            first.UpdateCameraVisuals(0.02f);
            second.UpdateCameraVisuals(0.05f);
            AssertPosition(first.VehicleBody.position + first.VehicleBody.rotation * adjustment * new Vector3(3f, 0f, 0f), first.AssignedCamera.transform.position, 0.0001f);
            AssertPosition(second.VehicleBody.position + second.VehicleBody.rotation * adjustment * new Vector3(7f, 0f, 0f), second.AssignedCamera.transform.position, 0.0001f);
            Assert.AreEqual(beforeProfile, JsonUtility.ToJson(shared));
            Assert.AreEqual(beforeFirstPreset, JsonUtility.ToJson(first.ActiveViewPreset));
            Assert.AreEqual(beforeSecondPreset, JsonUtility.ToJson(second.ActiveViewPreset));
        }

        private CameraSystemController CreateController(float scale, float speed, float response)
        {
            VehicleProfile profile = CreateProfile(scale);
            CameraViewPreset preset = ScriptableObject.CreateInstance<CameraViewPreset>();
            objects.Add(preset);
            preset.Configure(CameraViewType.ExteriorPresentation);
            preset.ConfigureOrbitTravel(speed, response);
            Camera camera = CreateObject("Camera").AddComponent<Camera>();
            CameraSystemController controller = CreateObject("Controller").AddComponent<CameraSystemController>();
            controller.enabled = false;
            controller.AssignCamera(camera);
            controller.AssignVehicle(CreateObject("Body").transform, profile);
            controller.SelectViewPreset(preset);
            Assert.AreEqual(CameraSystemActivationResult.Succeeded, controller.Activate());
            return controller;
        }

        private VehicleProfile CreateProfile(float scale)
        {
            Vector3[] anchors = { Vector3.zero, new Vector3(20f, 0f, 0f) * scale, new Vector3(20f, 0f, 10f) * scale, new Vector3(0f, 0f, 10f) * scale };
            List<BezierOrbitKnot> knots = new List<BezierOrbitKnot>();
            for (int index = 0; index < 4; index++)
            {
                BezierOrbitKnot knot = new BezierOrbitKnot();
                knot.Configure(anchors[index], Vector3.Lerp(anchors[index], anchors[(index + 3) % 4], 1f / 3f), Vector3.Lerp(anchors[index], anchors[(index + 1) % 4], 1f / 3f));
                knots.Add(knot);
            }
            VehicleOrbit orbit = new VehicleOrbit();
            orbit.Configure(knots, Quaternion.identity);
            AddWatchMarker(orbit);
            OrbitRemovableSection front = new OrbitRemovableSection();
            OrbitRemovableSection rear = new OrbitRemovableSection();
            front.Configure(0f, 0.2f);
            rear.Configure(0.5f, 5f / 6f);
            orbit.ConfigureRemovableSections(front, rear);
            VehicleConnectorAnchors connectors = new VehicleConnectorAnchors();
            connectors.Configure(new Vector3(10f, 0f, 0f) * scale, new Vector3(10f, 0f, 10f) * scale);
            VehicleProfile profile = ScriptableObject.CreateInstance<VehicleProfile>();
            objects.Add(profile);
            profile.ConfigureOrbit(orbit);
            profile.ConfigureConnectorAnchors(connectors);
            return profile;
        }

        private VehicleOrbit CreateCircle()
        {
            List<BezierOrbitKnot> knots = new List<BezierOrbitKnot>();
            Vector3[] anchors = { Vector3.right * 10f, Vector3.forward * 10f, Vector3.left * 10f, Vector3.back * 10f };
            foreach (Vector3 anchor in anchors)
            {
                Vector3 tangent = new Vector3(-anchor.z, 0f, anchor.x) * 0.55228475f;
                BezierOrbitKnot knot = new BezierOrbitKnot();
                knot.Configure(anchor, anchor - tangent, anchor + tangent);
                knots.Add(knot);
            }
            VehicleOrbit orbit = new VehicleOrbit();
            orbit.Configure(knots, Quaternion.identity);
            AddWatchMarker(orbit);
            return orbit;
        }

        private void AddWatchMarker(VehicleOrbit orbit)
        {
            OrbitWatchMarker marker = new OrbitWatchMarker();
            marker.Configure(0f, new Vector3(10f, 2f, 5f));
            orbit.ConfigureWatchMarkers(new List<OrbitWatchMarker> { marker });
        }

        private GameObject CreateObject(string name)
        {
            GameObject created = new GameObject(name);
            objects.Add(created);
            return created;
        }

        private float PolygonLength(List<Vector3> corners)
        {
            float length = 0f;
            for (int index = 0; index < corners.Count; index++)
            {
                length += Vector3.Distance(corners[index], corners[(index + 1) % corners.Count]);
            }
            return length;
        }

        private void MoveTo(CameraSystemController controller, float distance)
        {
            float difference = distance - controller.OrbitDistance;
            controller.SetHorizontalOrbitIntent(Mathf.Sign(difference));
            controller.UpdateCameraVisuals(Mathf.Abs(difference));
        }

        private void AssertPosition(Vector3 expected, Vector3 actual, float tolerance)
        {
            Assert.LessOrEqual(Vector3.Distance(expected, actual), tolerance, $"Expected {expected:F5}, got {actual:F5}");
        }

        private Vector3 PolygonPosition(List<Vector3> corners, float distance)
        {
            distance = Mathf.Repeat(distance, PolygonLength(corners));
            for (int index = 0; index < corners.Count; index++)
            {
                Vector3 start = corners[index];
                Vector3 end = corners[(index + 1) % corners.Count];
                float length = Vector3.Distance(start, end);
                if (distance <= length)
                {
                    return start + (end - start).normalized * distance;
                }
                distance -= length;
            }
            return corners[0];
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (Object created in objects)
            {
                Object.Destroy(created);
            }
            objects.Clear();
            yield return null;
        }
    }
}
