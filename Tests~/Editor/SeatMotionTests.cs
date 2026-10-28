using NUnit.Framework;
using UnityEngine;

namespace Gley.CameraSystem.Tests.EditMode
{
    public class SeatMotionTests
    {
        private const float FrameTime = 0.02f;
        private const float Tolerance = 0.0001f;

        [Test]
        public void SumOfModules_ClampedToEnvelopeOnce()
        {
            SeatSettings seat = new SeatSettings();
            seat.Configure(Vector3.zero, new Vector3(0.06f, 0.04f, 0.08f));
            SeatMotionTestModule firstModule = new SeatMotionTestModule(new Vector3(-0.2f, 0.2f, -0.2f));
            SeatMotionTestModule secondModule = new SeatMotionTestModule(new Vector3(0.15f, -0.15f, 0.15f));
            SeatMotion motion = new SeatMotion();
            motion.Configure(null, seat, new InteriorSettings());
            motion.AddModule(firstModule);
            motion.AddModule(secondModule);
            SeatMotionSignals signals = new SeatMotionSignals(Vector3.zero, 0f, FrameTime);

            Vector3 offset = motion.UpdateSeatMotion(signals);
            Assert.AreEqual(-0.05f, offset.x, Tolerance);
            Assert.AreEqual(0.04f, offset.y, Tolerance);
            Assert.AreEqual(-0.05f, offset.z, Tolerance);

            firstModule.Configure(new Vector3(1f, 1f, 1f));
            secondModule.Configure(Vector3.zero);
            offset = motion.UpdateSeatMotion(signals);
            Assert.AreEqual(0.06f, offset.x, Tolerance);
            Assert.AreEqual(0.04f, offset.y, Tolerance);
            Assert.AreEqual(0.08f, offset.z, Tolerance);
        }

        [Test]
        public void OffsetsNeverRotate()
        {
            GameObject body = new GameObject("Seat Body");
            try
            {
                body.transform.SetPositionAndRotation(new Vector3(3f, 2f, -5f), Quaternion.Euler(0f, 40f, 0f));
                SeatSettings seat = new SeatSettings();
                seat.Configure(new Vector3(-0.5f, 1.8f, 2f), new Vector3(0.06f, 0.04f, 0.08f));
                SeatMotion motion = new SeatMotion();
                motion.Configure(null, seat, new InteriorSettings());
                SeatMotionSignals signals = new SeatMotionSignals(Vector3.forward * 4f, 5f, FrameTime);
                InteriorView view = new InteriorView();
                view.Configure(new InteriorSettings());
                Quaternion before = view.ComputeRotation(body.transform.rotation, 0f, 1f);

                Vector3 offset = Vector3.zero;
                for (int frame = 0; frame < 100; frame++)
                {
                    offset = motion.UpdateSeatMotion(signals);
                }

                Vector3 eye = view.ComputeEyePosition(body.transform, seat, offset);
                Quaternion after = view.ComputeRotation(body.transform.rotation, 0f, 1f);
                Assert.Less(Vector3.Distance(body.transform.TransformPoint(seat.EyeLocalPosition + offset), eye), Tolerance);
                Assert.Less(Quaternion.Angle(before, after), Tolerance);
                Assert.AreEqual(-0.08f, offset.z, Tolerance);
            }
            finally
            {
                Object.DestroyImmediate(body);
            }
        }
    }
}
