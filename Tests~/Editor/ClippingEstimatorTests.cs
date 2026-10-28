using System.Collections.Generic;
using Gley.CameraSystem.Editor;
using NUnit.Framework;
using UnityEngine;

namespace Gley.CameraSystem.Tests.EditMode
{
    public class ClippingEstimatorTests
    {
        private const float MinimumHeight = 2.5f;
        private const float MaximumHeight = 6.5f;
        private const float MinimumZoom = -2f;
        private const float MaximumZoom = 1f;
        private const float Tolerance = 0.01f;

        private readonly List<Object> objects = new List<Object>();

        private ChainTestProfileFactory factory;

        [SetUp]
        public void SetUp()
        {
            factory = new ChainTestProfileFactory();
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
        public void TallTrailerBlocksLowSideView_Warned()
        {
            IReadOnlyList<ClippingWarning> warnings = EstimateTruckAndTrailer(6f);

            Assert.Greater(warnings.Count, 0);
            bool hasLowRightSide = false;
            bool hasLowLeftSide = false;
            for (int index = 0; index < warnings.Count; index++)
            {
                ClippingWarning warning = warnings[index];
                Assert.AreEqual(1, warning.BodyIndex, "Only the trailer is tall enough to block.");
                Assert.IsTrue(warning.IsLow, $"High pose warned at {warning.Bearing}°.");
                if (warning.IsZoomedIn && Mathf.Abs(warning.Bearing - 90f) < Tolerance)
                {
                    hasLowRightSide = true;
                    Assert.AreEqual(MaximumZoom, warning.ZoomOffset, Tolerance);
                    Assert.AreEqual(MinimumHeight, warning.HeightOffset, Tolerance);
                }

                if (warning.IsZoomedIn && Mathf.Abs(warning.Bearing + 90f) < Tolerance)
                {
                    hasLowLeftSide = true;
                }
            }

            Assert.IsTrue(hasLowRightSide, "The low, zoomed-in right side view is not warned.");
            Assert.IsTrue(hasLowLeftSide, "The low, zoomed-in left side view is not warned.");
        }

        [Test]
        public void ClearEnvelope_NoWarnings()
        {
            IReadOnlyList<ClippingWarning> warnings = EstimateTruckAndTrailer(1f);

            Assert.AreEqual(0, warnings.Count);
        }

        private IReadOnlyList<ClippingWarning> EstimateTruckAndTrailer(float trailerHeight)
        {
            VehicleProfile truck = factory.CreateRoundedProfile("Main", 2.5f, 3f, 3f);
            VehicleProfile trailer = factory.CreateRoundedProfile("Main", 2.5f, 5f, 5f);
            ConfigureOrbit(truck.PrimaryOrbit, new Vector3(0f, 8f, -5.5f));
            ConfigureOrbit(trailer.PrimaryOrbit, new Vector3(0f, 8f, 3.5f));
            VehicleProfile[] profiles = { truck, trailer };
            ChainLayout layout = new ChainLayout(profiles, 0, truck.PrimaryOrbit);
            Assert.AreEqual(ChainLayoutResult.Valid, layout.LayoutResult);
            Assert.AreEqual(new Vector3(0f, 0f, -9f), layout.Offsets[1]);
            ChainOrbit orbit = new ChainOrbit(layout, null, 0);
            Assert.IsTrue(orbit.IsClosed);

            Transform truckModel = CreateModel("Truck", new Vector3(100f, 0f, 50f), 30f, new Vector3(0f, 1f, 0f), new Vector3(2f, 2f, 5f));
            Transform trailerModel = CreateModel("Trailer", new Vector3(-40f, 3f, 20f), -70f, new Vector3(0f, trailerHeight * 0.5f, 0f), new Vector3(2f, trailerHeight, 9f));
            Transform[] models = { truckModel, trailerModel };
            Matrix4x4[] bodyToPreview = { Matrix4x4.Translate(layout.Offsets[0]), Matrix4x4.Translate(layout.Offsets[1]) };

            return new ClippingEstimator().Estimate(orbit, truck.PrimaryOrbit, models, bodyToPreview);
        }

        private void ConfigureOrbit(VehicleOrbit orbit, Vector3 watchPoint)
        {
            orbit.ConfigureOffsetRanges(MinimumHeight, MaximumHeight, MinimumZoom, MaximumZoom);
            OrbitWatchMarker marker = orbit.WatchMarkers[0];
            marker.Configure(marker.NormalizedOrbitPosition, watchPoint);
        }

        private Transform CreateModel(string name, Vector3 position, float yaw, Vector3 boxCentre, Vector3 boxSize)
        {
            GameObject root = new GameObject(name);
            objects.Add(root);
            root.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.transform.SetParent(root.transform, false);
            box.transform.localPosition = boxCentre;
            box.transform.localScale = boxSize;
            return root.transform;
        }
    }
}
