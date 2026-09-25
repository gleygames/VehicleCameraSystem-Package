namespace Gley.CameraSystem
{
    public readonly struct TransitionOptions
    {
        public TransitionMode Mode { get; }
        public float Value { get; }
        public bool LockPlayerControl { get; }

        public TransitionOptions(TransitionMode mode)
        {
            Mode = mode;
            Value = 0f;
            LockPlayerControl = false;
        }

        public TransitionOptions(TransitionMode mode, float value)
        {
            Mode = mode;
            Value = value;
            LockPlayerControl = false;
        }

        public TransitionOptions(TransitionMode mode, bool lockPlayerControl)
        {
            Mode = mode;
            Value = 0f;
            LockPlayerControl = lockPlayerControl;
        }

        public TransitionOptions(TransitionMode mode, float value, bool lockPlayerControl)
        {
            Mode = mode;
            Value = value;
            LockPlayerControl = lockPlayerControl;
        }
    }
}
