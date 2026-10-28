using NUnit.Framework;
using UnityEngine;

namespace Gley.CameraSystem.Tests.EditMode
{
    public class VehicleCameraTargetTests
    {
        private ChainTestProfileFactory factory;
        private VehicleCameraTarget target;
        private Transform root;
        private VehicleProfile rootProfile;

        [SetUp]
        public void SetUp()
        {
            factory = new ChainTestProfileFactory();
            root = factory.CreateBody("Root");
            rootProfile = factory.CreateProfile("Main", 20f, 10f);
            target = factory.CreateBody("Target").gameObject.AddComponent<VehicleCameraTarget>();
            target.Configure(root, rootProfile);
        }

        [Test]
        public void Attach_Rear_ChainOrderRootThenTrailer()
        {
            Transform trailer = factory.CreateBody("Trailer");
            VehicleProfile trailerProfile = factory.CreateProfile("Main", 40f, 20f);

            Assert.AreEqual(VehicleTargetResult.Accepted, target.Attach(trailer, trailerProfile, ChainEnd.Rear));
            Assert.AreEqual(2, target.BodyCount);
            Assert.AreSame(root, target.GetBody(0));
            Assert.AreSame(trailer, target.GetBody(1));
            Assert.AreSame(trailerProfile, target.GetProfile(1));
            Assert.AreEqual(0, target.RootIndex);
        }

        [Test]
        public void Attach_Front_RootIndexBecomesOne()
        {
            Transform front = factory.CreateBody("Front");
            VehicleProfile frontProfile = factory.CreateProfile("Main", 20f, 10f);

            Assert.AreEqual(VehicleTargetResult.Accepted, target.Attach(front, frontProfile, ChainEnd.Front));
            Assert.AreEqual(1, target.RootIndex);
            Assert.AreSame(front, target.GetBody(0));
            Assert.AreSame(root, target.GetBody(1));
            Transform fartherFront = factory.CreateBody("Farther front");
            Assert.AreEqual(VehicleTargetResult.Accepted, target.Attach(fartherFront, frontProfile, ChainEnd.Front));
            Assert.AreEqual(VehicleTargetResult.Accepted, target.Detach(front));
            Assert.AreEqual(1, target.BodyCount);
            Assert.AreSame(root, target.GetBody(0));
            Assert.AreEqual(0, target.RootIndex);
        }

        [Test]
        public void Attach_SameBodyTwice_RejectedBodyAlreadyInChain()
        {
            Transform trailer = factory.CreateBody("Trailer");
            VehicleProfile trailerProfile = factory.CreateProfile("Main", 40f, 20f);

            Assert.AreEqual(VehicleTargetResult.Accepted, target.Attach(trailer, trailerProfile, ChainEnd.Rear));
            Assert.AreEqual(VehicleTargetResult.BodyAlreadyInChain, target.Attach(trailer, trailerProfile, ChainEnd.Front));
            Assert.AreEqual(2, target.BodyCount);
        }

        [Test]
        public void Detach_MiddleOfThree_RemovesMiddleAndEverythingBehindIt()
        {
            Transform middle = factory.CreateBody("Middle");
            Transform rear = factory.CreateBody("Rear");
            VehicleProfile profile = factory.CreateProfile("Main", 20f, 10f);
            target.Attach(middle, profile, ChainEnd.Rear);
            target.Attach(rear, profile, ChainEnd.Rear);

            Assert.AreEqual(VehicleTargetResult.Accepted, target.Detach(middle));
            Assert.AreEqual(1, target.BodyCount);
            Assert.AreSame(root, target.GetBody(0));
            Assert.AreEqual(VehicleTargetResult.BodyNotInChain, target.Detach(rear));
        }

        [Test]
        public void Detach_Root_RejectedCannotDetachRoot()
        {
            Assert.AreEqual(VehicleTargetResult.CannotDetachRoot, target.Detach(root));
            Assert.AreEqual(1, target.BodyCount);
        }

        [Test]
        public void ChainChanged_RaisedOncePerAttachAndDetach()
        {
            int count = 0;
            int teleportCount = 0;
            Vector3 receivedShift = Vector3.zero;
            target.ChainChanged += delegate { count++; };
            target.Teleported += delegate { teleportCount++; };
            target.OriginShifted += delegate(Vector3 offset) { receivedShift = offset; };
            Transform trailer = factory.CreateBody("Trailer");
            VehicleProfile profile = factory.CreateProfile("Main", 40f, 20f);

            target.Attach(trailer, profile, ChainEnd.Rear);
            Assert.AreEqual(1, count);
            target.Detach(trailer);
            Assert.AreEqual(2, count);
            target.Teleport();
            target.ShiftOrigin(new Vector3(4f, -2f, 1f));
            Assert.AreEqual(1, teleportCount);
            Assert.AreEqual(new Vector3(4f, -2f, 1f), receivedShift);
            Assert.AreEqual(2, count);
        }

        [Test]
        public void ChainVersion_IncrementsOnChange()
        {
            int firstVersion = target.ChainVersion;
            Transform trailer = factory.CreateBody("Trailer");
            VehicleProfile profile = factory.CreateProfile("Main", 40f, 20f);

            target.Attach(trailer, profile, ChainEnd.Rear);
            Assert.AreEqual(firstVersion + 1, target.ChainVersion);
            target.Detach(trailer);
            Assert.AreEqual(firstVersion + 2, target.ChainVersion);
        }

        [Test]
        public void SuppliedSignals_SetAndClear()
        {
            target.SetSuppliedSpeed(12f);
            target.SetSuppliedAcceleration(new Vector3(1f, 2f, 3f));
            target.SetTurnHint(2f);

            Assert.IsTrue(target.HasSuppliedSpeed);
            Assert.AreEqual(12f, target.SuppliedSpeed);
            Assert.IsTrue(target.HasSuppliedAcceleration);
            Assert.AreEqual(new Vector3(1f, 2f, 3f), target.SuppliedAcceleration);
            Assert.IsTrue(target.HasTurnHint);
            Assert.AreEqual(1f, target.TurnHint);

            target.ClearSuppliedSpeed();
            target.ClearSuppliedAcceleration();
            target.ClearTurnHint();
            Assert.IsFalse(target.HasSuppliedSpeed);
            Assert.IsFalse(target.HasSuppliedAcceleration);
            Assert.IsFalse(target.HasTurnHint);
        }

        [Test]
        public void Attach_WithMatchingOverride_UsedForThatJoint()
        {
            Transform trailer = factory.CreateBody("Trailer");
            VehicleProfile trailerProfile = factory.CreateProfile("Main", 40f, 20f);
            VehicleProfile[] profiles = { rootProfile, trailerProfile };
            ChainLayout generated = new ChainLayout(profiles, 0, rootProfile.PrimaryOrbit);
            OrbitConnectorPairOverride connectorOverride = factory.CreateOverride(rootProfile, trailerProfile, generated.ConnectorPairs[0], OrbitAttachmentEnd.Rear, OrbitAttachmentEnd.Front);
            OrbitConnectorPairOverride[] overrides = { connectorOverride };
            target.ConfigurePairOverrides(overrides);

            Assert.AreEqual(VehicleTargetResult.Accepted, target.Attach(trailer, trailerProfile, ChainEnd.Rear));
            Assert.AreSame(connectorOverride, target.GetConnectorOverride(0));
            ChainOrbit orbit = new ChainOrbitBuilder().Build(target, "Main");
            Assert.IsNotNull(orbit);
            Assert.AreEqual(ChainOrbitAssemblyResult.Valid, orbit.AssemblyResult);
            bool foundConnector = false;
            for (int index = 0; index < orbit.Segments.Count; index++)
            {
                if (orbit.Segments[index].StartControlPoint == connectorOverride.RightConnector.StartControlPoint)
                {
                    foundConnector = true;
                    break;
                }
            }

            Assert.IsTrue(foundConnector);
            Assert.AreEqual(VehicleTargetResult.Accepted, target.Detach(trailer));
            target.ConfigurePairOverrides(null);
            Assert.AreEqual(VehicleTargetResult.Accepted, target.Attach(trailer, trailerProfile, ChainEnd.Rear, connectorOverride));
            Assert.AreSame(connectorOverride, target.GetConnectorOverride(0));
        }

        [Test]
        public void Attach_WithWrongOverride_OverrideDoesNotMatch()
        {
            Transform trailer = factory.CreateBody("Trailer");
            VehicleProfile trailerProfile = factory.CreateProfile("Main", 40f, 20f);
            VehicleProfile wrongProfile = factory.CreateProfile("Main", 20f, 10f);
            VehicleProfile[] profiles = { rootProfile, wrongProfile };
            ChainLayout generated = new ChainLayout(profiles, 0, rootProfile.PrimaryOrbit);
            OrbitConnectorPairOverride connectorOverride = factory.CreateOverride(rootProfile, wrongProfile, generated.ConnectorPairs[0], OrbitAttachmentEnd.Rear, OrbitAttachmentEnd.Front);

            Assert.AreEqual(VehicleTargetResult.OverrideDoesNotMatch, target.Attach(trailer, trailerProfile, ChainEnd.Rear, connectorOverride));
            Assert.AreEqual(1, target.BodyCount);
        }

        [TearDown]
        public void TearDown()
        {
            factory.Dispose();
        }
    }
}
