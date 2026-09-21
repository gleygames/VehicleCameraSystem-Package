# Step 01 — Fixed view

- **Difficulty:** Easy
- **Recommended agent:** Terra High (gpt-5.6-terra, high)
- **Why:** Bounded body-relative pose and ownership behavior.
- **Audit baseline:** Code present; unverified
- **Prerequisites:** [Step 00](./Step00-package-skeleton.md)
- **Requirement IDs:** CORE-02, DATA-01, DATA-02, VIEW-01. Shared IDs cover this step's slice only; see [coverage](../RequirementCoverage.md).

## Startup checkpoint

**Easy task — Terra High recommended.** The request to implement this step authorizes scoped work; no mandatory model-switch pause is required. If the work becomes Hard, announce that and stop for explicit continuation under the shared workflow before doing the Hard work.

## Required reading and entry evidence

Read [workflow](../ImplementationWorkflow.md), [status](../ImplementationStatus.md), [master plan](../ImplementationPlan.md), [design](../VehicleCameraSystemDesign.md) sections 1, 2, 5, [decision log](../../DecisionLog.md), and [C# conventions](../CSharpConventions.md). Read the [dated audit](../ImplementationAudit-2026-09-21.md) for recovery context.

After any required checkpoint release, verify current source and prerequisites rather than assuming the audit is still current. Start at [CameraSystemController](../../Runtime/CameraSystemController.cs), [VehicleProfile](../../Runtime/VehicleProfile.cs), [CameraViewPreset](../../Runtime/CameraViewPreset.cs), the relevant Runtime/Editor/Input implementations, and the existing [Edit Mode](../../Tests/EditMode) and [Play Mode](../../Tests/PlayMode) suites. These are entry points, not a prescribed class design or a restriction against focused new files.

Fixed pose, activation/release and independent instances exist. Revalidate them; do not replace functioning code without a failing contract.

**Product decisions:** Follow the decision log. No additional unresolved choice is predeclared for this slice; surface any consequential conflict before implementing dependent behavior.

## Original plan slice

Add profile/preset basics, explicit camera ownership, and Fixed view.

Original Edit Mode check: Validate required references and unchanged profile assets.

Original Play Mode check/observation: Move two bodies with separate camera instances; verify independent fixed poses and no movement from player camera input.

## Scope and acceptance outcomes

- Support reusable Fixed presets and vehicle-specific position/watch defaults through Inspector references and runtime assignment.
- Keep Fixed views immune to generic player movement and keep shared assets unchanged.

Do not implement later steps, add excluded features or silently reduce a requirement. Runtime changes must not mutate shared profiles/presets or vehicle prefabs. Use explicit elapsed time, per-instance state and the public command contracts. A helper or configuration field without runtime/editor consumption does not meet acceptance.

## Edit Mode validation

- Check missing references and coincident camera/watch positions produce explicit activation failures.
- Serialize both profiles and their shared preset before/after activation, movement commands and release; assert they are unchanged.

Use independently defined expected outcomes. Add/update meaningful tests for the changed behavior and run the package Edit Mode suite after the final change.

## Play Mode validation

- Use two different body instances and two Cameras sharing a preset. Move and rotate each body independently; verify exact expected local-to-world poses and no cross-instance state changes.
- Send horizontal, vertical and zoom intent to Fixed; verify pose remains authored. Release one instance and verify the other continues.

Exercise the actual Camera through the public runtime API wherever this slice has camera behavior. Add/update meaningful Play Mode tests and run the package Play Mode suite after the final change. A failed or unavailable suite leaves this step incomplete.

## Manual observation

Move and rotate two temporary vehicle cubes while their Fixed cameras run. Send player movement through the fixture, then release just one controller. Observe unchanged fixed framing and independence.

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

> Work on Step 01 only. Read Assets/Gley/VehicleCameraSystem/AGENTS.md, Docs/ImplementationWorkflow.md, Docs/ImplementationStatus.md and Docs/Steps/Step01-fixed-view.md in the package, plus the linked design and decisions. This is Easy and is intended for Terra High. Verify prerequisites, preserve existing uncommitted work, implement or repair only this step, run the required Edit Mode and Play Mode suites, provide the manual observation procedure, update the evidence/status, and stop for my review. Do not start the next step.
