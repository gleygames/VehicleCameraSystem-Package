namespace Gley.CameraSystem
{
    public class ResetController
    {
        private const float AtDestinationTolerance = 0.01f;

        private readonly OrbitPoseTravel travel = new OrbitPoseTravel();
        private readonly StoredPoseResolver storedPoseResolver = new StoredPoseResolver();

        private OrbitPose destinationPose;

        public bool IsTravelling => travel.IsTravelling;
        public bool IsPlannedAtDestination => travel.PlannedRouteLength <= AtDestinationTolerance;

        public bool UpdateResetTravel(float deltaTime)
        {
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

        public void BeginPlannedTravel(TransitionOptions speed, TravelSettings settings)
        {
            travel.BeginPlannedTravel(speed, settings);
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
        }

        public void Clear()
        {
            travel.Clear();
        }

        private CameraCommandResult Plan(ChainOrbit orbit, VehicleOrbit ranges, OrbitMovement movement, OrbitPose pose)
        {
            destinationPose = pose;
            LivePose destination = storedPoseResolver.Resolve(pose, orbit, ranges, movement.OrbitDistance);
            return travel.Plan(orbit, movement, destination, TravelDirection.Shortest);
        }
    }
}
