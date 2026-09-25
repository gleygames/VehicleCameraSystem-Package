namespace Gley.CameraSystem
{
    public readonly struct ViewAdjustment
    {
        public float ZoomOffset { get; }
        public float HeightOffset { get; }
        public int ViewId { get; }

        public ViewAdjustment(int viewId, float zoomOffset, float heightOffset)
        {
            ViewId = viewId;
            ZoomOffset = zoomOffset;
            HeightOffset = heightOffset;
        }
    }
}
