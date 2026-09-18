namespace Gley.CameraSystem
{
    public enum CameraSystemAttachmentResult
    {
        NotRequested,
        Attached,
        Detached,
        MissingRootVehicleBody,
        MissingRootVehicleProfile,
        MissingRearVehicleBody,
        MissingRearVehicleProfile,
        RearVehicleMatchesRoot,
        RearVehicleAlreadyAttached,
        NoRearVehicleAttached,
        InvalidActiveExteriorOrbit,
        InvalidActiveExteriorDetachment
    }
}
