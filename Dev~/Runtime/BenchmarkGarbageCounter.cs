using UnityEngine.Profiling;

namespace Gley.CameraSystem.Dev
{
    public class BenchmarkGarbageCounter
    {
        private const string SamplerName = "GC.Alloc";

        private readonly Recorder recorder;

        private object calibrationAllocation;
        private int lastCount;
        private bool isCumulative;

        public bool IsAvailable { get; private set; }
        public bool IsCumulative => isCumulative;

        public BenchmarkGarbageCounter()
        {
            recorder = Recorder.Get(SamplerName);
            recorder.enabled = false;
            recorder.FilterToCurrentThread();
            Calibrate();
        }

        public void Begin()
        {
            recorder.enabled = true;
        }

        public int End()
        {
            recorder.enabled = false;
            int count = recorder.sampleBlockCount;
            int allocations = count;
            if (isCumulative)
            {
                allocations = count - lastCount;
                if (allocations < 0)
                {
                    allocations = count;
                }
            }

            lastCount = count;
            return allocations;
        }

        private void Calibrate()
        {
            recorder.enabled = true;
            calibrationAllocation = new object[1];
            recorder.enabled = false;
            int first = recorder.sampleBlockCount;
            recorder.enabled = true;
            recorder.enabled = false;
            int second = recorder.sampleBlockCount;
            IsAvailable = first > 0 && calibrationAllocation != null;
            isCumulative = second > 0 && second >= first;
            lastCount = second;
        }

        public void Dispose()
        {
            recorder.enabled = false;
            recorder.CollectFromAllThreads();
        }
    }
}
