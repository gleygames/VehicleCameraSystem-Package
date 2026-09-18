namespace Gley.CameraSystem
{
    public enum TwoBodyOrbitConnectorGenerationResult
    {
        NotGenerated,
        Generated,
        MissingFrontProfile,
        MissingRearProfile,
        MissingFrontOrbit,
        MissingRearOrbit,
        InvalidFrontOrbit,
        InvalidRearOrbit,
        InvalidFrontRemovableSections,
        InvalidRearRemovableSections,
        MissingFrontConnectorAnchors,
        MissingRearConnectorAnchors,
        InvalidFrontConnectorAnchors,
        InvalidRearConnectorAnchors
    }
}
