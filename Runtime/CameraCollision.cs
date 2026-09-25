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
        private const float ParallelTolerance = 0.000001f;
        private const int ClearingSearchIterations = 16;

        private readonly List<CollisionCandidate> candidates = new List<CollisionCandidate>(QueryBufferSize);
        private readonly RaycastHit[] hits = new RaycastHit[QueryBufferSize];
        private readonly Collider[] overlaps = new Collider[QueryBufferSize];
        private readonly Collider[] blockers = new Collider[QueryBufferSize];
        private readonly HalfLifeSmoother smoother = new HalfLifeSmoother();

        private Vector3 nearestHitPoint;
        private Vector3 nearestWorldHitPoint;
        private float chosenInward;
        private float chosenUp;
        private float leastBlockedInward;
        private float leastBlockedUp;
        private float leastBlockedLength;
        private float nearestHitDistance;
        private float nearestWorldHitDistance;
        private int castBudget;
        private int blockerCount;
        private bool isSnapPending = true;
        private bool hasLeastBlocked;
        private bool hasWorldObstruction;
        private bool isInsideOwnBody;
        private bool hasNearestHit;
        private bool hasNearestWorldHit;

        public float InwardCorrection { get; private set; }
        public float UpCorrection { get; private set; }
        public int CastsThisFrame { get; private set; }
        public bool HasClearPose { get; private set; } = true;

        public Vector3 UpdateCollisionCorrection(Vector3 desiredPosition, Vector3 watchPoint, Vector3 inwardNormal, Vector3 up, float zoom, float height, VehicleOrbit ranges, CollisionSettings settings, OwnBodyColliderSet ownBodyColliders, float deltaTime)
        {
            CastsThisFrame = 0;
            castBudget = Mathf.Max(0, settings.MaximumCasts);
            Vector3 inward = inwardNormal.normalized;
            Vector3 orbitUp = up.normalized;
            float inwardLimit = Mathf.Max(0f, ranges.MaximumZoomOffset - zoom);
            float outwardLimit = Mathf.Max(0f, zoom - ranges.MinimumZoomOffset);
            float upLimit = Mathf.Max(0f, ranges.MaximumHeightOffset - height);
            ChooseCorrection(desiredPosition, watchPoint, inward, orbitUp, inwardLimit, outwardLimit, upLimit, settings, ownBodyColliders);
            EaseCorrection(deltaTime, settings);
            InwardCorrection = Mathf.Clamp(InwardCorrection, -outwardLimit, inwardLimit);
            UpCorrection = Mathf.Clamp(UpCorrection, 0f, upLimit);
            return desiredPosition + inward * InwardCorrection + orbitUp * UpCorrection;
        }

        public void Suspend()
        {
            isSnapPending = false;
            chosenInward = 0f;
            chosenUp = 0f;
            InwardCorrection = 0f;
            UpCorrection = 0f;
            CastsThisFrame = 0;
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

        private void ChooseCorrection(Vector3 desiredPosition, Vector3 watchPoint, Vector3 inward, Vector3 up, float inwardLimit, float outwardLimit, float upLimit, CollisionSettings settings, OwnBodyColliderSet ownBodyColliders)
        {
            chosenInward = 0f;
            chosenUp = 0f;
            HasClearPose = true;
            hasLeastBlocked = false;
            leastBlockedLength = 0f;
            ClearObstructions();
            if (castBudget == 0)
            {
                return;
            }

            if (IsClear(desiredPosition, watchPoint, 0f, 0f, true, settings, ownBodyColliders))
            {
                return;
            }

            PrepareCandidates(desiredPosition, watchPoint, inward, up, inwardLimit, outwardLimit, upLimit, settings);
            for (int index = 0; index < candidates.Count && CastsThisFrame < castBudget; index++)
            {
                CollisionCandidate candidate = candidates[index];
                Vector3 position = desiredPosition + inward * candidate.InwardOffset + up * candidate.UpOffset;
                if (IsClear(position, watchPoint, candidate.InwardOffset, candidate.UpOffset, false, settings, ownBodyColliders))
                {
                    chosenInward = candidate.InwardOffset;
                    chosenUp = candidate.UpOffset;
                    return;
                }
            }

            HasClearPose = false;
            chosenInward = leastBlockedInward;
            chosenUp = leastBlockedUp;
        }

        private void ClearObstructions()
        {
            hasWorldObstruction = false;
            isInsideOwnBody = false;
            hasNearestHit = false;
            hasNearestWorldHit = false;
            nearestHitDistance = float.MaxValue;
            nearestWorldHitDistance = float.MaxValue;
            blockerCount = 0;
        }

        private bool IsClear(Vector3 position, Vector3 watchPoint, float inwardOffset, float upOffset, bool isDesiredPose, CollisionSettings settings, OwnBodyColliderSet ownBodyColliders)
        {
            if (CastsThisFrame >= castBudget)
            {
                return false;
            }

            float radius = Mathf.Max(0f, settings.CameraRadius);
            int mask = settings.CollisionMask;
            float blockedLength = 0f;
            bool isBlocked = false;
            Vector3 offset = position - watchPoint;
            float distance = offset.magnitude;
            if (distance > MinimumCastDistance)
            {
                CastsThisFrame++;
                int hitCount = Physics.SphereCastNonAlloc(watchPoint, radius, offset / distance, hits, distance, mask, QueryTriggerInteraction.Ignore);
                for (int index = 0; index < hitCount; index++)
                {
                    RaycastHit hit = hits[index];
                    if (hit.distance <= 0f)
                    {
                        continue;
                    }

                    Collider hitCollider = hit.collider;
                    isBlocked = true;
                    blockedLength += MeasureBlockedLength(hitCollider, watchPoint, position);
                    if (isDesiredPose)
                    {
                        ClassifyHit(hit, ownBodyColliders.Contains(hitCollider), position, radius);
                    }
                }
            }

            if (!isBlocked)
            {
                if (CastsThisFrame >= castBudget)
                {
                    return true;
                }

                CastsThisFrame++;
                int overlapCount = Physics.OverlapSphereNonAlloc(position, radius, overlaps, mask, QueryTriggerInteraction.Ignore);
                for (int index = 0; index < overlapCount; index++)
                {
                    Collider overlap = overlaps[index];
                    isBlocked = true;
                    blockedLength += MeasureBlockedLength(overlap, watchPoint, position);
                    if (isDesiredPose)
                    {
                        ClassifyOverlap(overlap, ownBodyColliders.Contains(overlap));
                    }
                }
            }

            if (!isBlocked)
            {
                return true;
            }

            RecordBlockedCandidate(inwardOffset, upOffset, blockedLength);
            return false;
        }

        private float MeasureBlockedLength(Collider blocker, Vector3 start, Vector3 end)
        {
            float lineLength = Vector3.Distance(start, end);
            if (lineLength <= MinimumCastDistance)
            {
                return 0f;
            }

            float enter;
            float exit;
            BoxCollider box = blocker as BoxCollider;
            if (box != null)
            {
                Transform boxTransform = box.transform;
                Vector3 localStart = boxTransform.InverseTransformPoint(start) - box.center;
                Vector3 localEnd = boxTransform.InverseTransformPoint(end) - box.center;
                Vector3 halfSize = box.size * 0.5f;
                if (!ClipSegment(localStart, localEnd, -halfSize, halfSize, out enter, out exit))
                {
                    return 0f;
                }
            }
            else
            {
                Bounds bounds = blocker.bounds;
                if (!ClipSegment(start, end, bounds.min, bounds.max, out enter, out exit))
                {
                    return 0f;
                }
            }

            return (exit - enter) * lineLength;
        }

        private bool ClipSegment(Vector3 start, Vector3 end, Vector3 minimum, Vector3 maximum, out float enter, out float exit)
        {
            enter = 0f;
            exit = 1f;
            Vector3 delta = end - start;
            for (int axis = 0; axis < 3; axis++)
            {
                if (!ClipAxis(start[axis], delta[axis], minimum[axis], maximum[axis], ref enter, ref exit))
                {
                    return false;
                }
            }

            return exit > enter;
        }

        private bool ClipAxis(float start, float delta, float minimum, float maximum, ref float enter, ref float exit)
        {
            if (Mathf.Abs(delta) < ParallelTolerance)
            {
                return start >= minimum && start <= maximum;
            }

            float first = (minimum - start) / delta;
            float second = (maximum - start) / delta;
            if (first > second)
            {
                float swap = first;
                first = second;
                second = swap;
            }

            enter = Mathf.Max(enter, first);
            exit = Mathf.Min(exit, second);
            return enter <= exit;
        }

        private void ClassifyHit(RaycastHit hit, bool isOwnBody, Vector3 position, float radius)
        {
            if (hit.distance < nearestHitDistance)
            {
                nearestHitDistance = hit.distance;
                nearestHitPoint = hit.point;
                hasNearestHit = true;
            }

            if (isOwnBody)
            {
                if (IsWithinReach(hit.collider, position, radius))
                {
                    isInsideOwnBody = true;
                }

                AddBlocker(hit.collider);
                return;
            }

            hasWorldObstruction = true;
            AddBlocker(hit.collider);
            if (hit.distance < nearestWorldHitDistance)
            {
                nearestWorldHitDistance = hit.distance;
                nearestWorldHitPoint = hit.point;
                hasNearestWorldHit = true;
            }
        }

        private bool IsWithinReach(Collider blocker, Vector3 position, float radius)
        {
            BoxCollider box = blocker as BoxCollider;
            if (box == null)
            {
                return blocker.bounds.SqrDistance(position) <= radius * radius;
            }

            Transform boxTransform = box.transform;
            Vector3 halfSize = box.size * 0.5f;
            Vector3 local = boxTransform.InverseTransformPoint(position) - box.center;
            Vector3 closest = new Vector3(Mathf.Clamp(local.x, -halfSize.x, halfSize.x), Mathf.Clamp(local.y, -halfSize.y, halfSize.y), Mathf.Clamp(local.z, -halfSize.z, halfSize.z));
            Vector3 closestWorld = boxTransform.TransformPoint(closest + box.center);
            return (closestWorld - position).sqrMagnitude <= radius * radius;
        }

        private void AddBlocker(Collider blocker)
        {
            if (blockerCount >= blockers.Length)
            {
                return;
            }

            blockers[blockerCount] = blocker;
            blockerCount++;
        }

        private void ClassifyOverlap(Collider overlap, bool isOwnBody)
        {
            if (isOwnBody)
            {
                isInsideOwnBody = true;
                AddBlocker(overlap);
            }
            else
            {
                hasWorldObstruction = true;
                AddBlocker(overlap);
            }
        }

        private void RecordBlockedCandidate(float inwardOffset, float upOffset, float blockedLength)
        {
            if (hasLeastBlocked && blockedLength >= leastBlockedLength - TieTolerance)
            {
                return;
            }

            hasLeastBlocked = true;
            leastBlockedLength = blockedLength;
            leastBlockedInward = inwardOffset;
            leastBlockedUp = upOffset;
        }

        private void PrepareCandidates(Vector3 desiredPosition, Vector3 watchPoint, Vector3 inward, Vector3 up, float inwardLimit, float outwardLimit, float upLimit, CollisionSettings settings)
        {
            candidates.Clear();
            float radius = Mathf.Max(0f, settings.CameraRadius);
            float clearance = radius + SurfaceSkin;
            float clearingUp;
            bool hasClearingUp = TryGetClearingUp(desiredPosition, watchPoint, up, upLimit, radius, out clearingUp);
            float inwardEstimate = 0f;
            float upEstimate = 0f;
            if (hasNearestWorldHit)
            {
                inwardEstimate = Mathf.Clamp(Vector3.Dot(nearestWorldHitPoint - desiredPosition, inward) + clearance, 0f, inwardLimit);
            }

            if (hasNearestHit)
            {
                upEstimate = Mathf.Clamp(Vector3.Dot(nearestHitPoint - desiredPosition, up) + clearance, 0f, upLimit);
            }

            if (hasWorldObstruction && inwardEstimate > 0f)
            {
                InsertCandidate(new CollisionCandidate(CollisionCandidateKind.Inward, inwardEstimate, 0f, inwardEstimate));
            }

            if (hasClearingUp)
            {
                InsertCandidate(new CollisionCandidate(CollisionCandidateKind.Up, 0f, clearingUp, clearingUp));
            }

            if (upEstimate > 0f && !IsBelowClearingUp(upEstimate, hasClearingUp, clearingUp))
            {
                InsertCandidate(new CollisionCandidate(CollisionCandidateKind.Up, 0f, upEstimate, upEstimate));
            }

            int stepCount = Mathf.Max(1, settings.MaximumCasts - ReservedCastCount);
            if (upLimit > 0f)
            {
                for (int step = 1; step <= stepCount; step++)
                {
                    float upStep = upLimit * step / stepCount;
                    if ((upEstimate > 0f && Mathf.Abs(upStep - upEstimate) <= TieTolerance) || IsBelowClearingUp(upStep, hasClearingUp, clearingUp))
                    {
                        continue;
                    }

                    InsertCandidate(new CollisionCandidate(CollisionCandidateKind.Up, 0f, upStep, upStep));
                }
            }

            if (isInsideOwnBody && outwardLimit > 0f)
            {
                for (int step = 1; step <= stepCount; step++)
                {
                    float outwardStep = outwardLimit * step / stepCount;
                    InsertCandidate(new CollisionCandidate(CollisionCandidateKind.Outward, -outwardStep, 0f, outwardStep));
                }
            }
        }

        private bool TryGetClearingUp(Vector3 desiredPosition, Vector3 watchPoint, Vector3 up, float upLimit, float radius, out float clearingUp)
        {
            clearingUp = 0f;
            if (blockerCount == 0 || upLimit <= 0f)
            {
                return false;
            }

            for (int index = 0; index < blockerCount; index++)
            {
                float blockerUp;
                if (!TryGetBlockerClearingUp(blockers[index], desiredPosition, watchPoint, up, upLimit, radius, out blockerUp))
                {
                    clearingUp = 0f;
                    return false;
                }

                clearingUp = Mathf.Max(clearingUp, blockerUp);
            }

            clearingUp = Mathf.Min(clearingUp + SurfaceSkin, upLimit);
            return true;
        }

        private bool TryGetBlockerClearingUp(Collider blocker, Vector3 desiredPosition, Vector3 watchPoint, Vector3 up, float upLimit, float radius, out float clearingUp)
        {
            clearingUp = 0f;
            if (!IsSegmentBlocked(blocker, watchPoint, desiredPosition, radius))
            {
                return true;
            }

            if (IsSegmentBlocked(blocker, watchPoint, desiredPosition + up * upLimit, radius))
            {
                return false;
            }

            float blockedUp = 0f;
            float clearUp = upLimit;
            for (int iteration = 0; iteration < ClearingSearchIterations; iteration++)
            {
                float middle = (blockedUp + clearUp) * 0.5f;
                if (IsSegmentBlocked(blocker, watchPoint, desiredPosition + up * middle, radius))
                {
                    blockedUp = middle;
                }
                else
                {
                    clearUp = middle;
                }
            }

            clearingUp = clearUp;
            return true;
        }

        private bool IsSegmentBlocked(Collider blocker, Vector3 start, Vector3 end, float radius)
        {
            float enter;
            float exit;
            BoxCollider box = blocker as BoxCollider;
            if (box != null)
            {
                Transform boxTransform = box.transform;
                Vector3 scale = boxTransform.lossyScale;
                Vector3 halfSize = box.size * 0.5f + new Vector3(GetLocalRadius(radius, scale.x), GetLocalRadius(radius, scale.y), GetLocalRadius(radius, scale.z));
                Vector3 localStart = boxTransform.InverseTransformPoint(start) - box.center;
                Vector3 localEnd = boxTransform.InverseTransformPoint(end) - box.center;
                return ClipSegment(localStart, localEnd, -halfSize, halfSize, out enter, out exit);
            }

            Bounds bounds = blocker.bounds;
            bounds.Expand(2f * radius);
            return ClipSegment(start, end, bounds.min, bounds.max, out enter, out exit);
        }

        private float GetLocalRadius(float radius, float scale)
        {
            float magnitude = Mathf.Abs(scale);
            if (magnitude < ParallelTolerance)
            {
                return 0f;
            }

            return radius / magnitude;
        }

        private bool IsBelowClearingUp(float upOffset, bool hasClearingUp, float clearingUp)
        {
            return hasClearingUp && upOffset < clearingUp + TieTolerance;
        }

        private void InsertCandidate(CollisionCandidate candidate)
        {
            int index = candidates.Count;
            while (index > 0 && IsBefore(candidate, candidates[index - 1]))
            {
                index--;
            }

            candidates.Insert(index, candidate);
        }

        private bool IsBefore(CollisionCandidate first, CollisionCandidate second)
        {
            if (first.Kind == second.Kind)
            {
                return first.Displacement < second.Displacement;
            }

            if (Mathf.Abs(first.Displacement - second.Displacement) <= TieTolerance)
            {
                return first.Kind < second.Kind;
            }

            return first.Displacement < second.Displacement;
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
            if (Mathf.Abs(target) > Mathf.Abs(current) || target * current < 0f)
            {
                halfLife = settings.MoveInHalfLife;
            }

            return smoother.Smooth(current, target, deltaTime, halfLife);
        }
    }
}
