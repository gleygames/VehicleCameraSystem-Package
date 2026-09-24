using System.Collections.Generic;
using UnityEngine;

namespace Gley.CameraSystem
{
    public class ChainLayout
    {
        private const float PositionTolerance = 0.0001f;

        private readonly List<GeneratedOrbitConnectorPair> connectorPairs = new List<GeneratedOrbitConnectorPair>();
        private readonly List<Vector3> offsets = new List<Vector3>();
        private readonly List<VehicleOrbit> orbits = new List<VehicleOrbit>();
        private readonly IReadOnlyList<VehicleProfile> profiles;
        private readonly IReadOnlyList<OrbitConnectorPairOverride> pairOverrides;
        private readonly IReadOnlyList<OrbitConnectorPairOverride> jointOverrides;
        private readonly VehicleOrbit rootOrbit;
        private readonly int rootIndex;

        public IReadOnlyList<GeneratedOrbitConnectorPair> ConnectorPairs => connectorPairs;
        public IReadOnlyList<Vector3> Offsets => offsets;
        public IReadOnlyList<VehicleOrbit> Orbits => orbits;
        public ChainLayoutResult LayoutResult { get; private set; }

        public ChainLayout(IReadOnlyList<VehicleProfile> profiles, int rootIndex, VehicleOrbit rootOrbit, IReadOnlyList<OrbitConnectorPairOverride> pairOverrides = null, IReadOnlyList<OrbitConnectorPairOverride> jointOverrides = null)
        {
            this.profiles = profiles;
            this.rootIndex = rootIndex;
            this.rootOrbit = rootOrbit;
            this.pairOverrides = pairOverrides;
            this.jointOverrides = jointOverrides;
            Rebuild();
        }

        public List<ChainBody> CreateBodies(IReadOnlyList<Transform> bodyTransforms)
        {
            List<ChainBody> bodies = new List<ChainBody>();
            if (profiles == null || orbits.Count != profiles.Count || offsets.Count != profiles.Count || connectorPairs.Count != profiles.Count - 1)
            {
                return bodies;
            }

            for (int index = 0; index < profiles.Count; index++)
            {
                Transform body = null;
                if (bodyTransforms != null && index < bodyTransforms.Count)
                {
                    body = bodyTransforms[index];
                }

                bodies.Add(new ChainBody(body, profiles[index], orbits[index], offsets[index], index));
            }

            return bodies;
        }

        public void Rebuild()
        {
            connectorPairs.Clear();
            offsets.Clear();
            orbits.Clear();
            LayoutResult = ChainLayoutResult.MissingProfile;
            if (profiles == null || profiles.Count == 0 || rootIndex < 0 || rootIndex >= profiles.Count)
            {
                return;
            }

            for (int index = 0; index < profiles.Count; index++)
            {
                VehicleProfile profile = profiles[index];
                if (profile == null)
                {
                    return;
                }

                VehicleOrbit orbit = null;
                if (index == rootIndex)
                {
                    orbit = rootOrbit;
                }
                else
                {
                    if (rootOrbit != null)
                    {
                        profile.TryGetOrbit(rootOrbit.Name, out orbit);
                    }

                    if (orbit == null)
                    {
                        orbit = profile.PrimaryOrbit;
                    }
                }

                if (orbit == null)
                {
                    LayoutResult = ChainLayoutResult.MissingOrbit;
                    return;
                }

                orbits.Add(orbit);
                offsets.Add(Vector3.zero);
            }

            LayoutResult = ChainLayoutResult.InvalidConnector;
            for (int index = rootIndex; index < profiles.Count - 1; index++)
            {
                if (!TryBuildPair(index))
                {
                    return;
                }
            }

            for (int index = rootIndex - 1; index >= 0; index--)
            {
                if (!TryBuildPair(index))
                {
                    return;
                }
            }

            for (int index = 0; index < profiles.Count - 1; index++)
            {
                GeneratedOrbitConnectorPair pair;
                if (!TryCreatePair(index, out pair))
                {
                    return;
                }

                connectorPairs.Add(TranslatePair(pair, offsets[index]));
            }

            LayoutResult = ChainLayoutResult.Valid;
            ChainOrbit assembled = new ChainOrbit(CreateBodies(null), rootIndex, connectorPairs);
            if (assembled.AssemblyResult == ChainOrbitAssemblyResult.SelfIntersecting)
            {
                LayoutResult = ChainLayoutResult.SelfIntersecting;
            }
            else if (assembled.AssemblyResult == ChainOrbitAssemblyResult.Disconnected || assembled.AssemblyResult == ChainOrbitAssemblyResult.Degenerate)
            {
                LayoutResult = ChainLayoutResult.Open;
            }
            else if (assembled.AssemblyResult != ChainOrbitAssemblyResult.Valid)
            {
                LayoutResult = ChainLayoutResult.InvalidConnector;
            }
        }

        private bool TryBuildPair(int index)
        {
            VehicleConnectorAnchors frontAnchors = profiles[index].ConnectorAnchors;
            VehicleConnectorAnchors rearAnchors = profiles[index + 1].ConnectorAnchors;
            if (frontAnchors == null || rearAnchors == null)
            {
                return false;
            }

            Vector3 frontPlaneNormal = orbits[index].OrientationAdjustment * Vector3.up;
            Vector3 rearPlaneNormal = orbits[index + 1].OrientationAdjustment * Vector3.up;
            if (Mathf.Abs(Vector3.Dot(frontAnchors.FrontLocalPosition, frontPlaneNormal)) > PositionTolerance
                || Mathf.Abs(Vector3.Dot(frontAnchors.RearLocalPosition, frontPlaneNormal)) > PositionTolerance
                || Mathf.Abs(Vector3.Dot(rearAnchors.FrontLocalPosition, rearPlaneNormal)) > PositionTolerance
                || Mathf.Abs(Vector3.Dot(rearAnchors.RearLocalPosition, rearPlaneNormal)) > PositionTolerance)
            {
                return false;
            }

            Vector3 relativeOffset = frontAnchors.RearLocalPosition - rearAnchors.FrontLocalPosition;
            if (index >= rootIndex)
            {
                offsets[index + 1] = offsets[index] + relativeOffset;
            }
            else
            {
                offsets[index] = offsets[index + 1] - relativeOffset;
            }

            return true;
        }

        private bool TryCreatePair(int index, out GeneratedOrbitConnectorPair pair)
        {
            pair = null;
            ClosedBezierOrbit frontOrbit = new ClosedBezierOrbit(orbits[index]);
            ClosedBezierOrbit rearOrbit = new ClosedBezierOrbit(orbits[index + 1]);
            if (frontOrbit.ValidationResult != OrbitValidationResult.Valid || rearOrbit.ValidationResult != OrbitValidationResult.Valid
                || frontOrbit.RemovableSectionValidationResult != OrbitRemovableSectionValidationResult.Valid
                || rearOrbit.RemovableSectionValidationResult != OrbitRemovableSectionValidationResult.Valid)
            {
                return false;
            }

            Vector3 frontStart = frontOrbit.EvaluateBodyLocalRemovableSectionStart(OrbitAttachmentEnd.Rear);
            Vector3 frontEnd = frontOrbit.EvaluateBodyLocalRemovableSectionEnd(OrbitAttachmentEnd.Rear);
            Vector3 rearStart = rearOrbit.EvaluateBodyLocalRemovableSectionStart(OrbitAttachmentEnd.Front) + offsets[index + 1] - offsets[index];
            Vector3 rearEnd = rearOrbit.EvaluateBodyLocalRemovableSectionEnd(OrbitAttachmentEnd.Front) + offsets[index + 1] - offsets[index];
            Vector3 frontLeft = frontStart;
            Vector3 frontRight = frontEnd;
            Vector3 rearLeft = rearStart;
            Vector3 rearRight = rearEnd;
            Vector3 rightDirection = orbits[rootIndex].OrientationAdjustment * Vector3.right;
            if (Vector3.Dot(frontLeft, rightDirection) > Vector3.Dot(frontRight, rightDirection))
            {
                frontLeft = frontEnd;
                frontRight = frontStart;
            }

            if (Vector3.Dot(rearLeft, rightDirection) > Vector3.Dot(rearRight, rightDirection))
            {
                rearLeft = rearEnd;
                rearRight = rearStart;
            }

            GeneratedOrbitConnectorPair generated = new GeneratedOrbitConnectorPair(CreateStraightConnector(frontLeft, rearLeft), CreateStraightConnector(frontRight, rearRight));
            if (jointOverrides != null && index < jointOverrides.Count && jointOverrides[index] != null)
            {
                OrbitConnectorPairOverride jointOverride = jointOverrides[index];
                if (!jointOverride.Matches(profiles[index], profiles[index + 1], OrbitAttachmentEnd.Rear, OrbitAttachmentEnd.Front))
                {
                    return false;
                }

                GeneratedOrbitConnectorPair replacement = jointOverride.CreateConnectorPair();
                if (!EndpointsMatch(generated, replacement))
                {
                    return false;
                }

                pair = replacement;
                return true;
            }

            if (pairOverrides != null)
            {
                for (int overrideIndex = 0; overrideIndex < pairOverrides.Count; overrideIndex++)
                {
                    OrbitConnectorPairOverride candidate = pairOverrides[overrideIndex];
                    if (candidate == null || !candidate.Matches(profiles[index], profiles[index + 1], OrbitAttachmentEnd.Rear, OrbitAttachmentEnd.Front))
                    {
                        continue;
                    }

                    GeneratedOrbitConnectorPair replacement = candidate.CreateConnectorPair();
                    if (!EndpointsMatch(generated, replacement))
                    {
                        return false;
                    }

                    pair = replacement;
                    return true;
                }
            }

            pair = generated;
            return true;
        }

        private GeneratedOrbitConnector CreateStraightConnector(Vector3 start, Vector3 end)
        {
            Vector3 span = end - start;
            return new GeneratedOrbitConnector(start, start + span / 3f, start + span * (2f / 3f), end);
        }

        private bool EndpointsMatch(GeneratedOrbitConnectorPair generated, GeneratedOrbitConnectorPair replacement)
        {
            return replacement != null
                && Vector3.Distance(generated.LeftConnector.StartPosition, replacement.LeftConnector.StartPosition) <= PositionTolerance
                && Vector3.Distance(generated.LeftConnector.EndPosition, replacement.LeftConnector.EndPosition) <= PositionTolerance
                && Vector3.Distance(generated.RightConnector.StartPosition, replacement.RightConnector.StartPosition) <= PositionTolerance
                && Vector3.Distance(generated.RightConnector.EndPosition, replacement.RightConnector.EndPosition) <= PositionTolerance;
        }

        private GeneratedOrbitConnectorPair TranslatePair(GeneratedOrbitConnectorPair pair, Vector3 offset)
        {
            return new GeneratedOrbitConnectorPair(TranslateConnector(pair.LeftConnector, offset), TranslateConnector(pair.RightConnector, offset));
        }

        private GeneratedOrbitConnector TranslateConnector(GeneratedOrbitConnector connector, Vector3 offset)
        {
            return new GeneratedOrbitConnector(connector.StartPosition + offset, connector.StartControlPoint + offset, connector.EndControlPoint + offset, connector.EndPosition + offset);
        }
    }
}
