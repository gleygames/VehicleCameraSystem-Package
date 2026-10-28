namespace Gley.CameraSystem.Dev
{
    public readonly struct BenchmarkOperation
    {
        public string Name { get; }
        public string Result { get; }
        public double Milliseconds { get; }
        public int Frame { get; }
        public int Phase { get; }
        public int Allocations { get; }

        public BenchmarkOperation(int frame, int phase, string name, string result, double milliseconds, int allocations)
        {
            Frame = frame;
            Phase = phase;
            Name = name;
            Result = result;
            Milliseconds = milliseconds;
            Allocations = allocations;
        }
    }
}
