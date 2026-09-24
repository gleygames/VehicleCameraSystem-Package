using UnityEngine;

namespace Gley.CameraSystem
{
    public class CameraOwnershipMarker : MonoBehaviour
    {
        public CameraSystemController Owner { get; private set; }

        public void Configure(CameraSystemController owner)
        {
            Owner = owner;
            hideFlags = HideFlags.HideInInspector;
        }

        public void ClearOwner()
        {
            Owner = null;
        }

        public bool BlocksActivation(CameraSystemController requester, Camera camera)
        {
            if (Owner == null || Owner == requester)
            {
                return false;
            }

            return Owner.IsActive && Owner.AssignedCamera == camera;
        }
    }
}
