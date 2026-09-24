using UnityEngine;

namespace Gley.CameraSystem
{
    [CreateAssetMenu(fileName = "Camera View Preset", menuName = "Gley/Vehicle Camera System/Camera View Preset")]
    public class CameraViewPreset : ScriptableObject
    {
        public const int CurrentFormatVersion = 1;

        [SerializeField] private OrbitMovementSettings orbitMovement = new OrbitMovementSettings();
        [SerializeField] private TravelSettings travel = new TravelSettings();
        [SerializeField] private DrivingSettings driving = new DrivingSettings();
        [SerializeField] private InteriorSettings interior = new InteriorSettings();
        [SerializeField] private RecenterSettings recenter = new RecenterSettings();
        [SerializeField] private CollisionSettings collision = new CollisionSettings();
        [SerializeField] private RenderingSettings rendering = new RenderingSettings();
        [SerializeField] private AimFrame aimFrame;
        [SerializeField] private CameraTimeSource timeSource;
        [SerializeField] private CameraViewType viewType;
        [SerializeField] private int formatVersion = CurrentFormatVersion;

        public OrbitMovementSettings OrbitMovement => orbitMovement;
        public TravelSettings Travel => travel;
        public DrivingSettings Driving => driving;
        public InteriorSettings Interior => interior;
        public RecenterSettings Recenter => recenter;
        public CollisionSettings Collision => collision;
        public RenderingSettings Rendering => rendering;
        public AimFrame AimFrame => aimFrame;
        public CameraTimeSource TimeSource => timeSource;
        public CameraViewType ViewType => viewType;
        public int FormatVersion => formatVersion;

        public void Configure(CameraViewType cameraViewType)
        {
            viewType = cameraViewType;
            orbitMovement.Configure(6f, 0.06f, 3f, 2f, 20f, 4f, 5f, false, -180f, 180f, GetImageRollFollow(cameraViewType));
            travel.Configure(10f, 10f, 0.4f);
            driving.Configure(0.12f, 0.08f, 3f, GetSpeedDistanceEnabled(cameraViewType), 5f, 25f, 3f, 0.25f, false, GetDrivingTurnLookEnabled(cameraViewType), 12f, 30f, 0.4f, 0.5f);
            interior.Configure(140f, 140f, 40f, 30f, 120f, 180f, GetInteriorTurnLookEnabled(cameraViewType), 20f, 0.4f, 30f, 0.5f, true, true, true, 0.02f, 0.03f, 0.015f, 0.1f, 1.5f, 0.5f, 0.05f);
            recenter.Configure(GetRecenterMode(cameraViewType), GetRecenterDelay(cameraViewType), 0.25f, 0.5f);
            collision.Configure(GetCollisionEnabled(cameraViewType), 0.25f, 8, 0.05f, 0.3f, 1);
            rendering.Configure(CullingMaskMode.Unchanged, 0, 0, 0, false, 0.3f, false, 1000f);
            aimFrame = GetAimFrame(cameraViewType);
            timeSource = CameraTimeSource.Unscaled;
            formatVersion = CurrentFormatVersion;
        }

        public void ConfigureOrbitTravel(float travelSpeed, float startResponseSeconds)
        {
            orbitMovement.Configure(travelSpeed, startResponseSeconds, orbitMovement.ZoomRate, orbitMovement.HeightRate, orbitMovement.DragOrbit, orbitMovement.DragHeight, orbitMovement.Pinch, orbitMovement.AngleLimitsEnabled, orbitMovement.MinimumBearing, orbitMovement.MaximumBearing, orbitMovement.ImageRollFollow);
        }

        public void ConfigureOffsetTravel(float heightSpeed, float zoomSpeed)
        {
            orbitMovement.Configure(orbitMovement.ManualTravelSpeed, orbitMovement.StartResponseHalfLife, zoomSpeed, heightSpeed, orbitMovement.DragOrbit, orbitMovement.DragHeight, orbitMovement.Pinch, orbitMovement.AngleLimitsEnabled, orbitMovement.MinimumBearing, orbitMovement.MaximumBearing, orbitMovement.ImageRollFollow);
        }

        [ContextMenu("Apply Defaults For View Type")]
        public void ApplyDefaultsForViewType()
        {
            Configure(viewType);
        }

        private float GetImageRollFollow(CameraViewType cameraViewType)
        {
            if (cameraViewType == CameraViewType.Fixed || cameraViewType == CameraViewType.Interior)
            {
                return 1f;
            }

            return 0f;
        }

        private bool GetSpeedDistanceEnabled(CameraViewType cameraViewType)
        {
            return cameraViewType == CameraViewType.Driving;
        }

        private bool GetDrivingTurnLookEnabled(CameraViewType cameraViewType)
        {
            return cameraViewType == CameraViewType.Driving;
        }

        private bool GetInteriorTurnLookEnabled(CameraViewType cameraViewType)
        {
            return cameraViewType == CameraViewType.Interior;
        }

        private RecenterMode GetRecenterMode(CameraViewType cameraViewType)
        {
            if (cameraViewType == CameraViewType.Driving || cameraViewType == CameraViewType.Interior)
            {
                return RecenterMode.Timed;
            }

            return RecenterMode.Persistent;
        }

        private float GetRecenterDelay(CameraViewType cameraViewType)
        {
            if (cameraViewType == CameraViewType.Interior)
            {
                return 2f;
            }

            if (cameraViewType == CameraViewType.Driving)
            {
                return 4f;
            }

            return 0f;
        }

        private bool GetCollisionEnabled(CameraViewType cameraViewType)
        {
            return cameraViewType == CameraViewType.Driving || cameraViewType == CameraViewType.Presentation;
        }

        private AimFrame GetAimFrame(CameraViewType cameraViewType)
        {
            if (cameraViewType == CameraViewType.Presentation)
            {
                return AimFrame.OwnerBody;
            }

            return AimFrame.Root;
        }
    }
}
