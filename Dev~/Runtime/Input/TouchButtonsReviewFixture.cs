using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Gley.CameraSystem.Input.Dev
{
    public class TouchButtonsReviewFixture : MonoBehaviour
    {
        [SerializeField] private GameObject buttonsPrefab;
        [SerializeField] private InputActionAsset actions;
        private CameraSystemController controller;

        private IEnumerator Start()
        {
            yield return null;

            controller = Object.FindFirstObjectByType<CameraSystemController>();
            if (controller == null || buttonsPrefab == null || actions == null)
            {
                Debug.LogError("Touch buttons review setup is missing the controller, buttons prefab or input actions.", this);
                yield break;
            }

            VehicleCameraInput companion = controller.GetComponent<VehicleCameraInput>();
            if (companion == null)
            {
                companion = controller.gameObject.AddComponent<VehicleCameraInput>();
            }

            companion.Configure(controller, actions);

            GameObject canvasObject = new GameObject("Touch Buttons Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject panel = Instantiate(buttonsPrefab, canvas.transform);
            CameraCommandButton[] buttons = panel.GetComponentsInChildren<CameraCommandButton>(true);
            for (int index = 0; index < buttons.Length; index++)
            {
                buttons[index].Configure(companion, buttons[index].Action);
            }

            GameObject eventSystemObject = new GameObject("Touch Buttons EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystemObject.transform.SetParent(transform, false);
        }

        private void OnGUI()
        {
            if (controller == null || controller.ActiveView == null)
            {
                return;
            }

            GUI.Label(new Rect(16f, 16f, 420f, 28f), "S37 Touch Buttons: View / Reset / Prev / Next");
        }
    }
}
