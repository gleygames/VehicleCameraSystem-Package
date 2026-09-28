namespace Gley.CameraSystem.Editor
{
    public readonly struct VehicleCameraSetupResult
    {
        public VehicleCameraSetupStatus Status { get; }
        public VehicleProfile Profile { get; }
        public VehicleCameraTarget Target { get; }
        public CameraSystemController Controller { get; }
        public bool GeneratedProfile { get; }
        public bool CreatedTarget { get; }
        public bool CreatedController { get; }
        public bool IsInputSystemInstalled { get; }

        public VehicleCameraSetupResult(VehicleCameraSetupStatus status)
        {
            Status = status;
            Profile = null;
            Target = null;
            Controller = null;
            GeneratedProfile = false;
            CreatedTarget = false;
            CreatedController = false;
            IsInputSystemInstalled = false;
        }

        public VehicleCameraSetupResult(VehicleCameraSetupStatus status, VehicleProfile profile, VehicleCameraTarget target, CameraSystemController controller, bool generatedProfile, bool createdTarget, bool createdController, bool isInputSystemInstalled)
        {
            Status = status;
            Profile = profile;
            Target = target;
            Controller = controller;
            GeneratedProfile = generatedProfile;
            CreatedTarget = createdTarget;
            CreatedController = createdController;
            IsInputSystemInstalled = isInputSystemInstalled;
        }
    }
}
