using System.Collections;
using Gley.CameraSystem.Input;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace Gley.CameraSystem.Tests.Input
{
    public class VehicleCameraInputTests : InputTestFixture
    {
        private const string ActionsPath = "Assets/Gley/VehicleCameraSystem/Input/VehicleCameraInputActions.inputactions";
        private const float TraceFrameDeltaTime = 1f / 60f;
        private const float TraceTolerance = 0.001f;

        private InputTestProfileBuilder builder;
        private InputActionAsset actions;
        private Keyboard keyboard;
        private Mouse mouse;
        private VehicleProfile profile;
        private Transform body;
        private VehicleCameraTarget target;
        private Camera camera;
        private CameraSystemController controller;
        private VehicleCameraInput companion;

        public override void Setup()
        {
            base.Setup();
            keyboard = InputSystem.AddDevice<Keyboard>();
            mouse = InputSystem.AddDevice<Mouse>();
            actions = UnityEditor.AssetDatabase.LoadAssetAtPath<InputActionAsset>(ActionsPath);
            builder = new InputTestProfileBuilder();
            profile = builder.CreateProfile(1f, 2f, 2f);
            body = builder.CreateBody("Vehicle");
            target = builder.CreateTarget(body, profile);
            camera = builder.CreateCamera("Camera");
            controller = builder.CreateController("Controller", camera, target);
            companion = builder.CreateCompanion("Companion", controller, actions);
            controller.Activate();
        }

        [UnityTest]
        public IEnumerator HoldD_SendsPositiveOrbitIntent_ReleaseSendsZero()
        {
            yield return null;
            float distanceBeforeHold = controller.OrbitDistance;
            Press(keyboard.dKey);
            yield return null;
            yield return null;
            float distanceWhileHeld = controller.OrbitDistance;
            Assert.AreNotEqual(distanceBeforeHold, distanceWhileHeld);

            Release(keyboard.dKey);
            yield return null;
            float distanceAfterRelease = controller.OrbitDistance;
            yield return null;
            float distanceStable = controller.OrbitDistance;
            Assert.AreEqual(distanceAfterRelease, distanceStable, 0.0001f);
        }

        [UnityTest]
        public IEnumerator RightMouseDrag_SendsNormalizedDrag()
        {
            yield return null;
            float distanceBeforeDrag = controller.OrbitDistance;
            Vector3 positionBeforeDrag = camera.transform.position;
            Press(mouse.rightButton);
            Set(mouse.delta, new Vector2(40f, 0f));
            yield return null;
            float expectedStep = 40f / Screen.width * builder.DrivingPreset.OrbitMovement.DragOrbit * controller.OrbitSensitivity;
            Assert.AreEqual(expectedStep, Mathf.Abs(controller.OrbitDistance - distanceBeforeDrag), 0.0001f);

            yield return null;
            Assert.AreNotEqual(positionBeforeDrag, camera.transform.position);
        }

        [UnityTest]
        public IEnumerator Scroll_SendsPinch()
        {
            yield return null;
            float zoomBeforeScroll = controller.ZoomOffset;
            Vector3 positionBeforeScroll = camera.transform.position;
            Set(mouse.scroll, new Vector2(0f, 1f));
            yield return null;
            float expectedZoom = zoomBeforeScroll + 0.5f;
            Assert.AreEqual(expectedZoom, controller.ZoomOffset, 0.0001f);

            yield return null;
            Assert.AreEqual(expectedZoom, controller.ZoomOffset, 0.0001f);
            Assert.AreNotEqual(positionBeforeScroll, camera.transform.position);
        }

        [UnityTest]
        public IEnumerator Scroll_LargeDeviceDelta_IsLimitedToOneUnit()
        {
            yield return null;
            float zoomBeforeScroll = controller.ZoomOffset;
            Set(mouse.scroll, new Vector2(0f, 120f));
            yield return null;
            Assert.AreEqual(zoomBeforeScroll + 0.5f, controller.ZoomOffset, 0.0001f);
        }

        [UnityTest]
        public IEnumerator R_OrbitReset_PlayerSource()
        {
            Press(keyboard.dKey);
            yield return null;
            yield return null;
            yield return null;
            Release(keyboard.dKey);
            yield return null;
            float distanceAfterMove = controller.OrbitDistance;

            Press(keyboard.rKey);
            yield return null;
            Release(keyboard.rKey);
            yield return null;
            float distanceAfterReset = controller.OrbitDistance;

            Assert.AreNotEqual(distanceAfterMove, distanceAfterReset);
        }

        [UnityTest]
        public IEnumerator TwoPlayersWithPlayerInput_EachDrivesOwnCamera()
        {
            Gamepad gamepadOne = InputSystem.AddDevice<Gamepad>();
            Gamepad gamepadTwo = InputSystem.AddDevice<Gamepad>();

            Transform bodyTwo = builder.CreateBody("VehicleTwo");
            VehicleCameraTarget targetTwo = builder.CreateTarget(bodyTwo, profile);
            Camera cameraTwo = builder.CreateCamera("CameraTwo");
            CameraSystemController controllerTwo = builder.CreateController("ControllerTwo", cameraTwo, targetTwo);
            VehicleCameraInput companionTwo = builder.CreateCompanion("CompanionTwo", controllerTwo, null);
            controllerTwo.Activate();

            PlayerInput playerInputOne = companion.gameObject.AddComponent<PlayerInput>();
            playerInputOne.actions = Object.Instantiate(actions);
            playerInputOne.defaultActionMap = "Camera";
            PlayerInput playerInputTwo = companionTwo.gameObject.AddComponent<PlayerInput>();
            playerInputTwo.actions = Object.Instantiate(actions);
            playerInputTwo.defaultActionMap = "Camera";
            yield return null;

            playerInputOne.SwitchCurrentControlScheme("Gamepad", gamepadOne);
            playerInputTwo.SwitchCurrentControlScheme("Gamepad", gamepadTwo);
            companion.SetPlayerInput(playerInputOne);
            companionTwo.SetPlayerInput(playerInputTwo);
            yield return null;

            Vector3 positionOneBefore = camera.transform.position;
            Vector3 positionTwoBefore = cameraTwo.transform.position;

            Set(gamepadOne.rightStick, new Vector2(1f, 0f));
            yield return null;
            yield return null;

            Vector3 positionOneAfter = camera.transform.position;
            Vector3 positionTwoAfter = cameraTwo.transform.position;

            Assert.AreNotEqual(positionOneBefore, positionOneAfter);
            Assert.AreEqual(positionTwoBefore, positionTwoAfter);
        }

        [UnityTest]
        public IEnumerator CompanionAndDirectApi_SameCameraTrace()
        {
            Transform directBody = builder.CreateBody("VehicleDirect");
            VehicleCameraTarget directTarget = builder.CreateTarget(directBody, profile);
            Camera directCamera = builder.CreateCamera("CameraDirect");
            CameraSystemController directController = builder.CreateController("ControllerDirect", directCamera, directTarget);
            directController.Activate();
            controller.SetUpdateMode(CameraUpdateMode.Manual);
            directController.SetUpdateMode(CameraUpdateMode.Manual);
            yield return null;

            StepTraceFrame(directController, 0f);
            AssertSameCameraPosition(directCamera, "start");
            Vector3 startPosition = camera.transform.position;

            Press(keyboard.dKey);
            for (int frame = 0; frame < 5; frame++)
            {
                yield return null;
                StepTraceFrame(directController, 1f);
                AssertSameCameraPosition(directCamera, $"held frame {frame}");
            }

            Assert.Greater(Vector3.Distance(startPosition, camera.transform.position), TraceTolerance);

            Release(keyboard.dKey);
            for (int frame = 0; frame < 3; frame++)
            {
                yield return null;
                StepTraceFrame(directController, 0f);
                AssertSameCameraPosition(directCamera, $"released frame {frame}");
            }
        }

        private void StepTraceFrame(CameraSystemController directController, float horizontal)
        {
            directController.SetHeldIntent(horizontal, 0f, 0f);
            controller.UpdateCameraFrame(TraceFrameDeltaTime);
            directController.UpdateCameraFrame(TraceFrameDeltaTime);
        }

        private void AssertSameCameraPosition(Camera directCamera, string phase)
        {
            float distance = Vector3.Distance(camera.transform.position, directCamera.transform.position);
            Assert.Less(distance, TraceTolerance, phase);
        }

        public override void TearDown()
        {
            builder.Dispose();
            base.TearDown();
        }
    }
}
