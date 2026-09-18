namespace Gley.CameraSystem
{
    public enum OrbitValidationResult
    {
        Valid,
        TooFewKnots,
        NonPlanar,
        SelfIntersecting,
        Degenerate
    }
}
