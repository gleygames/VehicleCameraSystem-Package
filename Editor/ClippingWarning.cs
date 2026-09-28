namespace Gley.CameraSystem.Editor
{
    public readonly struct ClippingWarning
    {
        public float Bearing { get; }
        public float ZoomOffset { get; }
        public float HeightOffset { get; }
        public int BodyIndex { get; }
        public bool IsZoomedIn { get; }
        public bool IsLow { get; }

        public ClippingWarning(float bearing, float zoomOffset, float heightOffset, int bodyIndex, bool isZoomedIn, bool isLow)
        {
            Bearing = bearing;
            ZoomOffset = zoomOffset;
            HeightOffset = heightOffset;
            BodyIndex = bodyIndex;
            IsZoomedIn = isZoomedIn;
            IsLow = isLow;
        }
    }
}
