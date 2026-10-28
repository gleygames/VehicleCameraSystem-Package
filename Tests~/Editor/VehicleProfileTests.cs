using System;
using NUnit.Framework;
using UnityEngine;

namespace Gley.CameraSystem.Tests.EditMode
{
    public class VehicleProfileTests
    {
        private CameraViewPreset viewPreset;
        private VehicleProfile profile;

        [SetUp]
        public void SetUp()
        {
            profile = ScriptableObject.CreateInstance<VehicleProfile>();
            viewPreset = ScriptableObject.CreateInstance<CameraViewPreset>();
            viewPreset.Configure(CameraViewType.Presentation);
        }

        [Test]
        public void AddOrbit_AllocatesIncreasingIds()
        {
            VehicleOrbit firstOrbit = profile.AddOrbit("First");
            VehicleOrbit secondOrbit = profile.AddOrbit("Second");
            VehicleOrbit thirdOrbit = profile.AddOrbit("Third");

            Assert.AreEqual(1, firstOrbit.Id);
            Assert.AreEqual(2, secondOrbit.Id);
            Assert.AreEqual(3, thirdOrbit.Id);
            Assert.AreEqual("Second", secondOrbit.Name);
            Assert.AreEqual(3, profile.Orbits.Count);
        }

        [Test]
        public void StableId_NotReusedAfterRemoval()
        {
            VehicleOrbit firstOrbit = profile.AddOrbit("First");
            VehicleOrbit secondOrbit = profile.AddOrbit("Second");
            OrbitWatchMarker marker = profile.AddWatchMarker(secondOrbit, "Plate", 0.5f, Vector3.one, true);
            VehicleViewEntry view = profile.AddView("Garage", viewPreset, secondOrbit.Id);

            bool removed = profile.RemoveOrbit(secondOrbit.Id);
            VehicleOrbit replacementOrbit = profile.AddOrbit("Replacement");

            Assert.IsTrue(removed);
            Assert.AreEqual(2, profile.Orbits.Count);
            Assert.IsFalse(profile.TryGetOrbit(secondOrbit.Id, out VehicleOrbit _));
            Assert.Greater(replacementOrbit.Id, firstOrbit.Id);
            Assert.Greater(replacementOrbit.Id, secondOrbit.Id);
            Assert.Greater(replacementOrbit.Id, marker.Id);
            Assert.Greater(replacementOrbit.Id, view.Id);
        }

        [Test]
        public void NewProfile_HasFormatVersionOneAndProfileId()
        {
            profile.EnsureProfileId();
            string profileId = profile.ProfileId;
            profile.EnsureProfileId();

            Assert.AreEqual(1, VehicleProfile.CurrentFormatVersion);
            Assert.AreEqual(1, profile.FormatVersion);
            Assert.IsFalse(string.IsNullOrEmpty(profileId));
            Assert.IsTrue(Guid.TryParse(profileId, out Guid _));
            Assert.AreEqual(profileId, profile.ProfileId);
            Assert.AreEqual(0, profile.Orbits.Count);
            Assert.AreEqual(0, profile.Views.Count);
            Assert.IsNotNull(profile.FixedViewPose);
            Assert.IsNotNull(profile.Seat);
        }

        [Test]
        public void TryGetView_ByNameAndById_FindsSameEntry()
        {
            VehicleOrbit orbit = profile.AddOrbit("Main");
            profile.AddView("Driving", viewPreset, orbit.Id);
            VehicleViewEntry addedView = profile.AddView("Presentation", viewPreset, orbit.Id);

            bool foundByName = profile.TryGetView("Presentation", out VehicleViewEntry viewByName);
            bool foundById = profile.TryGetView(addedView.Id, out VehicleViewEntry viewById);
            bool foundMissing = profile.TryGetView("Missing", out VehicleViewEntry missingView);

            Assert.IsTrue(foundByName);
            Assert.IsTrue(foundById);
            Assert.AreSame(addedView, viewByName);
            Assert.AreSame(addedView, viewById);
            Assert.AreSame(viewPreset, viewByName.Preset);
            Assert.AreEqual(orbit.Id, viewByName.OrbitId);
            Assert.AreEqual(180f, viewByName.FrontDefaultPose.Bearing);
            Assert.IsFalse(foundMissing);
            Assert.IsNull(missingView);
        }

        [Test]
        public void PrimaryOrbit_IsFirstOrbit()
        {
            Assert.IsNull(profile.PrimaryOrbit);

            VehicleOrbit firstOrbit = profile.AddOrbit("First");
            profile.AddOrbit("Second");

            Assert.AreSame(firstOrbit, profile.PrimaryOrbit);
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(profile);
            UnityEngine.Object.DestroyImmediate(viewPreset);
        }
    }
}
