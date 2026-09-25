namespace Gley.CameraSystem
{
    public class ReverseController
    {
        private readonly OrbitPoseTravel travel = new OrbitPoseTravel();
        private readonly StoredPoseResolver storedPoseResolver = new StoredPoseResolver();

        private LivePose rememberedPose;
        private bool isReverseActive;
        private bool hasRememberedPose;
        private bool hasPlayerMoved;
        private bool isReturnPlanned;
        private bool isReturning;

        public bool IsReverseActive => isReverseActive;
        public bool IsTravelling => travel.IsTravelling;
        public bool CanReturn => hasRememberedPose && !hasPlayerMoved;

        public bool UpdateReverseTravel(float deltaTime)
        {
            if (!travel.UpdateOrbitPoseTravel(deltaTime))
            {
                return false;
            }

            if (isReturning)
            {
                hasRememberedPose = false;
                isReturning = false;
            }

            return true;
        }

        public bool SetReverseState(bool active)
        {
            if (active == isReverseActive)
            {
                return false;
            }

            isReverseActive = active;
            if (active)
            {
                hasPlayerMoved = false;
            }

            return true;
        }

        public CameraCommandResult PlanEnter(ChainOrbit orbit, VehicleOrbit ranges, OrbitMovement movement, OrbitPose frontDefault)
        {
            if (!isReturning || !travel.IsTravelling || !hasRememberedPose)
            {
                rememberedPose = movement.Pose;
                hasRememberedPose = true;
            }

            isReturnPlanned = false;
            LivePose destination = storedPoseResolver.Resolve(frontDefault, orbit, ranges, movement.OrbitDistance);
            return travel.Plan(orbit, movement, destination, TravelDirection.Shortest);
        }

        public CameraCommandResult PlanReturn(ChainOrbit orbit, OrbitMovement movement)
        {
            isReturnPlanned = true;
            return travel.Plan(orbit, movement, rememberedPose, TravelDirection.Shortest);
        }

        public void BeginPlannedTravel(TransitionOptions speed, TravelSettings settings)
        {
            isReturning = isReturnPlanned;
            travel.BeginPlannedTravel(speed, settings);
        }

        public CameraCommandResult ReplanTravel(ChainOrbit orbit, VehicleOrbit ranges, OrbitMovement movement, OrbitPose frontDefault, TravelSettings settings)
        {
            if (!travel.IsTravelling)
            {
                return CameraCommandResult.Accepted;
            }

            LivePose destination = rememberedPose;
            if (!isReturning)
            {
                destination = storedPoseResolver.Resolve(frontDefault, orbit, ranges, movement.OrbitDistance);
            }

            return travel.ReplanTravel(orbit, destination, settings);
        }

        public void RemapRememberedPose(OrbitRemapper remapper, ChainOrbit oldOrbit, ChainOrbit newOrbit)
        {
            if (!hasRememberedPose)
            {
                return;
            }

            float remappedDistance;
            if (remapper.Remap(oldOrbit, rememberedPose.OrbitDistance, newOrbit, out remappedDistance) == OrbitRemapResult.InvalidNewOrbit)
            {
                hasRememberedPose = false;
                return;
            }

            rememberedPose = new LivePose(remappedDistance, rememberedPose.ZoomOffset, rememberedPose.HeightOffset);
        }

        public void NotePlayerInput()
        {
            hasPlayerMoved = true;
        }

        public void ForgetRememberedPose()
        {
            hasRememberedPose = false;
        }

        public void StopTravel()
        {
            travel.Stop();
        }

        public void Clear()
        {
            travel.Clear();
            isReverseActive = false;
            hasRememberedPose = false;
            hasPlayerMoved = false;
            isReturnPlanned = false;
            isReturning = false;
        }
    }
}
