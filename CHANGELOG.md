## [Unreleased]
### Changed
- New vehicle profiles, pair overrides and duplicated presets are saved to `Assets/GleyData/VehicleCameraSystem/Profiles` and `Assets/GleyData/VehicleCameraSystem/Presets` by default, instead of `Assets/VehicleCameraSystemData`.
- The last folder used for saving is remembered per project, in `UserSettings/Gley/VehicleCameraSystem.asset`, instead of in EditorPrefs shared by all projects.
- The window and scene handles use the shared Gley editor colors: selected items, anchors and watch markers are orange, invalid curves and delete handles red, insert handles green. Front poses, the front removable section and pair overrides are violet instead of orange.
### Fixed
- The package now finds its own assets (default presets, input actions, touch buttons prefab) wherever the Gley folder is placed in the project, instead of expecting `Assets/Gley/VehicleCameraSystem`.
