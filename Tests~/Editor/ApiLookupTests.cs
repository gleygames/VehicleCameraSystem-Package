using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Gley.CameraSystem.Tests.EditMode
{
    public class ApiLookupTests
    {
        [SetUp]
        public void SetUp()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [Test]
        public void GetCameraControllers_NoControllerInScene_ReturnsEmpty()
        {
            Assert.That(API.GetCameraControllers(), Is.Empty);
        }

        [Test]
        public void GetCameraControllers_TwoControllers_ReturnsBoth()
        {
            CameraSystemController first = CreateController("First", null);
            CameraSystemController second = CreateController("Second", null);

            Assert.That(API.GetCameraControllers(), Is.EquivalentTo(new CameraSystemController[] { first, second }));
        }

        [Test]
        public void GetCameraController_MatchingCamera_ReturnsItsController()
        {
            Camera firstCamera = new GameObject("First Camera").AddComponent<Camera>();
            Camera secondCamera = new GameObject("Second Camera").AddComponent<Camera>();
            CreateController("First", firstCamera);
            CameraSystemController second = CreateController("Second", secondCamera);

            Assert.That(API.GetCameraController(secondCamera), Is.SameAs(second));
        }

        [Test]
        public void GetCameraController_CameraWithoutController_ReturnsNull()
        {
            Camera assignedCamera = new GameObject("Assigned Camera").AddComponent<Camera>();
            Camera otherCamera = new GameObject("Other Camera").AddComponent<Camera>();
            CreateController("Controller", assignedCamera);

            Assert.That(API.GetCameraController(otherCamera), Is.Null);
        }

        [Test]
        public void GetCameraController_NullCamera_ReturnsNull()
        {
            CreateController("Controller", null);

            Assert.That(API.GetCameraController(null), Is.Null);
        }

        private CameraSystemController CreateController(string name, Camera camera)
        {
            CameraSystemController controller = new GameObject(name).AddComponent<CameraSystemController>();
            controller.Configure(camera, null, null);
            return controller;
        }
    }
}
