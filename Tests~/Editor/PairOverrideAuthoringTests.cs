using Gley.CameraSystem.Editor;
using NUnit.Framework;
using UnityEngine;

namespace Gley.CameraSystem.Tests.EditMode
{
    public class PairOverrideAuthoringTests
    {
        private const float Tolerance = 0.0001f;

        private ChainTestProfileFactory factory;
        private PairOverrideAuthoring authoring;
        private OrbitConnectorPairOverride createdOverride;

        [SetUp]
        public void SetUp()
        {
            factory = new ChainTestProfileFactory();
            authoring = new PairOverrideAuthoring();
        }

        [TearDown]
        public void TearDown()
        {
            if (createdOverride != null)
            {
                Object.DestroyImmediate(createdOverride);
            }

            factory.Dispose();
        }

        [Test]
        public void CreateOverride_InitializedFromGeneratedConnectors()
        {
            VehicleProfile front = factory.CreateProfile("Main", 20f, 10f);
            VehicleProfile rear = factory.CreateProfile("Main", 20f, 10f);

            createdOverride = authoring.CreateOverride(front, rear, "Main");

            Assert.IsNotNull(createdOverride);
            Assert.IsTrue(createdOverride.Matches(front, rear, OrbitAttachmentEnd.Rear, OrbitAttachmentEnd.Front));
            AssertStraightConnector(new Vector3(0f, 0f, 10f), new Vector3(0f, 0f, 12f), createdOverride.LeftConnector);
            AssertStraightConnector(new Vector3(20f, 0f, 10f), new Vector3(20f, 0f, 12f), createdOverride.RightConnector);
        }

        [Test]
        public void CreatedOverride_UsedByChainLayout()
        {
            VehicleProfile front = factory.CreateProfile("Main", 20f, 10f);
            VehicleProfile rear = factory.CreateProfile("Main", 20f, 10f);
            createdOverride = authoring.CreateOverride(front, rear, "Main");
            OrbitConnectorGeometry right = createdOverride.RightConnector;
            Vector3 bentStartControl = new Vector3(21f, 0f, 10f + 2f / 3f);
            Vector3 bentEndControl = new Vector3(21f, 0f, 10f + 4f / 3f);
            right.Configure(right.StartPosition, bentStartControl, bentEndControl, right.EndPosition);
            VehicleProfile[] profiles = { front, rear };
            OrbitConnectorPairOverride[] jointOverrides = { createdOverride };

            ChainLayout jointLayout = new ChainLayout(profiles, 0, front.PrimaryOrbit, null, jointOverrides);
            ChainLayout pairLayout = new ChainLayout(profiles, 0, front.PrimaryOrbit, jointOverrides);

            Assert.AreEqual(ChainLayoutResult.Valid, jointLayout.LayoutResult);
            Assert.AreEqual(ChainLayoutResult.Valid, pairLayout.LayoutResult);
            AssertVector(bentStartControl, jointLayout.ConnectorPairs[0].RightConnector.StartControlPoint);
            AssertVector(bentEndControl, jointLayout.ConnectorPairs[0].RightConnector.EndControlPoint);
            AssertVector(bentStartControl, pairLayout.ConnectorPairs[0].RightConnector.StartControlPoint);
            GeneratedOrbitConnectorPair generated;
            Assert.IsTrue(authoring.TryGetGeneratedPair(front, rear, "Main", out generated));
            Assert.IsTrue(authoring.MatchesGeneratedEnds(createdOverride, generated));
        }

        private void AssertStraightConnector(Vector3 expectedStart, Vector3 expectedEnd, OrbitConnectorGeometry connector)
        {
            Assert.IsNotNull(connector);
            AssertVector(expectedStart, connector.StartPosition);
            AssertVector(expectedEnd, connector.EndPosition);
            AssertVector(expectedStart + (expectedEnd - expectedStart) / 3f, connector.StartControlPoint);
            AssertVector(expectedStart + (expectedEnd - expectedStart) * (2f / 3f), connector.EndControlPoint);
        }

        private void AssertVector(Vector3 expected, Vector3 actual)
        {
            Assert.AreEqual(expected.x, actual.x, Tolerance, $"x of {actual}");
            Assert.AreEqual(expected.y, actual.y, Tolerance, $"y of {actual}");
            Assert.AreEqual(expected.z, actual.z, Tolerance, $"z of {actual}");
        }
    }
}
