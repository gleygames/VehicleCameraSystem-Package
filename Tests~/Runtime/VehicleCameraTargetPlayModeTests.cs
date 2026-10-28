using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Gley.CameraSystem.Tests.PlayMode
{
    public class VehicleCameraTargetPlayModeTests
    {
        private readonly List<Object> objects = new List<Object>();

        [UnityTest]
        public IEnumerator DestroyTarget_RaisesDestroyed()
        {
            VehicleCameraTarget target = CreateObject("Target").AddComponent<VehicleCameraTarget>();
            int count = 0;
            target.Destroyed += delegate { count++; };

            Object.Destroy(target.gameObject);
            yield return null;

            Assert.AreEqual(1, count);
        }

        [UnityTest]
        public IEnumerator DestroyRootBody_IsRootAliveFalse()
        {
            Transform root = CreateObject("Root").transform;
            VehicleCameraTarget target = CreateObject("Target").AddComponent<VehicleCameraTarget>();
            target.Configure(root, CreateProfile(20f, 10f));
            Assert.IsTrue(target.IsRootAlive);

            Object.Destroy(root.gameObject);
            yield return null;

            Assert.IsFalse(target.IsRootAlive);
        }

        [Test]
        public void ChainOrbitBuilder_TwoTrucksSameTrailerProfile_BuildsIndependentOrbits()
        {
            VehicleProfile truckProfile = CreateProfile(20f, 10f);
            VehicleProfile trailerProfile = CreateProfile(40f, 20f);
            Transform firstTruck = CreateObject("First truck").transform;
            Transform secondTruck = CreateObject("Second truck").transform;
            Transform firstTrailer = CreateObject("First trailer").transform;
            Transform secondTrailer = CreateObject("Second trailer").transform;
            secondTruck.position = new Vector3(100f, 0f, 0f);
            VehicleCameraTarget firstTarget = CreateObject("First target").AddComponent<VehicleCameraTarget>();
            VehicleCameraTarget secondTarget = CreateObject("Second target").AddComponent<VehicleCameraTarget>();
            firstTarget.Configure(firstTruck, truckProfile);
            secondTarget.Configure(secondTruck, truckProfile);
            Assert.AreEqual(VehicleTargetResult.Accepted, firstTarget.Attach(firstTrailer, trailerProfile, ChainEnd.Rear));
            Assert.AreEqual(VehicleTargetResult.Accepted, secondTarget.Attach(secondTrailer, trailerProfile, ChainEnd.Rear));

            ChainOrbitBuilder builder = new ChainOrbitBuilder();
            ChainOrbit firstOrbit = builder.Build(firstTarget, "Main");
            ChainOrbit secondOrbit = builder.Build(secondTarget, "Main");

            Assert.IsNotNull(firstOrbit);
            Assert.IsNotNull(secondOrbit);
            Assert.AreNotSame(firstOrbit, secondOrbit);
            Assert.AreEqual(ChainOrbitAssemblyResult.Valid, firstOrbit.AssemblyResult);
            Assert.AreEqual(ChainOrbitAssemblyResult.Valid, secondOrbit.AssemblyResult);
            Assert.AreEqual(firstOrbit.Length, secondOrbit.Length, 0.0001f);
            Assert.AreEqual(firstOrbit.EvaluateWorldPosition(firstTruck, 0f) + new Vector3(100f, 0f, 0f), secondOrbit.EvaluateWorldPosition(secondTruck, 0f));
            Assert.AreSame(firstTrailer, FindTrailerMarkerBody(firstOrbit));
            Assert.AreSame(secondTrailer, FindTrailerMarkerBody(secondOrbit));
        }

        private Transform FindTrailerMarkerBody(ChainOrbit orbit)
        {
            for (int index = 0; index < orbit.MergedMarkers.Count; index++)
            {
                if (orbit.MergedMarkers[index].ChainIndex == 1)
                {
                    return orbit.MergedMarkers[index].OwnerBody;
                }
            }

            return null;
        }

        private VehicleProfile CreateProfile(float width, float depth)
        {
            VehicleProfile profile = ScriptableObject.CreateInstance<VehicleProfile>();
            objects.Add(profile);
            VehicleOrbit orbit = profile.AddOrbit("Main");
            Vector3[] anchors =
            {
                Vector3.zero,
                new Vector3(width, 0f, 0f),
                new Vector3(width, 0f, depth),
                new Vector3(0f, 0f, depth)
            };
            List<BezierOrbitKnot> knots = new List<BezierOrbitKnot>();
            for (int index = 0; index < anchors.Length; index++)
            {
                Vector3 anchor = anchors[index];
                BezierOrbitKnot knot = new BezierOrbitKnot();
                knot.Configure(anchor, anchor + (anchors[(index + 3) % 4] - anchor) / 3f, anchor + (anchors[(index + 1) % 4] - anchor) / 3f);
                knots.Add(knot);
            }

            orbit.Configure(knots, Quaternion.identity);
            float length = 2f * (width + depth);
            OrbitRemovableSection front = new OrbitRemovableSection();
            front.Configure(0f, width / length);
            OrbitRemovableSection rear = new OrbitRemovableSection();
            rear.Configure((width + depth) / length, (2f * width + depth) / length);
            orbit.ConfigureRemovableSections(front, rear);
            profile.AddWatchMarker(orbit, "Right", (width + depth * 0.5f) / length, new Vector3(width * 0.5f, 2f, depth * 0.5f), false);
            profile.AddWatchMarker(orbit, "Left", (2f * width + depth * 1.5f) / length, new Vector3(width * 0.5f, 2f, depth * 0.5f), false);
            VehicleConnectorAnchors connectorAnchors = new VehicleConnectorAnchors();
            connectorAnchors.Configure(new Vector3(width * 0.5f, 0f, 0f), new Vector3(width * 0.5f, 0f, depth + 2f));
            profile.ConfigureConnectorAnchors(connectorAnchors);
            return profile;
        }

        private GameObject CreateObject(string name)
        {
            GameObject created = new GameObject(name);
            objects.Add(created);
            return created;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            for (int index = objects.Count - 1; index >= 0; index--)
            {
                if (objects[index] != null)
                {
                    Object.Destroy(objects[index]);
                }
            }

            objects.Clear();
            yield return null;
        }
    }
}
