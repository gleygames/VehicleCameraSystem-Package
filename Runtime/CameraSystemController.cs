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
        private readonly PointOfInterestTravel pointOfInterestTravel = new PointOfInterestTravel();
        private readonly DistanceComposer distanceComposer = new DistanceComposer();
        private readonly OrbitAim orbitAim = new OrbitAim();
        private readonly CommandArbiter commandArbiter = new CommandArbiter();

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
        private Vector3 lastRootPosition;
        private Quaternion lastRootRotation = Quaternion.identity;
        [SerializeField] private string initialViewName;
        [SerializeField, Min(0f)] private float maximumFrameDeltaTime = 0.1f;
        private float destinationBearing;
        private int builtChainVersion;
        private bool isActive;
        private bool isTargetLost;
        private bool isTransitioning;
        private bool hasBearingDestination;

        public event Action<int, CommandEndResult> CommandEnded
        {
            add
            {
                commandArbiter.CommandEnded += value;
            }
            remove
            {
                commandArbiter.CommandEnded -= value;
            }
        }
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
        public bool IsPlayerControlLocked => commandArbiter.IsPlayerControlLocked;

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

            EndRunningCommand(CommandEndResult.Replaced);
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

                commandId = BeginCommand(CommandKind.Activation, options.LockPlayerControl);
                isTransitioning = true;
                hasBearingDestination = chainOrbit != null;
                destinationBearing = view.DefaultPose.Bearing;
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
            return SelectView(viewName, CommandSource.Game);
        }

        public CameraCommandResult SelectView(string viewName, CommandSource source)
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

            if (IsBlockedByPlayerLock(source))
            {
                ReportRejection($"{name}: SelectView '{viewName}' rejected: {CameraCommandResult.PlayerControlLocked}.");
                return CameraCommandResult.PlayerControlLocked;
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

        public CameraCommandResult RequestPoint(string pointName)
        {
            int commandId;
            return RequestPoint(pointName, new TravelRequest(TravelDirection.Shortest), out commandId);
        }

        public CameraCommandResult RequestPoint(string pointName, TravelRequest request, out int commandId)
        {
            return RequestPoint(pointName, request, CommandSource.Game, out commandId);
        }

        public CameraCommandResult RequestPoint(string pointName, TravelRequest request, CommandSource source, out int commandId)
        {
            commandId = 0;
            CameraCommandResult result = GetPointTravelAvailability(source);
            if (result == CameraCommandResult.Accepted)
            {
                result = pointOfInterestTravel.PlanPoint(pointName, request.Direction);
            }

            if (result != CameraCommandResult.Accepted)
            {
                ReportRejection($"{name}: RequestPoint '{pointName}' rejected: {result}.");
                return result;
            }

            commandId = BeginPointTravel(request);
            return CameraCommandResult.Accepted;
        }

        public CameraCommandResult RequestNextPoint()
        {
            int commandId;
            return RequestNextPoint(true, new TravelRequest(TravelDirection.Shortest), out commandId);
        }

        public CameraCommandResult RequestNextPoint(bool wrap, TravelRequest request, out int commandId)
        {
            return RequestNextPoint(wrap, request, CommandSource.Game, out commandId);
        }

        public CameraCommandResult RequestNextPoint(bool wrap, TravelRequest request, CommandSource source, out int commandId)
        {
            commandId = 0;
            CameraCommandResult result = GetPointTravelAvailability(source);
            if (result == CameraCommandResult.Accepted)
            {
                result = pointOfInterestTravel.PlanNext(wrap, request.Direction);
            }

            if (result != CameraCommandResult.Accepted)
            {
                ReportRejection($"{name}: RequestNextPoint rejected: {result}.");
                return result;
            }

            commandId = BeginPointTravel(request);
            return CameraCommandResult.Accepted;
        }

        public CameraCommandResult RequestPreviousPoint()
        {
            int commandId;
            return RequestPreviousPoint(true, new TravelRequest(TravelDirection.Shortest), out commandId);
        }

        public CameraCommandResult RequestPreviousPoint(bool wrap, TravelRequest request, out int commandId)
        {
            return RequestPreviousPoint(wrap, request, CommandSource.Game, out commandId);
        }

        public CameraCommandResult RequestPreviousPoint(bool wrap, TravelRequest request, CommandSource source, out int commandId)
        {
            commandId = 0;
            CameraCommandResult result = GetPointTravelAvailability(source);
            if (result == CameraCommandResult.Accepted)
            {
                result = pointOfInterestTravel.PlanPrevious(wrap, request.Direction);
            }

            if (result != CameraCommandResult.Accepted)
            {
                ReportRejection($"{name}: RequestPreviousPoint rejected: {result}.");
                return result;
            }

            commandId = BeginPointTravel(request);
            return CameraCommandResult.Accepted;
        }

        public CameraCommandResult SetHeldIntent(float horizontal, float vertical, float zoom)
        {
            HeldInputGate heldInput = commandArbiter.HeldInput;
            heldInput.ReceiveHeldInput(horizontal, vertical, zoom);
            CameraCommandResult result = CameraCommandResult.Accepted;
            if (!isActive)
            {
                result = CameraCommandResult.NotActive;
            }
            else if (commandArbiter.IsPlayerControlLocked)
            {
                heldInput.Rearm();
                if (!heldInput.IsReceivedNeutral)
                {
                    result = CameraCommandResult.PlayerControlLocked;
                }
            }
            else if (!heldInput.IsNeutral)
            {
                AcceptPlayerInput();
            }

            ApplyHeldInput();
            return result;
        }

        public void SetHorizontalOrbitIntent(float intent)
        {
            HeldInputGate heldInput = commandArbiter.HeldInput;
            SetHeldIntent(intent, heldInput.ReceivedVertical, heldInput.ReceivedZoom);
        }

        public void SetHeightIntent(float intent)
        {
            HeldInputGate heldInput = commandArbiter.HeldInput;
            SetHeldIntent(heldInput.ReceivedHorizontal, intent, heldInput.ReceivedZoom);
        }

        public void SetZoomIntent(float intent)
        {
            HeldInputGate heldInput = commandArbiter.HeldInput;
            SetHeldIntent(heldInput.ReceivedHorizontal, heldInput.ReceivedVertical, intent);
        }

        public CameraCommandResult AddDrag(Vector2 normalizedDelta)
        {
            if (!isActive)
            {
                return CameraCommandResult.NotActive;
            }

            if (normalizedDelta == Vector2.zero)
            {
                return CameraCommandResult.Accepted;
            }

            if (!AcceptPlayerInput())
            {
                return CameraCommandResult.PlayerControlLocked;
            }

            if (CanApplyManualOrbitInput())
            {
                orbitMovement.AddDrag(normalizedDelta);
            }

            return CameraCommandResult.Accepted;
        }

        public CameraCommandResult AddPinch(float normalizedSpan)
        {
            if (!isActive)
            {
                return CameraCommandResult.NotActive;
            }

            if (normalizedSpan == 0f)
            {
                return CameraCommandResult.Accepted;
            }

            if (!AcceptPlayerInput())
            {
                return CameraCommandResult.PlayerControlLocked;
            }

            if (CanApplyManualOrbitInput())
            {
                orbitMovement.AddPinch(normalizedSpan);
            }

            return CameraCommandResult.Accepted;
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

        public void SetPlayerControlLocked(bool locked)
        {
            commandArbiter.SetExplicitPlayerLock(locked);
            ApplyHeldInput();
        }

        private bool IsBlockedByPlayerLock(CommandSource source)
        {
            return source == CommandSource.Player && commandArbiter.IsPlayerControlLocked;
        }

        private CameraCommandResult GetPointTravelAvailability(CommandSource source)
        {
            if (!isActive)
            {
                return CameraCommandResult.NotActive;
            }

            if (isTargetLost || !IsTargetAlive())
            {
                return CameraCommandResult.MissingTarget;
            }

            if (IsBlockedByPlayerLock(source))
            {
                return CameraCommandResult.PlayerControlLocked;
            }

            if (chainOrbit == null)
            {
                return CameraCommandResult.PointNotFound;
            }

            return CameraCommandResult.Accepted;
        }

        private int BeginPointTravel(TravelRequest request)
        {
            int commandId = BeginCommand(CommandKind.PointOfInterest, request.LockPlayerControl);
            orbitMovement.StopManualTravel();
            pointOfInterestTravel.BeginPlannedTravel(request.Speed, activeView.Preset.Travel);
            return commandId;
        }

        private int BeginCommand(CommandKind kind, bool holdsLock)
        {
            StopCommandMotion();
            int commandId = commandArbiter.BeginCommand(kind, holdsLock);
            ApplyHeldInput();
            return commandId;
        }

        private void StopCommandMotion()
        {
            isTransitioning = false;
            hasBearingDestination = false;
            pointOfInterestTravel.Stop();
        }

        private void ApplyHeldInput()
        {
            HeldInputGate heldInput = commandArbiter.HeldInput;
            orbitMovement.SetHeldIntent(heldInput.Horizontal, heldInput.Vertical, heldInput.Zoom);
        }

        private bool AcceptPlayerInput()
        {
            if (commandArbiter.IsPlayerControlLocked)
            {
                return false;
            }

            if (commandArbiter.HasRunningCommand)
            {
                StopCommandMotion();
            }

            return commandArbiter.AcceptPlayerInput();
        }

        private bool CanApplyManualOrbitInput()
        {
            return isActive && !isTargetLost && !isTransitioning && !pointOfInterestTravel.IsTravelling && chainOrbit != null;
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
            target.Teleported += HandleTeleported;
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
            subscribedTarget.Teleported -= HandleTeleported;
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
                pointOfInterestTravel.SetOrbit(null, rootBody);
                EndRunningCommand(CommandEndResult.TargetDisconnected);
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
            pointOfInterestTravel.SetOrbit(chainOrbit, rootBody);
            orbitFrame = new OrbitFrame(rootBody, activeOrbit.OrientationAdjustment);
            if (hasBearingDestination)
            {
                OrbitPose destinationPose = new OrbitPose(destinationBearing, orbitMovement.PlayerZoom, orbitMovement.HeightOffset);
                orbitDistance = storedPoseResolver.Resolve(destinationPose, chainOrbit, activeOrbit, orbitDistance).OrbitDistance;
            }

            orbitMovement.Configure(chainOrbit, activeOrbit, activeView.Preset.OrbitMovement, new LivePose(orbitDistance, orbitMovement.PlayerZoom, orbitMovement.HeightOffset));
            ReplanRunningTravel();
        }

        private void ReplanRunningTravel()
        {
            if (!pointOfInterestTravel.IsTravelling)
            {
                return;
            }

            CameraCommandResult result = pointOfInterestTravel.ReplanTravel(activeView.Preset.Travel);
            if (result == CameraCommandResult.Unreachable)
            {
                EndRunningCommand(CommandEndResult.Unreachable);
            }
            else if (result != CameraCommandResult.Accepted)
            {
                EndRunningCommand(CommandEndResult.TargetDisconnected);
            }
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
            StopCommandMotion();
            commandArbiter.EndRunningCommand(result);
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

        private void HandleTeleported()
        {
            if (!isActive || isTargetLost || assignedCamera == null || !IsTargetAlive())
            {
                return;
            }

            if (isTransitioning)
            {
                Quaternion rootDelta = rootBody.rotation * Quaternion.Inverse(lastRootRotation);
                Vector3 startPosition = rootBody.position + rootDelta * (activationTransition.StartPosition - lastRootPosition);
                activationTransition.RelocateStart(startPosition, rootDelta * activationTransition.StartRotation);
            }

            WriteCameraPose(0f);
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
            pointOfInterestTravel.Clear();
            isTransitioning = false;
            hasBearingDestination = false;
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

            if (pointOfInterestTravel.IsTravelling)
            {
                if (pointOfInterestTravel.UpdatePointOfInterestTravel(deltaTime))
                {
                    EndRunningCommand(CommandEndResult.Completed);
                }

                return;
            }

            ApplyHeldInput();
            orbitMovement.UpdateOrbitMovement(deltaTime);
        }

        private void WriteCameraPose(float deltaTime)
        {
            if (activeOrbit != null && chainOrbit == null)
            {
                return;
            }

            lastRootPosition = rootBody.position;
            lastRootRotation = rootBody.rotation;
            Vector3 cameraPosition;
            Quaternion cameraRotation;
            CalculateCameraPose(out cameraPosition, out cameraRotation);
            if (isTransitioning)
            {
                activationTransition.AdvanceTransition(deltaTime, cameraPosition, cameraRotation, out cameraPosition, out cameraRotation);
                assignedCamera.transform.SetPositionAndRotation(cameraPosition, cameraRotation);
                if (activationTransition.IsComplete)
                {
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
            DiscardHeldInput();
            orbitFrame = null;
            orbitMovement.Clear();
            pointOfInterestTravel.Configure(chainOrbit, rootBody, orbitMovement);
            if (chainOrbit == null)
            {
                return;
            }

            orbitFrame = new OrbitFrame(rootBody, orbit.OrientationAdjustment);
            LivePose pose = storedPoseResolver.Resolve(view.DefaultPose, chainOrbit, orbit, 0f);
            orbitMovement.Configure(chainOrbit, orbit, view.Preset.OrbitMovement, pose);
        }

        private void DiscardHeldInput()
        {
            commandArbiter.HeldInput.Rearm();
            ApplyHeldInput();
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
