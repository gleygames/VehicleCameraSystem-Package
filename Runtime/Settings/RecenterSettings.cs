using System;
using UnityEngine;

namespace Gley.CameraSystem
{
    [Serializable]
    public class RecenterSettings
    {
        [SerializeField] private RecenterMode mode;
        [SerializeField] private float delay;
        [SerializeField] private float returnHalfLife = 0.25f;
        [SerializeField] private float stationaryThreshold = 0.5f;

        public RecenterMode Mode => mode;
        public float Delay => delay;
        public float ReturnHalfLife => returnHalfLife;
        public float StationaryThreshold => stationaryThreshold;

        public void Configure(RecenterMode recenterMode, float recenterDelay, float returnSmoothing, float speedThreshold)
        {
            mode = recenterMode;
            delay = recenterDelay;
            returnHalfLife = returnSmoothing;
            stationaryThreshold = speedThreshold;
        }
    }
}
