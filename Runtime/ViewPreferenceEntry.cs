using System;
using UnityEngine;

namespace Gley.CameraSystem
{
    [Serializable]
    public struct ViewPreferenceEntry
    {
        [SerializeField] private OrbitPose rearDefault;
        [SerializeField] private OrbitPose frontDefault;
        [SerializeField] private string profileId;
        [SerializeField] private int viewId;
        [SerializeField] private bool hasRearDefault;
        [SerializeField] private bool hasFrontDefault;

        public OrbitPose RearDefault => rearDefault;
        public OrbitPose FrontDefault => frontDefault;
        public string ProfileId => profileId;
        public int ViewId => viewId;
        public bool HasRearDefault => hasRearDefault;
        public bool HasFrontDefault => hasFrontDefault;

        public ViewPreferenceEntry(string profileId, int viewId, bool hasRearDefault, OrbitPose rearDefault, bool hasFrontDefault, OrbitPose frontDefault)
        {
            this.profileId = profileId;
            this.viewId = viewId;
            this.hasRearDefault = hasRearDefault;
            this.rearDefault = rearDefault;
            this.hasFrontDefault = hasFrontDefault;
            this.frontDefault = frontDefault;
        }
    }
}
