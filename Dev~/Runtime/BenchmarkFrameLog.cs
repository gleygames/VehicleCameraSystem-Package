namespace Gley.CameraSystem.Dev
{
    public class BenchmarkFrameLog
    {
        private readonly string[] viewNames;
        private readonly double[] updateMilliseconds;
        private readonly long[][] markerNanoseconds;
        private readonly int[] phases;
        private readonly int[] allocations;
        private readonly int[] casts;
        private readonly int[] castLimits;
        private readonly int[] bodyCounts;
        private readonly bool[] activeFrames;

        public int FrameCount { get; }

        public BenchmarkFrameLog(int frameCount, int markerCount)
        {
            FrameCount = frameCount;
            viewNames = new string[frameCount];
            updateMilliseconds = new double[frameCount];
            markerNanoseconds = new long[markerCount][];
            for (int marker = 0; marker < markerCount; marker++)
            {
                markerNanoseconds[marker] = new long[frameCount];
                for (int frame = 0; frame < frameCount; frame++)
                {
                    markerNanoseconds[marker][frame] = -1;
                }
            }

            phases = new int[frameCount];
            allocations = new int[frameCount];
            casts = new int[frameCount];
            castLimits = new int[frameCount];
            bodyCounts = new int[frameCount];
            activeFrames = new bool[frameCount];
        }

        public void RecordFrame(int frame, int phase, bool isActive, string viewName, int bodyCount, double milliseconds, int frameAllocations, int frameCasts, int castLimit)
        {
            phases[frame] = phase;
            activeFrames[frame] = isActive;
            viewNames[frame] = viewName;
            bodyCounts[frame] = bodyCount;
            updateMilliseconds[frame] = milliseconds;
            allocations[frame] = frameAllocations;
            casts[frame] = frameCasts;
            castLimits[frame] = castLimit;
        }

        public void RecordMarker(int marker, int frame, long nanoseconds)
        {
            if (frame < 0 || frame >= FrameCount)
            {
                return;
            }

            markerNanoseconds[marker][frame] = nanoseconds;
        }

        public string GetViewName(int frame)
        {
            return viewNames[frame];
        }

        public double GetUpdateMilliseconds(int frame)
        {
            return updateMilliseconds[frame];
        }

        public long GetMarkerNanoseconds(int marker, int frame)
        {
            return markerNanoseconds[marker][frame];
        }

        public int GetPhase(int frame)
        {
            return phases[frame];
        }

        public int GetAllocations(int frame)
        {
            return allocations[frame];
        }

        public int GetCasts(int frame)
        {
            return casts[frame];
        }

        public int GetCastLimit(int frame)
        {
            return castLimits[frame];
        }

        public int GetBodyCount(int frame)
        {
            return bodyCounts[frame];
        }

        public bool IsActive(int frame)
        {
            return activeFrames[frame];
        }
    }
}
