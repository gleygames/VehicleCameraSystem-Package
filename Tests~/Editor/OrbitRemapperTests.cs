using NUnit.Framework;
using UnityEngine;

namespace Gley.CameraSystem.Tests.EditMode
{
    public class OrbitRemapperTests
    {
        private ChainTestProfileFactory factory;

        [SetUp]
        public void SetUp()
        {
            factory = new ChainTestProfileFactory();
        }

        [Test]
        public void AttachTrailer_CameraOnTruckFront_Survives()
        {
            VehicleProfile truck = factory.CreateProfile("Main", 20f, 10f);
            VehicleProfile trailer = factory.CreateProfile("Main", 20f, 10f);
            Transform truckBody = factory.CreateBody("Truck");
            Transform trailerBody = factory.CreateBody("Trailer");
            ChainOrbit oldOrbit = CreateOrbit(new VehicleProfile[] { truck }, new Transform[] { truckBody });
            ChainOrbit newOrbit = CreateOrbit(new VehicleProfile[] { truck, trailer }, new Transform[] { truckBody, trailerBody });

            float distance;
            OrbitRemapResult result = new OrbitRemapper().Remap(oldOrbit, 25f, newOrbit, out distance);

            Assert.AreEqual(OrbitRemapResult.Survived, result);
            Assert.Less(Vector3.Distance(new Vector3(20f, 0f, 5f), newOrbit.EvaluateRootLocalPosition(distance)), 0.001f);
        }

        [Test]
        public void AttachTrailer_CameraOnTruckRearSection_MovesToTrailerRearMidpoint()
        {
            VehicleProfile truck = factory.CreateProfile("Main", 20f, 10f);
            VehicleProfile trailer = factory.CreateProfile("Main", 20f, 10f);
            Transform truckBody = factory.CreateBody("Truck");
            Transform trailerBody = factory.CreateBody("Trailer");
            ChainOrbit oldOrbit = CreateOrbit(new VehicleProfile[] { truck }, new Transform[] { truckBody });
            ChainOrbit newOrbit = CreateOrbit(new VehicleProfile[] { truck, trailer }, new Transform[] { truckBody, trailerBody });
            ChainLayout layout = new ChainLayout(new VehicleProfile[] { truck, trailer }, 0, truck.PrimaryOrbit);

            float distance;
            OrbitRemapResult result = new OrbitRemapper().Remap(oldOrbit, 40f, newOrbit, out distance);

            Assert.AreEqual(OrbitRemapResult.MovedToRearFallback, result);
            Assert.Less(Vector3.Distance(new Vector3(10f, 0f, 10f) + layout.Offsets[1], newOrbit.EvaluateRootLocalPosition(distance)), 0.001f);
        }

        [Test]
        public void DetachTrailer_CameraOnTrailer_MovesToTruckRearMidpoint()
        {
            VehicleProfile truck = factory.CreateProfile("Main", 20f, 10f);
            VehicleProfile trailer = factory.CreateProfile("Main", 20f, 10f);
            Transform truckBody = factory.CreateBody("Truck");
            Transform trailerBody = factory.CreateBody("Trailer");
            ChainOrbit oldOrbit = CreateOrbit(new VehicleProfile[] { truck, trailer }, new Transform[] { truckBody, trailerBody });
            ChainOrbit newOrbit = CreateOrbit(new VehicleProfile[] { truck }, new Transform[] { truckBody });
            float trailerDistance;
            Assert.IsTrue(oldOrbit.TryGetRetainedSourceDistance(trailerBody, trailer.PrimaryOrbit, 25f, out trailerDistance));

            float distance;
            OrbitRemapResult result = new OrbitRemapper().Remap(oldOrbit, trailerDistance, newOrbit, out distance);

            Assert.AreEqual(OrbitRemapResult.MovedToRearFallback, result);
            Assert.Less(Vector3.Distance(new Vector3(10f, 0f, 10f), newOrbit.EvaluateRootLocalPosition(distance)), 0.001f);
        }

        [Test]
        public void ThreeBodies_RemoveMiddle_CameraOnRearBody_MovesToFallback()
        {
            VehicleProfile truck = factory.CreateProfile("Main", 20f, 10f);
            VehicleProfile middle = factory.CreateProfile("Main", 20f, 10f);
            VehicleProfile rear = factory.CreateProfile("Main", 20f, 10f);
            Transform truckBody = factory.CreateBody("Truck");
            Transform middleBody = factory.CreateBody("Middle");
            Transform rearBody = factory.CreateBody("Rear");
            ChainOrbit oldOrbit = CreateOrbit(new VehicleProfile[] { truck, middle, rear }, new Transform[] { truckBody, middleBody, rearBody });
            ChainOrbit newOrbit = CreateOrbit(new VehicleProfile[] { truck }, new Transform[] { truckBody });
            float rearDistance;
            Assert.IsTrue(oldOrbit.TryGetRetainedSourceDistance(rearBody, rear.PrimaryOrbit, 25f, out rearDistance));

            float distance;
            OrbitRemapResult result = new OrbitRemapper().Remap(oldOrbit, rearDistance, newOrbit, out distance);

            Assert.AreEqual(OrbitRemapResult.MovedToRearFallback, result);
            Assert.Less(Vector3.Distance(new Vector3(10f, 0f, 10f), newOrbit.EvaluateRootLocalPosition(distance)), 0.001f);
        }

        [Test]
        public void TwoTrailersSameProfile_SurvivalUsesBodyInstanceNotProfile()
        {
            VehicleProfile truck = factory.CreateProfile("Main", 20f, 10f);
            VehicleProfile trailer = factory.CreateProfile("Main", 20f, 10f);
            Transform truckBody = factory.CreateBody("Truck");
            Transform oldTrailerBody = factory.CreateBody("Old Trailer");
            Transform newTrailerBody = factory.CreateBody("New Trailer");
            ChainOrbit oldOrbit = CreateOrbit(new VehicleProfile[] { truck, trailer }, new Transform[] { truckBody, oldTrailerBody });
            ChainOrbit newOrbit = CreateOrbit(new VehicleProfile[] { truck, trailer }, new Transform[] { truckBody, newTrailerBody });
            float oldDistance;
            Assert.IsTrue(oldOrbit.TryGetRetainedSourceDistance(oldTrailerBody, trailer.PrimaryOrbit, 25f, out oldDistance));

            float distance;
            OrbitRemapResult result = new OrbitRemapper().Remap(oldOrbit, oldDistance, newOrbit, out distance);

            Assert.AreEqual(OrbitRemapResult.MovedToRearFallback, result);
            Assert.Less(Vector3.Distance(newOrbit.EvaluateRootLocalPosition(distance), new Vector3(10f, 0f, 10f) + new ChainLayout(new VehicleProfile[] { truck, trailer }, 0, truck.PrimaryOrbit).Offsets[1]), 0.001f);
        }

        [Test]
        public void InvalidNewOrbit_ReturnsInvalidAndLeavesDistance()
        {
            VehicleProfile truck = factory.CreateProfile("Main", 20f, 10f);
            Transform truckBody = factory.CreateBody("Truck");
            ChainOrbit oldOrbit = CreateOrbit(new VehicleProfile[] { truck }, new Transform[] { truckBody });

            float distance;
            OrbitRemapResult result = new OrbitRemapper().Remap(oldOrbit, 25f, null, out distance);

            Assert.AreEqual(OrbitRemapResult.InvalidNewOrbit, result);
            Assert.AreEqual(25f, distance);
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
