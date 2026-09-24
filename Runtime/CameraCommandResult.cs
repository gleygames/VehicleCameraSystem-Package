namespace Gley.CameraSystem
{
    public enum CameraCommandResult
    {
        Accepted,
        NotActive,
        MissingCamera,
        MissingTarget,
        CameraInUse,
        ViewNotAvailable,
        OrbitNotFound,
        PointNotFound,
        ViewInvalid,
        AssetNeedsUpgrade,
        NoNextPoint,
        Unreachable,
        PlayerControlLocked,
        InvalidWhileActive
    }
}
