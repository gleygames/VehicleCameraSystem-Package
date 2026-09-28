using Gley.Common;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Gley.CameraSystem.Input
{
    public class CameraCommandButton : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private VehicleCameraInput companion;
        [SerializeField] private CameraButtonAction action;
        private bool hasWarnedMissingCompanion;

        public VehicleCameraInput Companion => companion;
        public CameraButtonAction Action => action;

        public void Configure(VehicleCameraInput cameraInput, CameraButtonAction buttonAction)
        {
            companion = cameraInput;
            action = buttonAction;
            hasWarnedMissingCompanion = false;
        }

        public CameraCommandResult Click()
        {
            if (companion == null)
            {
                if (!hasWarnedMissingCompanion)
                {
                    hasWarnedMissingCompanion = true;
                    CustomLogger.LogWarning($"{name}: Camera Command Button has no Vehicle Camera Input companion.", this);
                }

                return CameraCommandResult.NotActive;
            }

            return companion.IssueButtonCommand(action);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Click();
        }
    }
}
