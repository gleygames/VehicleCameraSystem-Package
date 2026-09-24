using System.Collections.Generic;
using UnityEngine;

namespace Gley.CameraSystem
{
    public class InwardZoomAnalyzer
    {
        private const float MinimumDerivativeSquared = 0.00000001f;
        private const int SamplesPerSegment = 32;

        public float MaximumSafeInwardZoom(ClosedBezierOrbit orbit)
        {
            if (orbit == null || orbit.ValidationResult != OrbitValidationResult.Valid)
            {
                return float.PositiveInfinity;
            }

            IReadOnlyList<BezierOrbitKnot> knots = orbit.SourceOrbit.Knots;
            float signedArea = CalculateSignedArea(knots);
            if (Mathf.Abs(signedArea) <= MinimumDerivativeSquared)
            {
                return float.PositiveInfinity;
            }

            float winding = Mathf.Sign(signedArea);
            float minimumRadius = float.PositiveInfinity;
            for (int segmentIndex = 0; segmentIndex < knots.Count; segmentIndex++)
            {
                BezierOrbitKnot start = knots[segmentIndex];
                BezierOrbitKnot end = knots[(segmentIndex + 1) % knots.Count];
                Vector3 p0 = start.Anchor;
                Vector3 p1 = start.OutgoingControlPoint;
                Vector3 p2 = end.IncomingControlPoint;
                Vector3 p3 = end.Anchor;
                for (int sampleIndex = 0; sampleIndex <= SamplesPerSegment; sampleIndex++)
                {
                    float t = (float)sampleIndex / SamplesPerSegment;
                    float inverseT = 1f - t;
                    Vector3 firstDerivative = 3f * (inverseT * inverseT * (p1 - p0) + 2f * inverseT * t * (p2 - p1) + t * t * (p3 - p2));
                    Vector3 secondDerivative = 6f * (inverseT * (p2 - 2f * p1 + p0) + t * (p3 - 2f * p2 + p1));
                    float speedSquared = firstDerivative.x * firstDerivative.x + firstDerivative.z * firstDerivative.z;
                    if (speedSquared <= MinimumDerivativeSquared)
                    {
                        continue;
                    }

                    float inwardCurvatureNumerator = winding * (firstDerivative.x * secondDerivative.z - firstDerivative.z * secondDerivative.x);
                    if (inwardCurvatureNumerator <= 0f)
                    {
                        continue;
                    }

                    float radius = speedSquared * Mathf.Sqrt(speedSquared) / inwardCurvatureNumerator;
                    if (radius < minimumRadius)
                    {
                        minimumRadius = radius;
                    }
                }
            }

            return minimumRadius;
        }

        private float CalculateSignedArea(IReadOnlyList<BezierOrbitKnot> knots)
        {
            float signedArea = 0f;
            Vector3 previous = knots[0].Anchor;
            for (int segmentIndex = 0; segmentIndex < knots.Count; segmentIndex++)
            {
                BezierOrbitKnot start = knots[segmentIndex];
                BezierOrbitKnot end = knots[(segmentIndex + 1) % knots.Count];
                Vector3 p0 = start.Anchor;
                Vector3 p1 = start.OutgoingControlPoint;
                Vector3 p2 = end.IncomingControlPoint;
                Vector3 p3 = end.Anchor;
                for (int sampleIndex = 1; sampleIndex <= SamplesPerSegment; sampleIndex++)
                {
                    float t = (float)sampleIndex / SamplesPerSegment;
                    float inverseT = 1f - t;
                    Vector3 current = inverseT * inverseT * inverseT * p0 + 3f * inverseT * inverseT * t * p1 + 3f * inverseT * t * t * p2 + t * t * t * p3;
                    signedArea += previous.x * current.z - current.x * previous.z;
                    previous = current;
                }
            }

            return signedArea;
        }
    }
}
