using NUnit.Framework;
using UnityEngine;

namespace Gley.CameraSystem.Tests.EditMode
{
    public class OrbitFrameTests
    {
        private GameObject rootObject;

        [Test]
        public void HeightAxis_WithPitchedOrientationAdjustment_PerpendicularToCurvePlane()
        {
            rootObject = new GameObject("Orbit root");
            Quaternion adjustment = Quaternion.Euler(27f, 68f, -19f);
            OrbitFrame frame = new OrbitFrame(rootObject.transform, adjustment);
            Vector3 alongX = frame.ToWorldPosition(adjustment * Vector3.right) - frame.ToWorldPosition(Vector3.zero);
            Vector3 alongZ = frame.ToWorldPosition(adjustment * Vector3.forward) - frame.ToWorldPosition(Vector3.zero);

            Assert.AreEqual(0f, Vector3.Dot(frame.Up, alongX), 0.00001f);
            Assert.AreEqual(0f, Vector3.Dot(frame.Up, alongZ), 0.00001f);
            Assert.Less(Vector3.Angle(frame.Up, adjustment * Vector3.up), 0.001f);
        }

        [Test]
        public void HeightAxis_RootPitchedAndRolled_FollowsRoot()
        {
            rootObject = new GameObject("Orbit root");
            rootObject.transform.SetPositionAndRotation(new Vector3(3f, 4f, -2f), Quaternion.Euler(31f, 12f, -28f));
            Quaternion adjustment = Quaternion.Euler(27f, 68f, -19f);
            OrbitFrame frame = new OrbitFrame(rootObject.transform, adjustment);
            Vector3 expectedUp = rootObject.transform.rotation * adjustment * Vector3.up;
            Vector3 localNormal = adjustment * Vector3.left;

            Assert.Less(Vector3.Angle(expectedUp, frame.Up), 0.001f);
            Assert.Less(Vector3.Angle(rootObject.transform.rotation * localNormal, frame.ToWorldInwardNormal(localNormal)), 0.001f);
            Vector3 expectedPosition = rootObject.transform.position + rootObject.transform.rotation * adjustment * Vector3.forward;
            Assert.Less(Vector3.Distance(expectedPosition, frame.ToWorldPosition(adjustment * Vector3.forward)), 0.00001f);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(rootObject);
        }
    }
}
