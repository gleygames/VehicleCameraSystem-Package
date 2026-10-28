using UnityEngine;

namespace Gley.CameraSystem.Dev
{
    public class BenchmarkRoute
    {
        private readonly Vector3[] positions;
        private readonly float[] headings;

        public BenchmarkRoute(BenchmarkScript script)
        {
            positions = new Vector3[script.TotalFrames];
            headings = new float[script.TotalFrames];
            Vector3 groundPosition = Vector3.zero;
            float heading = 0f;
            float deltaTime = script.FrameTime;
            for (int frame = 0; frame < script.TotalFrames; frame++)
            {
                int phase = script.GetPhaseAtFrame(frame);
                BenchmarkPhaseKind phaseKind = (BenchmarkPhaseKind)phase;
                float time = (frame - script.GetStartFrame(phase)) * deltaTime;
                heading += script.GetYawRate(phaseKind, time) * deltaTime;
                Vector3 forward = Quaternion.Euler(0f, heading, 0f) * Vector3.forward;
                groundPosition += forward * script.GetSpeed(phaseKind, time) * deltaTime;
                positions[frame] = groundPosition + Vector3.up * script.GetBumpHeight(phaseKind, time);
                headings[frame] = heading;
            }
        }

        public Vector3 GetPosition(int frame)
        {
            return positions[frame];
        }

        public Quaternion GetRotation(int frame)
        {
            return Quaternion.Euler(0f, headings[frame], 0f);
        }
    }
}
