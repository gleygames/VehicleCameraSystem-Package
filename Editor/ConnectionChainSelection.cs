using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gley.CameraSystem.Editor
{
    [Serializable]
    public class ConnectionChainSelection
    {
        [SerializeField] private List<ConnectionPartner> frontPartners = new List<ConnectionPartner>();
        [SerializeField] private List<ConnectionPartner> rearPartners = new List<ConnectionPartner>();
        [SerializeField] private int rootOrbitId;
        private int version;

        public IReadOnlyList<ConnectionPartner> FrontPartners => frontPartners;
        public IReadOnlyList<ConnectionPartner> RearPartners => rearPartners;
        public int RootOrbitId => rootOrbitId;
        public int Version => version;
        public int RootIndex => frontPartners.Count;
        public int BodyCount => frontPartners.Count + rearPartners.Count + 1;

        public ConnectionPartner AddPartner(ChainEnd end, VehicleProfile profile)
        {
            ConnectionPartner partner = new ConnectionPartner();
            partner.SetProfile(profile);
            GetPartners(end).Add(partner);
            MarkChanged();
            return partner;
        }

        public void RemovePartner(ChainEnd end, int index)
        {
            List<ConnectionPartner> partners = GetPartners(end);
            if (index < 0 || index >= partners.Count)
            {
                return;
            }

            partners.RemoveAt(index);
            MarkChanged();
        }

        public void SetRootOrbitId(int orbitId)
        {
            if (orbitId == rootOrbitId)
            {
                return;
            }

            rootOrbitId = orbitId;
            MarkChanged();
        }

        public ConnectionPartner GetBodyPartner(int chainIndex)
        {
            if (chainIndex < RootIndex && chainIndex >= 0)
            {
                return frontPartners[RootIndex - 1 - chainIndex];
            }

            int rearIndex = chainIndex - RootIndex - 1;
            if (rearIndex >= 0 && rearIndex < rearPartners.Count)
            {
                return rearPartners[rearIndex];
            }

            return null;
        }

        public ConnectionPartner GetJointPartner(int jointIndex)
        {
            if (jointIndex < RootIndex)
            {
                return GetBodyPartner(jointIndex);
            }

            return GetBodyPartner(jointIndex + 1);
        }

        public void MarkChanged()
        {
            version++;
        }

        private List<ConnectionPartner> GetPartners(ChainEnd end)
        {
            if (end == ChainEnd.Front)
            {
                return frontPartners;
            }

            return rearPartners;
        }
    }
}
