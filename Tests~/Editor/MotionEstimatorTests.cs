using NUnit.Framework;
using UnityEngine;

namespace Gley.CameraSystem.Tests.EditMode
{
    public class MotionEstimatorTests
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
            estimator.Configure(target, Vector3.zero);
        }

        [Test]
        public void TransformOnly_ConstantVelocity10_SpeedConvergesTo10()
        {
            Assert.AreEqual(0.05f, target.EstimateFilterHalfLife, 0.0001f);
            estimator.UpdateMotionEstimate(1f / 60f);
            for (int index = 0; index < 90; index++)
            {
                rootObject.transform.position += Vector3.forward * (10f / 60f);
                estimator.UpdateMotionEstimate(1f / 60f);
            }

            Assert.AreEqual(10f, estimator.Speed, 0.05f);
            Assert.AreEqual(10f, estimator.WorldVelocity.z, 0.05f);
            Assert.IsFalse(estimator.IsStationary(0.5f));
        }

        [Test]
        public void SuppliedSpeed_OverridesTransformEstimate()
        {
            estimator.UpdateMotionEstimate(0.02f);
            rootObject.transform.position += Vector3.forward * 0.2f;
            target.SetSuppliedSpeed(27f);
            target.SetSuppliedAcceleration(new Vector3(2f, 0f, -3f));
            estimator.UpdateMotionEstimate(0.02f);

            Assert.AreEqual(27f, estimator.Speed, 0.0001f);
            Assert.AreEqual(new Vector3(2f, 0f, -3f), estimator.LocalAcceleration);
            Assert.Greater(estimator.WorldVelocity.z, 0f);
            Assert.Less(estimator.WorldVelocity.z, 10f);
        }

        [Test]
        public void ZeroScaledDelta_OutputsHold()
        {
            target.SetSuppliedSpeed(12f);
            target.SetSuppliedAcceleration(new Vector3(1f, 2f, 3f));
            estimator.UpdateMotionEstimate(0.02f);
            Vector3 velocity = estimator.WorldVelocity;
            Vector3 acceleration = estimator.LocalAcceleration;
            float speed = estimator.Speed;
            float yawRate = estimator.YawRate;

            rootObject.transform.SetPositionAndRotation(new Vector3(100f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f));
            target.SetSuppliedSpeed(99f);
            target.SetSuppliedAcceleration(new Vector3(9f, 8f, 7f));
            estimator.UpdateMotionEstimate(0f);

            Assert.AreEqual(velocity, estimator.WorldVelocity);
            Assert.AreEqual(acceleration, estimator.LocalAcceleration);
            Assert.AreEqual(speed, estimator.Speed);
            Assert.AreEqual(yawRate, estimator.YawRate);
        }

        [Test]
        public void Teleport_ResetThenNextFrame_NoSpeedSpike()
        {
            estimator.UpdateMotionEstimate(0.02f);
            rootObject.transform.position += Vector3.forward * 0.2f;
            estimator.UpdateMotionEstimate(0.02f);

            rootObject.transform.position = new Vector3(1000f, 0f, 0f);
            estimator.Reset();
            estimator.UpdateMotionEstimate(0.02f);

            Assert.AreEqual(0f, estimator.Speed, 0.0001f);
            Assert.AreEqual(Vector3.zero, estimator.WorldVelocity);
            Assert.AreEqual(Vector3.zero, estimator.LocalAcceleration);
            Assert.AreEqual(0f, estimator.YawRate, 0.0001f);
        }

        [Test]
        public void ShiftOrigin_NoSpeedSpike()
        {
            estimator.UpdateMotionEstimate(0.02f);
            for (int index = 0; index < 40; index++)
            {
                rootObject.transform.position += Vector3.forward * 0.2f;
                estimator.UpdateMotionEstimate(0.02f);
            }

            Vector3 offset = new Vector3(-5000f, 0f, 3000f);
            rootObject.transform.position += offset;
            estimator.ShiftOrigin(offset);
            rootObject.transform.position += Vector3.forward * 0.2f;
            estimator.UpdateMotionEstimate(0.02f);

            Assert.AreEqual(10f, estimator.Speed, 0.05f);
            Assert.AreEqual(0f, estimator.LocalAcceleration.magnitude, 0.5f);
        }

        [Test]
        public void TurningRight_YawRatePositive()
        {
            estimator.UpdateMotionEstimate(1f / 60f);
            for (int index = 0; index < 60; index++)
            {
                rootObject.transform.Rotate(Vector3.up, 90f / 60f, Space.World);
                estimator.UpdateMotionEstimate(1f / 60f);
            }

            Assert.AreEqual(90f, estimator.YawRate, 0.5f);
        }

        [Test]
        public void SeatPointOffset_RotationAddsLateralAcceleration()
        {
            estimator.Configure(target, Vector3.forward * 2f);
            estimator.UpdateMotionEstimate(1f / 60f);
            for (int index = 0; index < 120; index++)
            {
                rootObject.transform.Rotate(Vector3.up, 90f / 60f, Space.World);
                estimator.UpdateMotionEstimate(1f / 60f);
            }

            float expectedCentripetal = Mathf.Pow(90f * Mathf.Deg2Rad, 2f) * 2f;
            Assert.AreEqual(expectedCentripetal, estimator.LocalAcceleration.magnitude, 0.1f);
            Assert.Less(estimator.LocalAcceleration.z, -4.8f);
        }

        [Test]
        public void VariableFrameTimes_SpeedWithinFivePercent()
        {
            estimator.UpdateMotionEstimate(1f / 30f);
            for (int index = 0; index < 120; index++)
            {
                float deltaTime;
                if (index % 2 == 0)
                {
                    deltaTime = 1f / 30f;
                }
                else
                {
                    deltaTime = 1f / 144f;
                }

                rootObject.transform.position += Vector3.forward * (20f * deltaTime);
                estimator.UpdateMotionEstimate(deltaTime);
                if (index > 20)
                {
                    Assert.AreEqual(20f, estimator.Speed, 1f);
                }
            }
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(targetObject);
            Object.DestroyImmediate(rootObject);
            Object.DestroyImmediate(profile);
        }
    }
}
