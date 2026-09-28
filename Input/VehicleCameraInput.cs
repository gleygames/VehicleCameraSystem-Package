using System.Collections.Generic;
using Gley.Common;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

namespace Gley.CameraSystem.Input
{
    public class VehicleCameraInput : MonoBehaviour
    {
        private const string ShippedActionsPath = "Assets/Gley/VehicleCameraSystem/Input/VehicleCameraInputActions.inputactions";
        private const string CameraMapName = "Camera";

        private readonly TouchGestureRecognizer gestures = new TouchGestureRecognizer();

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
        private InputAction touchContactAction;
        private InputAction nextViewAction;
        private InputAction orbitResetAction;
        private InputAction fullResetAction;
        private InputAction nextPointAction;
        private InputAction previousPointAction;
        [SerializeField] private RectTransform gestureArea;
        [SerializeField] private TouchDoubleTapAction doubleTapAction = TouchDoubleTapAction.OrbitReset;
        [SerializeField, Range(0f, 0.9f)] private float stickDeadZone = 0.15f;
        [SerializeField, Min(0f)] private float dragStartThreshold = 0.01f;
        [SerializeField, Min(0f)] private float doubleTapInterval = 0.3f;
        [SerializeField, Min(0f)] private float doubleTapDistance = 0.02f;
        [SerializeField, Min(0f)] private float scrollPinchPerUnit = 0.1f;
        private bool hasWarnedMissingMap;
        private bool mouseNeedsRelease;
        private bool wasLocked;

        public CameraSystemController CameraController => cameraController;
        public InputActionAsset Actions => actions;
        public PlayerInput PlayerInput => playerInput;
        public RectTransform GestureArea => gestureArea;
        public TouchDoubleTapAction DoubleTapAction => doubleTapAction;
        public float StickDeadZone => stickDeadZone;
        public float ScrollPinchPerUnit => scrollPinchPerUnit;

        private void OnEnable()
        {
            BindActions();
            EnableActions();
            if (cameraController != null)
            {
                cameraController.ViewChanged += OnViewChanged;
            }
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
            UpdateTouchGestures();
        }

        public void Configure(CameraSystemController controller, InputActionAsset actionsAsset)
        {
            if (isActiveAndEnabled && cameraController != null)
            {
                cameraController.ViewChanged -= OnViewChanged;
            }

            cameraController = controller;
            actions = actionsAsset;
            if (isActiveAndEnabled && cameraController != null)
            {
                cameraController.ViewChanged += OnViewChanged;
            }

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

        public void ConfigureGestures(RectTransform area, float dragStart, float tapInterval, float tapDistance, TouchDoubleTapAction action)
        {
            gestureArea = area;
            dragStartThreshold = Mathf.Max(0f, dragStart);
            doubleTapInterval = Mathf.Max(0f, tapInterval);
            doubleTapDistance = Mathf.Max(0f, tapDistance);
            doubleTapAction = action;
            DiscardGestures();
        }

        public CameraCommandResult IssueButtonCommand(CameraButtonAction action)
        {
            if (cameraController == null)
            {
                return CameraCommandResult.NotActive;
            }

            int commandId;
            if (action == CameraButtonAction.NextView)
            {
                return SelectNextView();
            }

            if (action == CameraButtonAction.OrbitReset)
            {
                return cameraController.OrbitReset(CommandSource.Player, out commandId);
            }

            if (action == CameraButtonAction.FullReset)
            {
                return cameraController.FullReset(CommandSource.Player, out commandId);
            }

            if (action == CameraButtonAction.NextPoint)
            {
                return cameraController.RequestNextPoint(true, new TravelRequest(TravelDirection.Shortest), CommandSource.Player, out commandId);
            }

            if (action == CameraButtonAction.PreviousPoint)
            {
                return cameraController.RequestPreviousPoint(true, new TravelRequest(TravelDirection.Shortest), CommandSource.Player, out commandId);
            }

            return CameraCommandResult.NotSupportedInView;
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
            touchContactAction = cameraActionMap.FindAction("TouchContact");
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

        private void UpdateTouchGestures()
        {
            bool locked = cameraController.IsPlayerControlLocked;
            if (locked)
            {
                if (!wasLocked)
                {
                    DiscardGestures();
                }

                wasLocked = true;
                return;
            }

            wasLocked = false;
            gestures.Configure(dragStartThreshold, doubleTapInterval, doubleTapDistance);
            gestures.BeginFrame(Screen.width, Screen.height, Time.unscaledTime);
            Touchscreen touchscreen = Touchscreen.current;
            bool hasActiveTouch = false;
            if (touchContactAction != null && touchContactAction.enabled && touchscreen != null && IsDevicePaired(touchscreen))
            {
                for (int index = 0; index < touchscreen.touches.Count; index++)
                {
                    UnityEngine.InputSystem.Controls.TouchControl touch = touchscreen.touches[index];
                    UnityEngine.InputSystem.TouchPhase phase = touch.phase.ReadValue();
                    if (phase == UnityEngine.InputSystem.TouchPhase.None)
                    {
                        continue;
                    }

                    hasActiveTouch = true;
                    int id = touch.touchId.ReadValue();
                    Vector2 position = touch.position.ReadValue();
                    bool overUi = false;
                    bool inArea = false;
                    if (phase == UnityEngine.InputSystem.TouchPhase.Began)
                    {
                        overUi = IsOverUi(id);
                        inArea = IsInsideGestureArea(position);
                    }

                    gestures.FeedTouch(id, phase, position, overUi, inArea);
                }
            }

            Mouse mouse = Mouse.current;
            if (!hasActiveTouch && mouse != null && IsDevicePaired(mouse))
            {
                bool pressed = mouse.leftButton.isPressed;
                if (mouseNeedsRelease)
                {
                    if (!pressed)
                    {
                        mouseNeedsRelease = false;
                    }
                }
                else
                {
                    Vector2 position = mouse.position.ReadValue();
                    gestures.FeedMouse(pressed, position, IsOverUi(-1), IsInsideGestureArea(position));
                    if (scrollAction != null)
                    {
                        float scrollUnits = Mathf.Clamp(scrollAction.ReadValue<float>(), -1f, 1f);
                        gestures.FeedMouseScroll(scrollUnits * scrollPinchPerUnit, IsOverUi(-1), IsInsideGestureArea(position));
                    }
                }
            }

            gestures.EndFrame();
            if (gestures.Drag != Vector2.zero)
            {
                cameraController.AddDrag(gestures.Drag);
            }

            if (gestures.Pinch != 0f)
            {
                cameraController.AddPinch(gestures.Pinch);
            }

            if (gestures.DoubleTapped)
            {
                int commandId;
                if (doubleTapAction == TouchDoubleTapAction.OrbitReset)
                {
                    cameraController.OrbitReset(CommandSource.Player, out commandId);
                }
                else if (doubleTapAction == TouchDoubleTapAction.FullReset)
                {
                    cameraController.FullReset(CommandSource.Player, out commandId);
                }
            }
        }

        private bool IsDevicePaired(InputDevice device)
        {
            if (playerInput == null)
            {
                return true;
            }

            for (int index = 0; index < playerInput.devices.Count; index++)
            {
                if (playerInput.devices[index] == device)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsOverUi(int pointerId)
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(pointerId);
        }

        private bool IsInsideGestureArea(Vector2 position)
        {
            if (gestureArea == null)
            {
                return true;
            }

            Canvas canvas = gestureArea.GetComponentInParent<Canvas>();
            Camera eventCamera = null;
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                eventCamera = canvas.worldCamera;
            }

            return RectTransformUtility.RectangleContainsScreenPoint(gestureArea, position, eventCamera);
        }

        private void OnViewChanged(int viewId)
        {
            DiscardGestures();
        }

        private void DiscardGestures()
        {
            gestures.Clear();
            Mouse mouse = Mouse.current;
            mouseNeedsRelease = mouse != null && mouse.leftButton.isPressed;
        }

        private void RebindIfEnabled()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            DisableActions();
            UnbindActions();
            DiscardGestures();
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

        private CameraCommandResult SelectNextView()
        {
            if (cameraController == null || cameraController.Target == null)
            {
                return CameraCommandResult.NotActive;
            }

            VehicleProfile profile = cameraController.Target.GetProfile(cameraController.Target.RootIndex);
            if (profile == null)
            {
                return CameraCommandResult.NotActive;
            }

            IReadOnlyList<VehicleViewEntry> views = profile.Views;
            if (views.Count == 0)
            {
                return CameraCommandResult.NotActive;
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

            return cameraController.SelectView(views[nextIndex].Name, CommandSource.Player);
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
            touchContactAction = null;
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
            if (cameraController != null)
            {
                cameraController.ViewChanged -= OnViewChanged;
            }

            DisableActions();
            UnbindActions();
            DiscardGestures();
        }
    }
}
