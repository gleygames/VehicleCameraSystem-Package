# Vehicle Camera System — implementation instructions

**Status:** Execution plan refactored on 2026-09-21 for one fresh task per step in the existing user-provided Unity project. The original behavior requirements and numbered slices are retained below.

**Start here:** [ImplementationStatus.md](ImplementationStatus.md) identifies the next task, current evidence and Easy/Hard classifications. [ImplementationWorkflow.md](ImplementationWorkflow.md) defines startup, validation and review rules. Each step has a linked execution file in section 5. [RequirementCoverage.md](RequirementCoverage.md) tracks shared outcomes; the [audit](ImplementationAudit-2026-09-21.md) records existing gaps.

**Mandatory user checkpoint:** Every Hard task must announce its difficulty and recommended Astra High agent, end its turn, and wait for the user's explicit continuation before implementation, tests or an in-depth code audit. This applies even if the model already appears correct. Approval of this plan does not release any future task's checkpoint. Easy tasks recommend Terra High. See the workflow for the complete rule.

## 1. Authority and intended result

Read `VehicleCameraSystemDesign.md` before planning or writing code. That document defines product behavior. This document defines delivery order, boundaries, checks, and how to handle unresolved choices. A later explicit user decision overrides either document. Record such decisions in the new repository and update the relevant requirement and check before coding the changed behavior.

Implement the independent camera runtime, profile and view authoring, guided editor tools, and optional Input System companion with PC controls, touch gestures, and simple uGUI buttons. The companion belongs to the same product but remains an optional assembly. Unity 2022.3 is the minimum version. **User documentation and product sample scenes are outside this implementation and will be done separately later.** The design specification and this handoff plan are inputs to the work, not package user documentation. Small internal test fixtures are permitted and should not become required product assets.

Do not copy the current game's camera scripts or make the new package depend on its assemblies, prefabs, input classes, Addressables, or Gley Truck Controller. The current game may later be used as a consumer to test integration, after the standalone package works on its own.

Do not add independent free-look tilt to exterior orbit views, camera pathfinding between views, watch-point or road visibility correction, automatic reverse detection from gear/speed, or a built-in player save system. These behaviors are outside the agreed camera contract.

The new agent implements **one small step per user review cycle**. Every implementation step must include meaningful Unity Edit Mode and Play Mode tests, run both sets, and give the user a simple way to observe the behavior in the Editor. After reporting the result, the agent stops and waits for the user to verify and request the next step. Do not bundle later steps into the current one or silently drop required behavior.

## 2. Starting conditions supplied by the user

The user has supplied and configured the Unity project and repository. The design and plan are already in Assets/Gley/VehicleCameraSystem/Docs. The package is a separate Git repository inside the outer Unity project. The implementation agent must not create or configure another Unity project, initialize another repository, or modify Chain of Industry. If a prerequisite such as the Unity Test Framework is missing, report the exact missing item before changing project-wide configuration.

First read the selected step's difficulty and apply the mandatory Hard-task checkpoint. After its release, or immediately for an Easy task, verify the project path, Unity version, working tree, available documents, and test setup. Read any new repository `AGENTS.md`; do not copy Chain of Industry's project-specific instructions. If the user's complete C# convention document is available, use it in addition to section 8. Verify the existing package-specific Runtime, Editor, optional Input Companion, Edit Mode test, and Play Mode test assemblies **within the user-provided project**. Do not recreate existing foundations merely because the original Step 0 describes creating them. The runtime must compile without the companion or a vehicle controller.

## 3. Architecture contracts to establish before detailed coding

**Data ownership.** Vehicle profiles, view presets, authored orbits, watch markers, and pair connector overrides are reusable configuration assets. Runtime attachment state, assembled curves, current view, player preferences, transition progress, and active commands belong to each camera instance. Runtime operations never mutate shared authoring assets or vehicle prefabs.

**Camera ownership.** One instance receives one explicit Unity Camera and one active view. Multiple instances are independent. Activation/deactivation and restoration of overridden Camera settings are explicit. The host can configure an Inspector component or construct/register the same behavior through an API.

**Dependencies.** The runtime may use Unity's standard transforms, camera, physics queries, and optional Rigidbody signals. It has no dependency on device input, a game controller, saving, a render pipeline, or a scene singleton. The input companion depends on the public camera command API, never on runtime internals.

**Public command contract.** Before implementing each behavior, define its inputs, effect on current state, completion or failure result, and interruption behavior. At minimum this covers activation, view and vehicle change, attach/detach, reverse, manual movement, named and next point-of-interest travel, the two resets, teleport, motion signals, and player control lock. A command that cannot run should report why rather than fail silently.

**Units and time.** Expose distances in Unity world units, travel speeds in world units per second, durations in seconds, and angles in degrees. Use elapsed time supplied to camera logic so the selected unscaled/scaled time source is explicit and behavior can be tested deterministically. Numeric defaults remain tunable configuration, not hidden constants.

**Validation.** Invalid curve topology blocks only the affected view. Diagnostics must identify the body, orbit, or connector involved and provide an actionable editor location. Warnings for possible clipping must not be confused with topology errors.

## 4. Requirement-to-check map

Each ID below names an observable result. The new agent may add finer checks, but should preserve these outcomes and keep this map aligned with the design specification.

| ID | Required result | Focused verification | Step |
| --- | --- | --- | --- |
| CORE-01 | Runtime compiles without the input companion or game-specific packages. | Assembly dependency check and Play Mode smoke test. | 0, 25–27 |
| CORE-02 | Two instances retain separate cameras, views, commands, and state. | Two-instance Play Mode behavior. | 1 |
| CORE-03 | Deactivation restores only Camera settings overridden by the instance; field of view is unchanged. | View-cycle and release tests. | 20 |
| DATA-01 | Runtime changes do not alter shared profiles or vehicle prefabs. | Serialized asset comparison after operations. | 1–24 |
| DATA-02 | Reusable view presets can serve different vehicles while each vehicle supplies its own active-view defaults. | Shared-preset/two-profile test. | 1, 11 |
| ORBIT-01 | Single-body curves are closed, non-self-intersecting, and sampled uniformly by physical distance. | Short/long curve geometry and movement tests. | 2 |
| ORBIT-02 | Removable front/rear sections and generated connectors form valid combined curves. | Geometry and attach tests. | 5 |
| ORBIT-03 | Interchangeable truck/trailer profiles need no authored combined curves; explicit attachment can resolve or accept a pair's connector override. | Pair validation, attach selection, and override behavior. | 7 |
| ORBIT-04 | Articulation does not deform the straight-reference orbit; body-bound aim follows the actual body. | Articulated body tests. | 8 |
| ORBIT-05 | Attachment preserves a surviving camera section and remaps a removed section smoothly. | Retained-front and removed-rear tests. | 6 |
| ORBIT-06 | A root-only orbit remains unchanged when a trailer attaches. | Merge-disabled attach test. | 6 |
| ORBIT-07 | Local +Z/+Y defaults, authorable orientation offsets, and lead-body pitch/roll produce the expected orbit frame. | Reference-frame and tilted-body tests. | 2, 4 |
| AIM-01 | Aim matches authored markers exactly and interpolates continuously between them. | Marker and pose tests. | 3 |
| AIM-02 | Named points of interest work in any order, stop exactly, and report completion or interruption. | Wheel → plate → engine and replacement tests. | 9 |
| AIM-03 | Aim binds to an exact body instance and responds predictably to disconnection. | Two instances of one profile and detach test. | 8 |
| AIM-04 | The next authored point of interest can be requested without a presentation sequence. | Next-point selection and exact-stop tests. | 9 |
| AIM-05 | Point-of-interest requests report completion, replacement, interruption, disconnection, or unreachable destination. | Exercise each terminal result through public commands. | 8–10 |
| AIM-06 | Exterior orbit aim follows authored watch markers without independent player free-look tilt. | Try vertical input and verify camera position changes without a separate aim offset. | 3–4 |
| CMD-01 | Manual input interrupts automatic travel unless locked; a lock blocks all player camera commands. | Command arbitration tests. | 10 |
| CMD-02 | Manual input can interrupt point-of-interest, smooth view, and reverse travel under the same command policy. | Interrupt each movement type; confirm lock behavior. | 10–11, 14 |
| VIEW-01 | Fixed, Interior, Driving, and Presentation views follow their own presets; none is mandatory. | View-by-view tests. | 1, 11, 13–15 |
| VIEW-02 | Reverse affects only Driving and is triggered by the host. | Reverse requests in every view. | 14 |
| VIEW-03 | A view switch chooses the nearest allowed angle and obeys smooth or one-frame snap mode. | Restricted-angle transition tests. | 11 |
| VIEW-04 | Smooth automatic travel uses preset speed by default and accepts speed or fixed-duration overrides. | Default, overridden speed, and duration timing tests. | 9, 11 |
| VIEW-05 | Driving reverse travels smoothly to its player front default pose; front and rear may use different zoom distances, and speed distance is disabled by default but optional in reverse. | Front/rear pose and reverse-distance tests. | 12, 14 |
| VIEW-06 | One camera instance can activate views on different named orbits, while views may also share an orbit. | Same-orbit and cross-orbit view-switch tests. | 11 |
| VIEW-07 | Both exterior views permit full 360-degree travel by default and obey authored angle limits when configured. | Full-circle and restricted-angle tests in both views. | 11, 13 |
| VIEW-08 | Driving favors road ahead at the rear while side framing retains a forward component and permits wheel or fifth-wheel detail; Presentation uses its own aim track. | Rear/side/front authored-marker pose tests in both views. | 3, 13 |
| MOVE-01 | Orbit travel uses physical speed, eases into manual movement, and stops when input ends. | Short versus five-times-long curve, start response, and release-stop tests. | 2 |
| MOVE-02 | Zoom and height move in their agreed directions and stay within one range per orbit. | Pose/range tests including pitch and roll. | 4 |
| MOVE-03 | Actual speed changes camera distance only; field of view remains fixed. | Supplied, Rigidbody, and estimated speed tests. | 13 |
| MOVE-04 | Follow lag, aim smoothing, maximum lag, and turn look are independent; turn look applies only to Interior and Driving by default. | Motion and per-view exclusion tests plus user visual review. | 13–15 |
| MOVE-05 | Low speed frames closer; high speed frames farther, with a smaller but nonzero side effect. | Rear/side speed response tests. | 13 |
| MOVE-06 | The orbit follows lead-body pitch and roll while image roll remains a per-view choice. | Tilted-body pose tests under both roll settings. | 4, 11 |
| SEAT-01 | Three Interior modules toggle independently, move position only, and respect the seat envelope. | Motion-signal and bounds tests. | 16–18 |
| SEAT-02 | Supplied motion signals take priority over Rigidbody measurements and Transform estimates; Rigidbody is optional. | Signal-source fallback and no-Rigidbody tests. | 16–18 |
| SEAT-03 | Players can disable or intensify the cushion without changing the other two seat modules. | Runtime intensity and independent-toggle tests. | 18 |
| COL-01 | Collision correction respects legal zoom/height limits and reports when no clear pose exists. | World and own-body obstruction tests. | 19 |
| COL-02 | World obstructions prefer an inward correction; own-body obstructions may move outward or upward within limits. | Tunnel and own-body obstruction tests. | 19 |
| COL-03 | Collision correction does not alter watch targets or pathfind through geometry during view switches. | Occluded-watch-target and smooth/snap switch tests. | 11, 19 |
| RESET-01 | Orbit-only and full-view resets differ; front/rear player defaults retain separate position, zoom, and height; persistent and timed recenter modes work, and timed recenter waits for motion. | Reset, distinct-default, persistence, and stationary tests. | 12 |
| STATE-01 | Vehicle change uses new vehicle defaults unless garage angle preservation is requested; teleport snaps while retaining orbit/zoom/height. | Target-change and teleport tests. | 21 |
| STATE-02 | Orbit rebuild maps saved front/rear defaults and carries player zoom/height adjustments relative to the new authored defaults, with clamping. | Attach/remap state-transfer tests. | 6, 21 |
| TIME-01 | Unscaled time is default, with a per-view scaled-time option. | Changed-time-scale tests. | 20 |
| RENDER-01 | Masks and clip distances derive from the Camera baseline and do not accumulate. | View-cycle and release tests. | 20 |
| RENDER-02 | Rendering presets require no project-specific layers. | Core import and baseline mask tests with ordinary layers. | 20 |
| EDIT-01 | Guided setup creates an editable profile, orbit, and watch target without prefab mutation. | Editor workflow tests. | 22–23 |
| EDIT-02 | Editor previews connected bodies, articulation, framing, and zoom/height envelope. | Preview and profile-consumption tests. | 24 |
| EDIT-03 | Invalid topology blocks only the affected view with a usable diagnostic. | Validation tests. | 24 |
| EDIT-04 | The generated connected Driving default is centered near the trailer rear and above its roof without changing the orbit. | Default-pose and curve-identity tests. | 22, 24 |
| INPUT-01 | Optional companion maps PC and touch input, gestures, and simple uGUI buttons to public commands. | Input simulation and assembly-isolation tests. | 25–27 |
| INPUT-02 | Each view can configure movement rate and drag sensitivity; player overrides stay instance-local. | Preset/override and two-instance tests. | 2, 25–26 |
| INPUT-03 | A held control must return to neutral after a view switch or lock before it acts again. | Held-input switch and lock tests. | 10, 11, 25–26 |
| INPUT-04 | Equivalent device-independent commands produce the same camera behavior whether game code, PC, or touch issues them. | Compare direct API and companion-driven command traces. | 25–26 |

## 5. Small implementation steps

The numbered steps remain the delivery/dependency overview. Each linked step file adds its difficulty, startup checkpoint, recovery scope, detailed acceptance cases and validation record. The status tracker selects the recovery order; do not assume earlier steps are accepted. **Implement one step, run its Edit Mode and Play Mode tests, provide a short manual observation procedure, and stop.** The user inspects the result and explicitly requests the next step. If one step proves too large to review, split it before implementation and keep the same test and review gate. Tests may create temporary GameObjects and geometry; committed product sample scenes are out of scope.

| Step | Implement only this slice | Edit Mode test | Play Mode test and observation |
| --- | --- | --- | --- |
| **[0. Package skeleton](Steps/Step00-package-skeleton.md)** | Establish package and test assemblies inside the supplied project. | Verify assembly references and core isolation. | Activate and release a minimal assigned Camera. |
| **[1. Fixed view](Steps/Step01-fixed-view.md)** | Add profile/preset basics, explicit camera ownership, and Fixed view. | Validate required references and unchanged profile assets. | Move two bodies with separate camera instances; verify independent fixed poses and no movement from player camera input. |
| **[2. Single-body orbit](Steps/Step02-single-body-orbit.md)** | Add a closed planar curve, topology validation, distance sampling, manual horizontal travel, per-view movement rate, and local-frame defaults with orientation adjustment. | Check closure, intersection, length, sampling, frame, and rate settings. | Orbit around short and five-times-long curves; observe gentle start and immediate stop on release. |
| **[3. Watch-marker aim](Steps/Step03-watch-marker-aim.md)** | Add any number of authored watch markers and continuous aim interpolation without exterior free-look tilt. | Validate marker locations and exact endpoint evaluation. | Move around the orbit and observe exact marker aim and continuous intermediate aim. |
| **[4. Height and zoom](Steps/Step04-height-and-zoom.md)** | Add local-up height and normal-direction physical zoom, with one range per orbit. | Validate ranges and out-of-range inputs. | Raise and zoom around a pitched/rolled body; observe legal clamping and unchanged field of view. |
| **[5. Two-body geometry](Steps/Step05-two-body-geometry.md)** | Add removable front/rear sections and generated connectors for a two-body curve. | Validate topology with different body sizes and front/rear joins. | Display the resulting orbit with connected bodies and confirm it forms one continuous loop. |
| **[6. Attach and remap](Steps/Step06-attach-and-remap.md)** | Add explicit attach/detach, retained-section behavior, removed-section remapping, root-only orbit option, and relative zoom/height state transfer. | Validate section identity, destination mapping, and clamped offsets. | Attach from front and rear positions; observe stay/remap, carried adjustments, and unchanged root-only orbit. |
| **[7. Longer chains](Steps/Step07-longer-chains.md)** | Extend composition to linear chains and ordered pair connector overrides. | Validate several body counts and interchangeable profile pairs. | Connect three bodies and observe the runtime curve and one overridden connector. |
| **[8. Body-bound aim](Steps/Step08-body-bound-aim.md)** | Bind watch targets to exact body instances, preserve straight orbit during articulation, and handle root-group split. | Validate binding identity with duplicate profile instances. | Articulate and detach a trailer; observe fixed orbit shape and target-disconnect response. |
| **[9. Point-of-interest travel](Steps/Step09-point-of-interest-travel.md)** | Add named/next destinations, shortest/directed route, preset speed or per-call speed/duration, precise stop, and command results. | Validate route, destination selection, and each terminal result. | Request wheel → number plate → engine and next, including destination replacement and an unreachable target. |
| **[10. Manual arbitration](Steps/Step10-manual-arbitration.md)** | Add manual interruption for automatic camera movement and a lock that blocks all player camera commands; require neutral input before a held control resumes. | Validate command state transitions, lock policy, and neutral rearming. | Interrupt point-of-interest travel manually, then repeat with the lock active and observe ignored player commands. |
| **[11. View switching](Steps/Step11-view-switching.md)** | Add closest-angle mapping across shared/different named orbits, default/per-call speed, fixed-duration smooth and one-frame snap transitions, and neutral rearming. | Validate view availability, angle limits, transition settings, orbit selection, and held-input reset. | Switch between full and restricted views on different orbits; observe nearest angle, smooth/snap choice near geometry, and each transition mode. |
| **[12. Reset and recenter](Steps/Step12-reset-and-recenter.md)** | Add front/rear player default poses with independent orbit position, zoom, and height; add orbit-only/full reset plus persistent and stationary-safe timed recenter modes. | Validate distinct defaults, reset scopes, and both recenter settings. | Compare reset levels and front/rear distances; verify persistent mode, then stop the body during the timed delay and observe no stationary reset. |
| **[13. Driving follow](Steps/Step13-driving-follow.md)** | Add Driving/Presentation behavior, independent follow/aim smoothing, lag cap, actual-speed distance, and their distinct road/detail aim tracks. | Validate speed thresholds, rear/side distance response, authored aim markers, and independent settings. | Supply speed, use Rigidbody speed, then estimate from Transform; observe low-speed closeness, high-speed distance, and road/wheel framing without field-of-view change. |
| **[14. Reverse and Driving turn look](Steps/Step14-reverse-and-driving-turn-look.md)** | Add explicit reverse request and gentle Driving turn look; disable speed distance in reverse by default. Resolve the reverse-clear behavior before implementation. | Validate reverse/view rules, default/optional reverse distance, direction-hint settings, excluded views, and manual interruption. | Reverse from Driving, then from other views; observe only Driving turn to its front default and test the agreed reverse-clear behavior. |
| **[15. Interior head and turn look](Steps/Step15-interior-head-and-turn-look.md)** | Add seat pose, limited yaw/pitch head movement, gentle Interior turn look, and manual override of that assistance. | Validate directional limits, seat reference, and no turn look in Fixed/Presentation. | Look in all directions, release to neutral, and observe Interior turn assistance resuming. |
| **[16. Acceleration/braking module](Steps/Step16-acceleration-and-braking.md)** | Add independent fore-aft seat displacement with separate acceleration and braking strengths and generic motion-signal fallback. | Validate module settings, signal-source priority, and its contribution to the envelope. | Feed positive and negative acceleration with and without a Rigidbody; observe position-only displacement. |
| **[17. Cornering module](Steps/Step17-cornering-module.md)** | Add independent lateral seat displacement from sideways acceleration at the seat. | Validate lateral response and its contribution to the envelope. | Feed left/right cornering signals; observe position-only displacement. |
| **[18. Cushion module](Steps/Step18-cushion-module.md)** | Add independent short vertical bump response, player intensity control, and combined seat envelope. | Validate filter settings, runtime intensity, and sum bounds. | Compare a short bump to a slow climb; toggle or intensify the cushion and observe bounded position only. |
| **[19. Collision](Steps/Step19-collision.md)** | Add separate collision mask and legal correction with failure result, without a watch-target or road-visibility solver. | Validate mask, zoom/height limits, inward/outward/upward choices, and unchanged watch targets. | Place tunnel, own-body, and aim-point obstructions; observe legal correction and no-clear-pose result. |
| **[20. Rendering and time](Steps/Step20-rendering-and-time.md)** | Add optional Camera mask/clip presets, baseline restore, and per-view time-source selection without requiring special project layers. | Validate preset modes and time settings. | Cycle views and release; vary time scale and verify original Camera settings return. |
| **[21. Target and teleport](Steps/Step21-target-and-teleport.md)** | Add vehicle-target change, optional garage angle preservation, front/rear default remapping, and teleport/respawn notification with agreed state transfer. | Validate target defaults, relative/clamped adjustments, and state mapping. | Swap vehicles and teleport one; observe transition versus snap and preserved orbit, zoom, and height. |
| **[22. Guided profile creation](Steps/Step22-guided-profile-creation.md)** | Generate initial profile, orbit, and watch target from selected renderers or colliders, with exclusions. | Test differing geometry bounds, exclusions, and no prefab mutation. | Use the generated profile to drive a runtime camera. |
| **[23. Scene editing handles](Steps/Step23-scene-editing-handles.md)** | Edit single-body curves, removable sections, watch markers, and zoom/height envelope visually. | Test asset edits, undo, and profile validity. | Run the edited profile and observe the expected camera path. |
| **[24. Connected preview and validation](Steps/Step24-connected-preview-and-validation.md)** | Preview chains, articulation, connector overrides, framing, generated Driving default, and structural errors. | Test valid/invalid previews, default pose, and affected-view diagnostics. | Run an authored chain and compare camera behavior with its preview. |
| **[25. PC input companion](Steps/Step25-pc-input-companion.md)** | Add optional Input System PC mappings through public commands and honor per-view movement rates. | Validate actions, bindings, assembly isolation, and equivalence to direct commands. | Simulate PC input, then remove the companion and check the core still runs. |
| **[26. Touch gestures](Steps/Step26-touch-gestures.md)** | Add normalized drag, pinch, double tap, UI gesture ownership, and configurable drag sensitivity. | Validate gesture thresholds, sensitivity, UI ownership, and equivalence to direct commands. | Simulate touch sequences, including a touch starting on UI and a double-tap reset. |
| **[27. Touch buttons](Steps/Step27-touch-buttons.md)** | Add simple replaceable uGUI buttons for camera actions. | Validate button references and action wiring. | Press each button and observe its corresponding camera command. |

## 6. Decisions the agent must surface at the relevant step

The design specification intentionally leaves some details open. Before the relevant step, the agent should prepare a concrete recommendation and ask the user **one consequential question at a time** when an answer affects public behavior, serialized data, or authoring workflow. Record each answer before implementing dependent work. Independent preparation may continue, but do not cross the one-step review boundary while waiting.

| Before | Decision needed | Safe work that can continue |
| --- | --- | --- |
| Step 7 remapping | Resolved in DecisionLog.md on 2026-09-21: preserve exact surviving body/source position; otherwise map to terminal rear midpoint in the root component. Reopen only for a genuinely uncovered case. | Verify the recorded rule through public chain attachment and remapping. |
| Step 9 next point | Whether `next point of interest` wraps at the final authored point or reports that there is no next point. | Named requests, arbitrary order, routes, and precise stops. |
| Step 12 recenter | Whether the timeout pauses or restarts while stationary. | Manual reset and the rule that no recenter occurs while stationary. |
| Step 14 reverse exit | Whether clearing reverse travels to the saved rear default or retains the player's current orbit position. | Entering reverse, driving-only response, and front-default framing. |
| Step 19 collision completion | Tie-break among multiple legal corrections and result when no clear pose exists. | Legal range checks and separate collision masks. |
| Step 22 editor asset layout | How advanced connector overrides and per-vehicle view defaults appear in the guided UI. | Runtime data contracts and basic Scene handles. |

Numeric defaults, button placement, and input bindings can be proposed as editable defaults and tuned through review. User documentation and product sample scenes are separate later work. Any choice that would contradict the specification requires explicit review rather than a local workaround.

Clean import into a second consumer project and Asset Store release validation can be scheduled after the current implementation. The agent must not create that second project under this plan.

## 7. Per-step verification and reporting gate

For **every numbered implementation step**, add or update at least one meaningful Unity **Edit Mode** test and at least one meaningful **Play Mode** test. Tests should check externally visible contracts or independent invariants, not restate the implementation formula. The Play Mode tests are runtime tests; Edit Mode tests cover configuration, geometry, validation, or editor tools as appropriate. Run both suites after each step and report pass/fail counts and the exact Unity version. If either suite cannot run, report the concrete blocker and leave the step incomplete.

Also provide a short manual observation procedure using temporary objects or test fixtures, so the user can see the new behavior in the Editor or Game view without a product sample scene. Do not proceed automatically after tests pass. Wait for the user's feedback and explicit request for the next step; fix issues they identify within the current step first.

At the end of each step, report changed files, the requirement IDs addressed, Edit Mode and Play Mode results, how the user can observe the result, limitations, and the proposed next step. Review the diff for unwanted dependencies, prefab mutations, or game-specific references. Keep a short decision log in the new repository for new behavior decisions. A requirement is complete only when its checks pass; exposing a setting without working behavior does not satisfy it.

## 8. C# conventions to carry into the new project

These are the user's standing conventions and must be available to the new agent even if the original local skill is not. Put them in the new repository's agent guidance or a linked style document before writing C#.

- No source comments, including XML documentation, unless the user explicitly requests them. User-facing API documentation is deferred to the later documentation work.
- Always use braces for control-flow bodies. Do not use ternary expressions, LINQ, or lambdas.
- Expose data with properties instead of public fields. Do not introduce static classes or static methods; use instance-based helpers.
- Cross-object per-tick calls use `Update<Domain><Phase>` names, such as `UpdateCameraVisuals`, and receive explicit `deltaTime`. Unity-reserved `Update`/`FixedUpdate` callbacks may forward to these methods.
- Within a class file, order fields/properties, constructor or initialization, physics/visual tick methods, remaining public methods, private helpers in first-use order, and cleanup last.
- Field/property lanes are: constants, readonly fields, private mutable fields, private properties, events, public properties. Separate nonempty lanes with one blank line; never insert blank lines inside the mutable-field lane. Within the readonly-field, mutable-field, private-property, and public-property lanes, collections precede concrete custom types, then interfaces, then built-in values ordered string, float, int, bool. `Telemetry`, if present, is the first public property. Keep private helpers in first-use order and teardown last. Preserve the user's full convention document if available for edge cases.

The new agent should present a brief design for the requested step before major C# changes. The user's explicit request to implement that step supplies authorization for its files; it does not require repeated confirmation for every file. Completion of one step does not authorize beginning the next.

## 9. Starting the next task

The next recovery task is **Step 02 — Single-body orbit (Hard, Astra High)**. Use its [starter prompt](Steps/Step02-single-body-orbit.md#starter-prompt-for-a-fresh-task). The agent must announce the Hard checkpoint and stop before implementation/tests; only your explicit continuation in that task releases it.

Each of the 28 step files includes its own ready-to-paste starter prompt. Request exactly one step or an approved slice in a fresh task. Fix review feedback within that task, record evidence, and stop for your explicit acceptance. Do not rely on an old chat transcript or assume a helper class means a requirement is complete.
