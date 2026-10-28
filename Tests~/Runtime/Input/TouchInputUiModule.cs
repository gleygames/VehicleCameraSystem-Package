using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Gley.CameraSystem.Tests.Input
{
    public class TouchInputUiModule : BaseInputModule
    {
        private RectTransform buttonArea;

        public void SetButtonArea(RectTransform area)
        {
            buttonArea = area;
        }

        public override bool IsPointerOverGameObject(int pointerId)
        {
            if (buttonArea == null || Touchscreen.current == null)
            {
                return false;
            }

            for (int index = 0; index < Touchscreen.current.touches.Count; index++)
            {
                UnityEngine.InputSystem.Controls.TouchControl touch = Touchscreen.current.touches[index];
                if (touch.touchId.ReadValue() == pointerId)
                {
                    return RectTransformUtility.RectangleContainsScreenPoint(buttonArea, touch.position.ReadValue());
                }
            }

            return false;
        }

        public override void Process()
        {
        }
    }
}
