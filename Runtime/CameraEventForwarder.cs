using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Gley.CameraSystem
{
    public class CameraEventForwarder : MonoBehaviour
    {
        private readonly List<CameraSystemController> sameObjectControllers = new List<CameraSystemController>();

        [SerializeField] private CameraSystemController controller;
        [SerializeField] private UnityEvent<int, CommandEndResult> commandEnded = new UnityEvent<int, CommandEndResult>();
        [SerializeField] private UnityEvent<int> viewChanged = new UnityEvent<int>();
        [SerializeField] private UnityEvent<bool> noClearPoseChanged = new UnityEvent<bool>();
        [SerializeField] private UnityEvent targetLost = new UnityEvent();
        [SerializeField] private UnityEvent cameraLost = new UnityEvent();
        private CameraSystemController subscribedController;

        public CameraSystemController Controller => controller;
        public UnityEvent<int, CommandEndResult> CommandEnded => commandEnded;
        public UnityEvent<int> ViewChanged => viewChanged;
        public UnityEvent<bool> NoClearPoseChanged => noClearPoseChanged;
        public UnityEvent TargetLost => targetLost;
        public UnityEvent CameraLost => cameraLost;

        private void OnEnable()
        {
            if (controller == null)
            {
                FillControllerFromSameObject();
            }

            SubscribeToController();
        }

        public void SetController(CameraSystemController newController)
        {
            controller = newController;
            if (isActiveAndEnabled)
            {
                SubscribeToController();
            }
        }

        private void FillControllerFromSameObject()
        {
            GetComponents(sameObjectControllers);
            if (sameObjectControllers.Count == 1)
            {
                controller = sameObjectControllers[0];
            }

            sameObjectControllers.Clear();
        }

        private void SubscribeToController()
        {
            if (ReferenceEquals(subscribedController, controller))
            {
                return;
            }

            UnsubscribeFromController();
            if (controller == null)
            {
                return;
            }

            controller.CommandEnded += HandleCommandEnded;
            controller.ViewChanged += HandleViewChanged;
            controller.NoClearPoseChanged += HandleNoClearPoseChanged;
            controller.TargetLost += HandleTargetLost;
            controller.CameraLost += HandleCameraLost;
            subscribedController = controller;
        }

        private void UnsubscribeFromController()
        {
            if (ReferenceEquals(subscribedController, null))
            {
                return;
            }

            subscribedController.CommandEnded -= HandleCommandEnded;
            subscribedController.ViewChanged -= HandleViewChanged;
            subscribedController.NoClearPoseChanged -= HandleNoClearPoseChanged;
            subscribedController.TargetLost -= HandleTargetLost;
            subscribedController.CameraLost -= HandleCameraLost;
            subscribedController = null;
        }

        private void HandleCommandEnded(int commandId, CommandEndResult result)
        {
            commandEnded.Invoke(commandId, result);
        }

        private void HandleViewChanged(int viewId)
        {
            viewChanged.Invoke(viewId);
        }

        private void HandleNoClearPoseChanged(bool hasNoClearPose)
        {
            noClearPoseChanged.Invoke(hasNoClearPose);
        }

        private void HandleTargetLost()
        {
            targetLost.Invoke();
        }

        private void HandleCameraLost()
        {
            cameraLost.Invoke();
        }

        private void Reset()
        {
            FillControllerFromSameObject();
            if (isActiveAndEnabled)
            {
                SubscribeToController();
            }
        }

        private void OnValidate()
        {
            if (isActiveAndEnabled)
            {
                SubscribeToController();
            }
        }

        private void OnDisable()
        {
            UnsubscribeFromController();
        }
    }
}
