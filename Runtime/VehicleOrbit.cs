using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gley.CameraSystem
{
    [Serializable]
    public class VehicleOrbit
    {
        [SerializeField] private List<BezierOrbitKnot> knots = new List<BezierOrbitKnot>();
        [SerializeField] private List<OrbitWatchMarker> watchMarkers = new List<OrbitWatchMarker>();
        [SerializeField] private OrbitRemovableSection frontRemovableSection;
        [SerializeField] private OrbitRemovableSection rearRemovableSection;
        [SerializeField] private Quaternion orientationAdjustment = Quaternion.identity;
        [SerializeField] private float minimumHeightOffset = -2f;
        [SerializeField] private float maximumHeightOffset = 2f;
        [SerializeField] private float minimumZoomOffset = -2f;
        [SerializeField] private float maximumZoomOffset = 2f;
        [SerializeField] private bool mergeWhenAttached = true;

        public IReadOnlyList<BezierOrbitKnot> Knots => knots;
        public IReadOnlyList<OrbitWatchMarker> WatchMarkers => watchMarkers;

        public OrbitRemovableSection FrontRemovableSection => frontRemovableSection;
        public OrbitRemovableSection RearRemovableSection => rearRemovableSection;
        public Quaternion OrientationAdjustment => orientationAdjustment;
        public float MinimumHeightOffset => minimumHeightOffset;
        public float MaximumHeightOffset => maximumHeightOffset;
        public float MinimumZoomOffset => minimumZoomOffset;
        public float MaximumZoomOffset => maximumZoomOffset;
        public bool MergeWhenAttached => mergeWhenAttached;

        public void Configure(List<BezierOrbitKnot> orbitKnots, Quaternion referenceOrientationAdjustment)
        {
            knots = new List<BezierOrbitKnot>();

            for (int index = 0; index < orbitKnots.Count; index++)
            {
                knots.Add(orbitKnots[index]);
            }

            orientationAdjustment = referenceOrientationAdjustment;
        }

        public void ConfigureWatchMarkers(List<OrbitWatchMarker> markers)
        {
            watchMarkers = new List<OrbitWatchMarker>();

            for (int index = 0; index < markers.Count; index++)
            {
                watchMarkers.Add(markers[index]);
            }
        }

        public void ConfigureOffsetRanges(float minimumHeight, float maximumHeight, float minimumZoom, float maximumZoom)
        {
            minimumHeightOffset = minimumHeight;
            maximumHeightOffset = maximumHeight;
            minimumZoomOffset = minimumZoom;
            maximumZoomOffset = maximumZoom;
        }

        public void ConfigureRemovableSections(OrbitRemovableSection frontSection, OrbitRemovableSection rearSection)
        {
            frontRemovableSection = frontSection;
            rearRemovableSection = rearSection;
        }

        public void ConfigureAttachmentMerge(bool shouldMergeWhenAttached)
        {
            mergeWhenAttached = shouldMergeWhenAttached;
        }

        public OrbitRemovableSection GetRemovableSection(OrbitAttachmentEnd attachmentEnd)
        {
            if (attachmentEnd == OrbitAttachmentEnd.Front)
            {
                return frontRemovableSection;
            }

            return rearRemovableSection;
        }
    }
}
