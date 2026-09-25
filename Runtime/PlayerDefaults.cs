using System.Collections.Generic;

namespace Gley.CameraSystem
{
    public class PlayerDefaults
    {
        private const int InitialEntryCapacity = 8;

        private readonly List<PlayerDefaultEntry> entries = new List<PlayerDefaultEntry>(InitialEntryCapacity);

        public int EntryCount => entries.Count;

        public void SaveCurrentPoseAsDefault(string profileId, int viewId, DefaultSlot slot, ChainOrbit orbit, LivePose pose)
        {
            float bearing = orbit.Bearing.BearingOfLocalPoint(orbit.EvaluateRootLocalPosition(pose.OrbitDistance));
            SetDefault(profileId, viewId, slot, new OrbitPose(bearing, pose.ZoomOffset, pose.HeightOffset));
        }

        public void SetDefault(string profileId, int viewId, DefaultSlot slot, OrbitPose pose)
        {
            int index = FindEntry(profileId, viewId);
            bool hasRearDefault = false;
            OrbitPose rearDefault = default;
            bool hasFrontDefault = false;
            OrbitPose frontDefault = default;
            if (index >= 0)
            {
                PlayerDefaultEntry existing = entries[index];
                hasRearDefault = existing.HasRearDefault;
                rearDefault = existing.RearDefault;
                hasFrontDefault = existing.HasFrontDefault;
                frontDefault = existing.FrontDefault;
            }

            if (slot == DefaultSlot.Front)
            {
                hasFrontDefault = true;
                frontDefault = pose;
            }
            else
            {
                hasRearDefault = true;
                rearDefault = pose;
            }

            SetEntry(new PlayerDefaultEntry(profileId, viewId, hasRearDefault, rearDefault, hasFrontDefault, frontDefault));
        }

        public void SetEntry(PlayerDefaultEntry entry)
        {
            int index = FindEntry(entry.ProfileId, entry.ViewId);
            if (index >= 0)
            {
                entries[index] = entry;
            }
            else
            {
                entries.Add(entry);
            }
        }

        public PlayerDefaultEntry GetEntry(int index)
        {
            return entries[index];
        }

        public bool TryGetDefault(string profileId, int viewId, DefaultSlot slot, out OrbitPose pose)
        {
            pose = default;
            int index = FindEntry(profileId, viewId);
            if (index < 0)
            {
                return false;
            }

            PlayerDefaultEntry entry = entries[index];
            if (slot == DefaultSlot.Front)
            {
                pose = entry.FrontDefault;
                return entry.HasFrontDefault;
            }

            pose = entry.RearDefault;
            return entry.HasRearDefault;
        }

        public void Clear()
        {
            entries.Clear();
        }

        private int FindEntry(string profileId, int viewId)
        {
            for (int index = 0; index < entries.Count; index++)
            {
                PlayerDefaultEntry entry = entries[index];
                if (entry.ViewId == viewId && entry.ProfileId == profileId)
                {
                    return index;
                }
            }

            return -1;
        }
    }
}
