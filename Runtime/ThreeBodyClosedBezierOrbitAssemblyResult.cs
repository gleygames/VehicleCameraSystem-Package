namespace Gley.CameraSystem
{
    public enum ThreeBodyClosedBezierOrbitAssemblyResult
    {
        NotAssembled,
        Valid,
        LeadMiddleConnectorGenerationFailed,
        MiddleRearConnectorGenerationFailed,
        Disconnected,
        NonPlanar,
        SelfIntersecting,
        Degenerate
    }
}
