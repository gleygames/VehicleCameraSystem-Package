namespace Gley.CameraSystem
{
    internal readonly struct RetainedLeadSegmentMapping
    {
        public OrbitSourceSegment SourceSegment { get; }
        public float SourceSegmentStartDistance { get; }
        public float SourceStartDistance { get; }
        public float SourceEndDistance { get; }
        public float SourceStartT { get; }
        public float SourceEndT { get; }
        public int AssembledSegmentIndex { get; }

        public RetainedLeadSegmentMapping(OrbitSourceSegment sourceSegment, float sourceSegmentStartDistance, float sourceStartDistance, float sourceEndDistance, float sourceStartT, float sourceEndT, int assembledSegmentIndex)
        {
            SourceSegment = sourceSegment;
            SourceSegmentStartDistance = sourceSegmentStartDistance;
            SourceStartDistance = sourceStartDistance;
            SourceEndDistance = sourceEndDistance;
            SourceStartT = sourceStartT;
            SourceEndT = sourceEndT;
            AssembledSegmentIndex = assembledSegmentIndex;
        }
    }
}
