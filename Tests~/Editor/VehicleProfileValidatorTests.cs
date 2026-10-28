using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Gley.CameraSystem.Tests.EditMode
{
    public class VehicleProfileValidatorTests
    {
        private CameraViewPreset fixedViewPreset;
        private CameraViewPreset orbitViewPreset;
        private VehicleProfile profile;
        private VehicleProfileValidator validator;

        [SetUp]
        public void SetUp()
        {
            fixedViewPreset = ScriptableObject.CreateInstance<CameraViewPreset>();
            fixedViewPreset.Configure(CameraViewType.Fixed);
            orbitViewPreset = ScriptableObject.CreateInstance<CameraViewPreset>();
            orbitViewPreset.Configure(CameraViewType.Presentation);
            profile = ScriptableObject.CreateInstance<VehicleProfile>();
            profile.EnsureProfileId();
            validator = new VehicleProfileValidator();
        }

        [Test]
        public void Validate_ValidProfile_NoIssues()
        {
            VehicleOrbit orbit = profile.AddOrbit("Main");
            profile.AddWatchMarker(orbit, "Rear", 0f, Vector3.forward, false);
            profile.AddWatchMarker(orbit, "Plate", 0.5f, Vector3.back, true);
            profile.AddView("Presentation", orbitViewPreset, orbit.Id);
            profile.AddView("Fixed", fixedViewPreset, 0);

            VehicleProfileValidationReport report = validator.Validate(profile);

            Assert.IsFalse(report.HasErrors);
            Assert.AreEqual(0, report.Issues.Count);
        }

        [Test]
        public void Validate_DuplicateOrbitName_ReportsIdAndName()
        {
            profile.AddOrbit("Main");
            VehicleOrbit duplicateOrbit = profile.AddOrbit("Main");
            profile.AddView("Fixed", fixedViewPreset, 0);

            VehicleProfileValidationReport report = validator.Validate(profile);

            Assert.IsTrue(report.HasErrors);
            Assert.IsTrue(report.TryGetIssue(VehicleProfileIssueCode.DuplicateOrbitName, out VehicleProfileIssue issue));
            Assert.AreEqual(duplicateOrbit.Id, issue.ItemId);
            Assert.AreEqual("Main", issue.ItemName);
            Assert.AreEqual(1, report.Issues.Count);
        }

        [Test]
        public void Validate_DuplicateMarkerNameInSameOrbit_Reported()
        {
            VehicleOrbit orbit = profile.AddOrbit("Main");
            profile.AddWatchMarker(orbit, "Plate", 0f, Vector3.forward, true);
            OrbitWatchMarker duplicateMarker = profile.AddWatchMarker(orbit, "Plate", 0.5f, Vector3.back, true);
            profile.AddView("Fixed", fixedViewPreset, 0);

            VehicleProfileValidationReport report = validator.Validate(profile);

            Assert.IsTrue(report.TryGetIssue(VehicleProfileIssueCode.DuplicateMarkerName, out VehicleProfileIssue issue));
            Assert.AreEqual(duplicateMarker.Id, issue.ItemId);
            Assert.AreEqual("Plate", issue.ItemName);
            Assert.AreEqual(1, report.Issues.Count);
        }

        [Test]
        public void Validate_SameMarkerNameInDifferentOrbits_Allowed()
        {
            VehicleOrbit firstOrbit = profile.AddOrbit("Driving");
            VehicleOrbit secondOrbit = profile.AddOrbit("Presentation");
            profile.AddWatchMarker(firstOrbit, "Rear", 0f, Vector3.forward, false);
            profile.AddWatchMarker(secondOrbit, "Rear", 0f, Vector3.forward, false);
            profile.AddView("Fixed", fixedViewPreset, 0);

            VehicleProfileValidationReport report = validator.Validate(profile);

            Assert.IsFalse(report.HasErrors);
        }

        [Test]
        public void Validate_ViewReferencesMissingOrbit_Reported()
        {
            VehicleOrbit orbit = profile.AddOrbit("Main");
            VehicleViewEntry view = profile.AddView("Presentation", orbitViewPreset, orbit.Id + 100);
            profile.AddView("Fixed", fixedViewPreset, orbit.Id + 100);

            VehicleProfileValidationReport report = validator.Validate(profile);

            Assert.IsTrue(report.TryGetIssue(VehicleProfileIssueCode.ViewOrbitNotFound, out VehicleProfileIssue issue));
            Assert.AreEqual(view.Id, issue.ItemId);
            Assert.AreEqual("Presentation", issue.ItemName);
            Assert.AreEqual(1, report.Issues.Count);
        }

        [Test]
        public void Validate_FormatVersionTwo_ReportsNewerThanSupported()
        {
            profile.AddView("Fixed", fixedViewPreset, 0);
            SerializedObject serializedProfile = new SerializedObject(profile);
            serializedProfile.FindProperty("formatVersion").intValue = 2;
            serializedProfile.ApplyModifiedPropertiesWithoutUndo();

            VehicleProfileValidationReport report = validator.Validate(profile);

            Assert.AreEqual(2, profile.FormatVersion);
            Assert.IsTrue(report.TryGetIssue(VehicleProfileIssueCode.FormatVersionNewerThanSupported, out VehicleProfileIssue _));
            Assert.AreEqual(1, report.Issues.Count);
        }

        [Test]
        public void Validate_EmptyName_Reported()
        {
            VehicleOrbit orbit = profile.AddOrbit("  ");
            profile.AddView("Fixed", fixedViewPreset, 0);

            VehicleProfileValidationReport report = validator.Validate(profile);

            Assert.IsTrue(report.TryGetIssue(VehicleProfileIssueCode.EmptyName, out VehicleProfileIssue issue));
            Assert.AreEqual(orbit.Id, issue.ItemId);
            Assert.AreEqual(1, report.Issues.Count);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(profile);
            Object.DestroyImmediate(fixedViewPreset);
            Object.DestroyImmediate(orbitViewPreset);
        }
    }
}
