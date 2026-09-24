using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Gley.CameraSystem.Tests.EditMode
{
    public class WatchMarkerAimEditModeTests
    {
        [Test]
        public void OneWatchMarkerRemainsTheAimTargetAcrossTheWholeOrbit()
        {
            Vector3 watchPoint = new Vector3(4f, 3f, -2f);
            VehicleOrbit vehicleOrbit = CreateRectangleOrbit();
            vehicleOrbit.ConfigureWatchMarkers(CreateWatchMarkers(
                CreateWatchMarker(0.4f, watchPoint)));
            ClosedBezierOrbit orbit = new ClosedBezierOrbit(vehicleOrbit);

            Assert.AreEqual(OrbitWatchMarkerValidationResult.Valid, orbit.WatchMarkerValidationResult);
            Assert.AreEqual(watchPoint, orbit.EvaluateBodyLocalWatchPoint(0f));
            Assert.AreEqual(watchPoint, orbit.EvaluateBodyLocalWatchPoint(orbit.Length * 0.25f));
            Assert.AreEqual(watchPoint, orbit.EvaluateBodyLocalWatchPoint(orbit.Length * 0.9f));
        }

        [Test]
        public void UnsortedWatchMarkersReachExactTargetsAndInterpolateAcrossTheWrap()
        {
            Vector3 firstWatchPoint = new Vector3(1f, 2f, 3f);
            Vector3 secondWatchPoint = new Vector3(5f, 4f, 7f);
            Vector3 thirdWatchPoint = new Vector3(9f, 6f, 11f);
            VehicleOrbit vehicleOrbit = CreateRectangleOrbit();
            vehicleOrbit.ConfigureWatchMarkers(CreateWatchMarkers(
                CreateWatchMarker(0.75f, thirdWatchPoint),
                CreateWatchMarker(0f, firstWatchPoint),
                CreateWatchMarker(0.25f, secondWatchPoint)));
            ClosedBezierOrbit orbit = new ClosedBezierOrbit(vehicleOrbit);

            Assert.AreEqual(OrbitWatchMarkerValidationResult.Valid, orbit.WatchMarkerValidationResult);
            Assert.AreEqual(firstWatchPoint, orbit.EvaluateBodyLocalWatchPoint(0f));
            Assert.AreEqual(secondWatchPoint, orbit.EvaluateBodyLocalWatchPoint(orbit.Length * 0.25f));
            Assert.AreEqual(thirdWatchPoint, orbit.EvaluateBodyLocalWatchPoint(orbit.Length * 0.75f));
            Assert.AreEqual(Vector3.Lerp(firstWatchPoint, secondWatchPoint, 0.5f), orbit.EvaluateBodyLocalWatchPoint(orbit.Length * 0.125f));
            Assert.AreEqual(Vector3.Lerp(secondWatchPoint, thirdWatchPoint, 0.5f), orbit.EvaluateBodyLocalWatchPoint(orbit.Length * 0.5f));
            Assert.AreEqual(Vector3.Lerp(thirdWatchPoint, firstWatchPoint, 0.5f), orbit.EvaluateBodyLocalWatchPoint(orbit.Length * 0.875f));
        }

        [Test]
        public void MissingOutOfRangeAndWrapDuplicateWatchMarkersAreInvalid()
        {
            VehicleOrbit missingMarkersOrbit = CreateRectangleOrbit();
            VehicleOrbit outOfRangeMarkerOrbit = CreateRectangleOrbit();
            VehicleOrbit wrapDuplicateMarkerOrbit = CreateRectangleOrbit();
            outOfRangeMarkerOrbit.ConfigureWatchMarkers(CreateWatchMarkers(
                CreateWatchMarker(-0.01f, Vector3.one)));
            wrapDuplicateMarkerOrbit.ConfigureWatchMarkers(CreateWatchMarkers(
                CreateWatchMarker(0f, Vector3.one),
                CreateWatchMarker(0.99995f, Vector3.right)));

            ClosedBezierOrbit missingMarkers = new ClosedBezierOrbit(missingMarkersOrbit);
            ClosedBezierOrbit outOfRangeMarker = new ClosedBezierOrbit(outOfRangeMarkerOrbit);
            ClosedBezierOrbit wrapDuplicateMarker = new ClosedBezierOrbit(wrapDuplicateMarkerOrbit);

            Assert.AreEqual(OrbitWatchMarkerValidationResult.MissingMarkers, missingMarkers.WatchMarkerValidationResult);
            Assert.AreEqual(OrbitWatchMarkerValidationResult.InvalidMarkerPosition, outOfRangeMarker.WatchMarkerValidationResult);
            Assert.AreEqual(OrbitWatchMarkerValidationResult.DuplicateMarkerPosition, wrapDuplicateMarker.WatchMarkerValidationResult);
        }

        private VehicleOrbit CreateRectangleOrbit()
        {
            List<BezierOrbitKnot> knots = new List<BezierOrbitKnot>();
            knots.Add(CreateKnot(new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 10f), new Vector3(10f, 0f, 0f)));
            knots.Add(CreateKnot(new Vector3(10f, 0f, 0f), new Vector3(0f, 0f, 0f), new Vector3(10f, 0f, 10f)));
            knots.Add(CreateKnot(new Vector3(10f, 0f, 10f), new Vector3(10f, 0f, 0f), new Vector3(0f, 0f, 10f)));
            knots.Add(CreateKnot(new Vector3(0f, 0f, 10f), new Vector3(10f, 0f, 10f), new Vector3(0f, 0f, 0f)));
            VehicleOrbit vehicleOrbit = new VehicleOrbit();
            vehicleOrbit.Configure(knots, Quaternion.identity);
            return vehicleOrbit;
        }

        private BezierOrbitKnot CreateKnot(Vector3 anchor, Vector3 previousAnchor, Vector3 nextAnchor)
        {
            BezierOrbitKnot knot = new BezierOrbitKnot();
            knot.Configure(anchor, anchor + (previousAnchor - anchor) / 3f, anchor + (nextAnchor - anchor) / 3f);
            return knot;
        }

        private List<OrbitWatchMarker> CreateWatchMarkers(params OrbitWatchMarker[] markers)
        {
            List<OrbitWatchMarker> watchMarkers = new List<OrbitWatchMarker>();

            for (int markerIndex = 0; markerIndex < markers.Length; markerIndex++)
            {
                watchMarkers.Add(markers[markerIndex]);
            }

            return watchMarkers;
        }

        private OrbitWatchMarker CreateWatchMarker(float normalizedOrbitPosition, Vector3 watchPoint)
        {
            OrbitWatchMarker marker = new OrbitWatchMarker();
            marker.Configure(normalizedOrbitPosition, watchPoint);
            return marker;
        }
    }
}
