using UnityEngine;

namespace Gley.CameraSystem
{
    [CreateAssetMenu(fileName = "Vehicle Profile", menuName = "Gley/Vehicle Camera System/Vehicle Profile")]
    public class VehicleProfile : ScriptableObject
    {
        [SerializeField] private VehicleConnectorAnchors connectorAnchors;
        [SerializeField] private VehicleOrbit vehicleOrbit;
        [SerializeField] private CameraViewPreset fixedViewPreset;
        [SerializeField] private CameraViewPreset orbitViewPreset;
        [SerializeField] private Vector3 fixedCameraLocalPosition;
        [SerializeField] private Vector3 fixedWatchPointLocalPosition = Vector3.forward;

        public VehicleConnectorAnchors ConnectorAnchors => connectorAnchors;
        public VehicleOrbit VehicleOrbit => vehicleOrbit;
        public CameraViewPreset FixedViewPreset => fixedViewPreset;
        public CameraViewPreset OrbitViewPreset => orbitViewPreset;
        public Vector3 FixedCameraLocalPosition => fixedCameraLocalPosition;
        public Vector3 FixedWatchPointLocalPosition => fixedWatchPointLocalPosition;

        public void ConfigureFixedView(CameraViewPreset viewPreset, Vector3 cameraLocalPosition, Vector3 watchPointLocalPosition)
        {
            fixedViewPreset = viewPreset;
            fixedCameraLocalPosition = cameraLocalPosition;
            fixedWatchPointLocalPosition = watchPointLocalPosition;
        }

        public void ConfigureOrbit(VehicleOrbit orbit)
        {
            vehicleOrbit = orbit;
        }

        public void ConfigureOrbitView(CameraViewPreset viewPreset)
        {
            orbitViewPreset = viewPreset;
        }

        public void ConfigureConnectorAnchors(VehicleConnectorAnchors anchors)
        {
            connectorAnchors = anchors;
        }
    }
}
