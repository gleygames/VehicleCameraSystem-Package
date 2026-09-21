# Vehicle Camera System — design specification

**Status:** Review draft, 2026-09-17. This records design decisions from the camera discussion. The user will create and configure a new Unity project and repository, completely separate from the current game project. This document is not an instruction to change the existing game camera.

## 1. Product and boundaries

Vehicle Camera System is an independent Unity Asset Store product developed in its own new Unity project. It is not an extraction of the current game's camera code. Trucks and articulated trailers are the primary authoring case; a vehicle without a trailer must work equally well. The basic camera contract also applies to other vehicles, including boats and aircraft, without adding specialized boat or flight behavior in the first release. Unity 2022.3 is the minimum supported version.

The package must not depend on Chain of Industry, Gley Truck Controller, a particular vehicle controller, an input device, a save system, Cinemachine, or a render pipeline. No camera type is mandatory. A game may use only the garage presentation views, for example. One system instance controls one explicitly assigned Unity Camera, and multiple instances may coexist. There is no global camera singleton and no automatic takeover of `MainCamera`. Activation and deactivation are explicit.

The present implementation covers the runtime core, editor authoring tools, and an **optional input companion included in the same product**. The companion uses Unity's new Input System only. It supplies PC controls, touch gestures, and simple replaceable uGUI buttons. The runtime core works without the companion. User documentation and sample scenes are separate later work and are outside the present implementation scope. Internal test fixtures may be used to verify behavior; they are not product sample scenes.

Two setup paths use the same runtime: an Inspector component referencing an existing Camera, and an API for games that create cameras at runtime. Neither requires changing a vehicle prefab. A guided profile setup tool is part of the first release.

## 2. Configuration and ownership

| Concept | Stores or controls |
| --- | --- |
| Vehicle profile | Reusable body reference frame, authored curves, removable end sections, connector ends, seat data, watch points, and vehicle-specific view defaults. |
| View preset | Reusable behavior for a fixed, interior, driving, or presentation view: motion, limits, smoothing, reset, collision, and optional rendering settings. |
| Orbit | One authored closed horizontal curve, its watch-point track, zoom and height range, and an option to merge attached bodies. A vehicle may have any number of named orbits. |
| Pair override | Optional advanced connector geometry for an ordered pair of vehicle profiles and attachment ends. It replaces only the generated connection, not either body's curve. |
| Camera instance | The assigned Camera, active view, registered body instances, assembled orbit, player adjustments, active command, and transition state. |
| Player preferences | Runtime overrides such as front/rear defaults and recenter behavior. The host game owns persistence. |

Profiles and presets are reusable authored assets. Attaching a trailer, changing a player's default, or moving the camera must change only instance state; it must not edit shared assets or vehicle prefabs. A profile needs defaults for the views it actually offers, but the package does not enforce a particular set of views.

An active instance overrides only Camera rendering properties explicitly enabled by its rendering preset. It records the original values and restores them when released. Rendering presets may replace a culling mask or add/remove layers relative to the original mask, and may override near/far clip distances. The package requires no particular project layers. View changes derive from the original baseline rather than accumulating mask changes. The field of view stays at its initial value. Render-pipeline-specific settings remain outside the core.

## 3. Coordinates, curves, and articulation

The default body reference frame is local +Z forward and +Y up, with an authorable orientation adjustment for unusual models. Curves lie in the body's local horizontal plane. Camera height moves perpendicular to that plane. The orbit follows the lead body's full rotation, including pitch and roll; camera image roll is a separate per-view setting.

Each body profile has a complete standalone closed curve with removable front and rear sections. Developers may reshape it and add points freely, but a valid orbit cannot self-intersect. The guided tool generates a starting curve from selected vehicle geometry; selected renderers, colliders, exclusions, and manual correction let developers avoid irrelevant protrusions. It also generates an initial forward-facing watch target for the default rear position. The authored curve and watch markers are the source of truth after editing.

When bodies connect in a linear front-to-back chain, the facing removable sections disappear and left/right connectors join the retained curves into one closed orbit. Connectors are generated by default. An optional pair override can replace a generated connector for a particular ordered profile pair; the explicit attach API resolves or accepts that authored override when assembling the runtime orbit. This lets ten truck profiles and ten trailer profiles work together without authoring one hundred complete combined curves. Branching attachments are outside the first-release model.

The assembled orbit uses the **straight reference arrangement** of the connected bodies. Actual trailer articulation does not bend the orbit in a curve: the trailer can swing toward or away from the camera while the camera's orbit remains tied to the lead body. Watch targets bound to a trailer may still follow that trailer's actual transform. The editor must preview a connected chain, its generated or overridden connectors, watch framing, and articulated poses.

Merging is chosen per orbit. An authored truck-only orbit can remain unchanged while a trailer is attached; a developer may simply extend that truck curve behind the vehicle. Explicit attach and detach API calls rebuild only the affected runtime orbit. If the current camera position survives the rebuild, it stays there. If it lies on a removed section, it moves smoothly to the corresponding location on the new orbit. In the discussed truck/trailer case, a camera on the retained truck front curve stays in place; a camera on the removed truck rear curve moves to the new trailer rear. When a chain splits, the camera remains with the component containing its registered root unless the game explicitly retargets it.

Invalid open, disconnected, or self-intersecting assembled curves block the affected view and produce a clear authoring error. Other valid views remain usable.

## 4. Aim and points of interest

Camera position on an orbit, camera aim, and the source of movement commands are separate concerns. An orbit supports any number of authored watch markers. At each marker the camera aims at its exact authored watch point; between markers the watch point interpolates continuously. An exterior orbit view does not gain an independent free-look tilt away from that aim. Manual vertical movement is straight along local up, and zoom changes physical distance inward or outward from the horizontal orbit, without changing field of view.

Watch targets can bind to the truck/root or to a specific attached body instance, such as a trailer. The API can select that binding, so a driving view may keep its aim attached to the truck and a presentation view may follow the trailer. Binding to an exact instance matters when multiple bodies share the same profile. If a targeted body detaches, a command targeting it ends with a `target disconnected` result and the view returns to its root-group default aim.

Presentation logic can request a named point of interest in any order, or request the next authored point of interest: for example, wheel, engine, number plate. The camera travels around its curve until it reaches the requested marker, eases at the start and end, and stops precisely there. Its default travel route is the shortest valid route, with an explicit direction override available. The view's configured travel speed applies by default; a call may supply another speed or a fixed arrival duration. A new destination replaces the current destination immediately from the current pose. Completion, replacement, interruption, disconnection, and unreachable destination must be observable results. The camera system receives requests; it does not run a garage presentation sequence itself.

## 5. Views

There is one active view per camera instance, but any number of named view presets and orbits may be authored. A view selects an available named orbit from its vehicle profile; several views may share one orbit, or select different orbits. Selecting another view can therefore move the same Camera to a different orbit under the view-transition rules. This is how one camera instance can provide many developer-authored presentation views.

**Fixed:** Holds a body-relative camera position and watches a fixed body-relative point. Player camera input does not move it.

**Interior:** Uses an authored seat/eye position. Player yaw and pitch act like head rotation with independently configurable directional limits. Gentle automatic look into a turn is available; manual head look pauses that assistance until the view returns to neutral or is reset. The eyes remain aimed at the road as the seat motion modules change camera position.

**Exterior Driving:** Smoothly follows from a default behind the vehicle, keeps the road readable, and allows a full 360-degree orbit by default. Configurable angle limits can restrict that range later. Its aim track is distinct from a presentation view: the rear favors road ahead, the sides retain forward bias while permitting wheel or fifth-wheel views, and the front can aim back toward the vehicle. Automatically generated markers are editable starting points. Only this view responds to reverse requests.

**Exterior Vehicle/Presentation:** Uses the same general orbit and aim machinery, with framing chosen by the developer for viewing the vehicle and its details. It also permits a full 360-degree orbit by default, with configurable angle limits. It can use named points of interest and does not react to reverse.

The generated Driving default for a connected truck and trailer is a centered view near or behind the trailer's rear edge, with the camera roughly two metres above the trailer roof so the road ahead is visible. This is an editable **default camera position**, not a change to the orbit curve, a forced side view, or a visibility-correction rule. A custom truck-only orbit remains valid.

## 6. Motion and transitions

Manual orbit travel uses physical distance along the curve per unit time. A much longer trailer therefore takes proportionally more input to circle. Movement may ease gently when input begins but stops when input ends, allowing precise framing. Zoom moves along the curve's local horizontal inward/outward normal; height moves along local up. Zoom and height each have one allowed range for the whole orbit; developers reshape the curve where a particular angle needs to be closer. The editor previews the whole allowed zoom and height envelope.

Driving follow position lag is required and configurable. Aim smoothing has an independent setting. A configurable maximum lag and catch-up behavior prevent the camera from falling excessively far behind. Speed adaptation changes camera **distance only**: low speed stays closer and higher speed moves farther away. It uses actual world speed, with supplied speed preferred, then Rigidbody velocity, then a Transform-change estimate. The distance effect is strongest behind the vehicle and fades toward the sides while retaining a small nonzero side effect. It does not change field of view or speed-dependent aim. A gentle turn look can use measured angular motion, with an optional direction hint from the game for anticipation. This applies to Interior and Exterior Driving by default, not Fixed or Presentation.

Reverse is an explicit camera API request. The game decides whether gear, velocity, or another condition calls it. In Exterior Driving, activation smoothly travels to a player-configurable front default pose, looking past the vehicle toward the road behind. Front and rear defaults may use different zoom distances. The speed distance offset is off in reverse by default but configurable. The other views do nothing on reverse requests.

When switching views, the camera maps the present angle to the closest angle available in the destination view, then transitions smoothly. Automatic travel uses the destination view's configured speed by default; a call may override the speed or request a fixed-duration smooth transition or a one-frame snap. The caller can choose a snap when a straight transition would pass through geometry; the camera does not pathfind between views. When changing the target vehicle, the new vehicle's defaults are used with a smooth transition; an explicit garage option may preserve the viewing angle instead. When an attached orbit changes shape, saved front/rear defaults map to the new front/rear, while player zoom and height adjustments carry relative to the new orbit's authored defaults and clamp to its ranges. A teleport/respawn notification snaps the camera to the vehicle's new pose while retaining current orbit, zoom, and height.

The camera uses unscaled elapsed time by default, with a per-view option for scaled game time.

## 7. Interior motion modules

Interior seat movement has three independent, optional **position-only** modules. They sum within a per-vehicle seat movement envelope; none rotates the camera.

1. **Acceleration/braking:** Small fore-aft displacement, with separate acceleration and braking strengths.
2. **Cornering:** Sideways displacement from lateral acceleration at the seat, with its own response and limits.
3. **Cushion:** Subtle short vertical motion from bumps, filtering out slow hills and altitude changes. Players may disable or intensify it.

The reusable Interior preset holds the module settings; the vehicle profile holds the seat position and safe movement bounds. Motion signals supplied by the game take priority, followed by available Rigidbody measurements and then estimates from the registered Transform. No Rigidbody is required. Default strengths should be subtle and will be tuned later.

## 8. Collision and visibility

Collision correction works within the same zoom and height limits as manual movement. For an environmental obstruction it first tries moving closer along the allowed zoom direction; for the vehicle's own body it may move outward; it may rise when needed. The actual choice is the least obstructed legal pose, with a reported limitation if no legal clear pose exists. Collision geometry and render culling use separate layer masks.

There is no automatic watch-point visibility correction, road visibility solver, camera pathfinding, or unbounded move over an obstacle. The authored Driving default is only a suggestion.

## 9. Commands, input, and reset

The core accepts device-independent commands: held horizontal/vertical/zoom intent, normalized drag displacement, view selection, reverse, named or next point of interest, orbit reset, full active-view reset, attach/detach, teleport, and optional motion data. Each view maps the same generic movement intent to its own behavior. View presets provide configurable movement rates and drag sensitivity; player overrides remain instance state for the host game to save if desired. The optional Input System companion handles PC mappings, touch gestures, simple uGUI buttons, double tap, pinch, dead zones, and UI blocking. A touch that begins on UI stays UI-owned until release. Stale held input or gestures are discarded across view changes and manual locks; a held control must return to neutral before it can act again.

Manual input interrupts an active automatic camera movement, including point-of-interest travel or a smooth view/reverse transition, unless that command has explicitly locked player control. A lock blocks all player camera commands for that instance, including movement, switching, and reset, while game API calls remain available. The garage can therefore lock the camera while editing a number plate and allow manual orbiting in a general view.

The player may set separate front and rear default poses for the Driving view, each with its own orbit position, zoom distance, and height. **Orbit reset** returns only to the applicable authored/player orbit position. **Full view reset** returns the active view's orbit position, zoom, height, and rotation to its defaults. Recenter can be configured to stay where the player left it until reset or to occur after a delay. Automatic recenter does not occur while the vehicle is stationary; it resumes only after movement starts. A double-tap or button reset is supplied by the input companion. The host game saves player preferences; the camera package supplies runtime settings and commands, not a save system.

## 10. Editor and package development

The guided tool creates a reusable vehicle profile and initial curve from a selected model. Scene handles let the developer edit the curve, removable sections, connectors, orbit defaults, watch markers, seat limits, and zoom/height envelope. The editor must show two-body and full-chain connected previews and articulation, while keeping profiles separate from prefabs. Generated connectors are the default; pair overrides are an advanced option. Structural errors prevent use of only the invalid view, and likely clipping at extreme poses is shown as a warning.

The product should keep one core Runtime assembly and one Editor assembly initially. The optional input companion has a separate assembly and depends on the core public API and Unity Input System. It must not cause a compilation failure when the Input System is absent. Product files belong under one Asset Store root folder, including the optional companion. The first distribution target is a conventional `.unitypackage`, subject to format validation at release time. The user provides the new standalone Unity project and repository; the implementation agent works only there. Each small implementation step must include meaningful Unity Edit Mode and Play Mode tests, then stop for the user to observe and verify before the next step. The source should remain organized so the Asset Store distribution format can change without changing the camera architecture.

## 11. Acceptance checks derived from the decisions

| Scenario | Expected behavior |
| --- | --- |
| Ten interchangeable trucks and ten interchangeable trailers | Twenty reusable body profiles plus optional connector overrides; no requirement for one hundred combined curves. |
| Attach a trailer while viewing the truck's retained front curve | Camera remains at the same orbit position. |
| Attach a trailer while viewing the truck's removed rear curve | Camera moves smoothly to the new combined rear curve. |
| Turn sharply with an attached trailer | Straight-reference orbit retains its shape; the trailer articulates relative to it. |
| Circle a trailer five times longer than another | Approximately five times more held orbit input is needed at the same configured travel speed. |
| Request number plate while viewing wheel | Camera follows the orbit to the named marker and stops on it, with aim interpolating between markers. |
| Request the next authored point of interest | Camera travels to that point and stops precisely, without requiring the package to run a presentation sequence. |
| Request reverse while in Interior or Presentation | No automatic view change. |
| Reverse in Driving | Camera rotates smoothly to the saved front default; speed-based distance is off by default and can be enabled. |
| Stop vehicle during timed recenter | No automatic reset until the vehicle moves again. |
| Change speed | Only configured camera distance changes; field of view remains fixed. |
| Import core without input companion or a particular vehicle controller | Runtime remains usable through its API. |
| Import the optional companion with the product | PC, touch, and simple button controls issue the same core commands without changing core behavior. |
| Switch views while holding movement input | The old held input is discarded until the control returns to neutral. |
| Generate a connected truck/trailer Driving default | Centered near or behind the trailer's rear edge and roughly two metres above its roof, without editing the orbit. |
| Run two camera instances | Each retains independent active view, state, commands, and Camera ownership. |

## 12. Open decisions and items to tune

These are **not** recorded as settled requirements:

- The new standalone Unity project's exact location and source-control arrangement will be supplied by the user when it is ready. Project creation and configuration are not implementation-agent tasks.
- Numeric defaults: orbit speed, transition durations, follow/aim smoothing, speed thresholds, seat movement amplitudes, cushion filtering, and collision clearances.
- Exact editor workflow and asset schema, including how the guided tool presents advanced connector overrides and per-vehicle view defaults.
- The precise orbit remapping rule for complex multi-body attachment changes beyond the agreed retained/removed-section examples.
- How `next point of interest` behaves at the final authored point (wrap or report no next point); named points can already be requested in any order.
- Exact collision solver priorities when several legal corrections compete, and the status reported when none clears the view.
- Input companion's initial button layout and default PC/touch bindings. Only its Input System dependency and gesture-plus-simple-button scope are decided.
- Whether a timed recenter countdown pauses or restarts while stationary; recenter itself must wait until movement resumes.
- Whether clearing a Driving reverse request automatically travels back to the saved rear default or preserves the player's current orbit position. Entering reverse and the other views' no-op behavior are settled.
- Whether rendering presets will eventually expose more common Camera properties. Culling mask and clip distances are the agreed first scope; field of view stays unchanged.
- User documentation and sample scenes, including their render-pipeline presentation, are separate later work.

Changes to these open items should be reviewed against the acceptance checks before implementation.
