using UnityEngine;

namespace Gley.CameraSystem.Dev
{
    public class BenchmarkScript
    {
        private const int PhaseTotal = 15;

        private readonly int[] startFrames = new int[PhaseTotal];
        private readonly int[] frameCounts = new int[PhaseTotal];

        public float FrameTime { get; }
        public int FrameRate { get; }
        public int PhaseCount => PhaseTotal;
        public int TotalFrames { get; }

        public BenchmarkScript(int frameRate)
        {
            FrameRate = Mathf.Max(1, frameRate);
            FrameTime = 1f / FrameRate;
            int frame = 0;
            for (int phase = 0; phase < PhaseTotal; phase++)
            {
                startFrames[phase] = frame;
                frameCounts[phase] = SecondsToFrames(GetDuration((BenchmarkPhaseKind)phase));
                frame += frameCounts[phase];
            }

            TotalFrames = frame;
        }

        public int SecondsToFrames(float seconds)
        {
            return Mathf.RoundToInt(seconds * FrameRate);
        }

        public int GetStartFrame(int phase)
        {
            return startFrames[phase];
        }

        public int GetFrameCount(int phase)
        {
            return frameCounts[phase];
        }

        public int GetPhaseAtFrame(int frame)
        {
            for (int phase = PhaseTotal - 1; phase > 0; phase--)
            {
                if (frame >= startFrames[phase])
                {
                    return phase;
                }
            }

            return 0;
        }

        public float GetSpeed(BenchmarkPhaseKind phase, float time)
        {
            if (phase == BenchmarkPhaseKind.Accelerate)
            {
                return 20f * time / 6f;
            }

            if (phase == BenchmarkPhaseKind.Curves)
            {
                return Mathf.Lerp(20f, 15f, time / 8f);
            }

            if (phase == BenchmarkPhaseKind.Brake)
            {
                return 15f * (1f - time / 3f);
            }

            if (phase == BenchmarkPhaseKind.Reverse)
            {
                return GetReverseSpeed(time);
            }

            if (phase == BenchmarkPhaseKind.Tunnel)
            {
                return 12f * Mathf.Min(1f, time);
            }

            if (phase == BenchmarkPhaseKind.PlayerInput || phase == BenchmarkPhaseKind.ChainAttach)
            {
                return 10f;
            }

            if (phase == BenchmarkPhaseKind.Interior)
            {
                return 15f * Mathf.Min(1f, time / 3f);
            }

            if (phase == BenchmarkPhaseKind.Fixed)
            {
                return 15f;
            }

            if (phase == BenchmarkPhaseKind.ChainDetach)
            {
                return 8f;
            }

            if (phase == BenchmarkPhaseKind.Reactivate)
            {
                return 5f;
            }

            return 0f;
        }

        public float GetYawRate(BenchmarkPhaseKind phase, float time)
        {
            if (phase == BenchmarkPhaseKind.Curves)
            {
                return 20f * Mathf.Sin(2f * Mathf.PI * time / 4f);
            }

            if (phase == BenchmarkPhaseKind.Interior)
            {
                return 15f * Mathf.Sin(2f * Mathf.PI * time / 5f);
            }

            if (phase == BenchmarkPhaseKind.Fixed)
            {
                return 10f * Mathf.Sin(2f * Mathf.PI * time / 4f);
            }

            if (phase == BenchmarkPhaseKind.ChainAttach && time >= 5f)
            {
                return 10f * Mathf.Sin(2f * Mathf.PI * (time - 5f) / 5f);
            }

            return 0f;
        }

        public float GetBumpHeight(BenchmarkPhaseKind phase, float time)
        {
            if (phase == BenchmarkPhaseKind.Interior)
            {
                return 0.04f * Mathf.Sin(2f * Mathf.PI * 3f * time);
            }

            return 0f;
        }

        private float GetDuration(BenchmarkPhaseKind phase)
        {
            if (phase == BenchmarkPhaseKind.Warmup)
            {
                return 2f;
            }

            if (phase == BenchmarkPhaseKind.Accelerate || phase == BenchmarkPhaseKind.PlayerInput || phase == BenchmarkPhaseKind.ChainPresentation || phase == BenchmarkPhaseKind.ChainDetach)
            {
                return 6f;
            }

            if (phase == BenchmarkPhaseKind.Curves)
            {
                return 8f;
            }

            if (phase == BenchmarkPhaseKind.Brake)
            {
                return 3f;
            }

            if (phase == BenchmarkPhaseKind.Reverse)
            {
                return 5f;
            }

            if (phase == BenchmarkPhaseKind.Tunnel || phase == BenchmarkPhaseKind.TargetChange)
            {
                return 7f;
            }

            if (phase == BenchmarkPhaseKind.PresentationPoints)
            {
                return 11f;
            }

            if (phase == BenchmarkPhaseKind.Interior || phase == BenchmarkPhaseKind.ChainAttach)
            {
                return 10f;
            }

            return 4f;
        }

        private float GetReverseSpeed(float time)
        {
            if (time < 1f)
            {
                return -4f * time;
            }

            if (time < 4f)
            {
                return -4f;
            }

            return -4f * (5f - time);
        }
    }
}
