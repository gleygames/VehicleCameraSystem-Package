using System.Collections.Generic;
using UnityEngine;

namespace Gley.CameraSystem
{
    public class WatchPointCurve
    {
        private readonly List<WatchPointKey> keys = new List<WatchPointKey>();

        public void Rebuild(IReadOnlyList<WatchPointKey> sourceKeys)
        {
            keys.Clear();

            if (sourceKeys == null)
            {
                return;
            }

            if (keys.Capacity < sourceKeys.Count)
            {
                keys.Capacity = sourceKeys.Count;
            }

            for (int sourceIndex = 0; sourceIndex < sourceKeys.Count; sourceIndex++)
            {
                WatchPointKey key = sourceKeys[sourceIndex];
                int insertionIndex = keys.Count;
                keys.Add(key);

                while (insertionIndex > 0 && keys[insertionIndex - 1].NormalizedProgress > key.NormalizedProgress)
                {
                    keys[insertionIndex] = keys[insertionIndex - 1];
                    insertionIndex--;
                }

                keys[insertionIndex] = key;
            }
        }

        public Vector3 Evaluate(float normalizedProgress)
        {
            if (keys.Count == 0)
            {
                return Vector3.zero;
            }

            if (keys.Count == 1)
            {
                return keys[0].Point;
            }

            float progress = Mathf.Repeat(normalizedProgress, 1f);
            int firstIndex = keys.Count - 1;

            for (int index = 0; index < keys.Count; index++)
            {
                if (progress < keys[index].NormalizedProgress)
                {
                    break;
                }

                firstIndex = index;
            }

            int secondIndex = (firstIndex + 1) % keys.Count;
            WatchPointKey first = keys[firstIndex];
            WatchPointKey second = keys[secondIndex];

            if (progress == first.NormalizedProgress)
            {
                return first.Point;
            }

            float firstProgress = first.NormalizedProgress;
            float secondProgress = second.NormalizedProgress;

            if (secondIndex == 0)
            {
                secondProgress += 1f;
            }

            if (progress < firstProgress)
            {
                progress += 1f;
            }

            float segmentProgress = (progress - firstProgress) / (secondProgress - firstProgress);
            int previousIndex = (firstIndex - 1 + keys.Count) % keys.Count;
            int nextIndex = (secondIndex + 1) % keys.Count;
            Vector3 previousPoint = keys[previousIndex].Point;
            Vector3 firstPoint = first.Point;
            Vector3 secondPoint = second.Point;
            Vector3 nextPoint = keys[nextIndex].Point;
            float previousChord = Mathf.Sqrt(Vector3.Distance(previousPoint, firstPoint));
            float currentChord = Mathf.Sqrt(Vector3.Distance(firstPoint, secondPoint));
            float nextChord = Mathf.Sqrt(Vector3.Distance(secondPoint, nextPoint));

            if (previousChord == 0f || currentChord == 0f || nextChord == 0f)
            {
                return Vector3.LerpUnclamped(firstPoint, secondPoint, segmentProgress);
            }

            float segmentLength = secondProgress - firstProgress;
            float previousLength = first.NormalizedProgress - keys[previousIndex].NormalizedProgress;
            float nextLength = keys[nextIndex].NormalizedProgress - second.NormalizedProgress;

            if (previousLength <= 0f)
            {
                previousLength += 1f;
            }

            if (nextLength <= 0f)
            {
                nextLength += 1f;
            }

            float startRate = Mathf.Min(previousChord / previousLength, currentChord / segmentLength);
            float endRate = Mathf.Min(currentChord / segmentLength, nextChord / nextLength);
            float startSlope = startRate * segmentLength / currentChord;
            float endSlope = endRate * segmentLength / currentChord;
            float squaredProgress = segmentProgress * segmentProgress;
            float curvedProgress = (startSlope + endSlope - 2f) * squaredProgress * segmentProgress
                + (3f - 2f * startSlope - endSlope) * squaredProgress
                + startSlope * segmentProgress;
            float t0 = 0f;
            float t1 = previousChord;
            float t2 = t1 + currentChord;
            float t3 = t2 + nextChord;
            float t = t1 + currentChord * curvedProgress;
            Vector3 a1 = Vector3.LerpUnclamped(previousPoint, firstPoint, (t - t0) / (t1 - t0));
            Vector3 a2 = Vector3.LerpUnclamped(firstPoint, secondPoint, (t - t1) / (t2 - t1));
            Vector3 a3 = Vector3.LerpUnclamped(secondPoint, nextPoint, (t - t2) / (t3 - t2));
            Vector3 b1 = Vector3.LerpUnclamped(a1, a2, (t - t0) / (t2 - t0));
            Vector3 b2 = Vector3.LerpUnclamped(a2, a3, (t - t1) / (t3 - t1));
            return Vector3.LerpUnclamped(b1, b2, (t - t1) / (t2 - t1));
        }
    }
}
