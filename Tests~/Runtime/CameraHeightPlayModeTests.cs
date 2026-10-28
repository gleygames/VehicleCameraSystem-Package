using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Gley.CameraSystem.Tests.PlayMode
{
    public class CameraHeightPlayModeTests
    {
        private GameObject bodyObject;
        private GameObject cameraObject;
        private GameObject controllerObject;
        private VehicleProfile profile;
        private CameraViewPreset preset;

        [Test]
        public void HeightOffset_OrientationAdjusted_MovesAlongAdjustedUp()
        {
            bodyObject = new GameObject("Body");
            bodyObject.transform.SetPositionAndRotation(new Vector3(2f, 3f, -5f), Quaternion.Euler(13f, 29f, -11f));
            cameraObject = new GameObject("Camera");
            controllerObject = new GameObject("Controller");
            Camera camera = cameraObject.AddComponent<Camera>();
            CameraSystemController controller = controllerObject.AddComponent<CameraSystemController>();
            controller.SetUpdateMode(CameraUpdateMode.Manual);
            profile = ScriptableObject.CreateInstance<VehicleProfile>();
            preset = ScriptableObject.CreateInstance<CameraViewPreset>();
            preset.Configure(CameraViewType.Presentation);
            VehicleOrbit orbit = profile.AddOrbit("Main");
            Quaternion adjustment = Quaternion.Euler(27f, 68f, -19f);
            orbit.Configure(CreateRectangleKnots(), adjustment);
            orbit.ConfigureOffsetRanges(-2f, 2f, -4f, 2f);
            profile.AddWatchMarker(orbit, "Centre", 0f, new Vector3(2f, 1f, 3f), false);
            profile.AddView("Presentation", preset, orbit.Id);
            VehicleCameraTarget target = bodyObject.AddComponent<VehicleCameraTarget>();
            target.Configure(bodyObject.transform, profile);
            controller.Configure(camera, target, "Presentation");

            Assert.AreEqual(CameraCommandResult.Accepted, controller.Activate());
            Vector3 before = camera.transform.position;
            controller.SetHeightIntent(1f);
            controller.UpdateCameraFrame(0.5f);
            Vector3 displacement = camera.transform.position - before;
            Vector3 expectedUp = bodyObject.transform.rotation * adjustment * Vector3.up;

            Assert.Greater(displacement.magnitude, 0.1f);
            Assert.Less(Vector3.Angle(displacement, expectedUp), 0.1f);
            Assert.AreEqual(controller.HeightOffset, displacement.magnitude, 0.0001f);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(controllerObject);
            Object.DestroyImmediate(cameraObject);
            Object.DestroyImmediate(bodyObject);
            Object.DestroyImmediate(profile);
            Object.DestroyImmediate(preset);
        }

        private List<BezierOrbitKnot> CreateRectangleKnots()
        {
            Vector3[] anchors =
            {
                Vector3.zero,
                new Vector3(4f, 0f, 0f),
                new Vector3(4f, 0f, 6f),
                new Vector3(0f, 0f, 6f)
            };
            List<BezierOrbitKnot> knots = new List<BezierOrbitKnot>();
            for (int index = 0; index < anchors.Length; index++)
            {
                Vector3 anchor = anchors[index];
                Vector3 incoming = Vector3.Lerp(anchor, anchors[(index + 3) % anchors.Length], 1f / 3f);
                Vector3 outgoing = Vector3.Lerp(anchor, anchors[(index + 1) % anchors.Length], 1f / 3f);
                BezierOrbitKnot knot = new BezierOrbitKnot();
                knot.Configure(anchor, incoming, outgoing);
                knots.Add(knot);
            }

            return knots;
        }
    }
}
