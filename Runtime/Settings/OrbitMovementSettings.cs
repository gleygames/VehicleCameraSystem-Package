using System;
using UnityEngine;

namespace Gley.CameraSystem
{
    [Serializable]
    public class OrbitMovementSettings
    {
        [SerializeField] private float manualTravelSpeed = 6f;
        [SerializeField] private float startResponseHalfLife = 0.06f;
        [SerializeField] private float zoomRate = 3f;
        [SerializeField] private float heightRate = 2f;
        [SerializeField] private float dragOrbit = 20f;
        [SerializeField] private float dragHeight = 4f;
        [SerializeField] private float pinch = 5f;
        [SerializeField] private float minimumBearing = -180f;
        [SerializeField] private float maximumBearing = 180f;
        [SerializeField] private float imageRollFollow;
        [SerializeField] private bool angleLimitsEnabled;

        public float ManualTravelSpeed => manualTravelSpeed;
        public float StartResponseHalfLife => startResponseHalfLife;
        public float ZoomRate => zoomRate;
        public float HeightRate => heightRate;
        public float DragOrbit => dragOrbit;
        public float DragHeight => dragHeight;
        public float Pinch => pinch;
        public float MinimumBearing => minimumBearing;
        public float MaximumBearing => maximumBearing;
        public float ImageRollFollow => imageRollFollow;
        public bool AngleLimitsEnabled => angleLimitsEnabled;

        public void Configure(float manualSpeed, float startHalfLife, float zoomSpeed, float heightSpeed, float orbitDrag, float heightDrag, float pinchSensitivity, bool useAngleLimits, float minimumAngle, float maximumAngle, float rollFollow)
        {
            manualTravelSpeed = manualSpeed;
            startResponseHalfLife = startHalfLife;
            zoomRate = zoomSpeed;
            heightRate = heightSpeed;
            dragOrbit = orbitDrag;
            dragHeight = heightDrag;
            pinch = pinchSensitivity;
            angleLimitsEnabled = useAngleLimits;
            minimumBearing = minimumAngle;
            maximumBearing = maximumAngle;
            imageRollFollow = rollFollow;
        }
    }
}
