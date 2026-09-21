namespace Gley.CameraSystem
{
    public enum ThreeBodyClosedBezierOrbitAssemblyResult
    {
        NotAssembled,
        Valid,
        LeadMiddleConnectorGenerationFailed,
        MiddleRearConnectorGenerationFailed,
        LeadMiddleConnectorOverrideInvalid,
        MiddleRearConnectorOverrideInvalid,
        Disconnected,
        NonPlanar,
        SelfIntersecting,
        Degenerate
    }
}
