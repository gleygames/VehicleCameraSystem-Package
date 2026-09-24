using UnityEngine;

namespace Gley.CameraSystem
{
    public class StraightLineTransition
    {
        private readonly TravelProfile travelProfile = new TravelProfile();

        private Vector3 startPosition;
        private Quaternion startRotation;

        public float Progress
        {
            get
            {
                if (travelProfile.RouteLength <= 0f)
                {
                    return 1f;
                }

                return travelProfile.DistanceTravelled / travelProfile.RouteLength;
            }
        }
        public bool IsComplete => travelProfile.IsComplete;

        public void Begin(Vector3 initialPosition, Quaternion initialRotation, Vector3 destinationPosition, float speed, float easeTime)
        {
            startPosition = initialPosition;
            startRotation = initialRotation;
            travelProfile.Begin(Vector3.Distance(initialPosition, destinationPosition), speed, easeTime);
        }

        public void BeginWithDuration(Vector3 initialPosition, Quaternion initialRotation, Vector3 destinationPosition, float duration, float easeTime)
        {
            startPosition = initialPosition;
            startRotation = initialRotation;
            travelProfile.BeginWithDuration(Vector3.Distance(initialPosition, destinationPosition), duration, easeTime);
        }

        public void AdvanceTransition(float deltaTime, Vector3 destinationPosition, Quaternion destinationRotation, out Vector3 cameraPosition, out Quaternion cameraRotation)
        {
            travelProfile.AdvanceTravel(deltaTime);
            cameraPosition = Vector3.Lerp(startPosition, destinationPosition, Progress);
            cameraRotation = Quaternion.Slerp(startRotation, destinationRotation, Progress);
        }
    }
}
