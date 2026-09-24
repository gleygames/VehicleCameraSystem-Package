namespace Gley.CameraSystem
{
    public readonly struct TravelRequest
    {
        public TravelDirection Direction { get; }
        public TransitionOptions Speed { get; }

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
    }
}
