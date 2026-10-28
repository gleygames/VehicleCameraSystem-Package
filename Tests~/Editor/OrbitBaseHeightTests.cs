using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Gley.CameraSystem.Tests.EditMode
{
    public class OrbitBaseHeightTests
    {
        private const float Tolerance = 0.0001f;

        private ChainTestProfileFactory factory;

        [SetUp]
        public void SetUp()
        {
            factory = new ChainTestProfileFactory();
        }

        [TearDown]
        public void TearDown()
        {
            factory.Dispose();
        }

        [Test]
        public void EvaluateBodyLocalPosition_BaseHeight_RaisesCurveWithoutChangingShape()
        {
            VehicleProfile flat = factory.CreateRoundedProfile("Main", 1.5f, 4f, 4f);
            VehicleProfile raised = factory.CreateRoundedProfile("Main", 1.5f, 4f, 4f);
            raised.PrimaryOrbit.ConfigureBaseHeight(1.5f);
            ClosedBezierOrbit flatOrbit = new ClosedBezierOrbit(flat.PrimaryOrbit);
            ClosedBezierOrbit raisedOrbit = new ClosedBezierOrbit(raised.PrimaryOrbit);

            Assert.AreEqual(OrbitValidationResult.Valid, raisedOrbit.ValidationResult);
            Assert.AreEqual(flatOrbit.Length, raisedOrbit.Length, Tolerance);

            for (int sampleIndex = 0; sampleIndex < 16; sampleIndex++)
            {
                float distance = flatOrbit.Length * sampleIndex / 16f;
                Vector3 flatPosition = flatOrbit.EvaluateBodyLocalPosition(distance);
                Vector3 raisedPosition = raisedOrbit.EvaluateBodyLocalPosition(distance);
                Assert.AreEqual(flatPosition.x, raisedPosition.x, Tolerance);
                Assert.AreEqual(1.5f, raisedPosition.y, Tolerance);
                Assert.AreEqual(flatPosition.z, raisedPosition.z, Tolerance);
            }

            Assert.AreEqual(flatOrbit.Bearing.BearingOfLocalPoint(new Vector3(1f, 0f, 1f)), raisedOrbit.Bearing.BearingOfLocalPoint(new Vector3(1f, 0f, 1f)), Tolerance);
        }

        [Test]
        public void ChainLayout_AnchorsAtBaseHeight_AssemblesOnRootPlane()
        {
            VehicleProfile truck = CreateRaisedProfile(1f);
            VehicleProfile trailer = CreateRaisedProfile(1.5f);
            Transform[] bodies = { factory.CreateBody("Truck"), factory.CreateBody("Trailer") };

            ChainLayout layout = new ChainLayout(new List<VehicleProfile> { truck, trailer }, 0, truck.PrimaryOrbit);
            ChainOrbit chainOrbit = new ChainOrbit(layout, bodies, 0);

            Assert.AreEqual(ChainLayoutResult.Valid, layout.LayoutResult);
            Assert.IsTrue(chainOrbit.IsClosed);
            Assert.AreEqual(-0.5f, layout.Offsets[1].y, Tolerance);

            for (int sampleIndex = 0; sampleIndex < 32; sampleIndex++)
            {
                Assert.AreEqual(1f, chainOrbit.EvaluateRootLocalPosition(chainOrbit.Length * sampleIndex / 32f).y, Tolerance);
            }
        }

        [Test]
        public void ChainLayout_AnchorsOffBaseHeight_InvalidConnector()
        {
            VehicleProfile truck = factory.CreateRoundedProfile("Main", 1.5f, 4f, 4f);
            VehicleProfile trailer = factory.CreateRoundedProfile("Main", 1.5f, 6f, 6f);
            truck.PrimaryOrbit.ConfigureBaseHeight(1f);

            ChainLayout layout = new ChainLayout(new List<VehicleProfile> { truck, trailer }, 0, truck.PrimaryOrbit);

            Assert.AreEqual(ChainLayoutResult.InvalidConnector, layout.LayoutResult);
        }

        private VehicleProfile CreateRaisedProfile(float baseHeight)
        {
            VehicleProfile profile = factory.CreateRoundedProfile("Main", 1.5f, 4f, 4f);
            profile.PrimaryOrbit.ConfigureBaseHeight(baseHeight);
            VehicleConnectorAnchors anchors = new VehicleConnectorAnchors();
            anchors.Configure(profile.ConnectorAnchors.FrontLocalPosition + Vector3.up * baseHeight, profile.ConnectorAnchors.RearLocalPosition + Vector3.up * baseHeight);
            profile.ConfigureConnectorAnchors(anchors);
            return profile;
        }
    }
}
