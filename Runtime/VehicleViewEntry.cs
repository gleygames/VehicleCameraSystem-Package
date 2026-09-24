using System;
using UnityEngine;

namespace Gley.CameraSystem
{
    [Serializable]
    public class VehicleViewEntry
    {
        [SerializeField] private CameraViewPreset preset;
        [SerializeField] private OrbitPose defaultPose;
        [SerializeField] private OrbitPose frontDefaultPose = new OrbitPose(180f, 0f, 0f);
        [SerializeField] private string name;
        [SerializeField] private int id;
        [SerializeField] private int orbitId;

        public CameraViewPreset Preset => preset;
        public OrbitPose DefaultPose => defaultPose;
        public OrbitPose FrontDefaultPose => frontDefaultPose;
        public string Name => name;
        public int Id => id;
        public int OrbitId => orbitId;

        public VehicleViewEntry()
        {
        }

        public VehicleViewEntry(int viewId, string viewName, CameraViewPreset viewPreset, int viewOrbitId)
        {
            id = viewId;
            name = viewName;
            preset = viewPreset;
            orbitId = viewOrbitId;
        }

        public void ConfigureDefaultPose(OrbitPose pose)
        {
            defaultPose = pose;
        }

        public void ConfigureFrontDefaultPose(OrbitPose pose)
        {
            frontDefaultPose = pose;
        }
    }
}
