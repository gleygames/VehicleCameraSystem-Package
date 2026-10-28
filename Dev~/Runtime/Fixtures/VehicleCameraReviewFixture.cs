using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Gley.CameraSystem.Dev
{
    public class VehicleCameraReviewFixture : MonoBehaviour
    {
        private const string DrivingName = "Driving";
        private const string PresentationName = "Presentation";
        private const string FixedName = "Fixed";
        private const string InteriorName = "Interior";
        private const string ReviewTruckProfileId = "vehicle-camera-review-truck";
        private const float HitchGap = 0.5f;
        private const float TruckRearWatchHeightFraction = 0.6f;
        private const float TrailerRearWatchAboveRoof = 1f;
        private const float MinimumHitchDistance = 0.0001f;
        private const float MaximumCornerRadius = 1.5f;
        private const float ArcHandleScale = 0.55228475f;
        private const float MaximumInwardZoom = 1f;
        private const int OrbitKnotCount = 8;
        private const float MinimumLoggedStep = 0.0001f;
        private const float PathSampleSpacing = 0.02f;
        private const float OffPoseTolerance = 0.01f;
        private const float LongFrameTolerance = 0.0001f;
        private const int MaximumPathSamples = 5000;
        private const int FlushLineCount = 120;
        private const int MaximumLoggedLines = 50000;
        private const float ReviewPoseTolerance = 0.001f;
        private const float ReviewMovementThreshold = 0.05f;
        private const float ReviewCommandTimeout = 15f;
        private const float SpeedRampPeakSpeed = 30f;
        private const float SpeedRampChangeSeconds = 10f;
        private const float SpeedRampHoldSeconds = 4f;
        private const float SpeedRampLogInterval = 1f;
        private const float CushionReviewSpeed = 10f;
        private const float CushionBumpStart = 1f;
        private const float CushionBumpDuration = 0.75f;
        private const float CushionBumpHeight = 0.0015f;
        private const float CushionHillStart = 4f;
        private const float CushionHillEntryDuration = 10f;
        private const float CushionReviewDuration = 20f;
        private const float CollisionWallAhead = 10f;
        private const float CollisionWallLength = 40f;
        private const float CollisionWallHeight = 12f;
        private const float CollisionWallThickness = 0.5f;
        private const float TunnelAhead = 15f;
        private const float TunnelLength = 60f;
        private const float TunnelSideClearance = 0.4f;
        private const float TunnelRoofClearance = 0.5f;
        private const float TunnelWallThickness = 1f;
        private const float TunnelCeilingThickness = 6f;
        private const int TunnelCeilingIndex = 0;
        private const int TunnelLeftWallIndex = 1;
        private const int TunnelRightWallIndex = 2;

        private readonly List<Transform> trailers = new List<Transform>();
        private readonly List<Object> createdAssets = new List<Object>();
        private readonly StringBuilder debugBuffer = new StringBuilder();

        private CameraSystemController controller;
        private CameraSystemController secondController;
        private CameraEventForwarder eventForwarder;
        private VehicleCameraTarget target;
        private VehicleProfile truckProfile;
        private VehicleProfile trailerProfile;
        private CameraViewPreset drivingPreset;
        private CameraViewPreset presentationPreset;
        private Transform truck;
        private Transform secondTruck;
        private Transform collisionWall;
        private Transform tunnel;
        private VehicleCameraTarget secondTarget;
        private ChainOrbit debugOrbit;
        private Vector3 lastCameraLocalPosition;
        private Vector3 lastInwardNormal;
        private CommandEndResult lastEndedResult;
        [SerializeField] private LayerMask drivingRemovedLayers;
        [SerializeField] private TransitionMode viewTransition = TransitionMode.PresetSpeed;
        private string debugFilePath;
        [SerializeField] private string viewName = DrivingName;
        [SerializeField] private string pointName = "plate";
        [SerializeField, TextArea(3, 10)] private string playerPreferencesJson;
        [SerializeField] private float speed;
        [SerializeField] private float steering;
        [SerializeField, Range(-45f, 45f)] private float truckRoll;
        [SerializeField, Range(0f, 1f)] private float imageRollFollow;
        [SerializeField, Min(0f)] private float viewTransitionValue = 2f;
        [SerializeField, Range(0f, 180f)] private float presentationAngleLimit = 90f;
        private float heading;
        [SerializeField, Min(0.001f)] private float jumpTolerance = 0.05f;
        private float lastOrbitDistance;
        private float lastZoom;
        private float lastHeight;
        private float lastUnscaledDeltaTime;
        [SerializeField, Range(0f, 2f)] private float cushionIntensity = 1f;
        private float cushionReviewElapsed;
        private float cushionReviewStartHeight;
        private float cushionBumpPeak;
        private float cushionSlopePeak;
        [SerializeField, Min(0.5f)] private float truckHalfWidth = 1.25f;
        [SerializeField, Min(0.5f)] private float truckLength = 6f;
        [SerializeField, Min(0.5f)] private float trailerLength = 12f;
        [SerializeField, Min(0.5f)] private float bodyHeight = 3.5f;
        [SerializeField, Min(0f)] private float collisionWallInset = 0.35f;
        [SerializeField, Range(-1f, 1f)] private float horizontalIntent;
        [SerializeField, Range(-1f, 1f)] private float heightIntent;
        [SerializeField, Range(-1f, 1f)] private float zoomIntent;
        [SerializeField, Range(0, 3)] private int trailerCount;
        private int smoothActivationRequestFrame;
        private int debugOrbitChainVersion = -1;
        private int debugOrbitViewId = -1;
        private int bufferedLineCount;
        private int loggedLineCount;
        private int jumpCount;
        private int offPoseCount;
        private int longFrameCount;
        private int lastEndedCommandId;
        private int reviewPassCount;
        private int reviewCheckCount;
        [SerializeField] private bool activateOnStart = true;
        [SerializeField] private bool lockPlayerControl;
        [SerializeField] private bool presentationAngleLimits;
        [SerializeField] private bool reverse;
        [SerializeField] private bool preserveBearing;
        private bool isSmoothActivationPending;
        [SerializeField] private bool logCameraMotion = true;
        [SerializeField] private bool bodyColliders;
        private bool hasMotionSample;
        private bool isLockReviewRunning;
        private bool isSpeedRampRunning;
        private bool isCushionReviewRunning;

        public CameraSystemController Controller => controller;
        public VehicleCameraTarget Target => target;

        private void Start()
        {
            debugFilePath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "VehicleCameraReviewDebug.txt"));
            Directory.CreateDirectory(Path.GetDirectoryName(debugFilePath));
            File.WriteAllText(debugFilePath, $"Vehicle camera review debug log, started {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}{System.Environment.NewLine}");
            Debug.Log($"Camera debug log: {debugFilePath}", this);
            drivingPreset = CreatePreset(CameraViewType.Driving);
            presentationPreset = CreatePreset(CameraViewType.Presentation);
            CameraViewPreset fixedPreset = CreatePreset(CameraViewType.Fixed);
            CameraViewPreset interiorPreset = CreatePreset(CameraViewType.Interior);
            if (drivingRemovedLayers.value != 0)
            {
                drivingPreset.Rendering.Configure(CullingMaskMode.AddRemove, 0, 0, drivingRemovedLayers, false, 0.3f, false, 1000f);
            }

            truckProfile = CreateProfile(truckLength, bodyHeight * TruckRearWatchHeightFraction);
            JsonUtility.FromJsonOverwrite("{\"profileId\":\"" + ReviewTruckProfileId + "\"}", truckProfile);
            trailerProfile = CreateProfile(trailerLength, bodyHeight + TrailerRearWatchAboveRoof);
            AddViews(truckProfile, drivingPreset, presentationPreset, fixedPreset, interiorPreset);
            truck = CreateBody("Review Truck", truckLength, new Color(0.8f, 0.35f, 0.2f));
            truck.SetParent(transform, false);
            target = truck.gameObject.AddComponent<VehicleCameraTarget>();
            target.Configure(truck, truckProfile);
            GameObject cameraObject = new GameObject("Review Camera");
            cameraObject.transform.SetParent(transform, false);
            Camera reviewCamera = cameraObject.AddComponent<Camera>();
            reviewCamera.nearClipPlane = 0.05f;
            reviewCamera.farClipPlane = 2000f;
            controller = cameraObject.AddComponent<CameraSystemController>();
            controller.Configure(reviewCamera, target, viewName);
            controller.CommandEnded += LogCommandEnded;
            eventForwarder = cameraObject.AddComponent<CameraEventForwarder>();
            eventForwarder.SetController(controller);
            eventForwarder.NoClearPoseChanged.AddListener(LogNoClearPoseChanged);
            CreateGround();
            if (activateOnStart)
            {
                ActivateCamera();
            }
        }

        private void Update()
        {
            if (truck == null)
            {
                return;
            }

            LogCameraMotion();
            float deltaTime = Time.deltaTime;
            heading += steering * deltaTime;
            truck.localRotation = Quaternion.Euler(0f, heading, truckRoll);
            truck.position += truck.forward * speed * deltaTime;
            UpdateCushionReviewVisuals(deltaTime);
            UpdateImageRollFollow();
            UpdatePresentationAngleLimits();
            UpdateTrailerPoses();
            UpdateTrailerCount();
            UpdateSmoothActivationRequest();
            controller.SetHeldIntent(horizontalIntent, heightIntent, zoomIntent);
            if (controller.IsActive && !controller.IsTargetLost)
            {
                controller.SetReverse(reverse);
            }
        }

        [ContextMenu("Activate")]
        public void ActivateCamera()
        {
            if (!controller.IsActive)
            {
                controller.Configure(controller.AssignedCamera, target, viewName);
            }

            Debug.Log($"Activate: {controller.Activate()}", this);
        }

        [ContextMenu("Activate Smoothly (2 Seconds)")]
        public void ActivateSmoothly()
        {
            if (controller.IsActive)
            {
                Debug.LogWarning("Deactivate the camera or prepare the smooth activation before starting this transition.", this);
                return;
            }

            isSmoothActivationPending = true;
            smoothActivationRequestFrame = Time.frameCount;
        }

        [ContextMenu("Prepare Smooth Activation")]
        public void PrepareSmoothActivation()
        {
            if (controller == null || truck == null)
            {
                return;
            }

            controller.Deactivate();
            Camera camera = controller.AssignedCamera;
            if (camera == null)
            {
                return;
            }

            Vector3 startPosition = truck.position - truck.forward * 20f + truck.up * (bodyHeight + 10f);
            Quaternion startRotation = Quaternion.LookRotation(truck.position - startPosition, truck.up);
            camera.transform.SetPositionAndRotation(startPosition, startRotation);
        }

        [ContextMenu("Deactivate")]
        public void DeactivateCamera()
        {
            controller.Deactivate();
        }

        [ContextMenu("Select View")]
        public void SelectView()
        {
            Debug.Log($"SelectView '{viewName}': {controller.SelectView(viewName)}; zoom {controller.ZoomOffset:F2}, height {controller.HeightOffset:F2}", this);
        }

        [ContextMenu("Switch View (Transition)")]
        public void SwitchView()
        {
            int commandId;
            CameraCommandResult result = controller.SelectView(viewName, new TransitionOptions(viewTransition, viewTransitionValue, lockPlayerControl), out commandId);
            Debug.Log($"SwitchView '{viewName}' ({viewTransition} {viewTransitionValue}): {result}, command {commandId}; zoom {controller.ZoomOffset:F2}, height {controller.HeightOffset:F2}", this);
        }

        [ContextMenu("Request Point")]
        public void RequestPoint()
        {
            int commandId;
            CameraCommandResult result = controller.RequestPoint(pointName, new TravelRequest(TravelDirection.Shortest, lockPlayerControl), out commandId);
            Debug.Log($"RequestPoint '{pointName}': {result}, command {commandId}", this);
        }

        [ContextMenu("Request Next Point")]
        public void RequestNextPoint()
        {
            int commandId;
            CameraCommandResult result = controller.RequestNextPoint(true, new TravelRequest(TravelDirection.Shortest, lockPlayerControl), out commandId);
            Debug.Log($"RequestNextPoint: {result}, command {commandId}", this);
        }

        [ContextMenu("Request Previous Point")]
        public void RequestPreviousPoint()
        {
            int commandId;
            CameraCommandResult result = controller.RequestPreviousPoint(true, new TravelRequest(TravelDirection.Shortest, lockPlayerControl), out commandId);
            Debug.Log($"RequestPreviousPoint: {result}, command {commandId}", this);
        }

        [ContextMenu("Toggle Reverse")]
        public void ToggleReverse()
        {
            reverse = !reverse;
            Debug.Log($"Reverse: {reverse}", this);
        }

        [ContextMenu("Orbit Reset")]
        public void OrbitReset()
        {
            int commandId;
            CameraCommandResult result = controller.OrbitReset(CommandSource.Player, out commandId);
            Debug.Log($"OrbitReset: {result}, command {commandId}", this);
        }

        [ContextMenu("Full Reset")]
        public void FullReset()
        {
            int commandId;
            CameraCommandResult result = controller.FullReset(CommandSource.Player, out commandId);
            Debug.Log($"FullReset: {result}, command {commandId}", this);
        }

        [ContextMenu("Save Rear Default")]
        public void SaveRearDefault()
        {
            Debug.Log($"SaveCurrentPoseAsDefault Rear: {controller.SaveCurrentPoseAsDefault(DefaultSlot.Rear)}", this);
        }

        [ContextMenu("Save Front Default")]
        public void SaveFrontDefault()
        {
            Debug.Log($"SaveCurrentPoseAsDefault Front: {controller.SaveCurrentPoseAsDefault(DefaultSlot.Front)}", this);
        }

        [ContextMenu("Log Player Preferences JSON")]
        public void LogPlayerPreferencesJson()
        {
            playerPreferencesJson = JsonUtility.ToJson(controller.GetPlayerPreferences());
            Debug.Log($"Player preferences JSON: {playerPreferencesJson}", this);
        }

        [ContextMenu("Apply Player Preferences JSON")]
        public void ApplyPlayerPreferencesJson()
        {
            if (string.IsNullOrEmpty(playerPreferencesJson))
            {
                Debug.LogWarning("Player Preferences Json is empty.", this);
                return;
            }

            controller.ApplyPlayerPreferences(JsonUtility.FromJson<CameraPlayerPreferences>(playerPreferencesJson));
            Debug.Log($"Applied player preferences JSON: {playerPreferencesJson}", this);
        }

        [ContextMenu("Run Lock Review (S17)")]
        public void RunLockReview()
        {
            if (isLockReviewRunning)
            {
                Debug.LogWarning("Lock review is already running.", this);
                return;
            }

            StartCoroutine(RunLockReviewSequence());
        }

        [ContextMenu("Run Speed Ramp (S19)")]
        public void RunSpeedRamp()
        {
            if (isSpeedRampRunning)
            {
                Debug.LogWarning("Speed ramp is already running.", this);
                return;
            }

            StartCoroutine(RunSpeedRampSequence());
        }

        [ContextMenu("Run Cushion Review (S27)")]
        public void RunCushionReview()
        {
            if (isCushionReviewRunning || isSpeedRampRunning || controller == null || !controller.IsActive)
            {
                Debug.LogWarning("Activate the camera before starting the cushion review.", this);
                return;
            }

            controller.Deactivate();
            viewName = InteriorName;
            controller.Configure(controller.AssignedCamera, target, InteriorName);
            CameraCommandResult activationResult = controller.Activate();
            if (activationResult != CameraCommandResult.Accepted)
            {
                Debug.LogError($"Cushion review activation failed: {activationResult}.", this);
                return;
            }

            controller.SetCushionIntensity(cushionIntensity);
            target.ClearSuppliedAcceleration();
            target.ClearSuppliedSpeed();
            speed = CushionReviewSpeed;
            steering = 0f;
            cushionReviewElapsed = 0f;
            cushionReviewStartHeight = truck.position.y;
            cushionBumpPeak = 0f;
            cushionSlopePeak = 0f;
            isCushionReviewRunning = true;
            StartCoroutine(MeasureCushionReviewSequence());
            Debug.Log($"Cushion review started at intensity {cushionIntensity:F1}: bump at 1 second, then a gradual 5% climb from 4 seconds.", this);
        }

        [ContextMenu("Activate Second Controller On Same Camera")]
        public void ActivateSecondController()
        {
            if (secondController == null)
            {
                GameObject secondObject = new GameObject("Second Controller");
                secondObject.transform.SetParent(transform, false);
                secondController = secondObject.AddComponent<CameraSystemController>();
                secondController.Configure(controller.AssignedCamera, target, FixedName);
            }

            Debug.Log($"Second controller Activate: {secondController.Activate()}", this);
        }

        [ContextMenu("Attach Trailer")]
        public void AttachTrailer()
        {
            Transform front = GetLastBody();
            VehicleProfile frontProfile = GetLastProfile();
            Transform trailer = CreateBody("Review Trailer " + (trailers.Count + 1), trailerLength, new Color(0.25f, 0.45f, 0.8f));
            trailer.SetParent(transform, false);
            trailer.rotation = front.rotation;
            trailer.position = front.TransformPoint(frontProfile.ConnectorAnchors.RearLocalPosition) - trailer.rotation * trailerProfile.ConnectorAnchors.FrontLocalPosition;
            VehicleTargetResult result = target.Attach(trailer, trailerProfile, ChainEnd.Rear);
            if (result != VehicleTargetResult.Accepted)
            {
                Debug.LogWarning($"Attach: {result}", this);
                Destroy(trailer.gameObject);
                return;
            }

            trailers.Add(trailer);
            trailerCount = trailers.Count;
        }

        [ContextMenu("Detach Last Trailer")]
        public void DetachLastTrailer()
        {
            if (trailers.Count == 0)
            {
                return;
            }

            Transform trailer = trailers[trailers.Count - 1];
            trailers.RemoveAt(trailers.Count - 1);
            VehicleTargetResult result = target.Detach(trailer);
            if (result != VehicleTargetResult.Accepted)
            {
                Debug.LogWarning($"Detach: {result}", this);
            }

            Destroy(trailer.gameObject);
            trailerCount = trailers.Count;
        }

        [ContextMenu("Change Target (Transition)")]
        public void ChangeTarget()
        {
            if (secondTarget == null)
            {
                VehicleProfile secondProfile = CreateProfile(truckLength, bodyHeight * TruckRearWatchHeightFraction);
                AddViews(secondProfile, drivingPreset, presentationPreset, CreatePreset(CameraViewType.Fixed), CreatePreset(CameraViewType.Interior));
                secondTruck = CreateBody("Review Second Truck", truckLength, new Color(0.3f, 0.7f, 0.35f));
                secondTruck.SetParent(transform, false);
                secondTruck.position = truck.position + truck.right * 15f;
                secondTruck.rotation = truck.rotation;
                secondTarget = secondTruck.gameObject.AddComponent<VehicleCameraTarget>();
                secondTarget.Configure(secondTruck, secondProfile);
            }

            VehicleCameraTarget destination = secondTarget;
            if (ReferenceEquals(controller.Target, secondTarget))
            {
                destination = target;
            }

            int commandId;
            CameraCommandResult result = controller.ChangeTarget(destination, new TransitionOptions(viewTransition, viewTransitionValue, lockPlayerControl), preserveBearing, out commandId);
            Debug.Log($"ChangeTarget ({viewTransition} {viewTransitionValue}, preserve bearing {preserveBearing}): {result}, command {commandId}", this);
        }

        [ContextMenu("Shift Origin (-1000, 0, 0)")]
        public void ShiftOrigin()
        {
            Vector3 offset = new Vector3(-1000f, 0f, 0f);
            transform.position += offset;
            if (target != null)
            {
                target.ShiftOrigin(offset);
            }

            if (secondTarget != null)
            {
                secondTarget.ShiftOrigin(offset);
            }

            Debug.Log($"ShiftOrigin {offset}: world moved, camera position {controller.AssignedCamera.transform.position}", this);
        }

        [ContextMenu("Place Collision Wall (S28)")]
        public void PlaceCollisionWall()
        {
            if (truck == null || controller == null || !controller.IsActive)
            {
                Debug.LogWarning("Activate the camera before placing the collision wall.", this);
                return;
            }

            Vector3 cameraLocalPosition = truck.InverseTransformPoint(controller.AssignedCamera.transform.position);
            float lateral = Mathf.Abs(cameraLocalPosition.x);
            if (lateral < truckHalfWidth)
            {
                Debug.LogWarning("Orbit the camera to the side of the truck before placing the collision wall.", this);
                return;
            }

            if (collisionWall == null)
            {
                GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.name = "Review Collision Wall";
                wall.transform.SetParent(transform, true);
                wall.GetComponent<Renderer>().material.color = new Color(0.55f, 0.55f, 0.6f);
                collisionWall = wall.transform;
            }

            float side = Mathf.Sign(cameraLocalPosition.x);
            string sideName = "right";
            if (side < 0f)
            {
                sideName = "left";
            }

            float innerFace = lateral - collisionWallInset;
            float centreZ = truckLength * 0.5f + CollisionWallAhead + CollisionWallLength * 0.5f;
            Quaternion yaw = Quaternion.Euler(0f, truck.eulerAngles.y, 0f);
            Vector3 localCentre = new Vector3(side * (innerFace + CollisionWallThickness * 0.5f), CollisionWallHeight * 0.5f, centreZ);
            collisionWall.SetPositionAndRotation(truck.position + yaw * localCentre, yaw);
            collisionWall.localScale = new Vector3(CollisionWallThickness, CollisionWallHeight, CollisionWallLength);
            Physics.SyncTransforms();
            Debug.Log($"Collision wall placed {CollisionWallAhead:F0} m ahead of the truck on the {sideName} side: inner face {collisionWallInset:F2} m inside the camera path, {innerFace:F2} m from the truck centre line.", this);
        }

        [ContextMenu("Place Tunnel (S29)")]
        public void PlaceTunnel()
        {
            if (truck == null)
            {
                Debug.LogWarning("Enter Play Mode before placing the tunnel.", this);
                return;
            }

            if (tunnel == null)
            {
                tunnel = new GameObject("Review Tunnel").transform;
                tunnel.SetParent(transform, true);
                CreateTunnelPart("Review Tunnel Ceiling");
                CreateTunnelPart("Review Tunnel Left Wall");
                CreateTunnelPart("Review Tunnel Right Wall");
            }

            float innerHalfWidth = truckHalfWidth + TunnelSideClearance;
            float ceiling = bodyHeight + TunnelRoofClearance;
            float centreZ = truckLength * 0.5f + TunnelAhead + TunnelLength * 0.5f;
            Quaternion yaw = Quaternion.Euler(0f, truck.eulerAngles.y, 0f);
            tunnel.SetPositionAndRotation(truck.position + yaw * new Vector3(0f, 0f, centreZ), yaw);
            SetTunnelPart(TunnelCeilingIndex, new Vector3(0f, ceiling + TunnelCeilingThickness * 0.5f, 0f), new Vector3(2f * (innerHalfWidth + TunnelWallThickness), TunnelCeilingThickness, TunnelLength));
            SetTunnelPart(TunnelLeftWallIndex, new Vector3(-(innerHalfWidth + TunnelWallThickness * 0.5f), ceiling * 0.5f, 0f), new Vector3(TunnelWallThickness, ceiling, TunnelLength));
            SetTunnelPart(TunnelRightWallIndex, new Vector3(innerHalfWidth + TunnelWallThickness * 0.5f, ceiling * 0.5f, 0f), new Vector3(TunnelWallThickness, ceiling, TunnelLength));
            Physics.SyncTransforms();
            Debug.Log($"Tunnel placed {TunnelAhead:F0} m ahead of the truck: {TunnelLength:F0} m long, inner half-width {innerHalfWidth:F2} m, ceiling {ceiling:F2} m (roof + {TunnelRoofClearance:F2} m).", this);
        }

        [ContextMenu("Log Orbit Diagnostics")]
        public void LogOrbitDiagnostics()
        {
            if (controller == null || controller.ActiveView == null || target == null)
            {
                WriteDebugLine("Orbit diagnostics: camera not active.");
                FlushDebugLog();
                return;
            }

            VehicleOrbit rootOrbit;
            if (!truckProfile.TryGetOrbit(controller.ActiveView.OrbitId, out rootOrbit))
            {
                WriteDebugLine($"Orbit diagnostics: view '{controller.ActiveView.Name}' has no orbit.");
                FlushDebugLog();
                return;
            }

            WriteDebugLine($"=== Orbit diagnostics at frame {Time.frameCount}: view '{controller.ActiveView.Name}', orbit '{rootOrbit.Name}', bodies {target.BodyCount}, speed {speed:F2}, steering {steering:F2}, roll {truckRoll:F1}");
            for (int bodyIndex = 0; bodyIndex < target.BodyCount; bodyIndex++)
            {
                VehicleOrbit bodyOrbit;
                if (!target.GetProfile(bodyIndex).TryGetOrbit(rootOrbit.Name, out bodyOrbit))
                {
                    bodyOrbit = target.GetProfile(bodyIndex).PrimaryOrbit;
                }

                ClosedBezierOrbit closedOrbit = new ClosedBezierOrbit(bodyOrbit);
                WriteDebugLine($"  Body {bodyIndex} '{target.GetBody(bodyIndex).name}': zoom range {bodyOrbit.MinimumZoomOffset:F2}..{bodyOrbit.MaximumZoomOffset:F2}, height range {bodyOrbit.MinimumHeightOffset:F2}..{bodyOrbit.MaximumHeightOffset:F2}, offset validation {closedOrbit.OffsetRangeValidationResult}, max safe inward zoom {closedOrbit.MaximumSafeInwardZoom:F3}");
                for (int knotIndex = 0; knotIndex < bodyOrbit.Knots.Count; knotIndex++)
                {
                    BezierOrbitKnot knot = bodyOrbit.Knots[knotIndex];
                    Vector3 incoming = knot.Anchor - knot.IncomingControlPoint;
                    Vector3 outgoing = knot.OutgoingControlPoint - knot.Anchor;
                    WriteDebugLine($"    Knot {knotIndex} anchor {knot.Anchor:F3}: tangent turn {Vector3.Angle(incoming, outgoing):F1} degrees");
                }
            }

            ChainOrbit orbit = GetDebugOrbit();
            if (orbit != null)
            {
                WriteDebugLine($"  Assembled orbit length {orbit.Length:F3}, segments {orbit.Segments.Count}, bearing centre {orbit.Bearing.Centre:F3}");
                for (int segmentIndex = 0; segmentIndex < orbit.Segments.Count; segmentIndex++)
                {
                    AssembledBezierOrbitSegment segment = orbit.Segments[segmentIndex];
                    WriteDebugLine($"    Segment {segmentIndex}: {segment.EvaluatePosition(0f):F3} -> {segment.EvaluatePosition(1f):F3}");
                }
            }

            LivePose pose = controller.CurrentPose;
            WriteDebugLine($"  Pose: orbit distance {pose.OrbitDistance:F3}, zoom {pose.ZoomOffset:F3}, height {pose.HeightOffset:F3}");
            WriteDebugLine("  Columns: frame | unscaled dt of the frame that moved the camera | intents h/v/z | orbit distance | wrapped | zoom | height | camera step (truck space) | expected step along the zoomed path | normal turn deg | distance from expected pose | inward normal | curve point | camera local | flags");
            WriteDebugLine($"  Flags: JUMP = camera step exceeds the expected path step by more than {jumpTolerance:F3} m; OFF POSE = camera more than {OffPoseTolerance:F3} m from the pose point (expected during transitions); LONG FRAME = the frame was longer than the {controller.MaximumFrameDeltaTime:F3} s cap, so the camera used the capped step");
            FlushDebugLog();
        }

        public void LogViewChanged(int viewId)
        {
            Debug.Log($"View changed: {viewId}", this);
        }

        public void LogCommandEnded(int commandId, CommandEndResult result)
        {
            lastEndedCommandId = commandId;
            lastEndedResult = result;
            LivePose pose = controller.CurrentPose;
            Debug.Log($"Command {commandId} ended: {result}; orbit distance {pose.OrbitDistance:F4}", this);
        }

        public void LogNoClearPoseChanged(bool hasNoClearPose)
        {
            Debug.Log($"NoClearPoseChanged({hasNoClearPose}) at frame {Time.frameCount}", this);
        }

        private IEnumerator RunLockReviewSequence()
        {
            isLockReviewRunning = true;
            reviewPassCount = 0;
            reviewCheckCount = 0;
            speed = 0f;
            steering = 0f;
            horizontalIntent = 0f;
            heightIntent = 0f;
            zoomIntent = 0f;
            trailerCount = 0;
            yield return null;
            viewName = PresentationName;
            if (!controller.IsActive)
            {
                ActivateCamera();
            }
            else
            {
                SelectView();
            }

            yield return new WaitForSeconds(0.5f);
            Debug.Log("Lock review step 1: locked travel to 'engine', Horizontal Intent set to 1 while it travels.", this);
            int commandId;
            CameraCommandResult result = controller.RequestPoint("engine", new TravelRequest(TravelDirection.Shortest, true), out commandId);
            ReportReviewCheck("locked request accepted", result == CameraCommandResult.Accepted, result.ToString());
            yield return new WaitForSeconds(0.2f);
            ReportReviewCheck("player control locked during the travel", controller.IsPlayerControlLocked, string.Empty);
            horizontalIntent = 1f;
            float timeout = Time.time + ReviewCommandTimeout;
            while (lastEndedCommandId != commandId && Time.time < timeout)
            {
                yield return null;
            }

            ReportReviewCheck("locked travel ended Completed", lastEndedCommandId == commandId && lastEndedResult == CommandEndResult.Completed, lastEndedResult.ToString());
            ReportReviewCheck("camera stopped exactly on 'engine'", IsOnPoint("engine"), $"orbit distance {controller.OrbitDistance:F4}");
            ReportReviewCheck("lock released after arrival", !controller.IsPlayerControlLocked, string.Empty);

            Debug.Log("Lock review step 2: Horizontal Intent stays at 1 for 1 s after arrival; the stale input must not move the camera.", this);
            float distanceAfterArrival = controller.OrbitDistance;
            yield return new WaitForSeconds(1f);
            ReportReviewCheck("stale held input ignored", Mathf.Abs(controller.OrbitDistance - distanceAfterArrival) < ReviewPoseTolerance, $"moved {Mathf.Abs(controller.OrbitDistance - distanceAfterArrival):F4} m");
            horizontalIntent = 0f;
            yield return new WaitForSeconds(0.2f);
            horizontalIntent = 1f;
            float distanceBeforeRearm = controller.OrbitDistance;
            yield return new WaitForSeconds(0.5f);
            ReportReviewCheck("input works again after returning to neutral", Mathf.Abs(controller.OrbitDistance - distanceBeforeRearm) > ReviewMovementThreshold, $"moved {Mathf.Abs(controller.OrbitDistance - distanceBeforeRearm):F4} m");
            horizontalIntent = 0f;
            yield return new WaitForSeconds(0.3f);

            Debug.Log("Lock review step 3: unlocked travel to 'plate', Horizontal Intent set to 1 while it travels.", this);
            result = controller.RequestPoint("plate", new TravelRequest(TravelDirection.Shortest, false), out commandId);
            ReportReviewCheck("unlocked request accepted", result == CameraCommandResult.Accepted, result.ToString());
            yield return new WaitForSeconds(0.2f);
            ReportReviewCheck("travel still running before the input", lastEndedCommandId != commandId, string.Empty);
            horizontalIntent = 1f;
            yield return null;
            yield return null;
            ReportReviewCheck("travel ended Interrupted", lastEndedCommandId == commandId && lastEndedResult == CommandEndResult.Interrupted, lastEndedResult.ToString());
            float distanceAfterInterrupt = controller.OrbitDistance;
            yield return new WaitForSeconds(0.5f);
            ReportReviewCheck("camera orbits under the input after the interruption", Mathf.Abs(controller.OrbitDistance - distanceAfterInterrupt) > ReviewMovementThreshold, $"moved {Mathf.Abs(controller.OrbitDistance - distanceAfterInterrupt):F4} m");
            ReportReviewCheck("camera did not reach 'plate'", !IsOnPoint("plate"), string.Empty);
            horizontalIntent = 0f;

            string summary = $"Lock review finished: {reviewPassCount}/{reviewCheckCount} checks passed.";
            if (reviewPassCount == reviewCheckCount)
            {
                Debug.Log(summary, this);
            }
            else
            {
                Debug.LogError(summary, this);
            }

            isLockReviewRunning = false;
        }

        private void ReportReviewCheck(string check, bool passed, string detail)
        {
            reviewCheckCount++;
            if (passed)
            {
                reviewPassCount++;
                Debug.Log($"Lock review PASS: {check}. {detail}", this);
                return;
            }

            Debug.LogError($"Lock review FAIL: {check}. {detail}", this);
        }

        private bool IsOnPoint(string markerName)
        {
            ChainOrbit orbit = GetDebugOrbit();
            if (orbit == null)
            {
                return false;
            }

            IReadOnlyList<MergedMarker> markers = orbit.MergedMarkers;
            for (int index = 0; index < markers.Count; index++)
            {
                if (markers[index].Name != markerName)
                {
                    continue;
                }

                float gap = Mathf.Repeat(controller.OrbitDistance - markers[index].OrbitDistance, orbit.Length);
                return gap < ReviewPoseTolerance || orbit.Length - gap < ReviewPoseTolerance;
            }

            return false;
        }

        private void UpdateCushionReviewVisuals(float deltaTime)
        {
            if (!isCushionReviewRunning)
            {
                return;
            }

            cushionReviewElapsed += deltaTime;
            float height = 0f;
            if (cushionReviewElapsed >= CushionBumpStart && cushionReviewElapsed <= CushionBumpStart + CushionBumpDuration)
            {
                float progress = (cushionReviewElapsed - CushionBumpStart) / CushionBumpDuration;
                height += CushionBumpHeight * 0.5f * (1f - Mathf.Cos(2f * Mathf.PI * progress));
            }

            if (cushionReviewElapsed > CushionHillStart)
            {
                float hillTime = cushionReviewElapsed - CushionHillStart;
                float entryTime = Mathf.Min(hillTime, CushionHillEntryDuration);
                float climbSpeed = CushionReviewSpeed * 0.05f;
                height += climbSpeed * 0.5f * (entryTime - CushionHillEntryDuration / Mathf.PI * Mathf.Sin(Mathf.PI * entryTime / CushionHillEntryDuration));
                if (hillTime > CushionHillEntryDuration)
                {
                    height += climbSpeed * (hillTime - CushionHillEntryDuration);
                }
            }

            Vector3 position = truck.position;
            position.y = cushionReviewStartHeight + height;
            truck.position = position;
            if (cushionReviewElapsed >= CushionReviewDuration)
            {
                speed = 0f;
                isCushionReviewRunning = false;
            }
        }

        private IEnumerator MeasureCushionReviewSequence()
        {
            WaitForEndOfFrame frameEnd = new WaitForEndOfFrame();
            while (isCushionReviewRunning)
            {
                yield return frameEnd;
                float eyeHeight = truck.InverseTransformPoint(controller.AssignedCamera.transform.position).y;
                float displacement = Mathf.Abs(eyeHeight - truckProfile.Seat.EyeLocalPosition.y);
                if (cushionReviewElapsed >= CushionBumpStart && cushionReviewElapsed < CushionHillStart)
                {
                    cushionBumpPeak = Mathf.Max(cushionBumpPeak, displacement);
                }

                if (cushionReviewElapsed >= CushionHillStart + CushionHillEntryDuration + 1f)
                {
                    cushionSlopePeak = Mathf.Max(cushionSlopePeak, displacement);
                }
            }

            Debug.Log($"Cushion review finished at intensity {cushionIntensity:F1}: bump peak {cushionBumpPeak * 1000f:F1} mm, steady climb peak {cushionSlopePeak * 1000f:F1} mm.", this);
        }

        private IEnumerator RunSpeedRampSequence()
        {
            isSpeedRampRunning = true;
            steering = 0f;
            float startFieldOfView = controller.AssignedCamera.fieldOfView;
            float totalSeconds = 2f * SpeedRampChangeSeconds + SpeedRampHoldSeconds;
            float elapsed = 0f;
            float nextLogTime = 0f;
            Debug.Log($"Speed ramp: 0 to {SpeedRampPeakSpeed:F0} m/s over {SpeedRampChangeSeconds:F0} s, hold {SpeedRampHoldSeconds:F0} s, back to 0 over {SpeedRampChangeSeconds:F0} s.", this);
            while (elapsed < totalSeconds)
            {
                speed = GetSpeedRampSpeed(elapsed);
                if (elapsed >= nextLogTime)
                {
                    LogSpeedRampSample(elapsed, startFieldOfView);
                    nextLogTime += SpeedRampLogInterval;
                }

                yield return null;
                elapsed += Time.deltaTime;
            }

            speed = 0f;
            yield return new WaitForSeconds(1f);
            LogSpeedRampSample(elapsed + 1f, startFieldOfView);
            Debug.Log("Speed ramp finished.", this);
            isSpeedRampRunning = false;
        }

        private float GetSpeedRampSpeed(float elapsed)
        {
            if (elapsed < SpeedRampChangeSeconds)
            {
                return SpeedRampPeakSpeed * elapsed / SpeedRampChangeSeconds;
            }

            float slowDownStart = SpeedRampChangeSeconds + SpeedRampHoldSeconds;
            if (elapsed < slowDownStart)
            {
                return SpeedRampPeakSpeed;
            }

            return Mathf.Max(0f, SpeedRampPeakSpeed * (1f - (elapsed - slowDownStart) / SpeedRampChangeSeconds));
        }

        private void LogSpeedRampSample(float elapsed, float startFieldOfView)
        {
            Camera reviewCamera = controller.AssignedCamera;
            Vector3 cameraLocalPosition = truck.InverseTransformPoint(reviewCamera.transform.position);
            Debug.Log($"Speed ramp {elapsed:F1} s: speed {speed:F1} m/s; camera {-cameraLocalPosition.z:F2} m behind the truck origin, {cameraLocalPosition.y:F2} m up, {cameraLocalPosition.x:F2} m sideways; field of view {reviewCamera.fieldOfView:F2} (start {startFieldOfView:F2}).", this);
        }

        private CameraViewPreset CreatePreset(CameraViewType viewType)
        {
            CameraViewPreset preset = ScriptableObject.CreateInstance<CameraViewPreset>();
            preset.Configure(viewType);
            createdAssets.Add(preset);
            return preset;
        }

        private VehicleProfile CreateProfile(float length, float rearWatchHeight)
        {
            VehicleProfile profile = ScriptableObject.CreateInstance<VehicleProfile>();
            createdAssets.Add(profile);
            profile.EnsureProfileId();
            float half = length * 0.5f;
            float margin = 1f;
            float halfWidth = truckHalfWidth + margin;
            float front = half + margin;
            float rear = half + margin;
            VehicleOrbit drivingOrbit = profile.AddOrbit(DrivingName);
            ConfigureOrbit(drivingOrbit, halfWidth, front, rear);
            VehicleOrbit presentationOrbit = profile.AddOrbit(PresentationName);
            ConfigureOrbit(presentationOrbit, halfWidth, front, rear);
            float radius = GetCornerRadius(halfWidth);
            float edge = 2f * halfWidth - 2f * radius;
            float side = front + rear - 2f * radius;
            float arc = Mathf.PI * radius * 0.5f;
            float perimeter = 2f * (edge + side) + 4f * arc;
            profile.AddWatchMarker(drivingOrbit, "Rear", (arc + edge * 0.5f) / perimeter, new Vector3(0f, rearWatchHeight, half + 10f), false);
            profile.AddWatchMarker(drivingOrbit, "Left", (2f * arc + edge + side * 0.5f) / perimeter, new Vector3(0f, bodyHeight * 0.5f, half), false);
            profile.AddWatchMarker(drivingOrbit, "Front", (3f * arc + edge * 1.5f + side) / perimeter, new Vector3(0f, bodyHeight * 0.5f, 0f), false);
            profile.AddWatchMarker(drivingOrbit, "Right", (4f * arc + edge * 2f + side * 1.5f) / perimeter, new Vector3(0f, bodyHeight * 0.5f, half), false);
            profile.AddWatchMarker(presentationOrbit, "Centre", (4f * arc + edge * 2f + side * 1.5f) / perimeter, new Vector3(0f, bodyHeight * 0.5f, 0f), false);
            profile.AddWatchMarker(presentationOrbit, "wheel", (2f * arc + edge + half - 1.5f + rear - radius) / perimeter, new Vector3(-truckHalfWidth, 0.5f, half - 1.5f), true);
            profile.AddWatchMarker(presentationOrbit, "plate", (arc + edge * 0.5f) / perimeter, new Vector3(0f, bodyHeight * 0.3f, -half), true);
            profile.AddWatchMarker(presentationOrbit, "engine", (3f * arc + edge * 1.5f + side) / perimeter, new Vector3(0f, bodyHeight * 0.5f, half), true);
            VehicleConnectorAnchors anchors = new VehicleConnectorAnchors();
            anchors.Configure(new Vector3(0f, 0f, front + HitchGap), new Vector3(0f, 0f, -rear - HitchGap));
            profile.ConfigureConnectorAnchors(anchors);
            profile.FixedViewPose.Configure(new Vector3(0f, bodyHeight * 0.5f + 4f, -6f), new Vector3(0f, bodyHeight * 0.5f, 0f));
            profile.Seat.Configure(new Vector3(-truckHalfWidth * 0.4f, bodyHeight * 0.75f, half - length * 0.25f), new Vector3(0.06f, 0.04f, 0.08f));
            return profile;
        }

        private void ConfigureOrbit(VehicleOrbit orbit, float halfWidth, float front, float rear)
        {
            float radius = GetCornerRadius(halfWidth);
            Vector3[] corners =
            {
                new Vector3(halfWidth, 0f, -rear),
                new Vector3(-halfWidth, 0f, -rear),
                new Vector3(-halfWidth, 0f, front),
                new Vector3(halfWidth, 0f, front)
            };
            Vector3[] anchors =
            {
                new Vector3(halfWidth, 0f, -rear + radius),
                new Vector3(halfWidth - radius, 0f, -rear),
                new Vector3(-halfWidth + radius, 0f, -rear),
                new Vector3(-halfWidth, 0f, -rear + radius),
                new Vector3(-halfWidth, 0f, front - radius),
                new Vector3(-halfWidth + radius, 0f, front),
                new Vector3(halfWidth - radius, 0f, front),
                new Vector3(halfWidth, 0f, front - radius)
            };
            List<BezierOrbitKnot> knots = new List<BezierOrbitKnot>();
            for (int index = 0; index < OrbitKnotCount; index++)
            {
                Vector3 anchor = anchors[index];
                Vector3 corner = corners[index / 2];
                Vector3 previous = anchors[(index + OrbitKnotCount - 1) % OrbitKnotCount];
                Vector3 next = anchors[(index + 1) % OrbitKnotCount];
                if (index % 2 == 0)
                {
                    knots.Add(CreateKnot(anchor, anchor + (previous - anchor) / 3f, anchor + (corner - anchor) * ArcHandleScale));
                }
                else
                {
                    knots.Add(CreateKnot(anchor, anchor + (corner - anchor) * ArcHandleScale, anchor + (next - anchor) / 3f));
                }
            }

            orbit.Configure(knots, Quaternion.identity);
            orbit.ConfigureOffsetRanges(0f, bodyHeight + 4f, -4f, MaximumInwardZoom);
            float edge = 2f * halfWidth - 2f * radius;
            float side = front + rear - 2f * radius;
            float arc = Mathf.PI * radius * 0.5f;
            float perimeter = 2f * (edge + side) + 4f * arc;
            OrbitRemovableSection frontSection = new OrbitRemovableSection();
            frontSection.Configure((2f * arc + edge + side) / perimeter, (4f * arc + 2f * edge + side) / perimeter);
            OrbitRemovableSection rearSection = new OrbitRemovableSection();
            rearSection.Configure(0f, (2f * arc + edge) / perimeter);
            orbit.ConfigureRemovableSections(frontSection, rearSection);
        }

        private float GetCornerRadius(float halfWidth)
        {
            return Mathf.Min(MaximumCornerRadius, halfWidth * 0.5f);
        }

        private BezierOrbitKnot CreateKnot(Vector3 anchor, Vector3 incomingControlPoint, Vector3 outgoingControlPoint)
        {
            BezierOrbitKnot knot = new BezierOrbitKnot();
            knot.Configure(anchor, incomingControlPoint, outgoingControlPoint);
            return knot;
        }

        private void AddViews(VehicleProfile profile, CameraViewPreset drivingPreset, CameraViewPreset presentationPreset, CameraViewPreset fixedPreset, CameraViewPreset interiorPreset)
        {
            VehicleOrbit drivingOrbit;
            VehicleOrbit presentationOrbit;
            profile.TryGetOrbit(DrivingName, out drivingOrbit);
            profile.TryGetOrbit(PresentationName, out presentationOrbit);
            VehicleViewEntry driving = profile.AddView(DrivingName, drivingPreset, drivingOrbit.Id);
            driving.ConfigureDefaultPose(new OrbitPose(0f, 0f, bodyHeight + 2f));
            driving.ConfigureFrontDefaultPose(new OrbitPose(180f, -1f, bodyHeight + 1f));
            VehicleViewEntry presentation = profile.AddView(PresentationName, presentationPreset, presentationOrbit.Id);
            presentation.ConfigureDefaultPose(new OrbitPose(35f, 0f, 1.5f));
            profile.AddView(FixedName, fixedPreset, drivingOrbit.Id);
            profile.AddView(InteriorName, interiorPreset, drivingOrbit.Id);
        }

        private Transform CreateBody(string bodyName, float length, Color color)
        {
            GameObject body = new GameObject(bodyName);
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = bodyName + " Visual";
            visual.transform.SetParent(body.transform, false);
            visual.transform.localPosition = new Vector3(0f, bodyHeight * 0.5f, 0f);
            visual.transform.localScale = new Vector3(truckHalfWidth * 2f, bodyHeight, length);
            visual.GetComponent<Renderer>().material.color = color;
            if (!bodyColliders)
            {
                Destroy(visual.GetComponent<Collider>());
            }

            return body.transform;
        }

        private void CreateTunnelPart(string partName)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = partName;
            part.transform.SetParent(tunnel, false);
            part.GetComponent<Renderer>().material.color = new Color(0.45f, 0.4f, 0.35f);
        }

        private void SetTunnelPart(int index, Vector3 localCentre, Vector3 size)
        {
            Transform part = tunnel.GetChild(index);
            part.localPosition = localCentre;
            part.localRotation = Quaternion.identity;
            part.localScale = size;
        }

        private void CreateGround()
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Review Ground";
            ground.transform.SetParent(transform, false);
            ground.transform.localScale = new Vector3(100f, 1f, 100f);
            ground.GetComponent<Renderer>().material.color = new Color(0.3f, 0.32f, 0.3f);
        }

        private void UpdateImageRollFollow()
        {
            if (presentationPreset.OrbitMovement.ImageRollFollow == imageRollFollow)
            {
                return;
            }

            drivingPreset.ConfigureImageRollFollow(imageRollFollow);
            presentationPreset.ConfigureImageRollFollow(imageRollFollow);
        }

        private void UpdatePresentationAngleLimits()
        {
            OrbitMovementSettings settings = presentationPreset.OrbitMovement;
            if (settings.AngleLimitsEnabled == presentationAngleLimits && settings.MaximumBearing == presentationAngleLimit)
            {
                return;
            }

            presentationPreset.ConfigureAngleLimits(presentationAngleLimits, -presentationAngleLimit, presentationAngleLimit);
        }

        private void UpdateTrailerPoses()
        {
            for (int index = 0; index < trailers.Count; index++)
            {
                Transform front;
                VehicleProfile frontProfile;
                if (index == 0)
                {
                    front = truck;
                    frontProfile = truckProfile;
                }
                else
                {
                    front = trailers[index - 1];
                    frontProfile = trailerProfile;
                }

                Transform trailer = trailers[index];
                Vector3 hitch = front.TransformPoint(frontProfile.ConnectorAnchors.RearLocalPosition);
                Vector3 towDirection = Vector3.ProjectOnPlane(hitch - trailer.position, front.up);
                if (towDirection.sqrMagnitude < MinimumHitchDistance)
                {
                    continue;
                }

                trailer.rotation = Quaternion.LookRotation(towDirection, front.up);
                trailer.position = hitch - trailer.rotation * trailerProfile.ConnectorAnchors.FrontLocalPosition;
            }
        }

        private void UpdateTrailerCount()
        {
            while (trailers.Count < trailerCount)
            {
                int before = trailers.Count;
                AttachTrailer();
                if (trailers.Count == before)
                {
                    trailerCount = before;
                    return;
                }
            }

            while (trailers.Count > trailerCount)
            {
                DetachLastTrailer();
            }
        }

        private void UpdateSmoothActivationRequest()
        {
            if (!isSmoothActivationPending)
            {
                return;
            }

            if (Time.frameCount <= smoothActivationRequestFrame + 1)
            {
                Debug.Log($"Smooth activation waits one frame; this frame's unscaled delta is {Time.unscaledDeltaTime:F3} s.", this);
                return;
            }

            isSmoothActivationPending = false;
            if (controller.IsActive)
            {
                return;
            }

            controller.Configure(controller.AssignedCamera, target, viewName);
            int commandId;
            CameraCommandResult result = controller.Activate(new TransitionOptions(TransitionMode.Duration, 2f), out commandId);
            Debug.Log($"Smooth activation: {result}, command {commandId}", this);
        }

        private void LogCameraMotion()
        {
            Camera camera = controller.AssignedCamera;
            if (!logCameraMotion || !controller.IsActive || camera == null || controller.ActiveView == null)
            {
                hasMotionSample = false;
                return;
            }

            ChainOrbit orbit = GetDebugOrbit();
            if (orbit == null)
            {
                hasMotionSample = false;
                return;
            }

            LivePose pose = controller.CurrentPose;
            Vector3 cameraLocalPosition = truck.InverseTransformPoint(camera.transform.position);
            Vector3 inwardNormal = orbit.EvaluateRootLocalInwardNormal(pose.OrbitDistance);
            if (!hasMotionSample)
            {
                LogOrbitDiagnostics();
            }
            else
            {
                float cameraStep = Vector3.Distance(cameraLocalPosition, lastCameraLocalPosition);
                float pathStep = MeasureOffsetPathLength(orbit, lastOrbitDistance, pose.OrbitDistance, pose.ZoomOffset) + Mathf.Abs(pose.ZoomOffset - lastZoom) + Mathf.Abs(pose.HeightOffset - lastHeight);
                Vector3 curvePoint = orbit.EvaluateRootLocalPosition(pose.OrbitDistance);
                Vector3 expectedLocalPosition = curvePoint + inwardNormal * pose.ZoomOffset + Vector3.up * pose.HeightOffset;
                float offPose = Vector3.Distance(cameraLocalPosition, expectedLocalPosition);
                if (cameraStep > MinimumLoggedStep || pathStep > MinimumLoggedStep)
                {
                    string flags = string.Empty;
                    if (cameraStep > pathStep + jumpTolerance)
                    {
                        jumpCount++;
                        flags += $" | JUMP {jumpCount}";
                    }

                    if (offPose > OffPoseTolerance)
                    {
                        offPoseCount++;
                        flags += " | OFF POSE";
                    }

                    if (lastUnscaledDeltaTime > controller.MaximumFrameDeltaTime + LongFrameTolerance)
                    {
                        longFrameCount++;
                        flags += " | LONG FRAME";
                    }

                    WriteDebugLine($"{Time.frameCount} | {lastUnscaledDeltaTime:F4} | {horizontalIntent:F2}/{heightIntent:F2}/{zoomIntent:F2} | {pose.OrbitDistance:F3} | {Mathf.Repeat(pose.OrbitDistance, orbit.Length):F3} | {pose.ZoomOffset:F3} | {pose.HeightOffset:F3} | {cameraStep:F4} | {pathStep:F4} | {Vector3.Angle(lastInwardNormal, inwardNormal):F2} | {offPose:F4} | {inwardNormal:F3} | {curvePoint:F3} | {cameraLocalPosition:F3}{flags}");
                }
            }

            lastCameraLocalPosition = cameraLocalPosition;
            lastInwardNormal = inwardNormal;
            lastOrbitDistance = pose.OrbitDistance;
            lastZoom = pose.ZoomOffset;
            lastHeight = pose.HeightOffset;
            lastUnscaledDeltaTime = Time.unscaledDeltaTime;
            hasMotionSample = true;
        }

        private float MeasureOffsetPathLength(ChainOrbit orbit, float fromDistance, float toDistance, float zoom)
        {
            int samples = Mathf.Clamp(Mathf.CeilToInt(Mathf.Abs(toDistance - fromDistance) / PathSampleSpacing), 1, MaximumPathSamples);
            float length = 0f;
            Vector3 previous = EvaluateOffsetPoint(orbit, fromDistance, zoom);
            for (int sample = 1; sample <= samples; sample++)
            {
                Vector3 current = EvaluateOffsetPoint(orbit, Mathf.Lerp(fromDistance, toDistance, (float)sample / samples), zoom);
                length += Vector3.Distance(previous, current);
                previous = current;
            }

            return length;
        }

        private Vector3 EvaluateOffsetPoint(ChainOrbit orbit, float distance, float zoom)
        {
            return orbit.EvaluateRootLocalPosition(distance) + orbit.EvaluateRootLocalInwardNormal(distance) * zoom;
        }

        private ChainOrbit GetDebugOrbit()
        {
            VehicleViewEntry view = controller.ActiveView;
            VehicleOrbit rootOrbit;
            if (view == null || !truckProfile.TryGetOrbit(view.OrbitId, out rootOrbit))
            {
                return null;
            }

            if (debugOrbit == null || debugOrbitChainVersion != target.ChainVersion || debugOrbitViewId != view.Id)
            {
                debugOrbit = new ChainOrbitBuilder().Build(target, rootOrbit.Name);
                debugOrbitChainVersion = target.ChainVersion;
                debugOrbitViewId = view.Id;
                hasMotionSample = false;
            }

            return debugOrbit;
        }

        private void WriteDebugLine(string line)
        {
            if (loggedLineCount >= MaximumLoggedLines)
            {
                return;
            }

            debugBuffer.AppendLine(line);
            bufferedLineCount++;
            loggedLineCount++;
            if (loggedLineCount == MaximumLoggedLines)
            {
                debugBuffer.AppendLine("Log line limit reached; further lines are dropped.");
            }

            if (bufferedLineCount >= FlushLineCount)
            {
                FlushDebugLog();
            }
        }

        private void FlushDebugLog()
        {
            if (string.IsNullOrEmpty(debugFilePath) || debugBuffer.Length == 0)
            {
                return;
            }

            File.AppendAllText(debugFilePath, debugBuffer.ToString());
            debugBuffer.Clear();
            bufferedLineCount = 0;
        }

        private Transform GetLastBody()
        {
            if (trailers.Count == 0)
            {
                return truck;
            }

            return trailers[trailers.Count - 1];
        }

        private VehicleProfile GetLastProfile()
        {
            if (trailers.Count == 0)
            {
                return truckProfile;
            }

            return trailerProfile;
        }

        private void OnDestroy()
        {
            if (controller != null)
            {
                controller.CommandEnded -= LogCommandEnded;
            }

            if (eventForwarder != null)
            {
                eventForwarder.NoClearPoseChanged.RemoveListener(LogNoClearPoseChanged);
            }

            WriteDebugLine($"Session ended; jumps: {jumpCount}, off pose: {offPoseCount}, long frames (capped): {longFrameCount}");
            FlushDebugLog();
            for (int index = 0; index < createdAssets.Count; index++)
            {
                Destroy(createdAssets[index]);
            }

            createdAssets.Clear();
        }
    }
}
