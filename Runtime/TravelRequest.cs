namespace Gley.CameraSystem
{
    public readonly struct TravelRequest
    {
        public TravelDirection Direction { get; }
        public TransitionOptions Speed { get; }
        public bool LockPlayerControl => Speed.LockPlayerControl;

        public TravelRequest(TravelDirection direction)
        {
            Direction = direction;
            Speed = new TransitionOptions(TransitionMode.PresetSpeed);
        }

        public TravelRequest(TravelDirection direction, TransitionOptions speed)
        {
            Direction = direction;
            Speed = speed;
        }

        public TravelRequest(TravelDirection direction, bool lockPlayerControl)
        {
            Direction = direction;
            Speed = new TransitionOptions(TransitionMode.PresetSpeed, lockPlayerControl);
        }
    }
}
