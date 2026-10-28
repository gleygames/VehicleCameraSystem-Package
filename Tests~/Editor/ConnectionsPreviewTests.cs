using System.Collections.Generic;
using Gley.CameraSystem.Editor;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Gley.CameraSystem.Tests.EditMode
{
    public class ConnectionsPreviewTests
    {
        private const float Tolerance = 0.0001f;
        private const float Articulation = 40f;

        private readonly List<Object> objects = new List<Object>();
        private readonly List<EditorIssue> issues = new List<EditorIssue>();

        private ChainTestProfileFactory factory;

        [SetUp]
        public void SetUp()
        {
            factory = new ChainTestProfileFactory();
            issues.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            for (int index = objects.Count - 1; index >= 0; index--)
            {
                if (objects[index] != null)
                {
                    Object.DestroyImmediate(objects[index]);
                }
            }

            objects.Clear();
            factory.Dispose();
        }

        [Test]
        public void ArticulationPreview_OrbitShapeUnchanged()
        {
            VehicleProfile truck = factory.CreateRoundedProfile("Main", 2.5f, 3f, 3f);
            VehicleProfile trailer = factory.CreateRoundedProfile("Main", 2.5f, 5f, 5f);
            ConnectionChainSelection selection = new ConnectionChainSelection();
            ConnectionPartner partner = selection.AddPartner(ChainEnd.Rear, trailer);
            ConnectionChainPreview preview = new ConnectionChainPreview();

            preview.Rebuild(truck, null, truck.PrimaryOrbit, selection);

            Assert.AreEqual(ChainLayoutResult.Valid, preview.LayoutResult);
            Assert.IsTrue(preview.IsOrbitValid);
            Assert.AreEqual(2, preview.BodyCount);
            ChainOrbit straightOrbit = preview.Orbit;
            float straightLength = straightOrbit.Length;
            Vector3[] straightPoints = new Vector3[preview.OrbitPointCount];
            System.Array.Copy(preview.OrbitPoints, straightPoints, straightPoints.Length);
            MergedMarker trailerMarker = FindTrailerMarker(preview.Orbit);
            Vector3 straightRootWatchPoint = preview.EvaluateRootWatchPoint(trailerMarker.OrbitDistance);

            partner.SetArticulation(Articulation);
            preview.ApplyArticulation(selection);

            Assert.AreSame(straightOrbit, preview.Orbit);
            Assert.AreEqual(straightLength, preview.Orbit.Length);
            Assert.AreEqual(straightPoints.Length, preview.OrbitPointCount);
            for (int index = 0; index < straightPoints.Length; index++)
            {
                Assert.AreEqual(straightPoints[index], preview.OrbitPoints[index], $"Orbit point {index} moved.");
            }

            float radians = Articulation * Mathf.Deg2Rad;
            Vector3 pivot = new Vector3(0f, 0f, -3.5f);
            Vector3 swungRear = pivot + new Vector3(-10.5f * Mathf.Sin(radians), 0f, -10.5f * Mathf.Cos(radians));
            AssertVector(pivot, preview.BodyMatrices[1].MultiplyPoint3x4(new Vector3(0f, 0f, 5.5f)));
            AssertVector(swungRear, preview.BodyMatrices[1].MultiplyPoint3x4(new Vector3(0f, 0f, -5f)));
            AssertVector(new Vector3(1f, 2f, 3f), preview.BodyMatrices[0].MultiplyPoint3x4(new Vector3(1f, 2f, 3f)));

            Vector3 swungWatchPoint = pivot + new Vector3(-5.5f * Mathf.Sin(radians), 1f, -5.5f * Mathf.Cos(radians));
            AssertVector(new Vector3(0f, 1f, -9f), straightRootWatchPoint);
            AssertVector(new Vector3(0f, 1f, -9f), preview.EvaluateRootWatchPoint(trailerMarker.OrbitDistance));
            AssertVector(swungWatchPoint, preview.EvaluateOwnerWatchPoint(trailerMarker.OrbitDistance));
        }

        [Test]
        public void PreviewCreatesNoSceneObjects()
        {
            VehicleProfile truck = factory.CreateRoundedProfile("Main", 2.5f, 3f, 3f);
            VehicleProfile trailer = factory.CreateRoundedProfile("Main", 2.5f, 5f, 5f);
            Transform truckModel = CreateModel("Truck", new Vector3(10f, 0f, 5f), new Vector3(2f, 2f, 5f));
            Transform trailerModel = CreateModel("Trailer", new Vector3(-20f, 0f, 5f), new Vector3(2f, 6f, 9f));
            Vector3 truckPosition = truckModel.position;
            Vector3 trailerPosition = trailerModel.position;
            Quaternion trailerRotation = trailerModel.rotation;
            ConnectionChainSelection selection = new ConnectionChainSelection();
            ConnectionPartner partner = selection.AddPartner(ChainEnd.Rear, trailer);
            partner.SetModel(trailerModel.gameObject);
            int rootCount = SceneManager.GetActiveScene().rootCount;
            int gameObjectCount = Resources.FindObjectsOfTypeAll<GameObject>().Length;
            ConnectionChainPreview preview = new ConnectionChainPreview();
            VehicleCameraValidationCollector collector = new VehicleCameraValidationCollector(new FakeInputSystemInstallation(true));

            preview.Rebuild(truck, truckModel, truck.PrimaryOrbit, selection);
            partner.SetArticulation(Articulation);
            preview.ApplyArticulation(selection);
            collector.CollectChainIssues(preview, issues);

            Assert.IsTrue(preview.IsOrbitValid);
            Assert.AreEqual(rootCount, SceneManager.GetActiveScene().rootCount);
            Assert.AreEqual(gameObjectCount, Resources.FindObjectsOfTypeAll<GameObject>().Length);
            Assert.AreEqual(truckPosition, truckModel.position);
            Assert.AreEqual(trailerPosition, trailerModel.position);
            Assert.AreEqual(trailerRotation, trailerModel.rotation);
        }

        private MergedMarker FindTrailerMarker(ChainOrbit orbit)
        {
            IReadOnlyList<MergedMarker> markers = orbit.MergedMarkers;
            for (int index = 0; index < markers.Count; index++)
            {
                if (markers[index].ChainIndex == 1)
                {
                    return markers[index];
                }
            }

            Assert.Fail("The trailer marker is not on the assembled orbit.");
            return default;
        }

        private Transform CreateModel(string name, Vector3 position, Vector3 boxSize)
        {
            GameObject root = new GameObject(name);
            objects.Add(root);
            root.transform.position = position;
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.transform.SetParent(root.transform, false);
            box.transform.localPosition = new Vector3(0f, boxSize.y * 0.5f, 0f);
            box.transform.localScale = boxSize;
            return root.transform;
        }

        private void AssertVector(Vector3 expected, Vector3 actual)
        {
            Assert.AreEqual(expected.x, actual.x, Tolerance, $"x of {actual}");
            Assert.AreEqual(expected.y, actual.y, Tolerance, $"y of {actual}");
            Assert.AreEqual(expected.z, actual.z, Tolerance, $"z of {actual}");
        }
    }
}
