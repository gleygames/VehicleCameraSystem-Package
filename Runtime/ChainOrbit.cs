using System.Collections.Generic;
using UnityEngine;

namespace Gley.CameraSystem
{
    public class ChainOrbit
    {
        private const float MinimumLength = 0.0001f;
        private const float NormalSamplingDistance = 0.01f;
        private const float PlanarTolerance = 0.0001f;
        private const int SamplesPerSegment = 32;

        private readonly List<OrbitArcLengthSample> samples = new List<OrbitArcLengthSample>();
        private readonly List<RetainedLeadSegmentMapping> retainedLeadSegmentMappings = new List<RetainedLeadSegmentMapping>();
        private readonly List<AssembledOrbitSegmentSource> segmentSources = new List<AssembledOrbitSegmentSource>();
        private readonly List<AssembledBezierOrbitSegment> segments = new List<AssembledBezierOrbitSegment>();
        private readonly List<ClosedBezierOrbit> sourceOrbits = new List<ClosedBezierOrbit>();
        private readonly List<VehicleOrbit> bodyOrbits = new List<VehicleOrbit>();
        private readonly List<MergedMarker> mergedMarkers = new List<MergedMarker>();
        private readonly List<WatchPointKey> watchPointKeys = new List<WatchPointKey>();
        private readonly List<Vector3> bearingPositions = new List<Vector3>();
        private readonly List<float> bearingDistances = new List<float>();
        private readonly List<VehicleProfile> bodyProfiles = new List<VehicleProfile>();
        private readonly IReadOnlyList<ChainBody> bodies;
        private readonly IReadOnlyList<GeneratedOrbitConnectorPair> connectorPairs;
        private readonly IReadOnlyList<Vector3> bodyOffsets;
        private readonly WatchPointCurve watchPointCurve = new WatchPointCurve();
        private readonly OrbitBearing bearing = new OrbitBearing();
        private readonly int rootIndex;

        private float leadOrbitLength;
        private float retainedLeadStartDistance;

        public IReadOnlyList<AssembledBezierOrbitSegment> Segments => segments;
        public IReadOnlyList<MergedMarker> MergedMarkers => mergedMarkers;
        public OrbitBearing Bearing => bearing;

        public ChainOrbitAssemblyResult AssemblyResult { get; private set; }
        public float Length { get; private set; }
        public bool HasValidWatchMarkers => AreWatchMarkersValid() && mergedMarkers.Count > 0;
        public bool IsClosed => AssemblyResult == ChainOrbitAssemblyResult.Valid;

        public ChainOrbit(IReadOnlyList<ChainBody> chainBodies, int rootIndex, IReadOnlyList<GeneratedOrbitConnectorPair> generatedConnectorPairs)
        {
            bodies = chainBodies;
            this.rootIndex = rootIndex;
            connectorPairs = generatedConnectorPairs;
            List<Vector3> offsets = new List<Vector3>();
            if (chainBodies != null)
            {
                for (int index = 0; index < chainBodies.Count; index++)
                {
                    bodyProfiles.Add(chainBodies[index].Profile);
                    bodyOrbits.Add(chainBodies[index].Orbit);
                    offsets.Add(chainBodies[index].StraightOffset);
                }
            }

            bodyOffsets = offsets;
            Rebuild();
        }

        public ChainOrbit(ChainLayout layout, IReadOnlyList<Transform> bodyTransforms, int rootIndex)
            : this(layout.CreateBodies(bodyTransforms), rootIndex, layout.ConnectorPairs)
        {
        }

        public Vector3 EvaluateLeadBodyLocalPosition(float distance)
        {
            int segmentIndex;
            float segmentT;

            if (!IsClosed || !TryGetOrbitSegmentPosition(distance, out segmentIndex, out segmentT))
            {
                return Vector3.zero;
            }

            return segments[segmentIndex].EvaluatePosition(segmentT);
        }

        public Vector3 EvaluateRootLocalPosition(float distance)
        {
            return EvaluateLeadBodyLocalPosition(distance);
        }

        public Vector3 EvaluateWorldPosition(Transform rootBody, float distance)
        {
            return rootBody.TransformPoint(EvaluateRootLocalPosition(distance));
        }

        public Vector3 EvaluateLeadBodyLocalInwardNormal(float distance)
        {
            if (!IsClosed || Length <= 0f)
            {
                return Vector3.zero;
            }

            Vector3 tangent = EvaluateLeadBodyLocalPosition(distance + NormalSamplingDistance) - EvaluateLeadBodyLocalPosition(distance - NormalSamplingDistance);

            if (tangent.sqrMagnitude < MinimumLength)
            {
                return Vector3.zero;
            }

            Vector3 planeNormal = bodyOrbits[rootIndex].OrientationAdjustment * Vector3.up;
            Vector3 inwardNormal = Vector3.Cross(tangent.normalized, planeNormal);

            if (CalculateSignedArea() < 0f)
            {
                inwardNormal = Vector3.Cross(planeNormal, tangent.normalized);
            }

            return inwardNormal;
        }

        public Vector3 EvaluateRootLocalInwardNormal(float distance)
        {
            return EvaluateLeadBodyLocalInwardNormal(distance);
        }

        public Vector3 EvaluateRootLocalWatchPoint(float distance)
        {
            if (!IsClosed || !HasValidWatchMarkers)
            {
                return Vector3.zero;
            }

            return watchPointCurve.Evaluate(Mathf.Repeat(distance, Length) / Length);
        }

        public Vector3 EvaluateWorldWatchPointOwnerFrame(float distance, IReadOnlyList<Transform> bodyTransforms)
        {
            if (!IsClosed || !HasValidWatchMarkers)
            {
                return Vector3.zero;
            }

            return watchPointCurve.EvaluateWorld(Mathf.Repeat(distance, Length) / Length, bodyTransforms, mergedMarkers);
        }

        public Vector3 EvaluateLeadBodyLocalWatchPoint(float distance)
        {
            return EvaluateRootLocalWatchPoint(distance);
        }

        public bool TryGetRetainedFrontOrbitDistance(float frontOrbitDistance, out float combinedOrbitDistance)
        {
            return TryGetRetainedLeadOrbitDistance(frontOrbitDistance, out combinedOrbitDistance);
        }

        public bool TryGetRetainedFrontSourceOrbitDistance(float combinedOrbitDistance, out float frontOrbitDistance)
        {
            return TryGetRetainedLeadSourceOrbitDistance(combinedOrbitDistance, out frontOrbitDistance);
        }

        public bool TryGetSourcePosition(float distance, out Transform body, out VehicleOrbit orbit, out float sourceDistance)
        {
            body = null;
            orbit = null;
            sourceDistance = 0f;

            int segmentIndex;
            float segmentT;
            if (!IsClosed || !TryGetOrbitSegmentPosition(distance, out segmentIndex, out segmentT))
            {
                return false;
            }

            AssembledOrbitSegmentSource source = segmentSources[segmentIndex];
            if (source == null)
            {
                return false;
            }

            body = bodies[source.BodyIndex].Body;
            orbit = bodyOrbits[source.BodyIndex];
            float sourceT = Mathf.Lerp(source.SourceStartT, source.SourceEndT, segmentT);
            sourceDistance = Mathf.Repeat(source.SourceSegmentStartDistance + GetSourceSegmentDistance(source.SourceSegment, sourceT), source.SourceOrbit.Length);
            return true;
        }

        public bool TryGetRetainedSourceDistance(Transform body, VehicleOrbit orbit, float sourceDistance, out float assembledDistance)
        {
            assembledDistance = 0f;
            if (!IsClosed || body == null || orbit == null)
            {
                return false;
            }

            for (int bodyIndex = 0; bodyIndex < bodies.Count; bodyIndex++)
            {
                if (ReferenceEquals(bodies[bodyIndex].Body, body) && ReferenceEquals(bodyOrbits[bodyIndex], orbit))
                {
                    return TryGetRetainedSourceDistance(bodyIndex, sourceDistance, out assembledDistance);
                }
            }

            return false;
        }

        public bool TryGetRearFallbackDistance(out float distance)
        {
            distance = 0f;
            if (!IsClosed || bodies.Count == 0)
            {
                return false;
            }

            int terminalIndex = bodies.Count - 1;
            OrbitRemovableSection rear = bodyOrbits[terminalIndex].RearRemovableSection;
            if (rear == null)
            {
                return false;
            }

            float sectionLength = Mathf.Repeat(rear.NormalizedEndPosition - rear.NormalizedStartPosition, 1f);
            float midpoint = Mathf.Repeat(rear.NormalizedStartPosition + sectionLength * 0.5f, 1f);
            return TryGetRetainedSourceDistance(terminalIndex, sourceOrbits[terminalIndex].Length * midpoint, out distance);
        }

        public OrbitWatchMarkerValidationResult GetWatchMarkerValidationResult(int bodyIndex)
        {
            if (bodyIndex < 0 || bodyIndex >= sourceOrbits.Count || sourceOrbits[bodyIndex] == null)
            {
                return OrbitWatchMarkerValidationResult.MissingMarkers;
            }

            return sourceOrbits[bodyIndex].WatchMarkerValidationResult;
        }

        public bool TryGetRetainedLeadOrbitDistance(float leadOrbitDistance, out float combinedOrbitDistance)
        {
            combinedOrbitDistance = 0f;

            if (!IsClosed || retainedLeadSegmentMappings.Count == 0 || leadOrbitLength <= 0f)
            {
                return false;
            }

            float wrappedLeadDistance = Mathf.Repeat(leadOrbitDistance, leadOrbitLength);

            if (wrappedLeadDistance < retainedLeadStartDistance)
            {
                wrappedLeadDistance += leadOrbitLength;
            }

            for (int mappingIndex = 0; mappingIndex < retainedLeadSegmentMappings.Count; mappingIndex++)
            {
                RetainedLeadSegmentMapping mapping = retainedLeadSegmentMappings[mappingIndex];

                if (wrappedLeadDistance >= mapping.SourceStartDistance - MinimumLength && wrappedLeadDistance <= mapping.SourceEndDistance + MinimumLength)
                {
                    float sourceSegmentDistance = wrappedLeadDistance - mapping.SourceSegmentStartDistance;
                    float sourceSegmentT = GetSegmentParameter(mapping.SourceSegment, sourceSegmentDistance);
                    float assembledSegmentT = (sourceSegmentT - mapping.SourceStartT) / (mapping.SourceEndT - mapping.SourceStartT);
                    combinedOrbitDistance = GetAssembledSegmentDistance(mapping.AssembledSegmentIndex, Mathf.Clamp01(assembledSegmentT));
                    return true;
                }
            }

            return false;
        }

        public bool TryGetRetainedLeadSourceOrbitDistance(float combinedOrbitDistance, out float leadOrbitDistance)
        {
            leadOrbitDistance = 0f;

            if (!IsClosed || retainedLeadSegmentMappings.Count == 0 || leadOrbitLength <= 0f)
            {
                return false;
            }

            int assembledSegmentIndex;
            float assembledSegmentT;

            if (!TryGetOrbitSegmentPosition(combinedOrbitDistance, out assembledSegmentIndex, out assembledSegmentT))
            {
                return false;
            }

            for (int mappingIndex = 0; mappingIndex < retainedLeadSegmentMappings.Count; mappingIndex++)
            {
                RetainedLeadSegmentMapping mapping = retainedLeadSegmentMappings[mappingIndex];

                if (mapping.AssembledSegmentIndex == assembledSegmentIndex)
                {
                    float sourceSegmentT = Mathf.Lerp(mapping.SourceStartT, mapping.SourceEndT, assembledSegmentT);
                    leadOrbitDistance = mapping.SourceSegmentStartDistance + GetSourceSegmentDistance(mapping.SourceSegment, sourceSegmentT);
                    return true;
                }
            }

            return false;
        }

        public void Rebuild()
        {
            CreateSourceOrbits();
            samples.Clear();
            retainedLeadSegmentMappings.Clear();
            segmentSources.Clear();
            segments.Clear();
            mergedMarkers.Clear();
            watchPointKeys.Clear();
            Length = 0f;
            leadOrbitLength = 0f;
            retainedLeadStartDistance = 0f;
            AssemblyResult = ChainOrbitAssemblyResult.NotAssembled;

            if (!HasValidInputs())
            {
                AssemblyResult = ChainOrbitAssemblyResult.ConnectorGenerationFailed;
                return;
            }

            if (bodyProfiles.Count == 1)
            {
                AddRetainedInterval(0, 0f, sourceOrbits[0].Length, true);
            }
            else
            {
                AddLeadRetainedSegments();
            }

            for (int connectorIndex = 0; connectorIndex < connectorPairs.Count; connectorIndex++)
            {
                AddConnector(connectorPairs[connectorIndex].RightConnector);
                int bodyIndex = connectorIndex + 1;

                if (bodyIndex < bodyProfiles.Count - 1)
                {
                    AddMiddleRightSide(bodyIndex);
                }
                else
                {
                    AddRetainedSectionComplement(bodyIndex, OrbitAttachmentEnd.Front, false);
                }
            }

            for (int connectorIndex = connectorPairs.Count - 1; connectorIndex >= 0; connectorIndex--)
            {
                AddReversedConnector(connectorPairs[connectorIndex].LeftConnector);

                if (connectorIndex > 0)
                {
                    AddMiddleLeftSide(connectorIndex);
                }
            }

            if (!AreSegmentEndpointsConnected())
            {
                AssemblyResult = ChainOrbitAssemblyResult.Disconnected;
                return;
            }

            if (!AreSegmentsPlanar())
            {
                AssemblyResult = ChainOrbitAssemblyResult.NonPlanar;
                return;
            }

            BuildArcLengthSamples();

            if (Length < MinimumLength)
            {
                AssemblyResult = ChainOrbitAssemblyResult.Degenerate;
                return;
            }

            if (HasSelfIntersection())
            {
                AssemblyResult = ChainOrbitAssemblyResult.SelfIntersecting;
                return;
            }

            AssemblyResult = ChainOrbitAssemblyResult.Valid;
            BuildMergedMarkers();
            BuildBearing();
        }

        private void BuildMergedMarkers()
        {
            for (int bodyIndex = 0; bodyIndex < bodies.Count; bodyIndex++)
            {
                VehicleOrbit orbit = bodyOrbits[bodyIndex];
                ClosedBezierOrbit sourceOrbit = sourceOrbits[bodyIndex];
                if (sourceOrbit.WatchMarkerValidationResult != OrbitWatchMarkerValidationResult.Valid)
                {
                    continue;
                }

                for (int markerIndex = 0; markerIndex < orbit.WatchMarkers.Count; markerIndex++)
                {
                    OrbitWatchMarker marker = orbit.WatchMarkers[markerIndex];
                    float markerDistance = sourceOrbit.Length * marker.NormalizedOrbitPosition;
                    float assembledDistance;
                    if (!TryGetRetainedSourceDistance(bodyIndex, markerDistance, out assembledDistance))
                    {
                        continue;
                    }

                    float progress = assembledDistance / Length;
                    Vector3 ownerPoint = marker.WatchPointLocalPosition;
                    MergedMarker merged = new MergedMarker(bodies[bodyIndex].Body, ownerPoint, ownerPoint + bodyOffsets[bodyIndex], marker.Name, assembledDistance, progress, bodies[bodyIndex].ChainIndex, marker.Id, marker.IsPointOfInterest);
                    int insertionIndex = mergedMarkers.Count;
                    mergedMarkers.Add(merged);
                    while (insertionIndex > 0 && mergedMarkers[insertionIndex - 1].NormalizedProgress > progress)
                    {
                        mergedMarkers[insertionIndex] = mergedMarkers[insertionIndex - 1];
                        insertionIndex--;
                    }

                    mergedMarkers[insertionIndex] = merged;
                }
            }

            for (int markerIndex = 0; markerIndex < mergedMarkers.Count; markerIndex++)
            {
                MergedMarker marker = mergedMarkers[markerIndex];
                watchPointKeys.Add(new WatchPointKey(marker.NormalizedProgress, marker.RootLocalWatchPoint, marker.ChainIndex));
            }

            watchPointCurve.Rebuild(watchPointKeys);
        }

        private bool TryGetRetainedSourceDistance(int bodyIndex, float sourceDistance, out float assembledDistance)
        {
            assembledDistance = 0f;
            float sourceLength = sourceOrbits[bodyIndex].Length;
            for (int segmentIndex = 0; segmentIndex < segmentSources.Count; segmentIndex++)
            {
                AssembledOrbitSegmentSource source = segmentSources[segmentIndex];
                if (source == null || source.BodyIndex != bodyIndex)
                {
                    continue;
                }

                float startDistance = source.SourceSegmentStartDistance + GetSourceSegmentDistance(source.SourceSegment, source.SourceStartT);
                float endDistance = source.SourceSegmentStartDistance + GetSourceSegmentDistance(source.SourceSegment, source.SourceEndT);
                float candidateDistance = sourceDistance;
                if (candidateDistance < startDistance - MinimumLength)
                {
                    candidateDistance += sourceLength;
                }

                if (candidateDistance < startDistance - MinimumLength || candidateDistance > endDistance + MinimumLength)
                {
                    continue;
                }

                float sourceT = GetSegmentParameter(source.SourceSegment, candidateDistance - source.SourceSegmentStartDistance);
                float assembledT = (sourceT - source.SourceStartT) / (source.SourceEndT - source.SourceStartT);
                assembledDistance = GetAssembledSegmentDistance(segmentIndex, Mathf.Clamp01(assembledT));
                return true;
            }

            return false;
        }

        private void BuildBearing()
        {
            bearingPositions.Clear();
            bearingDistances.Clear();
            for (int sampleIndex = 0; sampleIndex < samples.Count; sampleIndex++)
            {
                bearingPositions.Add(samples[sampleIndex].Position);
                bearingDistances.Add(samples[sampleIndex].Distance);
            }

            bearing.Rebuild(bearingPositions, bearingDistances, Length);
        }

        private void CreateSourceOrbits()
        {
            sourceOrbits.Clear();

            if (bodyProfiles == null)
            {
                return;
            }

            for (int profileIndex = 0; profileIndex < bodyProfiles.Count; profileIndex++)
            {
                VehicleProfile profile = bodyProfiles[profileIndex];

                if (profile == null || bodyOrbits[profileIndex] == null)
                {
                    sourceOrbits.Add(null);
                }
                else
                {
                    sourceOrbits.Add(new ClosedBezierOrbit(bodyOrbits[profileIndex]));
                }
            }
        }

        private bool HasValidInputs()
        {
            if (bodies == null || bodyOffsets == null || connectorPairs == null || bodyProfiles.Count < 1 || rootIndex < 0 || rootIndex >= bodyProfiles.Count || bodyOffsets.Count != bodyProfiles.Count || connectorPairs.Count != bodyProfiles.Count - 1 || sourceOrbits.Count != bodyProfiles.Count)
            {
                return false;
            }

            for (int bodyIndex = 0; bodyIndex < bodyProfiles.Count; bodyIndex++)
            {
                if (bodyProfiles[bodyIndex] == null || bodyOrbits[bodyIndex] == null || sourceOrbits[bodyIndex] == null || bodies[bodyIndex].ChainIndex != bodyIndex)
                {
                    return false;
                }

                if (sourceOrbits[bodyIndex].ValidationResult != OrbitValidationResult.Valid
                    || sourceOrbits[bodyIndex].RemovableSectionValidationResult != OrbitRemovableSectionValidationResult.Valid)
                {
                    return false;
                }
            }

            for (int connectorIndex = 0; connectorIndex < connectorPairs.Count; connectorIndex++)
            {
                if (connectorPairs[connectorIndex] == null || connectorPairs[connectorIndex].LeftConnector == null || connectorPairs[connectorIndex].RightConnector == null)
                {
                    return false;
                }
            }

            return true;
        }

        private bool AreWatchMarkersValid()
        {
            for (int orbitIndex = 0; orbitIndex < sourceOrbits.Count; orbitIndex++)
            {
                if (sourceOrbits[orbitIndex] == null || sourceOrbits[orbitIndex].WatchMarkerValidationResult != OrbitWatchMarkerValidationResult.Valid)
                {
                    return false;
                }
            }

            return sourceOrbits.Count > 0;
        }

        private bool TryGetOrbitSegmentPosition(float distance, out int segmentIndex, out float segmentT)
        {
            segmentIndex = 0;
            segmentT = 0f;

            if (Length <= 0f || samples.Count == 0)
            {
                return false;
            }

            float wrappedDistance = Mathf.Repeat(distance, Length);

            for (int sampleIndex = 1; sampleIndex < samples.Count; sampleIndex++)
            {
                OrbitArcLengthSample previousSample = samples[sampleIndex - 1];
                OrbitArcLengthSample currentSample = samples[sampleIndex];

                if (wrappedDistance <= currentSample.Distance)
                {
                    float sampleDistance = currentSample.Distance - previousSample.Distance;

                    if (sampleDistance <= 0f)
                    {
                        continue;
                    }

                    float previousSegmentT = 0f;
                    if (previousSample.SegmentIndex == currentSample.SegmentIndex)
                    {
                        previousSegmentT = previousSample.SegmentT;
                    }

                    segmentIndex = currentSample.SegmentIndex;
                    segmentT = Mathf.Lerp(previousSegmentT, currentSample.SegmentT, (wrappedDistance - previousSample.Distance) / sampleDistance);
                    return true;
                }
            }

            return false;
        }

        private void AddLeadRetainedSegments()
        {
            AddRetainedSectionComplement(0, OrbitAttachmentEnd.Rear, true);
        }

        private void AddMiddleRightSide(int bodyIndex)
        {
            float sourceLength = sourceOrbits[bodyIndex].Length;
            OrbitRemovableSection frontSection = bodyOrbits[bodyIndex].FrontRemovableSection;
            OrbitRemovableSection rearSection = bodyOrbits[bodyIndex].RearRemovableSection;
            AddRetainedInterval(bodyIndex, sourceLength * frontSection.NormalizedEndPosition, sourceLength * rearSection.NormalizedStartPosition, false);
        }

        private void AddMiddleLeftSide(int bodyIndex)
        {
            float sourceLength = sourceOrbits[bodyIndex].Length;
            OrbitRemovableSection frontSection = bodyOrbits[bodyIndex].FrontRemovableSection;
            OrbitRemovableSection rearSection = bodyOrbits[bodyIndex].RearRemovableSection;
            AddRetainedInterval(bodyIndex, sourceLength * rearSection.NormalizedEndPosition, sourceLength * frontSection.NormalizedStartPosition, false);
        }

        private void AddRetainedSectionComplement(int bodyIndex, OrbitAttachmentEnd attachmentEnd, bool isLeadBody)
        {
            float sourceLength = sourceOrbits[bodyIndex].Length;
            OrbitRemovableSection removableSection = bodyOrbits[bodyIndex].GetRemovableSection(attachmentEnd);
            AddRetainedInterval(bodyIndex, sourceLength * removableSection.NormalizedEndPosition, sourceLength * removableSection.NormalizedStartPosition, isLeadBody);
        }

        private void AddRetainedInterval(int bodyIndex, float startDistance, float endDistance, bool isLeadBody)
        {
            List<OrbitSourceSegment> sourceSegments = CreateSourceSegments(bodyIndex);
            float sourceLength = GetSourceLength(sourceSegments);

            if (isLeadBody)
            {
                leadOrbitLength = sourceLength;
                retainedLeadStartDistance = startDistance;
            }

            if (endDistance <= startDistance)
            {
                endDistance += sourceLength;
            }

            float currentDistance = startDistance;

            while (currentDistance < endDistance - MinimumLength)
            {
                float wrappedDistance = Mathf.Repeat(currentDistance, sourceLength);
                OrbitSourceSegment sourceSegment = GetSourceSegment(sourceSegments, wrappedDistance);
                float cycleStartDistance = currentDistance - wrappedDistance;
                float sourceStartDistance = cycleStartDistance + sourceSegment.StartDistance;
                float sourceEndDistance = cycleStartDistance + sourceSegment.EndDistance;

                if (sourceEndDistance <= currentDistance)
                {
                    sourceStartDistance += sourceLength;
                    sourceEndDistance += sourceLength;
                }

                float nextDistance = Mathf.Min(endDistance, sourceEndDistance);
                float startT = GetSegmentParameter(sourceSegment, currentDistance - sourceStartDistance);
                float endT = GetSegmentParameter(sourceSegment, nextDistance - sourceStartDistance);
                int assembledSegmentIndex = segments.Count;
                segments.Add(CreateSubsegment(sourceSegment.Segment, startT, endT));
                segmentSources.Add(new AssembledOrbitSegmentSource(sourceOrbits[bodyIndex], sourceSegment, sourceStartDistance, startT, endT, bodyOffsets[bodyIndex], bodyIndex));

                if (isLeadBody)
                {
                    retainedLeadSegmentMappings.Add(new RetainedLeadSegmentMapping(sourceSegment, sourceStartDistance, currentDistance, nextDistance, startT, endT, assembledSegmentIndex));
                }

                currentDistance = nextDistance;
            }
        }

        private void AddConnector(GeneratedOrbitConnector connector)
        {
            segments.Add(new AssembledBezierOrbitSegment(connector.StartPosition, connector.StartControlPoint, connector.EndControlPoint, connector.EndPosition));
            segmentSources.Add(null);
        }

        private void AddReversedConnector(GeneratedOrbitConnector connector)
        {
            AssembledBezierOrbitSegment segment = new AssembledBezierOrbitSegment(connector.StartPosition, connector.StartControlPoint, connector.EndControlPoint, connector.EndPosition);
            segments.Add(segment.CreateReversedSegment());
            segmentSources.Add(null);
        }

        private List<OrbitSourceSegment> CreateSourceSegments(int bodyIndex)
        {
            VehicleOrbit vehicleOrbit = bodyOrbits[bodyIndex];
            Vector3 positionOffset = bodyOffsets[bodyIndex];
            List<OrbitSourceSegment> sourceSegments = new List<OrbitSourceSegment>();
            float startDistance = 0f;

            for (int segmentIndex = 0; segmentIndex < vehicleOrbit.Knots.Count; segmentIndex++)
            {
                int nextKnotIndex = segmentIndex + 1;

                if (nextKnotIndex == vehicleOrbit.Knots.Count)
                {
                    nextKnotIndex = 0;
                }

                BezierOrbitKnot startKnot = vehicleOrbit.Knots[segmentIndex];
                BezierOrbitKnot endKnot = vehicleOrbit.Knots[nextKnotIndex];
                Quaternion orientationAdjustment = vehicleOrbit.OrientationAdjustment;
                AssembledBezierOrbitSegment segment = new AssembledBezierOrbitSegment(orientationAdjustment * startKnot.Anchor + positionOffset, orientationAdjustment * startKnot.OutgoingControlPoint + positionOffset, orientationAdjustment * endKnot.IncomingControlPoint + positionOffset, orientationAdjustment * endKnot.Anchor + positionOffset);
                float segmentLength = GetSegmentLength(segment);
                sourceSegments.Add(new OrbitSourceSegment(segment, startDistance, startDistance + segmentLength));
                startDistance += segmentLength;
            }

            return sourceSegments;
        }

        private float GetSourceLength(List<OrbitSourceSegment> sourceSegments)
        {
            if (sourceSegments.Count == 0)
            {
                return 0f;
            }

            return sourceSegments[sourceSegments.Count - 1].EndDistance;
        }

        private OrbitSourceSegment GetSourceSegment(List<OrbitSourceSegment> sourceSegments, float distance)
        {
            for (int segmentIndex = 0; segmentIndex < sourceSegments.Count; segmentIndex++)
            {
                if (distance < sourceSegments[segmentIndex].EndDistance - MinimumLength)
                {
                    return sourceSegments[segmentIndex];
                }
            }

            return sourceSegments[sourceSegments.Count - 1];
        }

        private float GetSegmentParameter(OrbitSourceSegment sourceSegment, float distance)
        {
            if (distance <= 0f)
            {
                return 0f;
            }

            float segmentLength = sourceSegment.EndDistance - sourceSegment.StartDistance;

            if (distance >= segmentLength)
            {
                return 1f;
            }

            float previousDistance = 0f;
            Vector3 previousPosition = sourceSegment.Segment.EvaluatePosition(0f);

            for (int sampleIndex = 1; sampleIndex <= SamplesPerSegment; sampleIndex++)
            {
                float currentT = (float)sampleIndex / SamplesPerSegment;
                Vector3 currentPosition = sourceSegment.Segment.EvaluatePosition(currentT);
                float currentDistance = previousDistance + Vector3.Distance(previousPosition, currentPosition);

                if (distance <= currentDistance)
                {
                    float previousT = (float)(sampleIndex - 1) / SamplesPerSegment;
                    return Mathf.Lerp(previousT, currentT, (distance - previousDistance) / (currentDistance - previousDistance));
                }

                previousDistance = currentDistance;
                previousPosition = currentPosition;
            }

            return 1f;
        }

        private float GetSourceSegmentDistance(OrbitSourceSegment sourceSegment, float segmentT)
        {
            if (segmentT <= 0f)
            {
                return 0f;
            }

            float segmentLength = sourceSegment.EndDistance - sourceSegment.StartDistance;

            if (segmentT >= 1f)
            {
                return segmentLength;
            }

            float previousDistance = 0f;
            float previousT = 0f;
            Vector3 previousPosition = sourceSegment.Segment.EvaluatePosition(0f);

            for (int sampleIndex = 1; sampleIndex <= SamplesPerSegment; sampleIndex++)
            {
                float currentT = (float)sampleIndex / SamplesPerSegment;
                Vector3 currentPosition = sourceSegment.Segment.EvaluatePosition(currentT);
                float currentDistance = previousDistance + Vector3.Distance(previousPosition, currentPosition);

                if (segmentT <= currentT)
                {
                    return Mathf.Lerp(previousDistance, currentDistance, (segmentT - previousT) / (currentT - previousT));
                }

                previousDistance = currentDistance;
                previousT = currentT;
                previousPosition = currentPosition;
            }

            return segmentLength;
        }

        private AssembledBezierOrbitSegment CreateSubsegment(AssembledBezierOrbitSegment sourceSegment, float startT, float endT)
        {
            Vector3 startPosition = sourceSegment.EvaluatePosition(startT);
            Vector3 endPosition = sourceSegment.EvaluatePosition(endT);
            float normalizedRange = endT - startT;
            Vector3 startControlPoint = startPosition + normalizedRange * EvaluateDerivative(sourceSegment, startT) / 3f;
            Vector3 endControlPoint = endPosition - normalizedRange * EvaluateDerivative(sourceSegment, endT) / 3f;
            return new AssembledBezierOrbitSegment(startPosition, startControlPoint, endControlPoint, endPosition);
        }

        private Vector3 EvaluateDerivative(AssembledBezierOrbitSegment segment, float segmentT)
        {
            float inverseT = 1f - segmentT;
            Vector3 firstDifference = segment.StartControlPoint - segment.StartPosition;
            Vector3 secondDifference = segment.EndControlPoint - segment.StartControlPoint;
            Vector3 thirdDifference = segment.EndPosition - segment.EndControlPoint;
            return 3f * inverseT * inverseT * firstDifference + 6f * inverseT * segmentT * secondDifference + 3f * segmentT * segmentT * thirdDifference;
        }

        private bool AreSegmentEndpointsConnected()
        {
            if (segments.Count == 0)
            {
                return false;
            }

            for (int segmentIndex = 0; segmentIndex < segments.Count; segmentIndex++)
            {
                int nextSegmentIndex = segmentIndex + 1;

                if (nextSegmentIndex == segments.Count)
                {
                    nextSegmentIndex = 0;
                }

                if (Vector3.Distance(segments[segmentIndex].EndPosition, segments[nextSegmentIndex].StartPosition) > PlanarTolerance)
                {
                    return false;
                }
            }

            return true;
        }

        private bool AreSegmentsPlanar()
        {
            Vector3 planeNormal = bodyOrbits[rootIndex].OrientationAdjustment * Vector3.up;
            Vector3 planePosition = segments[0].StartPosition;

            for (int segmentIndex = 0; segmentIndex < segments.Count; segmentIndex++)
            {
                AssembledBezierOrbitSegment segment = segments[segmentIndex];

                if (!IsPointOnPlane(segment.StartPosition, planePosition, planeNormal) || !IsPointOnPlane(segment.StartControlPoint, planePosition, planeNormal) || !IsPointOnPlane(segment.EndControlPoint, planePosition, planeNormal) || !IsPointOnPlane(segment.EndPosition, planePosition, planeNormal))
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsPointOnPlane(Vector3 point, Vector3 planePosition, Vector3 planeNormal)
        {
            return Mathf.Abs(Vector3.Dot(point - planePosition, planeNormal)) <= PlanarTolerance;
        }

        private void BuildArcLengthSamples()
        {
            samples.Clear();
            Length = 0f;
            samples.Add(new OrbitArcLengthSample(segments[0].StartPosition, 0f, 0, 0f));

            for (int segmentIndex = 0; segmentIndex < segments.Count; segmentIndex++)
            {
                AssembledBezierOrbitSegment segment = segments[segmentIndex];
                Vector3 previousPosition = segment.EvaluatePosition(0f);

                for (int sampleIndex = 1; sampleIndex <= SamplesPerSegment; sampleIndex++)
                {
                    float segmentT = (float)sampleIndex / SamplesPerSegment;
                    Vector3 currentPosition = segment.EvaluatePosition(segmentT);
                    Length += Vector3.Distance(previousPosition, currentPosition);
                    samples.Add(new OrbitArcLengthSample(currentPosition, Length, segmentIndex, segmentT));
                    previousPosition = currentPosition;
                }
            }
        }

        private float GetAssembledSegmentDistance(int assembledSegmentIndex, float assembledSegmentT)
        {
            float previousDistance = 0f;
            float previousT = 0f;

            for (int sampleIndex = 1; sampleIndex < samples.Count; sampleIndex++)
            {
                OrbitArcLengthSample currentSample = samples[sampleIndex];

                if (currentSample.SegmentIndex == assembledSegmentIndex)
                {
                    if (assembledSegmentT <= currentSample.SegmentT)
                    {
                        return Mathf.Lerp(previousDistance, currentSample.Distance, (assembledSegmentT - previousT) / (currentSample.SegmentT - previousT));
                    }

                    previousT = currentSample.SegmentT;
                }

                previousDistance = currentSample.Distance;
            }

            return 0f;
        }

        private float GetSegmentLength(AssembledBezierOrbitSegment segment)
        {
            float length = 0f;
            Vector3 previousPosition = segment.EvaluatePosition(0f);

            for (int sampleIndex = 1; sampleIndex <= SamplesPerSegment; sampleIndex++)
            {
                float segmentT = (float)sampleIndex / SamplesPerSegment;
                Vector3 currentPosition = segment.EvaluatePosition(segmentT);
                length += Vector3.Distance(previousPosition, currentPosition);
                previousPosition = currentPosition;
            }

            return length;
        }

        private float CalculateSignedArea()
        {
            float signedArea = 0f;

            for (int sampleIndex = 1; sampleIndex < samples.Count; sampleIndex++)
            {
                Vector3 previousPosition = samples[sampleIndex - 1].Position;
                Vector3 currentPosition = samples[sampleIndex].Position;
                signedArea += previousPosition.x * currentPosition.z - currentPosition.x * previousPosition.z;
            }

            return signedArea * 0.5f;
        }

        private bool HasSelfIntersection()
        {
            int segmentCount = samples.Count - 1;

            for (int firstSegmentIndex = 0; firstSegmentIndex < segmentCount; firstSegmentIndex++)
            {
                Vector2 firstStart = ToPlanePosition(samples[firstSegmentIndex].Position);
                Vector2 firstEnd = ToPlanePosition(samples[firstSegmentIndex + 1].Position);

                for (int secondSegmentIndex = firstSegmentIndex + 1; secondSegmentIndex < segmentCount; secondSegmentIndex++)
                {
                    if (secondSegmentIndex == firstSegmentIndex + 1 || firstSegmentIndex == 0 && secondSegmentIndex == segmentCount - 1)
                    {
                        continue;
                    }

                    Vector2 secondStart = ToPlanePosition(samples[secondSegmentIndex].Position);
                    Vector2 secondEnd = ToPlanePosition(samples[secondSegmentIndex + 1].Position);

                    if (DoSegmentsIntersect(firstStart, firstEnd, secondStart, secondEnd))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private Vector2 ToPlanePosition(Vector3 position)
        {
            return new Vector2(position.x, position.z);
        }

        private bool DoSegmentsIntersect(Vector2 firstStart, Vector2 firstEnd, Vector2 secondStart, Vector2 secondEnd)
        {
            float firstStartSide = CalculateCross(firstStart, firstEnd, secondStart);
            float firstEndSide = CalculateCross(firstStart, firstEnd, secondEnd);
            float secondStartSide = CalculateCross(secondStart, secondEnd, firstStart);
            float secondEndSide = CalculateCross(secondStart, secondEnd, firstEnd);

            if (firstStartSide == 0f && IsPointOnSegment(firstStart, firstEnd, secondStart))
            {
                return true;
            }

            if (firstEndSide == 0f && IsPointOnSegment(firstStart, firstEnd, secondEnd))
            {
                return true;
            }

            if (secondStartSide == 0f && IsPointOnSegment(secondStart, secondEnd, firstStart))
            {
                return true;
            }

            if (secondEndSide == 0f && IsPointOnSegment(secondStart, secondEnd, firstEnd))
            {
                return true;
            }

            bool firstSegmentSeparatesSecondSegment = firstStartSide > 0f && firstEndSide < 0f || firstStartSide < 0f && firstEndSide > 0f;
            bool secondSegmentSeparatesFirstSegment = secondStartSide > 0f && secondEndSide < 0f || secondStartSide < 0f && secondEndSide > 0f;
            return firstSegmentSeparatesSecondSegment && secondSegmentSeparatesFirstSegment;
        }

        private float CalculateCross(Vector2 lineStart, Vector2 lineEnd, Vector2 point)
        {
            Vector2 line = lineEnd - lineStart;
            Vector2 offset = point - lineStart;
            return line.x * offset.y - line.y * offset.x;
        }

        private bool IsPointOnSegment(Vector2 segmentStart, Vector2 segmentEnd, Vector2 point)
        {
            float minimumX = Mathf.Min(segmentStart.x, segmentEnd.x);
            float maximumX = Mathf.Max(segmentStart.x, segmentEnd.x);
            float minimumY = Mathf.Min(segmentStart.y, segmentEnd.y);
            float maximumY = Mathf.Max(segmentStart.y, segmentEnd.y);
            return point.x >= minimumX && point.x <= maximumX && point.y >= minimumY && point.y <= maximumY;
        }
    }
}
