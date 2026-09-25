using System.Collections.Generic;
using UnityEngine;

namespace Gley.CameraSystem.Editor
{
    public class ProfileGenerator
    {
        public const string DrivingName = "Driving";
        public const string PresentationName = "Presentation";
        public const string InteriorName = "Interior";
        public const string FixedName = "Fixed";
        public const string RearMarkerName = "Rear";
        public const string RightMarkerName = "Right";
        public const string LeftMarkerName = "Left";
        public const string FrontMarkerName = "Front";
        public const string CentreMarkerName = "Centre";
        private const float RearBearing = 0f;
        private const float RightBearing = 90f;
        private const float LeftBearing = -90f;
        private const float FrontBearing = 180f;
        private const float ArcHandleScale = 0.55228475f;
        private const int KnotCount = 9;
        private const int BoxCornerCount = 8;

        private readonly Vector3[] anchors = new Vector3[KnotCount];
        private readonly Vector3[] incomingHandles = new Vector3[KnotCount];
        private readonly Vector3[] outgoingHandles = new Vector3[KnotCount];
        private readonly ProfileGenerationSettings settings;

        private Bounds combinedBounds;
        private float curveRear;
        private float curveFront;
        private float curveMiddle;
        private float cornerRadius;
        private bool hasBounds;

        public ProfileGenerator(ProfileGenerationSettings generationSettings)
        {
            settings = generationSettings;
        }

        public ProfileGenerationResult Generate(Transform root, IReadOnlyList<Renderer> renderers, IReadOnlyList<Collider> colliders, IReadOnlyList<Transform> exclusions, GeneratorPresets presets, VehicleProfile target)
        {
            if (root == null)
            {
                return ProfileGenerationResult.MissingRoot;
            }

            if (target == null)
            {
                return ProfileGenerationResult.MissingProfile;
            }

            if (!TryCalculateBounds(root, renderers, colliders, exclusions))
            {
                return ProfileGenerationResult.NoGeometry;
            }

            target.EnsureProfileId();
            CalculateCurveAnchors();

            float baseHeight = combinedBounds.center.y;
            float roofOffset = combinedBounds.max.y - baseHeight;
            float minimumHeight = -settings.HeightBelowBase;
            float maximumHeight = roofOffset + settings.HeightAboveRoof;
            float drivingHeight = Mathf.Clamp(roofOffset + settings.DrivingDefaultHeightAboveRoof, minimumHeight, maximumHeight);

            VehicleOrbit drivingOrbit = ConfigureOrbit(target, DrivingName, baseHeight, minimumHeight, maximumHeight);
            VehicleOrbit presentationOrbit = ConfigureOrbit(target, PresentationName, baseHeight, minimumHeight, maximumHeight);
            ClosedBezierOrbit closedOrbit = new ClosedBezierOrbit(drivingOrbit);
            ConfigureRemovableSections(drivingOrbit, closedOrbit.Length);
            ConfigureRemovableSections(presentationOrbit, closedOrbit.Length);
            ConfigureDrivingMarkers(target, drivingOrbit, closedOrbit, baseHeight + drivingHeight);
            SetMarker(target, presentationOrbit, CentreMarkerName, GetNormalizedPositionAtBearing(closedOrbit, RearBearing), combinedBounds.center);
            ConfigureConnectorAnchors(target, baseHeight);
            ConfigureSeatAndFixedPose(target);
            ConfigureViews(target, presets, drivingOrbit, presentationOrbit, drivingHeight);
            return ProfileGenerationResult.Generated;
        }

        private bool TryCalculateBounds(Transform root, IReadOnlyList<Renderer> renderers, IReadOnlyList<Collider> colliders, IReadOnlyList<Transform> exclusions)
        {
            combinedBounds = new Bounds();
            hasBounds = false;

            if (renderers != null)
            {
                for (int index = 0; index < renderers.Count; index++)
                {
                    Renderer renderer = renderers[index];
                    if (renderer == null || IsExcluded(renderer.transform, exclusions))
                    {
                        continue;
                    }

                    EncapsulateRenderer(root, renderer);
                }
            }

            if (colliders != null)
            {
                for (int index = 0; index < colliders.Count; index++)
                {
                    Collider collider = colliders[index];
                    if (collider == null || IsExcluded(collider.transform, exclusions))
                    {
                        continue;
                    }

                    EncapsulateCollider(root, collider);
                }
            }

            return hasBounds;
        }

        private bool IsExcluded(Transform candidate, IReadOnlyList<Transform> exclusions)
        {
            if (exclusions == null)
            {
                return false;
            }

            for (int index = 0; index < exclusions.Count; index++)
            {
                Transform exclusion = exclusions[index];
                if (exclusion != null && (candidate == exclusion || candidate.IsChildOf(exclusion)))
                {
                    return true;
                }
            }

            return false;
        }

        private void EncapsulateRenderer(Transform root, Renderer renderer)
        {
            MeshRenderer meshRenderer = renderer as MeshRenderer;
            if (meshRenderer != null)
            {
                MeshFilter meshFilter = meshRenderer.GetComponent<MeshFilter>();
                if (meshFilter != null && meshFilter.sharedMesh != null)
                {
                    EncapsulateLocalBox(root, meshRenderer.transform, meshFilter.sharedMesh.bounds);
                    return;
                }
            }

            SkinnedMeshRenderer skinnedMeshRenderer = renderer as SkinnedMeshRenderer;
            if (skinnedMeshRenderer != null)
            {
                Transform boundsSpace = skinnedMeshRenderer.rootBone;
                if (boundsSpace == null)
                {
                    boundsSpace = skinnedMeshRenderer.transform;
                }

                EncapsulateLocalBox(root, boundsSpace, skinnedMeshRenderer.localBounds);
                return;
            }

            EncapsulateWorldBox(root, renderer.bounds);
        }

        private void EncapsulateLocalBox(Transform root, Transform space, Bounds localBox)
        {
            Matrix4x4 matrix = root.worldToLocalMatrix * space.localToWorldMatrix;
            EncapsulateCorners(matrix, localBox);
        }

        private void EncapsulateCorners(Matrix4x4 matrix, Bounds box)
        {
            Vector3 minimum = box.min;
            Vector3 maximum = box.max;

            for (int corner = 0; corner < BoxCornerCount; corner++)
            {
                Vector3 point = minimum;
                if ((corner & 1) != 0)
                {
                    point.x = maximum.x;
                }

                if ((corner & 2) != 0)
                {
                    point.y = maximum.y;
                }

                if ((corner & 4) != 0)
                {
                    point.z = maximum.z;
                }

                EncapsulatePoint(matrix.MultiplyPoint3x4(point));
            }
        }

        private void EncapsulatePoint(Vector3 point)
        {
            if (!hasBounds)
            {
                combinedBounds = new Bounds(point, Vector3.zero);
                hasBounds = true;
                return;
            }

            combinedBounds.Encapsulate(point);
        }

        private void EncapsulateWorldBox(Transform root, Bounds worldBox)
        {
            if (worldBox.size == Vector3.zero)
            {
                return;
            }

            EncapsulateCorners(root.worldToLocalMatrix, worldBox);
        }

        private void EncapsulateCollider(Transform root, Collider collider)
        {
            BoxCollider boxCollider = collider as BoxCollider;
            if (boxCollider != null)
            {
                EncapsulateLocalBox(root, boxCollider.transform, new Bounds(boxCollider.center, boxCollider.size));
                return;
            }

            SphereCollider sphereCollider = collider as SphereCollider;
            if (sphereCollider != null)
            {
                EncapsulateLocalBox(root, sphereCollider.transform, new Bounds(sphereCollider.center, Vector3.one * sphereCollider.radius * 2f));
                return;
            }

            CapsuleCollider capsuleCollider = collider as CapsuleCollider;
            if (capsuleCollider != null)
            {
                float diameter = capsuleCollider.radius * 2f;
                float length = Mathf.Max(capsuleCollider.height, diameter);
                Vector3 size = Vector3.one * diameter;
                if (capsuleCollider.direction == 0)
                {
                    size.x = length;
                }
                else if (capsuleCollider.direction == 1)
                {
                    size.y = length;
                }
                else
                {
                    size.z = length;
                }

                EncapsulateLocalBox(root, capsuleCollider.transform, new Bounds(capsuleCollider.center, size));
                return;
            }

            MeshCollider meshCollider = collider as MeshCollider;
            if (meshCollider != null && meshCollider.sharedMesh != null)
            {
                EncapsulateLocalBox(root, meshCollider.transform, meshCollider.sharedMesh.bounds);
                return;
            }

            EncapsulateWorldBox(root, collider.bounds);
        }

        private void CalculateCurveAnchors()
        {
            float margin = settings.CurveMargin;
            float left = combinedBounds.min.x - margin;
            float right = combinedBounds.max.x + margin;
            curveRear = combinedBounds.min.z - margin;
            curveFront = combinedBounds.max.z + margin;
            curveMiddle = combinedBounds.center.z;
            float width = right - left;
            float depth = curveFront - curveRear;
            cornerRadius = Mathf.Min(settings.MaximumCornerRadius, Mathf.Min(width, depth) * settings.CornerRadiusWidthFraction);

            anchors[0] = new Vector3(right, 0f, curveMiddle);
            anchors[1] = new Vector3(right, 0f, curveRear + cornerRadius);
            anchors[2] = new Vector3(right - cornerRadius, 0f, curveRear);
            anchors[3] = new Vector3(left + cornerRadius, 0f, curveRear);
            anchors[4] = new Vector3(left, 0f, curveRear + cornerRadius);
            anchors[5] = new Vector3(left, 0f, curveFront - cornerRadius);
            anchors[6] = new Vector3(left + cornerRadius, 0f, curveFront);
            anchors[7] = new Vector3(right - cornerRadius, 0f, curveFront);
            anchors[8] = new Vector3(right, 0f, curveFront - cornerRadius);

            ConfigureStraightSegment(0);
            ConfigureCornerSegment(1, new Vector3(right, 0f, curveRear));
            ConfigureStraightSegment(2);
            ConfigureCornerSegment(3, new Vector3(left, 0f, curveRear));
            ConfigureStraightSegment(4);
            ConfigureCornerSegment(5, new Vector3(left, 0f, curveFront));
            ConfigureStraightSegment(6);
            ConfigureCornerSegment(7, new Vector3(right, 0f, curveFront));
            ConfigureStraightSegment(8);
        }

        private void ConfigureStraightSegment(int startIndex)
        {
            int endIndex = (startIndex + 1) % KnotCount;
            Vector3 start = anchors[startIndex];
            Vector3 end = anchors[endIndex];
            outgoingHandles[startIndex] = start + (end - start) / 3f;
            incomingHandles[endIndex] = end + (start - end) / 3f;
        }

        private void ConfigureCornerSegment(int startIndex, Vector3 corner)
        {
            int endIndex = (startIndex + 1) % KnotCount;
            Vector3 start = anchors[startIndex];
            Vector3 end = anchors[endIndex];
            outgoingHandles[startIndex] = start + (corner - start) * ArcHandleScale;
            incomingHandles[endIndex] = end + (corner - end) * ArcHandleScale;
        }

        private VehicleOrbit ConfigureOrbit(VehicleProfile target, string orbitName, float baseHeight, float minimumHeight, float maximumHeight)
        {
            VehicleOrbit orbit;
            if (!target.TryGetOrbit(orbitName, out orbit))
            {
                orbit = target.AddOrbit(orbitName);
            }

            orbit.Configure(CreateKnots(), Quaternion.identity);
            orbit.ConfigureBaseHeight(baseHeight);
            orbit.ConfigureOffsetRanges(minimumHeight, maximumHeight, settings.MinimumZoomOffset, settings.MaximumZoomOffset);
            return orbit;
        }

        private List<BezierOrbitKnot> CreateKnots()
        {
            List<BezierOrbitKnot> knots = new List<BezierOrbitKnot>(KnotCount);

            for (int index = 0; index < KnotCount; index++)
            {
                BezierOrbitKnot knot = new BezierOrbitKnot();
                knot.Configure(anchors[index], incomingHandles[index], outgoingHandles[index]);
                knots.Add(knot);
            }

            return knots;
        }

        private void ConfigureRemovableSections(VehicleOrbit orbit, float length)
        {
            float inset = Mathf.Min(settings.RemovableSectionInset, combinedBounds.size.z * 0.25f);
            float rearBoundary = Mathf.Max(combinedBounds.min.z + inset, curveRear + cornerRadius);
            float frontBoundary = Mathf.Min(combinedBounds.max.z - inset, curveFront - cornerRadius);
            float rearOffset = curveMiddle - rearBoundary;
            float frontOffset = frontBoundary - curveMiddle;
            float halfLength = length * 0.5f;

            OrbitRemovableSection rearSection = new OrbitRemovableSection();
            rearSection.Configure(rearOffset / length, (halfLength - rearOffset) / length);
            OrbitRemovableSection frontSection = new OrbitRemovableSection();
            frontSection.Configure((halfLength + frontOffset) / length, (length - frontOffset) / length);
            orbit.ConfigureRemovableSections(frontSection, rearSection);
        }

        private void ConfigureDrivingMarkers(VehicleProfile target, VehicleOrbit orbit, ClosedBezierOrbit closedOrbit, float cameraHeight)
        {
            Vector3 centre = combinedBounds.center;
            Vector3 frontBumper = new Vector3(centre.x, centre.y, combinedBounds.max.z);
            float rearWatchDistance = combinedBounds.max.z + settings.RearMarkerDistanceAhead;
            Vector3 rearWatchPoint = new Vector3(centre.x, CalculateRearWatchHeight(rearWatchDistance, cameraHeight), rearWatchDistance);

            SetMarker(target, orbit, RightMarkerName, GetNormalizedPositionAtBearing(closedOrbit, RightBearing), frontBumper);
            SetMarker(target, orbit, RearMarkerName, GetNormalizedPositionAtBearing(closedOrbit, RearBearing), rearWatchPoint);
            SetMarker(target, orbit, LeftMarkerName, GetNormalizedPositionAtBearing(closedOrbit, LeftBearing), frontBumper);
            SetMarker(target, orbit, FrontMarkerName, GetNormalizedPositionAtBearing(closedOrbit, FrontBearing), centre);
        }

        private float CalculateRearWatchHeight(float watchDistance, float cameraHeight)
        {
            float fractionHeight = combinedBounds.min.y + combinedBounds.size.y * settings.RearMarkerHeightFraction;
            float roofEdgeDistance = combinedBounds.max.z - curveRear;
            if (roofEdgeDistance <= 0f)
            {
                return fractionHeight;
            }

            float roofEdgeHeight = combinedBounds.max.y + settings.RearMarkerRoofClearance;
            float clearingHeight = cameraHeight + (roofEdgeHeight - cameraHeight) * (watchDistance - curveRear) / roofEdgeDistance;
            return Mathf.Max(fractionHeight, clearingHeight);
        }

        private float GetNormalizedPositionAtBearing(ClosedBezierOrbit closedOrbit, float bearing)
        {
            float distance;
            if (closedOrbit.Length <= 0f || !closedOrbit.Bearing.TryGetDistanceAtBearing(bearing, 0f, out distance))
            {
                return 0f;
            }

            float normalizedPosition = distance / closedOrbit.Length;
            if (normalizedPosition >= 1f || normalizedPosition < 0f)
            {
                return 0f;
            }

            return normalizedPosition;
        }

        private void SetMarker(VehicleProfile target, VehicleOrbit orbit, string markerName, float normalizedPosition, Vector3 watchPoint)
        {
            for (int index = 0; index < orbit.WatchMarkers.Count; index++)
            {
                OrbitWatchMarker marker = orbit.WatchMarkers[index];
                if (marker != null && marker.Name == markerName)
                {
                    marker.Configure(normalizedPosition, watchPoint);
                    return;
                }
            }

            target.AddWatchMarker(orbit, markerName, normalizedPosition, watchPoint, false);
        }

        private void ConfigureConnectorAnchors(VehicleProfile target, float baseHeight)
        {
            float centreX = combinedBounds.center.x;
            VehicleConnectorAnchors connectorAnchors = new VehicleConnectorAnchors();
            connectorAnchors.Configure(new Vector3(centreX, baseHeight, combinedBounds.max.z), new Vector3(centreX, baseHeight, combinedBounds.min.z));
            target.ConfigureConnectorAnchors(connectorAnchors);
        }

        private void ConfigureSeatAndFixedPose(VehicleProfile target)
        {
            Vector3 centre = combinedBounds.center;
            Vector3 eye = new Vector3(centre.x, combinedBounds.min.y + combinedBounds.size.y * settings.InteriorEyeHeightFraction, combinedBounds.max.z - combinedBounds.size.z * settings.InteriorEyeFromFrontFraction);
            target.Seat.Configure(eye, target.Seat.EnvelopeHalfExtents);
            target.FixedViewPose.Configure(centre + new Vector3(0f, settings.FixedCameraAbove, -settings.FixedCameraBehind), centre);
        }

        private void ConfigureViews(VehicleProfile target, GeneratorPresets presets, VehicleOrbit drivingOrbit, VehicleOrbit presentationOrbit, float drivingHeight)
        {
            VehicleViewEntry drivingView = ConfigureView(target, DrivingName, presets.Driving, drivingOrbit.Id, new OrbitPose(RearBearing, 0f, drivingHeight));
            if (drivingView != null)
            {
                drivingView.ConfigureFrontDefaultPose(new OrbitPose(FrontBearing, 0f, drivingHeight));
            }

            ConfigureView(target, PresentationName, presets.Presentation, presentationOrbit.Id, new OrbitPose(RearBearing, 0f, 0f));
            ConfigureView(target, InteriorName, presets.Interior, drivingOrbit.Id, new OrbitPose(RearBearing, 0f, 0f));
            ConfigureView(target, FixedName, presets.Fixed, drivingOrbit.Id, new OrbitPose(RearBearing, 0f, 0f));
        }

        private VehicleViewEntry ConfigureView(VehicleProfile target, string viewName, CameraViewPreset preset, int orbitId, OrbitPose defaultPose)
        {
            if (preset == null)
            {
                return null;
            }

            VehicleViewEntry view;
            if (target.TryGetView(viewName, out view))
            {
                view.ConfigurePreset(preset);
                view.ConfigureOrbit(orbitId);
            }
            else
            {
                view = target.AddView(viewName, preset, orbitId);
            }

            view.ConfigureDefaultPose(defaultPose);
            return view;
        }
    }
}
