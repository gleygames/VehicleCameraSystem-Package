# Step 25 — PC input companion

- **Difficulty:** Easy
- **Recommended agent:** Terra High (gpt-5.6-terra, high)
- **Why:** Device-to-public-command mapping after the runtime contracts are settled.
- **Audit baseline:** Not implemented; conditional assembly only
- **Prerequisites:** [Step 24](./Step24-connected-preview-and-validation.md)
- **Requirement IDs:** CORE-01, INPUT-01, INPUT-02, INPUT-03, INPUT-04. Shared IDs cover this step's slice only; see [coverage](../RequirementCoverage.md).

## Startup checkpoint

**Easy task — Terra High recommended.** The request to implement this step authorizes scoped work; no mandatory model-switch pause is required. If the work becomes Hard, announce that and stop for explicit continuation under the shared workflow before doing the Hard work.

## Required reading and entry evidence

Read [workflow](../ImplementationWorkflow.md), [status](../ImplementationStatus.md), [master plan](../ImplementationPlan.md), [design](../VehicleCameraSystemDesign.md) sections 1, 9, 10, [decision log](../../DecisionLog.md), and [C# conventions](../CSharpConventions.md). Read the [dated audit](../ImplementationAudit-2026-09-21.md) for recovery context.

After any required checkpoint release, verify current source and prerequisites rather than assuming the audit is still current. Start at [CameraSystemController](../../Runtime/CameraSystemController.cs), [VehicleProfile](../../Runtime/VehicleProfile.cs), [CameraViewPreset](../../Runtime/CameraViewPreset.cs), the relevant Runtime/Editor/Input implementations, and the existing [Edit Mode](../../Tests/EditMode) and [Play Mode](../../Tests/PlayMode) suites. These are entry points, not a prescribed class design or a restriction against focused new files.

The current project has no Input System package. Report that exact prerequisite before any project-wide dependency change and obtain authorization for installation if needed; do not silently install it.

**Product decisions:** Follow the decision log. No additional unresolved choice is predeclared for this slice; surface any consequential conflict before implementing dependent behavior.

## Original plan slice

Add optional Input System PC mappings through public commands and honor per-view movement rates.

Original Edit Mode check: Validate actions, bindings, assembly isolation, and equivalence to direct commands.

Original Play Mode check/observation: Simulate PC input, then remove the companion and check the core still runs.

## Scope and acceptance outcomes

- Implement optional Input System PC mappings through public camera commands only, with configurable bindings, dead zones and appropriate UI blocking.
- Honor per-view movement rates and instance-local player overrides; preserve player lock, interruption and neutral-rearm behavior.
- Prove runtime compilation and use without the companion/Input System. Do not create a second project or remove user dependencies to test this.

Do not implement later steps, add excluded features or silently reduce a requirement. Runtime changes must not mutate shared profiles/presets or vehicle prefabs. Use explicit elapsed time, per-instance state and the public command contracts. A helper or configuration field without runtime/editor consumption does not meet acceptance.

## Edit Mode validation

- Use input simulation to test bindings, dead zones, held intent/drag mapping and equivalent direct-command traces.
- Check assembly references/conditional compilation and isolation without Input System; specify a reversible verification procedure in the existing project.
- Verify per-view settings and player overrides remain independent for two instances.

Use independently defined expected outcomes. Add/update meaningful tests for the changed behavior and run the package Edit Mode suite after the final change.

## Play Mode validation

- Simulate PC inputs against an actual Camera and compare poses/results with equivalent public API commands.
- Hold input through view switches and locks, then neutral/repress; verify discarded stale movement and public-command policy.
- Run the core smoke test with the companion excluded and record how Input System absence was actually verified.

Exercise the actual Camera through the public runtime API wherever this slice has camera behavior. Add/update meaningful Play Mode tests and run the package Play Mode suite after the final change. A failed or unavailable suite leaves this step incomplete.

## Manual observation

Use keyboard/mouse mappings on temporary views, switch while holding movement, lock/unlock and interact with UI. Compare equivalent fixture buttons that issue direct core commands.

Use temporary internal fixtures, not a product sample scene. At handoff, give exact setup/activation actions and expected observations for the final implementation; do not merely repeat this outline. Record whether the agent observed it and whether the user verified it. Do not claim a manual pass without performing or receiving confirmation of the check.

## Exit and handoff

Follow the [shared validation and completion rules](../ImplementationWorkflow.md). Report requirement IDs delivered, pending shared checks, changed files, public API integration, actual Edit/Play counts, Unity version, exact invocation or runner procedure, result/log paths, source revision/dirty state, manual instructions, limitations and next proposed step. Update this record and the [status tracker](../ImplementationStatus.md). Stop for review; do not begin another step.

## Validation record

- Implementation: Not implemented; conditional assembly only
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

> Work on Step 25 only. Read Assets/Gley/VehicleCameraSystem/AGENTS.md, Docs/ImplementationWorkflow.md, Docs/ImplementationStatus.md and Docs/Steps/Step25-pc-input-companion.md in the package, plus the linked design and decisions. This is Easy and is intended for Terra High. Verify prerequisites, preserve existing uncommitted work, implement or repair only this step, run the required Edit Mode and Play Mode suites, provide the manual observation procedure, update the evidence/status, and stop for my review. Do not start the next step.
