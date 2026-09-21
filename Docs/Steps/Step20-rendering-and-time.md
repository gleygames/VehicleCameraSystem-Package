# Step 20 — Rendering and time

- **Difficulty:** Easy
- **Recommended agent:** Terra High (gpt-5.6-terra, high)
- **Why:** Explicit Camera property baselines and per-view clock selection.
- **Audit baseline:** Partial; unscaled ticking only
- **Prerequisites:** [Step 19](./Step19-collision.md)
- **Requirement IDs:** CORE-03, DATA-01, TIME-01, RENDER-01, RENDER-02. Shared IDs cover this step's slice only; see [coverage](../RequirementCoverage.md).

## Startup checkpoint

**Easy task — Terra High recommended.** The request to implement this step authorizes scoped work; no mandatory model-switch pause is required. If the work becomes Hard, announce that and stop for explicit continuation under the shared workflow before doing the Hard work.

## Required reading and entry evidence

Read [workflow](../ImplementationWorkflow.md), [status](../ImplementationStatus.md), [master plan](../ImplementationPlan.md), [design](../VehicleCameraSystemDesign.md) sections 2, 6, [decision log](../../DecisionLog.md), and [C# conventions](../CSharpConventions.md). Read the [dated audit](../ImplementationAudit-2026-09-21.md) for recovery context.

After any required checkpoint release, verify current source and prerequisites rather than assuming the audit is still current. Start at [CameraSystemController](../../Runtime/CameraSystemController.cs), [VehicleProfile](../../Runtime/VehicleProfile.cs), [CameraViewPreset](../../Runtime/CameraViewPreset.cs), the relevant Runtime/Editor/Input implementations, and the existing [Edit Mode](../../Tests/EditMode) and [Play Mode](../../Tests/PlayMode) suites. These are entry points, not a prescribed class design or a restriction against focused new files.

No completed implementation was established by the audit. Inspect for intervening changes before adding behavior. Preserve and build on accepted prerequisites.

**Product decisions:** Follow the decision log. No additional unresolved choice is predeclared for this slice; surface any consequential conflict before implementing dependent behavior.

## Original plan slice

Add optional Camera mask/clip presets, baseline restore, and per-view time-source selection without requiring special project layers.

Original Edit Mode check: Validate preset modes and time settings.

Original Play Mode check/observation: Cycle views and release; vary time scale and verify original Camera settings return.

## Scope and acceptance outcomes

- Add opt-in culling mask replace/add/remove and near/far clip overrides derived from a captured Camera baseline.
- Restore only properties overridden by this instance on release; never accumulate view masks or modify FOV.
- Use unscaled time by default and a per-view scaled option, with elapsed time passed into camera logic.
- Do not add render-pipeline-specific properties or required project layers.

Do not implement later steps, add excluded features or silently reduce a requirement. Runtime changes must not mutate shared profiles/presets or vehicle prefabs. Use explicit elapsed time, per-instance state and the public command contracts. A helper or configuration field without runtime/editor consumption does not meet acceptance.

## Edit Mode validation

- Test mask modes against an ordinary-layer baseline, valid clip ranges and per-property override tracking.
- Test view A/B cycling and release restore semantics, including a property the instance never overrides.
- Verify default/unscaled and scaled selection with independent timing inputs.

Use independently defined expected outcomes. Add/update meaningful tests for the changed behavior and run the package Edit Mode suite after the final change.

## Play Mode validation

- Cycle rendering presets repeatedly on a real Camera and release; verify exact original mask/clips return as applicable and FOV never changes.
- Set time scale to zero and a fractional value; unscaled travel must continue at real-time rate while scaled travel follows game time.
- Repeat with two Cameras having different baselines and verify no shared state.

Exercise the actual Camera through the public runtime API wherever this slice has camera behavior. Add/update meaningful Play Mode tests and run the package Play Mode suite after the final change. A failed or unavailable suite leaves this step incomplete.

## Manual observation

Record a temporary Camera's mask, clips and FOV. Cycle presets, release, and compare values. Pause game time and compare scaled versus unscaled views.

Use temporary internal fixtures, not a product sample scene. At handoff, give exact setup/activation actions and expected observations for the final implementation; do not merely repeat this outline. Record whether the agent observed it and whether the user verified it. Do not claim a manual pass without performing or receiving confirmation of the check.

## Exit and handoff

Follow the [shared validation and completion rules](../ImplementationWorkflow.md). Report requirement IDs delivered, pending shared checks, changed files, public API integration, actual Edit/Play counts, Unity version, exact invocation or runner procedure, result/log paths, source revision/dirty state, manual instructions, limitations and next proposed step. Update this record and the [status tracker](../ImplementationStatus.md). Stop for review; do not begin another step.

## Validation record

- Implementation: Partial; unscaled ticking only
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

> Work on Step 20 only. Read Assets/Gley/VehicleCameraSystem/AGENTS.md, Docs/ImplementationWorkflow.md, Docs/ImplementationStatus.md and Docs/Steps/Step20-rendering-and-time.md in the package, plus the linked design and decisions. This is Easy and is intended for Terra High. Verify prerequisites, preserve existing uncommitted work, implement or repair only this step, run the required Edit Mode and Play Mode suites, provide the manual observation procedure, update the evidence/status, and stop for my review. Do not start the next step.
