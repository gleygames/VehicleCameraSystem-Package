using UnityEngine;

namespace Gley.CameraSystem
{
    public class OrbitPoseTravel
    {
        private const float RouteContinuityTolerance = 0.01f;

        private readonly TravelProfile travelProfile = new TravelProfile();
        private readonly OrbitRoutePlanner routePlanner = new OrbitRoutePlanner();

        private OrbitMovement movement;
        private TransitionOptions travelSpeed;
        private TravelDirection plannedDirection;
        private TravelDirection travelDirection;
        private float plannedZoom;
        private float plannedHeight;
        private float startDistance;
        private float destinationDistance;
        private float routeSign;
        private float arcLength;
        private float startZoom;
        private float startHeight;
        private float destinationZoom;
        private float destinationHeight;
        private float plannedRouteLength;
        private bool isTravelling;

        public float PlannedRouteLength => plannedRouteLength;
        public bool IsTravelling => isTravelling;

        public bool UpdateOrbitPoseTravel(float deltaTime)
        {
            if (!isTravelling || movement == null)
            {
                return false;
            }

            travelProfile.AdvanceTravel(deltaTime);
            if (!travelProfile.IsComplete)
            {
                ApplyProgress(GetProgress());
                return false;
            }

            movement.SetOrbitDistance(destinationDistance);
            movement.SetZoomAndHeight(destinationZoom, destinationHeight);
            isTravelling = false;
            return true;
        }

        public CameraCommandResult Plan(ChainOrbit orbit, OrbitMovement orbitMovement, LivePose destination, TravelDirection direction)
        {
            if (orbit == null || orbitMovement == null || !orbit.IsClosed || orbit.Length <= 0f)
            {
                return CameraCommandResult.Unreachable;
            }

            movement = orbitMovement;
            plannedDirection = direction;
            plannedZoom = destination.ZoomOffset;
            plannedHeight = destination.HeightOffset;
            CameraCommandResult result = routePlanner.Plan(orbit.Length, movement, destination.OrbitDistance, direction);
            if (result == CameraCommandResult.Accepted)
            {
                float zoomChange = plannedZoom - movement.PlayerZoom;
                float heightChange = plannedHeight - movement.HeightOffset;
                float arc = routePlanner.RouteLength;
                plannedRouteLength = Mathf.Sqrt(arc * arc + zoomChange * zoomChange + heightChange * heightChange);
            }

            return result;
        }

        public void BeginPlannedTravel(TransitionOptions speed, TravelSettings settings)
        {
            travelSpeed = speed;
            travelDirection = plannedDirection;
            startDistance = routePlanner.StartDistance;
            destinationDistance = routePlanner.DestinationDistance;
            routeSign = routePlanner.RouteSign;
            arcLength = routePlanner.RouteLength;
            startZoom = movement.PlayerZoom;
            startHeight = movement.HeightOffset;
            destinationZoom = plannedZoom;
            destinationHeight = plannedHeight;
            movement.SetOrbitDistance(startDistance);
            float zoomChange = destinationZoom - startZoom;
            float heightChange = destinationHeight - startHeight;
            float routeLength = Mathf.Sqrt(arcLength * arcLength + zoomChange * zoomChange + heightChange * heightChange);
            if (speed.Mode == TransitionMode.Duration)
            {
                travelProfile.BeginWithDuration(routeLength, speed.Value, settings.EaseTime);
            }
            else if (speed.Mode == TransitionMode.Snap)
            {
                travelProfile.BeginWithDuration(routeLength, 0f, settings.EaseTime);
            }
            else
            {
                float cruiseSpeed = speed.Value;
                if (speed.Mode == TransitionMode.PresetSpeed)
                {
                    cruiseSpeed = settings.AutomaticTravelSpeed;
                }

                travelProfile.Begin(routeLength, cruiseSpeed, settings.EaseTime);
            }

            isTravelling = true;
        }

        public CameraCommandResult ReplanTravel(ChainOrbit orbit, LivePose destination, TravelSettings settings)
        {
            if (!isTravelling)
            {
                return CameraCommandResult.Accepted;
            }

            float progress = GetProgress();
            CameraCommandResult result = Plan(orbit, movement, destination, travelDirection);
            if (result != CameraCommandResult.Accepted)
            {
                isTravelling = false;
                return result;
            }

            float remainingArc = arcLength * (1f - progress);
            bool isSameRoute = routePlanner.RouteSign == routeSign && Mathf.Abs(routePlanner.RouteLength - remainingArc) <= RouteContinuityTolerance;
            bool isSameOffsets = Mathf.Abs(plannedZoom - destinationZoom) <= RouteContinuityTolerance && Mathf.Abs(plannedHeight - destinationHeight) <= RouteContinuityTolerance;
            if (isSameRoute && isSameOffsets)
            {
                destinationDistance = routePlanner.DestinationDistance;
                startDistance = routePlanner.StartDistance - routeSign * arcLength * progress;
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
            movement = null;
            isTravelling = false;
        }

        private void ApplyProgress(float progress)
        {
            movement.SetOrbitDistance(startDistance + routeSign * arcLength * progress);
            movement.SetZoomAndHeight(Mathf.Lerp(startZoom, destinationZoom, progress), Mathf.Lerp(startHeight, destinationHeight, progress));
        }

        private float GetProgress()
        {
            if (travelProfile.RouteLength <= 0f)
            {
                return 1f;
            }

            return Mathf.Clamp01(travelProfile.DistanceTravelled / travelProfile.RouteLength);
        }
    }
}
