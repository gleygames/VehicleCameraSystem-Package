using System.Collections.Generic;
using UnityEngine;

namespace Gley.CameraSystem
{
    public class CameraCollision
    {
        private const int QueryBufferSize = 32;
        private const int ReservedCastCount = 3;
        private const float SurfaceSkin = 0.01f;
        private const float TieTolerance = 0.02f;
        private const float MinimumCastDistance = 0.0001f;

        private readonly RaycastHit[] hits = new RaycastHit[QueryBufferSize];
        private readonly Collider[] overlaps = new Collider[QueryBufferSize];
        private readonly HalfLifeSmoother smoother = new HalfLifeSmoother();

        private float chosenInward;
        private float chosenUp;
        private float inwardEstimate;
        private float upEstimate;
        private float upStep;
        private int castBudget;
        private int upStepCount;
        private int nextUpStep;
        private bool isSnapPending = true;
        private bool isInwardTaken;
        private bool isUpEstimateTaken;

        public float InwardCorrection { get; private set; }
        public float UpCorrection { get; private set; }
        public int CastsThisFrame { get; private set; }
        public bool HasClearPose { get; private set; } = true;

        public Vector3 UpdateCollisionCorrection(Vector3 desiredPosition, Vector3 watchPoint, Vector3 inwardNormal, Vector3 up, float zoom, float height, VehicleOrbit ranges, CollisionSettings settings, IReadOnlyList<Collider> ownBodyColliders, float deltaTime)
        {
            CastsThisFrame = 0;
            castBudget = Mathf.Max(0, settings.MaximumCasts);
            Vector3 inward = inwardNormal.normalized;
            Vector3 orbitUp = up.normalized;
            float inwardLimit = Mathf.Max(0f, ranges.MaximumZoomOffset - zoom);
            float upLimit = Mathf.Max(0f, ranges.MaximumHeightOffset - height);
            ChooseCorrection(desiredPosition, watchPoint, inward, orbitUp, inwardLimit, upLimit, settings, ownBodyColliders);
            EaseCorrection(deltaTime, settings);
            InwardCorrection = Mathf.Clamp(InwardCorrection, 0f, inwardLimit);
            UpCorrection = Mathf.Clamp(UpCorrection, 0f, upLimit);
            return desiredPosition + inward * InwardCorrection + orbitUp * UpCorrection;
        }

        public void Reset()
        {
            isSnapPending = true;
            chosenInward = 0f;
            chosenUp = 0f;
            InwardCorrection = 0f;
            UpCorrection = 0f;
            CastsThisFrame = 0;
            HasClearPose = true;
        }

        private void ChooseCorrection(Vector3 desiredPosition, Vector3 watchPoint, Vector3 inward, Vector3 up, float inwardLimit, float upLimit, CollisionSettings settings, IReadOnlyList<Collider> ownBodyColliders)
        {
            chosenInward = 0f;
            chosenUp = 0f;
            HasClearPose = true;
            if (castBudget == 0)
            {
                return;
            }

            bool isHit;
            Vector3 hitPoint;
            if (IsClear(desiredPosition, watchPoint, settings, ownBodyColliders, out isHit, out hitPoint))
            {
                return;
            }

            PrepareCandidates(desiredPosition, inward, up, inwardLimit, upLimit, isHit, hitPoint, settings);
            float inwardDisplacement;
            float upDisplacement;
            while (CastsThisFrame < castBudget && TryTakeNextCandidate(out inwardDisplacement, out upDisplacement))
            {
                if (TryChooseCandidate(desiredPosition, watchPoint, inward, up, inwardDisplacement, upDisplacement, settings, ownBodyColliders))
                {
                    return;
                }
            }

            HasClearPose = false;
            ChooseLeastDisplacedCandidate();
        }

        private bool IsClear(Vector3 position, Vector3 watchPoint, CollisionSettings settings, IReadOnlyList<Collider> ownBodyColliders, out bool isHit, out Vector3 hitPoint)
        {
            isHit = false;
            hitPoint = position;
            if (CastsThisFrame >= castBudget)
            {
                return false;
            }

            float radius = Mathf.Max(0f, settings.CameraRadius);
            int mask = settings.CollisionMask;
            Vector3 offset = position - watchPoint;
            float distance = offset.magnitude;
            if (distance > MinimumCastDistance)
            {
                CastsThisFrame++;
                int hitCount = Physics.SphereCastNonAlloc(watchPoint, radius, offset / distance, hits, distance, mask, QueryTriggerInteraction.Ignore);
                if (TryGetNearestHit(hitCount, ownBodyColliders, out hitPoint))
                {
                    isHit = true;
                    return false;
                }
            }

            if (CastsThisFrame >= castBudget)
            {
                return true;
            }

            CastsThisFrame++;
            int overlapCount = Physics.OverlapSphereNonAlloc(position, radius, overlaps, mask, QueryTriggerInteraction.Ignore);
            for (int index = 0; index < overlapCount; index++)
            {
                if (!IsOwnBody(overlaps[index], ownBodyColliders))
                {
                    return false;
                }
            }

            return true;
        }

        private bool TryGetNearestHit(int hitCount, IReadOnlyList<Collider> ownBodyColliders, out Vector3 hitPoint)
        {
            hitPoint = Vector3.zero;
            float nearestDistance = float.MaxValue;
            bool isFound = false;
            for (int index = 0; index < hitCount; index++)
            {
                RaycastHit hit = hits[index];
                if (hit.distance <= 0f || hit.distance >= nearestDistance || IsOwnBody(hit.collider, ownBodyColliders))
                {
                    continue;
                }

                nearestDistance = hit.distance;
                hitPoint = hit.point;
                isFound = true;
            }

            return isFound;
        }

        private bool IsOwnBody(Collider candidate, IReadOnlyList<Collider> ownBodyColliders)
        {
            if (ownBodyColliders == null)
            {
                return false;
            }

            for (int index = 0; index < ownBodyColliders.Count; index++)
            {
                if (ownBodyColliders[index] == candidate)
                {
                    return true;
                }
            }

            return false;
        }

        private void PrepareCandidates(Vector3 desiredPosition, Vector3 inward, Vector3 up, float inwardLimit, float upLimit, bool isHit, Vector3 hitPoint, CollisionSettings settings)
        {
            float clearance = Mathf.Max(0f, settings.CameraRadius) + SurfaceSkin;
            inwardEstimate = 0f;
            upEstimate = 0f;
            if (isHit)
            {
                Vector3 hitOffset = hitPoint - desiredPosition;
                inwardEstimate = Mathf.Clamp(Vector3.Dot(hitOffset, inward) + clearance, 0f, inwardLimit);
                upEstimate = Mathf.Clamp(Vector3.Dot(hitOffset, up) + clearance, 0f, upLimit);
            }

            upStepCount = 0;
            upStep = 0f;
            if (upLimit > 0f)
            {
                upStepCount = Mathf.Max(1, settings.MaximumCasts - ReservedCastCount);
                upStep = upLimit / upStepCount;
            }

            nextUpStep = 1;
            isInwardTaken = inwardEstimate <= 0f;
            isUpEstimateTaken = upEstimate <= 0f;
        }

        private bool TryTakeNextCandidate(out float inwardDisplacement, out float upDisplacement)
        {
            inwardDisplacement = 0f;
            upDisplacement = 0f;
            while (nextUpStep <= upStepCount && upEstimate > 0f && Mathf.Abs(upStep * nextUpStep - upEstimate) <= TieTolerance)
            {
                nextUpStep++;
            }

            bool hasUp = false;
            bool isUpEstimateNext = false;
            float nextUp = 0f;
            if (nextUpStep <= upStepCount)
            {
                hasUp = true;
                nextUp = upStep * nextUpStep;
            }

            if (!isUpEstimateTaken && (!hasUp || upEstimate <= nextUp))
            {
                hasUp = true;
                isUpEstimateNext = true;
                nextUp = upEstimate;
            }

            if (!isInwardTaken && (!hasUp || inwardEstimate <= nextUp + TieTolerance))
            {
                isInwardTaken = true;
                inwardDisplacement = inwardEstimate;
                return true;
            }

            if (!hasUp)
            {
                return false;
            }

            if (isUpEstimateNext)
            {
                isUpEstimateTaken = true;
            }
            else
            {
                nextUpStep++;
            }

            upDisplacement = nextUp;
            return true;
        }

        private bool TryChooseCandidate(Vector3 desiredPosition, Vector3 watchPoint, Vector3 inward, Vector3 up, float inwardDisplacement, float upDisplacement, CollisionSettings settings, IReadOnlyList<Collider> ownBodyColliders)
        {
            bool isHit;
            Vector3 hitPoint;
            Vector3 candidate = desiredPosition + inward * inwardDisplacement + up * upDisplacement;
            if (!IsClear(candidate, watchPoint, settings, ownBodyColliders, out isHit, out hitPoint))
            {
                return false;
            }

            chosenInward = inwardDisplacement;
            chosenUp = upDisplacement;
            return true;
        }

        private void ChooseLeastDisplacedCandidate()
        {
            bool hasUp = false;
            float leastUp = 0f;
            if (upStepCount > 0)
            {
                hasUp = true;
                leastUp = upStep;
            }

            if (upEstimate > 0f && (!hasUp || upEstimate < leastUp))
            {
                hasUp = true;
                leastUp = upEstimate;
            }

            if (inwardEstimate > 0f && (!hasUp || inwardEstimate <= leastUp + TieTolerance))
            {
                chosenInward = inwardEstimate;
                return;
            }

            chosenUp = leastUp;
        }

        private void EaseCorrection(float deltaTime, CollisionSettings settings)
        {
            if (isSnapPending)
            {
                isSnapPending = false;
                InwardCorrection = chosenInward;
                UpCorrection = chosenUp;
                return;
            }

            InwardCorrection = EaseAxis(InwardCorrection, chosenInward, deltaTime, settings);
            UpCorrection = EaseAxis(UpCorrection, chosenUp, deltaTime, settings);
        }

        private float EaseAxis(float current, float target, float deltaTime, CollisionSettings settings)
        {
            float halfLife = settings.EaseOutHalfLife;
            if (target > current)
            {
                halfLife = settings.MoveInHalfLife;
            }

            return smoother.Smooth(current, target, deltaTime, halfLife);
        }
    }
}
