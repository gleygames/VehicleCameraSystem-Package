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

            return colliderIds.BinarySearch(candidate.GetInstanceID()) >= 0;
        }

        public void Clear()
        {
            colliderIds.Clear();
            bodyColliders.Clear();
        }
    }
}
