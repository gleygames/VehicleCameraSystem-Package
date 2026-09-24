using System.Collections.Generic;

namespace Gley.CameraSystem
{
    public class VehicleProfileValidator
    {
        public VehicleProfileValidationReport Validate(VehicleProfile profile)
        {
            VehicleProfileValidationReport report = new VehicleProfileValidationReport();

            if (profile == null)
            {
                return report;
            }

            if (profile.FormatVersion > VehicleProfile.CurrentFormatVersion)
            {
                report.AddIssue(VehicleProfileIssueCode.FormatVersionNewerThanSupported, 0, profile.name);
            }

            if (string.IsNullOrEmpty(profile.ProfileId))
            {
                report.AddIssue(VehicleProfileIssueCode.MissingProfileId, 0, profile.name);
            }

            ValidateOrbits(profile, report);
            ValidateViews(profile, report);
            ValidateStableIds(profile, report);

            if (profile.Views.Count == 0)
            {
                report.AddIssue(VehicleProfileIssueCode.NoViews, 0, profile.name);
            }

            return report;
        }

        private void ValidateOrbits(VehicleProfile profile, VehicleProfileValidationReport report)
        {
            IReadOnlyList<VehicleOrbit> orbits = profile.Orbits;

            for (int orbitIndex = 0; orbitIndex < orbits.Count; orbitIndex++)
            {
                VehicleOrbit orbit = orbits[orbitIndex];

                if (IsEmptyName(orbit.Name))
                {
                    report.AddIssue(VehicleProfileIssueCode.EmptyName, orbit.Id, orbit.Name);
                }
                else
                {
                    for (int previousIndex = 0; previousIndex < orbitIndex; previousIndex++)
                    {
                        if (AreSameName(orbit.Name, orbits[previousIndex].Name))
                        {
                            report.AddIssue(VehicleProfileIssueCode.DuplicateOrbitName, orbit.Id, orbit.Name);
                            break;
                        }
                    }
                }

                ValidateMarkers(orbit, report);
            }
        }

        private bool IsEmptyName(string itemName)
        {
            return string.IsNullOrWhiteSpace(itemName);
        }

        private bool AreSameName(string firstName, string secondName)
        {
            if (IsEmptyName(firstName) || IsEmptyName(secondName))
            {
                return false;
            }

            return firstName.Trim() == secondName.Trim();
        }

        private void ValidateMarkers(VehicleOrbit orbit, VehicleProfileValidationReport report)
        {
            IReadOnlyList<OrbitWatchMarker> markers = orbit.WatchMarkers;

            for (int markerIndex = 0; markerIndex < markers.Count; markerIndex++)
            {
                OrbitWatchMarker marker = markers[markerIndex];

                if (IsEmptyName(marker.Name))
                {
                    report.AddIssue(VehicleProfileIssueCode.EmptyName, marker.Id, marker.Name);
                    continue;
                }

                for (int previousIndex = 0; previousIndex < markerIndex; previousIndex++)
                {
                    if (AreSameName(marker.Name, markers[previousIndex].Name))
                    {
                        report.AddIssue(VehicleProfileIssueCode.DuplicateMarkerName, marker.Id, marker.Name);
                        break;
                    }
                }
            }
        }

        private void ValidateViews(VehicleProfile profile, VehicleProfileValidationReport report)
        {
            IReadOnlyList<VehicleViewEntry> views = profile.Views;

            for (int viewIndex = 0; viewIndex < views.Count; viewIndex++)
            {
                VehicleViewEntry view = views[viewIndex];

                if (IsEmptyName(view.Name))
                {
                    report.AddIssue(VehicleProfileIssueCode.EmptyName, view.Id, view.Name);
                }
                else
                {
                    for (int previousIndex = 0; previousIndex < viewIndex; previousIndex++)
                    {
                        if (AreSameName(view.Name, views[previousIndex].Name))
                        {
                            report.AddIssue(VehicleProfileIssueCode.DuplicateViewName, view.Id, view.Name);
                            break;
                        }
                    }
                }

                if (view.Preset == null)
                {
                    report.AddIssue(VehicleProfileIssueCode.ViewMissingPreset, view.Id, view.Name);
                    continue;
                }

                if (IsOrbitView(view.Preset.ViewType) && !profile.TryGetOrbit(view.OrbitId, out VehicleOrbit _))
                {
                    report.AddIssue(VehicleProfileIssueCode.ViewOrbitNotFound, view.Id, view.Name);
                }
            }
        }

        private bool IsOrbitView(CameraViewType viewType)
        {
            return viewType == CameraViewType.ExteriorDriving || viewType == CameraViewType.ExteriorPresentation;
        }

        private void ValidateStableIds(VehicleProfile profile, VehicleProfileValidationReport report)
        {
            List<int> itemIds = new List<int>();
            List<string> itemNames = new List<string>();
            IReadOnlyList<VehicleOrbit> orbits = profile.Orbits;
            IReadOnlyList<VehicleViewEntry> views = profile.Views;

            for (int orbitIndex = 0; orbitIndex < orbits.Count; orbitIndex++)
            {
                itemIds.Add(orbits[orbitIndex].Id);
                itemNames.Add(orbits[orbitIndex].Name);
                IReadOnlyList<OrbitWatchMarker> markers = orbits[orbitIndex].WatchMarkers;

                for (int markerIndex = 0; markerIndex < markers.Count; markerIndex++)
                {
                    itemIds.Add(markers[markerIndex].Id);
                    itemNames.Add(markers[markerIndex].Name);
                }
            }

            for (int viewIndex = 0; viewIndex < views.Count; viewIndex++)
            {
                itemIds.Add(views[viewIndex].Id);
                itemNames.Add(views[viewIndex].Name);
            }

            for (int itemIndex = 0; itemIndex < itemIds.Count; itemIndex++)
            {
                for (int previousIndex = 0; previousIndex < itemIndex; previousIndex++)
                {
                    if (itemIds[itemIndex] == itemIds[previousIndex])
                    {
                        report.AddIssue(VehicleProfileIssueCode.DuplicateStableId, itemIds[itemIndex], itemNames[itemIndex]);
                        break;
                    }
                }
            }
        }
    }
}
