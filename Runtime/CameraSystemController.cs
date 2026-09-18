using UnityEngine;

namespace Gley.CameraSystem
{
    public class CameraSystemController : MonoBehaviour
    {
        private ClosedBezierOrbit closedBezierOrbit;
        private TwoBodyClosedBezierOrbit twoBodyClosedBezierOrbit;
        [SerializeField] private Camera assignedCamera;
        [SerializeField] private CameraViewPreset selectedViewPreset;
        [SerializeField] private Transform vehicleBody;
        [SerializeField] private VehicleProfile vehicleProfile;
        private Transform rearVehicleBody;
        private VehicleProfile rearVehicleProfile;
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
        public Transform RearVehicleBody => rearVehicleBody;
        public VehicleProfile RearVehicleProfile => rearVehicleProfile;
        public CameraSystemActivationResult LastActivationResult { get; private set; }
        public CameraSystemAttachmentResult LastAttachmentResult { get; private set; }
        public float CurrentOrbitTravelSpeed => currentOrbitTravelSpeed;
        public float HeightOffset => heightOffset;
        public float OrbitDistance => orbitDistance;
        public float ZoomOffset => zoomOffset;
        public bool IsActive { get; private set; }
        public bool HasRearVehicleAttachment => rearVehicleBody != null;
        public bool UsesAttachedOrbit => twoBodyClosedBezierOrbit != null;

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

            if (ActiveViewPreset.ViewType == CameraViewType.ExteriorPresentation)
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
            rearVehicleBody = null;
            rearVehicleProfile = null;
        }

        public void SelectViewPreset(CameraViewPreset viewPreset)
        {
            Release();
            selectedViewPreset = viewPreset;
        }

        public CameraSystemAttachmentResult AttachRearBody(Transform body, VehicleProfile profile)
        {
            if (vehicleBody == null)
            {
                LastAttachmentResult = CameraSystemAttachmentResult.MissingRootVehicleBody;
                return LastAttachmentResult;
            }

            if (vehicleProfile == null)
            {
                LastAttachmentResult = CameraSystemAttachmentResult.MissingRootVehicleProfile;
                return LastAttachmentResult;
            }

            if (body == null)
            {
                LastAttachmentResult = CameraSystemAttachmentResult.MissingRearVehicleBody;
                return LastAttachmentResult;
            }

            if (profile == null)
            {
                LastAttachmentResult = CameraSystemAttachmentResult.MissingRearVehicleProfile;
                return LastAttachmentResult;
            }

            if (body == vehicleBody)
            {
                LastAttachmentResult = CameraSystemAttachmentResult.RearVehicleMatchesRoot;
                return LastAttachmentResult;
            }

            if (rearVehicleBody != null)
            {
                LastAttachmentResult = CameraSystemAttachmentResult.RearVehicleAlreadyAttached;
                return LastAttachmentResult;
            }

            if (IsActive && ActiveViewPreset.ViewType == CameraViewType.ExteriorPresentation && vehicleProfile.VehicleOrbit.MergeWhenAttached)
            {
                TwoBodyOrbitAttachmentRemapResolver remapResolver = new TwoBodyOrbitAttachmentRemapResolver(vehicleProfile, profile);

                if (remapResolver.CombinedOrbit.AssemblyResult != TwoBodyClosedBezierOrbitAssemblyResult.Valid)
                {
                    LastAttachmentResult = CameraSystemAttachmentResult.InvalidActiveExteriorOrbit;
                    return LastAttachmentResult;
                }

                if (!remapResolver.CombinedOrbit.HasValidWatchMarkers)
                {
                    LastAttachmentResult = CameraSystemAttachmentResult.InvalidActiveExteriorOrbit;
                    return LastAttachmentResult;
                }

                OrbitAttachmentRemapResult remapResult = remapResolver.ResolveFrontOrbitDistance(orbitDistance);

                if (remapResult == OrbitAttachmentRemapResult.InvalidCombinedOrbit)
                {
                    LastAttachmentResult = CameraSystemAttachmentResult.InvalidActiveExteriorOrbit;
                    return LastAttachmentResult;
                }

                rearVehicleBody = body;
                rearVehicleProfile = profile;
                closedBezierOrbit = null;
                twoBodyClosedBezierOrbit = remapResolver.CombinedOrbit;
                orbitDistance = remapResolver.RemappedOrbitDistance;
                ApplyOrbitPose();
                LastAttachmentResult = CameraSystemAttachmentResult.Attached;
                return LastAttachmentResult;
            }

            rearVehicleBody = body;
            rearVehicleProfile = profile;
            LastAttachmentResult = CameraSystemAttachmentResult.Attached;
            return LastAttachmentResult;
        }

        public CameraSystemAttachmentResult DetachRearBody()
        {
            if (rearVehicleBody == null)
            {
                LastAttachmentResult = CameraSystemAttachmentResult.NoRearVehicleAttached;
                return LastAttachmentResult;
            }

            if (IsActive && ActiveViewPreset.ViewType == CameraViewType.ExteriorPresentation && twoBodyClosedBezierOrbit != null)
            {
                TwoBodyOrbitDetachmentRemapResolver remapResolver = new TwoBodyOrbitDetachmentRemapResolver(vehicleProfile, twoBodyClosedBezierOrbit);
                OrbitDetachmentRemapResult remapResult = remapResolver.ResolveCombinedOrbitDistance(orbitDistance);

                if (remapResult == OrbitDetachmentRemapResult.InvalidRootOrbit)
                {
                    LastAttachmentResult = CameraSystemAttachmentResult.InvalidActiveExteriorDetachment;
                    return LastAttachmentResult;
                }

                closedBezierOrbit = remapResolver.RootOrbit;
                twoBodyClosedBezierOrbit = null;
                orbitDistance = remapResolver.RemappedOrbitDistance;
                rearVehicleBody = null;
                rearVehicleProfile = null;
                ApplyOrbitPose();
                LastAttachmentResult = CameraSystemAttachmentResult.Detached;
                return LastAttachmentResult;
            }

            rearVehicleBody = null;
            rearVehicleProfile = null;
            LastAttachmentResult = CameraSystemAttachmentResult.Detached;
            return LastAttachmentResult;
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

            if (ActiveViewPreset.ViewType == CameraViewType.ExteriorPresentation)
            {
                heightOffset = Mathf.Clamp(heightOffset, vehicleProfile.VehicleOrbit.MinimumHeightOffset, vehicleProfile.VehicleOrbit.MaximumHeightOffset);
                zoomOffset = Mathf.Clamp(zoomOffset, vehicleProfile.VehicleOrbit.MinimumZoomOffset, vehicleProfile.VehicleOrbit.MaximumZoomOffset);
            }

            if (ActiveViewPreset.ViewType == CameraViewType.Fixed)
            {
                ApplyFixedPose();
            }

            if (ActiveViewPreset.ViewType == CameraViewType.ExteriorPresentation)
            {
                if (ShouldUseAttachedOrbit())
                {
                    twoBodyClosedBezierOrbit = new TwoBodyClosedBezierOrbit(vehicleProfile, rearVehicleProfile);
                }
                else
                {
                    closedBezierOrbit = new ClosedBezierOrbit(vehicleProfile.VehicleOrbit);
                }

                ApplyOrbitPose();
            }

            return activationResult;
        }

        public void Release()
        {
            IsActive = false;
            ActiveViewPreset = null;
            closedBezierOrbit = null;
            twoBodyClosedBezierOrbit = null;
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

            return vehicleProfile.FixedViewPreset;
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
                if (vehicleProfile.FixedCameraLocalPosition == vehicleProfile.FixedWatchPointLocalPosition)
                {
                    return CameraSystemActivationResult.InvalidFixedView;
                }

                return CameraSystemActivationResult.Succeeded;
            }

            if (viewPreset.ViewType == CameraViewType.ExteriorPresentation)
            {
                if (vehicleProfile.VehicleOrbit == null)
                {
                    return CameraSystemActivationResult.MissingOrbit;
                }

                ClosedBezierOrbit orbit = new ClosedBezierOrbit(vehicleProfile.VehicleOrbit);

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

                if (orbit.OffsetRangeValidationResult != OrbitOffsetRangeValidationResult.Valid)
                {
                    return CameraSystemActivationResult.InvalidOrbitOffsetRange;
                }

                if (ShouldUseAttachedOrbit())
                {
                    TwoBodyClosedBezierOrbit attachedOrbit = new TwoBodyClosedBezierOrbit(vehicleProfile, rearVehicleProfile);

                    if (attachedOrbit.AssemblyResult != TwoBodyClosedBezierOrbitAssemblyResult.Valid)
                    {
                        return CameraSystemActivationResult.InvalidAttachedOrbit;
                    }

                    if (attachedOrbit.FrontWatchMarkerValidationResult == OrbitWatchMarkerValidationResult.MissingMarkers || attachedOrbit.RearWatchMarkerValidationResult == OrbitWatchMarkerValidationResult.MissingMarkers)
                    {
                        return CameraSystemActivationResult.MissingAttachedWatchMarkers;
                    }

                    if (!attachedOrbit.HasValidWatchMarkers)
                    {
                        return CameraSystemActivationResult.InvalidAttachedWatchMarkers;
                    }
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

            float targetOrbitTravelSpeed = ActiveViewPreset.OrbitTravelSpeed * horizontalOrbitIntent;

            if (ActiveViewPreset.OrbitStartResponseSeconds <= 0f)
            {
                currentOrbitTravelSpeed = targetOrbitTravelSpeed;
            }
            else
            {
                float acceleration = ActiveViewPreset.OrbitTravelSpeed / ActiveViewPreset.OrbitStartResponseSeconds;
                currentOrbitTravelSpeed = Mathf.MoveTowards(currentOrbitTravelSpeed, targetOrbitTravelSpeed, acceleration * deltaTime);
            }

            orbitDistance += currentOrbitTravelSpeed * deltaTime;
        }

        private void UpdateOffsetVisuals(float deltaTime)
        {
            heightOffset += ActiveViewPreset.HeightTravelSpeed * heightIntent * deltaTime;
            heightOffset = Mathf.Clamp(heightOffset, vehicleProfile.VehicleOrbit.MinimumHeightOffset, vehicleProfile.VehicleOrbit.MaximumHeightOffset);
            zoomOffset += ActiveViewPreset.ZoomTravelSpeed * zoomIntent * deltaTime;
            zoomOffset = Mathf.Clamp(zoomOffset, vehicleProfile.VehicleOrbit.MinimumZoomOffset, vehicleProfile.VehicleOrbit.MaximumZoomOffset);
        }

        private void ApplyOrbitPose()
        {
            Vector3 cameraPosition;
            Vector3 inwardNormal;
            Vector3 watchPointLocalPosition;

            if (twoBodyClosedBezierOrbit != null)
            {
                cameraPosition = twoBodyClosedBezierOrbit.EvaluateWorldPosition(vehicleBody, orbitDistance);
                inwardNormal = twoBodyClosedBezierOrbit.EvaluateLeadBodyLocalInwardNormal(orbitDistance);
                watchPointLocalPosition = twoBodyClosedBezierOrbit.EvaluateLeadBodyLocalWatchPoint(orbitDistance);
            }
            else
            {
                cameraPosition = closedBezierOrbit.EvaluateWorldPosition(vehicleBody, orbitDistance);
                inwardNormal = closedBezierOrbit.EvaluateBodyLocalInwardNormal(orbitDistance);
                watchPointLocalPosition = closedBezierOrbit.EvaluateBodyLocalWatchPoint(orbitDistance);
            }

            Vector3 watchPointPosition = vehicleBody.TransformPoint(watchPointLocalPosition);

            cameraPosition += vehicleBody.up * heightOffset;
            cameraPosition += vehicleBody.TransformDirection(inwardNormal) * zoomOffset;
            Vector3 watchDirection = watchPointPosition - cameraPosition;

            assignedCamera.transform.SetPositionAndRotation(cameraPosition, Quaternion.LookRotation(watchDirection, vehicleBody.up));
        }

        private bool ShouldUseAttachedOrbit()
        {
            if (rearVehicleBody == null)
            {
                return false;
            }

            if (rearVehicleProfile == null)
            {
                return false;
            }

            return vehicleProfile.VehicleOrbit.MergeWhenAttached;
        }

        private void ApplyFixedPose()
        {
            Vector3 cameraPosition = vehicleBody.TransformPoint(vehicleProfile.FixedCameraLocalPosition);
            Vector3 watchPointPosition = vehicleBody.TransformPoint(vehicleProfile.FixedWatchPointLocalPosition);
            Vector3 watchDirection = watchPointPosition - cameraPosition;

            assignedCamera.transform.SetPositionAndRotation(cameraPosition, Quaternion.LookRotation(watchDirection, vehicleBody.up));
        }
    }
}
