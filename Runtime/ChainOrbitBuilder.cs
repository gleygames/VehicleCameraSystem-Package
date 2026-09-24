using System.Collections.Generic;
using UnityEngine;

namespace Gley.CameraSystem
{
    public class ChainOrbitBuilder
    {
        public ChainOrbit Build(VehicleCameraTarget target, string rootOrbitName)
        {
            if (target == null || target.BodyCount == 0 || !target.IsRootAlive)
            {
                return null;
            }

            int rootIndex = target.RootIndex;
            VehicleOrbit rootOrbit;
            if (!target.GetProfile(rootIndex).TryGetOrbit(rootOrbitName, out rootOrbit))
            {
                return null;
            }

            if (!rootOrbit.MergeWhenAttached)
            {
                VehicleProfile[] rootProfiles = { target.GetProfile(rootIndex) };
                Transform[] rootBodies = { target.GetBody(rootIndex) };
                ChainLayout rootLayout = new ChainLayout(rootProfiles, 0, rootOrbit);
                return new ChainOrbit(rootLayout, rootBodies, 0);
            }

            List<VehicleProfile> profiles = new List<VehicleProfile>(target.BodyCount);
            List<Transform> bodies = new List<Transform>(target.BodyCount);
            List<OrbitConnectorPairOverride> overrides = new List<OrbitConnectorPairOverride>(target.BodyCount - 1);
            for (int index = 0; index < target.BodyCount; index++)
            {
                profiles.Add(target.GetProfile(index));
                bodies.Add(target.GetBody(index));
                if (index < target.BodyCount - 1)
                {
                    overrides.Add(target.GetConnectorOverride(index));
                }
            }

            ChainLayout layout = new ChainLayout(profiles, rootIndex, rootOrbit, null, overrides);
            return new ChainOrbit(layout, bodies, rootIndex);
        }
    }
}
