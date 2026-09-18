namespace Gley.CameraSystem
{
    public enum CameraSystemActivationResult
    {
        NotActivated,
        Succeeded,
        MissingCamera,
        MissingVehicleBody,
        MissingVehicleProfile,
        MissingViewPreset,
        MissingOrbit,
        MissingWatchMarkers,
        UnsupportedViewPreset,
        InvalidOrbit,
        InvalidWatchMarkers,
        InvalidOrbitOffsetRange,
        InvalidFixedView,
        InvalidAttachedOrbit,
        MissingAttachedWatchMarkers,
        InvalidAttachedWatchMarkers
    }
}
