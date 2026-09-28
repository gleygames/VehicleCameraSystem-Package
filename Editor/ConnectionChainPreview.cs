using System.Collections.Generic;
using UnityEngine;

namespace Gley.CameraSystem.Editor
{
    public class ConnectionChainPreview
    {
        private const float SampleSpacing = 0.25f;
        private const int MinimumOrbitSamples = 64;
        private const int MaximumOrbitSamples = 512;
        private const int BodyCurveSamples = 128;

        private readonly List<VehicleProfile> profiles = new List<VehicleProfile>();
        private readonly List<VehicleOrbit> bodyOrbits = new List<VehicleOrbit>();
        private readonly List<OrbitConnectorPairOverride> jointOverrides = new List<OrbitConnectorPairOverride>();
        private readonly List<Transform> models = new List<Transform>();
        private readonly List<Vector3[]> bodyCurvePoints = new List<Vector3[]>();
        private readonly List<Matrix4x4> straightMatrices = new List<Matrix4x4>();
        private readonly List<Matrix4x4> articulationMatrices = new List<Matrix4x4>();
        private readonly List<Matrix4x4> bodyMatrices = new List<Matrix4x4>();
        private readonly List<WatchPointKey> ownerKeys = new List<WatchPointKey>();
        private readonly List<Vector3> jointPivots = new List<Vector3>();
        private readonly List<Vector3> articulatedPivots = new List<Vector3>();
        private readonly Vector3[] emptyPoints = new Vector3[0];
        private readonly WatchPointCurve ownerWatchCurve = new WatchPointCurve();

        private Vector3[] orbitPoints = new Vector3[0];
        private ChainLayout layout;
        private ChainOrbit orbit;
        private VehicleOrbit rootOrbit;

        public IReadOnlyList<VehicleProfile> Profiles => profiles;
        public IReadOnlyList<VehicleOrbit> BodyOrbits => bodyOrbits;
        public IReadOnlyList<OrbitConnectorPairOverride> JointOverrides => jointOverrides;
        public IReadOnlyList<Transform> Models => models;
        public IReadOnlyList<Matrix4x4> StraightBodyMatrices => straightMatrices;
        public IReadOnlyList<Matrix4x4> BodyMatrices => bodyMatrices;
        public IReadOnlyList<Vector3> ArticulatedJointPivots => articulatedPivots;
        public Vector3[] OrbitPoints => orbitPoints;
        public ChainLayout Layout => layout;
        public ChainOrbit Orbit => orbit;
        public VehicleOrbit RootOrbit => rootOrbit;
        public Vector3 PlaneNormal { get; private set; }
        public ChainLayoutResult LayoutResult { get; private set; }
        public int RootIndex { get; private set; }
        public int OrbitPointCount { get; private set; }
        public int BodyCount => profiles.Count;
        public bool IsMerged { get; private set; }
        public bool IsOrbitValid => orbit != null && orbit.IsClosed;
        public bool HasOwnerWatchPoints => ownerKeys.Count > 0;

        public void Rebuild(VehicleProfile editedProfile, Transform editedModel, VehicleOrbit selectedRootOrbit, ConnectionChainSelection selection)
        {
            Clear();
            if (editedProfile == null || selection == null)
            {
                return;
            }

            rootOrbit = selectedRootOrbit;
            RootIndex = selection.RootIndex;
            PlaneNormal = Vector3.up;
            if (rootOrbit != null)
            {
                PlaneNormal = rootOrbit.OrientationAdjustment * Vector3.up;
            }

            CollectBodies(editedProfile, editedModel, selection);
            layout = new ChainLayout(profiles, RootIndex, rootOrbit, null, jointOverrides);
            IsMerged = rootOrbit == null || rootOrbit.MergeWhenAttached || profiles.Count == 1;

            if (IsMerged)
            {
                LayoutResult = layout.LayoutResult;
                orbit = GetAssembledOrbit(layout, RootIndex);
            }
            else
            {
                VehicleProfile[] rootProfiles = { editedProfile };
                ChainLayout rootLayout = new ChainLayout(rootProfiles, 0, rootOrbit);
                LayoutResult = rootLayout.LayoutResult;
                orbit = GetAssembledOrbit(rootLayout, 0);
            }

            BuildStraightArrangement();
            BuildOrbitPoints();
            BuildBodyCurves();
            ApplyArticulation(selection);
        }

        public void ApplyArticulation(ConnectionChainSelection selection)
        {
            int bodyCount = profiles.Count;
            if (selection == null || selection.BodyCount != bodyCount || straightMatrices.Count != bodyCount)
            {
                return;
            }

            articulationMatrices.Clear();
            bodyMatrices.Clear();
            articulatedPivots.Clear();
            for (int index = 0; index < bodyCount; index++)
            {
                articulationMatrices.Add(Matrix4x4.identity);
                bodyMatrices.Add(Matrix4x4.identity);
            }

            for (int jointIndex = 0; jointIndex < bodyCount - 1; jointIndex++)
            {
                articulatedPivots.Add(Vector3.zero);
            }

            for (int jointIndex = RootIndex; jointIndex < bodyCount - 1; jointIndex++)
            {
                articulationMatrices[jointIndex + 1] = articulationMatrices[jointIndex] * RotateAboutPivot(jointPivots[jointIndex], GetArticulation(selection, jointIndex));
                articulatedPivots[jointIndex] = articulationMatrices[jointIndex].MultiplyPoint3x4(jointPivots[jointIndex]);
            }

            for (int jointIndex = RootIndex - 1; jointIndex >= 0; jointIndex--)
            {
                articulationMatrices[jointIndex] = articulationMatrices[jointIndex + 1] * RotateAboutPivot(jointPivots[jointIndex], GetArticulation(selection, jointIndex));
                articulatedPivots[jointIndex] = articulationMatrices[jointIndex + 1].MultiplyPoint3x4(jointPivots[jointIndex]);
            }

            for (int index = 0; index < bodyCount; index++)
            {
                bodyMatrices[index] = articulationMatrices[index] * straightMatrices[index];
            }

            BuildOwnerWatchCurve();
        }

        public Vector3[] GetBodyCurvePoints(int chainIndex)
        {
            if (chainIndex < 0 || chainIndex >= bodyCurvePoints.Count)
            {
                return emptyPoints;
            }

            return bodyCurvePoints[chainIndex];
        }

        public Vector3 EvaluateRootWatchPoint(float distance)
        {
            if (!IsOrbitValid || !orbit.HasValidWatchMarkers)
            {
                return Vector3.zero;
            }

            return orbit.EvaluateRootLocalWatchPoint(distance);
        }

        public Vector3 EvaluateOwnerWatchPoint(float distance)
        {
            if (!IsOrbitValid || ownerKeys.Count == 0 || orbit.Length <= 0f)
            {
                return Vector3.zero;
            }

            return ownerWatchCurve.Evaluate(Mathf.Repeat(distance, orbit.Length) / orbit.Length);
        }

        public void Clear()
        {
            profiles.Clear();
            bodyOrbits.Clear();
            jointOverrides.Clear();
            models.Clear();
            bodyCurvePoints.Clear();
            straightMatrices.Clear();
            articulationMatrices.Clear();
            bodyMatrices.Clear();
            ownerKeys.Clear();
            jointPivots.Clear();
            articulatedPivots.Clear();
            ownerWatchCurve.Rebuild(ownerKeys);
            orbitPoints = emptyPoints;
            OrbitPointCount = 0;
            layout = null;
            orbit = null;
            rootOrbit = null;
            LayoutResult = ChainLayoutResult.MissingProfile;
            RootIndex = 0;
            IsMerged = true;
        }

        private void CollectBodies(VehicleProfile editedProfile, Transform editedModel, ConnectionChainSelection selection)
        {
            int bodyCount = selection.BodyCount;
            for (int chainIndex = 0; chainIndex < bodyCount; chainIndex++)
            {
                ConnectionPartner partner = selection.GetBodyPartner(chainIndex);
                if (partner == null)
                {
                    profiles.Add(editedProfile);
                    models.Add(editedModel);
                }
                else
                {
                    profiles.Add(partner.Profile);
                    Transform partnerModel = null;
                    if (partner.Model != null)
                    {
                        partnerModel = partner.Model.transform;
                    }

                    models.Add(partnerModel);
                }

                bodyOrbits.Add(ResolveBodyOrbit(profiles[chainIndex], chainIndex));
            }

            for (int jointIndex = 0; jointIndex < bodyCount - 1; jointIndex++)
            {
                ConnectionPartner partner = selection.GetJointPartner(jointIndex);
                OrbitConnectorPairOverride connectorOverride = null;
                if (partner != null)
                {
                    connectorOverride = partner.JointOverride;
                }

                jointOverrides.Add(connectorOverride);
            }
        }

        private VehicleOrbit ResolveBodyOrbit(VehicleProfile profile, int chainIndex)
        {
            if (profile == null)
            {
                return null;
            }

            if (chainIndex == RootIndex)
            {
                return rootOrbit;
            }

            VehicleOrbit bodyOrbit = null;
            if (rootOrbit != null)
            {
                profile.TryGetOrbit(rootOrbit.Name, out bodyOrbit);
            }

            if (bodyOrbit == null)
            {
                bodyOrbit = profile.PrimaryOrbit;
            }

            return bodyOrbit;
        }

        private ChainOrbit GetAssembledOrbit(ChainLayout builtLayout, int rootIndex)
        {
            if (builtLayout.Orbit != null)
            {
                return builtLayout.Orbit;
            }

            return new ChainOrbit(builtLayout, null, rootIndex);
        }

        private void BuildStraightArrangement()
        {
            for (int index = 0; index < profiles.Count; index++)
            {
                straightMatrices.Add(Matrix4x4.Translate(GetStraightOffset(index)));
            }

            for (int jointIndex = 0; jointIndex < profiles.Count - 1; jointIndex++)
            {
                jointPivots.Add(CalculateJointPivot(jointIndex));
            }
        }

        private Vector3 GetStraightOffset(int chainIndex)
        {
            if (layout == null || chainIndex >= layout.Offsets.Count)
            {
                return Vector3.zero;
            }

            return layout.Offsets[chainIndex];
        }

        private Vector3 CalculateJointPivot(int jointIndex)
        {
            VehicleProfile frontProfile = profiles[jointIndex];
            if (frontProfile != null && frontProfile.ConnectorAnchors != null)
            {
                return GetStraightOffset(jointIndex) + frontProfile.ConnectorAnchors.RearLocalPosition;
            }

            VehicleProfile rearProfile = profiles[jointIndex + 1];
            if (rearProfile != null && rearProfile.ConnectorAnchors != null)
            {
                return GetStraightOffset(jointIndex + 1) + rearProfile.ConnectorAnchors.FrontLocalPosition;
            }

            return (GetStraightOffset(jointIndex) + GetStraightOffset(jointIndex + 1)) * 0.5f;
        }

        private void BuildOrbitPoints()
        {
            if (!IsOrbitValid || orbit.Length <= 0f)
            {
                return;
            }

            int sampleCount = Mathf.Clamp(Mathf.CeilToInt(orbit.Length / SampleSpacing), MinimumOrbitSamples, MaximumOrbitSamples);
            orbitPoints = new Vector3[sampleCount + 1];
            for (int index = 0; index < sampleCount; index++)
            {
                orbitPoints[index] = orbit.EvaluateRootLocalPosition(orbit.Length * index / sampleCount);
            }

            orbitPoints[sampleCount] = orbitPoints[0];
            OrbitPointCount = sampleCount + 1;
        }

        private void BuildBodyCurves()
        {
            for (int index = 0; index < bodyOrbits.Count; index++)
            {
                VehicleOrbit bodyOrbit = bodyOrbits[index];
                if (bodyOrbit == null)
                {
                    bodyCurvePoints.Add(emptyPoints);
                    continue;
                }

                ClosedBezierOrbit closedOrbit = new ClosedBezierOrbit(bodyOrbit);
                if (!closedOrbit.IsClosed || closedOrbit.Length <= 0f)
                {
                    bodyCurvePoints.Add(emptyPoints);
                    continue;
                }

                Vector3[] points = new Vector3[BodyCurveSamples + 1];
                for (int sampleIndex = 0; sampleIndex < BodyCurveSamples; sampleIndex++)
                {
                    points[sampleIndex] = closedOrbit.EvaluateBodyLocalPosition(closedOrbit.Length * sampleIndex / BodyCurveSamples);
                }

                points[BodyCurveSamples] = points[0];
                bodyCurvePoints.Add(points);
            }
        }

        private Matrix4x4 RotateAboutPivot(Vector3 pivot, float degrees)
        {
            return Matrix4x4.Translate(pivot) * Matrix4x4.Rotate(Quaternion.AngleAxis(degrees, PlaneNormal)) * Matrix4x4.Translate(-pivot);
        }

        private float GetArticulation(ConnectionChainSelection selection, int jointIndex)
        {
            ConnectionPartner partner = selection.GetJointPartner(jointIndex);
            if (partner == null)
            {
                return 0f;
            }

            return partner.Articulation;
        }

        private void BuildOwnerWatchCurve()
        {
            ownerKeys.Clear();
            if (IsOrbitValid && orbit.HasValidWatchMarkers)
            {
                IReadOnlyList<MergedMarker> markers = orbit.MergedMarkers;
                for (int index = 0; index < markers.Count; index++)
                {
                    MergedMarker marker = markers[index];
                    int bodyIndex = marker.ChainIndex;
                    if (!IsMerged)
                    {
                        bodyIndex = RootIndex;
                    }

                    Vector3 point = marker.RootLocalWatchPoint;
                    if (bodyIndex >= 0 && bodyIndex < bodyMatrices.Count)
                    {
                        point = bodyMatrices[bodyIndex].MultiplyPoint3x4(marker.OwnerLocalWatchPoint);
                    }

                    ownerKeys.Add(new WatchPointKey(marker.NormalizedProgress, point, bodyIndex));
                }
            }

            ownerWatchCurve.Rebuild(ownerKeys);
        }
    }
}
