using UnityEngine;

namespace Gley.CameraSystem.Dev
{
    public class GenerationReviewFixture : MonoBehaviour
    {
        private const string DrivingName = "Driving";
        private const string PresentationName = "Presentation";
        private const string InteriorName = "Interior";
        private const string FixedName = "Fixed";

        private readonly Rect helpArea = new Rect(10f, 10f, 560f, 340f);

        [SerializeField] private Transform vehicleModel;
        [SerializeField] private CameraSystemController cameraController;
        [SerializeField] private float driveSpeed = 8f;
        [SerializeField] private float turnRate = 30f;
        private string status = string.Empty;
        private float speed;

        public void ConfigureReview(Transform model, CameraSystemController controller)
        {
            vehicleModel = model;
            cameraController = controller;
        }

        [ContextMenu("Toggle Player Control Lock")]
        public void TogglePlayerControlLock()
        {
            if (cameraController == null || !cameraController.IsActive)
            {
                Debug.LogWarning("Enter Play Mode before toggling the player control lock.", this);
                return;
            }

            cameraController.SetPlayerControlLocked(!cameraController.IsPlayerControlLocked);
            status = $"Player control locked: {cameraController.IsPlayerControlLocked}";
            Debug.Log(status, this);
        }

        private void Start()
        {
            if (vehicleModel == null || cameraController == null)
            {
                status = "Assign Vehicle Model and Camera Controller on the Generation Review object, or rebuild the scene from Tools > Gley > Vehicle Camera System > Review.";
                Debug.LogError(status, this);
                return;
            }

            status = $"Activate '{cameraController.InitialViewName}': {cameraController.Activate()}";
            Debug.Log(status, this);
        }

        private void Update()
        {
            if (cameraController == null || !cameraController.IsActive)
            {
                return;
            }

#if ENABLE_LEGACY_INPUT_MANAGER
            UpdateViewKeys();
            UpdateVehicleDriving(Time.deltaTime);
#endif
        }

#if ENABLE_LEGACY_INPUT_MANAGER
        private void UpdateViewKeys()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                SelectView(DrivingName);
            }

            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                SelectView(PresentationName);
            }

            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                SelectView(InteriorName);
            }

            if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                SelectView(FixedName);
            }
        }

        private void SelectView(string viewName)
        {
            status = $"Select view '{viewName}': {cameraController.SelectView(viewName)}";
        }

        private void UpdateVehicleDriving(float deltaTime)
        {
            float throttle = GetAxis(KeyCode.UpArrow, KeyCode.DownArrow);
            speed = Mathf.MoveTowards(speed, throttle * driveSpeed, driveSpeed * deltaTime);
            float steering = GetAxis(KeyCode.RightArrow, KeyCode.LeftArrow);
            float speedFraction = speed / driveSpeed;
            vehicleModel.Rotate(0f, steering * turnRate * speedFraction * deltaTime, 0f, Space.World);
            vehicleModel.position += vehicleModel.forward * speed * deltaTime;
        }

        private float GetAxis(KeyCode positive, KeyCode negative)
        {
            float value = 0f;
            if (Input.GetKey(positive))
            {
                value += 1f;
            }

            if (Input.GetKey(negative))
            {
                value -= 1f;
            }

            return value;
        }
#endif

        private void OnGUI()
        {
            GUILayout.BeginArea(helpArea, GUI.skin.box);
            GUILayout.Label("Generation review");
            GUILayout.Label("Camera input (input companion): A / D orbit, W / S height, Q / E or scroll zoom, right-mouse drag");
            GUILayout.Label("C next view   R orbit reset   Shift+R full reset   [ / ] points of interest");
            GUILayout.Label("1 Driving   2 Presentation   3 Interior   4 Fixed");
            GUILayout.Label("Arrow keys  drive the vehicle");
            GUILayout.Label("Right-click component > Toggle Player Control Lock");
            GUILayout.Label(status);
            if (cameraController != null && cameraController.ActiveView != null)
            {
                GUILayout.Label($"Active view: {cameraController.ActiveView.Name}   no clear pose: {cameraController.HasNoClearPose}   locked: {cameraController.IsPlayerControlLocked}");
                GUILayout.Label($"Orbit distance {cameraController.OrbitDistance:F2}   zoom {cameraController.ZoomOffset:F2}   height {cameraController.HeightOffset:F2}");
                Transform cameraTransform = cameraController.AssignedCamera.transform;
                Vector3 cameraPosition = vehicleModel.InverseTransformPoint(cameraTransform.position);
                GUILayout.Label($"Camera relative to vehicle {cameraPosition:F2}   rotation {cameraTransform.eulerAngles:F1}");
            }

            GUILayout.EndArea();
        }
    }
}
