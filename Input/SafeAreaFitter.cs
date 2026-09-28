using UnityEngine;

namespace Gley.CameraSystem.Input
{
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform rectTransform;
        private Rect lastSafeArea;
        private Vector2 lastScreenSize;

        private void OnEnable()
        {
            rectTransform = GetComponent<RectTransform>();
            lastScreenSize = Vector2.zero;
            UpdateSafeAreaVisuals();
        }

        private void Update()
        {
            UpdateSafeAreaVisuals();
        }

        public void ApplySafeArea(Rect safeArea, Vector2 screenSize)
        {
            if (rectTransform == null)
            {
                rectTransform = GetComponent<RectTransform>();
            }

            if (screenSize.x <= 0f || screenSize.y <= 0f)
            {
                return;
            }

            Rect clipped = new Rect(
                Mathf.Clamp(safeArea.xMin, 0f, screenSize.x),
                Mathf.Clamp(safeArea.yMin, 0f, screenSize.y),
                0f,
                0f);
            clipped.xMax = Mathf.Clamp(safeArea.xMax, clipped.xMin, screenSize.x);
            clipped.yMax = Mathf.Clamp(safeArea.yMax, clipped.yMin, screenSize.y);
            rectTransform.anchorMin = new Vector2(clipped.xMin / screenSize.x, clipped.yMin / screenSize.y);
            rectTransform.anchorMax = new Vector2(clipped.xMax / screenSize.x, clipped.yMax / screenSize.y);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            lastSafeArea = safeArea;
            lastScreenSize = screenSize;
        }

        private void UpdateSafeAreaVisuals()
        {
            Rect safeArea = Screen.safeArea;
            Vector2 screenSize = new Vector2(Screen.width, Screen.height);
            if (safeArea == lastSafeArea && screenSize == lastScreenSize)
            {
                return;
            }

            ApplySafeArea(safeArea, screenSize);
        }
    }
}
