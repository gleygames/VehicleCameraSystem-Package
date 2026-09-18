using System.Collections.Generic;
using UnityEngine;

namespace Gley.CameraSystem
{
    public class TwoBodyClosedBezierOrbit
    {
        private readonly LinearOrbitComposer composer;
        private readonly ClosedBezierOrbit frontSourceOrbit;
        private readonly ClosedBezierOrbit rearSourceOrbit;

        public IReadOnlyList<AssembledBezierOrbitSegment> Segments => composer.Segments;

        public TwoBodyClosedBezierOrbitAssemblyResult AssemblyResult => GetAssemblyResult();
        public OrbitWatchMarkerValidationResult FrontWatchMarkerValidationResult => GetWatchMarkerValidationResult(frontSourceOrbit);
        public OrbitWatchMarkerValidationResult RearWatchMarkerValidationResult => GetWatchMarkerValidationResult(rearSourceOrbit);
        public float Length => composer.Length;
        public bool HasValidWatchMarkers => FrontWatchMarkerValidationResult == OrbitWatchMarkerValidationResult.Valid && RearWatchMarkerValidationResult == OrbitWatchMarkerValidationResult.Valid;
        public bool IsClosed => AssemblyResult == TwoBodyClosedBezierOrbitAssemblyResult.Valid;

        public TwoBodyClosedBezierOrbit(VehicleProfile frontVehicleProfile, VehicleProfile rearVehicleProfile)
        {
            frontSourceOrbit = CreateSourceOrbit(frontVehicleProfile);
            rearSourceOrbit = CreateSourceOrbit(rearVehicleProfile);
            composer = new LinearOrbitComposer(CreateProfiles(frontVehicleProfile, rearVehicleProfile), CreateOffsets(frontVehicleProfile, rearVehicleProfile), CreateConnectors(frontVehicleProfile, rearVehicleProfile));
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

        public bool TryGetRetainedFrontOrbitDistance(float frontOrbitDistance, out float combinedOrbitDistance)
        {
            return composer.TryGetRetainedLeadOrbitDistance(frontOrbitDistance, out combinedOrbitDistance);
        }

        public bool TryGetRetainedFrontSourceOrbitDistance(float combinedOrbitDistance, out float frontOrbitDistance)
        {
            return composer.TryGetRetainedLeadSourceOrbitDistance(combinedOrbitDistance, out frontOrbitDistance);
        }

        public void Rebuild()
        {
            composer.Rebuild();
        }

        private TwoBodyClosedBezierOrbitAssemblyResult GetAssemblyResult()
        {
            if (composer.AssemblyResult == LinearOrbitComposerAssemblyResult.Valid)
            {
                return TwoBodyClosedBezierOrbitAssemblyResult.Valid;
            }

            if (composer.AssemblyResult == LinearOrbitComposerAssemblyResult.ConnectorGenerationFailed)
            {
                return TwoBodyClosedBezierOrbitAssemblyResult.ConnectorGenerationFailed;
            }

            if (composer.AssemblyResult == LinearOrbitComposerAssemblyResult.Disconnected)
            {
                return TwoBodyClosedBezierOrbitAssemblyResult.Disconnected;
            }

            if (composer.AssemblyResult == LinearOrbitComposerAssemblyResult.NonPlanar)
            {
                return TwoBodyClosedBezierOrbitAssemblyResult.NonPlanar;
            }

            if (composer.AssemblyResult == LinearOrbitComposerAssemblyResult.SelfIntersecting)
            {
                return TwoBodyClosedBezierOrbitAssemblyResult.SelfIntersecting;
            }

            if (composer.AssemblyResult == LinearOrbitComposerAssemblyResult.Degenerate)
            {
                return TwoBodyClosedBezierOrbitAssemblyResult.Degenerate;
            }

            return TwoBodyClosedBezierOrbitAssemblyResult.NotAssembled;
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

        private List<VehicleProfile> CreateProfiles(VehicleProfile frontProfile, VehicleProfile rearProfile)
        {
            List<VehicleProfile> profiles = new List<VehicleProfile>();
            profiles.Add(frontProfile);
            profiles.Add(rearProfile);
            return profiles;
        }

        private List<Vector3> CreateOffsets(VehicleProfile frontProfile, VehicleProfile rearProfile)
        {
            List<Vector3> offsets = new List<Vector3>();
            offsets.Add(Vector3.zero);
            offsets.Add(Vector3.zero);
            TwoBodyOrbitConnectorGenerator connectorGenerator = new TwoBodyOrbitConnectorGenerator(frontProfile, rearProfile);

            if (connectorGenerator.GenerationResult == TwoBodyOrbitConnectorGenerationResult.Generated)
            {
                offsets[1] = connectorGenerator.RearBodyLocalPosition;
            }

            return offsets;
        }

        private List<GeneratedOrbitConnectorPair> CreateConnectors(VehicleProfile frontProfile, VehicleProfile rearProfile)
        {
            List<GeneratedOrbitConnectorPair> connectorPairs = new List<GeneratedOrbitConnectorPair>();
            TwoBodyOrbitConnectorGenerator connectorGenerator = new TwoBodyOrbitConnectorGenerator(frontProfile, rearProfile);

            if (connectorGenerator.GenerationResult == TwoBodyOrbitConnectorGenerationResult.Generated)
            {
                connectorPairs.Add(connectorGenerator.ConnectorPair);
            }

            return connectorPairs;
        }
    }
}
