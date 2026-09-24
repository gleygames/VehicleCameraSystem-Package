using System;
using UnityEngine;

namespace Gley.CameraSystem
{
    [Serializable]
    public class TravelSettings
    {
        [SerializeField] private float automaticTravelSpeed = 10f;
        [SerializeField] private float straightLineTransitionSpeed = 10f;
        [SerializeField] private float easeTime = 0.4f;

        public float AutomaticTravelSpeed => automaticTravelSpeed;
        public float StraightLineTransitionSpeed => straightLineTransitionSpeed;
        public float EaseTime => easeTime;

        public void Configure(float automaticSpeed, float transitionSpeed, float travelEaseTime)
        {
            automaticTravelSpeed = automaticSpeed;
            straightLineTransitionSpeed = transitionSpeed;
            easeTime = travelEaseTime;
        }
    }
}
