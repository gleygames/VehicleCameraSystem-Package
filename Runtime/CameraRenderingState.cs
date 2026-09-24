using UnityEngine;

namespace Gley.CameraSystem
{
    public class CameraRenderingState
    {
        private Camera renderedCamera;
        private float baselineNearClip;
        private float baselineFarClip;
        private int baselineCullingMask;
        private bool isCullingMaskOverridden;
        private bool isNearClipOverridden;
        private bool isFarClipOverridden;
        private bool isRecorded;

        public bool IsRecorded => isRecorded;

        public void RecordBaseline(Camera camera)
        {
            renderedCamera = camera;
            baselineCullingMask = camera.cullingMask;
            baselineNearClip = camera.nearClipPlane;
            baselineFarClip = camera.farClipPlane;
            isCullingMaskOverridden = false;
            isNearClipOverridden = false;
            isFarClipOverridden = false;
            isRecorded = true;
        }

        public void ApplyRenderingSettings(RenderingSettings settings)
        {
            if (!isRecorded || renderedCamera == null || settings == null)
            {
                return;
            }

            ApplyCullingMask(settings);
            ApplyNearClip(settings);
            ApplyFarClip(settings);
        }

        private void ApplyCullingMask(RenderingSettings settings)
        {
            if (settings.CullingMaskMode == CullingMaskMode.Replace)
            {
                SetCullingMask(settings.ReplaceMask.value);
                return;
            }

            if (settings.CullingMaskMode == CullingMaskMode.AddRemove)
            {
                SetCullingMask((baselineCullingMask | settings.AddMask.value) & ~settings.RemoveMask.value);
                return;
            }

            RestoreCullingMask();
        }

        private void SetCullingMask(int mask)
        {
            renderedCamera.cullingMask = mask;
            isCullingMaskOverridden = true;
        }

        private void RestoreCullingMask()
        {
            if (!isCullingMaskOverridden)
            {
                return;
            }

            renderedCamera.cullingMask = baselineCullingMask;
            isCullingMaskOverridden = false;
        }

        private void ApplyNearClip(RenderingSettings settings)
        {
            if (!settings.OverrideNearClip)
            {
                RestoreNearClip();
                return;
            }

            renderedCamera.nearClipPlane = settings.NearClip;
            isNearClipOverridden = true;
        }

        private void RestoreNearClip()
        {
            if (!isNearClipOverridden)
            {
                return;
            }

            renderedCamera.nearClipPlane = baselineNearClip;
            isNearClipOverridden = false;
        }

        private void ApplyFarClip(RenderingSettings settings)
        {
            if (!settings.OverrideFarClip)
            {
                RestoreFarClip();
                return;
            }

            renderedCamera.farClipPlane = settings.FarClip;
            isFarClipOverridden = true;
        }

        private void RestoreFarClip()
        {
            if (!isFarClipOverridden)
            {
                return;
            }

            renderedCamera.farClipPlane = baselineFarClip;
            isFarClipOverridden = false;
        }

        public void Restore()
        {
            if (isRecorded && renderedCamera != null)
            {
                RestoreCullingMask();
                RestoreNearClip();
                RestoreFarClip();
            }

            renderedCamera = null;
            isCullingMaskOverridden = false;
            isNearClipOverridden = false;
            isFarClipOverridden = false;
            isRecorded = false;
        }
    }
}
