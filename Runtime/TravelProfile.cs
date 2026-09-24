namespace Gley.CameraSystem
{
    public class TravelProfile
    {
        private const float CompletionTimeTolerance = 0.0001f;

        private float acceleration;
        private float accelerationTime;
        private float cruiseTime;
        private float distanceTravelled;
        private float elapsedTime;
        private float peakSpeed;
        private float routeLength;
        private float totalTime;

        public float DistanceTravelled => distanceTravelled;
        public float ElapsedTime => elapsedTime;
        public float PeakSpeed => peakSpeed;
        public float RouteLength => routeLength;
        public bool IsComplete { get; private set; } = true;

        public void Begin(float travelRouteLength, float cruiseSpeed, float easeTime)
        {
            Reset(travelRouteLength);
            if (IsComplete)
            {
                return;
            }

            float safeCruiseSpeed = cruiseSpeed;
            if (safeCruiseSpeed <= 0f)
            {
                CompleteImmediately();
                return;
            }

            if (easeTime <= 0f)
            {
                peakSpeed = safeCruiseSpeed;
                cruiseTime = routeLength / peakSpeed;
                totalTime = cruiseTime;
                return;
            }

            acceleration = safeCruiseSpeed / easeTime;
            if (routeLength < safeCruiseSpeed * easeTime)
            {
                peakSpeed = UnityEngine.Mathf.Sqrt(routeLength * acceleration);
                accelerationTime = peakSpeed / acceleration;
                cruiseTime = 0f;
            }
            else
            {
                peakSpeed = safeCruiseSpeed;
                accelerationTime = easeTime;
                cruiseTime = (routeLength - peakSpeed * easeTime) / peakSpeed;
            }

            totalTime = 2f * accelerationTime + cruiseTime;
        }

        public void BeginWithDuration(float travelRouteLength, float duration, float easeTime)
        {
            Reset(travelRouteLength);
            if (IsComplete)
            {
                return;
            }

            if (duration <= 0f)
            {
                CompleteImmediately();
                return;
            }

            float safeEaseTime = UnityEngine.Mathf.Clamp(easeTime, 0f, duration * 0.5f);
            if (safeEaseTime <= 0f)
            {
                peakSpeed = routeLength / duration;
                cruiseTime = duration;
                totalTime = duration;
                return;
            }

            accelerationTime = safeEaseTime;
            totalTime = duration;
            peakSpeed = routeLength / (duration - safeEaseTime);
            acceleration = peakSpeed / safeEaseTime;
            cruiseTime = duration - 2f * safeEaseTime;
        }

        public float AdvanceTravel(float deltaTime)
        {
            if (IsComplete)
            {
                return distanceTravelled;
            }

            if (deltaTime > 0f)
            {
                elapsedTime += deltaTime;
            }

            if (totalTime - elapsedTime <= CompletionTimeTolerance)
            {
                distanceTravelled = routeLength;
                elapsedTime = totalTime;
                IsComplete = true;
                return distanceTravelled;
            }

            if (accelerationTime <= 0f)
            {
                distanceTravelled = UnityEngine.Mathf.Min(routeLength, peakSpeed * elapsedTime);
                return distanceTravelled;
            }

            if (elapsedTime < accelerationTime)
            {
                distanceTravelled = 0.5f * acceleration * elapsedTime * elapsedTime;
                return distanceTravelled;
            }

            float decelerationStartTime = accelerationTime + cruiseTime;
            if (elapsedTime < decelerationStartTime)
            {
                float accelerationDistance = 0.5f * peakSpeed * accelerationTime;
                distanceTravelled = accelerationDistance + peakSpeed * (elapsedTime - accelerationTime);
                return UnityEngine.Mathf.Min(routeLength, distanceTravelled);
            }

            float remainingTime = totalTime - elapsedTime;
            distanceTravelled = routeLength - 0.5f * acceleration * remainingTime * remainingTime;
            distanceTravelled = UnityEngine.Mathf.Clamp(distanceTravelled, 0f, routeLength);
            return distanceTravelled;
        }

        private void Reset(float travelRouteLength)
        {
            routeLength = travelRouteLength;
            if (routeLength < 0f)
            {
                routeLength = 0f;
            }

            acceleration = 0f;
            accelerationTime = 0f;
            cruiseTime = 0f;
            distanceTravelled = 0f;
            elapsedTime = 0f;
            peakSpeed = 0f;
            totalTime = 0f;
            IsComplete = routeLength <= 0f;
        }

        private void CompleteImmediately()
        {
            distanceTravelled = routeLength;
            elapsedTime = totalTime;
            IsComplete = true;
        }
    }
}
