using UnityEngine;

namespace Gley.CameraSystem
{
    public class OrbitRoutePlanner
    {
        private const float ArrivalTolerance = 0.0001f;

        public float StartDistance { get; private set; }
        public float DestinationDistance { get; private set; }
        public float RouteLength { get; private set; }
        public float RouteSign { get; private set; }

        public CameraCommandResult Plan(float orbitLength, OrbitMovement movement, float destinationDistance, TravelDirection direction)
        {
            float current = Mathf.Repeat(movement.OrbitDistance, orbitLength);
            float destination = Mathf.Repeat(destinationDistance, orbitLength);
            float routeLength;
            float sign = 1f;
            if (movement.HasAngleLimits)
            {
                float arcLength = movement.LimitArcLength;
                float destinationRelative = movement.GetLimitRelativeDistance(destination);
                if (destinationRelative < -ArrivalTolerance || destinationRelative > arcLength + ArrivalTolerance)
                {
                    return CameraCommandResult.Unreachable;
                }

                float currentRelative = Mathf.Clamp(movement.GetLimitRelativeDistance(current), 0f, arcLength);
                float delta = Mathf.Clamp(destinationRelative, 0f, arcLength) - currentRelative;
                routeLength = Mathf.Abs(delta);
                if (delta < 0f)
                {
                    sign = -1f;
                }

                if (routeLength > ArrivalTolerance && direction != TravelDirection.Shortest && GetDirectionSign(movement, direction) != sign)
                {
                    return CameraCommandResult.Unreachable;
                }
            }
            else
            {
                float forward = Mathf.Repeat(destination - current, orbitLength);
                float backward = orbitLength - forward;
                if (forward <= ArrivalTolerance || backward <= ArrivalTolerance)
                {
                    forward = 0f;
                    backward = 0f;
                }

                if (direction == TravelDirection.Shortest)
                {
                    if (backward < forward)
                    {
                        sign = -1f;
                    }
                }
                else
                {
                    sign = GetDirectionSign(movement, direction);
                }

                routeLength = forward;
                if (sign < 0f)
                {
                    routeLength = backward;
                }
            }

            StartDistance = current;
            DestinationDistance = destination;
            RouteLength = routeLength;
            RouteSign = sign;
            return CameraCommandResult.Accepted;
        }

        private float GetDirectionSign(OrbitMovement movement, TravelDirection direction)
        {
            if (direction == TravelDirection.CounterClockwise)
            {
                return movement.IncreasingBearingSign;
            }

            return -movement.IncreasingBearingSign;
        }
    }
}
