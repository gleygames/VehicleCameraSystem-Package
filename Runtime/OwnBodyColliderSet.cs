using System.Collections.Generic;
using UnityEngine;
#if UNITY_6000_4_OR_NEWER
using ColliderId = UnityEngine.EntityId;
#else
using ColliderId = System.Int32;
#endif

namespace Gley.CameraSystem
{
    public class OwnBodyColliderSet
    {
        private readonly List<Collider> bodyColliders = new List<Collider>();
        private readonly List<ColliderId> colliderIds = new List<ColliderId>();

        public int Count => colliderIds.Count;

        public void Rebuild(VehicleCameraTarget target)
        {
            colliderIds.Clear();
            if (target == null)
            {
                return;
            }

            for (int bodyIndex = 0; bodyIndex < target.BodyCount; bodyIndex++)
            {
                Transform body = target.GetBody(bodyIndex);
                if (body == null)
                {
                    continue;
                }

                bodyColliders.Clear();
                body.GetComponentsInChildren(true, bodyColliders);
                for (int index = 0; index < bodyColliders.Count; index++)
                {
                    colliderIds.Add(GetColliderId(bodyColliders[index]));
                }
            }

            bodyColliders.Clear();
            colliderIds.Sort();
        }

        public bool Contains(Collider candidate)
        {
            if (ReferenceEquals(candidate, null) || colliderIds.Count == 0)
            {
                return false;
            }

            ColliderId instanceId = GetColliderId(candidate);
            int low = 0;
            int high = colliderIds.Count - 1;
            while (low <= high)
            {
                int middle = low + (high - low) / 2;
                int comparison = colliderIds[middle].CompareTo(instanceId);
                if (comparison == 0)
                {
                    return true;
                }

                if (comparison < 0)
                {
                    low = middle + 1;
                }
                else
                {
                    high = middle - 1;
                }
            }

            return false;
        }

        public void Clear()
        {
            colliderIds.Clear();
            bodyColliders.Clear();
        }

        private ColliderId GetColliderId(Collider collider)
        {
#if UNITY_6000_4_OR_NEWER
            return collider.GetEntityId();
#else
            return collider.GetInstanceID();
#endif
        }
    }
}
