namespace Gley.CameraSystem
{
    public readonly struct PlayerDefaultEntry
    {
        public OrbitPose RearDefault { get; }
        public OrbitPose FrontDefault { get; }
        public string ProfileId { get; }
        public int ViewId { get; }
        public bool HasRearDefault { get; }
        public bool HasFrontDefault { get; }

        public PlayerDefaultEntry(string profileId, int viewId, bool hasRearDefault, OrbitPose rearDefault, bool hasFrontDefault, OrbitPose frontDefault)
        {
            ProfileId = profileId;
            ViewId = viewId;
            HasRearDefault = hasRearDefault;
            RearDefault = rearDefault;
            HasFrontDefault = hasFrontDefault;
            FrontDefault = frontDefault;
        }
    }
}
