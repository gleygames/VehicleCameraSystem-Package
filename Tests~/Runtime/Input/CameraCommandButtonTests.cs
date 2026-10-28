using Gley.CameraSystem.Input;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace Gley.CameraSystem.Tests.Input
{
    public class CameraCommandButtonTests : InputTestFixture
    {
        private const string ActionsPath = "Assets/Gley/VehicleCameraSystem/Input/VehicleCameraInputActions.inputactions";

        private InputTestProfileBuilder builder;
        private CameraSystemController controller;
        private VehicleCameraInput companion;
        private GameObject buttonObject;
        private CameraCommandButton button;

        public override void Setup()
        {
            base.Setup();
            InputActionAsset actions = UnityEditor.AssetDatabase.LoadAssetAtPath<InputActionAsset>(ActionsPath);
            builder = new InputTestProfileBuilder();
            VehicleProfile profile = builder.CreateProfile(1f, 2f, 2f);
            Transform body = builder.CreateBody("Vehicle");
            VehicleCameraTarget target = builder.CreateTarget(body, profile);
            Camera camera = builder.CreateCamera("Camera");
            controller = builder.CreateController("Controller", camera, target);
            companion = builder.CreateCompanion("Companion", controller, actions);
            controller.Activate();
            buttonObject = new GameObject("Camera Button");
            button = buttonObject.AddComponent<CameraCommandButton>();
            button.Configure(companion, CameraButtonAction.OrbitReset);
        }

        [Test]
        public void Click_OrbitReset_SendsPlayerReset()
        {
            CameraCommandResult result = button.Click();

            Assert.AreEqual(CameraCommandResult.Accepted, result);
        }

        [Test]
        public void Click_DuringLock_Rejected()
        {
            controller.SetPlayerControlLocked(true);

            CameraCommandResult result = button.Click();

            Assert.AreEqual(CameraCommandResult.PlayerControlLocked, result);
        }

        [Test]
        public void MissingCompanion_LogsOneWarning_NoException()
        {
            button.Configure(null, CameraButtonAction.OrbitReset);
            LogAssert.Expect(LogType.Warning, "Camera Button: Camera Command Button has no Vehicle Camera Input companion.");
            PointerEventData eventData = new PointerEventData(null);

            button.OnPointerClick(eventData);
            button.OnPointerClick(eventData);
            LogAssert.NoUnexpectedReceived();
        }

        public override void TearDown()
        {
            Object.Destroy(buttonObject);
            builder.Dispose();
            base.TearDown();
        }
    }
}
