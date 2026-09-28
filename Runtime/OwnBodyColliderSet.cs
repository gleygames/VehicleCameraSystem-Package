using System.Collections.Generic;
using UnityEngine;

namespace Gley.CameraSystem
{
    public class OwnBodyColliderSet
    {
        private readonly List<Collider> bodyColliders = new List<Collider>();
        private readonly List<int> colliderIds = new List<int>();

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
                    colliderIds.Add(bodyColliders[index].GetInstanceID());
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

            int instanceId = candidate.GetInstanceID();
            int low = 0;
            int high = colliderIds.Count - 1;
            while (low <= high)
            {
                int middle = low + (high - low) / 2;
                int middleId = colliderIds[middle];
                if (middleId == instanceId)
                {
                    return true;
                }

                if (middleId < instanceId)
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
    }
}
