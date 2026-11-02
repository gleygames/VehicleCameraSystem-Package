using UnityEngine;

namespace Gley.CameraSystem.Editor
{
    internal class GleyColors
    {
        public Color Accent { get; }
        public Color AccentHover { get; }
        public Color AccentText { get; }
        public Color AccentForeground { get; }
        public Color Ok { get; }
        public Color Warning { get; }
        public Color Error { get; }
        public Color Info { get; }
        public Color Background { get; }
        public Color RowBackground { get; }
        public Color RowHover { get; }
        public Color Border { get; }
        public Color Text { get; }
        public Color TextSecondary { get; }
        public Color Handle { get; }
        public Color HandleHover { get; }
        public Color HandleSelected { get; }
        public Color HandlePreview { get; }
        public Color HandleValid { get; }
        public Color HandleInvalid { get; }
        public Color HandleInactive { get; }

        public GleyColors(bool darkSkin)
        {
            if (darkSkin)
            {
                Accent = ToColor("#FFB73B");
                AccentHover = ToColor("#FFC84C");
                AccentForeground = ToColor("#FFB73B");
                Ok = ToColor("#7BC86C");
                Warning = ToColor("#E8D44D");
                Error = ToColor("#FF7B6E");
                Info = ToColor("#7DB4F0");
                Background = ToColor("#383838");
                RowBackground = ToColor("#414141");
                RowHover = ToColor("#4D4D4D");
                Border = ToColor("#232323");
                Text = ToColor("#D2D2D2");
                TextSecondary = ToColor("#B4B4B4");
            }
            else
            {
                Accent = ToColor("#DD9519");
                AccentHover = ToColor("#E8A63A");
                AccentForeground = ToColor("#764700");
                Ok = ToColor("#235C21");
                Warning = ToColor("#5C4D00");
                Error = ToColor("#8C1F17");
                Info = ToColor("#184A84");
                Background = ToColor("#C8C8C8");
                RowBackground = ToColor("#D6D6D6");
                RowHover = ToColor("#E2E2E2");
                Border = ToColor("#999999");
                Text = ToColor("#090909");
                TextSecondary = ToColor("#3C3C3C");
            }
            AccentText = ToColor("#111111");
            Handle = ToColor("#5BC0EB");
            HandleHover = ToColor("#FFFFFF");
            HandleSelected = ToColor("#FFB73B");
            HandlePreview = ToColor("#FFB73B99");
            HandleValid = ToColor("#6EE07A");
            HandleInvalid = ToColor("#FF5A4F");
            HandleInactive = ToColor("#9A9A9A");
        }

        private Color ToColor(string hex)
        {
            Color color;
            ColorUtility.TryParseHtmlString(hex, out color);
            return color;
        }
    }
}
