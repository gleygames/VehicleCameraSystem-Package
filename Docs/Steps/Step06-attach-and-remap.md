# Step 06 — Attach and remap

- **Difficulty:** Hard
- **Recommended agent:** Astra High (gpt-6-astra, high)
- **Why:** State-preserving rebuilds and interruptible motion across topology changes.
- **Audit baseline:** Partial; smooth remap and default-relative transfer missing
- **Prerequisites:** [Step 04](./Step04-height-and-zoom.md), [Step 05](./Step05-two-body-geometry.md)
- **Requirement IDs:** DATA-01, ORBIT-05, ORBIT-06, STATE-02. Shared IDs cover this step's slice only; see [coverage](../RequirementCoverage.md).

## Startup checkpoint

**MANDATORY HARD CHECKPOINT — STOP FIRST.** Read this task's guidance and difficulty, then say:

> This is a Hard task: Step 06 — Attach and remap. Recommended agent: Astra High. Please set the agent you want to use, then explicitly tell me to continue. I am paused before implementation and tests.

End the turn. Do not implement, run tests, inspect implementation code in depth or delegate until the user explicitly replies to proceed with this task. This is required even if Astra High appears selected. Plan approval, the starter prompt, silence and another task's approval do not release the checkpoint. Once released in this task, record it and proceed without repeated model-gate requests.

## Required reading and entry evidence

Read [workflow](../ImplementationWorkflow.md), [status](../ImplementationStatus.md), [master plan](../ImplementationPlan.md), [design](../VehicleCameraSystemDesign.md) sections 2, 3, 6, 9, [decision log](../../DecisionLog.md), and [C# conventions](../CSharpConventions.md). Read the [dated audit](../ImplementationAudit-2026-09-21.md) for recovery context.

After any required checkpoint release, verify current source and prerequisites rather than assuming the audit is still current. Start at [CameraSystemController](../../Runtime/CameraSystemController.cs), [VehicleProfile](../../Runtime/VehicleProfile.cs), [CameraViewPreset](../../Runtime/CameraViewPreset.cs), the relevant Runtime/Editor/Input implementations, and the existing [Edit Mode](../../Tests/EditMode) and [Play Mode](../../Tests/PlayMode) suites. These are entry points, not a prescribed class design or a restriction against focused new files.

Attach/detach currently assigns remapped distance and immediately applies the pose. Retained-position mapping and a rear fallback exist; smooth removed-section travel and authored-default-relative transfer do not.

**Product decisions:** Follow the decision log. No additional unresolved choice is predeclared for this slice; surface any consequential conflict before implementing dependent behavior.

## Original plan slice

Add explicit attach/detach, retained-section behavior, removed-section remapping, root-only orbit option, and relative zoom/height state transfer.

Original Edit Mode check: Validate section identity, destination mapping, and clamped offsets.

Original Play Mode check/observation: Attach from front and rear positions; observe stay/remap, carried adjustments, and unchanged root-only orbit.

## Scope and acceptance outcomes

- Complete explicit pair attach/detach using the public controller, preserving retained sections and smoothly moving removed sections to the decided rear midpoint.
- Keep root-only orbits unchanged and make failed rebuilds leave the existing active state intact.
- Introduce the authored-default and instance-adjustment data required for relative zoom/height transfer now. Do not postpone this acceptance item to Step 12; Step 12 adds player default/reset commands.
- Preserve/map separate front/rear pose data through rebuilds; Step 21 verifies this again with the complete player preference system.

Do not implement later steps, add excluded features or silently reduce a requirement. Runtime changes must not mutate shared profiles/presets or vehicle prefabs. Use explicit elapsed time, per-instance state and the public command contracts. A helper or configuration field without runtime/editor consumption does not meet acceptance.

## Edit Mode validation

- Verify exact source-section identity mapping, curved retained positions, endpoint cases, removed rear midpoint and detach fallback.
- Use different before/after authored zoom and height defaults; carry player deltas relative to those defaults and assert clamping to destination ranges.
- Verify invalid attach/detach leaves camera state and assets unchanged and root-only attachment requires no usable trailer orbit.

Use independently defined expected outcomes. Add/update meaningful tests for the changed behavior and run the package Edit Mode suite after the final change.

## Play Mode validation

- Attach while on the retained front and assert unchanged pose. Attach from the removed rear and sample intermediate frames: no immediate destination snap, continuous motion, exact final position.
- Detach while on both retained and removed portions; assert expected root-component mapping and smooth fallback.
- Repeat with nonzero player zoom/height deltas, differing authored defaults, range clamping, root-only mode and two independent cameras.

Exercise the actual Camera through the public runtime API wherever this slice has camera behavior. Add/update meaningful Play Mode tests and run the package Play Mode suite after the final change. A failed or unavailable suite leaves this step incomplete.

## Manual observation

Pause at retained front, attach, and observe no movement. Repeat at removed rear and observe gradual travel to the combined rear. Adjust zoom/height before attaching and detaching; verify carried relative adjustments.

Use temporary internal fixtures, not a product sample scene. At handoff, give exact setup/activation actions and expected observations for the final implementation; do not merely repeat this outline. Record whether the agent observed it and whether the user verified it. Do not claim a manual pass without performing or receiving confirmation of the check.

## Exit and handoff

Follow the [shared validation and completion rules](../ImplementationWorkflow.md). Report requirement IDs delivered, pending shared checks, changed files, public API integration, actual Edit/Play counts, Unity version, exact invocation or runner procedure, result/log paths, source revision/dirty state, manual instructions, limitations and next proposed step. Update this record and the [status tracker](../ImplementationStatus.md). Stop for review; do not begin another step.

## Validation record

- Implementation: Partial; smooth remap and default-relative transfer missing
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

> Work on Step 06 only. Read Assets/Gley/VehicleCameraSystem/AGENTS.md, Docs/ImplementationWorkflow.md, Docs/ImplementationStatus.md and Docs/Steps/Step06-attach-and-remap.md in the package, plus the linked design and decisions. This is Hard: announce the recommended Astra High agent and stop your turn before implementation, tests or an in-depth code audit. Wait for my explicit reply to continue, even if the model already appears correct. After that reply, Verify prerequisites, preserve existing uncommitted work, implement or repair only this step, run the required Edit Mode and Play Mode suites, provide the manual observation procedure, update the evidence/status, and stop for my review. Do not start the next step.
