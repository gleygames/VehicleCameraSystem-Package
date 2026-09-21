# Step 04 — Height and zoom

- **Difficulty:** Easy
- **Recommended agent:** Terra High (gpt-5.6-terra, high)
- **Why:** Bounded correction of axes, offset ranges, and FOV invariants.
- **Audit baseline:** Partial; orientation-axis defect
- **Prerequisites:** [Step 02](./Step02-single-body-orbit.md), [Step 03](./Step03-watch-marker-aim.md)
- **Requirement IDs:** DATA-01, ORBIT-07, AIM-06, MOVE-02, MOVE-06. Shared IDs cover this step's slice only; see [coverage](../RequirementCoverage.md).

## Startup checkpoint

**Easy task — Terra High recommended.** The request to implement this step authorizes scoped work; no mandatory model-switch pause is required. If the work becomes Hard, announce that and stop for explicit continuation under the shared workflow before doing the Hard work.

## Required reading and entry evidence

Read [workflow](../ImplementationWorkflow.md), [status](../ImplementationStatus.md), [master plan](../ImplementationPlan.md), [design](../VehicleCameraSystemDesign.md) sections 3, 4, 6, [decision log](../../DecisionLog.md), and [C# conventions](../CSharpConventions.md). Read the [dated audit](../ImplementationAudit-2026-09-21.md) for recovery context.

After any required checkpoint release, verify current source and prerequisites rather than assuming the audit is still current. Start at [CameraSystemController](../../Runtime/CameraSystemController.cs), [VehicleProfile](../../Runtime/VehicleProfile.cs), [CameraViewPreset](../../Runtime/CameraViewPreset.cs), the relevant Runtime/Editor/Input implementations, and the existing [Edit Mode](../../Tests/EditMode) and [Play Mode](../../Tests/PlayMode) suites. These are entry points, not a prescribed class design or a restriction against focused new files.

Height currently uses vehicleBody.up even when OrientationAdjustment changes the orbit plane. Repair the height reference frame and check zoom near the sampler boundaries.

**Product decisions:** Follow the decision log. No additional unresolved choice is predeclared for this slice; surface any consequential conflict before implementing dependent behavior.

## Original plan slice

Add local-up height and normal-direction physical zoom, with one range per orbit.

Original Edit Mode check: Validate ranges and out-of-range inputs.

Original Play Mode check/observation: Raise and zoom around a pitched/rolled body; observe legal clamping and unchanged field of view.

## Scope and acceptance outcomes

- Move height perpendicular to the adjusted orbit plane and zoom along its inward/outward horizontal normal.
- Apply one zoom range and one height range for each orbit. Positive zoom intent moves inward per the decision log.
- Preserve vehicle pitch/roll following. Per-view image-roll selection belongs to Step 11.

Do not implement later steps, add excluded features or silently reduce a requirement. Runtime changes must not mutate shared profiles/presets or vehicle prefabs. Use explicit elapsed time, per-instance state and the public command contracts. A helper or configuration field without runtime/editor consumption does not meet acceptance.

## Edit Mode validation

- Test ordered/invalid ranges, clamping, positive/negative inputs and independent per-view rates.
- Use an orientation adjustment that changes up, not only yaw, plus a pitched/rolled body; compare expected axes from independently composed rotations.
- Check normal directions at straight sections, curved sections and boundaries after Step 2.

Use independently defined expected outcomes. Add/update meaningful tests for the changed behavior and run the package Edit Mode suite after the final change.

## Play Mode validation

- Use public height/zoom commands while orbiting an adjusted and tilted body; verify offsets in world units, correct axes, legal bounds and unchanged FOV.
- Confirm aim remains on authored targets and commands do not introduce free-look tilt or mutate shared profiles.

Exercise the actual Camera through the public runtime API wherever this slice has camera behavior. Add/update meaningful Play Mode tests and run the package Play Mode suite after the final change. A failed or unavailable suite leaves this step incomplete.

## Manual observation

Tilt a temporary body and give its orbit a non-yaw orientation adjustment. Raise and lower the camera, then zoom both ways to the limits. Height must be perpendicular to the visible orbit plane.

Use temporary internal fixtures, not a product sample scene. At handoff, give exact setup/activation actions and expected observations for the final implementation; do not merely repeat this outline. Record whether the agent observed it and whether the user verified it. Do not claim a manual pass without performing or receiving confirmation of the check.

## Exit and handoff

Follow the [shared validation and completion rules](../ImplementationWorkflow.md). Report requirement IDs delivered, pending shared checks, changed files, public API integration, actual Edit/Play counts, Unity version, exact invocation or runner procedure, result/log paths, source revision/dirty state, manual instructions, limitations and next proposed step. Update this record and the [status tracker](../ImplementationStatus.md). Stop for review; do not begin another step.

## Validation record

- Implementation: Partial; orientation-axis defect
- Hard checkpoint: Not required unless escalated to Hard.
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

> Work on Step 04 only. Read Assets/Gley/VehicleCameraSystem/AGENTS.md, Docs/ImplementationWorkflow.md, Docs/ImplementationStatus.md and Docs/Steps/Step04-height-and-zoom.md in the package, plus the linked design and decisions. This is Easy and is intended for Terra High. Verify prerequisites, preserve existing uncommitted work, implement or repair only this step, run the required Edit Mode and Play Mode suites, provide the manual observation procedure, update the evidence/status, and stop for my review. Do not start the next step.
