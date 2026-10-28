using System.Collections.Generic;
using Gley.CameraSystem.Editor;
using NUnit.Framework;
using UnityEngine;

namespace Gley.CameraSystem.Tests.EditMode
{
    public class VehicleCameraValidationCollectorTests
    {
        private readonly List<EditorIssue> issues = new List<EditorIssue>();

        private CameraViewPreset presentationPreset;
        private CameraViewPreset fixedPreset;
        private VehicleProfile profile;
        private GameObject vehicle;

        [SetUp]
        public void SetUp()
        {
            presentationPreset = ScriptableObject.CreateInstance<CameraViewPreset>();
            presentationPreset.Configure(CameraViewType.Presentation);
            fixedPreset = ScriptableObject.CreateInstance<CameraViewPreset>();
            fixedPreset.Configure(CameraViewType.Fixed);
            profile = ScriptableObject.CreateInstance<VehicleProfile>();
            profile.EnsureProfileId();
            issues.Clear();
        }

        [Test]
        public void ValidProfile_NoIssues()
        {
            VehicleOrbit orbit = CreateOrbit(CreateRoundedRectangleKnots(), -4f, 0.5f);
            AddViews(orbit);
            VehicleCameraValidationCollector collector = CreateCollector(true);

            collector.CollectIssues(profile, VehicleCameraTabKind.Setup, issues);
            collector.CollectIssues(profile, VehicleCameraTabKind.Orbits, issues);
            collector.CollectIssues(profile, VehicleCameraTabKind.Views, issues);
            collector.CollectIssues(profile, VehicleCameraTabKind.Connections, issues);

            Assert.AreEqual(0, issues.Count, DescribeIssues());
        }

        [Test]
        public void DuplicateViewName_ErrorOnViewsTab()
        {
            VehicleOrbit orbit = CreateOrbit(CreateRoundedRectangleKnots(), -4f, 0.5f);
            AddViews(orbit);
            VehicleViewEntry duplicate = profile.AddView("Presentation", presentationPreset, orbit.Id);
            VehicleCameraValidationCollector collector = CreateCollector(true);

            collector.CollectIssues(profile, VehicleCameraTabKind.Views, issues);

            Assert.AreEqual(1, issues.Count, DescribeIssues());
            Assert.AreEqual(EditorIssueSeverity.Error, issues[0].Severity);
            Assert.AreEqual(duplicate.Id, issues[0].ItemId);
            Assert.AreEqual(profile, issues[0].Target);
            StringAssert.Contains("Presentation", issues[0].Message);

            issues.Clear();
            collector.CollectIssues(profile, VehicleCameraTabKind.Orbits, issues);
            collector.CollectIssues(profile, VehicleCameraTabKind.Setup, issues);

            Assert.AreEqual(0, issues.Count, DescribeIssues());
        }

        [Test]
        public void LShapedOrbit_BearingWarning()
        {
            Vector3[] corners =
            {
                new Vector3(-4f, 0f, -4f),
                new Vector3(4f, 0f, -4f),
                new Vector3(4f, 0f, -2f),
                new Vector3(-2f, 0f, -2f),
                new Vector3(-2f, 0f, 4f),
                new Vector3(-4f, 0f, 4f)
            };
            VehicleOrbit orbit = CreateOrbit(CreatePolygonKnots(corners), 0f, 0f);
            AddViews(orbit);
            VehicleCameraValidationCollector collector = CreateCollector(true);

            collector.CollectIssues(profile, VehicleCameraTabKind.Orbits, issues);

            Assert.AreEqual(1, issues.Count, DescribeIssues());
            Assert.AreEqual(EditorIssueSeverity.Warning, issues[0].Severity);
            Assert.AreEqual(orbit.Id, issues[0].ItemId);
            StringAssert.Contains("bearing", issues[0].Message);
        }

        [Test]
        public void InwardZoomTooLarge_WarningOnOrbitsTab()
        {
            VehicleOrbit orbit = CreateOrbit(CreateRoundedRectangleKnots(), -4f, 2f);
            AddViews(orbit);
            VehicleCameraValidationCollector collector = CreateCollector(true);

            collector.CollectIssues(profile, VehicleCameraTabKind.Orbits, issues);

            Assert.AreEqual(1, issues.Count, DescribeIssues());
            Assert.AreEqual(EditorIssueSeverity.Warning, issues[0].Severity);
            Assert.AreEqual(orbit.Id, issues[0].ItemId);
            StringAssert.Contains("inward zoom", issues[0].Message);
        }

        [Test]
        public void SharpCornerWithZoomRange_WarningOnOrbitsTab()
        {
            VehicleOrbit orbit = CreateOrbit(CreatePolygonKnots(CreateSquareRectangleCorners()), -4f, 1f);
            AddViews(orbit);
            VehicleCameraValidationCollector collector = CreateCollector(true);

            collector.CollectIssues(profile, VehicleCameraTabKind.Orbits, issues);

            Assert.AreEqual(1, issues.Count, DescribeIssues());
            Assert.AreEqual(EditorIssueSeverity.Warning, issues[0].Severity);
            Assert.AreEqual(orbit.Id, issues[0].ItemId);
            StringAssert.Contains("sharp corner", issues[0].Message);

            ClosedBezierOrbit closedOrbit = new ClosedBezierOrbit(orbit);
            Assert.AreEqual(90f, closedOrbit.MaximumCornerTurn, 0.01f);
        }

        [Test]
        public void SharpCornerWithZeroZoomRange_NoWarning()
        {
            VehicleOrbit orbit = CreateOrbit(CreatePolygonKnots(CreateSquareRectangleCorners()), 0f, 0f);
            AddViews(orbit);
            VehicleCameraValidationCollector collector = CreateCollector(true);

            collector.CollectIssues(profile, VehicleCameraTabKind.Orbits, issues);

            Assert.AreEqual(0, issues.Count, DescribeIssues());
        }

        [Test]
        public void RoundedCorners_NoSharpCornerWarning()
        {
            VehicleOrbit orbit = CreateOrbit(CreateRoundedRectangleKnots(), -4f, 0.5f);
            AddViews(orbit);
            VehicleCameraValidationCollector collector = CreateCollector(true);

            collector.CollectIssues(profile, VehicleCameraTabKind.Orbits, issues);

            Assert.AreEqual(0, issues.Count, DescribeIssues());
            ClosedBezierOrbit closedOrbit = new ClosedBezierOrbit(orbit);
            Assert.Less(closedOrbit.MaximumCornerTurn, 1f);
        }

        [Test]
        public void InputSystemMissing_SetupTabReportsIt()
        {
            VehicleOrbit orbit = CreateOrbit(CreateRoundedRectangleKnots(), -4f, 0.5f);
            AddViews(orbit);
            VehicleCameraValidationCollector collector = CreateCollector(false);

            collector.CollectIssues(profile, VehicleCameraTabKind.Setup, issues);

            Assert.AreEqual(1, issues.Count, DescribeIssues());
            Assert.AreEqual(EditorIssueSeverity.Warning, issues[0].Severity);
            StringAssert.Contains("Input System", issues[0].Message);

            issues.Clear();
            CreateCollector(true).CollectIssues(profile, VehicleCameraTabKind.Setup, issues);

            Assert.AreEqual(0, issues.Count, DescribeIssues());
        }

        [Test]
        public void TargetWithUninterpolatedRigidbody_Warned()
        {
            vehicle = new GameObject("Truck");
            Rigidbody bodyRigidbody = vehicle.AddComponent<Rigidbody>();
            bodyRigidbody.interpolation = RigidbodyInterpolation.None;
            VehicleCameraTarget target = vehicle.AddComponent<VehicleCameraTarget>();
            target.Configure(vehicle.transform, profile);
            VehicleCameraValidationCollector collector = CreateCollector(true);

            collector.CollectTargetIssues(target, issues);

            Assert.AreEqual(1, issues.Count, DescribeIssues());
            Assert.AreEqual(EditorIssueSeverity.Warning, issues[0].Severity);
            Assert.AreEqual(bodyRigidbody, issues[0].Target);

            issues.Clear();
            bodyRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            collector.CollectTargetIssues(target, issues);

            Assert.AreEqual(0, issues.Count, DescribeIssues());
        }

        private VehicleCameraValidationCollector CreateCollector(bool isInputSystemInstalled)
        {
            return new VehicleCameraValidationCollector(new FakeInputSystemInstallation(isInputSystemInstalled));
        }

        private VehicleOrbit CreateOrbit(List<BezierOrbitKnot> knots, float minimumZoom, float maximumZoom)
        {
            VehicleOrbit orbit = profile.AddOrbit("Presentation");
            orbit.Configure(knots, Quaternion.identity);
            orbit.ConfigureOffsetRanges(-1f, 4f, minimumZoom, maximumZoom);
            profile.AddWatchMarker(orbit, "Rear", 0f, new Vector3(0f, 1f, 0f), false);
            profile.AddWatchMarker(orbit, "Plate", 0.5f, new Vector3(0f, 1f, 2f), true);
            return orbit;
        }

        private void AddViews(VehicleOrbit orbit)
        {
            profile.AddView("Presentation", presentationPreset, orbit.Id);
            profile.AddView("Fixed", fixedPreset, 0);
        }

        private List<BezierOrbitKnot> CreateRoundedRectangleKnots()
        {
            const float control = 0.55228475f;
            return new List<BezierOrbitKnot>
            {
                CreateKnot(new Vector3(-3f, 0f, -6f), new Vector3(-3f - control, 0f, -6f), new Vector3(-1f, 0f, -6f)),
                CreateKnot(new Vector3(3f, 0f, -6f), new Vector3(1f, 0f, -6f), new Vector3(3f + control, 0f, -6f)),
                CreateKnot(new Vector3(4f, 0f, -5f), new Vector3(4f, 0f, -5f - control), new Vector3(4f, 0f, -5f + 10f / 3f)),
                CreateKnot(new Vector3(4f, 0f, 5f), new Vector3(4f, 0f, 5f - 10f / 3f), new Vector3(4f, 0f, 5f + control)),
                CreateKnot(new Vector3(3f, 0f, 6f), new Vector3(3f + control, 0f, 6f), new Vector3(1f, 0f, 6f)),
                CreateKnot(new Vector3(-3f, 0f, 6f), new Vector3(-1f, 0f, 6f), new Vector3(-3f - control, 0f, 6f)),
                CreateKnot(new Vector3(-4f, 0f, 5f), new Vector3(-4f, 0f, 5f + control), new Vector3(-4f, 0f, 5f - 10f / 3f)),
                CreateKnot(new Vector3(-4f, 0f, -5f), new Vector3(-4f, 0f, -5f + 10f / 3f), new Vector3(-4f, 0f, -5f - control))
            };
        }

        private Vector3[] CreateSquareRectangleCorners()
        {
            return new Vector3[]
            {
                new Vector3(-3f, 0f, -6f),
                new Vector3(3f, 0f, -6f),
                new Vector3(3f, 0f, 6f),
                new Vector3(-3f, 0f, 6f)
            };
        }

        private List<BezierOrbitKnot> CreatePolygonKnots(Vector3[] corners)
        {
            List<BezierOrbitKnot> knots = new List<BezierOrbitKnot>();

            for (int index = 0; index < corners.Length; index++)
            {
                Vector3 previous = corners[(index + corners.Length - 1) % corners.Length];
                Vector3 current = corners[index];
                Vector3 next = corners[(index + 1) % corners.Length];
                knots.Add(CreateKnot(current, current + (previous - current) / 3f, current + (next - current) / 3f));
            }

            return knots;
        }

        private BezierOrbitKnot CreateKnot(Vector3 anchor, Vector3 incoming, Vector3 outgoing)
        {
            BezierOrbitKnot knot = new BezierOrbitKnot();
            knot.Configure(anchor, incoming, outgoing);
            return knot;
        }

        private string DescribeIssues()
        {
            string description = string.Empty;

            for (int index = 0; index < issues.Count; index++)
            {
                description += issues[index].Severity + ": " + issues[index].Message + "\n";
            }

            return description;
        }

        [TearDown]
        public void TearDown()
        {
            if (vehicle != null)
            {
                Object.DestroyImmediate(vehicle);
            }

            Object.DestroyImmediate(profile);
            Object.DestroyImmediate(presentationPreset);
            Object.DestroyImmediate(fixedPreset);
        }
    }
}
