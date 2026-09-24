using System;
using UnityEngine;

namespace Gley.CameraSystem
{
    [Serializable]
    public class RenderingSettings
    {
        [SerializeField] private CullingMaskMode cullingMaskMode;
        [SerializeField] private LayerMask replaceMask;
        [SerializeField] private LayerMask addMask;
        [SerializeField] private LayerMask removeMask;
        [SerializeField] private float nearClip = 0.3f;
        [SerializeField] private float farClip = 1000f;
        [SerializeField] private bool overrideNearClip;
        [SerializeField] private bool overrideFarClip;

        public CullingMaskMode CullingMaskMode => cullingMaskMode;
        public LayerMask ReplaceMask => replaceMask;
        public LayerMask AddMask => addMask;
        public LayerMask RemoveMask => removeMask;
        public float NearClip => nearClip;
        public float FarClip => farClip;
        public bool OverrideNearClip => overrideNearClip;
        public bool OverrideFarClip => overrideFarClip;

        public void Configure(CullingMaskMode maskMode, LayerMask replacement, LayerMask addition, LayerMask removal, bool useNearClip, float nearClipDistance, bool useFarClip, float farClipDistance)
        {
            cullingMaskMode = maskMode;
            replaceMask = replacement;
            addMask = addition;
            removeMask = removal;
            overrideNearClip = useNearClip;
            nearClip = nearClipDistance;
            overrideFarClip = useFarClip;
            farClip = farClipDistance;
        }
    }
}
