namespace Gley.CameraSystem
{
    public readonly struct VehicleProfileIssue
    {
        public VehicleProfileIssueCode Code { get; }
        public string ItemName { get; }
        public int ItemId { get; }

        public VehicleProfileIssue(VehicleProfileIssueCode code, int itemId, string itemName)
        {
            Code = code;
            ItemId = itemId;
            ItemName = itemName;
        }
    }
}
