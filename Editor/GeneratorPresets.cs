namespace Gley.CameraSystem.Editor
{
    public readonly struct GeneratorPresets
    {
        public CameraViewPreset Driving { get; }
        public CameraViewPreset Presentation { get; }
        public CameraViewPreset Interior { get; }
        public CameraViewPreset Fixed { get; }

        public GeneratorPresets(CameraViewPreset driving, CameraViewPreset presentation, CameraViewPreset interior, CameraViewPreset fixedView)
        {
            Driving = driving;
            Presentation = presentation;
            Interior = interior;
            Fixed = fixedView;
        }
    }
}
