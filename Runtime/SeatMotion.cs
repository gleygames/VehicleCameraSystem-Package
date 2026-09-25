using System.Collections.Generic;
using UnityEngine;

namespace Gley.CameraSystem
{
    public class SeatMotion
    {
        private readonly List<ISeatModule> modules = new List<ISeatModule>(3);
        private readonly MotionEstimator estimator = new MotionEstimator();
        private readonly AccelerationBrakingModule accelerationBraking = new AccelerationBrakingModule();
        private readonly CorneringModule cornering = new CorneringModule();

        private SeatSettings seat;

        public Vector3 UpdateSeatMotion(float scaledDeltaTime)
        {
            if (seat == null)
            {
                return Vector3.zero;
            }

            estimator.UpdateMotionEstimate(scaledDeltaTime);
            SeatMotionSignals signals = new SeatMotionSignals(estimator.LocalAcceleration, estimator.Speed, scaledDeltaTime);
            return UpdateSeatMotion(signals);
        }

        public Vector3 UpdateSeatMotion(SeatMotionSignals signals)
        {
            if (seat == null)
            {
                return Vector3.zero;
            }

            Vector3 offset = Vector3.zero;
            for (int index = 0; index < modules.Count; index++)
            {
                ISeatModule module = modules[index];
                if (module.IsEnabled)
                {
                    offset += module.UpdateSeatModule(signals, signals.DeltaTime);
                }
            }

            Vector3 envelope = seat.EnvelopeHalfExtents;
            offset.x = Mathf.Clamp(offset.x, -envelope.x, envelope.x);
            offset.y = Mathf.Clamp(offset.y, -envelope.y, envelope.y);
            offset.z = Mathf.Clamp(offset.z, -envelope.z, envelope.z);
            return offset;
        }

        public void Configure(VehicleCameraTarget target, SeatSettings seatSettings, InteriorSettings settings)
        {
            seat = seatSettings;
            modules.Clear();
            accelerationBraking.Configure(settings.AccelerationBrakingEnabled, settings.AccelerationStrength, settings.BrakingStrength, settings.AccelerationCorneringResponse);
            modules.Add(accelerationBraking);
            cornering.Configure(settings.CorneringEnabled, settings.CorneringStrength, settings.AccelerationCorneringResponse);
            modules.Add(cornering);
            estimator.Configure(target, seat.EyeLocalPosition);
        }

        public void AddModule(ISeatModule module)
        {
            if (module != null)
            {
                modules.Add(module);
            }
        }

        public void ShiftOrigin(Vector3 offset)
        {
            estimator.ShiftOrigin(offset);
        }

        public void Reset()
        {
            estimator.Reset();
            accelerationBraking.Reset();
            cornering.Reset();
        }

        public void Clear()
        {
            modules.Clear();
            seat = null;
            estimator.Configure(null, Vector3.zero);
            accelerationBraking.Reset();
            cornering.Reset();
        }
    }
}
