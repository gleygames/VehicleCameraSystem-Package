using UnityEngine;

namespace Gley.CameraSystem
{
    public class OrbitMovement
    {
        private const float FullTurn = 360f;
        private const int WindingSampleCount = 64;

        private readonly HalfLifeSmoother smoother = new HalfLifeSmoother();

        private ChainOrbit orbit;
        private VehicleOrbit ranges;
        private OrbitMovementSettings settings;
        private float horizontalIntent;
        private float verticalIntent;
        private float zoomIntent;
        private float orbitDistance;
        private float playerZoom;
        private float heightOffset;
        private float travelSpeed;
        private float windingSign = 1f;
        private float limitStartDistance;
        private float limitArcLength;
        private bool hasAngleLimits;

        public LivePose Pose => new LivePose(orbitDistance, playerZoom, heightOffset);
        public float HorizontalIntent => horizontalIntent;
        public float VerticalIntent => verticalIntent;
        public float ZoomIntent => zoomIntent;
        public float OrbitDistance => orbitDistance;
        public float PlayerZoom => playerZoom;
        public float HeightOffset => heightOffset;
        public float TravelSpeed => travelSpeed;
        public bool HasAngleLimits => hasAngleLimits;

        public void Configure(ChainOrbit chainOrbit, VehicleOrbit rangeSource, OrbitMovementSettings movementSettings, LivePose pose)
        {
            orbit = null;
            hasAngleLimits = false;
            if (chainOrbit == null || !chainOrbit.IsClosed || chainOrbit.Length <= 0f || rangeSource == null || movementSettings == null)
            {
                return;
            }

            orbit = chainOrbit;
            ranges = rangeSource;
            settings = movementSettings;
            orbitDistance = pose.OrbitDistance;
            playerZoom = ClampZoom(pose.ZoomOffset);
            heightOffset = ClampHeight(pose.HeightOffset);
            windingSign = CalculateWindingSign();
            UpdateAngleLimits();
            ClampToAngleLimits();
        }

        public void UpdateOrbitMovement(float deltaTime)
        {
            if (orbit == null)
            {
                return;
            }

            UpdateTravelSpeed(deltaTime);
            if (travelSpeed != 0f && MoveAlongOrbit(travelSpeed * deltaTime))
            {
                travelSpeed = 0f;
            }

            heightOffset = ClampHeight(heightOffset + verticalIntent * settings.HeightRate * deltaTime);
            playerZoom = ClampZoom(playerZoom + zoomIntent * settings.ZoomRate * deltaTime);
        }

        public void SetHeldIntent(float horizontal, float vertical, float zoom)
        {
            horizontalIntent = Mathf.Clamp(horizontal, -1f, 1f);
            verticalIntent = Mathf.Clamp(vertical, -1f, 1f);
            zoomIntent = Mathf.Clamp(zoom, -1f, 1f);
            if (horizontalIntent == 0f)
            {
                travelSpeed = 0f;
            }
        }

        public void AddDrag(Vector2 normalizedDelta)
        {
            if (orbit == null)
            {
                return;
            }

            MoveAlongOrbit(normalizedDelta.x * settings.DragOrbit);
            heightOffset = ClampHeight(heightOffset + normalizedDelta.y * settings.DragHeight);
        }

        public void AddPinch(float normalizedSpan)
        {
            if (orbit == null)
            {
                return;
            }

            playerZoom = ClampZoom(playerZoom + normalizedSpan * settings.Pinch);
        }

        public void ClearHeldIntent()
        {
            horizontalIntent = 0f;
            verticalIntent = 0f;
            zoomIntent = 0f;
            travelSpeed = 0f;
        }

        public void Clear()
        {
            orbit = null;
            ranges = null;
            settings = null;
            orbitDistance = 0f;
            playerZoom = 0f;
            heightOffset = 0f;
            travelSpeed = 0f;
            windingSign = 1f;
            hasAngleLimits = false;
        }

        private float ClampZoom(float zoom)
        {
            return Mathf.Clamp(zoom, ranges.MinimumZoomOffset, ranges.MaximumZoomOffset);
        }

        private float ClampHeight(float height)
        {
            return Mathf.Clamp(height, ranges.MinimumHeightOffset, ranges.MaximumHeightOffset);
        }

        private float CalculateWindingSign()
        {
            float doubledArea = 0f;
            Vector3 previous = orbit.EvaluateRootLocalPosition(0f);
            for (int index = 1; index <= WindingSampleCount; index++)
            {
                Vector3 current = orbit.EvaluateRootLocalPosition(orbit.Length * index / WindingSampleCount);
                doubledArea += previous.x * current.z - current.x * previous.z;
                previous = current;
            }

            if (doubledArea < 0f)
            {
                return -1f;
            }

            return 1f;
        }

        private void UpdateAngleLimits()
        {
            hasAngleLimits = false;
            if (!settings.AngleLimitsEnabled)
            {
                return;
            }

            float span = settings.MaximumBearing - settings.MinimumBearing;
            if (span >= FullTurn)
            {
                return;
            }

            span = Mathf.Repeat(span, FullTurn);
            if (span <= 0f)
            {
                return;
            }

            float minimumDistance;
            float maximumDistance;
            if (!orbit.Bearing.TryGetDistanceAtBearing(settings.MinimumBearing, orbitDistance, out minimumDistance) || !orbit.Bearing.TryGetDistanceAtBearing(settings.MaximumBearing, orbitDistance, out maximumDistance))
            {
                return;
            }

            if (windingSign > 0f)
            {
                limitStartDistance = minimumDistance;
                limitArcLength = Mathf.Repeat(maximumDistance - minimumDistance, orbit.Length);
            }
            else
            {
                limitStartDistance = maximumDistance;
                limitArcLength = Mathf.Repeat(minimumDistance - maximumDistance, orbit.Length);
            }

            hasAngleLimits = true;
        }

        private void ClampToAngleLimits()
        {
            if (!hasAngleLimits)
            {
                return;
            }

            float relative = GetLimitRelativeDistance();
            orbitDistance += Mathf.Clamp(relative, 0f, limitArcLength) - relative;
        }

        private float GetLimitRelativeDistance()
        {
            float relative = Mathf.Repeat(orbitDistance - limitStartDistance, orbit.Length);
            if (relative > limitArcLength + (orbit.Length - limitArcLength) * 0.5f)
            {
                relative -= orbit.Length;
            }

            return relative;
        }

        private void UpdateTravelSpeed(float deltaTime)
        {
            float targetSpeed = horizontalIntent * settings.ManualTravelSpeed;
            if (travelSpeed * targetSpeed <= 0f)
            {
                travelSpeed = 0f;
            }

            if (Mathf.Abs(travelSpeed) >= Mathf.Abs(targetSpeed))
            {
                travelSpeed = targetSpeed;
                return;
            }

            travelSpeed = smoother.Smooth(travelSpeed, targetSpeed, deltaTime, settings.StartResponseHalfLife);
        }

        private bool MoveAlongOrbit(float bearingDirectionDistance)
        {
            float step = bearingDirectionDistance * windingSign;
            if (!hasAngleLimits)
            {
                orbitDistance += step;
                return false;
            }

            float relative = GetLimitRelativeDistance();
            float unclamped = relative + step;
            float clamped = Mathf.Clamp(unclamped, 0f, limitArcLength);
            orbitDistance += clamped - relative;
            return clamped != unclamped;
        }
    }
}
