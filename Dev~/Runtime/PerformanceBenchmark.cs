using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Gley.CameraSystem.Dev
{
    public class PerformanceBenchmark : MonoBehaviour
    {
        private const string DrivingName = "Driving";
        private const string PresentationName = "Presentation";
        private const string InteriorName = "Interior";
        private const string FixedName = "Fixed";
        private const float TunnelStart = 10f;
        private const float TunnelLength = 60f;
        private const float TunnelHalfWidth = 3.5f;
        private const float TunnelHeight = 7f;
        private const float TunnelRoofBottom = 6f;
        private const float WallThickness = 0.4f;
        private const float SecondVehicleSideOffset = 10f;
        private const int MarkerCapacityMargin = 600;
        private const int ProfileRunCount = 30;
        private const int ProfileStepCount = 9;

        private readonly List<BenchmarkOperation> operations = new List<BenchmarkOperation>(64);
        private readonly List<Collider> obstacles = new List<Collider>();
        private readonly List<Collider> warmedColliders = new List<Collider>();
        private readonly List<Transform> warmedTransforms = new List<Transform>();
        private readonly BenchmarkReportWriter reportWriter = new BenchmarkReportWriter();
        private readonly ChainOrbitBuilder profileBuilder = new ChainOrbitBuilder();
        private readonly Rect statusArea = new Rect(10f, 10f, 640f, 60f);

        [SerializeField] private Transform[] trailerBodies;
        [SerializeField] private CameraSystemController cameraController;
        [SerializeField] private VehicleCameraTarget mainTarget;
        [SerializeField] private VehicleCameraTarget secondTarget;
        [SerializeField] private VehicleProfile trailerProfile;
        private BenchmarkScript script;
        private BenchmarkRoute route;
        private BenchmarkFrameLog frameLog;
        private BenchmarkMarkerSampler markerSampler;
        private BenchmarkGarbageCounter garbageCounter;
        private BenchmarkOperationProfile operationProfile;
        private Transform mainBody;
        private Vector3 trailerOffset;
        private string status = "Vehicle camera benchmark: waiting to start.";
        private string mainReportPath;
        private string drivingOrbitName;
        [SerializeField] private int frameRate = 60;
        private long operationStart;
        private long operationEnd;
        private int operationAllocations;
        private int frame;
        private int attachedTrailers;
        private int reportedPhase = -1;
        private int savedVSyncCount;
        private int savedTargetFrameRate;
        private int profileStep;
        private int profileRun;
        private int profiledOperation = -1;
        private int profiledRun = -1;
        private bool isRunning;
        private bool isFramePacingOverridden;
        private bool isProfiling;

        public void Configure(CameraSystemController controller, VehicleCameraTarget main, VehicleCameraTarget second, Transform[] trailers, VehicleProfile profile)
        {
            cameraController = controller;
            mainTarget = main;
            secondTarget = second;
            trailerBodies = trailers;
            trailerProfile = profile;
        }

        private void Start()
        {
            if (!IsConfigured())
            {
                status = "Vehicle camera benchmark: not configured. Rebuild the scene from Tools > Gley > Vehicle Camera System > Review > Create Performance Benchmark Scene.";
                Debug.LogError(status, this);
                enabled = false;
                return;
            }

            UncapFramePacing();
            script = new BenchmarkScript(frameRate);
            route = new BenchmarkRoute(script);
            markerSampler = new BenchmarkMarkerSampler(script.TotalFrames + MarkerCapacityMargin);
            frameLog = new BenchmarkFrameLog(script.TotalFrames, markerSampler.MarkerCount);
            garbageCounter = new BenchmarkGarbageCounter();
            mainBody = mainTarget.GetBody(mainTarget.RootIndex);
            trailerOffset = mainTarget.GetProfile(mainTarget.RootIndex).ConnectorAnchors.RearLocalPosition - trailerProfile.ConnectorAnchors.FrontLocalPosition;
            for (int index = 0; index < trailerBodies.Length; index++)
            {
                trailerBodies[index].gameObject.SetActive(false);
            }

            PlaceSecondVehicle();
            CreateTunnel();
            WarmColliderWrappers();
            ApplyVehiclePose(0);
            Physics.SyncTransforms();
            cameraController.SetUpdateMode(CameraUpdateMode.Manual);
            BeginOperation();
            CameraCommandResult result = cameraController.Activate();
            StopOperation();
            RecordOperation(-1, "Activate (Driving, snap)", result.ToString());
            isRunning = true;
            Debug.Log($"Vehicle camera benchmark started: {script.TotalFrames} frames. Wait for the 'benchmark finished' message.", this);
        }

        private void LateUpdate()
        {
            if (isProfiling)
            {
                UpdateOperationProfile();
                return;
            }

            if (!isRunning)
            {
                return;
            }

            if (frame > 0)
            {
                CollectMarkers(frame - 1);
            }

            if (frame >= script.TotalFrames)
            {
                Finish();
                return;
            }

            int phase = script.GetPhaseAtFrame(frame);
            int phaseFrame = frame - script.GetStartFrame(phase);
            ApplyVehiclePose(frame);
            RunPhaseCommands((BenchmarkPhaseKind)phase, phaseFrame);
            Physics.SyncTransforms();
            garbageCounter.Begin();
            long start = Stopwatch.GetTimestamp();
            cameraController.UpdateCameraFrame(script.FrameTime);
            long end = Stopwatch.GetTimestamp();
            int allocations = garbageCounter.End();
            RecordFrame(phase, ToMilliseconds(end - start), allocations);
            if (phase != reportedPhase)
            {
                reportedPhase = phase;
                status = $"Vehicle camera benchmark running: {(BenchmarkPhaseKind)phase} ({phase + 1}/{script.PhaseCount}).";
            }

            frame++;
        }

        private bool IsConfigured()
        {
            if (cameraController == null || mainTarget == null || secondTarget == null || trailerProfile == null || trailerBodies == null || trailerBodies.Length == 0)
            {
                return false;
            }

            for (int index = 0; index < trailerBodies.Length; index++)
            {
                if (trailerBodies[index] == null)
                {
                    return false;
                }
            }

            return mainTarget.IsRootAlive && secondTarget.IsRootAlive;
        }

        private void UncapFramePacing()
        {
            savedVSyncCount = QualitySettings.vSyncCount;
            savedTargetFrameRate = Application.targetFrameRate;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            isFramePacingOverridden = true;
        }

        private void PlaceSecondVehicle()
        {
            int targetChangeFrame = script.GetStartFrame((int)BenchmarkPhaseKind.TargetChange);
            Quaternion rotation = route.GetRotation(targetChangeFrame);
            Vector3 position = route.GetPosition(targetChangeFrame) + rotation * Vector3.right * SecondVehicleSideOffset;
            secondTarget.GetBody(secondTarget.RootIndex).SetPositionAndRotation(position, rotation);
        }

        private void CreateTunnel()
        {
            int tunnelFrame = script.GetStartFrame((int)BenchmarkPhaseKind.Tunnel);
            Quaternion rotation = route.GetRotation(tunnelFrame);
            Vector3 forward = rotation * Vector3.forward;
            Vector3 right = rotation * Vector3.right;
            Vector3 centre = route.GetPosition(tunnelFrame) + forward * (TunnelStart + TunnelLength * 0.5f);
            CreateBox("Tunnel Left Wall", centre - right * TunnelHalfWidth + Vector3.up * TunnelHeight * 0.5f, rotation, new Vector3(WallThickness, TunnelHeight, TunnelLength));
            CreateBox("Tunnel Right Wall", centre + right * TunnelHalfWidth + Vector3.up * TunnelHeight * 0.5f, rotation, new Vector3(WallThickness, TunnelHeight, TunnelLength));
            CreateBox("Tunnel Roof", centre + Vector3.up * (TunnelRoofBottom + WallThickness * 0.5f), rotation, new Vector3(TunnelHalfWidth * 2f + WallThickness, WallThickness, TunnelLength));
        }

        private void CreateBox(string boxName, Vector3 position, Quaternion rotation, Vector3 size)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = boxName;
            box.transform.SetParent(transform, false);
            box.transform.SetPositionAndRotation(position, rotation);
            box.transform.localScale = size;
            obstacles.Add(box.GetComponent<Collider>());
        }

        private void WarmColliderWrappers()
        {
#if UNITY_6000_4_OR_NEWER
            Collider[] colliders = FindObjectsByType<Collider>(FindObjectsInactive.Include);
#else
            Collider[] colliders = FindObjectsByType<Collider>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#endif
            for (int index = 0; index < colliders.Length; index++)
            {
                warmedColliders.Add(colliders[index]);
                warmedTransforms.Add(colliders[index].transform);
            }
        }

        private void ApplyVehiclePose(int routeFrame)
        {
            Vector3 position = route.GetPosition(routeFrame);
            Quaternion rotation = route.GetRotation(routeFrame);
            mainBody.SetPositionAndRotation(position, rotation);
            for (int index = 0; index < attachedTrailers; index++)
            {
                PlaceTrailer(index, position, rotation);
            }
        }

        private void PlaceTrailer(int index, Vector3 truckPosition, Quaternion truckRotation)
        {
            trailerBodies[index].SetPositionAndRotation(truckPosition + truckRotation * (trailerOffset * (index + 1)), truckRotation);
        }

        private void BeginOperation()
        {
            garbageCounter.Begin();
            operationStart = Stopwatch.GetTimestamp();
        }

        private void StopOperation()
        {
            operationEnd = Stopwatch.GetTimestamp();
            operationAllocations = garbageCounter.End();
        }

        private void RecordOperation(int phase, string operationName, string result)
        {
            operations.Add(new BenchmarkOperation(frame, phase, operationName, result, ToMilliseconds(operationEnd - operationStart), operationAllocations));
        }

        private double ToMilliseconds(long ticks)
        {
            return ticks * 1000d / Stopwatch.Frequency;
        }

        private void CollectMarkers(int markerFrame)
        {
            for (int marker = 0; marker < markerSampler.MarkerCount; marker++)
            {
                long nanoseconds = markerSampler.TakeNewSample(marker);
                if (nanoseconds >= 0)
                {
                    frameLog.RecordMarker(marker, markerFrame, nanoseconds);
                }
            }
        }

        private void Finish()
        {
            isRunning = false;
            mainReportPath = reportWriter.WriteFiles(script, frameLog, markerSampler, operations, garbageCounter);
            BeginOperationProfile();
            if (isProfiling)
            {
                status = $"Vehicle camera benchmark: main run written to {mainReportPath}; profiling one-off operations ({ProfileRunCount} runs each).";
                return;
            }

            RestoreFramePacing();
            status = $"Vehicle camera benchmark finished. Results: {mainReportPath}. The one-off operation profile could not start (Driving view or trailers missing). You can stop Play Mode.";
            Debug.Log(status, this);
        }

        private void BeginOperationProfile()
        {
            VehicleProfile rootProfile = mainTarget.GetProfile(mainTarget.RootIndex);
            VehicleViewEntry drivingView;
            VehicleOrbit drivingOrbit;
            if (!cameraController.IsActive || trailerBodies.Length < 2 || !rootProfile.TryGetView(DrivingName, out drivingView) || !rootProfile.TryGetOrbit(drivingView.OrbitId, out drivingOrbit))
            {
                return;
            }

            drivingOrbitName = drivingOrbit.Name;
            operationProfile = new BenchmarkOperationProfile(System.Enum.GetValues(typeof(BenchmarkProfiledOperation)).Length, ProfileRunCount, markerSampler.MarkerCount);
            ApplyVehiclePose(script.TotalFrames - 1);
            while (attachedTrailers < trailerBodies.Length - 1)
            {
                Transform trailer = PrepareNextTrailer();
                if (mainTarget.Attach(trailer, trailerProfile, ChainEnd.Rear) != VehicleTargetResult.Accepted)
                {
                    trailer.gameObject.SetActive(false);
                    return;
                }

                attachedTrailers++;
            }

            profileStep = 0;
            profileRun = 0;
            profiledOperation = -1;
            profiledRun = -1;
            isProfiling = true;
        }

        private Transform PrepareNextTrailer()
        {
            Transform trailer = trailerBodies[attachedTrailers];
            trailer.gameObject.SetActive(true);
            PlaceTrailer(attachedTrailers, mainBody.position, mainBody.rotation);
            Physics.SyncTransforms();
            return trailer;
        }

        private void UpdateOperationProfile()
        {
            CollectProfileMarkers();
            if (profileRun >= ProfileRunCount)
            {
                FinishOperationProfile();
                return;
            }

            RunProfileStep(profileStep);
            profileStep++;
            if (profileStep >= ProfileStepCount)
            {
                profileStep = 0;
                profileRun++;
            }
        }

        private void CollectProfileMarkers()
        {
            for (int marker = 0; marker < markerSampler.MarkerCount; marker++)
            {
                long nanoseconds = markerSampler.TakeNewSample(marker);
                if (profiledOperation >= 0 && profiledRun >= 0 && nanoseconds > 0)
                {
                    operationProfile.SetMarker(profiledOperation, profiledRun, marker, nanoseconds / 1000000d);
                }
            }

            profiledOperation = -1;
            profiledRun = -1;
        }

        private void FinishOperationProfile()
        {
            isProfiling = false;
            RestoreFramePacing();
            string profilePath = reportWriter.WriteOperationProfile(operationProfile, markerSampler);
            status = $"Vehicle camera benchmark finished. Results: {mainReportPath} and {profilePath}. You can stop Play Mode.";
            Debug.Log(status, this);
        }

        private void RunProfileStep(int step)
        {
            if (step == 0)
            {
                Transform trailer = PrepareNextTrailer();
                BeginOperation();
                VehicleTargetResult result = mainTarget.Attach(trailer, trailerProfile, ChainEnd.Rear);
                StopOperation();
                if (result == VehicleTargetResult.Accepted)
                {
                    attachedTrailers++;
                }
                else
                {
                    trailer.gameObject.SetActive(false);
                }

                RecordProfiledOperation(BenchmarkProfiledOperation.AttachFifthBody, result == VehicleTargetResult.Accepted);
            }
            else if (step == 1)
            {
                ProfileSelectView(PresentationName, BenchmarkProfiledOperation.SelectViewPresentationFiveBodies);
            }
            else if (step == 2)
            {
                ProfileSelectView(DrivingName, BenchmarkProfiledOperation.SelectViewDrivingFiveBodies);
            }
            else if (step == 3)
            {
                ProfileChangeTarget(secondTarget, BenchmarkProfiledOperation.ChangeTargetToSecondVehicle);
            }
            else if (step == 4)
            {
                ProfileChangeTarget(mainTarget, BenchmarkProfiledOperation.ChangeTargetBackFiveBodies);
            }
            else if (step == 5)
            {
                cameraController.Deactivate();
            }
            else if (step == 6)
            {
                BeginOperation();
                CameraCommandResult result = cameraController.Activate();
                StopOperation();
                RecordProfiledOperation(BenchmarkProfiledOperation.ActivateFiveBodies, result == CameraCommandResult.Accepted);
            }
            else if (step == 7)
            {
                BeginOperation();
                ChainOrbit orbit = profileBuilder.Build(mainTarget, drivingOrbitName);
                StopOperation();
                RecordProfiledOperation(BenchmarkProfiledOperation.BuildDrivingOrbitFiveBodies, orbit != null && orbit.IsClosed);
            }
            else
            {
                ProfileDetach();
            }
        }

        private void RecordProfiledOperation(BenchmarkProfiledOperation profiled, bool isAccepted)
        {
            int operation = (int)profiled;
            if (!isAccepted)
            {
                operationProfile.AddRejection(operation);
                return;
            }

            profiledOperation = operation;
            profiledRun = operationProfile.AddRun(operation, ToMilliseconds(operationEnd - operationStart), operationAllocations);
        }

        private void ProfileSelectView(string viewName, BenchmarkProfiledOperation profiled)
        {
            BeginOperation();
            CameraCommandResult result = cameraController.SelectView(viewName);
            StopOperation();
            RecordProfiledOperation(profiled, result == CameraCommandResult.Accepted);
        }

        private void ProfileChangeTarget(VehicleCameraTarget newTarget, BenchmarkProfiledOperation profiled)
        {
            int commandId;
            BeginOperation();
            CameraCommandResult result = cameraController.ChangeTarget(newTarget, new TransitionOptions(TransitionMode.Snap), false, out commandId);
            StopOperation();
            RecordProfiledOperation(profiled, result == CameraCommandResult.Accepted);
        }

        private void ProfileDetach()
        {
            if (attachedTrailers == 0)
            {
                return;
            }

            Transform trailer = trailerBodies[attachedTrailers - 1];
            BeginOperation();
            VehicleTargetResult result = mainTarget.Detach(trailer);
            StopOperation();
            if (result == VehicleTargetResult.Accepted)
            {
                attachedTrailers--;
                trailer.gameObject.SetActive(false);
            }

            RecordProfiledOperation(BenchmarkProfiledOperation.DetachFifthBody, result == VehicleTargetResult.Accepted);
        }

        private void RestoreFramePacing()
        {
            if (!isFramePacingOverridden)
            {
                return;
            }

            QualitySettings.vSyncCount = savedVSyncCount;
            Application.targetFrameRate = savedTargetFrameRate;
            isFramePacingOverridden = false;
        }

        private void RunPhaseCommands(BenchmarkPhaseKind phase, int phaseFrame)
        {
            int phaseIndex = (int)phase;
            if (phase == BenchmarkPhaseKind.Reverse && phaseFrame == 0)
            {
                SetReverse(phaseIndex, true);
            }
            else if (phase == BenchmarkPhaseKind.Tunnel && phaseFrame == 0)
            {
                SetReverse(phaseIndex, false);
            }
            else if (phase == BenchmarkPhaseKind.PlayerInput)
            {
                RunPlayerInput(phaseIndex, phaseFrame);
            }
            else if (phase == BenchmarkPhaseKind.PresentationPoints)
            {
                RunPresentationPoints(phaseIndex, phaseFrame);
            }
            else if (phase == BenchmarkPhaseKind.Interior)
            {
                RunInterior(phaseIndex, phaseFrame);
            }
            else if (phase == BenchmarkPhaseKind.Fixed && phaseFrame == 0)
            {
                SelectView(phaseIndex, FixedName, "SelectView Fixed (snap)");
            }
            else if (phase == BenchmarkPhaseKind.ChainAttach)
            {
                RunChainAttach(phaseIndex, phaseFrame);
            }
            else if (phase == BenchmarkPhaseKind.ChainPresentation)
            {
                RunChainPresentation(phaseIndex, phaseFrame);
            }
            else if (phase == BenchmarkPhaseKind.ChainDetach)
            {
                RunChainDetach(phaseIndex, phaseFrame);
            }
            else if (phase == BenchmarkPhaseKind.TargetChange)
            {
                RunTargetChange(phaseIndex, phaseFrame);
            }
            else if (phase == BenchmarkPhaseKind.Reactivate)
            {
                RunReactivate(phaseIndex, phaseFrame);
            }
        }

        private void SetReverse(int phase, bool isReverse)
        {
            BeginOperation();
            CameraCommandResult result = cameraController.SetReverse(isReverse);
            StopOperation();
            if (isReverse)
            {
                RecordOperation(phase, "SetReverse(true)", result.ToString());
            }
            else
            {
                RecordOperation(phase, "SetReverse(false)", result.ToString());
            }
        }

        private void RunPlayerInput(int phase, int phaseFrame)
        {
            if (phaseFrame == 0)
            {
                SetHeldIntent(phase, 1f, 0f, 0f, "SetHeldIntent orbit");
            }
            else if (phaseFrame == script.SecondsToFrames(2f))
            {
                SetHeldIntent(phase, 0f, 1f, 1f, "SetHeldIntent height and zoom");
            }
            else if (phaseFrame == script.SecondsToFrames(3f))
            {
                SetHeldIntent(phase, 0f, 0f, 0f, "SetHeldIntent release");
            }
            else if (phaseFrame == script.SecondsToFrames(4f))
            {
                BeginOperation();
                CameraCommandResult result = cameraController.OrbitReset();
                StopOperation();
                RecordOperation(phase, "OrbitReset", result.ToString());
            }
            else if (phaseFrame == script.SecondsToFrames(5f))
            {
                BeginOperation();
                CameraCommandResult result = cameraController.FullReset();
                StopOperation();
                RecordOperation(phase, "FullReset", result.ToString());
            }

            if (phaseFrame > script.SecondsToFrames(3f) && phaseFrame < script.SecondsToFrames(4f))
            {
                cameraController.AddDrag(new Vector2(0.004f, 0.002f));
            }
        }

        private void SetHeldIntent(int phase, float horizontal, float vertical, float zoom, string operationName)
        {
            BeginOperation();
            CameraCommandResult result = cameraController.SetHeldIntent(horizontal, vertical, zoom);
            StopOperation();
            RecordOperation(phase, operationName, result.ToString());
        }

        private void RunPresentationPoints(int phase, int phaseFrame)
        {
            if (phaseFrame == 0)
            {
                BeginOperation();
                int commandId;
                CameraCommandResult result = cameraController.SelectView(PresentationName, new TransitionOptions(TransitionMode.PresetSpeed), out commandId);
                StopOperation();
                RecordOperation(phase, "SelectView Presentation (smooth)", result.ToString());
            }
            else if (phaseFrame == script.SecondsToFrames(3f) || phaseFrame == script.SecondsToFrames(6f))
            {
                RequestNextPoint(phase);
            }
            else if (phaseFrame == script.SecondsToFrames(9f))
            {
                BeginOperation();
                CameraCommandResult result = cameraController.RequestPreviousPoint();
                StopOperation();
                RecordOperation(phase, "RequestPreviousPoint", result.ToString());
            }
        }

        private void RequestNextPoint(int phase)
        {
            BeginOperation();
            CameraCommandResult result = cameraController.RequestNextPoint();
            StopOperation();
            RecordOperation(phase, "RequestNextPoint", result.ToString());
        }

        private void RunInterior(int phase, int phaseFrame)
        {
            if (phaseFrame == 0)
            {
                SelectView(phase, InteriorName, "SelectView Interior (snap)");
            }
            else if (phaseFrame == script.SecondsToFrames(4f))
            {
                SetHeldIntent(phase, 1f, 0.5f, 0f, "SetHeldIntent head look");
            }
            else if (phaseFrame == script.SecondsToFrames(6f))
            {
                SetHeldIntent(phase, 0f, 0f, 0f, "SetHeldIntent release");
            }
        }

        private void SelectView(int phase, string viewName, string operationName)
        {
            BeginOperation();
            CameraCommandResult result = cameraController.SelectView(viewName);
            StopOperation();
            RecordOperation(phase, operationName, result.ToString());
        }

        private void RunChainAttach(int phase, int phaseFrame)
        {
            if (phaseFrame == 0)
            {
                SelectView(phase, DrivingName, "SelectView Driving (snap, 1 body)");
                return;
            }

            for (int index = 1; index <= trailerBodies.Length; index++)
            {
                if (phaseFrame == script.SecondsToFrames(index))
                {
                    AttachTrailer(phase);
                }
            }
        }

        private void AttachTrailer(int phase)
        {
            if (attachedTrailers >= trailerBodies.Length)
            {
                return;
            }

            Transform trailer = trailerBodies[attachedTrailers];
            trailer.gameObject.SetActive(true);
            PlaceTrailer(attachedTrailers, mainBody.position, mainBody.rotation);
            Physics.SyncTransforms();
            BeginOperation();
            VehicleTargetResult result = mainTarget.Attach(trailer, trailerProfile, ChainEnd.Rear);
            StopOperation();
            RecordOperation(phase, $"Attach trailer ({attachedTrailers + 2} bodies)", result.ToString());
            if (result == VehicleTargetResult.Accepted)
            {
                attachedTrailers++;
            }
            else
            {
                trailer.gameObject.SetActive(false);
            }
        }

        private void RunChainPresentation(int phase, int phaseFrame)
        {
            if (phaseFrame == 0)
            {
                SelectView(phase, PresentationName, $"SelectView Presentation (snap, {attachedTrailers + 1} bodies)");
            }
            else if (phaseFrame == script.SecondsToFrames(1f) || phaseFrame == script.SecondsToFrames(3.5f))
            {
                RequestNextPoint(phase);
            }
        }

        private void RunChainDetach(int phase, int phaseFrame)
        {
            if (phaseFrame == 0)
            {
                SelectView(phase, DrivingName, $"SelectView Driving (snap, {attachedTrailers + 1} bodies)");
                return;
            }

            for (int index = 1; index <= trailerBodies.Length; index++)
            {
                if (phaseFrame == script.SecondsToFrames(index))
                {
                    DetachTrailer(phase);
                }
            }
        }

        private void DetachTrailer(int phase)
        {
            if (attachedTrailers == 0)
            {
                return;
            }

            Transform trailer = trailerBodies[attachedTrailers - 1];
            BeginOperation();
            VehicleTargetResult result = mainTarget.Detach(trailer);
            StopOperation();
            RecordOperation(phase, $"Detach trailer ({attachedTrailers} bodies left)", result.ToString());
            if (result == VehicleTargetResult.Accepted)
            {
                attachedTrailers--;
                trailer.gameObject.SetActive(false);
            }
        }

        private void RunTargetChange(int phase, int phaseFrame)
        {
            int commandId;
            if (phaseFrame == 0)
            {
                BeginOperation();
                CameraCommandResult result = cameraController.ChangeTarget(secondTarget, new TransitionOptions(TransitionMode.PresetSpeed), true, out commandId);
                StopOperation();
                RecordOperation(phase, "ChangeTarget to second vehicle (smooth, preserve bearing)", result.ToString());
            }
            else if (phaseFrame == script.SecondsToFrames(4f))
            {
                BeginOperation();
                CameraCommandResult result = cameraController.ChangeTarget(mainTarget, new TransitionOptions(TransitionMode.Snap), false, out commandId);
                StopOperation();
                RecordOperation(phase, "ChangeTarget back to main vehicle (snap)", result.ToString());
            }
        }

        private void RunReactivate(int phase, int phaseFrame)
        {
            if (phaseFrame == script.SecondsToFrames(1f))
            {
                BeginOperation();
                cameraController.Deactivate();
                StopOperation();
                RecordOperation(phase, "Deactivate", "Done");
            }
            else if (phaseFrame == script.SecondsToFrames(1.5f))
            {
                BeginOperation();
                int commandId;
                CameraCommandResult result = cameraController.Activate(new TransitionOptions(TransitionMode.Duration, 1f), out commandId);
                StopOperation();
                RecordOperation(phase, "Activate (Driving, 1 s transition)", result.ToString());
            }
        }

        private void RecordFrame(int phase, double milliseconds, int allocations)
        {
            string viewName = null;
            int castLimit = 0;
            CameraViewPreset preset = cameraController.ActivePreset;
            if (cameraController.ActiveView != null)
            {
                viewName = cameraController.ActiveView.Name;
            }

            if (preset != null)
            {
                castLimit = preset.Collision.MaximumCasts;
            }

            int bodyCount = 0;
            if (cameraController.Target != null)
            {
                bodyCount = cameraController.Target.BodyCount;
            }

            frameLog.RecordFrame(frame, phase, cameraController.IsActive, viewName, bodyCount, milliseconds, allocations, cameraController.CollisionCastsThisFrame, castLimit);
        }

        private void OnGUI()
        {
            GUI.Label(statusArea, status);
        }

        private void OnDestroy()
        {
            RestoreFramePacing();
            if (markerSampler != null)
            {
                markerSampler.Dispose();
            }

            if (garbageCounter != null)
            {
                garbageCounter.Dispose();
            }
        }
    }
}
