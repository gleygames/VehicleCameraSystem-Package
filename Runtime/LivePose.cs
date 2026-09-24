namespace Gley.CameraSystem
{
    public readonly struct LivePose
    {
        public float OrbitDistance { get; }
        public float ZoomOffset { get; }
        public float HeightOffset { get; }

        public LivePose(float orbitDistance, float zoomOffset, float heightOffset)
        {
            OrbitDistance = orbitDistance;
            ZoomOffset = zoomOffset;
            HeightOffset = heightOffset;
        }
    }
}
