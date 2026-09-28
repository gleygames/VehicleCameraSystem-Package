using System.Collections.Generic;
using Gley.Common;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Gley.CameraSystem.Input
{
    public class VehicleCameraInput : MonoBehaviour
    {
        private const string ShippedActionsPath = "Assets/Gley/VehicleCameraSystem/Input/VehicleCameraInputActions.inputactions";
        private const string CameraMapName = "Camera";

        [SerializeField] private CameraSystemController cameraController;
        [SerializeField] private InputActionAsset actions;
        [SerializeField] private PlayerInput playerInput;
        private InputActionMap cameraActionMap;
        private InputAction orbitKeyboardAction;
        private InputAction heightKeyboardAction;
        private InputAction zoomKeyboardAction;
        private InputAction orbitStickAction;
        private InputAction zoomTriggerAction;
        private InputAction orbitButtonAction;
        private InputAction pointerDeltaAction;
        private InputAction scrollAction;
        private InputAction nextViewAction;
        private InputAction orbitResetAction;
        private InputAction fullResetAction;
        private InputAction nextPointAction;
        private InputAction previousPointAction;
        [SerializeField, Range(0f, 0.9f)] private float stickDeadZone = 0.15f;
        private bool hasWarnedMissingMap;

        public CameraSystemController CameraController => cameraController;
        public InputActionAsset Actions => actions;
        public PlayerInput PlayerInput => playerInput;
        public float StickDeadZone => stickDeadZone;

        private void OnEnable()
        {
            BindActions();
            EnableActions();
        }

        private void Update()
        {
            if (cameraController == null)
            {
                return;
            }

            if (cameraActionMap == null)
            {
                WarnMissingMapOnce();
                return;
            }

            UpdateHeldIntent();
            UpdateOrbitDrag();
            UpdateScroll();
        }

        public void Configure(CameraSystemController controller, InputActionAsset actionsAsset)
        {
            cameraController = controller;
            actions = actionsAsset;
            RebindIfEnabled();
        }

        public void SetPlayerInput(PlayerInput input)
        {
            playerInput = input;
            RebindIfEnabled();
        }

        public void SetStickDeadZone(float value)
        {
            stickDeadZone = Mathf.Clamp(value, 0f, 0.9f);
        }

        private void BindActions()
        {
            InputActionAsset source = actions;
            if (playerInput != null && playerInput.actions != null)
            {
                source = playerInput.actions;
            }

            if (source == null)
            {
                return;
            }

            cameraActionMap = source.FindActionMap(CameraMapName);
            if (cameraActionMap == null)
            {
                return;
            }

            orbitKeyboardAction = cameraActionMap.FindAction("OrbitKeyboard");
            heightKeyboardAction = cameraActionMap.FindAction("HeightKeyboard");
            zoomKeyboardAction = cameraActionMap.FindAction("ZoomKeyboard");
            orbitStickAction = cameraActionMap.FindAction("OrbitStick");
            zoomTriggerAction = cameraActionMap.FindAction("ZoomTrigger");
            orbitButtonAction = cameraActionMap.FindAction("OrbitButton");
            pointerDeltaAction = cameraActionMap.FindAction("PointerDelta");
            scrollAction = cameraActionMap.FindAction("Scroll");
            nextViewAction = cameraActionMap.FindAction("NextView");
            orbitResetAction = cameraActionMap.FindAction("OrbitReset");
            fullResetAction = cameraActionMap.FindAction("FullReset");
            nextPointAction = cameraActionMap.FindAction("NextPoint");
            previousPointAction = cameraActionMap.FindAction("PreviousPoint");
            SubscribeButtons();
        }

        private void EnableActions()
        {
            if (cameraActionMap != null)
            {
                cameraActionMap.Enable();
            }
        }

        private void WarnMissingMapOnce()
        {
            if (hasWarnedMissingMap)
            {
                return;
            }

            hasWarnedMissingMap = true;
            CustomLogger.LogWarning($"{name}: no '{CameraMapName}' action map found on the assigned actions; the input companion will not drive the camera.", this);
        }

        private void UpdateHeldIntent()
        {
            float horizontal = ReadAxis(orbitKeyboardAction);
            float vertical = ReadAxis(heightKeyboardAction);
            float zoom = ReadAxis(zoomKeyboardAction);
            Vector2 stick = ReadVector2(orbitStickAction);
            Vector2 deadZonedStick = ApplyRadialDeadZone(stick, stickDeadZone);
            horizontal = Mathf.Clamp(horizontal + deadZonedStick.x, -1f, 1f);
            vertical = Mathf.Clamp(vertical + deadZonedStick.y, -1f, 1f);
            zoom = Mathf.Clamp(zoom + ReadAxis(zoomTriggerAction), -1f, 1f);
            cameraController.SetHeldIntent(horizontal, vertical, zoom);
        }

        private void UpdateOrbitDrag()
        {
            if (orbitButtonAction == null || pointerDeltaAction == null)
            {
                return;
            }

            if (!orbitButtonAction.IsPressed())
            {
                return;
            }

            Vector2 delta = pointerDeltaAction.ReadValue<Vector2>();
            if (delta == Vector2.zero)
            {
                return;
            }

            Vector2 normalizedDelta = new Vector2(delta.x / Screen.width, delta.y / Screen.height);
            cameraController.AddDrag(normalizedDelta);
        }

        private void UpdateScroll()
        {
            if (scrollAction == null)
            {
                return;
            }

            float scrollValue = scrollAction.ReadValue<float>();
            if (scrollValue == 0f)
            {
                return;
            }

            cameraController.AddPinch(scrollValue);
        }

        private void RebindIfEnabled()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            DisableActions();
            UnbindActions();
            BindActions();
            EnableActions();
        }

        private void SubscribeButtons()
        {
            if (nextViewAction != null)
            {
                nextViewAction.performed += OnNextViewPerformed;
            }

            if (orbitResetAction != null)
            {
                orbitResetAction.performed += OnOrbitResetPerformed;
            }

            if (fullResetAction != null)
            {
                fullResetAction.performed += OnFullResetPerformed;
            }

            if (nextPointAction != null)
            {
                nextPointAction.performed += OnNextPointPerformed;
            }

            if (previousPointAction != null)
            {
                previousPointAction.performed += OnPreviousPointPerformed;
            }
        }

        private float ReadAxis(InputAction action)
        {
            if (action == null)
            {
                return 0f;
            }

            return action.ReadValue<float>();
        }

        private Vector2 ReadVector2(InputAction action)
        {
            if (action == null)
            {
                return Vector2.zero;
            }

            return action.ReadValue<Vector2>();
        }

        private Vector2 ApplyRadialDeadZone(Vector2 value, float deadZone)
        {
            float magnitude = value.magnitude;
            if (magnitude <= deadZone)
            {
                return Vector2.zero;
            }

            float scaledMagnitude = Mathf.Clamp01((magnitude - deadZone) / (1f - deadZone));
            return value.normalized * scaledMagnitude;
        }

        private void OnNextViewPerformed(InputAction.CallbackContext context)
        {
            SelectNextView();
        }

        private void OnOrbitResetPerformed(InputAction.CallbackContext context)
        {
            int commandId;
            cameraController.OrbitReset(CommandSource.Player, out commandId);
        }

        private void OnFullResetPerformed(InputAction.CallbackContext context)
        {
            int commandId;
            cameraController.FullReset(CommandSource.Player, out commandId);
        }

        private void OnNextPointPerformed(InputAction.CallbackContext context)
        {
            int commandId;
            cameraController.RequestNextPoint(true, new TravelRequest(TravelDirection.Shortest), CommandSource.Player, out commandId);
        }

        private void OnPreviousPointPerformed(InputAction.CallbackContext context)
        {
            int commandId;
            cameraController.RequestPreviousPoint(true, new TravelRequest(TravelDirection.Shortest), CommandSource.Player, out commandId);
        }

        private void SelectNextView()
        {
            if (cameraController == null || cameraController.Target == null)
            {
                return;
            }

            VehicleProfile profile = cameraController.Target.GetProfile(cameraController.Target.RootIndex);
            if (profile == null)
            {
                return;
            }

            IReadOnlyList<VehicleViewEntry> views = profile.Views;
            if (views.Count == 0)
            {
                return;
            }

            VehicleViewEntry activeView = cameraController.ActiveView;
            int nextIndex = 0;
            if (activeView != null)
            {
                for (int index = 0; index < views.Count; index++)
                {
                    if (views[index].Id == activeView.Id)
                    {
                        nextIndex = (index + 1) % views.Count;
                        break;
                    }
                }
            }

            cameraController.SelectView(views[nextIndex].Name, CommandSource.Player);
        }

        private void Reset()
        {
            CameraSystemController[] controllers = GetComponents<CameraSystemController>();
            if (controllers.Length == 1)
            {
                cameraController = controllers[0];
            }

#if UNITY_EDITOR
            if (actions == null)
            {
                actions = UnityEditor.AssetDatabase.LoadAssetAtPath<InputActionAsset>(ShippedActionsPath);
            }
#endif
        }

        private void UnbindActions()
        {
            UnsubscribeButtons();
            cameraActionMap = null;
            orbitKeyboardAction = null;
            heightKeyboardAction = null;
            zoomKeyboardAction = null;
            orbitStickAction = null;
            zoomTriggerAction = null;
            orbitButtonAction = null;
            pointerDeltaAction = null;
            scrollAction = null;
            nextViewAction = null;
            orbitResetAction = null;
            fullResetAction = null;
            nextPointAction = null;
            previousPointAction = null;
        }

        private void UnsubscribeButtons()
        {
            if (nextViewAction != null)
            {
                nextViewAction.performed -= OnNextViewPerformed;
            }

            if (orbitResetAction != null)
            {
                orbitResetAction.performed -= OnOrbitResetPerformed;
            }

            if (fullResetAction != null)
            {
                fullResetAction.performed -= OnFullResetPerformed;
            }

            if (nextPointAction != null)
            {
                nextPointAction.performed -= OnNextPointPerformed;
            }

            if (previousPointAction != null)
            {
                previousPointAction.performed -= OnPreviousPointPerformed;
            }
        }

        private void DisableActions()
        {
            if (cameraActionMap != null)
            {
                cameraActionMap.Disable();
            }
        }

        private void OnDisable()
        {
            DisableActions();
            UnbindActions();
        }
    }
}
