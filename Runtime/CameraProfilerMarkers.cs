using Unity.Profiling;

namespace Gley.CameraSystem
{
    public class CameraProfilerMarkers
    {
        public const string UpdateCameraFrameName = "VehicleCamera.UpdateCameraFrame";
        public const string MotionEstimationName = "VehicleCamera.MotionEstimation";
        public const string SeatMotionName = "VehicleCamera.SeatMotion";
        public const string CommandsAndInputName = "VehicleCamera.CommandsAndInput";
        public const string TargetPoseName = "VehicleCamera.TargetPose";
        public const string FollowLagName = "VehicleCamera.FollowLag";
        public const string CollisionName = "VehicleCamera.Collision";
        public const string AimName = "VehicleCamera.Aim";
        public const string AimSmoothingName = "VehicleCamera.AimSmoothing";
        public const string OrbitRebuildName = "VehicleCamera.OrbitRebuild";
        public const string ActivationName = "VehicleCamera.Activation";
        public const string TargetChangeName = "VehicleCamera.TargetChange";
        public const string ViewSwitchName = "VehicleCamera.ViewSwitch";

        public ProfilerMarker UpdateCameraFrame { get; }
        public ProfilerMarker MotionEstimation { get; }
        public ProfilerMarker SeatMotion { get; }
        public ProfilerMarker CommandsAndInput { get; }
        public ProfilerMarker TargetPose { get; }
        public ProfilerMarker FollowLag { get; }
        public ProfilerMarker Collision { get; }
        public ProfilerMarker Aim { get; }
        public ProfilerMarker AimSmoothing { get; }
        public ProfilerMarker OrbitRebuild { get; }
        public ProfilerMarker Activation { get; }
        public ProfilerMarker TargetChange { get; }
        public ProfilerMarker ViewSwitch { get; }

        public CameraProfilerMarkers()
        {
            UpdateCameraFrame = new ProfilerMarker(UpdateCameraFrameName);
            MotionEstimation = new ProfilerMarker(MotionEstimationName);
            SeatMotion = new ProfilerMarker(SeatMotionName);
            CommandsAndInput = new ProfilerMarker(CommandsAndInputName);
            TargetPose = new ProfilerMarker(TargetPoseName);
            FollowLag = new ProfilerMarker(FollowLagName);
            Collision = new ProfilerMarker(CollisionName);
            Aim = new ProfilerMarker(AimName);
            AimSmoothing = new ProfilerMarker(AimSmoothingName);
            OrbitRebuild = new ProfilerMarker(OrbitRebuildName);
            Activation = new ProfilerMarker(ActivationName);
            TargetChange = new ProfilerMarker(TargetChangeName);
            ViewSwitch = new ProfilerMarker(ViewSwitchName);
        }
    }
}
