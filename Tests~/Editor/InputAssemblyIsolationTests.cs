using System.Reflection;
using NUnit.Framework;

namespace Gley.CameraSystem.Tests.EditMode
{
    public class InputAssemblyIsolationTests
    {
        [Test]
        public void RuntimeAssembly_HasNoReferenceToInputAssembly()
        {
            Assembly runtimeAssembly = typeof(CameraSystemController).Assembly;
            AssemblyName[] referencedAssemblies = runtimeAssembly.GetReferencedAssemblies();
            for (int index = 0; index < referencedAssemblies.Length; index++)
            {
                Assert.AreNotEqual("Gley.CameraSystem.Input", referencedAssemblies[index].Name);
            }
        }
    }
}
