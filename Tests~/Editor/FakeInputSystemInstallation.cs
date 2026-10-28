using Gley.CameraSystem.Editor;

namespace Gley.CameraSystem.Tests.EditMode
{
    public class FakeInputSystemInstallation : IInputSystemInstallation
    {
        public bool IsInstalled { get; }

        public FakeInputSystemInstallation(bool isInstalled)
        {
            IsInstalled = isInstalled;
        }
    }
}
