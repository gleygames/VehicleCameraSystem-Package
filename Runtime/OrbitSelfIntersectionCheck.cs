using UnityEngine;

namespace Gley.CameraSystem
{
    public class OrbitSelfIntersectionCheck
    {
        private const float BoxTolerance = 0.0001f;
        private const int InitialCapacity = 64;

        private Vector2[] points = new Vector2[0];
        private float[] edgeMinimumX = new float[0];
        private float[] edgeMaximumX = new float[0];
        private float[] edgeMinimumY = new float[0];
        private float[] edgeMaximumY = new float[0];
        private float[] groupMinimumX = new float[0];
        private float[] groupMaximumX = new float[0];
        private float[] groupMinimumY = new float[0];
        private float[] groupMaximumY = new float[0];
        private int[] pointSegments = new int[0];
        private int[] groupStarts = new int[1];
        private int pointCount;
        private int groupCount;

        public int PointCount => pointCount;

        public void Clear()
        {
            pointCount = 0;
            groupCount = 0;
        }

        public void Reserve(int expectedPointCount)
        {
            EnsureCapacity(expectedPointCount);
        }

        public void AddPoint(Vector2 point, int segmentIndex)
        {
            EnsureCapacity(pointCount + 1);
            points[pointCount] = point;
            pointSegments[pointCount] = segmentIndex;
            pointCount++;
        }

        public bool HasSelfIntersection()
        {
            int edgeCount = pointCount - 1;
            if (edgeCount < 1)
            {
                return false;
            }

            BuildEdgeBoxes(edgeCount);
            BuildGroups(edgeCount);
            for (int firstGroup = 0; firstGroup < groupCount; firstGroup++)
            {
                for (int secondGroup = firstGroup; secondGroup < groupCount; secondGroup++)
                {
                    if (!DoGroupBoxesOverlap(firstGroup, secondGroup))
                    {
                        continue;
                    }

                    if (HasIntersectionBetweenGroups(firstGroup, secondGroup, edgeCount))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void EnsureCapacity(int requiredCount)
        {
            if (requiredCount <= points.Length)
            {
                return;
            }

            int capacity = Mathf.Max(points.Length * 2, InitialCapacity);
            while (capacity < requiredCount)
            {
                capacity *= 2;
            }

            System.Array.Resize(ref points, capacity);
            System.Array.Resize(ref edgeMinimumX, capacity);
            System.Array.Resize(ref edgeMaximumX, capacity);
            System.Array.Resize(ref edgeMinimumY, capacity);
            System.Array.Resize(ref edgeMaximumY, capacity);
            System.Array.Resize(ref groupMinimumX, capacity);
            System.Array.Resize(ref groupMaximumX, capacity);
            System.Array.Resize(ref groupMinimumY, capacity);
            System.Array.Resize(ref groupMaximumY, capacity);
            System.Array.Resize(ref pointSegments, capacity);
            System.Array.Resize(ref groupStarts, capacity + 1);
        }

        private void BuildEdgeBoxes(int edgeCount)
        {
            for (int edgeIndex = 0; edgeIndex < edgeCount; edgeIndex++)
            {
                Vector2 start = points[edgeIndex];
                Vector2 end = points[edgeIndex + 1];
                edgeMinimumX[edgeIndex] = Mathf.Min(start.x, end.x);
                edgeMaximumX[edgeIndex] = Mathf.Max(start.x, end.x);
                edgeMinimumY[edgeIndex] = Mathf.Min(start.y, end.y);
                edgeMaximumY[edgeIndex] = Mathf.Max(start.y, end.y);
            }
        }

        private void BuildGroups(int edgeCount)
        {
            groupCount = 0;
            for (int edgeIndex = 0; edgeIndex < edgeCount; edgeIndex++)
            {
                if (edgeIndex == 0 || pointSegments[edgeIndex + 1] != pointSegments[edgeIndex])
                {
                    groupStarts[groupCount] = edgeIndex;
                    groupMinimumX[groupCount] = edgeMinimumX[edgeIndex];
                    groupMaximumX[groupCount] = edgeMaximumX[edgeIndex];
                    groupMinimumY[groupCount] = edgeMinimumY[edgeIndex];
                    groupMaximumY[groupCount] = edgeMaximumY[edgeIndex];
                    groupCount++;
                    continue;
                }

                int group = groupCount - 1;
                groupMinimumX[group] = Mathf.Min(groupMinimumX[group], edgeMinimumX[edgeIndex]);
                groupMaximumX[group] = Mathf.Max(groupMaximumX[group], edgeMaximumX[edgeIndex]);
                groupMinimumY[group] = Mathf.Min(groupMinimumY[group], edgeMinimumY[edgeIndex]);
                groupMaximumY[group] = Mathf.Max(groupMaximumY[group], edgeMaximumY[edgeIndex]);
            }

            groupStarts[groupCount] = edgeCount;
        }

        private bool DoGroupBoxesOverlap(int firstGroup, int secondGroup)
        {
            return groupMinimumX[firstGroup] <= groupMaximumX[secondGroup] + BoxTolerance
                && groupMinimumX[secondGroup] <= groupMaximumX[firstGroup] + BoxTolerance
                && groupMinimumY[firstGroup] <= groupMaximumY[secondGroup] + BoxTolerance
                && groupMinimumY[secondGroup] <= groupMaximumY[firstGroup] + BoxTolerance;
        }

        private bool HasIntersectionBetweenGroups(int firstGroup, int secondGroup, int edgeCount)
        {
            int firstEnd = groupStarts[firstGroup + 1];
            int secondEnd = groupStarts[secondGroup + 1];
            for (int firstEdge = groupStarts[firstGroup]; firstEdge < firstEnd; firstEdge++)
            {
                if (!DoesEdgeOverlapGroup(firstEdge, secondGroup))
                {
                    continue;
                }

                int secondStart = groupStarts[secondGroup];
                if (firstGroup == secondGroup)
                {
                    secondStart = firstEdge + 1;
                }

                for (int secondEdge = secondStart; secondEdge < secondEnd; secondEdge++)
                {
                    if (secondEdge == firstEdge + 1 || firstEdge == 0 && secondEdge == edgeCount - 1)
                    {
                        continue;
                    }

                    if (!DoEdgeBoxesOverlap(firstEdge, secondEdge))
                    {
                        continue;
                    }

                    if (DoSegmentsIntersect(points[firstEdge], points[firstEdge + 1], points[secondEdge], points[secondEdge + 1]))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool DoesEdgeOverlapGroup(int edge, int group)
        {
            return edgeMinimumX[edge] <= groupMaximumX[group] + BoxTolerance
                && groupMinimumX[group] <= edgeMaximumX[edge] + BoxTolerance
                && edgeMinimumY[edge] <= groupMaximumY[group] + BoxTolerance
                && groupMinimumY[group] <= edgeMaximumY[edge] + BoxTolerance;
        }

        private bool DoEdgeBoxesOverlap(int firstEdge, int secondEdge)
        {
            return edgeMinimumX[firstEdge] <= edgeMaximumX[secondEdge] + BoxTolerance
                && edgeMinimumX[secondEdge] <= edgeMaximumX[firstEdge] + BoxTolerance
                && edgeMinimumY[firstEdge] <= edgeMaximumY[secondEdge] + BoxTolerance
                && edgeMinimumY[secondEdge] <= edgeMaximumY[firstEdge] + BoxTolerance;
        }

        private bool DoSegmentsIntersect(Vector2 firstStart, Vector2 firstEnd, Vector2 secondStart, Vector2 secondEnd)
        {
            float firstStartSide = CalculateCross(firstStart, firstEnd, secondStart);
            float firstEndSide = CalculateCross(firstStart, firstEnd, secondEnd);
            float secondStartSide = CalculateCross(secondStart, secondEnd, firstStart);
            float secondEndSide = CalculateCross(secondStart, secondEnd, firstEnd);

            if (firstStartSide == 0f && IsPointOnSegment(firstStart, firstEnd, secondStart))
            {
                return true;
            }

            if (firstEndSide == 0f && IsPointOnSegment(firstStart, firstEnd, secondEnd))
            {
                return true;
            }

            if (secondStartSide == 0f && IsPointOnSegment(secondStart, secondEnd, firstStart))
            {
                return true;
            }

            if (secondEndSide == 0f && IsPointOnSegment(secondStart, secondEnd, firstEnd))
            {
                return true;
            }

            bool firstSegmentSeparatesSecondSegment = (firstStartSide > 0f && firstEndSide < 0f) || (firstStartSide < 0f && firstEndSide > 0f);
            bool secondSegmentSeparatesFirstSegment = (secondStartSide > 0f && secondEndSide < 0f) || (secondStartSide < 0f && secondEndSide > 0f);
            return firstSegmentSeparatesSecondSegment && secondSegmentSeparatesFirstSegment;
        }

        private float CalculateCross(Vector2 lineStart, Vector2 lineEnd, Vector2 point)
        {
            Vector2 line = lineEnd - lineStart;
            Vector2 offset = point - lineStart;
            return line.x * offset.y - line.y * offset.x;
        }

        private bool IsPointOnSegment(Vector2 segmentStart, Vector2 segmentEnd, Vector2 point)
        {
            float minimumX = Mathf.Min(segmentStart.x, segmentEnd.x);
            float maximumX = Mathf.Max(segmentStart.x, segmentEnd.x);
            float minimumY = Mathf.Min(segmentStart.y, segmentEnd.y);
            float maximumY = Mathf.Max(segmentStart.y, segmentEnd.y);
            return point.x >= minimumX && point.x <= maximumX && point.y >= minimumY && point.y <= maximumY;
        }
    }
}
