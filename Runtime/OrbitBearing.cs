using System.Collections.Generic;
using UnityEngine;

namespace Gley.CameraSystem
{
    public class OrbitBearing
    {
        private readonly List<Vector3> positions = new List<Vector3>();
        private readonly List<float> distances = new List<float>();

        private float length;

        public Vector3 Centre { get; private set; }

        public void Rebuild(IReadOnlyList<Vector3> samplePositions, IReadOnlyList<float> sampleDistances, float orbitLength)
        {
            positions.Clear();
            distances.Clear();
            length = 0f;
            Centre = Vector3.zero;

            if (samplePositions == null || sampleDistances == null || samplePositions.Count != sampleDistances.Count || samplePositions.Count < 2 || orbitLength <= 0f)
            {
                return;
            }

            float minimumX = float.PositiveInfinity;
            float maximumX = float.NegativeInfinity;
            float minimumZ = float.PositiveInfinity;
            float maximumZ = float.NegativeInfinity;

            for (int index = 0; index < samplePositions.Count; index++)
            {
                Vector3 position = samplePositions[index];
                positions.Add(position);
                distances.Add(sampleDistances[index]);
                minimumX = Mathf.Min(minimumX, position.x);
                maximumX = Mathf.Max(maximumX, position.x);
                minimumZ = Mathf.Min(minimumZ, position.z);
                maximumZ = Mathf.Max(maximumZ, position.z);
            }

            Centre = new Vector3((minimumX + maximumX) * 0.5f, 0f, (minimumZ + maximumZ) * 0.5f);
            length = orbitLength;
        }

        public float BearingOfLocalPoint(Vector3 localPoint)
        {
            float degrees = Mathf.Atan2(localPoint.x - Centre.x, Centre.z - localPoint.z) * Mathf.Rad2Deg;
            return WrapBearing(degrees);
        }

        public bool TryGetDistanceAtBearing(float bearing, float nearDistance, out float distance)
        {
            distance = 0f;

            if (length <= 0f)
            {
                return false;
            }

            float radians = WrapBearing(bearing) * Mathf.Deg2Rad;
            float rayX = Mathf.Sin(radians);
            float rayZ = -Mathf.Cos(radians);
            float wrappedNearDistance = WrapDistance(nearDistance);
            float nearestSeparation = float.PositiveInfinity;
            bool found = false;

            for (int index = 0; index < positions.Count; index++)
            {
                float crossingDistance;
                if (!TryGetSegmentIntersection(index, rayX, rayZ, out crossingDistance))
                {
                    continue;
                }

                float separation = Mathf.Abs(crossingDistance - wrappedNearDistance);
                separation = Mathf.Min(separation, length - separation);
                if (!found || separation < nearestSeparation)
                {
                    distance = crossingDistance;
                    nearestSeparation = separation;
                    found = true;
                }
            }

            return found;
        }

        public int CountCrossings(float bearing)
        {
            if (length <= 0f)
            {
                return 0;
            }

            float radians = WrapBearing(bearing) * Mathf.Deg2Rad;
            float rayX = Mathf.Sin(radians);
            float rayZ = -Mathf.Cos(radians);
            int count = 0;

            for (int index = 0; index < positions.Count; index++)
            {
                float crossingDistance;
                if (TryGetSegmentIntersection(index, rayX, rayZ, out crossingDistance))
                {
                    count++;
                }
            }

            return count;
        }

        public float WrapBearing(float degrees)
        {
            float wrapped = Mathf.Repeat(degrees + 180f, 360f) - 180f;
            if (wrapped <= -180f)
            {
                wrapped += 360f;
            }

            return wrapped;
        }

        private float WrapDistance(float distance)
        {
            return Mathf.Repeat(distance, length);
        }

        private bool TryGetSegmentIntersection(int index, float rayX, float rayZ, out float distance)
        {
            distance = 0f;

            int nextIndex = index + 1;
            if (nextIndex == positions.Count)
            {
                nextIndex = 0;
            }

            Vector3 start = positions[index];
            Vector3 end = positions[nextIndex];
            double edgeX = (double)end.x - start.x;
            double edgeZ = (double)end.z - start.z;
            double determinant = rayX * edgeZ - rayZ * edgeX;

            if (System.Math.Abs(determinant) < 0.000000000001d)
            {
                return false;
            }

            double offsetX = (double)start.x - Centre.x;
            double offsetZ = (double)start.z - Centre.z;
            double rayDistance = (offsetX * edgeZ - offsetZ * edgeX) / determinant;
            double segmentFraction = (offsetX * rayZ - offsetZ * rayX) / determinant;

            if (rayDistance < 0d || segmentFraction < 0d || segmentFraction >= 1d)
            {
                return false;
            }

            float startDistance = distances[index];
            float endDistance = length;
            if (nextIndex != 0)
            {
                endDistance = distances[nextIndex];
            }

            distance = WrapDistance(Mathf.Lerp(startDistance, endDistance, (float)segmentFraction));
            return true;
        }
    }
}
