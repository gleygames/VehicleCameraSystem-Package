using UnityEditor;

namespace Gley.CameraSystem.Editor.Dev
{
    internal class DefaultPresetDevMenu
    {
        [MenuItem("Tools/GleyDev/CameraSystem/Rebuild Default Presets")]
        private static void RebuildDefaultPresetsMenuItem()
        {
            new DefaultPresetBuilder().RebuildDefaultPresets();
        }
    }
}
