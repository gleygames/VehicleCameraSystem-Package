using UnityEditor;
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

        public void RecordHandleChange(Object target, string undoName)
        {
            int hotControl = GUIUtility.hotControl;
            if (hotControl == 0)
            {
                Undo.RecordObject(target, undoName);
                return;
            }

            if (hotControl != recordedControl)
            {
                Undo.RecordObject(target, undoName);
                recordedControl = hotControl;
            }
        }

        public void CompleteHandleChange()
        {
            context.CompleteProfileChange();
        }

        public void CompleteHandleChange(Object target)
        {
            EditorUtility.SetDirty(target);
            context.MarkIssuesDirty();
        }
    }
}
