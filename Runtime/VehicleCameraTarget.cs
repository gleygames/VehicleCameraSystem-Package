using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gley.CameraSystem
{
    public class VehicleCameraTarget : MonoBehaviour
    {
        private readonly List<Transform> bodies = new List<Transform>();
        private readonly List<VehicleProfile> profiles = new List<VehicleProfile>();
        private readonly List<OrbitConnectorPairOverride> jointOverrides = new List<OrbitConnectorPairOverride>();

        [SerializeField] private List<OrbitConnectorPairOverride> pairOverrides = new List<OrbitConnectorPairOverride>();
        [SerializeField] private Transform rootBody;
        [SerializeField] private VehicleProfile rootProfile;
        [SerializeField] private float estimateFilterHalfLife = 0.05f;
        private Vector3 suppliedAcceleration;
        private float suppliedSpeed;
        private float turnHint;
        private int rootIndex;
        private int chainVersion;
        private bool hasSuppliedSpeed;
        private bool hasSuppliedAcceleration;
        private bool hasTurnHint;

        public event Action ChainChanged;
        public event Action Teleported;
        public event Action<Vector3> OriginShifted;
        public event Action Destroyed;

        public Vector3 SuppliedAcceleration => suppliedAcceleration;
        public float EstimateFilterHalfLife => estimateFilterHalfLife;
        public float SuppliedSpeed => suppliedSpeed;
        public float TurnHint => turnHint;
        public int BodyCount
        {
            get
            {
                EnsureInitialized();
                return bodies.Count;
            }
        }
        public int RootIndex
        {
            get
            {
                EnsureInitialized();
                return rootIndex;
            }
        }
        public int ChainVersion => chainVersion;
        public bool HasSuppliedSpeed => hasSuppliedSpeed;
        public bool HasSuppliedAcceleration => hasSuppliedAcceleration;
        public bool HasTurnHint => hasTurnHint;
        public bool IsRootAlive => rootBody != null;

        public void Configure(Transform root, VehicleProfile profile)
        {
            rootBody = root;
            rootProfile = profile;
            bodies.Clear();
            profiles.Clear();
            jointOverrides.Clear();
            rootIndex = 0;
            if (root != null && profile != null)
            {
                bodies.Add(root);
                profiles.Add(profile);
            }

            chainVersion++;
            ChainChanged?.Invoke();
        }

        public void ConfigurePairOverrides(IReadOnlyList<OrbitConnectorPairOverride> overrides)
        {
            pairOverrides.Clear();
            if (overrides != null)
            {
                for (int index = 0; index < overrides.Count; index++)
                {
                    pairOverrides.Add(overrides[index]);
                }
            }
        }

        public Transform GetBody(int index)
        {
            EnsureInitialized();
            return bodies[index];
        }

        public VehicleProfile GetProfile(int index)
        {
            EnsureInitialized();
            return profiles[index];
        }

        public OrbitConnectorPairOverride GetConnectorOverride(int frontIndex)
        {
            EnsureInitialized();
            return jointOverrides[frontIndex];
        }

        public VehicleTargetResult Attach(Transform body, VehicleProfile profile, ChainEnd end)
        {
            return Attach(body, profile, end, null);
        }

        public VehicleTargetResult Attach(Transform body, VehicleProfile profile, ChainEnd end, OrbitConnectorPairOverride connectorOverride)
        {
            EnsureInitialized();
            if (bodies.Count == 0 || rootBody == null || rootProfile == null)
            {
                return VehicleTargetResult.NotConfigured;
            }

            if (body == null)
            {
                return VehicleTargetResult.MissingBody;
            }

            if (profile == null)
            {
                return VehicleTargetResult.MissingProfile;
            }

            for (int index = 0; index < bodies.Count; index++)
            {
                if (bodies[index] == body)
                {
                    return VehicleTargetResult.BodyAlreadyInChain;
                }
            }

            VehicleProfile frontProfile = profile;
            VehicleProfile rearProfile = profiles[0];
            if (end == ChainEnd.Rear)
            {
                frontProfile = profiles[profiles.Count - 1];
                rearProfile = profile;
            }

            if (connectorOverride != null && !connectorOverride.Matches(frontProfile, rearProfile, OrbitAttachmentEnd.Rear, OrbitAttachmentEnd.Front))
            {
                return VehicleTargetResult.OverrideDoesNotMatch;
            }

            if (connectorOverride == null)
            {
                for (int index = 0; index < pairOverrides.Count; index++)
                {
                    OrbitConnectorPairOverride candidate = pairOverrides[index];
                    if (candidate != null && candidate.Matches(frontProfile, rearProfile, OrbitAttachmentEnd.Rear, OrbitAttachmentEnd.Front))
                    {
                        connectorOverride = candidate;
                        break;
                    }
                }
            }

            if (end == ChainEnd.Front)
            {
                bodies.Insert(0, body);
                profiles.Insert(0, profile);
                jointOverrides.Insert(0, connectorOverride);
                rootIndex++;
            }
            else
            {
                bodies.Add(body);
                profiles.Add(profile);
                jointOverrides.Add(connectorOverride);
            }

            chainVersion++;
            ChainChanged?.Invoke();
            return VehicleTargetResult.Accepted;
        }

        public VehicleTargetResult Detach(Transform body)
        {
            EnsureInitialized();
            if (bodies.Count == 0)
            {
                return VehicleTargetResult.NotConfigured;
            }

            int bodyIndex = -1;
            for (int index = 0; index < bodies.Count; index++)
            {
                if (bodies[index] == body)
                {
                    bodyIndex = index;
                    break;
                }
            }

            if (bodyIndex < 0)
            {
                return VehicleTargetResult.BodyNotInChain;
            }

            if (bodyIndex == rootIndex)
            {
                return VehicleTargetResult.CannotDetachRoot;
            }

            for (int index = 0; index < jointOverrides.Count; index++)
            {
                OrbitConnectorPairOverride connectorOverride = jointOverrides[index];
                if (connectorOverride != null && !connectorOverride.Matches(profiles[index], profiles[index + 1], OrbitAttachmentEnd.Rear, OrbitAttachmentEnd.Front))
                {
                    return VehicleTargetResult.OverrideDoesNotMatch;
                }
            }

            if (bodyIndex < rootIndex)
            {
                bodies.RemoveRange(0, bodyIndex + 1);
                profiles.RemoveRange(0, bodyIndex + 1);
                jointOverrides.RemoveRange(0, bodyIndex + 1);
                rootIndex -= bodyIndex + 1;
            }
            else
            {
                int removedCount = bodies.Count - bodyIndex;
                bodies.RemoveRange(bodyIndex, removedCount);
                profiles.RemoveRange(bodyIndex, removedCount);
                jointOverrides.RemoveRange(bodyIndex - 1, removedCount);
            }

            chainVersion++;
            ChainChanged?.Invoke();
            return VehicleTargetResult.Accepted;
        }

        public void Teleport()
        {
            Teleported?.Invoke();
        }

        public void ShiftOrigin(Vector3 offset)
        {
            OriginShifted?.Invoke(offset);
        }

        public void SetSuppliedSpeed(float metresPerSecond)
        {
            suppliedSpeed = metresPerSecond;
            hasSuppliedSpeed = true;
        }

        public void ClearSuppliedSpeed()
        {
            hasSuppliedSpeed = false;
        }

        public void SetSuppliedAcceleration(Vector3 bodyLocal)
        {
            suppliedAcceleration = bodyLocal;
            hasSuppliedAcceleration = true;
        }

        public void ClearSuppliedAcceleration()
        {
            hasSuppliedAcceleration = false;
        }

        public void SetTurnHint(float minusOneToOne)
        {
            turnHint = Mathf.Clamp(minusOneToOne, -1f, 1f);
            hasTurnHint = true;
        }

        public void ClearTurnHint()
        {
            hasTurnHint = false;
        }

        private void EnsureInitialized()
        {
            if (bodies.Count == 0 && rootBody != null && rootProfile != null)
            {
                bodies.Add(rootBody);
                profiles.Add(rootProfile);
                rootIndex = 0;
            }
        }

        private void OnDestroy()
        {
            Destroyed?.Invoke();
        }
    }
}
