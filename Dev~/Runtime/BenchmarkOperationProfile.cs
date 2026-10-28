namespace Gley.CameraSystem.Dev
{
    public class BenchmarkOperationProfile
    {
        private readonly double[] milliseconds;
        private readonly double[] markerMilliseconds;
        private readonly int[] allocations;
        private readonly int[] runCounts;
        private readonly int[] rejectedCounts;

        public int OperationCount { get; }
        public int RunCapacity { get; }
        public int MarkerCount { get; }

        public BenchmarkOperationProfile(int operationCount, int runCapacity, int markerCount)
        {
            OperationCount = operationCount;
            RunCapacity = runCapacity;
            MarkerCount = markerCount;
            milliseconds = new double[operationCount * runCapacity];
            markerMilliseconds = new double[operationCount * runCapacity * markerCount];
            allocations = new int[operationCount * runCapacity];
            runCounts = new int[operationCount];
            rejectedCounts = new int[operationCount];
        }

        public int AddRun(int operation, double operationMilliseconds, int operationAllocations)
        {
            int run = runCounts[operation];
            if (run >= RunCapacity)
            {
                return -1;
            }

            milliseconds[operation * RunCapacity + run] = operationMilliseconds;
            allocations[operation * RunCapacity + run] = operationAllocations;
            runCounts[operation] = run + 1;
            return run;
        }

        public void AddRejection(int operation)
        {
            rejectedCounts[operation]++;
        }

        public void SetMarker(int operation, int run, int marker, double value)
        {
            markerMilliseconds[(operation * RunCapacity + run) * MarkerCount + marker] = value;
        }

        public int GetRunCount(int operation)
        {
            return runCounts[operation];
        }

        public int GetRejectedCount(int operation)
        {
            return rejectedCounts[operation];
        }

        public double GetMilliseconds(int operation, int run)
        {
            return milliseconds[operation * RunCapacity + run];
        }

        public int GetAllocations(int operation, int run)
        {
            return allocations[operation * RunCapacity + run];
        }

        public double GetMarkerMilliseconds(int operation, int run, int marker)
        {
            return markerMilliseconds[(operation * RunCapacity + run) * MarkerCount + marker];
        }
    }
}
