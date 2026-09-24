using UnityEngine;

namespace Gley.CameraSystem
{
    public readonly struct ChainBody
    {
        public Transform Body { get; }
        public VehicleProfile Profile { get; }
        public VehicleOrbit Orbit { get; }
        public Vector3 StraightOffset { get; }
        public int ChainIndex { get; }

        public ChainBody(Transform body, VehicleProfile profile, VehicleOrbit orbit, Vector3 straightOffset, int chainIndex)
        {
            Body = body;
            Profile = profile;
            Orbit = orbit;
            StraightOffset = straightOffset;
            ChainIndex = chainIndex;
        }
    }
}
