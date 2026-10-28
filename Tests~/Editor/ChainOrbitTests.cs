using NUnit.Framework;
using UnityEngine;

namespace Gley.CameraSystem.Tests.EditMode
{
    public class ChainOrbitTests
    {
        private ChainTestProfileFactory factory;

        [SetUp]
        public void SetUp()
        {
            factory = new ChainTestProfileFactory();
        }

        [Test]
        public void ThreeBodies_ClosedAndNotSelfIntersecting()
        {
            VehicleProfile front = factory.CreateProfile("Main", 20f, 10f);
            VehicleProfile root = factory.CreateProfile("Main", 20f, 10f);
            VehicleProfile rear = factory.CreateProfile("Main", 20f, 10f);
            VehicleProfile[] profiles = { front, root, rear };
            ChainLayout layout = new ChainLayout(profiles, 1, root.PrimaryOrbit);
            ChainOrbit orbit = new ChainOrbit(layout, null, 1);

            Assert.AreEqual(ChainLayoutResult.Valid, layout.LayoutResult);
            Assert.AreEqual(ChainOrbitAssemblyResult.Valid, orbit.AssemblyResult);
            Assert.AreEqual(108f, orbit.Length, 0.01f);
            Assert.AreEqual(12, orbit.Segments.Count);
            Assert.Less(Vector3.Distance(new Vector3(20f, 0f, -1f), orbit.EvaluateRootLocalPosition(41f)), 0.01f);
            Assert.Less(Vector3.Distance(new Vector3(20f, 0f, 11f), orbit.EvaluateRootLocalPosition(53f)), 0.01f);
            Assert.Less(Vector3.Distance(new Vector3(0f, 0f, 11f), orbit.EvaluateRootLocalPosition(95f)), 0.01f);
            Assert.Less(Vector3.Distance(new Vector3(0f, 0f, -1f), orbit.EvaluateRootLocalPosition(107f)), 0.01f);
            for (int index = 0; index < orbit.Segments.Count; index++)
            {
                int nextIndex = (index + 1) % orbit.Segments.Count;
                Assert.Less(Vector3.Distance(orbit.Segments[index].EndPosition, orbit.Segments[nextIndex].StartPosition), 0.0001f);
            }
        }

        [Test]
        public void FiveTimesLongerTrailer_LengthIncreasesByRetainedTrailerLength()
        {
            VehicleProfile truck = factory.CreateProfile("Main", 20f, 10f);
            VehicleProfile shortTrailer = factory.CreateProfile("Main", 20f, 10f);
            VehicleProfile longTrailer = factory.CreateProfile("Main", 20f, 50f);
            VehicleProfile[] shortProfiles = { truck, shortTrailer };
            VehicleProfile[] longProfiles = { truck, longTrailer };
            ChainOrbit shortOrbit = CreateOrbit(shortProfiles, 0, null);
            ChainOrbit longOrbit = CreateOrbit(longProfiles, 0, null);

            Assert.AreEqual(ChainOrbitAssemblyResult.Valid, shortOrbit.AssemblyResult);
            Assert.AreEqual(ChainOrbitAssemblyResult.Valid, longOrbit.AssemblyResult);
            Assert.AreEqual(80f, longOrbit.Length - shortOrbit.Length, 0.02f);
        }

        [Test]
        public void MergedMarkers_RemovedSectionMarkersDropped()
        {
            VehicleProfile truck = factory.CreateProfile("Main", 20f, 10f);
            VehicleProfile trailer = factory.CreateProfile("Main", 20f, 10f);
            OrbitWatchMarker truckRemoved = truck.AddWatchMarker(truck.PrimaryOrbit, "Truck rear removed", 0.6f, Vector3.one, true);
            OrbitWatchMarker trailerRemoved = trailer.AddWatchMarker(trailer.PrimaryOrbit, "Trailer front removed", 0.1f, Vector3.one, true);
            VehicleProfile[] profiles = { truck, trailer };
            ChainOrbit orbit = CreateOrbit(profiles, 0, null);
            bool foundTruckRemoved = false;
            bool foundTrailerRemoved = false;
            for (int index = 0; index < orbit.MergedMarkers.Count; index++)
            {
                MergedMarker marker = orbit.MergedMarkers[index];
                if (marker.ChainIndex == 0 && marker.MarkerId == truckRemoved.Id)
                {
                    foundTruckRemoved = true;
                }

                if (marker.ChainIndex == 1 && marker.MarkerId == trailerRemoved.Id)
                {
                    foundTrailerRemoved = true;
                }
            }

            Assert.AreEqual(ChainOrbitAssemblyResult.Valid, orbit.AssemblyResult);
            Assert.IsFalse(foundTruckRemoved);
            Assert.IsFalse(foundTrailerRemoved);
            Assert.AreEqual(4, orbit.MergedMarkers.Count);
        }

        [Test]
        public void MergedMarkers_OwnersPreserved_TwoTrailersSameProfile()
        {
            VehicleProfile truck = factory.CreateProfile("Main", 20f, 10f);
            VehicleProfile trailer = factory.CreateProfile("Main", 20f, 10f);
            VehicleProfile[] profiles = { truck, trailer, trailer };
            Transform[] bodies = { factory.CreateBody("Truck"), factory.CreateBody("Trailer A"), factory.CreateBody("Trailer B") };
            ChainOrbit orbit = CreateOrbit(profiles, 0, bodies);
            OrbitWatchMarker authored = trailer.PrimaryOrbit.WatchMarkers[0];
            MergedMarker first = FindMarker(orbit, 1, authored.Id);
            MergedMarker second = FindMarker(orbit, 2, authored.Id);

            Assert.AreEqual(ChainOrbitAssemblyResult.Valid, orbit.AssemblyResult);
            Assert.AreEqual(authored.Id, first.MarkerId);
            Assert.AreEqual(authored.Id, second.MarkerId);
            Assert.AreSame(bodies[1], first.OwnerBody);
            Assert.AreSame(bodies[2], second.OwnerBody);
            Assert.AreNotSame(first.OwnerBody, second.OwnerBody);
        }

        [Test]
        public void RootAimFrame_ArticulatedTrailer_WatchPointUnchanged()
        {
            VehicleProfile truck = factory.CreateProfile("Main", 20f, 10f);
            VehicleProfile trailer = factory.CreateProfile("Main", 20f, 10f);
            VehicleProfile[] profiles = { truck, trailer };
            Transform[] bodies = { factory.CreateBody("Truck"), factory.CreateBody("Trailer") };
            ChainOrbit orbit = CreateOrbit(profiles, 0, bodies);
            MergedMarker marker = FindMarker(orbit, 1, trailer.PrimaryOrbit.WatchMarkers[0].Id);
            Vector3 before = orbit.EvaluateRootLocalWatchPoint(marker.OrbitDistance);
            Vector3 orbitPositionBefore = orbit.EvaluateRootLocalPosition(marker.OrbitDistance);
            bodies[1].rotation = Quaternion.Euler(0f, 30f, 0f);
            Vector3 after = orbit.EvaluateRootLocalWatchPoint(marker.OrbitDistance);

            Assert.AreEqual(marker.RootLocalWatchPoint, before);
            Assert.AreEqual(before, after);
            Assert.AreEqual(orbitPositionBefore, orbit.EvaluateRootLocalPosition(marker.OrbitDistance));
        }

        [Test]
        public void OwnerAimFrame_ArticulatedTrailer_WatchPointFollowsTrailer()
        {
            VehicleProfile truck = factory.CreateProfile("Main", 20f, 10f);
            VehicleProfile trailer = factory.CreateProfile("Main", 20f, 10f);
            VehicleProfile[] profiles = { truck, trailer };
            Transform[] bodies = { factory.CreateBody("Truck"), factory.CreateBody("Trailer") };
            ChainLayout layout = new ChainLayout(profiles, 0, truck.PrimaryOrbit);
            bodies[1].position = layout.Offsets[1];
            ChainOrbit orbit = new ChainOrbit(layout, bodies, 0);
            MergedMarker marker = FindMarker(orbit, 1, trailer.PrimaryOrbit.WatchMarkers[0].Id);
            Vector3 before = orbit.EvaluateWorldWatchPointOwnerFrame(marker.OrbitDistance, bodies);
            bodies[1].rotation = Quaternion.Euler(0f, 30f, 0f);
            Vector3 after = orbit.EvaluateWorldWatchPointOwnerFrame(marker.OrbitDistance, bodies);

            Assert.AreEqual(marker.OwnerLocalWatchPoint + layout.Offsets[1], marker.RootLocalWatchPoint);
            Assert.AreEqual(bodies[1].TransformPoint(marker.OwnerLocalWatchPoint), after);
            Assert.Greater(Vector3.Distance(before, after), 0.1f);
        }

        [Test]
        public void WatchPointCurve_ContinuousAcrossConnector()
        {
            VehicleProfile truck = factory.CreateProfile("Main", 20f, 10f);
            VehicleProfile trailer = factory.CreateProfile("Main", 20f, 10f);
            VehicleProfile[] profiles = { truck, trailer };
            ChainOrbit orbit = CreateOrbit(profiles, 0, null);
            float connectorStart = 40f;
            Vector3 before = orbit.EvaluateRootLocalWatchPoint(connectorStart - 0.001f);
            Vector3 atBoundary = orbit.EvaluateRootLocalWatchPoint(connectorStart);
            Vector3 after = orbit.EvaluateRootLocalWatchPoint(connectorStart + 0.001f);

            Assert.AreEqual(ChainOrbitAssemblyResult.Valid, orbit.AssemblyResult);
            Assert.Less(Vector3.Distance(before, atBoundary), 0.02f);
            Assert.Less(Vector3.Distance(atBoundary, after), 0.02f);
        }

        [Test]
        public void AttachedOrbitSelection_SameNamePreferred_ElsePrimary()
        {
            VehicleProfile truck = factory.CreateProfile("Main", 20f, 10f);
            VehicleProfile matching = factory.CreateProfile("Other", 20f, 10f);
            VehicleOrbit sameName = matching.AddOrbit("Main");
            sameName.Configure(new System.Collections.Generic.List<BezierOrbitKnot>(matching.PrimaryOrbit.Knots), Quaternion.identity);
            sameName.ConfigureRemovableSections(matching.PrimaryOrbit.FrontRemovableSection, matching.PrimaryOrbit.RearRemovableSection);
            VehicleProfile fallback = factory.CreateProfile("Other", 20f, 10f);
            VehicleProfile[] matchingProfiles = { truck, matching };
            VehicleProfile[] fallbackProfiles = { truck, fallback };
            ChainLayout matchingLayout = new ChainLayout(matchingProfiles, 0, truck.PrimaryOrbit);
            ChainLayout fallbackLayout = new ChainLayout(fallbackProfiles, 0, truck.PrimaryOrbit);

            Assert.AreSame(sameName, matchingLayout.Orbits[1]);
            Assert.AreSame(fallback.PrimaryOrbit, fallbackLayout.Orbits[1]);
        }

        [Test]
        public void TenTrucksTenTrailers_EveryPairAssembles_NoCombinedCurvesAuthored()
        {
            VehicleProfile[] trucks = new VehicleProfile[10];
            VehicleProfile[] trailers = new VehicleProfile[10];
            for (int index = 0; index < 10; index++)
            {
                trucks[index] = factory.CreateProfile("Main", 20f, 10f + index);
                trailers[index] = factory.CreateProfile("Main", 20f, 10f + index);
            }

            for (int truckIndex = 0; truckIndex < trucks.Length; truckIndex++)
            {
                for (int trailerIndex = 0; trailerIndex < trailers.Length; trailerIndex++)
                {
                    VehicleProfile[] profiles = { trucks[truckIndex], trailers[trailerIndex] };
                    ChainOrbit orbit = CreateOrbit(profiles, 0, null);
                    Assert.AreEqual(ChainOrbitAssemblyResult.Valid, orbit.AssemblyResult);
                    Assert.AreEqual(1, trucks[truckIndex].Orbits.Count);
                    Assert.AreEqual(1, trailers[trailerIndex].Orbits.Count);
                }
            }
        }

        [Test]
        public void SectionEndsNearRoundedCornerKnots_ThreeBodies_AssembleWithoutSlivers()
        {
            VehicleProfile truck = factory.CreateRoundedProfile("Main", 2.25f, 4f, 4f);
            VehicleProfile trailer = factory.CreateRoundedProfile("Main", 2.25f, 7.8f, 7.8f);
            VehicleProfile[] profiles = { truck, trailer, trailer };
            ChainLayout layout = new ChainLayout(profiles, 0, truck.PrimaryOrbit);
            ChainOrbit orbit = new ChainOrbit(layout, null, 0);
            ClosedBezierOrbit trailerOrbit = new ClosedBezierOrbit(trailer.PrimaryOrbit);

            Assert.AreEqual(ChainLayoutResult.Valid, layout.LayoutResult);
            Assert.AreEqual(ChainOrbitAssemblyResult.Valid, orbit.AssemblyResult);
            Assert.Less(Vector3.Distance(new Vector3(-2.25f, 0f, -6.675f), trailerOrbit.EvaluateBodyLocalRemovableSectionEnd(OrbitAttachmentEnd.Rear)), 0.00001f);
            Assert.Less(Vector3.Distance(new Vector3(2.25f, 0f, 6.675f), trailerOrbit.EvaluateBodyLocalRemovableSectionEnd(OrbitAttachmentEnd.Front)), 0.00001f);
            for (int index = 0; index < orbit.Segments.Count; index++)
            {
                AssembledBezierOrbitSegment segment = orbit.Segments[index];
                AssembledBezierOrbitSegment next = orbit.Segments[(index + 1) % orbit.Segments.Count];
                Assert.Greater(Vector3.Distance(segment.StartPosition, segment.EndPosition), 0.001f, $"Segment {index} is a sliver.");
                Assert.Less(Vector3.Distance(segment.EndPosition, next.StartPosition), 0.0001f);
            }
        }

        private ChainOrbit CreateOrbit(VehicleProfile[] profiles, int rootIndex, Transform[] bodies)
        {
            ChainLayout layout = new ChainLayout(profiles, rootIndex, profiles[rootIndex].PrimaryOrbit);
            return new ChainOrbit(layout, bodies, rootIndex);
        }

        private MergedMarker FindMarker(ChainOrbit orbit, int ownerIndex, int markerId)
        {
            for (int index = 0; index < orbit.MergedMarkers.Count; index++)
            {
                MergedMarker marker = orbit.MergedMarkers[index];
                if (marker.ChainIndex == ownerIndex && marker.MarkerId == markerId)
                {
                    return marker;
                }
            }

            Assert.Fail("Expected retained marker was not merged.");
            return default(MergedMarker);
        }

        [TearDown]
        public void TearDown()
        {
            factory.Dispose();
        }
    }
}
