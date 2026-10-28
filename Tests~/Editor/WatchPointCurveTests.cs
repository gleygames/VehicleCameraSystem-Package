using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Gley.CameraSystem.Tests.EditMode
{
    public class WatchPointCurveTests
    {
        [Test]
        public void Evaluate_AtEachKey_ReturnsKeyPointExactly()
        {
            List<WatchPointKey> keys = CreateFiveKeys();
            WatchPointCurve curve = new WatchPointCurve();
            curve.Rebuild(keys);

            for (int index = 0; index < keys.Count; index++)
            {
                Assert.Less(Vector3.Distance(keys[index].Point, curve.Evaluate(keys[index].NormalizedProgress)), 0.00001f);
            }
        }

        [Test]
        public void Evaluate_SingleKey_Constant()
        {
            WatchPointCurve curve = new WatchPointCurve();
            Vector3 point = new Vector3(2f, 3f, 4f);
            curve.Rebuild(new List<WatchPointKey> { new WatchPointKey(0.37f, point, 0) });

            Assert.AreEqual(point, curve.Evaluate(0f));
            Assert.AreEqual(point, curve.Evaluate(0.37f));
            Assert.AreEqual(point, curve.Evaluate(0.94f));
        }

        [Test]
        public void Evaluate_TwoKeys_PassesThroughBoth()
        {
            WatchPointCurve curve = new WatchPointCurve();
            Vector3 firstPoint = new Vector3(1f, 2f, 3f);
            Vector3 secondPoint = new Vector3(4f, 5f, 6f);
            curve.Rebuild(new List<WatchPointKey>
            {
                new WatchPointKey(0.1f, firstPoint, 0),
                new WatchPointKey(0.7f, secondPoint, 0)
            });

            Assert.AreEqual(firstPoint, curve.Evaluate(0.1f));
            Assert.AreEqual(secondPoint, curve.Evaluate(0.7f));
            Assert.Less(Vector3.Distance(firstPoint, curve.Evaluate(1.1f)), 0.00001f);
        }

        [Test]
        public void Evaluate_AcrossWrap_ContinuousAtZero()
        {
            WatchPointCurve curve = new WatchPointCurve();
            curve.Rebuild(new List<WatchPointKey>
            {
                new WatchPointKey(0.1f, new Vector3(0f, 0f, 0f), 0),
                new WatchPointKey(0.5f, new Vector3(0.25f, 0f, 0f), 0),
                new WatchPointKey(0.9f, new Vector3(0.25f, 0.25f, 0f), 0)
            });

            Assert.Less(Vector3.Distance(curve.Evaluate(0.9999f), curve.Evaluate(0.0001f)), 0.001f);
        }

        [Test]
        public void Evaluate_DerivativeContinuousAtKeys()
        {
            WatchPointCurve curve = new WatchPointCurve();
            curve.Rebuild(CreateFiveKeys());
            float progress = 0.36f;
            float interval = 0.0001f;
            Vector3 leftSlope = (curve.Evaluate(progress) - curve.Evaluate(progress - interval)) / interval;
            Vector3 rightSlope = (curve.Evaluate(progress + interval) - curve.Evaluate(progress)) / interval;

            Assert.Greater(leftSlope.magnitude, 0.1f);
            Assert.Less((leftSlope - rightSlope).magnitude, leftSlope.magnitude * 0.05f);
        }

        [Test]
        public void Evaluate_CoincidentPoints_NoNaN()
        {
            WatchPointCurve curve = new WatchPointCurve();
            curve.Rebuild(new List<WatchPointKey>
            {
                new WatchPointKey(0.1f, Vector3.zero, 0),
                new WatchPointKey(0.4f, Vector3.zero, 0),
                new WatchPointKey(0.8f, Vector3.right, 0)
            });

            for (int index = 0; index <= 10; index++)
            {
                Vector3 point = curve.Evaluate(index * 0.1f);
                Assert.IsFalse(float.IsNaN(point.x) || float.IsNaN(point.y) || float.IsNaN(point.z));
            }
        }

        [Test]
        public void Evaluate_UnsortedInput_SortedInternally()
        {
            WatchPointCurve curve = new WatchPointCurve();
            Vector3 firstPoint = Vector3.right;
            Vector3 secondPoint = Vector3.up;
            Vector3 thirdPoint = Vector3.forward;
            curve.Rebuild(new List<WatchPointKey>
            {
                new WatchPointKey(0.8f, thirdPoint, 0),
                new WatchPointKey(0.2f, firstPoint, 0),
                new WatchPointKey(0.5f, secondPoint, 0)
            });

            Assert.AreEqual(firstPoint, curve.Evaluate(0.2f));
            Assert.AreEqual(secondPoint, curve.Evaluate(0.5f));
            Assert.AreEqual(thirdPoint, curve.Evaluate(0.8f));
        }

        private List<WatchPointKey> CreateFiveKeys()
        {
            return new List<WatchPointKey>
            {
                new WatchPointKey(0.03f, new Vector3(0f, 0f, 0f), 0),
                new WatchPointKey(0.12f, new Vector3(1f, 1f, 0f), 0),
                new WatchPointKey(0.36f, new Vector3(2f, 0f, 0f), 0),
                new WatchPointKey(0.58f, new Vector3(3f, -1f, 1f), 0),
                new WatchPointKey(0.83f, new Vector3(4f, 0f, 0f), 0)
            };
        }
    }
}
