using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gley.CameraSystem
{
    [CreateAssetMenu(fileName = "Vehicle Profile", menuName = "Gley/Vehicle Camera System/Vehicle Profile")]
    public class VehicleProfile : ScriptableObject
    {
        public const int CurrentFormatVersion = 1;

        [SerializeField] private List<VehicleOrbit> orbits = new List<VehicleOrbit>();
        [SerializeField] private List<VehicleViewEntry> views = new List<VehicleViewEntry>();
        [SerializeField] private VehicleConnectorAnchors connectorAnchors;
        [SerializeField] private FixedViewPose fixedViewPose = new FixedViewPose();
        [SerializeField] private SeatSettings seat = new SeatSettings();
        [SerializeField] private string profileId;
        [SerializeField] private int formatVersion = CurrentFormatVersion;
        [SerializeField] private int nextStableId = 1;

        public IReadOnlyList<VehicleOrbit> Orbits => orbits;
        public IReadOnlyList<VehicleViewEntry> Views => views;
        public VehicleConnectorAnchors ConnectorAnchors => connectorAnchors;
        public FixedViewPose FixedViewPose => fixedViewPose;
        public SeatSettings Seat => seat;
        public VehicleOrbit PrimaryOrbit
        {
            get
            {
                if (orbits.Count == 0)
                {
                    return null;
                }

                return orbits[0];
            }
        }
        public string ProfileId => profileId;
        public int FormatVersion => formatVersion;

        public VehicleOrbit AddOrbit(string name)
        {
            VehicleOrbit orbit = new VehicleOrbit(AllocateStableId(), name);
            orbits.Add(orbit);
            return orbit;
        }

        public bool RemoveOrbit(int id)
        {
            for (int index = 0; index < orbits.Count; index++)
            {
                if (orbits[index].Id == id)
                {
                    orbits.RemoveAt(index);
                    return true;
                }
            }

            return false;
        }

        public VehicleViewEntry AddView(string name, CameraViewPreset preset, int orbitId)
        {
            VehicleViewEntry view = new VehicleViewEntry(AllocateStableId(), name, preset, orbitId);
            views.Add(view);
            return view;
        }

        public OrbitWatchMarker AddWatchMarker(VehicleOrbit orbit, string name, float normalizedPosition, Vector3 watchPoint, bool isPointOfInterest)
        {
            if (orbit == null)
            {
                return null;
            }

            OrbitWatchMarker marker = new OrbitWatchMarker(AllocateStableId(), name, normalizedPosition, watchPoint, isPointOfInterest);
            orbit.AddWatchMarker(marker);
            return marker;
        }

        public bool TryGetOrbit(int id, out VehicleOrbit orbit)
        {
            for (int index = 0; index < orbits.Count; index++)
            {
                if (orbits[index].Id == id)
                {
                    orbit = orbits[index];
                    return true;
                }
            }

            orbit = null;
            return false;
        }

        public bool TryGetOrbit(string name, out VehicleOrbit orbit)
        {
            for (int index = 0; index < orbits.Count; index++)
            {
                if (orbits[index].Name == name)
                {
                    orbit = orbits[index];
                    return true;
                }
            }

            orbit = null;
            return false;
        }

        public bool TryGetView(string name, out VehicleViewEntry view)
        {
            for (int index = 0; index < views.Count; index++)
            {
                if (views[index].Name == name)
                {
                    view = views[index];
                    return true;
                }
            }

            view = null;
            return false;
        }

        public bool TryGetView(int id, out VehicleViewEntry view)
        {
            for (int index = 0; index < views.Count; index++)
            {
                if (views[index].Id == id)
                {
                    view = views[index];
                    return true;
                }
            }

            view = null;
            return false;
        }

        public int AllocateStableId()
        {
            int stableId = nextStableId;
            nextStableId++;
            return stableId;
        }

        public void EnsureProfileId()
        {
            if (string.IsNullOrEmpty(profileId))
            {
                profileId = Guid.NewGuid().ToString();
            }
        }

        public void ConfigureConnectorAnchors(VehicleConnectorAnchors anchors)
        {
            connectorAnchors = anchors;
        }

        private void Reset()
        {
            EnsureProfileId();
        }

        private void OnValidate()
        {
            EnsureProfileId();
        }
    }
}
