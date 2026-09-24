using System;
using System.Collections.Generic;
using Gley.Common;
using UnityEngine;

namespace Gley.CameraSystem
{
    [DefaultExecutionOrder(10000)]
    public class CameraSystemController : MonoBehaviour
    {
        private readonly List<Transform> chainBodies = new List<Transform>();
        private readonly List<Transform> warnedBodies = new List<Transform>();
        private readonly List<CameraOwnershipMarker> ownershipMarkers = new List<CameraOwnershipMarker>();
        private readonly ChainOrbitBuilder chainOrbitBuilder = new ChainOrbitBuilder();
        private readonly OrbitRemapper orbitRemapper = new OrbitRemapper();
        private readonly StoredPoseResolver storedPoseResolver = new StoredPoseResolver();
        private readonly MotionEstimator rootMotionEstimator = new MotionEstimator();
        private readonly StraightLineTransition activationTransition = new StraightLineTransition();
        private readonly CameraRenderingState renderingState = new CameraRenderingState();
        private readonly OrbitMovement orbitMovement = new OrbitMovement();
        private readonly DistanceComposer distanceComposer = new DistanceComposer();
        private readonly OrbitAim orbitAim = new OrbitAim();

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
        private AimFrame aimFrame;
        [SerializeField] private string initialViewName;
        [SerializeField, Min(0f)] private float maximumFrameDeltaTime = 0.1f;
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
        public LivePose CurrentPose => orbitMovement.Pose;
        public AimFrame AimFrame => aimFrame;
        public string InitialViewName => initialViewName;
        public float MaximumFrameDeltaTime => maximumFrameDeltaTime;
        public float CurrentOrbitTravelSpeed => orbitMovement.TravelSpeed;
        public float HeightOffset => orbitMovement.HeightOffset;
        public float OrbitDistance => orbitMovement.OrbitDistance;
        public float ZoomOffset => orbitMovement.PlayerZoom;
        public bool IsActive => isActive;
        public bool IsTargetLost => isTargetLost;

        private void LateUpdate()
        {
            if (updateMode != CameraUpdateMode.LateUpdate || !isActive)
            {
                return;
            }

            UpdateCameraFrame(GetFrameDeltaTime(), Time.deltaTime);
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

            UpdateCameraFrame(deltaTime, GetScaledDeltaTime(deltaTime));
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

            if (!isActive)
            {
                renderingState.RecordBaseline(assignedCamera);
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

        public void SetHeldIntent(float horizontal, float vertical, float zoom)
        {
            orbitMovement.SetHeldIntent(horizontal, vertical, zoom);
        }

        public void SetHorizontalOrbitIntent(float intent)
        {
            orbitMovement.SetHeldIntent(intent, orbitMovement.VerticalIntent, orbitMovement.ZoomIntent);
        }

        public void SetHeightIntent(float intent)
        {
            orbitMovement.SetHeldIntent(orbitMovement.HorizontalIntent, intent, orbitMovement.ZoomIntent);
        }

        public void SetZoomIntent(float intent)
        {
            orbitMovement.SetHeldIntent(orbitMovement.HorizontalIntent, orbitMovement.VerticalIntent, intent);
        }

        public void AddDrag(Vector2 normalizedDelta)
        {
            if (!CanApplyManualOrbitInput())
            {
                return;
            }

            orbitMovement.AddDrag(normalizedDelta);
        }

        public void AddPinch(float normalizedSpan)
        {
            if (!CanApplyManualOrbitInput())
            {
                return;
            }

            orbitMovement.AddPinch(normalizedSpan);
        }

        public CameraCommandResult SetAimFrame(AimFrame frame)
        {
            if (!isActive)
            {
                ReportRejection($"{name}: SetAimFrame {frame} rejected: {CameraCommandResult.NotActive}.");
                return CameraCommandResult.NotActive;
            }

            aimFrame = frame;
            return CameraCommandResult.Accepted;
        }

        private bool CanApplyManualOrbitInput()
        {
            return isActive && !isTargetLost && !isTransitioning && chainOrbit != null;
        }

        private float GetFrameDeltaTime()
        {
            float deltaTime = Time.unscaledDeltaTime;
            CameraViewPreset preset = ActivePreset;
            if (preset != null && preset.TimeSource == CameraTimeSource.Scaled)
            {
                deltaTime = Time.deltaTime;
            }

            if (maximumFrameDeltaTime > 0f)
            {
                deltaTime = Mathf.Min(deltaTime, maximumFrameDeltaTime);
            }

            return deltaTime;
        }

        private void UpdateCameraFrame(float deltaTime, float scaledDeltaTime)
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

            rootMotionEstimator.UpdateMotionEstimate(scaledDeltaTime);
            UpdateCommandsAndInput(deltaTime);
            WriteCameraPose(deltaTime);
        }

        private float GetScaledDeltaTime(float deltaTime)
        {
            if (activeView.Preset.TimeSource == CameraTimeSource.Scaled)
            {
                return deltaTime;
            }

            return deltaTime * Time.timeScale;
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

            float orbitDistance = orbitMovement.OrbitDistance;
            float remappedDistance;
            if (orbitRemapper.Remap(chainOrbit, orbitDistance, rebuiltOrbit, out remappedDistance) != OrbitRemapResult.InvalidNewOrbit)
            {
                orbitDistance = remappedDistance;
            }

            chainOrbit = rebuiltOrbit;
            orbitFrame = new OrbitFrame(rootBody, activeOrbit.OrientationAdjustment);
            orbitMovement.Configure(chainOrbit, activeOrbit, activeView.Preset.OrbitMovement, new LivePose(orbitDistance, orbitMovement.PlayerZoom, orbitMovement.HeightOffset));
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
            renderingState.Restore();
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
            orbitMovement.Clear();
            orbitMovement.ClearHeldIntent();
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

        private void UpdateCommandsAndInput(float deltaTime)
        {
            if (isTransitioning || chainOrbit == null)
            {
                return;
            }

            orbitMovement.UpdateOrbitMovement(deltaTime);
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

            float orbitDistance = orbitMovement.OrbitDistance;
            float zoom = distanceComposer.ComposeZoom(orbitMovement.PlayerZoom, 0f, activeOrbit);
            Vector3 position = orbitFrame.ToWorldPosition(chainOrbit.EvaluateRootLocalPosition(orbitDistance));
            position += orbitFrame.ToWorldInwardNormal(chainOrbit.EvaluateRootLocalInwardNormal(orbitDistance)) * zoom;
            position += orbitFrame.Up * orbitMovement.HeightOffset;
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

            float imageRollFollow = activeView.Preset.OrbitMovement.ImageRollFollow;
            Quaternion fallbackRotation = assignedCamera.transform.rotation;
            if (viewType == CameraViewType.Fixed)
            {
                Vector3 fixedWatchPoint = rootBody.TransformPoint(rootProfile.FixedViewPose.WatchPointLocalPosition);
                return orbitAim.ComputeAim(cameraPosition, fixedWatchPoint, rootBody.up, imageRollFollow, fallbackRotation);
            }

            Vector3 watchPoint = orbitAim.EvaluateWatchPoint(chainOrbit, orbitMovement.OrbitDistance, aimFrame, rootBody, chainBodies);
            return orbitAim.ComputeAim(cameraPosition, watchPoint, orbitFrame.Up, imageRollFollow, fallbackRotation);
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
            renderingState.ApplyRenderingSettings(view.Preset.Rendering);
            builtChainVersion = target.ChainVersion;
            rootBody = target.GetBody(target.RootIndex);
            rootProfile = target.GetProfile(target.RootIndex);
            RebuildChainBodies();
            aimFrame = view.Preset.AimFrame;
            orbitFrame = null;
            orbitMovement.Clear();
            if (chainOrbit == null)
            {
                return;
            }

            orbitFrame = new OrbitFrame(rootBody, orbit.OrientationAdjustment);
            LivePose pose = storedPoseResolver.Resolve(view.DefaultPose, chainOrbit, orbit, 0f);
            orbitMovement.Configure(chainOrbit, orbit, view.Preset.OrbitMovement, pose);
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
