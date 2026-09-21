# Requirement coverage

This retains every requirement ID from [ImplementationPlan.md](ImplementationPlan.md) and links every original owner step. It supplements, rather than replaces, the authoritative requirement table.

All full-outcome validation starts **Pending** after the audit. This does not mean no code exists; see the [step tracker](ImplementationStatus.md) for implementation progress. Update evidence only after the relevant checks run. Shared requirements remain pending until all owner contributions and integration checks have passed; user acceptance remains separate.

| ID | Required outcome | Owner step files | Full-outcome evidence |
| --- | --- | --- | --- |
| CORE-01 | Runtime compiles without the input companion or game-specific packages. | [00](Steps/Step00-package-skeleton.md), [25](Steps/Step25-pc-input-companion.md), [26](Steps/Step26-touch-gestures.md), [27](Steps/Step27-touch-buttons.md) | Pending |
| CORE-02 | Two instances retain separate cameras, views, commands, and state. | [01](Steps/Step01-fixed-view.md) | Pending |
| CORE-03 | Deactivation restores only Camera settings overridden by the instance; field of view is unchanged. | [20](Steps/Step20-rendering-and-time.md) | Pending |
| DATA-01 | Runtime changes do not alter shared profiles or vehicle prefabs. | [01](Steps/Step01-fixed-view.md), [02](Steps/Step02-single-body-orbit.md), [03](Steps/Step03-watch-marker-aim.md), [04](Steps/Step04-height-and-zoom.md), [05](Steps/Step05-two-body-geometry.md), [06](Steps/Step06-attach-and-remap.md), [07](Steps/Step07-longer-chains.md), [08](Steps/Step08-body-bound-aim.md), [09](Steps/Step09-point-of-interest-travel.md), [10](Steps/Step10-manual-arbitration.md), [11](Steps/Step11-view-switching.md), [12](Steps/Step12-reset-and-recenter.md), [13](Steps/Step13-driving-follow.md), [14](Steps/Step14-reverse-and-driving-turn-look.md), [15](Steps/Step15-interior-head-and-turn-look.md), [16](Steps/Step16-acceleration-and-braking.md), [17](Steps/Step17-cornering-module.md), [18](Steps/Step18-cushion-module.md), [19](Steps/Step19-collision.md), [20](Steps/Step20-rendering-and-time.md), [21](Steps/Step21-target-and-teleport.md), [22](Steps/Step22-guided-profile-creation.md), [23](Steps/Step23-scene-editing-handles.md), [24](Steps/Step24-connected-preview-and-validation.md) | Pending |
| DATA-02 | Reusable view presets can serve different vehicles while each vehicle supplies its own active-view defaults. | [01](Steps/Step01-fixed-view.md), [11](Steps/Step11-view-switching.md) | Pending |
| ORBIT-01 | Single-body curves are closed, non-self-intersecting, and sampled uniformly by physical distance. | [02](Steps/Step02-single-body-orbit.md) | Pending |
| ORBIT-02 | Removable front/rear sections and generated connectors form valid combined curves. | [05](Steps/Step05-two-body-geometry.md) | Pending |
| ORBIT-03 | Interchangeable truck/trailer profiles need no authored combined curves; explicit attachment can resolve or accept a pair's connector override. | [07](Steps/Step07-longer-chains.md) | Pending |
| ORBIT-04 | Articulation does not deform the straight-reference orbit; body-bound aim follows the actual body. | [08](Steps/Step08-body-bound-aim.md) | Pending |
| ORBIT-05 | Attachment preserves a surviving camera section and remaps a removed section smoothly. | [06](Steps/Step06-attach-and-remap.md) | Pending |
| ORBIT-06 | A root-only orbit remains unchanged when a trailer attaches. | [06](Steps/Step06-attach-and-remap.md) | Pending |
| ORBIT-07 | Local +Z/+Y defaults, authorable orientation offsets, and lead-body pitch/roll produce the expected orbit frame. | [02](Steps/Step02-single-body-orbit.md), [04](Steps/Step04-height-and-zoom.md) | Pending |
| AIM-01 | Aim matches authored markers exactly and interpolates continuously between them. | [03](Steps/Step03-watch-marker-aim.md) | Pending |
| AIM-02 | Named points of interest work in any order, stop exactly, and report completion or interruption. | [09](Steps/Step09-point-of-interest-travel.md) | Pending |
| AIM-03 | Aim binds to an exact body instance and responds predictably to disconnection. | [08](Steps/Step08-body-bound-aim.md) | Pending |
| AIM-04 | The next authored point of interest can be requested without a presentation sequence. | [09](Steps/Step09-point-of-interest-travel.md) | Pending |
| AIM-05 | Point-of-interest requests report completion, replacement, interruption, disconnection, or unreachable destination. | [08](Steps/Step08-body-bound-aim.md), [09](Steps/Step09-point-of-interest-travel.md), [10](Steps/Step10-manual-arbitration.md) | Pending |
| AIM-06 | Exterior orbit aim follows authored watch markers without independent player free-look tilt. | [03](Steps/Step03-watch-marker-aim.md), [04](Steps/Step04-height-and-zoom.md) | Pending |
| CMD-01 | Manual input interrupts automatic travel unless locked; a lock blocks all player camera commands. | [10](Steps/Step10-manual-arbitration.md) | Pending |
| CMD-02 | Manual input can interrupt point-of-interest, smooth view, and reverse travel under the same command policy. | [10](Steps/Step10-manual-arbitration.md), [11](Steps/Step11-view-switching.md), [14](Steps/Step14-reverse-and-driving-turn-look.md) | Pending |
| VIEW-01 | Fixed, Interior, Driving, and Presentation views follow their own presets; none is mandatory. | [01](Steps/Step01-fixed-view.md), [11](Steps/Step11-view-switching.md), [13](Steps/Step13-driving-follow.md), [14](Steps/Step14-reverse-and-driving-turn-look.md), [15](Steps/Step15-interior-head-and-turn-look.md) | Pending |
| VIEW-02 | Reverse affects only Driving and is triggered by the host. | [14](Steps/Step14-reverse-and-driving-turn-look.md) | Pending |
| VIEW-03 | A view switch chooses the nearest allowed angle and obeys smooth or one-frame snap mode. | [11](Steps/Step11-view-switching.md) | Pending |
| VIEW-04 | Smooth automatic travel uses preset speed by default and accepts speed or fixed-duration overrides. | [09](Steps/Step09-point-of-interest-travel.md), [11](Steps/Step11-view-switching.md) | Pending |
| VIEW-05 | Driving reverse travels smoothly to its player front default pose; front and rear may use different zoom distances, and speed distance is disabled by default but optional in reverse. | [12](Steps/Step12-reset-and-recenter.md), [14](Steps/Step14-reverse-and-driving-turn-look.md) | Pending |
| VIEW-06 | One camera instance can activate views on different named orbits, while views may also share an orbit. | [11](Steps/Step11-view-switching.md) | Pending |
| VIEW-07 | Both exterior views permit full 360-degree travel by default and obey authored angle limits when configured. | [11](Steps/Step11-view-switching.md), [13](Steps/Step13-driving-follow.md) | Pending |
| VIEW-08 | Driving favors road ahead at the rear while side framing retains a forward component and permits wheel or fifth-wheel detail; Presentation uses its own aim track. | [03](Steps/Step03-watch-marker-aim.md), [13](Steps/Step13-driving-follow.md) | Pending |
| MOVE-01 | Orbit travel uses physical speed, eases into manual movement, and stops when input ends. | [02](Steps/Step02-single-body-orbit.md) | Pending |
| MOVE-02 | Zoom and height move in their agreed directions and stay within one range per orbit. | [04](Steps/Step04-height-and-zoom.md) | Pending |
| MOVE-03 | Actual speed changes camera distance only; field of view remains fixed. | [13](Steps/Step13-driving-follow.md) | Pending |
| MOVE-04 | Follow lag, aim smoothing, maximum lag, and turn look are independent; turn look applies only to Interior and Driving by default. | [13](Steps/Step13-driving-follow.md), [14](Steps/Step14-reverse-and-driving-turn-look.md), [15](Steps/Step15-interior-head-and-turn-look.md) | Pending |
| MOVE-05 | Low speed frames closer; high speed frames farther, with a smaller but nonzero side effect. | [13](Steps/Step13-driving-follow.md) | Pending |
| MOVE-06 | The orbit follows lead-body pitch and roll while image roll remains a per-view choice. | [04](Steps/Step04-height-and-zoom.md), [11](Steps/Step11-view-switching.md) | Pending |
| SEAT-01 | Three Interior modules toggle independently, move position only, and respect the seat envelope. | [16](Steps/Step16-acceleration-and-braking.md), [17](Steps/Step17-cornering-module.md), [18](Steps/Step18-cushion-module.md) | Pending |
| SEAT-02 | Supplied motion signals take priority over Rigidbody measurements and Transform estimates; Rigidbody is optional. | [16](Steps/Step16-acceleration-and-braking.md), [17](Steps/Step17-cornering-module.md), [18](Steps/Step18-cushion-module.md) | Pending |
| SEAT-03 | Players can disable or intensify the cushion without changing the other two seat modules. | [18](Steps/Step18-cushion-module.md) | Pending |
| COL-01 | Collision correction respects legal zoom/height limits and reports when no clear pose exists. | [19](Steps/Step19-collision.md) | Pending |
| COL-02 | World obstructions prefer an inward correction; own-body obstructions may move outward or upward within limits. | [19](Steps/Step19-collision.md) | Pending |
| COL-03 | Collision correction does not alter watch targets or pathfind through geometry during view switches. | [11](Steps/Step11-view-switching.md), [19](Steps/Step19-collision.md) | Pending |
| RESET-01 | Orbit-only and full-view resets differ; front/rear player defaults retain separate position, zoom, and height; persistent and timed recenter modes work, and timed recenter waits for motion. | [12](Steps/Step12-reset-and-recenter.md) | Pending |
| STATE-01 | Vehicle change uses new vehicle defaults unless garage angle preservation is requested; teleport snaps while retaining orbit/zoom/height. | [21](Steps/Step21-target-and-teleport.md) | Pending |
| STATE-02 | Orbit rebuild maps saved front/rear defaults and carries player zoom/height adjustments relative to the new authored defaults, with clamping. | [06](Steps/Step06-attach-and-remap.md), [21](Steps/Step21-target-and-teleport.md) | Pending |
| TIME-01 | Unscaled time is default, with a per-view scaled-time option. | [20](Steps/Step20-rendering-and-time.md) | Pending |
| RENDER-01 | Masks and clip distances derive from the Camera baseline and do not accumulate. | [20](Steps/Step20-rendering-and-time.md) | Pending |
| RENDER-02 | Rendering presets require no project-specific layers. | [20](Steps/Step20-rendering-and-time.md) | Pending |
| EDIT-01 | Guided setup creates an editable profile, orbit, and watch target without prefab mutation. | [22](Steps/Step22-guided-profile-creation.md), [23](Steps/Step23-scene-editing-handles.md) | Pending |
| EDIT-02 | Editor previews connected bodies, articulation, framing, and zoom/height envelope. | [24](Steps/Step24-connected-preview-and-validation.md) | Pending |
| EDIT-03 | Invalid topology blocks only the affected view with a usable diagnostic. | [24](Steps/Step24-connected-preview-and-validation.md) | Pending |
| EDIT-04 | The generated connected Driving default is centered near the trailer rear and above its roof without changing the orbit. | [22](Steps/Step22-guided-profile-creation.md), [24](Steps/Step24-connected-preview-and-validation.md) | Pending |
| INPUT-01 | Optional companion maps PC and touch input, gestures, and simple uGUI buttons to public commands. | [25](Steps/Step25-pc-input-companion.md), [26](Steps/Step26-touch-gestures.md), [27](Steps/Step27-touch-buttons.md) | Pending |
| INPUT-02 | Each view can configure movement rate and drag sensitivity; player overrides stay instance-local. | [02](Steps/Step02-single-body-orbit.md), [25](Steps/Step25-pc-input-companion.md), [26](Steps/Step26-touch-gestures.md) | Pending |
| INPUT-03 | A held control must return to neutral after a view switch or lock before it acts again. | [10](Steps/Step10-manual-arbitration.md), [11](Steps/Step11-view-switching.md), [25](Steps/Step25-pc-input-companion.md), [26](Steps/Step26-touch-gestures.md) | Pending |
| INPUT-04 | Equivalent device-independent commands produce the same camera behavior whether game code, PC, or touch issues them. | [25](Steps/Step25-pc-input-companion.md), [26](Steps/Step26-touch-gestures.md) | Pending |

## Cross-step checks that must not disappear

- AIM-05: binding/disconnection in 08; all POI terminal results in 09; manual interruption/lock in 10.
- CMD-02 and INPUT-03: common arbitration in 10; view-switch integration in 11; reverse integration in 14; device held-input/gesture integration in 25–26. Reset/button player routes must also honor the policy in 12/27.
- STATE-02: authored-default-relative transfer is required in 06, not deferred; 12 adds player default commands; 21 verifies the resulting complete preference state through rebuilds.
- VIEW-01/07 and MOVE-04/06: basic exterior view/mapping and image-roll policy in 11; Driving follow and aim tracks in 13; reverse/Driving turn assistance in 14; Interior in 15.
- VIEW-08: marker interpolation in 03 is only a foundation; rear/side/front road/detail framing for distinct view tracks is validated in 13.
- EDIT-04: generation in 22 and connected preview/validation in 24 both prove the default changes pose data without modifying the curve.
- Design section 10 additionally requires visual editing of default poses, seat bounds and advanced connector geometry. Step 23 explicitly owns these handles even though the original short step row named only curves/sections/markers/envelopes.
- Input System/uGUI code stays in the optional companion throughout 25–27; core isolation must remain true with that companion absent.
