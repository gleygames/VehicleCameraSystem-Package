using System.Collections;
using Gley.CameraSystem.Input;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Gley.CameraSystem.Tests.Input
{
    public class TouchInputPlayModeTests : InputTestFixture
    {
        private const string ActionsPath = "Input/VehicleCameraInputActions.inputactions";

        private InputTestProfileBuilder builder;
        private Touchscreen touchscreen;
        private CameraSystemController controller;
        private GameObject uiObject;

        public override void Setup()
        {
            base.Setup();
            touchscreen = InputSystem.AddDevice<Touchscreen>();
            InputActionAsset actions = UnityEditor.AssetDatabase.LoadAssetAtPath<InputActionAsset>(new VehicleCameraPackageLocator().AssetPath(ActionsPath));
            builder = new InputTestProfileBuilder();
            VehicleProfile profile = builder.CreateProfile(1f, 2f, 2f);
            Transform body = builder.CreateBody("Vehicle");
            VehicleCameraTarget target = builder.CreateTarget(body, profile);
            Camera camera = builder.CreateCamera("Camera");
            controller = builder.CreateController("Controller", camera, target);
            builder.CreateCompanion("Companion", controller, actions);
            controller.Activate();
        }

        [UnityTest]
        public IEnumerator SimulatedTouchDrag_OrbitsCamera()
        {
            yield return null;
            Vector2 start = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            float before = controller.OrbitDistance;
            BeginTouch(1, start, queueEventOnly: true, screen: touchscreen);
            yield return null;
            MoveTouch(1, start + new Vector2(Screen.width * 0.1f, 0f), queueEventOnly: true, screen: touchscreen);
            yield return null;
            Assert.AreNotEqual(before, controller.OrbitDistance);
        }

        [UnityTest]
        public IEnumerator TouchOnButton_DoesNotOrbit()
        {
            uiObject = new GameObject("Touch UI");
            EventSystem eventSystem = uiObject.AddComponent<EventSystem>();
            TouchInputUiModule module = uiObject.AddComponent<TouchInputUiModule>();
            EventSystem.current = eventSystem;
            GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
            canvasObject.transform.SetParent(uiObject.transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            GameObject buttonObject = new GameObject("Button", typeof(RectTransform), typeof(Button));
            buttonObject.transform.SetParent(canvasObject.transform, false);
            RectTransform buttonArea = buttonObject.GetComponent<RectTransform>();
            buttonArea.anchorMin = new Vector2(0.5f, 0.5f);
            buttonArea.anchorMax = new Vector2(0.5f, 0.5f);
            buttonArea.sizeDelta = new Vector2(200f, 200f);
            buttonArea.anchoredPosition = Vector2.zero;
            module.SetButtonArea(buttonArea);
            Canvas.ForceUpdateCanvases();
            yield return null;
            Assert.AreEqual(module, eventSystem.currentInputModule);

            Vector2 start = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            Assert.IsTrue(RectTransformUtility.RectangleContainsScreenPoint(buttonArea, start));
            float before = controller.OrbitDistance;
            BeginTouch(2, start, queueEventOnly: true, screen: touchscreen);
            yield return null;
            Assert.IsTrue(eventSystem.IsPointerOverGameObject(2));
            MoveTouch(2, start + new Vector2(Screen.width * 0.4f, 0f), queueEventOnly: true, screen: touchscreen);
            yield return null;
            Assert.AreEqual(before, controller.OrbitDistance, 0.0001f);
        }

        public override void TearDown()
        {
            if (uiObject != null)
            {
                Object.Destroy(uiObject);
            }

            builder.Dispose();
            base.TearDown();
        }
    }
}
