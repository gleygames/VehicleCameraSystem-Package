using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Gley.CameraSystem.Tests.PlayMode
{
    public class CameraOwnershipMarkerTests
    {
        private CameraTestRig rig;

        [SetUp]
        public void SetUp()
        {
            rig = new CameraTestRig();
        }

        [UnityTest]
        public IEnumerator Marker_AddedOnActivate_RemovedOnDeactivate()
        {
            VehicleCameraTarget target = rig.CreateTarget(rig.CreateBody("Truck"), rig.CreateProfile(1f, 3f, 3f));
            Camera camera = rig.CreateCamera("Camera");
            CameraSystemController controller = rig.CreateController("Controller", camera, target, CameraTestRig.FixedView);

            Assert.IsNull(camera.GetComponent<CameraOwnershipMarker>());
            Assert.AreEqual(CameraCommandResult.Accepted, controller.Activate());
            CameraOwnershipMarker marker = camera.GetComponent<CameraOwnershipMarker>();

            Assert.IsNotNull(marker);
            Assert.AreSame(controller, marker.Owner);
            Assert.AreEqual(HideFlags.HideInInspector, marker.hideFlags);

            controller.Deactivate();
            yield return null;

            Assert.IsNull(camera.GetComponent<CameraOwnershipMarker>());
        }

        [UnityTest]
        public IEnumerator OwnerDestroyed_MarkerNoLongerBlocks()
        {
            VehicleCameraTarget target = rig.CreateTarget(rig.CreateBody("Truck"), rig.CreateProfile(1f, 3f, 3f));
            Camera camera = rig.CreateCamera("Camera");
            CameraSystemController first = rig.CreateController("First Controller", camera, target, CameraTestRig.FixedView);
            CameraSystemController second = rig.CreateController("Second Controller", camera, target, CameraTestRig.InteriorView);

            Assert.AreEqual(CameraCommandResult.Accepted, first.Activate());
            Object.Destroy(first.gameObject);
            yield return null;

            Assert.AreEqual(CameraCommandResult.Accepted, second.Activate());
            yield return null;

            CameraOwnershipMarker[] markers = camera.GetComponents<CameraOwnershipMarker>();
            Assert.AreEqual(1, markers.Length);
            Assert.AreSame(second, markers[0].Owner);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            rig.Dispose();
            yield return null;
        }
    }
}
