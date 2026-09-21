# Step 11 — View switching

- **Difficulty:** Hard
- **Recommended agent:** Astra High (gpt-6-astra, high)
- **Why:** Named-orbit schema, constrained angle mapping, transitions, and cross-command integration.
- **Audit baseline:** Not implemented; current selection releases the camera
- **Prerequisites:** [Step 10](./Step10-manual-arbitration.md)
- **Requirement IDs:** DATA-01, DATA-02, CMD-02, VIEW-01, VIEW-03, VIEW-04, VIEW-06, VIEW-07, MOVE-06, COL-03, INPUT-03. Shared IDs cover this step's slice only; see [coverage](../RequirementCoverage.md).

## Startup checkpoint

**MANDATORY HARD CHECKPOINT — STOP FIRST.** Read this task's guidance and difficulty, then say:

> This is a Hard task: Step 11 — View switching. Recommended agent: Astra High. Please set the agent you want to use, then explicitly tell me to continue. I am paused before implementation and tests.

End the turn. Do not implement, run tests, inspect implementation code in depth or delegate until the user explicitly replies to proceed with this task. This is required even if Astra High appears selected. Plan approval, the starter prompt, silence and another task's approval do not release the checkpoint. Once released in this task, record it and proceed without repeated model-gate requests.

## Required reading and entry evidence

Read [workflow](../ImplementationWorkflow.md), [status](../ImplementationStatus.md), [master plan](../ImplementationPlan.md), [design](../VehicleCameraSystemDesign.md) sections 2, 5, 6, 9, [decision log](../../DecisionLog.md), and [C# conventions](../CSharpConventions.md). Read the [dated audit](../ImplementationAudit-2026-09-21.md) for recovery context.

After any required checkpoint release, verify current source and prerequisites rather than assuming the audit is still current. Start at [CameraSystemController](../../Runtime/CameraSystemController.cs), [VehicleProfile](../../Runtime/VehicleProfile.cs), [CameraViewPreset](../../Runtime/CameraViewPreset.cs), the relevant Runtime/Editor/Input implementations, and the existing [Edit Mode](../../Tests/EditMode) and [Play Mode](../../Tests/PlayMode) suites. These are entry points, not a prescribed class design or a restriction against focused new files.

No completed implementation was established by the audit. Inspect for intervening changes before adding behavior. Preserve and build on accepted prerequisites.

**Product decisions:** Follow the decision log. No additional unresolved choice is predeclared for this slice; surface any consequential conflict before implementing dependent behavior.

## Original plan slice

Add closest-angle mapping across shared/different named orbits, default/per-call speed, fixed-duration smooth and one-frame snap transitions, and neutral rearming.

Original Edit Mode check: Validate view availability, angle limits, transition settings, orbit selection, and held-input reset.

Original Play Mode check/observation: Switch between full and restricted views on different orbits; observe nearest angle, smooth/snap choice near geometry, and each transition mode.

## Scope and acceptance outcomes

- Support any number of named views and orbits, several views sharing an orbit, and per-vehicle defaults for reusable presets.
- Map current angle to the nearest allowed destination angle; allow full-circle exterior views by default and authored restrictions.
- Implement destination-preset speed, per-call speed, fixed-duration smooth transition and one-frame snap without pathfinding.
- Integrate player lock/manual interruption/neutral rearming and per-view image-roll choice. Establish exterior Driving selection/movement contract here; Step 13 adds Driving follow behavior.

Do not implement later steps, add excluded features or silently reduce a requirement. Runtime changes must not mutate shared profiles/presets or vehicle prefabs. Use explicit elapsed time, per-instance state and the public command contracts. A helper or configuration field without runtime/editor consumption does not meet acceptance.

## Edit Mode validation

- Test named lookups, missing/invalid destination views, shared presets across profiles and same-/cross-orbit closest-angle mapping around wrap and limits.
- Test timing modes, unsupported combinations, configurable roll, and source/destination state when a transition is replaced or interrupted.
- Ensure an invalid destination affects only that view and provides diagnostic identity while another valid view stays usable.

Use independently defined expected outcomes. Add/update meaningful tests for the changed behavior and run the package Edit Mode suite after the final change.

## Play Mode validation

- Switch an actual Camera between shared and different orbits, full/restricted exterior views, and Fixed; verify nearest legal angle and smooth/snap timing.
- Hold movement across a switch and require neutral before rearming. Interrupt smooth travel manually, and verify locks block player switches but allow host switches.
- Test tilted-body image roll enabled/disabled and transitions near geometry without adding pathfinding. Recheck collision integration in Step 19.

Exercise the actual Camera through the public runtime API wherever this slice has camera behavior. Add/update meaningful Play Mode tests and run the package Play Mode suite after the final change. A failed or unavailable suite leaves this step incomplete.

## Manual observation

Create temporary shared-orbit and different-orbit presets, including restricted angles. Switch while holding movement, compare smooth and snap transitions, and toggle image roll on a tilted vehicle.

Use temporary internal fixtures, not a product sample scene. At handoff, give exact setup/activation actions and expected observations for the final implementation; do not merely repeat this outline. Record whether the agent observed it and whether the user verified it. Do not claim a manual pass without performing or receiving confirmation of the check.

## Exit and handoff

Follow the [shared validation and completion rules](../ImplementationWorkflow.md). Report requirement IDs delivered, pending shared checks, changed files, public API integration, actual Edit/Play counts, Unity version, exact invocation or runner procedure, result/log paths, source revision/dirty state, manual instructions, limitations and next proposed step. Update this record and the [status tracker](../ImplementationStatus.md). Stop for review; do not begin another step.

## Validation record

- Implementation: Not implemented; current selection releases the camera
- Hard checkpoint: Pending in each new task; no continuation granted by this document.
- Baseline revision / relevant dirty files: Not recorded for this task.
- Decisions resolved in this task: None recorded.
- Edit Mode run/date/version/command/results: Not run by this refactor.
- Play Mode run/date/version/command/results: Not run by this refactor.
- Independent expected-result evidence: Pending.
- Public API / actual Camera or editor integration evidence: Pending.
- Manual fixture, actions and observations: Pending.
- Pending shared requirements / later owners: See requirement coverage; update with exact remaining cases at handoff.
- Limitations / blockers: See audit baseline and entry conditions; verify current state.
- User acceptance: Not recorded.
- Completion revision / dirty state and evidence date: Pending.

Preserve dated evidence when updating this record; do not erase a failure or prior acceptance when a regression is reopened.

## Starter prompt for a fresh task

> Work on Step 11 only. Read Assets/Gley/VehicleCameraSystem/AGENTS.md, Docs/ImplementationWorkflow.md, Docs/ImplementationStatus.md and Docs/Steps/Step11-view-switching.md in the package, plus the linked design and decisions. This is Hard: announce the recommended Astra High agent and stop your turn before implementation, tests or an in-depth code audit. Wait for my explicit reply to continue, even if the model already appears correct. After that reply, Verify prerequisites, preserve existing uncommitted work, implement or repair only this step, run the required Edit Mode and Play Mode suites, provide the manual observation procedure, update the evidence/status, and stop for my review. Do not start the next step.
