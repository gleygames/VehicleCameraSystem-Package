using System.Collections.Generic;
using UnityEngine;

namespace Gley.CameraSystem
{
    public class ViewSwitcher
    {
        private const float FullTurn = 360f;
        private const int InitialViewCapacity = 8;

        private readonly List<ViewAdjustment> adjustments = new List<ViewAdjustment>(InitialViewCapacity);
        private readonly StraightLineTransition transition = new StraightLineTransition();
        private readonly StoredPoseResolver storedPoseResolver = new StoredPoseResolver();

        private Vector3 startLocalPosition;
        private Quaternion startLocalRotation = Quaternion.identity;
        private bool isSwitching;

        public bool IsSwitching => isSwitching;
        public bool IsComplete => transition.IsComplete;

        public void Begin(Transform root, Vector3 cameraPosition, Quaternion cameraRotation, Vector3 destinationPosition, TransitionOptions options, TravelSettings settings)
        {
            startLocalPosition = root.InverseTransformPoint(cameraPosition);
            startLocalRotation = Quaternion.Inverse(root.rotation) * cameraRotation;
            if (options.Mode == TransitionMode.Duration)
            {
                transition.BeginWithDuration(cameraPosition, cameraRotation, destinationPosition, options.Value, settings.EaseTime);
            }
            else
            {
                float speed = options.Value;
                if (options.Mode == TransitionMode.PresetSpeed)
                {
                    speed = settings.StraightLineTransitionSpeed;
                }

                transition.Begin(cameraPosition, cameraRotation, destinationPosition, speed, settings.EaseTime);
            }

            isSwitching = true;
        }

        public void UpdateViewSwitchTransition(float deltaTime, Transform root, Vector3 destinationPosition, Quaternion destinationRotation, out Vector3 cameraPosition, out Quaternion cameraRotation)
        {
            transition.RelocateStart(root.TransformPoint(startLocalPosition), root.rotation * startLocalRotation);
            transition.AdvanceTransition(deltaTime, destinationPosition, destinationRotation, out cameraPosition, out cameraRotation);
        }

        public bool TryGetOrbitBearing(ChainOrbit orbit, float distance, out float bearing)
        {
            bearing = 0f;
            if (orbit == null || !orbit.IsClosed || orbit.Length <= 0f)
            {
                return false;
            }

            bearing = orbit.Bearing.BearingOfLocalPoint(orbit.EvaluateRootLocalPosition(distance));
            return true;
        }

        public LivePose PlanDestinationPose(VehicleViewEntry view, VehicleOrbit orbit, ChainOrbit destinationOrbit, bool hasSourceBearing, float sourceBearing, bool hasCurrentDistance, float currentDistance, out float destinationBearing)
        {
            OrbitPose authoredPose = view.DefaultPose;
            destinationBearing = authoredPose.Bearing;
            if (hasSourceBearing)
            {
                destinationBearing = GetNearestAllowedBearing(sourceBearing, view.Preset.OrbitMovement);
            }

            float zoom = authoredPose.ZoomOffset;
            float height = authoredPose.HeightOffset;
            int index = FindAdjustment(view.Id);
            if (index >= 0)
            {
                zoom += adjustments[index].ZoomOffset;
                height += adjustments[index].HeightOffset;
            }

            float nearDistance = currentDistance;
            if (!hasCurrentDistance)
            {
                nearDistance = storedPoseResolver.Resolve(authoredPose, destinationOrbit, orbit, 0f).OrbitDistance;
            }

            return storedPoseResolver.Resolve(new OrbitPose(destinationBearing, zoom, height), destinationOrbit, orbit, nearDistance);
        }

        public float GetNearestAllowedBearing(float bearing, OrbitMovementSettings settings)
        {
            if (!settings.AngleLimitsEnabled)
            {
                return bearing;
            }

            float span = settings.MaximumBearing - settings.MinimumBearing;
            if (span >= FullTurn)
            {
                return bearing;
            }

            span = Mathf.Repeat(span, FullTurn);
            if (span <= 0f)
            {
                return bearing;
            }

            float relative = Mathf.Repeat(bearing - settings.MinimumBearing, FullTurn);
            if (relative <= span)
            {
                return bearing;
            }

            if (relative - span < FullTurn - relative)
            {
                return settings.MaximumBearing;
            }

            return settings.MinimumBearing;
        }

        public void StoreAdjustment(VehicleViewEntry view, LivePose pose)
        {
            OrbitPose authoredPose = view.DefaultPose;
            ViewAdjustment adjustment = new ViewAdjustment(view.Id, pose.ZoomOffset - authoredPose.ZoomOffset, pose.HeightOffset - authoredPose.HeightOffset);
            int index = FindAdjustment(view.Id);
            if (index >= 0)
            {
                adjustments[index] = adjustment;
            }
            else
            {
                adjustments.Add(adjustment);
            }
        }

        public void ClearAdjustment(int viewId)
        {
            int index = FindAdjustment(viewId);
            if (index >= 0)
            {
                adjustments.RemoveAt(index);
            }
        }

        public void Stop()
        {
            isSwitching = false;
        }

        public void ClearAdjustments()
        {
            adjustments.Clear();
        }

        public void Clear()
        {
            adjustments.Clear();
            isSwitching = false;
        }

        private int FindAdjustment(int viewId)
        {
            for (int index = 0; index < adjustments.Count; index++)
            {
                if (adjustments[index].ViewId == viewId)
                {
                    return index;
                }
            }

            return -1;
        }
    }
}
