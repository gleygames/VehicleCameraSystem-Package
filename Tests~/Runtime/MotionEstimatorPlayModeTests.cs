using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Gley.CameraSystem.Tests.PlayMode
{
    public class MotionEstimatorPlayModeTests
    {
        private MotionEstimator estimator;
        private VehicleCameraTarget target;
        private VehicleProfile profile;
        private GameObject rootObject;
        private GameObject targetObject;

        [SetUp]
        public void SetUp()
        {
            rootObject = new GameObject("Root");
            targetObject = new GameObject("Target");
            profile = ScriptableObject.CreateInstance<VehicleProfile>();
            target = targetObject.AddComponent<VehicleCameraTarget>();
            target.Configure(rootObject.transform, profile);
            estimator = new MotionEstimator();
        }

        [Test]
        public void Rigidbody_Present_UsesPointVelocity()
        {
            Rigidbody body = rootObject.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.velocity = Vector3.right * 10f;
            body.angularVelocity = Vector3.up;
            estimator.Configure(target, Vector3.forward * 2f);

            estimator.UpdateMotionEstimate(0.02f);

            Assert.AreEqual(12f, estimator.WorldVelocity.x, 0.01f);
            Assert.AreEqual(12f, estimator.Speed, 0.01f);
            Assert.AreEqual(Mathf.Rad2Deg, estimator.YawRate, 0.01f);
        }

        [Test]
        public void TimeScaleHalf_SpeedMatchesGameSpeed()
        {
            estimator.Configure(target, Vector3.zero);
            float originalTimeScale = Time.timeScale;
            try
            {
                Time.timeScale = 0.5f;
                float unscaledStep = 1f / 60f;
                float scaledStep = unscaledStep * Time.timeScale;
                estimator.UpdateMotionEstimate(scaledStep);
                for (int index = 0; index < 120; index++)
                {
                    rootObject.transform.position += Vector3.forward * (20f * scaledStep);
                    estimator.UpdateMotionEstimate(scaledStep);
                }

                Assert.AreEqual(20f, estimator.Speed, 0.2f);
                Assert.AreEqual(20f, estimator.WorldVelocity.z, 0.2f);
            }
            finally
            {
                Time.timeScale = originalTimeScale;
            }
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(targetObject);
            Object.Destroy(rootObject);
            Object.Destroy(profile);
            yield return null;
        }
    }
}
