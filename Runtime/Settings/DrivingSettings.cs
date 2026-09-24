using System;
using UnityEngine;

namespace Gley.CameraSystem
{
    [Serializable]
    public class DrivingSettings
    {
        [SerializeField] private float followHalfLife = 0.12f;
        [SerializeField] private float aimHalfLife = 0.08f;
        [SerializeField] private float maximumLag = 3f;
        [SerializeField] private float lowSpeed = 5f;
        [SerializeField] private float highSpeed = 25f;
        [SerializeField] private float maximumSpeedDistanceOffset = 3f;
        [SerializeField] private float speedDistanceSideMinimum = 0.25f;
        [SerializeField] private float turnLookMaximumOffset = 12f;
        [SerializeField] private float turnLookFullTurnRate = 30f;
        [SerializeField] private float turnLookHalfLife = 0.4f;
        [SerializeField] private float turnLookHintWeight = 0.5f;
        [SerializeField] private bool speedDistanceEnabled = true;
        [SerializeField] private bool speedDistanceInReverse;
        [SerializeField] private bool turnLookEnabled = true;

        public float FollowHalfLife => followHalfLife;
        public float AimHalfLife => aimHalfLife;
        public float MaximumLag => maximumLag;
        public float LowSpeed => lowSpeed;
        public float HighSpeed => highSpeed;
        public float MaximumSpeedDistanceOffset => maximumSpeedDistanceOffset;
        public float SpeedDistanceSideMinimum => speedDistanceSideMinimum;
        public float TurnLookMaximumOffset => turnLookMaximumOffset;
        public float TurnLookFullTurnRate => turnLookFullTurnRate;
        public float TurnLookHalfLife => turnLookHalfLife;
        public float TurnLookHintWeight => turnLookHintWeight;
        public bool SpeedDistanceEnabled => speedDistanceEnabled;
        public bool SpeedDistanceInReverse => speedDistanceInReverse;
        public bool TurnLookEnabled => turnLookEnabled;

        public void Configure(float followSmoothing, float aimSmoothing, float maxLag, bool speedDistance, float speedLow, float speedHigh, float maxDistanceOffset, float sideMinimum, bool reverseSpeedDistance, bool turnLook, float maxTurnOffset, float fullTurnRate, float turnHalfLife, float hintWeight)
        {
            followHalfLife = followSmoothing;
            aimHalfLife = aimSmoothing;
            maximumLag = maxLag;
            speedDistanceEnabled = speedDistance;
            lowSpeed = speedLow;
            highSpeed = speedHigh;
            maximumSpeedDistanceOffset = maxDistanceOffset;
            speedDistanceSideMinimum = sideMinimum;
            speedDistanceInReverse = reverseSpeedDistance;
            turnLookEnabled = turnLook;
            turnLookMaximumOffset = maxTurnOffset;
            turnLookFullTurnRate = fullTurnRate;
            turnLookHalfLife = turnHalfLife;
            turnLookHintWeight = hintWeight;
        }
    }
}
