using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Gley.CameraSystem.Tests.EditMode
{
    public class ChainLayoutTests
    {
        private ChainTestProfileFactory factory;

        [SetUp]
        public void SetUp()
        {
            factory = new ChainTestProfileFactory();
        }

        [Test]
        public void TwoBodies_RearAnchorsMeet_StraightOffsetMatchesAnchors()
        {
            VehicleProfile front = factory.CreateProfile("Main", 20f, 10f);
            VehicleProfile rear = factory.CreateProfile("Main", 20f, 10f);
            VehicleProfile[] profiles = { front, rear };
            ChainLayout layout = new ChainLayout(profiles, 0, front.PrimaryOrbit);

            Assert.AreEqual(ChainLayoutResult.Valid, layout.LayoutResult);
            Assert.AreEqual(new Vector3(0f, 0f, 12f), layout.Offsets[1]);
            Assert.AreEqual(front.ConnectorAnchors.RearLocalPosition, rear.ConnectorAnchors.FrontLocalPosition + layout.Offsets[1]);
            GeneratedOrbitConnector connector = layout.ConnectorPairs[0].RightConnector;
            Assert.AreEqual(new Vector3(20f, 0f, 10f), connector.StartPosition);
            Assert.AreEqual(new Vector3(20f, 0f, 12f), connector.EndPosition);
            Assert.AreEqual(connector.StartPosition + (connector.EndPosition - connector.StartPosition) / 3f, connector.StartControlPoint);
            Assert.AreEqual(connector.StartPosition + (connector.EndPosition - connector.StartPosition) * (2f / 3f), connector.EndControlPoint);
        }

        [Test]
        public void RootInMiddle_FrontAndRearBodies_OffsetsOnBothSides()
        {
            VehicleProfile front = factory.CreateProfile("Main", 20f, 10f);
            VehicleProfile root = factory.CreateProfile("Main", 20f, 10f);
            VehicleProfile rear = factory.CreateProfile("Main", 20f, 10f);
            VehicleProfile[] profiles = { front, root, rear };
            ChainLayout layout = new ChainLayout(profiles, 1, root.PrimaryOrbit);

            Assert.AreEqual(ChainLayoutResult.Valid, layout.LayoutResult);
            Assert.AreEqual(new Vector3(0f, 0f, -12f), layout.Offsets[0]);
            Assert.AreEqual(Vector3.zero, layout.Offsets[1]);
            Assert.AreEqual(new Vector3(0f, 0f, 12f), layout.Offsets[2]);
            Assert.AreEqual(front.ConnectorAnchors.RearLocalPosition + layout.Offsets[0], root.ConnectorAnchors.FrontLocalPosition);
            Assert.AreEqual(root.ConnectorAnchors.RearLocalPosition, rear.ConnectorAnchors.FrontLocalPosition + layout.Offsets[2]);
        }

        [Test]
        public void RotatedScaledBodies_AlignedAnchors_AssembleInAdjustedPlane()
        {
            VehicleProfile front = factory.CreateProfile("Main", 20f, 10f);
            VehicleProfile rear = factory.CreateProfile("Main", 40f, 20f);
            front.PrimaryOrbit.RearRemovableSection.Configure(0.49f, 0.85f);
            rear.PrimaryOrbit.FrontRemovableSection.Configure(0f, 1f / 3f);
            Quaternion adjustment = Quaternion.Euler(27f, 68f, -19f);
            front.PrimaryOrbit.Configure(new List<BezierOrbitKnot>(front.PrimaryOrbit.Knots), adjustment);
            rear.PrimaryOrbit.Configure(new List<BezierOrbitKnot>(rear.PrimaryOrbit.Knots), adjustment);
            VehicleConnectorAnchors frontAnchors = new VehicleConnectorAnchors();
            frontAnchors.Configure(adjustment * new Vector3(10f, 0f, 0f), adjustment * new Vector3(10f, 0f, 10f));
            front.ConfigureConnectorAnchors(frontAnchors);
            VehicleConnectorAnchors rearAnchors = new VehicleConnectorAnchors();
            rearAnchors.Configure(adjustment * new Vector3(20f, 0f, 0f), adjustment * new Vector3(20f, 0f, 20f));
            rear.ConfigureConnectorAnchors(rearAnchors);
            VehicleProfile[] profiles = { front, rear };
            ChainLayout layout = new ChainLayout(profiles, 0, front.PrimaryOrbit);
            ChainOrbit orbit = new ChainOrbit(layout, null, 0);

            Assert.AreEqual(ChainLayoutResult.Valid, layout.LayoutResult);
            Assert.AreEqual(ChainOrbitAssemblyResult.Valid, orbit.AssemblyResult);
            Assert.Less(Vector3.Distance(adjustment * new Vector3(-10f, 0f, 10f), layout.Offsets[1]), 0.0001f);
            Assert.Less(Mathf.Abs(Vector3.Dot(orbit.Segments[0].StartPosition, adjustment * Vector3.up)), 0.0001f);
        }

        [Test]
        public void PairOverride_Matching_ReplacesGeneratedConnector()
        {
            VehicleProfile front = factory.CreateProfile("Main", 20f, 10f);
            VehicleProfile rear = factory.CreateProfile("Main", 20f, 10f);
            VehicleProfile[] profiles = { front, rear };
            ChainLayout generated = new ChainLayout(profiles, 0, front.PrimaryOrbit);
            OrbitConnectorPairOverride pairOverride = factory.CreateOverride(front, rear, generated.ConnectorPairs[0], OrbitAttachmentEnd.Rear, OrbitAttachmentEnd.Front);
            OrbitConnectorPairOverride[] overrides = { pairOverride };
            ChainLayout layout = new ChainLayout(profiles, 0, front.PrimaryOrbit, overrides);

            Assert.AreEqual(ChainLayoutResult.Valid, layout.LayoutResult);
            Assert.AreEqual(generated.ConnectorPairs[0].LeftConnector.StartPosition, layout.ConnectorPairs[0].LeftConnector.StartPosition);
            Assert.AreNotEqual(generated.ConnectorPairs[0].LeftConnector.StartControlPoint, layout.ConnectorPairs[0].LeftConnector.StartControlPoint);
        }

        [Test]
        public void PairOverride_WrongEnds_Ignored()
        {
            VehicleProfile front = factory.CreateProfile("Main", 20f, 10f);
            VehicleProfile rear = factory.CreateProfile("Main", 20f, 10f);
            VehicleProfile[] profiles = { front, rear };
            ChainLayout generated = new ChainLayout(profiles, 0, front.PrimaryOrbit);
            OrbitConnectorPairOverride pairOverride = factory.CreateOverride(front, rear, generated.ConnectorPairs[0], OrbitAttachmentEnd.Front, OrbitAttachmentEnd.Rear);
            OrbitConnectorPairOverride[] overrides = { pairOverride };
            ChainLayout layout = new ChainLayout(profiles, 0, front.PrimaryOrbit, overrides);

            Assert.AreEqual(ChainLayoutResult.Valid, layout.LayoutResult);
            Assert.AreEqual(generated.ConnectorPairs[0].LeftConnector.StartControlPoint, layout.ConnectorPairs[0].LeftConnector.StartControlPoint);
        }

        [TearDown]
        public void TearDown()
        {
            factory.Dispose();
        }
    }
}
