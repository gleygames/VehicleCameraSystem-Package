using System;
using UnityEngine;

namespace Gley.CameraSystem.Editor
{
    [Serializable]
    public class ProfileGenerationSettings
    {
        [SerializeField] private float curveMargin = 1f;
        [SerializeField] private float maximumCornerRadius = 1.5f;
        [SerializeField] private float cornerRadiusWidthFraction = 0.25f;
        [SerializeField] private float removableSectionInset = 0.5f;
        [SerializeField] private float minimumZoomOffset = -4f;
        [SerializeField] private float maximumZoomOffset = 2f;
        [SerializeField] private float heightBelowBase = 1f;
        [SerializeField] private float heightAboveRoof = 4f;
        [SerializeField] private float drivingDefaultHeightAboveRoof = 2f;
        [SerializeField] private float rearMarkerDistanceAhead = 10f;
        [SerializeField] private float rearMarkerHeightFraction = 0.6f;
        [SerializeField] private float rearMarkerRoofClearance = 0.5f;
        [SerializeField] private float interiorEyeFromFrontFraction = 0.25f;
        [SerializeField] private float interiorEyeHeightFraction = 0.75f;
        [SerializeField] private float fixedCameraAbove = 4f;
        [SerializeField] private float fixedCameraBehind = 6f;

        public float CurveMargin => curveMargin;
        public float MaximumCornerRadius => maximumCornerRadius;
        public float CornerRadiusWidthFraction => cornerRadiusWidthFraction;
        public float RemovableSectionInset => removableSectionInset;
        public float MinimumZoomOffset => minimumZoomOffset;
        public float MaximumZoomOffset => maximumZoomOffset;
        public float HeightBelowBase => heightBelowBase;
        public float HeightAboveRoof => heightAboveRoof;
        public float DrivingDefaultHeightAboveRoof => drivingDefaultHeightAboveRoof;
        public float RearMarkerDistanceAhead => rearMarkerDistanceAhead;
        public float RearMarkerHeightFraction => rearMarkerHeightFraction;
        public float RearMarkerRoofClearance => rearMarkerRoofClearance;
        public float InteriorEyeFromFrontFraction => interiorEyeFromFrontFraction;
        public float InteriorEyeHeightFraction => interiorEyeHeightFraction;
        public float FixedCameraAbove => fixedCameraAbove;
        public float FixedCameraBehind => fixedCameraBehind;
    }
}
