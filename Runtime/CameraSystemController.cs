using System;
using System.Collections.Generic;
using Gley.Common;
using UnityEngine;

namespace Gley.CameraSystem
{
    [DefaultExecutionOrder(10000)]
    public class CameraSystemController : MonoBehaviour
    {
        private const float MinimumAimDistance = 0.0001f;

        private readonly List<Transform> chainBodies = new List<Transform>();
        private readonly List<Transform> warnedBodies = new List<Transform>();
        private readonly List<CameraOwnershipMarker> ownershipMarkers = new List<CameraOwnershipMarker>();
        private readonly ChainOrbitBuilder chainOrbitBuilder = new ChainOrbitBuilder();
        private readonly OrbitRemapper orbitRemapper = new OrbitRemapper();
        private readonly StoredPoseResolver storedPoseResolver = new StoredPoseResolver();
        private readonly MotionEstimator rootMotionEstimator = new MotionEstimator();
        private readonly StraightLineTransition activationTransition = new StraightLineTransition();

        [SerializeField] private Camera assignedCamera;
        [SerializeField] private VehicleCameraTarget target;
        [SerializeField] private CameraUpdateMode updateMode;
        private VehicleCameraTarget subscribedTarget;
        private CameraOwnershipMarker ownershipMarker;
        private VehicleViewEntry activeView;
        private VehicleProfile rootProfile;
        private VehicleOrbit activeOrbit;
        private ChainOrbit chainOrbit;
        private OrbitFrame orbitFrame;
        private Transform rootBody;
        [SerializeField] private string initialViewName;
        private float currentOrbitTravelSpeed;
        private float heightIntent;
        private float heightOffset;
        private float horizontalOrbitIntent;
        private float orbitDistance;
        private float zoomIntent;
        private float zoomOffset;
        private int nextCommandId = 1;
        private int runningCommandId;
        private int builtChainVersion;
        private bool isActive;
        private bool isTargetLost;
        private bool isTransitioning;

        public event Action<int, CommandEndResult> CommandEnded;
        public event Action<int> ViewChanged;
        public event Action TargetLost;
        public event Action CameraLost;
        public event Action<bool> NoClearPoseChanged;

        public Camera AssignedCamera => assignedCamera;
        public VehicleCameraTarget Target => target;
        public VehicleViewEntry ActiveView => activeView;
        public CameraViewPreset ActivePreset
        {
            get
            {
                if (activeView == null)
                {
                    return null;
                }

                return activeView.Preset;
            }
        }
        public CameraUpdateMode UpdateMode => updateMode;
        public string InitialViewName => initialViewName;
        public float CurrentOrbitTravelSpeed => currentOrbitTravelSpeed;
        public float HeightOffset => heightOffset;
        public float OrbitDistance => orbitDistance;
        public float ZoomOffset => zoomOffset;
        public bool IsActive => isActive;
        public bool IsTargetLost => isTargetLost;

        private void LateUpdate()
        {
            if (updateMode != CameraUpdateMode.LateUpdate || !isActive)
            {
                return;
            }

            UpdateCameraFrame(GetFrameDeltaTime());
        }

        private void OnEnable()
        {
            if (!isActive || isTargetLost)
            {
                return;
            }

            SubscribeToTarget();
            if (target != null && target.ChainVersion != builtChainVersion)
            {
                HandleChainChanged();
            }
        }

        public void UpdateCameraFrame(float deltaTime)
        {
            if (!isActive)
            {
                return;
            }

            if (assignedCamera == null)
            {
                HandleCameraLost();
                return;
            }

            if (isTargetLost)
            {
                return;
            }

            if (!IsTargetAlive())
            {
                HandleTargetLost();
                return;
            }

            rootMotionEstimator.UpdateMotionEstimate(GetScaledDeltaTime(deltaTime));
            UpdateCommandsAndInput(deltaTime);
            WriteCameraPose(deltaTime);
        }

        public void Configure(Camera camera, VehicleCameraTarget vehicleTarget, string viewName)
        {
            if (isActive)
            {
                ReportRejection($"{name}: Configure rejected: {CameraCommandResult.InvalidWhileActive}. Deactivate first.");
                return;
            }

            assignedCamera = camera;
            target = vehicleTarget;
            initialViewName = viewName;
        }

        public CameraCommandResult SetCamera(Camera camera)
        {
            if (isActive)
            {
                ReportRejection($"{name}: SetCamera rejected: {CameraCommandResult.InvalidWhileActive}. Deactivate first.");
                return CameraCommandResult.InvalidWhileActive;
            }

            if (camera == null)
            {
                ReportRejection($"{name}: SetCamera rejected: {CameraCommandResult.MissingCamera}.");
                return CameraCommandResult.MissingCamera;
            }

            assignedCamera = camera;
            return CameraCommandResult.Accepted;
        }

        public void SetUpdateMode(CameraUpdateMode mode)
        {
            updateMode = mode;
        }

        public CameraCommandResult Activate()
        {
            int commandId;
            return Activate(new TransitionOptions(TransitionMode.Snap), out commandId);
        }

        public CameraCommandResult Activate(TransitionOptions options, out int commandId)
        {
            commandId = 0;
            if (assignedCamera == null)
            {
                ReportRejection($"{name}: Activate rejected: {CameraCommandResult.MissingCamera}.");
                return CameraCommandResult.MissingCamera;
            }

            if (!IsTargetConfigured(target))
            {
                ReportRejection($"{name}: Activate rejected: {CameraCommandResult.MissingTarget}.");
                return CameraCommandResult.MissingTarget;
            }

            CameraSystemController owner;
            if (TryGetOtherOwner(out owner))
            {
                ReportRejection($"{name}: Activate rejected: camera in use. Camera '{assignedCamera.name}' is owned by '{owner.name}'.");
                return CameraCommandResult.CameraInUse;
            }

            VehicleViewEntry view;
            VehicleOrbit orbit;
            ChainOrbit builtOrbit;
            Vector3 startPosition = assignedCamera.transform.position;
            Quaternion startRotation = assignedCamera.transform.rotation;
            CameraCommandResult result = ValidateView(target, initialViewName, out view, out orbit, out builtOrbit);
            if (result != CameraCommandResult.Accepted)
            {
                ReportRejection($"{name}: Activate of view '{initialViewName}' rejected: {result}.");
                return result;
            }

            AcquireOwnership();
            SubscribeToTarget();
            isActive = true;
            isTargetLost = false;
            ApplyView(view, orbit, builtOrbit);
            rootMotionEstimator.Configure(target, Vector3.zero);
            WarnAboutRigidbodyInterpolation();
            if (options.Mode != TransitionMode.Snap)
            {
                Vector3 destinationPosition;
                Quaternion destinationRotation;
                CalculateCameraPose(out destinationPosition, out destinationRotation);
                float easeTime = activeView.Preset.Travel.EaseTime;
                if (options.Mode == TransitionMode.Duration)
                {
                    activationTransition.BeginWithDuration(startPosition, startRotation, destinationPosition, options.Value, easeTime);
                }
                else
                {
                    float speed = options.Value;
                    if (options.Mode == TransitionMode.PresetSpeed)
                    {
                        speed = activeView.Preset.Travel.StraightLineTransitionSpeed;
                    }

                    activationTransition.Begin(startPosition, startRotation, destinationPosition, speed, easeTime);
                }

                commandId = BeginCommand();
                isTransitioning = true;
            }

            WriteCameraPose(0f);
            return CameraCommandResult.Accepted;
        }

        public void Deactivate()
        {
            if (!isActive)
            {
                return;
            }

            EndRunningCommand(CommandEndResult.Interrupted);
            Release();
        }

        public CameraCommandResult SelectView(string viewName)
        {
            if (!isActive)
            {
                ReportRejection($"{name}: SelectView '{viewName}' rejected: {CameraCommandResult.NotActive}.");
                return CameraCommandResult.NotActive;
            }

            if (isTargetLost || !IsTargetAlive())
            {
                ReportRejection($"{name}: SelectView '{viewName}' rejected: {CameraCommandResult.MissingTarget}.");
                return CameraCommandResult.MissingTarget;
            }

            VehicleViewEntry view;
            VehicleOrbit orbit;
            ChainOrbit builtOrbit;
            CameraCommandResult result = ValidateView(target, viewName, out view, out orbit, out builtOrbit);
            if (result != CameraCommandResult.Accepted)
            {
                ReportRejection($"{name}: SelectView '{viewName}' rejected: {result}.");
                return result;
            }

            if (activeView != null && activeView.Id == view.Id)
            {
                return CameraCommandResult.Accepted;
            }

            EndRunningCommand(CommandEndResult.Replaced);
            ApplyView(view, orbit, builtOrbit);
            WriteCameraPose(0f);
            ViewChanged?.Invoke(view.Id);
            return CameraCommandResult.Accepted;
        }

        public CameraCommandResult SetTarget(VehicleCameraTarget newTarget)
        {
            if (!isActive)
            {
                target = newTarget;
                return CameraCommandResult.Accepted;
            }

            if (!IsTargetConfigured(newTarget))
            {
                ReportRejection($"{name}: SetTarget rejected: {CameraCommandResult.MissingTarget}.");
                return CameraCommandResult.MissingTarget;
            }

            VehicleViewEntry view;
            VehicleOrbit orbit;
            ChainOrbit builtOrbit;
            CameraCommandResult result = ValidateView(newTarget, activeView.Name, out view, out orbit, out builtOrbit);
            if (result != CameraCommandResult.Accepted)
            {
                ReportRejection($"{name}: SetTarget '{newTarget.name}' with view '{activeView.Name}' rejected: {result}.");
                return result;
            }

            EndRunningCommand(CommandEndResult.Replaced);
            UnsubscribeFromTarget();
            target = newTarget;
            SubscribeToTarget();
            isTargetLost = false;
            ApplyView(view, orbit, builtOrbit);
            rootMotionEstimator.Configure(target, Vector3.zero);
            WarnAboutRigidbodyInterpolation();
            WriteCameraPose(0f);
            return CameraCommandResult.Accepted;
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

        private float GetFrameDeltaTime()
        {
            CameraViewPreset preset = ActivePreset;
            if (preset != null && preset.TimeSource == CameraTimeSource.Scaled)
            {
                return Time.deltaTime;
            }

            return Time.unscaledDeltaTime;
        }

        private void SubscribeToTarget()
        {
            if (ReferenceEquals(subscribedTarget, target))
            {
                return;
            }

            UnsubscribeFromTarget();
            if (target == null)
            {
                return;
            }

            target.ChainChanged += HandleChainChanged;
            target.Destroyed += HandleTargetDestroyed;
            subscribedTarget = target;
        }

        private void UnsubscribeFromTarget()
        {
            if (ReferenceEquals(subscribedTarget, null))
            {
                return;
            }

            subscribedTarget.ChainChanged -= HandleChainChanged;
            subscribedTarget.Destroyed -= HandleTargetDestroyed;
            subscribedTarget = null;
        }

        private void HandleChainChanged()
        {
            if (!isActive || isTargetLost)
            {
                return;
            }

            if (!IsTargetAlive())
            {
                HandleTargetLost();
                return;
            }

            Transform previousRoot = rootBody;
            builtChainVersion = target.ChainVersion;
            rootBody = target.GetBody(target.RootIndex);
            rootProfile = target.GetProfile(target.RootIndex);
            RebuildChainBodies();
            WarnAboutRigidbodyInterpolation();
            if (previousRoot != rootBody)
            {
                rootMotionEstimator.Configure(target, Vector3.zero);
            }

            if (activeOrbit == null)
            {
                return;
            }

            ChainOrbit rebuiltOrbit = chainOrbitBuilder.Build(target, activeOrbit.Name);
            if (!IsOrbitUsable(rebuiltOrbit))
            {
                chainOrbit = null;
                orbitFrame = null;
                ReportRejection($"{name}: the orbit '{activeOrbit.Name}' is invalid after the vehicle chain changed; the camera holds its pose.");
                return;
            }

            float remappedDistance;
            if (orbitRemapper.Remap(chainOrbit, orbitDistance, rebuiltOrbit, out remappedDistance) != OrbitRemapResult.InvalidNewOrbit)
            {
                orbitDistance = remappedDistance;
            }

            chainOrbit = rebuiltOrbit;
            orbitFrame = new OrbitFrame(rootBody, activeOrbit.OrientationAdjustment);
        }

        private bool IsTargetAlive()
        {
            return target != null && target.IsRootAlive && rootBody != null;
        }

        private void HandleTargetLost()
        {
            if (isTargetLost)
            {
                return;
            }

            isTargetLost = true;
            EndRunningCommand(CommandEndResult.TargetLost);
            UnsubscribeFromTarget();
            ReportRejection($"{name}: target lost. The vehicle target or its root body was destroyed; the camera holds its last pose.");
            TargetLost?.Invoke();
        }

        private void EndRunningCommand(CommandEndResult result)
        {
            isTransitioning = false;
            if (runningCommandId == 0)
            {
                return;
            }

            int endedCommandId = runningCommandId;
            runningCommandId = 0;
            CommandEnded?.Invoke(endedCommandId, result);
        }

        private void ReportRejection(string message)
        {
            if (!Debug.isDebugBuild)
            {
                return;
            }

            CustomLogger.LogWarning(message, this);
        }

        private void RebuildChainBodies()
        {
            chainBodies.Clear();
            if (activeOrbit != null && !activeOrbit.MergeWhenAttached)
            {
                chainBodies.Add(rootBody);
                return;
            }

            for (int index = 0; index < target.BodyCount; index++)
            {
                chainBodies.Add(target.GetBody(index));
            }
        }

        private void WarnAboutRigidbodyInterpolation()
        {
            for (int index = 0; index < target.BodyCount; index++)
            {
                Transform body = target.GetBody(index);
                if (body == null || warnedBodies.Contains(body))
                {
                    continue;
                }

                Rigidbody bodyRigidbody = body.GetComponent<Rigidbody>();
                if (bodyRigidbody != null && bodyRigidbody.interpolation == RigidbodyInterpolation.None)
                {
                    warnedBodies.Add(body);
                    CustomLogger.LogWarning($"{body.name} has Rigidbody interpolation set to None; the camera may jitter. Set it to Interpolate.", body);
                }
            }
        }

        private bool IsOrbitUsable(ChainOrbit orbit)
        {
            return orbit != null && orbit.IsClosed && orbit.HasValidWatchMarkers;
        }

        private void HandleTargetDestroyed()
        {
            if (!isActive)
            {
                return;
            }

            HandleTargetLost();
        }

        private void HandleCameraLost()
        {
            EndRunningCommand(CommandEndResult.CameraLost);
            Release();
            ReportRejection($"{name}: camera lost. The assigned Camera was destroyed; the camera instance deactivated.");
            CameraLost?.Invoke();
        }

        private void Release()
        {
            ReleaseOwnership();
            UnsubscribeFromTarget();
            isActive = false;
            isTargetLost = false;
            activeView = null;
            activeOrbit = null;
            rootProfile = null;
            chainOrbit = null;
            orbitFrame = null;
            rootBody = null;
            chainBodies.Clear();
            currentOrbitTravelSpeed = 0f;
            heightIntent = 0f;
            horizontalOrbitIntent = 0f;
            zoomIntent = 0f;
            isTransitioning = false;
        }

        private void ReleaseOwnership()
        {
            if (ownershipMarker != null)
            {
                ownershipMarker.ClearOwner();
                DestroyOwnedObject(ownershipMarker);
            }

            ownershipMarker = null;
        }

        private void DestroyOwnedObject(UnityEngine.Object objectToDestroy)
        {
            if (Application.isPlaying)
            {
                Destroy(objectToDestroy);
            }
            else
            {
                DestroyImmediate(objectToDestroy);
            }
        }

        private float GetScaledDeltaTime(float deltaTime)
        {
            if (activeView.Preset.TimeSource == CameraTimeSource.Scaled)
            {
                return deltaTime;
            }

            return deltaTime * Time.timeScale;
        }

        private void UpdateCommandsAndInput(float deltaTime)
        {
            if (isTransitioning || chainOrbit == null)
            {
                return;
            }

            UpdateManualOrbitTravel(deltaTime);
            UpdateManualOffsets(deltaTime);
        }

        private void UpdateManualOrbitTravel(float deltaTime)
        {
            if (horizontalOrbitIntent == 0f)
            {
                currentOrbitTravelSpeed = 0f;
                return;
            }

            OrbitMovementSettings movement = activeView.Preset.OrbitMovement;
            float targetOrbitTravelSpeed = movement.ManualTravelSpeed * horizontalOrbitIntent;

            if (movement.StartResponseHalfLife <= 0f)
            {
                currentOrbitTravelSpeed = targetOrbitTravelSpeed;
            }
            else
            {
                float acceleration = movement.ManualTravelSpeed / movement.StartResponseHalfLife;
                currentOrbitTravelSpeed = Mathf.MoveTowards(currentOrbitTravelSpeed, targetOrbitTravelSpeed, acceleration * deltaTime);
            }

            orbitDistance += currentOrbitTravelSpeed * deltaTime;
        }

        private void UpdateManualOffsets(float deltaTime)
        {
            OrbitMovementSettings movement = activeView.Preset.OrbitMovement;
            heightOffset += movement.HeightRate * heightIntent * deltaTime;
            heightOffset = Mathf.Clamp(heightOffset, activeOrbit.MinimumHeightOffset, activeOrbit.MaximumHeightOffset);
            zoomOffset += movement.ZoomRate * zoomIntent * deltaTime;
            zoomOffset = Mathf.Clamp(zoomOffset, activeOrbit.MinimumZoomOffset, activeOrbit.MaximumZoomOffset);
        }

        private void WriteCameraPose(float deltaTime)
        {
            if (activeOrbit != null && chainOrbit == null)
            {
                return;
            }

            Vector3 cameraPosition;
            Quaternion cameraRotation;
            CalculateCameraPose(out cameraPosition, out cameraRotation);
            if (isTransitioning)
            {
                activationTransition.AdvanceTransition(deltaTime, cameraPosition, cameraRotation, out cameraPosition, out cameraRotation);
                assignedCamera.transform.SetPositionAndRotation(cameraPosition, cameraRotation);
                if (activationTransition.IsComplete)
                {
                    isTransitioning = false;
                    EndRunningCommand(CommandEndResult.Completed);
                }

                return;
            }

            assignedCamera.transform.SetPositionAndRotation(cameraPosition, cameraRotation);
        }

        private void CalculateCameraPose(out Vector3 cameraPosition, out Quaternion cameraRotation)
        {
            Vector3 targetPosition = ComputeTargetPose();
            Vector3 laggedPosition = ApplyFollowLag(targetPosition);
            Vector3 correctedPosition = ApplyCollision(laggedPosition);
            Quaternion aim = ComputeAim(correctedPosition);
            cameraPosition = correctedPosition;
            cameraRotation = aim;
        }

        private Vector3 ComputeTargetPose()
        {
            CameraViewType viewType = activeView.Preset.ViewType;
            if (viewType == CameraViewType.Fixed)
            {
                return rootBody.TransformPoint(rootProfile.FixedViewPose.CameraLocalPosition);
            }

            if (viewType == CameraViewType.Interior)
            {
                return rootBody.TransformPoint(rootProfile.Seat.EyeLocalPosition);
            }

            Vector3 position = orbitFrame.ToWorldPosition(chainOrbit.EvaluateRootLocalPosition(orbitDistance));
            position += orbitFrame.ToWorldInwardNormal(chainOrbit.EvaluateRootLocalInwardNormal(orbitDistance)) * zoomOffset;
            position += orbitFrame.Up * heightOffset;
            return position;
        }

        private Vector3 ApplyFollowLag(Vector3 targetPosition)
        {
            return targetPosition;
        }

        private Vector3 ApplyCollision(Vector3 smoothedPosition)
        {
            return smoothedPosition;
        }

        private Quaternion ComputeAim(Vector3 cameraPosition)
        {
            CameraViewType viewType = activeView.Preset.ViewType;
            if (viewType == CameraViewType.Interior)
            {
                return rootBody.rotation;
            }

            Vector3 watchPoint;
            if (viewType == CameraViewType.Fixed)
            {
                watchPoint = rootBody.TransformPoint(rootProfile.FixedViewPose.WatchPointLocalPosition);
            }
            else if (activeView.Preset.AimFrame == AimFrame.OwnerBody)
            {
                watchPoint = chainOrbit.EvaluateWorldWatchPointOwnerFrame(orbitDistance, chainBodies);
            }
            else
            {
                watchPoint = rootBody.TransformPoint(chainOrbit.EvaluateRootLocalWatchPoint(orbitDistance));
            }

            Vector3 watchDirection = watchPoint - cameraPosition;
            if (watchDirection.sqrMagnitude < MinimumAimDistance)
            {
                return assignedCamera.transform.rotation;
            }

            return Quaternion.LookRotation(watchDirection, rootBody.up);
        }

        private bool IsTargetConfigured(VehicleCameraTarget candidate)
        {
            return candidate != null && candidate.IsRootAlive && candidate.BodyCount > 0;
        }

        private bool TryGetOtherOwner(out CameraSystemController owner)
        {
            owner = null;
            assignedCamera.GetComponents(ownershipMarkers);
            for (int index = 0; index < ownershipMarkers.Count; index++)
            {
                if (ownershipMarkers[index].BlocksActivation(this, assignedCamera))
                {
                    owner = ownershipMarkers[index].Owner;
                    break;
                }
            }

            ownershipMarkers.Clear();
            return owner != null;
        }

        private CameraCommandResult ValidateView(VehicleCameraTarget candidate, string viewName, out VehicleViewEntry view, out VehicleOrbit orbit, out ChainOrbit builtOrbit)
        {
            orbit = null;
            builtOrbit = null;
            VehicleProfile profile = candidate.GetProfile(candidate.RootIndex);
            if (profile == null || !profile.TryGetView(viewName, out view))
            {
                view = null;
                return CameraCommandResult.ViewNotAvailable;
            }

            CameraViewPreset preset = view.Preset;
            if (preset == null)
            {
                return CameraCommandResult.ViewInvalid;
            }

            if (preset.ViewType == CameraViewType.Fixed && profile.FixedViewPose.CameraLocalPosition == profile.FixedViewPose.WatchPointLocalPosition)
            {
                return CameraCommandResult.ViewInvalid;
            }

            if (preset.ViewType == CameraViewType.Driving || preset.ViewType == CameraViewType.Presentation)
            {
                if (!profile.TryGetOrbit(view.OrbitId, out orbit))
                {
                    return CameraCommandResult.ViewInvalid;
                }

                builtOrbit = chainOrbitBuilder.Build(candidate, orbit.Name);
                if (!IsOrbitUsable(builtOrbit))
                {
                    return CameraCommandResult.ViewInvalid;
                }

                OrbitOffsetRangeValidationResult rangeResult = new ClosedBezierOrbit(orbit).OffsetRangeValidationResult;
                if (rangeResult != OrbitOffsetRangeValidationResult.Valid && rangeResult != OrbitOffsetRangeValidationResult.InwardZoomExceedsCurvature)
                {
                    return CameraCommandResult.ViewInvalid;
                }
            }

            if (preset.FormatVersion > CameraViewPreset.CurrentFormatVersion)
            {
                return CameraCommandResult.AssetNeedsUpgrade;
            }

            for (int index = 0; index < candidate.BodyCount; index++)
            {
                VehicleProfile bodyProfile = candidate.GetProfile(index);
                if (bodyProfile != null && bodyProfile.FormatVersion > VehicleProfile.CurrentFormatVersion)
                {
                    return CameraCommandResult.AssetNeedsUpgrade;
                }
            }

            return CameraCommandResult.Accepted;
        }

        private void AcquireOwnership()
        {
            ownershipMarker = null;
            assignedCamera.GetComponents(ownershipMarkers);
            for (int index = 0; index < ownershipMarkers.Count; index++)
            {
                CameraOwnershipMarker marker = ownershipMarkers[index];
                if (marker.Owner == this && ownershipMarker == null)
                {
                    ownershipMarker = marker;
                }
                else
                {
                    marker.ClearOwner();
                    DestroyOwnedObject(marker);
                }
            }

            ownershipMarkers.Clear();
            if (ownershipMarker == null)
            {
                ownershipMarker = assignedCamera.gameObject.AddComponent<CameraOwnershipMarker>();
                ownershipMarker.Configure(this);
            }
        }

        private void ApplyView(VehicleViewEntry view, VehicleOrbit orbit, ChainOrbit builtOrbit)
        {
            activeView = view;
            activeOrbit = orbit;
            chainOrbit = builtOrbit;
            builtChainVersion = target.ChainVersion;
            rootBody = target.GetBody(target.RootIndex);
            rootProfile = target.GetProfile(target.RootIndex);
            RebuildChainBodies();
            currentOrbitTravelSpeed = 0f;
            orbitFrame = null;
            orbitDistance = 0f;
            zoomOffset = 0f;
            heightOffset = 0f;
            if (chainOrbit == null)
            {
                return;
            }

            orbitFrame = new OrbitFrame(rootBody, orbit.OrientationAdjustment);
            LivePose pose = storedPoseResolver.Resolve(view.DefaultPose, chainOrbit, orbit, 0f);
            orbitDistance = pose.OrbitDistance;
            zoomOffset = pose.ZoomOffset;
            heightOffset = pose.HeightOffset;
        }

        private int BeginCommand()
        {
            EndRunningCommand(CommandEndResult.Replaced);
            runningCommandId = nextCommandId;
            nextCommandId++;
            return runningCommandId;
        }

        [ContextMenu("Activate")]
        private void ActivateFromContextMenu()
        {
            Activate();
        }

        [ContextMenu("Deactivate")]
        private void DeactivateFromContextMenu()
        {
            Deactivate();
        }

        private void OnDisable()
        {
            UnsubscribeFromTarget();
        }

        private void OnDestroy()
        {
            Deactivate();
            UnsubscribeFromTarget();
        }
    }
}
