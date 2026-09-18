using UnityEngine;

namespace Gley.CameraSystem
{
    [CreateAssetMenu(fileName = "Camera View Preset", menuName = "Gley/Vehicle Camera System/Camera View Preset")]
    public class CameraViewPreset : ScriptableObject
    {
        [SerializeField] private CameraViewType viewType;
        [SerializeField] private float orbitTravelSpeed = 5f;
        [SerializeField] private float orbitStartResponseSeconds = 0.15f;
        [SerializeField] private float heightTravelSpeed = 2f;
        [SerializeField] private float zoomTravelSpeed = 2f;

        public CameraViewType ViewType => viewType;
        public float OrbitTravelSpeed => orbitTravelSpeed;
        public float OrbitStartResponseSeconds => orbitStartResponseSeconds;
        public float HeightTravelSpeed => heightTravelSpeed;
        public float ZoomTravelSpeed => zoomTravelSpeed;

        public void Configure(CameraViewType cameraViewType)
        {
            viewType = cameraViewType;
        }

        public void ConfigureOrbitTravel(float travelSpeed, float startResponseSeconds)
        {
            orbitTravelSpeed = travelSpeed;
            orbitStartResponseSeconds = startResponseSeconds;
        }

        public void ConfigureOffsetTravel(float heightSpeed, float zoomSpeed)
        {
            heightTravelSpeed = heightSpeed;
            zoomTravelSpeed = zoomSpeed;
        }
    }
}
