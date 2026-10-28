using Unity.Profiling;

namespace Gley.CameraSystem.Dev
{
    public class BenchmarkMarkerSampler
    {
        private readonly ProfilerRecorder[] recorders;
        private readonly string[] names =
        {
            CameraProfilerMarkers.UpdateCameraFrameName,
            CameraProfilerMarkers.MotionEstimationName,
            CameraProfilerMarkers.SeatMotionName,
            CameraProfilerMarkers.CommandsAndInputName,
            CameraProfilerMarkers.TargetPoseName,
            CameraProfilerMarkers.FollowLagName,
            CameraProfilerMarkers.CollisionName,
            CameraProfilerMarkers.AimName,
            CameraProfilerMarkers.AimSmoothingName,
            CameraProfilerMarkers.OrbitRebuildName,
            CameraProfilerMarkers.ActivationName,
            CameraProfilerMarkers.TargetChangeName,
            CameraProfilerMarkers.ViewSwitchName
        };
        private readonly int[] lastCounts;

        public int MarkerCount => names.Length;

        public BenchmarkMarkerSampler(int capacity)
        {
            recorders = new ProfilerRecorder[names.Length];
            lastCounts = new int[names.Length];
            for (int index = 0; index < names.Length; index++)
            {
                recorders[index] = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, names[index], capacity, ProfilerRecorderOptions.StartImmediately | ProfilerRecorderOptions.SumAllSamplesInFrame);
            }
        }

        public string GetName(int index)
        {
            return names[index];
        }

        public bool IsValid(int index)
        {
            return recorders[index].Valid;
        }

        public long TakeNewSample(int index)
        {
            ProfilerRecorder recorder = recorders[index];
            if (!recorder.Valid)
            {
                return -1;
            }

            int count = recorder.Count;
            if (count == lastCounts[index] || count == 0)
            {
                return -1;
            }

            lastCounts[index] = count;
            return recorder.GetSample(count - 1).Value;
        }

        public void Dispose()
        {
            for (int index = 0; index < recorders.Length; index++)
            {
                recorders[index].Dispose();
            }
        }
    }
}
