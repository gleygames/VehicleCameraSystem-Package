using Gley.CameraSystem.Editor;
using NUnit.Framework;
using UnityEditor;

namespace Gley.CameraSystem.Tests.EditMode
{
    public class VehicleCameraWindowTests
    {
        [Test]
        public void OpenWindow_NoExceptions()
        {
            VehicleCameraWindow window = EditorWindow.GetWindow<VehicleCameraWindow>();

            Assert.IsNotNull(window);
            window.Repaint();
            window.Close();
        }
    }
}
