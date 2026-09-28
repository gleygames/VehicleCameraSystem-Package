using System;
using UnityEngine;

namespace Gley.CameraSystem.Editor
{
    [Serializable]
    public class ConnectionPartner
    {
        [SerializeField] private VehicleProfile profile;
        [SerializeField] private GameObject model;
        [SerializeField] private OrbitConnectorPairOverride jointOverride;
        [SerializeField] private float articulation;

        public VehicleProfile Profile => profile;
        public GameObject Model => model;
        public OrbitConnectorPairOverride JointOverride => jointOverride;
        public float Articulation => articulation;

        public void SetProfile(VehicleProfile partnerProfile)
        {
            profile = partnerProfile;
        }

        public void SetModel(GameObject partnerModel)
        {
            model = partnerModel;
        }

        public void SetJointOverride(OrbitConnectorPairOverride connectorOverride)
        {
            jointOverride = connectorOverride;
        }

        public void SetArticulation(float degrees)
        {
            articulation = degrees;
        }
    }
}
