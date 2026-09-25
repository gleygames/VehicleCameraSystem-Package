namespace Gley.CameraSystem
{
    public readonly struct CollisionCandidate
    {
        public CollisionCandidateKind Kind { get; }
        public float InwardOffset { get; }
        public float UpOffset { get; }
        public float Displacement { get; }

        public CollisionCandidate(CollisionCandidateKind kind, float inwardOffset, float upOffset, float displacement)
        {
            Kind = kind;
            InwardOffset = inwardOffset;
            UpOffset = upOffset;
            Displacement = displacement;
        }
    }
}
