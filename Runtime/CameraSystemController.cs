using UnityEngine;

namespace Gley.CameraSystem
{
    public class CameraSystemController : MonoBehaviour
    {
        private ClosedBezierOrbit closedBezierOrbit;
        private OrbitFrame orbitFrame;
        [SerializeField] private Camera assignedCamera;
        [SerializeField] private CameraViewPreset selectedViewPreset;
        [SerializeField] private Transform vehicleBody;
        [SerializeField] private VehicleProfile vehicleProfile;
        private float currentOrbitTravelSpeed;
        private float heightIntent;
        private float heightOffset;
        private float horizontalOrbitIntent;
        private float orbitDistance;
        private float zoomIntent;
        private float zoomOffset;

        public Camera AssignedCamera => assignedCamera;
        public CameraViewPreset ActiveViewPreset { get; private set; }
        public CameraViewPreset SelectedViewPreset => selectedViewPreset;
        public Transform VehicleBody => vehicleBody;
        public VehicleProfile VehicleProfile => vehicleProfile;
        public CameraSystemActivationResult LastActivationResult { get; private set; }
        public float CurrentOrbitTravelSpeed => currentOrbitTravelSpeed;
        public float HeightOffset => heightOffset;
        public float OrbitDistance => orbitDistance;
        public float ZoomOffset => zoomOffset;
        public bool IsActive { get; private set; }

        private void LateUpdate()
        {
            UpdateCameraVisuals(Time.unscaledDeltaTime);
        }

        public void UpdateCameraVisuals(float deltaTime)
        {
            if (!IsActive)
            {
                return;
            }

            if (ActiveViewPreset.ViewType == CameraViewType.Fixed)
            {
                ApplyFixedPose();
            }

            if (ActiveViewPreset.ViewType == CameraViewType.Presentation)
            {
                UpdateOrbitVisuals(deltaTime);
                UpdateOffsetVisuals(deltaTime);
                ApplyOrbitPose();
            }
        }

        public void AssignCamera(Camera camera)
        {
            Release();
            assignedCamera = camera;
        }

        public void AssignVehicle(Transform body, VehicleProfile profile)
        {
            Release();
            selectedViewPreset = null;
            vehicleBody = body;
            vehicleProfile = profile;
        }

        public void SelectViewPreset(CameraViewPreset viewPreset)
        {
            Release();
            selectedViewPreset = viewPreset;
        }

        public void SetHorizontalOrbitIntent(float intent)
        {
            horizontalOrbitIntent = Mathf.Clamp(intent, -1f, 1f);

            if (horizontalOrbitIntent == 0f)
            {
                currentOrbitTravelSpeed = 0f;
            }
        }

        public void SetHeightIntent(float intent)
        {
            heightIntent = Mathf.Clamp(intent, -1f, 1f);
        }

        public void SetZoomIntent(float intent)
        {
            zoomIntent = Mathf.Clamp(intent, -1f, 1f);
        }

        public CameraSystemActivationResult Activate()
        {
            CameraViewPreset viewPreset = ResolveActivationViewPreset();
            CameraSystemActivationResult activationResult = ValidateActivation(viewPreset);
            LastActivationResult = activationResult;

            if (activationResult != CameraSystemActivationResult.Succeeded)
            {
                Release();
                LastActivationResult = activationResult;
                return activationResult;
            }

            ActiveViewPreset = viewPreset;
            IsActive = true;
            if (ActiveViewPreset.ViewType == CameraViewType.Presentation)
            {
                orbitFrame = new OrbitFrame(vehicleBody, vehicleProfile.PrimaryOrbit.OrientationAdjustment);
            }

            if (ActiveViewPreset.ViewType == CameraViewType.Presentation)
            {
                heightOffset = Mathf.Clamp(heightOffset, vehicleProfile.PrimaryOrbit.MinimumHeightOffset, vehicleProfile.PrimaryOrbit.MaximumHeightOffset);
                zoomOffset = Mathf.Clamp(zoomOffset, vehicleProfile.PrimaryOrbit.MinimumZoomOffset, vehicleProfile.PrimaryOrbit.MaximumZoomOffset);
            }

            if (ActiveViewPreset.ViewType == CameraViewType.Fixed)
            {
                ApplyFixedPose();
            }

            if (ActiveViewPreset.ViewType == CameraViewType.Presentation)
            {
                closedBezierOrbit = new ClosedBezierOrbit(vehicleProfile.PrimaryOrbit);

                ApplyOrbitPose();
            }

            return activationResult;
        }

        public void Release()
        {
            IsActive = false;
            ActiveViewPreset = null;
            closedBezierOrbit = null;
            orbitFrame = null;
            currentOrbitTravelSpeed = 0f;
            heightIntent = 0f;
            horizontalOrbitIntent = 0f;
            zoomIntent = 0f;
        }

        private CameraViewPreset ResolveActivationViewPreset()
        {
            if (selectedViewPreset != null)
            {
                return selectedViewPreset;
            }

            if (vehicleProfile == null)
            {
                return null;
            }

            for (int viewIndex = 0; viewIndex < vehicleProfile.Views.Count; viewIndex++)
            {
                CameraViewPreset viewPreset = vehicleProfile.Views[viewIndex].Preset;

                if (viewPreset != null && viewPreset.ViewType == CameraViewType.Fixed)
                {
                    return viewPreset;
                }
            }

            return null;
        }

        private CameraSystemActivationResult ValidateActivation(CameraViewPreset viewPreset)
        {
            if (assignedCamera == null)
            {
                return CameraSystemActivationResult.MissingCamera;
            }

            if (vehicleBody == null)
            {
                return CameraSystemActivationResult.MissingVehicleBody;
            }

            if (vehicleProfile == null)
            {
                return CameraSystemActivationResult.MissingVehicleProfile;
            }

            if (viewPreset == null)
            {
                return CameraSystemActivationResult.MissingViewPreset;
            }

            if (viewPreset.ViewType == CameraViewType.Fixed)
            {
                if (vehicleProfile.FixedViewPose.CameraLocalPosition == vehicleProfile.FixedViewPose.WatchPointLocalPosition)
                {
                    return CameraSystemActivationResult.InvalidFixedView;
                }

                return CameraSystemActivationResult.Succeeded;
            }

            if (viewPreset.ViewType == CameraViewType.Presentation)
            {
                if (vehicleProfile.PrimaryOrbit == null)
                {
                    return CameraSystemActivationResult.MissingOrbit;
                }

                ClosedBezierOrbit orbit = new ClosedBezierOrbit(vehicleProfile.PrimaryOrbit);

                if (orbit.ValidationResult != OrbitValidationResult.Valid)
                {
                    return CameraSystemActivationResult.InvalidOrbit;
                }

                if (orbit.WatchMarkerValidationResult == OrbitWatchMarkerValidationResult.MissingMarkers)
                {
                    return CameraSystemActivationResult.MissingWatchMarkers;
                }

                if (orbit.WatchMarkerValidationResult != OrbitWatchMarkerValidationResult.Valid)
                {
                    return CameraSystemActivationResult.InvalidWatchMarkers;
                }

                if (orbit.OffsetRangeValidationResult != OrbitOffsetRangeValidationResult.Valid && orbit.OffsetRangeValidationResult != OrbitOffsetRangeValidationResult.InwardZoomExceedsCurvature)
                {
                    return CameraSystemActivationResult.InvalidOrbitOffsetRange;
                }

                return CameraSystemActivationResult.Succeeded;
            }

            return CameraSystemActivationResult.UnsupportedViewPreset;
        }

        private void UpdateOrbitVisuals(float deltaTime)
        {
            if (horizontalOrbitIntent == 0f)
            {
                currentOrbitTravelSpeed = 0f;
                return;
            }

            float targetOrbitTravelSpeed = ActiveViewPreset.OrbitMovement.ManualTravelSpeed * horizontalOrbitIntent;

            if (ActiveViewPreset.OrbitMovement.StartResponseHalfLife <= 0f)
            {
                currentOrbitTravelSpeed = targetOrbitTravelSpeed;
            }
            else
            {
                float acceleration = ActiveViewPreset.OrbitMovement.ManualTravelSpeed / ActiveViewPreset.OrbitMovement.StartResponseHalfLife;
                currentOrbitTravelSpeed = Mathf.MoveTowards(currentOrbitTravelSpeed, targetOrbitTravelSpeed, acceleration * deltaTime);
            }

            orbitDistance += currentOrbitTravelSpeed * deltaTime;
        }

        private void UpdateOffsetVisuals(float deltaTime)
        {
            heightOffset += ActiveViewPreset.OrbitMovement.HeightRate * heightIntent * deltaTime;
            heightOffset = Mathf.Clamp(heightOffset, vehicleProfile.PrimaryOrbit.MinimumHeightOffset, vehicleProfile.PrimaryOrbit.MaximumHeightOffset);
            zoomOffset += ActiveViewPreset.OrbitMovement.ZoomRate * zoomIntent * deltaTime;
            zoomOffset = Mathf.Clamp(zoomOffset, vehicleProfile.PrimaryOrbit.MinimumZoomOffset, vehicleProfile.PrimaryOrbit.MaximumZoomOffset);
        }

        private void ApplyOrbitPose()
        {
            Vector3 cameraPosition;
            Vector3 inwardNormal;
            Vector3 watchPointLocalPosition;

            cameraPosition = orbitFrame.ToWorldPosition(closedBezierOrbit.EvaluateBodyLocalPosition(orbitDistance));
            inwardNormal = closedBezierOrbit.EvaluateBodyLocalInwardNormal(orbitDistance);
            watchPointLocalPosition = closedBezierOrbit.EvaluateBodyLocalWatchPoint(orbitDistance);

            Vector3 watchPointPosition = vehicleBody.TransformPoint(watchPointLocalPosition);

            cameraPosition += orbitFrame.Up * heightOffset;
            cameraPosition += orbitFrame.ToWorldInwardNormal(inwardNormal) * zoomOffset;
            Vector3 watchDirection = watchPointPosition - cameraPosition;

            assignedCamera.transform.SetPositionAndRotation(cameraPosition, Quaternion.LookRotation(watchDirection, vehicleBody.up));
        }

        private void ApplyFixedPose()
        {
            Vector3 cameraPosition = vehicleBody.TransformPoint(vehicleProfile.FixedViewPose.CameraLocalPosition);
            Vector3 watchPointPosition = vehicleBody.TransformPoint(vehicleProfile.FixedViewPose.WatchPointLocalPosition);
            Vector3 watchDirection = watchPointPosition - cameraPosition;

            assignedCamera.transform.SetPositionAndRotation(cameraPosition, Quaternion.LookRotation(watchDirection, vehicleBody.up));
        }
    }
}
