using System;
using System.Collections.Generic;
using Gley.Common;
using UnityEngine;

namespace Gley.CameraSystem
{
    [DefaultExecutionOrder(10000)]
    public class CameraSystemController : MonoBehaviour
    {
        private const string OrbitResetName = "OrbitReset";
        private const string FullResetName = "FullReset";

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
        private readonly ViewSwitcher viewSwitcher = new ViewSwitcher();
        private readonly SpeedDistance speedDistance = new SpeedDistance();
        private readonly DrivingFollow drivingFollow = new DrivingFollow();
        private readonly AimSmoother aimSmoother = new AimSmoother();
        private readonly ReverseController reverseController = new ReverseController();
        private readonly TurnLook turnLook = new TurnLook();
        private readonly PlayerDefaults playerDefaults = new PlayerDefaults();
        private readonly ResetController resetController = new ResetController();
        private readonly Recenter recenter = new Recenter();
        private readonly CameraPlayerPreferences playerPreferences = new CameraPlayerPreferences();
        private readonly InteriorView interiorView = new InteriorView();
        private readonly SeatMotion seatMotion = new SeatMotion();

        [SerializeField] private Camera assignedCamera;
        [SerializeField] private VehicleCameraTarget target;
        [SerializeField] private CameraUpdateMode updateMode;
        private VehicleCameraTarget subscribedTarget;
        private CameraOwnershipMarker ownershipMarker;
        private VehicleViewEntry activeView;
        private VehicleViewEntry committedView;
        private VehicleProfile rootProfile;
        private VehicleOrbit activeOrbit;
        private ChainOrbit chainOrbit;
        private OrbitFrame orbitFrame;
        private Transform rootBody;
        private AimFrame aimFrame;
        private Vector3 lastRootPosition;
        private Quaternion lastRootRotation = Quaternion.identity;
        private Vector3 seatOffset;
        [SerializeField] private string initialViewName;
        [SerializeField, Min(0f)] private float maximumFrameDeltaTime = 0.1f;
        private float destinationBearing;
        private float composedOrbitDistance;
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
        public VehicleViewEntry ActiveView => committedView;
        public CameraViewPreset ActivePreset
        {
            get
            {
                if (committedView == null)
                {
                    return null;
                }

                return committedView.Preset;
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
        public float TurnLookOffset => turnLook.Offset;
        public float HeadYaw => interiorView.Yaw;
        public float HeadPitch => interiorView.Pitch;
        public float CushionIntensity => playerPreferences.CushionIntensity;
        public float OrbitSensitivity => playerPreferences.OrbitSensitivity;
        public float LookSensitivity => playerPreferences.LookSensitivity;
        public bool IsActive => isActive;
        public bool IsTargetLost => isTargetLost;
        public bool IsPlayerControlLocked => commandArbiter.IsPlayerControlLocked;
        public bool IsSwitchingView => viewSwitcher.IsSwitching;
        public bool IsReverseActive => reverseController.IsReverseActive;

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

            viewSwitcher.Stop();
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
                CalculateCameraPose(0f, out destinationPosition, out destinationRotation);
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

            viewSwitcher.Stop();
            EndRunningCommand(CommandEndResult.Interrupted);
            Release();
        }

        public CameraCommandResult SelectView(string viewName)
        {
            return SelectView(viewName, CommandSource.Game);
        }

        public CameraCommandResult SelectView(string viewName, CommandSource source)
        {
            int commandId;
            return SelectView(viewName, new TransitionOptions(TransitionMode.Snap), source, out commandId);
        }

        public CameraCommandResult SelectView(string viewName, TransitionOptions options, out int commandId)
        {
            return SelectView(viewName, options, CommandSource.Game, out commandId);
        }

        public CameraCommandResult SelectView(string viewName, TransitionOptions options, CommandSource source, out int commandId)
        {
            commandId = 0;
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

            if (activeView.Id == view.Id && !viewSwitcher.IsSwitching)
            {
                return CameraCommandResult.Accepted;
            }

            commandId = BeginViewSwitch(view, orbit, builtOrbit, options);
            return CameraCommandResult.Accepted;
        }

        public CameraCommandResult SetTarget(VehicleCameraTarget newTarget)
        {
            if (!isActive)
            {
                target = newTarget;
                return CameraCommandResult.Accepted;
            }

            int commandId;
            return ChangeTarget(newTarget, null, new TransitionOptions(TransitionMode.Snap), false, out commandId);
        }

        public CameraCommandResult ChangeTarget(VehicleCameraTarget newTarget, TransitionOptions options, bool preserveBearing, out int commandId)
        {
            return ChangeTarget(newTarget, null, options, preserveBearing, out commandId);
        }

        public CameraCommandResult ChangeTarget(VehicleCameraTarget newTarget, string viewName, TransitionOptions options, bool preserveBearing, out int commandId)
        {
            commandId = 0;
            if (!isActive)
            {
                target = newTarget;
                if (!string.IsNullOrEmpty(viewName))
                {
                    initialViewName = viewName;
                }

                return CameraCommandResult.Accepted;
            }

            if (!IsTargetConfigured(newTarget))
            {
                ReportRejection($"{name}: ChangeTarget rejected: {CameraCommandResult.MissingTarget}.");
                return CameraCommandResult.MissingTarget;
            }

            string destinationViewName = viewName;
            if (string.IsNullOrEmpty(destinationViewName))
            {
                destinationViewName = activeView.Name;
            }

            VehicleViewEntry view;
            VehicleOrbit orbit;
            ChainOrbit builtOrbit;
            CameraCommandResult result = ValidateView(newTarget, destinationViewName, out view, out orbit, out builtOrbit);
            if (result != CameraCommandResult.Accepted)
            {
                ReportRejection($"{name}: ChangeTarget '{newTarget.name}' with view '{destinationViewName}' rejected: {result}.");
                return result;
            }

            commandId = BeginTargetChange(newTarget, view, orbit, builtOrbit, options, preserveBearing);
            return CameraCommandResult.Accepted;
        }

        public void ShiftOrigin(Vector3 offset)
        {
            if (!isActive)
            {
                return;
            }

            drivingFollow.ShiftOrigin(offset);
            rootMotionEstimator.ShiftOrigin(offset);
            seatMotion.ShiftOrigin(offset);
            activationTransition.ShiftOrigin(offset);
            lastRootPosition += offset;
            if (isTargetLost && assignedCamera != null)
            {
                assignedCamera.transform.position += offset;
            }
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

        public CameraCommandResult SetReverse(bool active)
        {
            int commandId;
            return SetReverse(active, out commandId);
        }

        public CameraCommandResult SetReverse(bool active, out int commandId)
        {
            commandId = 0;
            if (!isActive)
            {
                ReportRejection($"{name}: SetReverse({active}) rejected: {CameraCommandResult.NotActive}.");
                return CameraCommandResult.NotActive;
            }

            if (isTargetLost || !IsTargetAlive())
            {
                ReportRejection($"{name}: SetReverse({active}) rejected: {CameraCommandResult.MissingTarget}.");
                return CameraCommandResult.MissingTarget;
            }

            if (!reverseController.SetReverseState(active) || !IsReverseView())
            {
                return CameraCommandResult.Accepted;
            }

            CameraCommandResult result;
            if (active)
            {
                result = reverseController.PlanEnter(chainOrbit, activeOrbit, orbitMovement, GetFrontDefaultPose());
            }
            else if (reverseController.CanReturn)
            {
                result = reverseController.PlanReturn(chainOrbit, orbitMovement);
            }
            else
            {
                reverseController.ForgetRememberedPose();
                return CameraCommandResult.Accepted;
            }

            if (result != CameraCommandResult.Accepted)
            {
                ReportRejection($"{name}: SetReverse({active}) travel rejected: {result}.");
                return result;
            }

            commandId = BeginCommand(CommandKind.Reverse, false);
            orbitMovement.StopManualTravel();
            reverseController.BeginPlannedTravel(new TransitionOptions(TransitionMode.PresetSpeed), activeView.Preset.Travel);
            return CameraCommandResult.Accepted;
        }

        public CameraCommandResult OrbitReset()
        {
            int commandId;
            return OrbitReset(CommandSource.Game, out commandId);
        }

        public CameraCommandResult OrbitReset(CommandSource source, out int commandId)
        {
            return RequestReset(false, source, out commandId);
        }

        public CameraCommandResult FullReset()
        {
            int commandId;
            return FullReset(CommandSource.Game, out commandId);
        }

        public CameraCommandResult FullReset(CommandSource source, out int commandId)
        {
            return RequestReset(true, source, out commandId);
        }

        public CameraCommandResult SaveCurrentPoseAsDefault(DefaultSlot slot)
        {
            CameraCommandResult result = GetSaveDefaultAvailability();
            if (result != CameraCommandResult.Accepted)
            {
                ReportRejection($"{name}: SaveCurrentPoseAsDefault {slot} rejected: {result}.");
                return result;
            }

            playerDefaults.SaveCurrentPoseAsDefault(rootProfile.ProfileId, activeView.Id, slot, chainOrbit, orbitMovement.Pose);
            return CameraCommandResult.Accepted;
        }

        public CameraPlayerPreferences GetPlayerPreferences()
        {
            CameraPlayerPreferences preferences = new CameraPlayerPreferences();
            preferences.CopyGlobalSettings(playerPreferences);
            for (int index = 0; index < playerDefaults.EntryCount; index++)
            {
                PlayerDefaultEntry entry = playerDefaults.GetEntry(index);
                preferences.AddViewEntry(new ViewPreferenceEntry(entry.ProfileId, entry.ViewId, entry.HasRearDefault, entry.RearDefault, entry.HasFrontDefault, entry.FrontDefault));
            }

            return preferences;
        }

        public void ApplyPlayerPreferences(CameraPlayerPreferences preferences)
        {
            if (preferences == null)
            {
                ReportRejection($"{name}: ApplyPlayerPreferences ignored: the preferences are null.");
                return;
            }

            if (preferences.FormatVersion > CameraPlayerPreferences.CurrentFormatVersion)
            {
                CustomLogger.LogWarning($"{name}: ApplyPlayerPreferences rejected: format version {preferences.FormatVersion} is newer than the supported version {CameraPlayerPreferences.CurrentFormatVersion}. Nothing was applied.", this);
                return;
            }

            playerPreferences.CopyGlobalSettings(preferences);
            seatMotion.SetCushionIntensity(playerPreferences.CushionIntensity);
            orbitMovement.SetSensitivity(playerPreferences.OrbitSensitivity);
            interiorView.SetSensitivity(playerPreferences.LookSensitivity);
            playerDefaults.Clear();
            IReadOnlyList<ViewPreferenceEntry> entries = preferences.ViewEntries;
            if (entries == null)
            {
                return;
            }

            for (int index = 0; index < entries.Count; index++)
            {
                ViewPreferenceEntry entry = entries[index];
                if (string.IsNullOrEmpty(entry.ProfileId) || (!entry.HasRearDefault && !entry.HasFrontDefault))
                {
                    continue;
                }

                playerDefaults.SetEntry(new PlayerDefaultEntry(entry.ProfileId, entry.ViewId, entry.HasRearDefault, entry.RearDefault, entry.HasFrontDefault, entry.FrontDefault));
            }
        }

        public void SetRecenterOverride(RecenterMode mode, float delay)
        {
            RecenterModeOverride modeOverride = RecenterModeOverride.Timed;
            if (mode == RecenterMode.Persistent)
            {
                modeOverride = RecenterModeOverride.Persistent;
            }

            playerPreferences.SetRecenterOverride(modeOverride, delay);
        }

        public void ClearRecenterOverride()
        {
            playerPreferences.ClearRecenterOverride();
        }

        public void SetCushionIntensity(float value)
        {
            playerPreferences.SetCushionIntensity(value);
            seatMotion.SetCushionIntensity(playerPreferences.CushionIntensity);
        }

        public void SetSensitivity(float orbit, float look)
        {
            playerPreferences.SetSensitivity(orbit, look);
            orbitMovement.SetSensitivity(playerPreferences.OrbitSensitivity);
            interiorView.SetSensitivity(playerPreferences.LookSensitivity);
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
            else if (!IsHeldInputNeutral(heldInput))
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

            recenter.NoteGestureInput();
            if (!AcceptPlayerInput())
            {
                return CameraCommandResult.PlayerControlLocked;
            }

            if (IsInteriorView())
            {
                if (CanApplyInteriorInput())
                {
                    interiorView.AddDrag(normalizedDelta);
                }
            }
            else if (CanApplyManualOrbitInput())
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

            if (IsInteriorView())
            {
                if (commandArbiter.IsPlayerControlLocked)
                {
                    return CameraCommandResult.PlayerControlLocked;
                }

                return CameraCommandResult.Accepted;
            }

            recenter.NoteGestureInput();
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

        private bool IsHeldInputNeutral(HeldInputGate heldInput)
        {
            if (IsInteriorView())
            {
                return heldInput.IsHorizontalAndVerticalNeutral;
            }

            return heldInput.IsNeutral;
        }

        private bool IsInteriorView()
        {
            return activeView != null && activeView.Preset.ViewType == CameraViewType.Interior;
        }

        private bool CanApplyInteriorInput()
        {
            return isActive && !isTargetLost && !isTransitioning && !viewSwitcher.IsSwitching && !resetController.IsTravelling;
        }

        private int BeginViewSwitch(VehicleViewEntry view, VehicleOrbit orbit, ChainOrbit builtOrbit, TransitionOptions options)
        {
            Vector3 startPosition = assignedCamera.transform.position;
            Quaternion startRotation = assignedCamera.transform.rotation;
            float currentDistance = orbitMovement.OrbitDistance;
            float sourceBearing;
            bool hasSourceBearing = viewSwitcher.TryGetOrbitBearing(chainOrbit, currentDistance, out sourceBearing);
            bool hasCurrentDistance = hasSourceBearing && orbit != null && activeOrbit.Id == orbit.Id;
            if (hasSourceBearing)
            {
                viewSwitcher.StoreAdjustment(activeView, orbitMovement.Pose);
            }

            viewSwitcher.Stop();
            int commandId = 0;
            if (options.Mode == TransitionMode.Snap)
            {
                EndRunningCommand(CommandEndResult.Replaced);
            }
            else
            {
                commandId = BeginCommand(CommandKind.ViewSwitch, options.LockPlayerControl);
            }

            float bearing;
            LivePose pose = viewSwitcher.PlanDestinationPose(view, orbit, builtOrbit, hasSourceBearing, sourceBearing, hasCurrentDistance, currentDistance, out bearing);
            ApplyViewGeometry(view, orbit, builtOrbit, pose);
            if (options.Mode == TransitionMode.Snap)
            {
                WriteCameraPose(0f);
                CommitActiveView();
                return 0;
            }

            Vector3 destinationPosition;
            Quaternion destinationRotation;
            CalculateCameraPose(0f, out destinationPosition, out destinationRotation);
            viewSwitcher.Begin(rootBody, startPosition, startRotation, destinationPosition, options, view.Preset.Travel);
            hasBearingDestination = chainOrbit != null;
            destinationBearing = bearing;
            WriteCameraPose(0f);
            return commandId;
        }

        private void CommitActiveView()
        {
            if (activeView == null || ReferenceEquals(committedView, activeView))
            {
                return;
            }

            bool isNameChanged = committedView == null || committedView.Name != activeView.Name;
            committedView = activeView;
            renderingState.ApplyRenderingSettings(activeView.Preset.Rendering);
            if (isNameChanged)
            {
                ViewChanged?.Invoke(activeView.Id);
            }
        }

        private int BeginTargetChange(VehicleCameraTarget newTarget, VehicleViewEntry view, VehicleOrbit orbit, ChainOrbit builtOrbit, TransitionOptions options, bool preserveBearing)
        {
            Vector3 startPosition = assignedCamera.transform.position;
            Quaternion startRotation = assignedCamera.transform.rotation;
            float sourceBearing = 0f;
            bool hasSourceBearing = false;
            if (preserveBearing && builtOrbit != null)
            {
                hasSourceBearing = viewSwitcher.TryGetOrbitBearing(chainOrbit, orbitMovement.OrbitDistance, out sourceBearing);
            }

            viewSwitcher.Stop();
            int commandId = 0;
            if (options.Mode == TransitionMode.Snap)
            {
                EndRunningCommand(CommandEndResult.Replaced);
            }
            else
            {
                commandId = BeginCommand(CommandKind.TargetChange, options.LockPlayerControl);
            }

            viewSwitcher.ClearAdjustments();
            UnsubscribeFromTarget();
            target = newTarget;
            SubscribeToTarget();
            isTargetLost = false;
            OrbitPose authoredPose = view.DefaultPose;
            float bearing = authoredPose.Bearing;
            if (hasSourceBearing)
            {
                bearing = viewSwitcher.GetNearestAllowedBearing(sourceBearing, view.Preset.OrbitMovement);
            }

            LivePose pose = storedPoseResolver.Resolve(new OrbitPose(bearing, authoredPose.ZoomOffset, authoredPose.HeightOffset), builtOrbit, orbit, 0f);
            ApplyViewGeometry(view, orbit, builtOrbit, pose);
            turnLook.Resume();
            rootMotionEstimator.Configure(target, Vector3.zero);
            WarnAboutRigidbodyInterpolation();
            if (options.Mode == TransitionMode.Snap)
            {
                WriteCameraPose(0f);
                CommitActiveView();
                return 0;
            }

            Vector3 destinationPosition;
            Quaternion destinationRotation;
            CalculateCameraPose(0f, out destinationPosition, out destinationRotation);
            viewSwitcher.Begin(rootBody, startPosition, startRotation, destinationPosition, options, view.Preset.Travel);
            hasBearingDestination = chainOrbit != null;
            destinationBearing = bearing;
            WriteCameraPose(0f);
            return commandId;
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
            if (request.Speed.Mode == TransitionMode.Snap)
            {
                ResetFollowSmoothing();
            }

            return commandId;
        }

        private void ResetFollowSmoothing()
        {
            drivingFollow.Reset();
            aimSmoother.Reset();
        }

        private bool IsReverseView()
        {
            return activeView.Preset.ViewType == CameraViewType.Driving && chainOrbit != null;
        }

        private OrbitPose GetFrontDefaultPose()
        {
            OrbitPose frontDefault;
            if (!playerDefaults.TryGetDefault(rootProfile.ProfileId, activeView.Id, DefaultSlot.Front, out frontDefault))
            {
                frontDefault = activeView.FrontDefaultPose;
            }

            return MapToAllowedBearing(frontDefault);
        }

        private OrbitPose MapToAllowedBearing(OrbitPose pose)
        {
            float bearing = viewSwitcher.GetNearestAllowedBearing(pose.Bearing, activeView.Preset.OrbitMovement);
            return new OrbitPose(bearing, pose.ZoomOffset, pose.HeightOffset);
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
            reverseController.StopTravel();
            resetController.StopTravel();
            if (viewSwitcher.IsSwitching)
            {
                viewSwitcher.Stop();
                CommitActiveView();
            }
        }

        private void ApplyHeldInput()
        {
            HeldInputGate heldInput = commandArbiter.HeldInput;
            orbitMovement.SetHeldIntent(heldInput.Horizontal, heldInput.Vertical, heldInput.Zoom);
            interiorView.SetHeldIntent(heldInput.Horizontal, heldInput.Vertical);
        }

        private CameraCommandResult RequestReset(bool isFullReset, CommandSource source, out int commandId)
        {
            commandId = 0;
            CameraCommandResult result = GetResetAvailability(source);
            if (result == CameraCommandResult.Accepted && (IsOrbitResetView() || IsInteriorView()))
            {
                result = BeginReset(isFullReset, false, out commandId);
            }

            if (result != CameraCommandResult.Accepted)
            {
                string commandName = OrbitResetName;
                if (isFullReset)
                {
                    commandName = FullResetName;
                }

                ReportRejection($"{name}: {commandName} rejected: {result}.");
            }

            return result;
        }

        private CameraCommandResult GetResetAvailability(CommandSource source)
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

            if (IsOrbitResetView() && chainOrbit == null)
            {
                return CameraCommandResult.ViewInvalid;
            }

            return CameraCommandResult.Accepted;
        }

        private bool IsOrbitResetView()
        {
            CameraViewType viewType = activeView.Preset.ViewType;
            return viewType == CameraViewType.Driving || viewType == CameraViewType.Presentation;
        }

        private CameraCommandResult BeginReset(bool isFullReset, bool isRecenter, out int commandId)
        {
            commandId = 0;
            bool isInterior = IsInteriorView();
            CameraCommandResult result;
            if (isInterior)
            {
                result = resetController.PlanHeadReset(interiorView);
            }
            else if (isFullReset)
            {
                result = resetController.PlanFullReset(chainOrbit, activeOrbit, orbitMovement, GetResetDefaultPose());
            }
            else
            {
                result = resetController.PlanOrbitReset(chainOrbit, activeOrbit, orbitMovement, GetResetDefaultPose());
            }

            if (result != CameraCommandResult.Accepted)
            {
                return result;
            }

            if (isRecenter && resetController.IsPlannedAtDestination)
            {
                ReturnToDefault();
                return CameraCommandResult.Accepted;
            }

            commandId = BeginCommand(CommandKind.Reset, false);
            if (isInterior)
            {
                resetController.BeginPlannedHeadReturn(activeView.Preset.Recenter.ReturnHalfLife);
                return CameraCommandResult.Accepted;
            }

            if (isFullReset)
            {
                viewSwitcher.ClearAdjustment(activeView.Id);
            }

            orbitMovement.StopManualTravel();
            resetController.BeginPlannedTravel(new TransitionOptions(TransitionMode.PresetSpeed), activeView.Preset.Travel);
            return CameraCommandResult.Accepted;
        }

        private OrbitPose GetResetDefaultPose()
        {
            if (activeView.Preset.ViewType != CameraViewType.Driving)
            {
                return MapToAllowedBearing(activeView.DefaultPose);
            }

            if (reverseController.IsReverseActive)
            {
                return GetFrontDefaultPose();
            }

            OrbitPose rearDefault;
            if (!playerDefaults.TryGetDefault(rootProfile.ProfileId, activeView.Id, DefaultSlot.Rear, out rearDefault))
            {
                rearDefault = activeView.DefaultPose;
            }

            return MapToAllowedBearing(rearDefault);
        }

        private void ReturnToDefault()
        {
            turnLook.Resume();
            recenter.ClearPending();
        }

        private CameraCommandResult GetSaveDefaultAvailability()
        {
            if (!isActive)
            {
                return CameraCommandResult.NotActive;
            }

            if (isTargetLost || !IsTargetAlive())
            {
                return CameraCommandResult.MissingTarget;
            }

            if (activeView.Preset.ViewType != CameraViewType.Driving)
            {
                return CameraCommandResult.NotSupportedInView;
            }

            if (chainOrbit == null)
            {
                return CameraCommandResult.ViewInvalid;
            }

            return CameraCommandResult.Accepted;
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

            if (!commandArbiter.AcceptPlayerInput())
            {
                return false;
            }

            reverseController.NotePlayerInput();
            recenter.MarkPending();
            if (activeView != null && (activeView.Preset.ViewType == CameraViewType.Driving || activeView.Preset.ViewType == CameraViewType.Interior))
            {
                turnLook.Suspend();
            }

            return true;
        }

        private bool CanApplyManualOrbitInput()
        {
            return isActive && !isTargetLost && !isTransitioning && !viewSwitcher.IsSwitching && !pointOfInterestTravel.IsTravelling && !reverseController.IsTravelling && !resetController.IsTravelling && chainOrbit != null;
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
            if (IsInteriorView())
            {
                seatOffset = seatMotion.UpdateSeatMotion(scaledDeltaTime);
            }

            UpdateCommandsAndInput(deltaTime, scaledDeltaTime);
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
            target.OriginShifted += ShiftOrigin;
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
            subscribedTarget.OriginShifted -= ShiftOrigin;
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
                if (IsInteriorView())
                {
                    seatMotion.Configure(target, rootProfile.Seat, activeView.Preset.Interior);
                    seatOffset = Vector3.zero;
                }
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
                reverseController.ForgetRememberedPose();
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

            reverseController.RemapRememberedPose(orbitRemapper, chainOrbit, rebuiltOrbit);
            chainOrbit = rebuiltOrbit;
            pointOfInterestTravel.SetOrbit(chainOrbit, rootBody);
            orbitFrame = new OrbitFrame(rootBody, activeOrbit.OrientationAdjustment);
            if (hasBearingDestination)
            {
                OrbitPose destinationPose = new OrbitPose(destinationBearing, orbitMovement.PlayerZoom, orbitMovement.HeightOffset);
                orbitDistance = storedPoseResolver.Resolve(destinationPose, chainOrbit, activeOrbit, orbitDistance).OrbitDistance;
            }

            orbitMovement.Configure(chainOrbit, activeOrbit, activeView.Preset.OrbitMovement, new LivePose(orbitDistance, orbitMovement.PlayerZoom, orbitMovement.HeightOffset));
            recenter.MarkPending();
            ReplanRunningTravel();
        }

        private void ReplanRunningTravel()
        {
            CameraCommandResult result = CameraCommandResult.Accepted;
            if (pointOfInterestTravel.IsTravelling)
            {
                result = pointOfInterestTravel.ReplanTravel(activeView.Preset.Travel);
            }
            else if (reverseController.IsTravelling)
            {
                result = reverseController.ReplanTravel(chainOrbit, activeOrbit, orbitMovement, GetFrontDefaultPose(), activeView.Preset.Travel);
            }
            else if (resetController.IsTravelling)
            {
                result = resetController.ReplanTravel(chainOrbit, activeOrbit, orbitMovement, activeView.Preset.Travel);
            }

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

            ResetFollowSmoothing();
            rootMotionEstimator.Reset();
            seatMotion.Reset();
            seatOffset = Vector3.zero;
            WriteCameraPose(0f);
        }

        private void HandleCameraLost()
        {
            viewSwitcher.Stop();
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
            committedView = null;
            activeOrbit = null;
            rootProfile = null;
            chainOrbit = null;
            orbitFrame = null;
            rootBody = null;
            chainBodies.Clear();
            orbitMovement.Clear();
            orbitMovement.ClearHeldIntent();
            interiorView.Clear();
            seatMotion.Clear();
            seatOffset = Vector3.zero;
            pointOfInterestTravel.Clear();
            viewSwitcher.Clear();
            reverseController.Clear();
            resetController.Clear();
            recenter.Clear();
            turnLook.Clear();
            ResetFollowSmoothing();
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

        private void UpdateCommandsAndInput(float deltaTime, float scaledDeltaTime)
        {
            UpdateRecenter(scaledDeltaTime);
            if (isTransitioning || viewSwitcher.IsSwitching)
            {
                return;
            }

            if (IsInteriorView())
            {
                UpdateInteriorHeadLook(deltaTime);
                return;
            }

            if (chainOrbit == null)
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

            if (reverseController.IsTravelling)
            {
                if (reverseController.UpdateReverseTravel(deltaTime))
                {
                    EndRunningCommand(CommandEndResult.Completed);
                }

                return;
            }

            if (resetController.IsTravelling)
            {
                if (resetController.UpdateResetTravel(deltaTime))
                {
                    EndRunningCommand(CommandEndResult.Completed);
                    ReturnToDefault();
                }

                return;
            }

            ApplyHeldInput();
            orbitMovement.UpdateOrbitMovement(deltaTime);
        }

        private void UpdateRecenter(float scaledDeltaTime)
        {
            bool canRecenter = IsRecenterView() && !commandArbiter.HasRunningCommand;
            HeldInputGate heldInput = commandArbiter.HeldInput;
            bool isHoldingInput = !heldInput.IsReceivedNeutral;
            if (IsInteriorView())
            {
                isHoldingInput = !heldInput.IsReceivedHorizontalAndVerticalNeutral;
            }

            RecenterSettings presetRecenter = activeView.Preset.Recenter;
            RecenterMode mode = playerPreferences.GetRecenterMode(presetRecenter);
            float delay = playerPreferences.GetRecenterDelay(presetRecenter);
            if (!recenter.UpdateRecenterCountdown(scaledDeltaTime, mode, delay, presetRecenter.StationaryThreshold, rootMotionEstimator.Speed, canRecenter, isHoldingInput))
            {
                return;
            }

            int commandId;
            if (BeginReset(false, true, out commandId) != CameraCommandResult.Accepted)
            {
                recenter.ClearPending();
            }
        }

        private void UpdateInteriorHeadLook(float deltaTime)
        {
            if (resetController.IsTravelling)
            {
                if (resetController.UpdateResetTravel(deltaTime))
                {
                    EndRunningCommand(CommandEndResult.Completed);
                    ReturnToDefault();
                }

                return;
            }

            ApplyHeldInput();
            interiorView.UpdateInteriorHeadLook(deltaTime);
        }

        private bool IsRecenterView()
        {
            if (IsInteriorView())
            {
                return true;
            }

            return activeView.Preset.ViewType == CameraViewType.Driving && chainOrbit != null;
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
            CalculateCameraPose(deltaTime, out cameraPosition, out cameraRotation);
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

            if (viewSwitcher.IsSwitching)
            {
                viewSwitcher.UpdateViewSwitchTransition(deltaTime, rootBody, cameraPosition, cameraRotation, out cameraPosition, out cameraRotation);
                assignedCamera.transform.SetPositionAndRotation(cameraPosition, cameraRotation);
                if (viewSwitcher.IsComplete)
                {
                    EndRunningCommand(CommandEndResult.Completed);
                    DiscardHeldInput();
                }

                return;
            }

            assignedCamera.transform.SetPositionAndRotation(cameraPosition, cameraRotation);
        }

        private void CalculateCameraPose(float deltaTime, out Vector3 cameraPosition, out Quaternion cameraRotation)
        {
            Vector3 targetPosition = ComputeTargetPose(deltaTime);
            Vector3 laggedPosition = ApplyFollowLag(targetPosition, deltaTime);
            Vector3 correctedPosition = ApplyCollision(laggedPosition);
            Quaternion aim = ComputeAim(correctedPosition);
            cameraPosition = correctedPosition;
            cameraRotation = ApplyAimSmoothing(aim, deltaTime);
        }

        private Vector3 ComputeTargetPose(float deltaTime)
        {
            CameraViewType viewType = activeView.Preset.ViewType;
            if (viewType == CameraViewType.Fixed)
            {
                return rootBody.TransformPoint(rootProfile.FixedViewPose.CameraLocalPosition);
            }

            if (viewType == CameraViewType.Interior)
            {
                turnLook.UpdateInteriorTurnLook(deltaTime, rootMotionEstimator.YawRate, target.HasTurnHint, target.TurnHint, activeView.Preset.Interior, reverseController.IsReverseActive);
                return interiorView.ComputeEyePosition(rootBody, rootProfile.Seat, seatOffset);
            }

            float orbitDistance = ApplyTurnLook(orbitMovement.OrbitDistance, deltaTime);
            composedOrbitDistance = orbitDistance;
            Vector3 localPosition = chainOrbit.EvaluateRootLocalPosition(orbitDistance);
            float bearing = chainOrbit.Bearing.BearingOfLocalPoint(localPosition);
            bool isReverseApplied = viewType == CameraViewType.Driving && reverseController.IsReverseActive;
            float speedOffset = speedDistance.Offset(rootMotionEstimator.Speed, bearing, activeView.Preset.Driving, isReverseApplied);
            float zoom = distanceComposer.ComposeZoom(orbitMovement.PlayerZoom, speedOffset, activeOrbit);
            Vector3 position = orbitFrame.ToWorldPosition(localPosition);
            position += orbitFrame.ToWorldInwardNormal(chainOrbit.EvaluateRootLocalInwardNormal(orbitDistance)) * zoom;
            position += orbitFrame.Up * orbitMovement.HeightOffset;
            return position;
        }

        private float ApplyTurnLook(float orbitDistance, float deltaTime)
        {
            CameraViewPreset preset = activeView.Preset;
            if (preset.ViewType != CameraViewType.Driving)
            {
                return orbitDistance;
            }

            float offset = turnLook.UpdateDrivingTurnLook(deltaTime, rootMotionEstimator.YawRate, target.HasTurnHint, target.TurnHint, preset.Driving, reverseController.IsReverseActive);
            if (offset == 0f)
            {
                return orbitDistance;
            }

            OrbitBearing orbitBearing = chainOrbit.Bearing;
            float bearing = orbitBearing.BearingOfLocalPoint(chainOrbit.EvaluateRootLocalPosition(orbitDistance));
            float shiftedDistance;
            if (!orbitBearing.TryGetDistanceAtBearing(bearing + offset, orbitDistance, out shiftedDistance))
            {
                return orbitDistance;
            }

            return orbitMovement.ClampDistanceToAngleLimits(shiftedDistance);
        }

        private Vector3 ApplyFollowLag(Vector3 targetPosition, float deltaTime)
        {
            CameraViewPreset preset = activeView.Preset;
            if (preset.ViewType != CameraViewType.Driving)
            {
                return targetPosition;
            }

            return drivingFollow.UpdateDrivingFollow(targetPosition, deltaTime, preset.Driving);
        }

        private Vector3 ApplyCollision(Vector3 smoothedPosition)
        {
            return smoothedPosition;
        }

        private Quaternion ComputeAim(Vector3 cameraPosition)
        {
            CameraViewType viewType = activeView.Preset.ViewType;
            float imageRollFollow = activeView.Preset.OrbitMovement.ImageRollFollow;
            if (viewType == CameraViewType.Interior)
            {
                return interiorView.ComputeRotation(rootBody.rotation, turnLook.Offset, imageRollFollow);
            }

            Quaternion fallbackRotation = assignedCamera.transform.rotation;
            if (viewType == CameraViewType.Fixed)
            {
                Vector3 fixedWatchPoint = rootBody.TransformPoint(rootProfile.FixedViewPose.WatchPointLocalPosition);
                return orbitAim.ComputeAim(cameraPosition, fixedWatchPoint, rootBody.up, imageRollFollow, fallbackRotation);
            }

            Vector3 watchPoint = orbitAim.EvaluateWatchPoint(chainOrbit, composedOrbitDistance, aimFrame, rootBody, chainBodies);
            return orbitAim.ComputeAim(cameraPosition, watchPoint, orbitFrame.Up, imageRollFollow, fallbackRotation);
        }

        private Quaternion ApplyAimSmoothing(Quaternion aim, float deltaTime)
        {
            CameraViewPreset preset = activeView.Preset;
            if (preset.ViewType != CameraViewType.Driving)
            {
                return aim;
            }

            return aimSmoother.UpdateAimSmoothing(aim, deltaTime, preset.Driving);
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
                    return CameraCommandResult.OrbitNotFound;
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
            committedView = view;
            renderingState.ApplyRenderingSettings(view.Preset.Rendering);
            turnLook.Resume();
            ApplyViewGeometry(view, orbit, builtOrbit, storedPoseResolver.Resolve(view.DefaultPose, builtOrbit, orbit, 0f));
        }

        private void ApplyViewGeometry(VehicleViewEntry view, VehicleOrbit orbit, ChainOrbit builtOrbit, LivePose pose)
        {
            activeView = view;
            activeOrbit = orbit;
            chainOrbit = builtOrbit;
            builtChainVersion = target.ChainVersion;
            rootBody = target.GetBody(target.RootIndex);
            rootProfile = target.GetProfile(target.RootIndex);
            RebuildChainBodies();
            aimFrame = view.Preset.AimFrame;
            DiscardHeldInput();
            ResetFollowSmoothing();
            turnLook.Reset();
            reverseController.ForgetRememberedPose();
            recenter.MarkPending();
            orbitFrame = null;
            orbitMovement.Clear();
            interiorView.Configure(view.Preset.Interior);
            seatMotion.Clear();
            seatOffset = Vector3.zero;
            if (view.Preset.ViewType == CameraViewType.Interior)
            {
                seatMotion.Configure(target, rootProfile.Seat, view.Preset.Interior);
            }

            pointOfInterestTravel.Configure(chainOrbit, rootBody, orbitMovement);
            if (chainOrbit == null)
            {
                return;
            }

            orbitFrame = new OrbitFrame(rootBody, orbit.OrientationAdjustment);
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
