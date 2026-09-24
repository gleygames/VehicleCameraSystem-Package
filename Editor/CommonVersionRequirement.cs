using Gley.Common;
using UnityEditor;

namespace Gley.CameraSystem.Editor
{
    [InitializeOnLoad]
    public class CommonVersionRequirement
    {
        private readonly int _minimumCommonShortVersion = 5;

        static CommonVersionRequirement()
        {
            new CommonVersionRequirement().CheckInstalledCommonVersion();
        }

        public int MinimumCommonShortVersion => _minimumCommonShortVersion;

        public void CheckInstalledCommonVersion()
        {
            Gley.Common.Editor.Version installedVersion = new Gley.Common.Editor.Version();
            CommonVersionComparer comparer = new CommonVersionComparer();
            if (comparer.IsSatisfied(_minimumCommonShortVersion, installedVersion.ShortVersion))
            {
                return;
            }

            CustomLogger.LogError($"Vehicle Camera System needs Gley Common {_minimumCommonShortVersion}+ but found {installedVersion.ShortVersion}. Re-import Vehicle Camera System, or update your other Gley assets.");
        }
    }
}
