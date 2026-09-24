using System.Collections.Generic;

namespace Gley.CameraSystem
{
    public class VehicleProfileValidationReport
    {
        private readonly List<VehicleProfileIssue> issues = new List<VehicleProfileIssue>();

        public IReadOnlyList<VehicleProfileIssue> Issues => issues;
        public bool HasErrors => issues.Count > 0;

        public void AddIssue(VehicleProfileIssueCode code, int itemId, string itemName)
        {
            issues.Add(new VehicleProfileIssue(code, itemId, itemName));
        }

        public bool TryGetIssue(VehicleProfileIssueCode code, out VehicleProfileIssue issue)
        {
            for (int index = 0; index < issues.Count; index++)
            {
                if (issues[index].Code == code)
                {
                    issue = issues[index];
                    return true;
                }
            }

            issue = default;
            return false;
        }
    }
}
