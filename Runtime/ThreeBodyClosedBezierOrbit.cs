using System.Collections.Generic;
using UnityEngine;

namespace Gley.CameraSystem
{
    public class ThreeBodyClosedBezierOrbit
    {
        private readonly List<GeneratedOrbitConnectorPair> connectorPairs = new List<GeneratedOrbitConnectorPair>();
        private readonly List<Vector3> bodyOffsets = new List<Vector3>();
        private readonly List<VehicleProfile> bodyProfiles = new List<VehicleProfile>();
        private readonly LinearOrbitComposer composer;
        private readonly VehicleProfile leadVehicleProfile;
        private readonly VehicleProfile middleVehicleProfile;
        private readonly VehicleProfile rearVehicleProfile;
        private readonly OrbitConnectorPairOverride leadMiddleConnectorOverride;
        private readonly OrbitConnectorPairOverride middleRearConnectorOverride;
        private ClosedBezierOrbit leadSourceOrbit;
        private ClosedBezierOrbit middleSourceOrbit;
        private ClosedBezierOrbit rearSourceOrbit;
        private ThreeBodyOrbitReferenceLayout referenceLayout;

        public IReadOnlyList<AssembledBezierOrbitSegment> Segments => composer.Segments;

        public ThreeBodyClosedBezierOrbitAssemblyResult AssemblyResult => GetAssemblyResult();
        public OrbitWatchMarkerValidationResult LeadWatchMarkerValidationResult => GetWatchMarkerValidationResult(leadSourceOrbit);
        public OrbitWatchMarkerValidationResult MiddleWatchMarkerValidationResult => GetWatchMarkerValidationResult(middleSourceOrbit);
        public OrbitWatchMarkerValidationResult RearWatchMarkerValidationResult => GetWatchMarkerValidationResult(rearSourceOrbit);
        public ThreeBodyOrbitReferenceLayout ReferenceLayout => referenceLayout;
        public float Length => composer.Length;
        public bool HasValidWatchMarkers => LeadWatchMarkerValidationResult == OrbitWatchMarkerValidationResult.Valid && MiddleWatchMarkerValidationResult == OrbitWatchMarkerValidationResult.Valid && RearWatchMarkerValidationResult == OrbitWatchMarkerValidationResult.Valid;
        public bool IsClosed => AssemblyResult == ThreeBodyClosedBezierOrbitAssemblyResult.Valid;

        public ThreeBodyClosedBezierOrbit(VehicleProfile leadVehicleProfile, VehicleProfile middleVehicleProfile, VehicleProfile rearVehicleProfile)
            : this(leadVehicleProfile, middleVehicleProfile, rearVehicleProfile, null, null)
        {
        }

        public ThreeBodyClosedBezierOrbit(VehicleProfile leadVehicleProfile, VehicleProfile middleVehicleProfile, VehicleProfile rearVehicleProfile, OrbitConnectorPairOverride leadMiddleOverride, OrbitConnectorPairOverride middleRearOverride)
        {
            this.leadVehicleProfile = leadVehicleProfile;
            this.middleVehicleProfile = middleVehicleProfile;
            this.rearVehicleProfile = rearVehicleProfile;
            leadMiddleConnectorOverride = leadMiddleOverride;
            middleRearConnectorOverride = middleRearOverride;
            bodyProfiles.Add(leadVehicleProfile);
            bodyProfiles.Add(middleVehicleProfile);
            bodyProfiles.Add(rearVehicleProfile);
            RefreshSourceOrbits();
            RefreshAssemblyInputs();
            composer = new LinearOrbitComposer(bodyProfiles, bodyOffsets, connectorPairs);
        }

        public Vector3 EvaluateLeadBodyLocalPosition(float distance)
        {
            return composer.EvaluateLeadBodyLocalPosition(distance);
        }

        public Vector3 EvaluateWorldPosition(Transform leadBody, float distance)
        {
            return composer.EvaluateWorldPosition(leadBody, distance);
        }

        public Vector3 EvaluateLeadBodyLocalInwardNormal(float distance)
        {
            return composer.EvaluateLeadBodyLocalInwardNormal(distance);
        }

        public Vector3 EvaluateLeadBodyLocalWatchPoint(float distance)
        {
            return composer.EvaluateLeadBodyLocalWatchPoint(distance);
        }

        public void Rebuild()
        {
            RefreshSourceOrbits();
            RefreshAssemblyInputs();
            composer.Rebuild();
        }

        private void RefreshSourceOrbits()
        {
            leadSourceOrbit = CreateSourceOrbit(leadVehicleProfile);
            middleSourceOrbit = CreateSourceOrbit(middleVehicleProfile);
            rearSourceOrbit = CreateSourceOrbit(rearVehicleProfile);
        }

        private void RefreshAssemblyInputs()
        {
            referenceLayout = new ThreeBodyOrbitReferenceLayout(leadVehicleProfile, middleVehicleProfile, rearVehicleProfile, leadMiddleConnectorOverride, middleRearConnectorOverride);
            bodyOffsets.Clear();
            connectorPairs.Clear();
            bodyOffsets.Add(Vector3.zero);
            bodyOffsets.Add(Vector3.zero);
            bodyOffsets.Add(Vector3.zero);

            if (referenceLayout.LayoutResult == ThreeBodyOrbitReferenceLayoutResult.Valid)
            {
                bodyOffsets[1] = referenceLayout.MiddleBodyLocalPosition;
                bodyOffsets[2] = referenceLayout.RearBodyLocalPosition;
                connectorPairs.Add(referenceLayout.LeadMiddleConnectors);
                connectorPairs.Add(referenceLayout.MiddleRearConnectors);
            }
        }

        private ThreeBodyClosedBezierOrbitAssemblyResult GetAssemblyResult()
        {
            if (referenceLayout.LayoutResult == ThreeBodyOrbitReferenceLayoutResult.LeadMiddleConnectorGenerationFailed)
            {
                return ThreeBodyClosedBezierOrbitAssemblyResult.LeadMiddleConnectorGenerationFailed;
            }

            if (referenceLayout.LayoutResult == ThreeBodyOrbitReferenceLayoutResult.MiddleRearConnectorGenerationFailed)
            {
                return ThreeBodyClosedBezierOrbitAssemblyResult.MiddleRearConnectorGenerationFailed;
            }

            if (referenceLayout.LayoutResult == ThreeBodyOrbitReferenceLayoutResult.LeadMiddleConnectorOverrideInvalid)
            {
                return ThreeBodyClosedBezierOrbitAssemblyResult.LeadMiddleConnectorOverrideInvalid;
            }

            if (referenceLayout.LayoutResult == ThreeBodyOrbitReferenceLayoutResult.MiddleRearConnectorOverrideInvalid)
            {
                return ThreeBodyClosedBezierOrbitAssemblyResult.MiddleRearConnectorOverrideInvalid;
            }

            if (composer.AssemblyResult == LinearOrbitComposerAssemblyResult.Valid)
            {
                return ThreeBodyClosedBezierOrbitAssemblyResult.Valid;
            }

            if (composer.AssemblyResult == LinearOrbitComposerAssemblyResult.Disconnected)
            {
                return ThreeBodyClosedBezierOrbitAssemblyResult.Disconnected;
            }

            if (composer.AssemblyResult == LinearOrbitComposerAssemblyResult.NonPlanar)
            {
                return ThreeBodyClosedBezierOrbitAssemblyResult.NonPlanar;
            }

            if (composer.AssemblyResult == LinearOrbitComposerAssemblyResult.SelfIntersecting)
            {
                return ThreeBodyClosedBezierOrbitAssemblyResult.SelfIntersecting;
            }

            if (composer.AssemblyResult == LinearOrbitComposerAssemblyResult.Degenerate)
            {
                return ThreeBodyClosedBezierOrbitAssemblyResult.Degenerate;
            }

            return ThreeBodyClosedBezierOrbitAssemblyResult.NotAssembled;
        }

        private OrbitWatchMarkerValidationResult GetWatchMarkerValidationResult(ClosedBezierOrbit sourceOrbit)
        {
            if (sourceOrbit == null)
            {
                return OrbitWatchMarkerValidationResult.MissingMarkers;
            }

            return sourceOrbit.WatchMarkerValidationResult;
        }

        private ClosedBezierOrbit CreateSourceOrbit(VehicleProfile profile)
        {
            if (profile == null || profile.VehicleOrbit == null)
            {
                return null;
            }

            return new ClosedBezierOrbit(profile.VehicleOrbit);
        }

    }
}
