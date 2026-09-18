using System.Collections.Generic;
using UnityEngine;

namespace Gley.CameraSystem
{
    public class ThreeBodyClosedBezierOrbit
    {
        private readonly LinearOrbitComposer composer;
        private readonly ClosedBezierOrbit leadSourceOrbit;
        private readonly ClosedBezierOrbit middleSourceOrbit;
        private readonly ClosedBezierOrbit rearSourceOrbit;
        private readonly ThreeBodyOrbitReferenceLayout referenceLayout;

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
        {
            leadSourceOrbit = CreateSourceOrbit(leadVehicleProfile);
            middleSourceOrbit = CreateSourceOrbit(middleVehicleProfile);
            rearSourceOrbit = CreateSourceOrbit(rearVehicleProfile);
            referenceLayout = new ThreeBodyOrbitReferenceLayout(leadVehicleProfile, middleVehicleProfile, rearVehicleProfile);
            composer = new LinearOrbitComposer(CreateProfiles(leadVehicleProfile, middleVehicleProfile, rearVehicleProfile), CreateOffsets(), CreateConnectors());
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
            composer.Rebuild();
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

        private List<VehicleProfile> CreateProfiles(VehicleProfile leadProfile, VehicleProfile middleProfile, VehicleProfile rearProfile)
        {
            List<VehicleProfile> profiles = new List<VehicleProfile>();
            profiles.Add(leadProfile);
            profiles.Add(middleProfile);
            profiles.Add(rearProfile);
            return profiles;
        }

        private List<Vector3> CreateOffsets()
        {
            List<Vector3> offsets = new List<Vector3>();
            offsets.Add(Vector3.zero);
            offsets.Add(Vector3.zero);
            offsets.Add(Vector3.zero);

            if (referenceLayout.LayoutResult == ThreeBodyOrbitReferenceLayoutResult.Valid)
            {
                offsets[1] = referenceLayout.MiddleBodyLocalPosition;
                offsets[2] = referenceLayout.RearBodyLocalPosition;
            }

            return offsets;
        }

        private List<GeneratedOrbitConnectorPair> CreateConnectors()
        {
            List<GeneratedOrbitConnectorPair> connectorPairs = new List<GeneratedOrbitConnectorPair>();

            if (referenceLayout.LayoutResult == ThreeBodyOrbitReferenceLayoutResult.Valid)
            {
                connectorPairs.Add(referenceLayout.LeadMiddleConnectors);
                connectorPairs.Add(referenceLayout.MiddleRearConnectors);
            }

            return connectorPairs;
        }
    }
}
