namespace Gley.CameraSystem
{
    public class ResetController
    {
        private const float AtDestinationTolerance = 0.01f;

        private readonly OrbitPoseTravel travel = new OrbitPoseTravel();
        private readonly StoredPoseResolver storedPoseResolver = new StoredPoseResolver();

        private OrbitPose destinationPose;
        private InteriorView headView;
        private float headReturnHalfLife;
        private bool isHeadPlanned;
        private bool isReturningHead;

        public bool IsTravelling => travel.IsTravelling || isReturningHead;
        public bool IsPlannedAtDestination
        {
            get
            {
                if (isHeadPlanned)
                {
                    return headView.IsNeutral;
                }

                return travel.PlannedRouteLength <= AtDestinationTolerance;
            }
        }

        public bool UpdateResetTravel(float deltaTime)
        {
            if (isReturningHead)
            {
                if (!headView.UpdateInteriorHeadReturn(deltaTime, headReturnHalfLife))
                {
                    return false;
                }

                isReturningHead = false;
                return true;
            }

            return travel.UpdateOrbitPoseTravel(deltaTime);
        }

        public CameraCommandResult PlanOrbitReset(ChainOrbit orbit, VehicleOrbit ranges, OrbitMovement movement, OrbitPose defaultPose)
        {
            return Plan(orbit, ranges, movement, new OrbitPose(defaultPose.Bearing, movement.PlayerZoom, movement.HeightOffset));
        }

        public CameraCommandResult PlanFullReset(ChainOrbit orbit, VehicleOrbit ranges, OrbitMovement movement, OrbitPose defaultPose)
        {
            return Plan(orbit, ranges, movement, defaultPose);
        }

        public CameraCommandResult PlanHeadReset(InteriorView view)
        {
            headView = view;
            isHeadPlanned = true;
            return CameraCommandResult.Accepted;
        }

        public void BeginPlannedTravel(TransitionOptions speed, TravelSettings settings)
        {
            travel.BeginPlannedTravel(speed, settings);
        }

        public void BeginPlannedHeadReturn(float returnHalfLife)
        {
            if (!isHeadPlanned)
            {
                return;
            }

            headReturnHalfLife = returnHalfLife;
            isReturningHead = true;
        }

        public CameraCommandResult ReplanTravel(ChainOrbit orbit, VehicleOrbit ranges, OrbitMovement movement, TravelSettings settings)
        {
            if (!travel.IsTravelling)
            {
                return CameraCommandResult.Accepted;
            }

            LivePose destination = storedPoseResolver.Resolve(destinationPose, orbit, ranges, movement.OrbitDistance);
            return travel.ReplanTravel(orbit, destination, settings);
        }

        public void StopTravel()
        {
            travel.Stop();
            isReturningHead = false;
        }

        public void Clear()
        {
            travel.Clear();
            headView = null;
            isHeadPlanned = false;
            isReturningHead = false;
        }

        private CameraCommandResult Plan(ChainOrbit orbit, VehicleOrbit ranges, OrbitMovement movement, OrbitPose pose)
        {
            isHeadPlanned = false;
            destinationPose = pose;
            LivePose destination = storedPoseResolver.Resolve(pose, orbit, ranges, movement.OrbitDistance);
            return travel.Plan(orbit, movement, destination, TravelDirection.Shortest);
        }
    }
}
