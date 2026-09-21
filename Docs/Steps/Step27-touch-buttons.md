# Step 27 — Touch buttons

- **Difficulty:** Easy
- **Recommended agent:** Terra High (gpt-5.6-terra, high)
- **Why:** Replaceable uGUI controls routed through existing companion commands.
- **Audit baseline:** Not implemented
- **Prerequisites:** [Step 26](./Step26-touch-gestures.md)
- **Requirement IDs:** CORE-01, INPUT-01. Shared IDs cover this step's slice only; see [coverage](../RequirementCoverage.md).

## Startup checkpoint

**Easy task — Terra High recommended.** The request to implement this step authorizes scoped work; no mandatory model-switch pause is required. If the work becomes Hard, announce that and stop for explicit continuation under the shared workflow before doing the Hard work.

## Required reading and entry evidence

Read [workflow](../ImplementationWorkflow.md), [status](../ImplementationStatus.md), [master plan](../ImplementationPlan.md), [design](../VehicleCameraSystemDesign.md) sections 9, 10, [decision log](../../DecisionLog.md), and [C# conventions](../CSharpConventions.md). Read the [dated audit](../ImplementationAudit-2026-09-21.md) for recovery context.

After any required checkpoint release, verify current source and prerequisites rather than assuming the audit is still current. Start at [CameraSystemController](../../Runtime/CameraSystemController.cs), [VehicleProfile](../../Runtime/VehicleProfile.cs), [CameraViewPreset](../../Runtime/CameraViewPreset.cs), the relevant Runtime/Editor/Input implementations, and the existing [Edit Mode](../../Tests/EditMode) and [Play Mode](../../Tests/PlayMode) suites. These are entry points, not a prescribed class design or a restriction against focused new files.

No completed implementation was established by the audit. Inspect for intervening changes before adding behavior. Preserve and build on accepted prerequisites.

**Product decisions:** Follow the decision log. No additional unresolved choice is predeclared for this slice; surface any consequential conflict before implementing dependent behavior.

## Original plan slice

Add simple replaceable uGUI buttons for camera actions.

Original Edit Mode check: Validate button references and action wiring.

Original Play Mode check/observation: Press each button and observe its corresponding camera command.

## Scope and acceptance outcomes

- Provide simple replaceable uGUI camera-action controls using the public player command route.
- Support required movement/view/reset controls with configurable assignments and no dependency from core to uGUI/Input System.
- Honor lock, interruption and neutral rearming and integrate button ownership with touch gestures.

Do not implement later steps, add excluded features or silently reduce a requirement. Runtime changes must not mutate shared profiles/presets or vehicle prefabs. Use explicit elapsed time, per-instance state and the public command contracts. A helper or configuration field without runtime/editor consumption does not meet acceptance.

## Edit Mode validation

- Validate missing references and action wiring for each supplied control; verify press/release/cancel/disable clears held intent.
- Ensure button commands use the same policy as PC/touch and do not bypass player lock.
- Inspect optional-assembly boundaries and verify no runtime-core dependency on UI.

Use independently defined expected outcomes. Add/update meaningful tests for the changed behavior and run the package Edit Mode suite after the final change.

## Play Mode validation

- Press every configured button on an actual Canvas and verify the corresponding Camera behavior/result.
- Hold a movement button across a view switch/lock, release it, then press again; verify neutral rearming and no stuck motion after disable/cancel.
- Touch a button while gesture handling runs and verify the same touch cannot also orbit/pinch the camera.

Exercise the actual Camera through the public runtime API wherever this slice has camera behavior. Add/update meaningful Play Mode tests and run the package Play Mode suite after the final change. A failed or unavailable suite leaves this step incomplete.

## Manual observation

Build a temporary Canvas using the supplied replaceable controls. Exercise every action, hold while switching/locking, disable a held control and verify no stuck movement or duplicate touch gesture.

Use temporary internal fixtures, not a product sample scene. At handoff, give exact setup/activation actions and expected observations for the final implementation; do not merely repeat this outline. Record whether the agent observed it and whether the user verified it. Do not claim a manual pass without performing or receiving confirmation of the check.

## Exit and handoff

Follow the [shared validation and completion rules](../ImplementationWorkflow.md). Report requirement IDs delivered, pending shared checks, changed files, public API integration, actual Edit/Play counts, Unity version, exact invocation or runner procedure, result/log paths, source revision/dirty state, manual instructions, limitations and next proposed step. Update this record and the [status tracker](../ImplementationStatus.md). Stop for review; do not begin another step.

## Validation record

- Implementation: Not implemented
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

> Work on Step 27 only. Read Assets/Gley/VehicleCameraSystem/AGENTS.md, Docs/ImplementationWorkflow.md, Docs/ImplementationStatus.md and Docs/Steps/Step27-touch-buttons.md in the package, plus the linked design and decisions. This is Easy and is intended for Terra High. Verify prerequisites, preserve existing uncommitted work, implement or repair only this step, run the required Edit Mode and Play Mode suites, provide the manual observation procedure, update the evidence/status, and stop for my review. Do not start the next step.
