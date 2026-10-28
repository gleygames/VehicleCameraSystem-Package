using NUnit.Framework;
using UnityEngine;

namespace Gley.CameraSystem.Tests.EditMode
{
    public class OrbitSelfIntersectionCheckTests
    {
        private OrbitSelfIntersectionCheck check;

        [SetUp]
        public void SetUp()
        {
            check = new OrbitSelfIntersectionCheck();
        }

        [Test]
        public void FigureEight_Detected()
        {
            check.AddPoint(new Vector2(0f, 0f), 0);
            check.AddPoint(new Vector2(2f, 2f), 0);
            check.AddPoint(new Vector2(2f, 0f), 1);
            check.AddPoint(new Vector2(0f, 2f), 2);
            check.AddPoint(new Vector2(0f, 0f), 3);

            Assert.IsTrue(check.HasSelfIntersection());
        }

        [Test]
        public void SquareLoop_NotDetected()
        {
            AddSide(new Vector2(0f, 0f), new Vector2(4f, 0f), 0, true);
            AddSide(new Vector2(4f, 0f), new Vector2(4f, 4f), 1, false);
            AddSide(new Vector2(4f, 4f), new Vector2(0f, 4f), 2, false);
            AddSide(new Vector2(0f, 4f), new Vector2(0f, 0f), 3, false);

            Assert.AreEqual(17, check.PointCount);
            Assert.IsFalse(check.HasSelfIntersection());
        }

        [Test]
        public void CrossingEdgesInDistantSegments_Detected()
        {
            check.AddPoint(new Vector2(0f, 0f), 0);
            check.AddPoint(new Vector2(1.5f, 0f), 0);
            check.AddPoint(new Vector2(3f, 0f), 0);
            check.AddPoint(new Vector2(4f, 0f), 0);
            check.AddPoint(new Vector2(4f, 5f), 1);
            check.AddPoint(new Vector2(4f, 10f), 1);
            check.AddPoint(new Vector2(7f, 10f), 2);
            check.AddPoint(new Vector2(10f, 10f), 2);
            check.AddPoint(new Vector2(10f, 15f), 3);
            check.AddPoint(new Vector2(10f, 20f), 3);
            check.AddPoint(new Vector2(6f, 20f), 4);
            check.AddPoint(new Vector2(2f, 20f), 4);
            check.AddPoint(new Vector2(2f, 5f), 5);
            check.AddPoint(new Vector2(2f, -5f), 5);
            check.AddPoint(new Vector2(1f, -2.5f), 6);
            check.AddPoint(new Vector2(0f, 0f), 6);

            Assert.IsTrue(check.HasSelfIntersection());
        }

        [Test]
        public void TouchingBoxesNoCrossing_NotDetected()
        {
            check.AddPoint(new Vector2(0f, 0f), 0);
            check.AddPoint(new Vector2(2f, 2f), 0);
            check.AddPoint(new Vector2(4f, 4f), 0);
            check.AddPoint(new Vector2(8f, 4f), 1);
            check.AddPoint(new Vector2(8f, -1f), 1);
            check.AddPoint(new Vector2(5f, -1f), 2);
            check.AddPoint(new Vector2(3f, 1f), 2);
            check.AddPoint(new Vector2(1f, -1f), 3);
            check.AddPoint(new Vector2(0f, 0f), 3);

            Assert.IsFalse(check.HasSelfIntersection());
        }

        [Test]
        public void AdjacentAndClosingEdges_Ignored()
        {
            check.AddPoint(new Vector2(0f, 0f), 0);
            check.AddPoint(new Vector2(4f, 0f), 0);
            check.AddPoint(new Vector2(2f, 3f), 1);
            check.AddPoint(new Vector2(0f, 0f), 2);

            Assert.IsFalse(check.HasSelfIntersection());
        }

        private void AddSide(Vector2 start, Vector2 end, int segmentIndex, bool includeStart)
        {
            if (includeStart)
            {
                check.AddPoint(start, segmentIndex);
            }

            for (int step = 1; step <= 4; step++)
            {
                check.AddPoint(Vector2.Lerp(start, end, step / 4f), segmentIndex);
            }
        }
    }
}
