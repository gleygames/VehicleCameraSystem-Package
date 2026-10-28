using System.Collections.Generic;
using Gley.CameraSystem.Editor;
using NUnit.Framework;
using UnityEngine;

namespace Gley.CameraSystem.Tests.EditMode
{
    public class ProfileGeneratorTests
    {
        private const float Tolerance = 0.01f;
        private const float TruckBaseHeight = 2f;
        private const float TruckRoof = 3.5f;

        private readonly List<Object> objects = new List<Object>();
        private readonly List<Renderer> renderers = new List<Renderer>();
        private readonly List<Collider> colliders = new List<Collider>();
        private readonly List<Transform> exclusions = new List<Transform>();

        private ProfileGenerator generator;
        private GeneratorPresets presets;

        [SetUp]
        public void SetUp()
        {
            generator = new ProfileGenerator(new ProfileGenerationSettings());
            presets = new GeneratorPresets(CreatePreset(CameraViewType.Driving), CreatePreset(CameraViewType.Presentation), CreatePreset(CameraViewType.Interior), CreatePreset(CameraViewType.Fixed));
            exclusions.Clear();
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
        }

        [Test]
        public void Generate_BoxTruck_ProfileValidatesWithNoErrors()
        {
            Transform truck = CreateBoxTruck();
            VehicleProfile profile = CreateProfile();

            ProfileGenerationResult result = Generate(truck, profile);

            Assert.AreEqual(ProfileGenerationResult.Generated, result);
            Assert.IsFalse(new VehicleProfileValidator().Validate(profile).HasErrors);
            Assert.AreEqual(2, profile.Orbits.Count);
            Assert.AreEqual(4, profile.Views.Count);

            for (int orbitIndex = 0; orbitIndex < profile.Orbits.Count; orbitIndex++)
            {
                ClosedBezierOrbit closedOrbit = new ClosedBezierOrbit(profile.Orbits[orbitIndex]);
                Assert.AreEqual(OrbitValidationResult.Valid, closedOrbit.ValidationResult);
                Assert.AreEqual(OrbitWatchMarkerValidationResult.Valid, closedOrbit.WatchMarkerValidationResult);
                Assert.AreEqual(OrbitRemovableSectionValidationResult.Valid, closedOrbit.RemovableSectionValidationResult);
                Assert.LessOrEqual(closedOrbit.MaximumCornerTurn, 1f);
            }

            List<EditorIssue> issues = new List<EditorIssue>();
            VehicleCameraValidationCollector collector = new VehicleCameraValidationCollector(new FakeInputSystemInstallation(true));
            collector.CollectIssues(profile, VehicleCameraTabKind.Setup, issues);
            collector.CollectIssues(profile, VehicleCameraTabKind.Orbits, issues);
            collector.CollectIssues(profile, VehicleCameraTabKind.Views, issues);
            collector.CollectIssues(profile, VehicleCameraTabKind.Connections, issues);

            for (int index = 0; index < issues.Count; index++)
            {
                Assert.AreNotEqual(EditorIssueSeverity.Error, issues[index].Severity, issues[index].Message);
            }

            VehicleOrbit drivingOrbit;
            Assert.IsTrue(profile.TryGetOrbit(ProfileGenerator.DrivingName, out drivingOrbit));
            ChainLayout layout = new ChainLayout(new List<VehicleProfile> { profile }, 0, drivingOrbit);
            Assert.AreEqual(ChainLayoutResult.Valid, layout.LayoutResult);
        }

        [Test]
        public void Generate_CurveEnclosesBoundsWithMargin()
        {
            Transform truck = CreateBoxTruck();
            VehicleProfile profile = CreateProfile();
            Generate(truck, profile);
            ClosedBezierOrbit closedOrbit = new ClosedBezierOrbit(GetOrbit(profile, ProfileGenerator.DrivingName));
            int sampleCount = 4000;
            float minimumClearance = float.PositiveInfinity;

            for (int sampleIndex = 0; sampleIndex < sampleCount; sampleIndex++)
            {
                Vector3 point = closedOrbit.EvaluateBodyLocalPosition(closedOrbit.Length * sampleIndex / sampleCount);
                Assert.AreEqual(TruckBaseHeight, point.y, Tolerance);
                float outsideX = Mathf.Max(Mathf.Abs(point.x) - 1.25f, 0f);
                float outsideZ = Mathf.Max(Mathf.Max(-4f - point.z, point.z - 4f), 0f);
                float clearance = Mathf.Sqrt(outsideX * outsideX + outsideZ * outsideZ);
                Assert.Greater(clearance, 0f);
                minimumClearance = Mathf.Min(minimumClearance, clearance);
            }

            float cornerRadius = 1.125f;
            float expectedCornerClearance = cornerRadius - Mathf.Sqrt(2f) * (cornerRadius - 1f);
            Assert.AreEqual(expectedCornerClearance, minimumClearance, Tolerance);
            AssertVector(new Vector3(0f, TruckBaseHeight, -5f), EvaluateAtBearing(closedOrbit, 0f));
            AssertVector(new Vector3(2.25f, TruckBaseHeight, 0f), EvaluateAtBearing(closedOrbit, 90f));
            AssertVector(new Vector3(-2.25f, TruckBaseHeight, 0f), EvaluateAtBearing(closedOrbit, -90f));
            AssertVector(new Vector3(0f, TruckBaseHeight, 5f), EvaluateAtBearing(closedOrbit, 180f));
        }

        [Test]
        public void Generate_ExcludedMirror_NotInBounds()
        {
            Transform truck = CreateBoxTruck();
            Transform mirror = CreateCube("Mirror", truck, new Vector3(1.6f, 2.2f, 3.5f), new Vector3(0.4f, 0.4f, 0.1f));
            VehicleProfile excludedProfile = CreateProfile();
            VehicleProfile includedProfile = CreateProfile();

            exclusions.Add(mirror);
            Generate(truck, excludedProfile);
            exclusions.Clear();
            Generate(truck, includedProfile);

            ClosedBezierOrbit excludedOrbit = new ClosedBezierOrbit(GetOrbit(excludedProfile, ProfileGenerator.DrivingName));
            ClosedBezierOrbit includedOrbit = new ClosedBezierOrbit(GetOrbit(includedProfile, ProfileGenerator.DrivingName));
            Assert.AreEqual(2.25f, EvaluateAtBearing(excludedOrbit, 90f).x, Tolerance);
            Assert.AreEqual(-2.25f, EvaluateAtBearing(excludedOrbit, -90f).x, Tolerance);
            Assert.AreEqual(2.8f, EvaluateAtBearing(includedOrbit, 90f).x, Tolerance);
        }

        [Test]
        public void Generate_DrivingMarkersAtFourBearings_WatchPointsAsSpecified()
        {
            Transform truck = CreateBoxTruck();
            VehicleProfile profile = CreateProfile();
            Generate(truck, profile);
            VehicleOrbit drivingOrbit = GetOrbit(profile, ProfileGenerator.DrivingName);
            ClosedBezierOrbit closedOrbit = new ClosedBezierOrbit(drivingOrbit);
            float cameraHeight = TruckRoof + 2f;
            float roofEdgeHeight = TruckRoof + 0.5f;
            float expectedRearHeight = cameraHeight + (roofEdgeHeight - cameraHeight) * (14f + 5f) / (4f + 5f);

            Assert.AreEqual(4, drivingOrbit.WatchMarkers.Count);
            Assert.Greater(expectedRearHeight, 0.5f + 0.6f * 3f);
            AssertMarker(closedOrbit, drivingOrbit, ProfileGenerator.RearMarkerName, new Vector3(0f, TruckBaseHeight, -5f), new Vector3(0f, expectedRearHeight, 14f));
            AssertMarker(closedOrbit, drivingOrbit, ProfileGenerator.RightMarkerName, new Vector3(2.25f, TruckBaseHeight, 0f), new Vector3(0f, 2f, 4f));
            AssertMarker(closedOrbit, drivingOrbit, ProfileGenerator.LeftMarkerName, new Vector3(-2.25f, TruckBaseHeight, 0f), new Vector3(0f, 2f, 4f));
            AssertMarker(closedOrbit, drivingOrbit, ProfileGenerator.FrontMarkerName, new Vector3(0f, TruckBaseHeight, 5f), new Vector3(0f, 2f, 0f));

            VehicleOrbit presentationOrbit = GetOrbit(profile, ProfileGenerator.PresentationName);
            Assert.AreEqual(1, presentationOrbit.WatchMarkers.Count);
            AssertMarker(new ClosedBezierOrbit(presentationOrbit), presentationOrbit, ProfileGenerator.CentreMarkerName, new Vector3(0f, TruckBaseHeight, -5f), new Vector3(0f, 2f, 0f));
        }

        [Test]
        public void Generate_DrivingDefault_BearingZero_RoofPlusTwo()
        {
            Transform truck = CreateBoxTruck();
            VehicleProfile profile = CreateProfile();
            Generate(truck, profile);
            VehicleOrbit drivingOrbit = GetOrbit(profile, ProfileGenerator.DrivingName);
            VehicleViewEntry drivingView;
            Assert.IsTrue(profile.TryGetView(ProfileGenerator.DrivingName, out drivingView));

            Assert.AreEqual(0f, drivingView.DefaultPose.Bearing, Tolerance);
            Assert.AreEqual(0f, drivingView.DefaultPose.ZoomOffset, Tolerance);
            Assert.AreEqual(TruckRoof + 2f - TruckBaseHeight, drivingView.DefaultPose.HeightOffset, Tolerance);
            Assert.AreEqual(-1f, drivingOrbit.MinimumHeightOffset, Tolerance);
            Assert.AreEqual(TruckRoof + 4f - TruckBaseHeight, drivingOrbit.MaximumHeightOffset, Tolerance);
            Assert.AreEqual(-4f, drivingOrbit.MinimumZoomOffset, Tolerance);
            Assert.AreEqual(2f, drivingOrbit.MaximumZoomOffset, Tolerance);

            ChainLayout layout = new ChainLayout(new List<VehicleProfile> { profile }, 0, drivingOrbit);
            ChainOrbit chainOrbit = new ChainOrbit(layout, new List<Transform> { truck }, 0);
            LivePose pose = new StoredPoseResolver().Resolve(drivingView.DefaultPose, chainOrbit, drivingOrbit, 0f);
            Vector3 cameraPosition = chainOrbit.EvaluateRootLocalPosition(pose.OrbitDistance) + Vector3.up * pose.HeightOffset;
            AssertVector(new Vector3(0f, TruckRoof + 2f, -5f), cameraPosition);
        }

        [Test]
        public void Generate_TruckAndTrailer_ChainDefaultBehindTrailerAboveRoof_OrbitUnchanged()
        {
            Transform truck = CreateBoxTruck();
            Transform trailer = CreateRoot("Trailer", new Vector3(0f, 0f, -10f), Quaternion.identity);
            CreateCube("Box", trailer, new Vector3(0f, 2.2f, 0f), new Vector3(2.6f, 3.4f, 12f));
            VehicleProfile truckProfile = CreateProfile();
            VehicleProfile trailerProfile = CreateProfile();
            Generate(truck, truckProfile);
            Generate(trailer, trailerProfile);
            VehicleOrbit truckOrbit = GetOrbit(truckProfile, ProfileGenerator.DrivingName);
            VehicleOrbit trailerOrbit = GetOrbit(trailerProfile, ProfileGenerator.DrivingName);
            List<Vector3> truckKnotsBefore = CaptureKnots(truckOrbit);
            List<Vector3> trailerKnotsBefore = CaptureKnots(trailerOrbit);
            Vector2 truckRearSectionBefore = CaptureRearSection(truckOrbit);

            ChainLayout layout = new ChainLayout(new List<VehicleProfile> { truckProfile, trailerProfile }, 0, truckOrbit);
            ChainOrbit chainOrbit = new ChainOrbit(layout, new List<Transform> { truck, trailer }, 0);
            VehicleViewEntry drivingView;
            Assert.IsTrue(truckProfile.TryGetView(ProfileGenerator.DrivingName, out drivingView));
            LivePose pose = new StoredPoseResolver().Resolve(drivingView.DefaultPose, chainOrbit, truckOrbit, 0f);
            Vector3 cameraPosition = chainOrbit.EvaluateRootLocalPosition(pose.OrbitDistance) + Vector3.up * pose.HeightOffset;

            Assert.AreEqual(ChainLayoutResult.Valid, layout.LayoutResult);
            Assert.IsTrue(chainOrbit.IsClosed);
            AssertVector(new Vector3(0f, -0.2f, -10f), layout.Offsets[1]);
            float trailerRearEdge = -16f;
            float trailerRoof = 3.9f - 0.2f;
            Assert.AreEqual(0f, cameraPosition.x, Tolerance);
            Assert.AreEqual(trailerRearEdge - 1f, cameraPosition.z, Tolerance);
            Assert.Less(cameraPosition.z, trailerRearEdge);
            Assert.AreEqual(1.8f, cameraPosition.y - trailerRoof, Tolerance);
            AssertKnotsUnchanged(truckKnotsBefore, truckOrbit);
            AssertKnotsUnchanged(trailerKnotsBefore, trailerOrbit);
            Assert.AreEqual(truckRearSectionBefore, CaptureRearSection(truckOrbit));
        }

        [Test]
        public void Generate_ModelUnchanged()
        {
            Transform truck = CreateBoxTruck();
            CreateCube("Mirror", truck, new Vector3(1.6f, 2.2f, 3.5f), new Vector3(0.4f, 0.4f, 0.1f));
            Transform[] transforms = truck.GetComponentsInChildren<Transform>(true);
            List<Vector3> positions = new List<Vector3>();
            List<Quaternion> rotations = new List<Quaternion>();
            List<Vector3> scales = new List<Vector3>();
            List<int> componentCounts = new List<int>();

            for (int index = 0; index < transforms.Length; index++)
            {
                positions.Add(transforms[index].localPosition);
                rotations.Add(transforms[index].localRotation);
                scales.Add(transforms[index].localScale);
                componentCounts.Add(transforms[index].GetComponents<Component>().Length);
            }

            Generate(truck, CreateProfile());

            Transform[] transformsAfter = truck.GetComponentsInChildren<Transform>(true);
            Assert.AreEqual(transforms.Length, transformsAfter.Length);

            for (int index = 0; index < transforms.Length; index++)
            {
                Assert.AreEqual(transforms[index], transformsAfter[index]);
                Assert.AreEqual(positions[index], transforms[index].localPosition);
                Assert.AreEqual(rotations[index], transforms[index].localRotation);
                Assert.AreEqual(scales[index], transforms[index].localScale);
                Assert.AreEqual(componentCounts[index], transforms[index].GetComponents<Component>().Length);
            }
        }

        private CameraViewPreset CreatePreset(CameraViewType viewType)
        {
            CameraViewPreset preset = ScriptableObject.CreateInstance<CameraViewPreset>();
            preset.Configure(viewType);
            objects.Add(preset);
            return preset;
        }

        private Transform CreateBoxTruck()
        {
            Transform truck = CreateRoot("Box Truck", new Vector3(5f, 0.3f, -3f), Quaternion.Euler(0f, 30f, 0f));
            CreateCube("Cargo", truck, new Vector3(0f, 2f, -1f), new Vector3(2.5f, 3f, 6f));
            CreateCube("Cab", truck, new Vector3(0f, 1.5f, 3f), new Vector3(2.5f, 2f, 2f));
            return truck;
        }

        private Transform CreateRoot(string name, Vector3 position, Quaternion rotation)
        {
            GameObject root = new GameObject(name);
            objects.Add(root);
            root.transform.SetPositionAndRotation(position, rotation);
            return root.transform;
        }

        private Transform CreateCube(string name, Transform parent, Vector3 localPosition, Vector3 localScale)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = localPosition;
            cube.transform.localScale = localScale;
            return cube.transform;
        }

        private VehicleProfile CreateProfile()
        {
            VehicleProfile profile = ScriptableObject.CreateInstance<VehicleProfile>();
            objects.Add(profile);
            return profile;
        }

        private ProfileGenerationResult Generate(Transform root, VehicleProfile profile)
        {
            root.GetComponentsInChildren(true, renderers);
            root.GetComponentsInChildren(true, colliders);
            return generator.Generate(root, renderers, colliders, exclusions, presets, profile);
        }

        private VehicleOrbit GetOrbit(VehicleProfile profile, string orbitName)
        {
            VehicleOrbit orbit;
            Assert.IsTrue(profile.TryGetOrbit(orbitName, out orbit));
            return orbit;
        }

        private Vector3 EvaluateAtBearing(ClosedBezierOrbit closedOrbit, float bearing)
        {
            float distance;
            Assert.IsTrue(closedOrbit.Bearing.TryGetDistanceAtBearing(bearing, 0f, out distance));
            return closedOrbit.EvaluateBodyLocalPosition(distance);
        }

        private void AssertVector(Vector3 expected, Vector3 actual)
        {
            Assert.AreEqual(expected.x, actual.x, Tolerance, $"x of {actual}");
            Assert.AreEqual(expected.y, actual.y, Tolerance, $"y of {actual}");
            Assert.AreEqual(expected.z, actual.z, Tolerance, $"z of {actual}");
        }

        private void AssertMarker(ClosedBezierOrbit closedOrbit, VehicleOrbit orbit, string markerName, Vector3 expectedOrbitPoint, Vector3 expectedWatchPoint)
        {
            for (int index = 0; index < orbit.WatchMarkers.Count; index++)
            {
                OrbitWatchMarker marker = orbit.WatchMarkers[index];
                if (marker.Name != markerName)
                {
                    continue;
                }

                Assert.IsFalse(marker.IsPointOfInterest);
                AssertVector(expectedOrbitPoint, closedOrbit.EvaluateBodyLocalPosition(marker.NormalizedOrbitPosition * closedOrbit.Length));
                AssertVector(expectedWatchPoint, marker.WatchPointLocalPosition);
                return;
            }

            Assert.Fail($"Marker '{markerName}' not found.");
        }

        private List<Vector3> CaptureKnots(VehicleOrbit orbit)
        {
            List<Vector3> points = new List<Vector3>();

            for (int index = 0; index < orbit.Knots.Count; index++)
            {
                points.Add(orbit.Knots[index].Anchor);
                points.Add(orbit.Knots[index].IncomingControlPoint);
                points.Add(orbit.Knots[index].OutgoingControlPoint);
            }

            points.Add(new Vector3(orbit.BaseHeight, orbit.MinimumHeightOffset, orbit.MaximumHeightOffset));
            return points;
        }

        private Vector2 CaptureRearSection(VehicleOrbit orbit)
        {
            return new Vector2(orbit.RearRemovableSection.NormalizedStartPosition, orbit.RearRemovableSection.NormalizedEndPosition);
        }

        private void AssertKnotsUnchanged(List<Vector3> expected, VehicleOrbit orbit)
        {
            List<Vector3> actual = CaptureKnots(orbit);
            Assert.AreEqual(expected.Count, actual.Count);

            for (int index = 0; index < expected.Count; index++)
            {
                Assert.AreEqual(expected[index], actual[index]);
            }
        }
    }
}
