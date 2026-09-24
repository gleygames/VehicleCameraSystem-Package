namespace Gley.CameraSystem
{
    public readonly struct TransitionOptions
    {
        public TransitionMode Mode { get; }
        public float Value { get; }

        public TransitionOptions(TransitionMode mode)
        {
            Mode = mode;
            Value = 0f;
        }

        public TransitionOptions(TransitionMode mode, float value)
        {
            Mode = mode;
            Value = value;
        }
    }
}
