namespace Gley.CameraSystem
{
    public enum TwoBodyClosedBezierOrbitAssemblyResult
    {
        NotAssembled,
        Valid,
        ConnectorGenerationFailed,
        Disconnected,
        NonPlanar,
        SelfIntersecting,
        Degenerate
    }
}
