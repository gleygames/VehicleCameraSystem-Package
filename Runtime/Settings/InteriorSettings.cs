using System;
using UnityEngine;

namespace Gley.CameraSystem
{
    [Serializable]
    public class InteriorSettings
    {
        [SerializeField] private float headLimitLeft = 140f;
        [SerializeField] private float headLimitRight = 140f;
        [SerializeField] private float headLimitUp = 40f;
        [SerializeField] private float headLimitDown = 30f;
        [SerializeField] private float headLookRate = 120f;
        [SerializeField] private float headDrag = 180f;
        [SerializeField] private float turnLookMaximumOffset = 20f;
        [SerializeField] private float turnLookHalfLife = 0.4f;
        [SerializeField] private float turnLookFullTurnRate = 30f;
        [SerializeField] private float turnLookHintWeight = 0.5f;
        [SerializeField] private float accelerationStrength = 0.02f;
        [SerializeField] private float brakingStrength = 0.03f;
        [SerializeField] private float corneringStrength = 0.015f;
        [SerializeField] private float accelerationCorneringResponse = 0.1f;
        [SerializeField] private float cushionCutoff = 1.5f;
        [SerializeField] private float cushionGain = 0.5f;
        [SerializeField] private float cushionResponse = 0.05f;
        [SerializeField] private bool turnLookEnabled = true;
        [SerializeField] private bool accelerationBrakingEnabled = true;
        [SerializeField] private bool corneringEnabled = true;
        [SerializeField] private bool cushionEnabled = true;

        public float HeadLimitLeft => headLimitLeft;
        public float HeadLimitRight => headLimitRight;
        public float HeadLimitUp => headLimitUp;
        public float HeadLimitDown => headLimitDown;
        public float HeadLookRate => headLookRate;
        public float HeadDrag => headDrag;
        public float TurnLookMaximumOffset => turnLookMaximumOffset;
        public float TurnLookHalfLife => turnLookHalfLife;
        public float TurnLookFullTurnRate => turnLookFullTurnRate;
        public float TurnLookHintWeight => turnLookHintWeight;
        public float AccelerationStrength => accelerationStrength;
        public float BrakingStrength => brakingStrength;
        public float CorneringStrength => corneringStrength;
        public float AccelerationCorneringResponse => accelerationCorneringResponse;
        public float CushionCutoff => cushionCutoff;
        public float CushionGain => cushionGain;
        public float CushionResponse => cushionResponse;
        public bool TurnLookEnabled => turnLookEnabled;
        public bool AccelerationBrakingEnabled => accelerationBrakingEnabled;
        public bool CorneringEnabled => corneringEnabled;
        public bool CushionEnabled => cushionEnabled;

        public void Configure(float leftLimit, float rightLimit, float upLimit, float downLimit, float lookRate, float dragSensitivity, bool turnLook, float maxTurnOffset, float turnHalfLife, float fullTurnRate, float hintWeight, bool accelerationBraking, bool cornering, bool cushion, float acceleration, float braking, float cornerStrength, float accelerationCorneringHalfLife, float cushionHighPassCutoff, float cushionMotionGain, float cushionResponseHalfLife)
        {
            headLimitLeft = leftLimit;
            headLimitRight = rightLimit;
            headLimitUp = upLimit;
            headLimitDown = downLimit;
            headLookRate = lookRate;
            headDrag = dragSensitivity;
            turnLookEnabled = turnLook;
            turnLookMaximumOffset = maxTurnOffset;
            turnLookHalfLife = turnHalfLife;
            turnLookFullTurnRate = fullTurnRate;
            turnLookHintWeight = hintWeight;
            accelerationBrakingEnabled = accelerationBraking;
            accelerationStrength = acceleration;
            brakingStrength = braking;
            corneringEnabled = cornering;
            corneringStrength = cornerStrength;
            accelerationCorneringResponse = accelerationCorneringHalfLife;
            cushionEnabled = cushion;
            cushionCutoff = cushionHighPassCutoff;
            cushionGain = cushionMotionGain;
            cushionResponse = cushionResponseHalfLife;
        }
    }
}
