namespace Gley.CameraSystem
{
    public enum VehicleTargetResult
    {
        Accepted,
        NotConfigured,
        MissingBody,
        MissingProfile,
        BodyAlreadyInChain,
        BodyNotInChain,
        CannotDetachRoot,
        OverrideDoesNotMatch
    }
}
