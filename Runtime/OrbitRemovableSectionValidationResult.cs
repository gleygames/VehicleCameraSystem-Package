namespace Gley.CameraSystem
{
    public enum OrbitRemovableSectionValidationResult
    {
        NotConfigured,
        Valid,
        MissingFrontSection,
        MissingRearSection,
        InvalidFrontSection,
        InvalidRearSection,
        OverlappingSections
    }
}
