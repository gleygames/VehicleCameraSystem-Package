using System.Collections.Generic;
using UnityEngine;

namespace Gley.CameraSystem
{
    public class TwoBodyClosedBezierOrbit
    {
        private readonly List<GeneratedOrbitConnectorPair> connectorPairs = new List<GeneratedOrbitConnectorPair>();
        private readonly List<Vector3> bodyOffsets = new List<Vector3>();
        private readonly List<VehicleProfile> bodyProfiles = new List<VehicleProfile>();
        private readonly LinearOrbitComposer composer;
        private readonly VehicleProfile frontVehicleProfile;
        private readonly VehicleProfile rearVehicleProfile;
        private ClosedBezierOrbit frontSourceOrbit;
        private ClosedBezierOrbit rearSourceOrbit;

        public IReadOnlyList<AssembledBezierOrbitSegment> Segments => composer.Segments;

        public TwoBodyClosedBezierOrbitAssemblyResult AssemblyResult => GetAssemblyResult();
        public OrbitWatchMarkerValidationResult FrontWatchMarkerValidationResult => GetWatchMarkerValidationResult(frontSourceOrbit);
        public OrbitWatchMarkerValidationResult RearWatchMarkerValidationResult => GetWatchMarkerValidationResult(rearSourceOrbit);
        public float Length => composer.Length;
        public bool HasValidWatchMarkers => FrontWatchMarkerValidationResult == OrbitWatchMarkerValidationResult.Valid && RearWatchMarkerValidationResult == OrbitWatchMarkerValidationResult.Valid;
        public bool IsClosed => AssemblyResult == TwoBodyClosedBezierOrbitAssemblyResult.Valid;

        public TwoBodyClosedBezierOrbit(VehicleProfile frontVehicleProfile, VehicleProfile rearVehicleProfile)
        {
            this.frontVehicleProfile = frontVehicleProfile;
            this.rearVehicleProfile = rearVehicleProfile;
            bodyProfiles.Add(frontVehicleProfile);
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
            RefreshSourceOrbits();
            RefreshAssemblyInputs();
            composer.Rebuild();
        }

        private void RefreshSourceOrbits()
        {
            frontSourceOrbit = CreateSourceOrbit(frontVehicleProfile);
            rearSourceOrbit = CreateSourceOrbit(rearVehicleProfile);
        }

        private void RefreshAssemblyInputs()
        {
            bodyOffsets.Clear();
            connectorPairs.Clear();
            bodyOffsets.Add(Vector3.zero);
            bodyOffsets.Add(Vector3.zero);

            TwoBodyOrbitConnectorGenerator connectorGenerator = new TwoBodyOrbitConnectorGenerator(frontVehicleProfile, rearVehicleProfile);

            if (connectorGenerator.GenerationResult == TwoBodyOrbitConnectorGenerationResult.Generated)
            {
                bodyOffsets[1] = connectorGenerator.RearBodyLocalPosition;
                connectorPairs.Add(connectorGenerator.ConnectorPair);
            }
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
            if (profile == null || profile.PrimaryOrbit == null)
            {
                return null;
            }

            return new ClosedBezierOrbit(profile.PrimaryOrbit);
        }

    }
}
