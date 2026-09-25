using UnityEditor;

namespace Gley.CameraSystem.Editor
{
    public class RenameNotice
    {
        private string oldName;
        private int itemId;
        private bool isVisible;

        public void Show(int renamedItemId, string previousName)
        {
            itemId = renamedItemId;
            oldName = previousName;
            isVisible = true;
        }

        public void Draw(int drawnItemId)
        {
            if (!isVisible || drawnItemId != itemId)
            {
                return;
            }

            EditorGUILayout.HelpBox($"Game code that uses the old name '{oldName}' must be updated; saved player preferences are unaffected.", MessageType.Info);
        }

        public void Clear()
        {
            isVisible = false;
            oldName = null;
            itemId = 0;
        }
    }
}
