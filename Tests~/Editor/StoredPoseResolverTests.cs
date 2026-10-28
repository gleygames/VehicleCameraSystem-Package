using NUnit.Framework;
using UnityEngine;

namespace Gley.CameraSystem.Tests.EditMode
{
    public class StoredPoseResolverTests
    {
        private ChainTestProfileFactory factory;

        [SetUp]
        public void SetUp()
        {
            factory = new ChainTestProfileFactory();
        }

        [Test]
        public void Resolve_BearingZero_OnTruckAndOnTruckTrailer_BothDirectlyBehindChain()
        {
            VehicleProfile truck = factory.CreateProfile("Main", 20f, 10f);
            VehicleProfile trailer = factory.CreateProfile("Main", 20f, 10f);
            Transform truckBody = factory.CreateBody("Truck");
            Transform trailerBody = factory.CreateBody("Trailer");
            ChainOrbit truckOrbit = CreateOrbit(new VehicleProfile[] { truck }, new Transform[] { truckBody });
            ChainOrbit chainOrbit = CreateOrbit(new VehicleProfile[] { truck, trailer }, new Transform[] { truckBody, trailerBody });
            StoredPoseResolver resolver = new StoredPoseResolver();
            OrbitPose stored = new OrbitPose(0f, 0f, 0f);

            LivePose truckPose = resolver.Resolve(stored, truckOrbit, truck.PrimaryOrbit, 0f);
            LivePose chainPose = resolver.Resolve(stored, chainOrbit, truck.PrimaryOrbit, 0f);
            Vector3 truckPosition = truckOrbit.EvaluateRootLocalPosition(truckPose.OrbitDistance);
            Vector3 chainPosition = chainOrbit.EvaluateRootLocalPosition(chainPose.OrbitDistance);

            Assert.AreEqual(truckOrbit.Bearing.Centre.x, truckPosition.x, 0.001f);
            Assert.Less(truckPosition.z, truckOrbit.Bearing.Centre.z);
            Assert.AreEqual(chainOrbit.Bearing.Centre.x, chainPosition.x, 0.001f);
            Assert.Less(chainPosition.z, chainOrbit.Bearing.Centre.z);
        }

        [Test]
        public void Resolve_ZoomOutsideRange_Clamped()
        {
            VehicleProfile truck = factory.CreateProfile("Main", 20f, 10f);
            truck.PrimaryOrbit.ConfigureOffsetRanges(-1f, 2f, -2f, 3f);
            Transform truckBody = factory.CreateBody("Truck");
            ChainOrbit orbit = CreateOrbit(new VehicleProfile[] { truck }, new Transform[] { truckBody });

            LivePose pose = new StoredPoseResolver().Resolve(new OrbitPose(0f, 8f, -5f), orbit, truck.PrimaryOrbit, 0f);

            Assert.AreEqual(3f, pose.ZoomOffset);
            Assert.AreEqual(-1f, pose.HeightOffset);
        }

        [Test]
        public void CarryAdjustment_PlayerZoomedInOneMetre_KeepsOneMetreRelativeToNewDefault()
        {
            VehicleProfile truck = factory.CreateProfile("Main", 20f, 10f);
            truck.PrimaryOrbit.ConfigureOffsetRanges(-5f, 5f, -5f, 5f);
            LivePose current = new LivePose(12f, 2f, 1.5f);

            LivePose result = new StoredPoseResolver().CarryAdjustment(current, new OrbitPose(0f, 1f, 1f), new OrbitPose(0f, -1f, -1f), truck.PrimaryOrbit);

            Assert.AreEqual(12f, result.OrbitDistance);
            Assert.AreEqual(0f, result.ZoomOffset);
            Assert.AreEqual(-0.5f, result.HeightOffset);
        }

        [Test]
        public void CarryAdjustment_ResultOutsideNewRange_Clamped()
        {
            VehicleProfile truck = factory.CreateProfile("Main", 20f, 10f);
            truck.PrimaryOrbit.ConfigureOffsetRanges(-1f, 1f, -2f, 2f);
            LivePose current = new LivePose(12f, 3f, -3f);

            LivePose result = new StoredPoseResolver().CarryAdjustment(current, new OrbitPose(0f, 0f, 0f), new OrbitPose(0f, 1f, -1f), truck.PrimaryOrbit);

            Assert.AreEqual(2f, result.ZoomOffset);
            Assert.AreEqual(-1f, result.HeightOffset);
        }

        [TearDown]
        public void TearDown()
        {
            factory.Dispose();
        }

        private ChainOrbit CreateOrbit(VehicleProfile[] profiles, Transform[] bodies)
        {
            ChainLayout layout = new ChainLayout(profiles, 0, profiles[0].PrimaryOrbit);
            return new ChainOrbit(layout, bodies, 0);
        }
    }
}
