using System.Collections.Generic;
using UnityEngine;

namespace Gley.CameraSystem
{
    public class PointOfInterestTravel
    {
        private const float ArrivalTolerance = 0.0001f;
        private const float RouteContinuityTolerance = 0.01f;
        private const int InitialPointCapacity = 16;

        private readonly List<int> orderedPoints = new List<int>(InitialPointCapacity);
        private readonly TravelProfile travelProfile = new TravelProfile();
        private readonly OrbitRoutePlanner routePlanner = new OrbitRoutePlanner();

        private ChainOrbit orbit;
        private OrbitMovement movement;
        private Transform rootBody;
        private Transform plannedOwner;
        private Transform destinationOwner;
        private Transform reachedOwner;
        private TransitionOptions travelSpeed;
        private TravelDirection plannedDirection;
        private TravelDirection travelDirection;
        private float plannedStartDistance;
        private float plannedDestinationDistance;
        private float plannedRouteLength;
        private float plannedRouteSign;
        private float startDistance;
        private float destinationDistance;
        private float routeSign;
        private int plannedMarkerId;
        private int destinationMarkerId;
        private int reachedMarkerId;
        private bool hasReachedPoint;
        private bool isTravelling;

        public bool IsTravelling => isTravelling;

        public void Configure(ChainOrbit chainOrbit, Transform root, OrbitMovement orbitMovement)
        {
            orbit = chainOrbit;
            rootBody = root;
            movement = orbitMovement;
            plannedOwner = null;
            destinationOwner = null;
            reachedOwner = null;
            hasReachedPoint = false;
            isTravelling = false;
        }

        public bool UpdatePointOfInterestTravel(float deltaTime)
        {
            if (!isTravelling || orbit == null || movement == null)
            {
                return false;
            }

            float travelled = travelProfile.AdvanceTravel(deltaTime);
            if (!travelProfile.IsComplete)
            {
                movement.SetOrbitDistance(startDistance + routeSign * travelled);
                return false;
            }

            movement.SetOrbitDistance(ResolveDestinationDistance());
            reachedOwner = destinationOwner;
            reachedMarkerId = destinationMarkerId;
            hasReachedPoint = true;
            isTravelling = false;
            return true;
        }

        public void SetOrbit(ChainOrbit chainOrbit, Transform root)
        {
            orbit = chainOrbit;
            rootBody = root;
        }

        public CameraCommandResult PlanPoint(string pointName, TravelDirection direction)
        {
            if (!IsOrbitReady() || string.IsNullOrEmpty(pointName))
            {
                return CameraCommandResult.PointNotFound;
            }

            BuildOrderedPoints();
            int markerIndex = FindPointByName(pointName);
            if (markerIndex < 0)
            {
                return CameraCommandResult.PointNotFound;
            }

            return PlanRoute(markerIndex, direction);
        }

        public CameraCommandResult PlanNext(bool wrap, TravelDirection direction)
        {
            return PlanNeighbour(1, wrap, direction);
        }

        public CameraCommandResult PlanPrevious(bool wrap, TravelDirection direction)
        {
            return PlanNeighbour(-1, wrap, direction);
        }

        public void BeginPlannedTravel(TransitionOptions speed, TravelSettings settings)
        {
            travelSpeed = speed;
            travelDirection = plannedDirection;
            destinationOwner = plannedOwner;
            destinationMarkerId = plannedMarkerId;
            destinationDistance = plannedDestinationDistance;
            startDistance = plannedStartDistance;
            routeSign = plannedRouteSign;
            movement.SetOrbitDistance(startDistance);
            if (speed.Mode == TransitionMode.Duration)
            {
                travelProfile.BeginWithDuration(plannedRouteLength, speed.Value, settings.EaseTime);
            }
            else if (speed.Mode == TransitionMode.Snap)
            {
                travelProfile.BeginWithDuration(plannedRouteLength, 0f, settings.EaseTime);
            }
            else
            {
                float cruiseSpeed = speed.Value;
                if (speed.Mode == TransitionMode.PresetSpeed)
                {
                    cruiseSpeed = settings.AutomaticTravelSpeed;
                }

                travelProfile.Begin(plannedRouteLength, cruiseSpeed, settings.EaseTime);
            }

            isTravelling = true;
        }

        public CameraCommandResult ReplanTravel(TravelSettings settings)
        {
            if (!isTravelling)
            {
                return CameraCommandResult.Accepted;
            }

            int markerIndex = -1;
            if (IsOrbitReady())
            {
                markerIndex = FindMarker(destinationOwner, destinationMarkerId);
            }

            CameraCommandResult result = CameraCommandResult.PointNotFound;
            if (markerIndex >= 0)
            {
                result = PlanRoute(markerIndex, travelDirection);
            }

            if (result != CameraCommandResult.Accepted)
            {
                isTravelling = false;
                return result;
            }

            float travelled = travelProfile.DistanceTravelled;
            float remaining = travelProfile.RouteLength - travelled;
            if (plannedRouteSign == routeSign && Mathf.Abs(plannedRouteLength - remaining) <= RouteContinuityTolerance)
            {
                destinationDistance = plannedDestinationDistance;
                startDistance = plannedStartDistance - routeSign * travelled;
                return CameraCommandResult.Accepted;
            }

            TransitionOptions remainingSpeed = travelSpeed;
            if (travelSpeed.Mode == TransitionMode.Duration)
            {
                remainingSpeed = new TransitionOptions(TransitionMode.Duration, Mathf.Max(0f, travelSpeed.Value - travelProfile.ElapsedTime), travelSpeed.LockPlayerControl);
            }

            BeginPlannedTravel(remainingSpeed, settings);
            return CameraCommandResult.Accepted;
        }

        public void Stop()
        {
            isTravelling = false;
        }

        public void Clear()
        {
            Configure(null, null, null);
        }

        private float ResolveDestinationDistance()
        {
            int markerIndex = FindMarker(destinationOwner, destinationMarkerId);
            if (markerIndex < 0)
            {
                return destinationDistance;
            }

            return orbit.MergedMarkers[markerIndex].OrbitDistance;
        }

        private int FindMarker(Transform owner, int markerId)
        {
            IReadOnlyList<MergedMarker> markers = orbit.MergedMarkers;
            for (int index = 0; index < markers.Count; index++)
            {
                MergedMarker marker = markers[index];
                if (ReferenceEquals(marker.OwnerBody, owner) && marker.MarkerId == markerId)
                {
                    return index;
                }
            }

            return -1;
        }

        private bool IsOrbitReady()
        {
            return orbit != null && movement != null && orbit.IsClosed && orbit.Length > 0f;
        }

        private void BuildOrderedPoints()
        {
            orderedPoints.Clear();
            IReadOnlyList<MergedMarker> markers = orbit.MergedMarkers;
            for (int markerIndex = 0; markerIndex < markers.Count; markerIndex++)
            {
                MergedMarker marker = markers[markerIndex];
                if (!marker.IsPointOfInterest)
                {
                    continue;
                }

                int insertionIndex = orderedPoints.Count;
                orderedPoints.Add(markerIndex);
                while (insertionIndex > 0 && IsAuthoredBefore(marker, markers[orderedPoints[insertionIndex - 1]]))
                {
                    orderedPoints[insertionIndex] = orderedPoints[insertionIndex - 1];
                    insertionIndex--;
                }

                orderedPoints[insertionIndex] = markerIndex;
            }
        }

        private bool IsAuthoredBefore(MergedMarker first, MergedMarker second)
        {
            if (first.ChainIndex != second.ChainIndex)
            {
                return first.ChainIndex < second.ChainIndex;
            }

            return first.AuthoredIndex < second.AuthoredIndex;
        }

        private int FindPointByName(string pointName)
        {
            IReadOnlyList<MergedMarker> markers = orbit.MergedMarkers;
            int firstMatch = -1;
            for (int index = 0; index < orderedPoints.Count; index++)
            {
                MergedMarker marker = markers[orderedPoints[index]];
                if (marker.Name != pointName)
                {
                    continue;
                }

                if (ReferenceEquals(marker.OwnerBody, rootBody))
                {
                    return orderedPoints[index];
                }

                if (firstMatch < 0)
                {
                    firstMatch = orderedPoints[index];
                }
            }

            return firstMatch;
        }

        private CameraCommandResult PlanRoute(int markerIndex, TravelDirection direction)
        {
            MergedMarker marker = orbit.MergedMarkers[markerIndex];
            CameraCommandResult result = routePlanner.Plan(orbit.Length, movement, marker.OrbitDistance, direction);
            if (result != CameraCommandResult.Accepted)
            {
                return result;
            }

            plannedOwner = marker.OwnerBody;
            plannedMarkerId = marker.MarkerId;
            plannedDirection = direction;
            plannedStartDistance = routePlanner.StartDistance;
            plannedDestinationDistance = routePlanner.DestinationDistance;
            plannedRouteLength = routePlanner.RouteLength;
            plannedRouteSign = routePlanner.RouteSign;
            return CameraCommandResult.Accepted;
        }

        private CameraCommandResult PlanNeighbour(int step, bool wrap, TravelDirection direction)
        {
            if (!IsOrbitReady())
            {
                return CameraCommandResult.PointNotFound;
            }

            BuildOrderedPoints();
            int count = orderedPoints.Count;
            if (count == 0)
            {
                return CameraCommandResult.PointNotFound;
            }

            int current = FindCurrentOrderedIndex();
            if (current < 0)
            {
                return PlanRoute(orderedPoints[FindNearestIndex(step)], direction);
            }

            int neighbour = current + step;
            if (neighbour < 0 || neighbour >= count)
            {
                if (!wrap)
                {
                    return CameraCommandResult.NoNextPoint;
                }

                neighbour = (neighbour + count) % count;
            }

            return PlanRoute(orderedPoints[neighbour], direction);
        }

        private int FindCurrentOrderedIndex()
        {
            int orderedIndex = -1;
            if (isTravelling)
            {
                orderedIndex = FindOrderedIndex(destinationOwner, destinationMarkerId);
            }

            if (orderedIndex < 0 && hasReachedPoint)
            {
                orderedIndex = FindOrderedIndex(reachedOwner, reachedMarkerId);
            }

            if (orderedIndex < 0)
            {
                orderedIndex = FindPointAtCameraIndex();
            }

            return orderedIndex;
        }

        private int FindOrderedIndex(Transform owner, int markerId)
        {
            int markerIndex = FindMarker(owner, markerId);
            if (markerIndex < 0)
            {
                return -1;
            }

            for (int index = 0; index < orderedPoints.Count; index++)
            {
                if (orderedPoints[index] == markerIndex)
                {
                    return index;
                }
            }

            return -1;
        }

        private int FindPointAtCameraIndex()
        {
            IReadOnlyList<MergedMarker> markers = orbit.MergedMarkers;
            float length = orbit.Length;
            float current = Mathf.Repeat(movement.OrbitDistance, length);
            for (int index = 0; index < orderedPoints.Count; index++)
            {
                float gap = Mathf.Repeat(markers[orderedPoints[index]].OrbitDistance - current, length);
                if (gap <= ArrivalTolerance || gap >= length - ArrivalTolerance)
                {
                    return index;
                }
            }

            return -1;
        }

        private int FindNearestIndex(int step)
        {
            IReadOnlyList<MergedMarker> markers = orbit.MergedMarkers;
            float length = orbit.Length;
            float current = Mathf.Repeat(movement.OrbitDistance, length);
            float searchSign = movement.IncreasingBearingSign * step;
            float nearestGap = float.MaxValue;
            int nearestIndex = 0;
            for (int index = 0; index < orderedPoints.Count; index++)
            {
                float gap = Mathf.Repeat((markers[orderedPoints[index]].OrbitDistance - current) * searchSign, length);
                if (gap < nearestGap)
                {
                    nearestGap = gap;
                    nearestIndex = index;
                }
            }

            return nearestIndex;
        }
    }
}
