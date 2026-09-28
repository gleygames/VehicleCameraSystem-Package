using UnityEngine;

namespace Gley.CameraSystem.Editor
{
    public class HandleDragUndo
    {
        private readonly VehicleCameraWindowContext context;

        private int recordedControl;

        public HandleDragUndo(VehicleCameraWindowContext windowContext)
        {
            context = windowContext;
        }

        public void ReleaseFinishedDrag()
        {
            if (GUIUtility.hotControl == 0)
            {
                recordedControl = 0;
            }
        }

        public void RecordHandleChange(string undoName)
        {
            int hotControl = GUIUtility.hotControl;
            if (hotControl == 0)
            {
                context.RecordProfileChange(undoName);
                return;
            }

            if (hotControl != recordedControl)
            {
                context.RecordProfileChange(undoName);
                recordedControl = hotControl;
            }
        }

        public void CompleteHandleChange()
        {
            context.CompleteProfileChange();
        }
    }
}
