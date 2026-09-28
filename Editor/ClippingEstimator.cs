using System.Collections.Generic;
using UnityEngine;

namespace Gley.CameraSystem.Editor
{
    public class ClippingEstimator
    {
        private const int BearingCount = 16;
        private const int ExtremeCount = 2;
        private const float StartOverlapDistance = 0.0001f;
        private const float MinimumRayLength = 0.0001f;

        private readonly List<ClippingWarning> warnings = new List<ClippingWarning>();
        private readonly List<Collider> bodyColliders = new List<Collider>();
        private readonly List<Collider> colliders = new List<Collider>();
        private readonly List<int> colliderBodies = new List<int>();
        private readonly float[] zoomExtremes = new float[ExtremeCount];
        private readonly float[] heightExtremes = new float[ExtremeCount];

        public IReadOnlyList<ClippingWarning> Estimate(ChainOrbit orbit, VehicleOrbit rangeOrbit, IReadOnlyList<Transform> models, IReadOnlyList<Matrix4x4> bodyToPreview)
        {
            warnings.Clear();
            if (orbit == null || !orbit.IsClosed || !orbit.HasValidWatchMarkers || rangeOrbit == null || models == null || bodyToPreview == null)
            {
                return warnings;
            }

            CollectColliders(models, bodyToPreview.Count);
            if (colliders.Count == 0)
            {
                return warnings;
            }

            Physics.SyncTransforms();
            zoomExtremes[0] = rangeOrbit.MinimumZoomOffset;
            zoomExtremes[1] = rangeOrbit.MaximumZoomOffset;
            heightExtremes[0] = rangeOrbit.MinimumHeightOffset;
            heightExtremes[1] = rangeOrbit.MaximumHeightOffset;
            Vector3 up = rangeOrbit.OrientationAdjustment * Vector3.up;

            for (int bearingIndex = 0; bearingIndex < BearingCount; bearingIndex++)
            {
                float bearing = orbit.Bearing.WrapBearing(bearingIndex * 360f / BearingCount);
                if (!orbit.Bearing.TryGetDistanceAtBearing(bearing, 0f, out float distance))
                {
                    continue;
                }

                Vector3 basePosition = orbit.EvaluateRootLocalPosition(distance);
                Vector3 inward = orbit.EvaluateRootLocalInwardNormal(distance);
                Vector3 watchPoint = orbit.EvaluateRootLocalWatchPoint(distance);
                EstimateBearing(bearing, basePosition, inward, up, watchPoint, models, bodyToPreview);
            }

            return warnings;
        }

        private void CollectColliders(IReadOnlyList<Transform> models, int bodyCount)
        {
            colliders.Clear();
            colliderBodies.Clear();
            int modelCount = Mathf.Min(models.Count, bodyCount);

            for (int bodyIndex = 0; bodyIndex < modelCount; bodyIndex++)
            {
                Transform model = models[bodyIndex];
                if (model == null || !model.gameObject.scene.IsValid())
                {
                    continue;
                }

                model.GetComponentsInChildren(false, bodyColliders);
                for (int index = 0; index < bodyColliders.Count; index++)
                {
                    Collider bodyCollider = bodyColliders[index];
                    if (bodyCollider.enabled && !bodyCollider.isTrigger)
                    {
                        colliders.Add(bodyCollider);
                        colliderBodies.Add(bodyIndex);
                    }
                }
            }

            bodyColliders.Clear();
        }

        private void EstimateBearing(float bearing, Vector3 basePosition, Vector3 inward, Vector3 up, Vector3 watchPoint, IReadOnlyList<Transform> models, IReadOnlyList<Matrix4x4> bodyToPreview)
        {
            for (int zoomIndex = 0; zoomIndex < ExtremeCount; zoomIndex++)
            {
                if (zoomIndex > 0 && zoomExtremes[zoomIndex] == zoomExtremes[0])
                {
                    continue;
                }

                for (int heightIndex = 0; heightIndex < ExtremeCount; heightIndex++)
                {
                    if (heightIndex > 0 && heightExtremes[heightIndex] == heightExtremes[0])
                    {
                        continue;
                    }

                    float zoom = zoomExtremes[zoomIndex];
                    float height = heightExtremes[heightIndex];
                    Vector3 cameraPosition = basePosition + inward * zoom + up * height;
                    if (TryFindBlockingBody(watchPoint, cameraPosition, models, bodyToPreview, out int bodyIndex))
                    {
                        warnings.Add(new ClippingWarning(bearing, zoom, height, bodyIndex, zoomIndex == 1, heightIndex == 0));
                    }
                }
            }
        }

        private bool TryFindBlockingBody(Vector3 watchPoint, Vector3 cameraPosition, IReadOnlyList<Transform> models, IReadOnlyList<Matrix4x4> bodyToPreview, out int blockingBody)
        {
            blockingBody = -1;
            int rayBody = -1;
            Ray worldRay = default;
            float rayLength = 0f;

            for (int index = 0; index < colliders.Count; index++)
            {
                int bodyIndex = colliderBodies[index];
                if (bodyIndex != rayBody)
                {
                    rayBody = bodyIndex;
                    Matrix4x4 previewToWorld = models[bodyIndex].localToWorldMatrix * bodyToPreview[bodyIndex].inverse;
                    Vector3 worldStart = previewToWorld.MultiplyPoint3x4(watchPoint);
                    Vector3 worldSpan = previewToWorld.MultiplyPoint3x4(cameraPosition) - worldStart;
                    rayLength = worldSpan.magnitude;
                    if (rayLength >= MinimumRayLength)
                    {
                        worldRay = new Ray(worldStart, worldSpan / rayLength);
                    }
                }

                if (rayLength < MinimumRayLength)
                {
                    continue;
                }

                if (colliders[index].Raycast(worldRay, out RaycastHit hit, rayLength) && hit.distance > StartOverlapDistance)
                {
                    blockingBody = bodyIndex;
                    return true;
                }
            }

            return false;
        }
    }
}
