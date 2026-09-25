using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Gley.CameraSystem.Editor
{
    public class VehicleCameraValidationCollector
    {
        private const float SharpCornerTolerance = 1f;
        private const string BlockedViewsSuffix = " Views that use this orbit are blocked.";
        private const string BlockedChainSuffix = " Views that use this orbit are blocked while other bodies are attached.";

        private readonly List<Transform> targetBodies = new List<Transform>();
        private readonly VehicleProfileValidator profileValidator = new VehicleProfileValidator();
        private readonly OrbitBearingValidator bearingValidator = new OrbitBearingValidator();
        private readonly StringBuilder messageBuilder = new StringBuilder();
        private readonly IInputSystemInstallation inputSystemInstallation;

        public VehicleCameraValidationCollector(IInputSystemInstallation installation)
        {
            inputSystemInstallation = installation;
        }

        public void CollectIssues(VehicleProfile profile, VehicleCameraTabKind tab, List<EditorIssue> issues)
        {
            if (tab == VehicleCameraTabKind.Setup)
            {
                CollectInputSystemIssues(issues);
            }

            if (profile == null)
            {
                return;
            }

            CollectProfileIssues(profile, tab, issues);

            if (tab == VehicleCameraTabKind.Orbits)
            {
                CollectOrbitIssues(profile, issues);
            }
        }

        public void CollectTargetIssues(VehicleCameraTarget target, List<EditorIssue> issues)
        {
            if (target == null)
            {
                return;
            }

            targetBodies.Clear();

            for (int index = 0; index < target.BodyCount; index++)
            {
                Transform body = target.GetBody(index);
                if (body != null)
                {
                    targetBodies.Add(body);
                }
            }

            if (targetBodies.Count == 0)
            {
                SerializedObject serializedTarget = new SerializedObject(target);
                Transform rootBody = serializedTarget.FindProperty("rootBody").objectReferenceValue as Transform;
                if (rootBody != null)
                {
                    targetBodies.Add(rootBody);
                }
            }

            for (int index = 0; index < targetBodies.Count; index++)
            {
                Rigidbody bodyRigidbody = targetBodies[index].GetComponent<Rigidbody>();
                if (bodyRigidbody != null && bodyRigidbody.interpolation == RigidbodyInterpolation.None)
                {
                    string message = $"'{targetBodies[index].name}' has Rigidbody interpolation set to None, the usual cause of camera jitter. Set it to Interpolate.";
                    issues.Add(new EditorIssue(EditorIssueSeverity.Warning, message, bodyRigidbody, 0));
                }
            }

            targetBodies.Clear();
        }

        private void CollectInputSystemIssues(List<EditorIssue> issues)
        {
            if (inputSystemInstallation == null || inputSystemInstallation.IsInstalled)
            {
                return;
            }

            issues.Add(new EditorIssue(EditorIssueSeverity.Warning, "The input companion needs the Input System package.", null, 0));
        }

        private void CollectProfileIssues(VehicleProfile profile, VehicleCameraTabKind tab, List<EditorIssue> issues)
        {
            VehicleProfileValidationReport report = profileValidator.Validate(profile);

            for (int index = 0; index < report.Issues.Count; index++)
            {
                VehicleProfileIssue issue = report.Issues[index];
                if (GetIssueTab(profile, issue) != tab)
                {
                    continue;
                }

                issues.Add(new EditorIssue(EditorIssueSeverity.Error, GetProfileIssueMessage(profile, issue), profile, issue.ItemId));
            }
        }

        private VehicleCameraTabKind GetIssueTab(VehicleProfile profile, VehicleProfileIssue issue)
        {
            switch (issue.Code)
            {
                case VehicleProfileIssueCode.FormatVersionNewerThanSupported:
                case VehicleProfileIssueCode.MissingProfileId:
                    return VehicleCameraTabKind.Setup;
                case VehicleProfileIssueCode.DuplicateOrbitName:
                case VehicleProfileIssueCode.DuplicateMarkerName:
                    return VehicleCameraTabKind.Orbits;
                case VehicleProfileIssueCode.DuplicateViewName:
                case VehicleProfileIssueCode.ViewMissingPreset:
                case VehicleProfileIssueCode.ViewOrbitNotFound:
                case VehicleProfileIssueCode.NoViews:
                    return VehicleCameraTabKind.Views;
                default:
                    return GetItemTab(profile, issue.ItemId);
            }
        }

        private VehicleCameraTabKind GetItemTab(VehicleProfile profile, int itemId)
        {
            if (profile.TryGetView(itemId, out VehicleViewEntry _))
            {
                return VehicleCameraTabKind.Views;
            }

            if (profile.TryGetOrbit(itemId, out VehicleOrbit _) || TryGetMarkerOrbit(profile, itemId, out VehicleOrbit _))
            {
                return VehicleCameraTabKind.Orbits;
            }

            return VehicleCameraTabKind.Setup;
        }

        private bool TryGetMarkerOrbit(VehicleProfile profile, int markerId, out VehicleOrbit markerOrbit)
        {
            for (int orbitIndex = 0; orbitIndex < profile.Orbits.Count; orbitIndex++)
            {
                VehicleOrbit orbit = profile.Orbits[orbitIndex];
                for (int markerIndex = 0; markerIndex < orbit.WatchMarkers.Count; markerIndex++)
                {
                    if (orbit.WatchMarkers[markerIndex] != null && orbit.WatchMarkers[markerIndex].Id == markerId)
                    {
                        markerOrbit = orbit;
                        return true;
                    }
                }
            }

            markerOrbit = null;
            return false;
        }

        private string GetProfileIssueMessage(VehicleProfile profile, VehicleProfileIssue issue)
        {
            switch (issue.Code)
            {
                case VehicleProfileIssueCode.FormatVersionNewerThanSupported:
                    return $"This profile uses data format version {profile.FormatVersion}, newer than this package supports ({VehicleProfile.CurrentFormatVersion}). Update Vehicle Camera System.";
                case VehicleProfileIssueCode.MissingProfileId:
                    return "This profile has no profile ID, so saved player preferences cannot refer to it.";
                case VehicleProfileIssueCode.DuplicateOrbitName:
                    return $"Two orbits are named '{issue.ItemName}'. Orbit names must be unique within a profile.";
                case VehicleProfileIssueCode.DuplicateMarkerName:
                    return $"Two watch markers in orbit '{GetMarkerOrbitName(profile, issue.ItemId)}' are named '{issue.ItemName}'. Marker names must be unique within an orbit.";
                case VehicleProfileIssueCode.DuplicateViewName:
                    return $"Two views are named '{issue.ItemName}'. View names must be unique within a profile.";
                case VehicleProfileIssueCode.EmptyName:
                    return $"A {GetItemKindName(profile, issue.ItemId)} has no name.";
                case VehicleProfileIssueCode.DuplicateStableId:
                    return $"The {GetItemKindName(profile, issue.ItemId)} '{issue.ItemName}' shares its stable ID {issue.ItemId} with another item. Remove it and add it again.";
                case VehicleProfileIssueCode.ViewMissingPreset:
                    return $"View '{issue.ItemName}' has no view preset.";
                case VehicleProfileIssueCode.ViewOrbitNotFound:
                    return $"View '{issue.ItemName}' uses an orbit that is not in this profile.";
                case VehicleProfileIssueCode.NoViews:
                    return "The profile has no views.";
                default:
                    return issue.Code.ToString();
            }
        }

        private string GetMarkerOrbitName(VehicleProfile profile, int markerId)
        {
            if (TryGetMarkerOrbit(profile, markerId, out VehicleOrbit markerOrbit))
            {
                return markerOrbit.Name;
            }

            return string.Empty;
        }

        private string GetItemKindName(VehicleProfile profile, int itemId)
        {
            if (profile.TryGetView(itemId, out VehicleViewEntry _))
            {
                return "view";
            }

            if (profile.TryGetOrbit(itemId, out VehicleOrbit _))
            {
                return "orbit";
            }

            if (TryGetMarkerOrbit(profile, itemId, out VehicleOrbit _))
            {
                return "watch marker";
            }

            return "item";
        }

        private void CollectOrbitIssues(VehicleProfile profile, List<EditorIssue> issues)
        {
            for (int orbitIndex = 0; orbitIndex < profile.Orbits.Count; orbitIndex++)
            {
                VehicleOrbit orbit = profile.Orbits[orbitIndex];
                if (orbit == null)
                {
                    continue;
                }

                ClosedBezierOrbit closedOrbit = new ClosedBezierOrbit(orbit);
                CollectCurveIssue(profile, orbit, closedOrbit, issues);
                CollectMarkerIssue(profile, orbit, closedOrbit, issues);
                CollectOffsetRangeIssue(profile, orbit, closedOrbit, issues);
                CollectRemovableSectionIssue(profile, orbit, closedOrbit, issues);

                if (closedOrbit.ValidationResult == OrbitValidationResult.Valid)
                {
                    CollectBearingIssue(profile, orbit, closedOrbit, issues);
                    CollectSharpCornerIssue(profile, orbit, closedOrbit, issues);
                }
            }
        }

        private void CollectCurveIssue(VehicleProfile profile, VehicleOrbit orbit, ClosedBezierOrbit closedOrbit, List<EditorIssue> issues)
        {
            string problem;

            switch (closedOrbit.ValidationResult)
            {
                case OrbitValidationResult.TooFewKnots:
                    problem = "the curve needs at least 3 knots.";
                    break;
                case OrbitValidationResult.NonPlanar:
                    problem = "every knot and handle must lie in the orbit's plane (local height 0).";
                    break;
                case OrbitValidationResult.SelfIntersecting:
                    problem = "the curve crosses itself.";
                    break;
                case OrbitValidationResult.Degenerate:
                    problem = "the curve has no length.";
                    break;
                default:
                    return;
            }

            AddOrbitIssue(profile, orbit, EditorIssueSeverity.Error, problem + BlockedViewsSuffix, issues);
        }

        private void CollectMarkerIssue(VehicleProfile profile, VehicleOrbit orbit, ClosedBezierOrbit closedOrbit, List<EditorIssue> issues)
        {
            string problem;

            switch (closedOrbit.WatchMarkerValidationResult)
            {
                case OrbitWatchMarkerValidationResult.MissingMarkers:
                    problem = "it has no watch markers.";
                    break;
                case OrbitWatchMarkerValidationResult.InvalidMarkerPosition:
                    problem = "a watch marker's orbit position is outside 0 to 1.";
                    break;
                case OrbitWatchMarkerValidationResult.DuplicateMarkerPosition:
                    problem = "two watch markers are at the same orbit position (within 0.001, including across the 0/1 wrap).";
                    break;
                default:
                    return;
            }

            AddOrbitIssue(profile, orbit, EditorIssueSeverity.Error, problem + BlockedViewsSuffix, issues);
        }

        private void CollectOffsetRangeIssue(VehicleProfile profile, VehicleOrbit orbit, ClosedBezierOrbit closedOrbit, List<EditorIssue> issues)
        {
            switch (closedOrbit.OffsetRangeValidationResult)
            {
                case OrbitOffsetRangeValidationResult.InvalidHeightRange:
                    AddOrbitIssue(profile, orbit, EditorIssueSeverity.Error, "the height range minimum is above its maximum.", issues);
                    break;
                case OrbitOffsetRangeValidationResult.InvalidZoomRange:
                    AddOrbitIssue(profile, orbit, EditorIssueSeverity.Error, "the zoom range minimum is above its maximum.", issues);
                    break;
                case OrbitOffsetRangeValidationResult.InwardZoomExceedsCurvature:
                    string problem = $"the inward zoom (+{orbit.MaximumZoomOffset:0.##} m) exceeds the curve's tightest convex radius ({closedOrbit.MaximumSafeInwardZoom:0.##} m), so the zoomed camera path can loop or jump there. Reduce the inward zoom or widen that part of the curve.";
                    AddOrbitIssue(profile, orbit, EditorIssueSeverity.Warning, problem, issues);
                    break;
            }
        }

        private void CollectRemovableSectionIssue(VehicleProfile profile, VehicleOrbit orbit, ClosedBezierOrbit closedOrbit, List<EditorIssue> issues)
        {
            string problem;

            switch (closedOrbit.RemovableSectionValidationResult)
            {
                case OrbitRemovableSectionValidationResult.MissingFrontSection:
                    problem = "the front removable section is missing.";
                    break;
                case OrbitRemovableSectionValidationResult.MissingRearSection:
                    problem = "the rear removable section is missing.";
                    break;
                case OrbitRemovableSectionValidationResult.InvalidFrontSection:
                    problem = "the front removable section is invalid (its positions must be in 0 to 1 and it must have length).";
                    break;
                case OrbitRemovableSectionValidationResult.InvalidRearSection:
                    problem = "the rear removable section is invalid (its positions must be in 0 to 1 and it must have length).";
                    break;
                case OrbitRemovableSectionValidationResult.OverlappingSections:
                    problem = "the front and rear removable sections overlap.";
                    break;
                default:
                    return;
            }

            AddOrbitIssue(profile, orbit, EditorIssueSeverity.Error, problem + BlockedChainSuffix, issues);
        }

        private void CollectBearingIssue(VehicleProfile profile, VehicleOrbit orbit, ClosedBezierOrbit closedOrbit, List<EditorIssue> issues)
        {
            IReadOnlyList<float> ambiguousBearings = bearingValidator.FindAmbiguousBearings(closedOrbit.Bearing);
            if (ambiguousBearings.Count == 0)
            {
                return;
            }

            string problem = $"a bearing ray from the orbit centre crosses the curve more than once at bearings {FormatBearingRanges(ambiguousBearings)}. Angle limits and view mapping there use the crossing nearest the camera.";
            AddOrbitIssue(profile, orbit, EditorIssueSeverity.Warning, problem, issues);
        }

        private string FormatBearingRanges(IReadOnlyList<float> bearings)
        {
            messageBuilder.Clear();

            if (bearings.Count >= 360)
            {
                return "all bearings";
            }

            int firstRangeEnd = FindRangeEnd(bearings, 0);
            int lastRangeStart = FindLastRangeStart(bearings);
            bool wrapsAcrossFront = firstRangeEnd < lastRangeStart && bearings[0] <= -179f && bearings[bearings.Count - 1] >= 180f;
            int rangeStart = 0;
            int stopIndex = bearings.Count;

            if (wrapsAcrossFront)
            {
                AppendRange(bearings[lastRangeStart], bearings[firstRangeEnd]);
                rangeStart = firstRangeEnd + 1;
                stopIndex = lastRangeStart;
            }

            while (rangeStart < stopIndex)
            {
                int rangeEnd = FindRangeEnd(bearings, rangeStart);
                AppendRange(bearings[rangeStart], bearings[rangeEnd]);
                rangeStart = rangeEnd + 1;
            }

            return messageBuilder.ToString();
        }

        private int FindRangeEnd(IReadOnlyList<float> bearings, int rangeStart)
        {
            int rangeEnd = rangeStart;

            while (rangeEnd + 1 < bearings.Count && bearings[rangeEnd + 1] - bearings[rangeEnd] <= 1.5f)
            {
                rangeEnd++;
            }

            return rangeEnd;
        }

        private int FindLastRangeStart(IReadOnlyList<float> bearings)
        {
            int rangeStart = bearings.Count - 1;

            while (rangeStart > 0 && bearings[rangeStart] - bearings[rangeStart - 1] <= 1.5f)
            {
                rangeStart--;
            }

            return rangeStart;
        }

        private void AppendRange(float startBearing, float endBearing)
        {
            if (messageBuilder.Length > 0)
            {
                messageBuilder.Append(", ");
            }

            if (startBearing == endBearing)
            {
                messageBuilder.Append($"{startBearing:0}°");
                return;
            }

            messageBuilder.Append($"{startBearing:0}° to {endBearing:0}°");
        }

        private void CollectSharpCornerIssue(VehicleProfile profile, VehicleOrbit orbit, ClosedBezierOrbit closedOrbit, List<EditorIssue> issues)
        {
            if (closedOrbit.MaximumCornerTurn <= SharpCornerTolerance)
            {
                return;
            }

            if (orbit.MinimumZoomOffset == 0f && orbit.MaximumZoomOffset == 0f)
            {
                return;
            }

            string problem = $"knot {closedOrbit.SharpestCornerKnotIndex} is a sharp corner (the curve turns {closedOrbit.MaximumCornerTurn:0.#}° there). With a zoom range, the camera jumps by about the zoom distance as it passes the corner. Round the corner, or set the zoom range to 0 to 0.";
            AddOrbitIssue(profile, orbit, EditorIssueSeverity.Warning, problem, issues);
        }

        private void AddOrbitIssue(VehicleProfile profile, VehicleOrbit orbit, EditorIssueSeverity severity, string problem, List<EditorIssue> issues)
        {
            issues.Add(new EditorIssue(severity, $"Orbit '{orbit.Name}': {problem}", profile, orbit.Id));
        }
    }
}
