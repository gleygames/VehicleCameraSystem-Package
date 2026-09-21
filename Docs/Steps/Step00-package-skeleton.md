# Step 00 — Package skeleton

- **Difficulty:** Easy
- **Recommended agent:** Terra High (gpt-5.6-terra, high)
- **Why:** Assembly boundaries and a small activation smoke test.
- **Audit baseline:** Code present; unverified
- **Prerequisites:** Existing user-configured project; no earlier implementation step.
- **Requirement IDs:** CORE-01. Shared IDs cover this step's slice only; see [coverage](../RequirementCoverage.md).

## Startup checkpoint

**Easy task — Terra High recommended.** The request to implement this step authorizes scoped work; no mandatory model-switch pause is required. If the work becomes Hard, announce that and stop for explicit continuation under the shared workflow before doing the Hard work.

## Required reading and entry evidence

Read [workflow](../ImplementationWorkflow.md), [status](../ImplementationStatus.md), [master plan](../ImplementationPlan.md), [design](../VehicleCameraSystemDesign.md) sections 1, 2, 10, [decision log](../../DecisionLog.md), and [C# conventions](../CSharpConventions.md). Read the [dated audit](../ImplementationAudit-2026-09-21.md) for recovery context.

After any required checkpoint release, verify current source and prerequisites rather than assuming the audit is still current. Start at [CameraSystemController](../../Runtime/CameraSystemController.cs), [VehicleProfile](../../Runtime/VehicleProfile.cs), [CameraViewPreset](../../Runtime/CameraViewPreset.cs), the relevant Runtime/Editor/Input implementations, and the existing [Edit Mode](../../Tests/EditMode) and [Play Mode](../../Tests/PlayMode) suites. These are entry points, not a prescribed class design or a restriction against focused new files.

Assemblies already exist. Verify and preserve them; do not recreate the project or reinstall packages to repeat completed setup.

**Product decisions:** Follow the decision log. No additional unresolved choice is predeclared for this slice; surface any consequential conflict before implementing dependent behavior.

## Original plan slice

Establish package and test assemblies inside the supplied project.

Original Edit Mode check: Verify assembly references and core isolation.

Original Play Mode check/observation: Activate and release a minimal assigned Camera.

## Scope and acceptance outcomes

- Verify Runtime, Editor, conditional Input companion, Edit Mode and Play Mode assemblies under the product folder.
- Prove core isolation with the Input System absent in this existing project. Do not implement input controls.

Do not implement later steps, add excluded features or silently reduce a requirement. Runtime changes must not mutate shared profiles/presets or vehicle prefabs. Use explicit elapsed time, per-instance state and the public command contracts. A helper or configuration field without runtime/editor consumption does not meet acceptance.

## Edit Mode validation

- Inspect assembly references and constraints: core has no Editor, Input System, controller, or render-pipeline dependency; test assemblies are correctly separated.
- Verify missing-camera activation reports a usable failure and leaves the instance inactive.

Use independently defined expected outcomes. Add/update meaningful tests for the changed behavior and run the package Edit Mode suite after the final change.

## Play Mode validation

- Activate and release an explicitly assigned Camera, then advance frames and verify the released controller no longer moves it.
- Confirm the core smoke test runs with the companion excluded and without a vehicle controller.

Exercise the actual Camera through the public runtime API wherever this slice has camera behavior. Add/update meaningful Play Mode tests and run the package Play Mode suite after the final change. A failed or unavailable suite leaves this step incomplete.

## Manual observation

Create temporary body, Camera, and controller objects. Assign the Camera explicitly, activate through a temporary fixture, then release and move the body. The released Camera must stay still.

Use temporary internal fixtures, not a product sample scene. At handoff, give exact setup/activation actions and expected observations for the final implementation; do not merely repeat this outline. Record whether the agent observed it and whether the user verified it. Do not claim a manual pass without performing or receiving confirmation of the check.

## Exit and handoff

Follow the [shared validation and completion rules](../ImplementationWorkflow.md). Report requirement IDs delivered, pending shared checks, changed files, public API integration, actual Edit/Play counts, Unity version, exact invocation or runner procedure, result/log paths, source revision/dirty state, manual instructions, limitations and next proposed step. Update this record and the [status tracker](../ImplementationStatus.md). Stop for review; do not begin another step.

## Validation record

- Implementation: Code present; unverified
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

> Work on Step 00 only. Read Assets/Gley/VehicleCameraSystem/AGENTS.md, Docs/ImplementationWorkflow.md, Docs/ImplementationStatus.md and Docs/Steps/Step00-package-skeleton.md in the package, plus the linked design and decisions. This is Easy and is intended for Terra High. Verify prerequisites, preserve existing uncommitted work, implement or repair only this step, run the required Edit Mode and Play Mode suites, provide the manual observation procedure, update the evidence/status, and stop for my review. Do not start the next step.
