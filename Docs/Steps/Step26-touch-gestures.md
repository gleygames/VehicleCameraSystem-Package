# Step 26 — Touch gestures

- **Difficulty:** Hard
- **Recommended agent:** Astra High (gpt-6-astra, high)
- **Why:** Multi-touch lifecycle, UI ownership, gesture cancellation, and equivalent commands.
- **Audit baseline:** Not implemented
- **Prerequisites:** [Step 25](./Step25-pc-input-companion.md)
- **Requirement IDs:** CORE-01, INPUT-01, INPUT-02, INPUT-03, INPUT-04. Shared IDs cover this step's slice only; see [coverage](../RequirementCoverage.md).

## Startup checkpoint

**MANDATORY HARD CHECKPOINT — STOP FIRST.** Read this task's guidance and difficulty, then say:

> This is a Hard task: Step 26 — Touch gestures. Recommended agent: Astra High. Please set the agent you want to use, then explicitly tell me to continue. I am paused before implementation and tests.

End the turn. Do not implement, run tests, inspect implementation code in depth or delegate until the user explicitly replies to proceed with this task. This is required even if Astra High appears selected. Plan approval, the starter prompt, silence and another task's approval do not release the checkpoint. Once released in this task, record it and proceed without repeated model-gate requests.

## Required reading and entry evidence

Read [workflow](../ImplementationWorkflow.md), [status](../ImplementationStatus.md), [master plan](../ImplementationPlan.md), [design](../VehicleCameraSystemDesign.md) sections 9, [decision log](../../DecisionLog.md), and [C# conventions](../CSharpConventions.md). Read the [dated audit](../ImplementationAudit-2026-09-21.md) for recovery context.

After any required checkpoint release, verify current source and prerequisites rather than assuming the audit is still current. Start at [CameraSystemController](../../Runtime/CameraSystemController.cs), [VehicleProfile](../../Runtime/VehicleProfile.cs), [CameraViewPreset](../../Runtime/CameraViewPreset.cs), the relevant Runtime/Editor/Input implementations, and the existing [Edit Mode](../../Tests/EditMode) and [Play Mode](../../Tests/PlayMode) suites. These are entry points, not a prescribed class design or a restriction against focused new files.

No completed implementation was established by the audit. Inspect for intervening changes before adding behavior. Preserve and build on accepted prerequisites.

**Product decisions:** Follow the decision log. No additional unresolved choice is predeclared for this slice; surface any consequential conflict before implementing dependent behavior.

## Original plan slice

Add normalized drag, pinch, double tap, UI gesture ownership, and configurable drag sensitivity.

Original Edit Mode check: Validate gesture thresholds, sensitivity, UI ownership, and equivalence to direct commands.

Original Play Mode check/observation: Simulate touch sequences, including a touch starting on UI and a double-tap reset.

## Scope and acceptance outcomes

- Implement normalized drag, pinch, double-tap reset and configurable sensitivity using the optional companion.
- A touch beginning on UI stays UI-owned until release; discard stale gestures across view switches and locks.
- Keep gesture thresholds configurable and player overrides local; issue only public device-independent commands.

Do not implement later steps, add excluded features or silently reduce a requirement. Runtime changes must not mutate shared profiles/presets or vehicle prefabs. Use explicit elapsed time, per-instance state and the public command contracts. A helper or configuration field without runtime/editor consumption does not meet acceptance.

## Edit Mode validation

- Simulate touch begin/move/end/cancel, multiple pointer IDs, pinch transitions, double-tap thresholds and different screen sizes.
- Test UI-origin touches moving off UI, world-origin gestures encountering UI, lock/view changes and fresh gesture/neutral rearming under the specified ownership policy.
- Compare normalized gesture output against known direct command traces and per-view/instance sensitivity.

Use independently defined expected outcomes. Add/update meaningful tests for the changed behavior and run the package Edit Mode suite after the final change.

## Play Mode validation

- Drive an actual Camera with simulated drag/pinch/double-tap and compare with equivalent API commands, including reset and lock behavior.
- Start a touch on UI and drag off: it must never become camera-owned before release. Switch or lock during a gesture: stale input must not resume.
- Run simultaneous instances with different overrides and verify no cross-instance effects.

Exercise the actual Camera through the public runtime API wherever this slice has camera behavior. Add/update meaningful Play Mode tests and run the package Play Mode suite after the final change. A failed or unavailable suite leaves this step incomplete.

## Manual observation

On a supported touch device or input simulation fixture, drag, pinch and double tap. Begin touches over UI then leave it; switch/lock mid-gesture and verify cancellation until a fresh valid gesture.

Use temporary internal fixtures, not a product sample scene. At handoff, give exact setup/activation actions and expected observations for the final implementation; do not merely repeat this outline. Record whether the agent observed it and whether the user verified it. Do not claim a manual pass without performing or receiving confirmation of the check.

## Exit and handoff

Follow the [shared validation and completion rules](../ImplementationWorkflow.md). Report requirement IDs delivered, pending shared checks, changed files, public API integration, actual Edit/Play counts, Unity version, exact invocation or runner procedure, result/log paths, source revision/dirty state, manual instructions, limitations and next proposed step. Update this record and the [status tracker](../ImplementationStatus.md). Stop for review; do not begin another step.

## Validation record

- Implementation: Not implemented
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

> Work on Step 26 only. Read Assets/Gley/VehicleCameraSystem/AGENTS.md, Docs/ImplementationWorkflow.md, Docs/ImplementationStatus.md and Docs/Steps/Step26-touch-gestures.md in the package, plus the linked design and decisions. This is Hard: announce the recommended Astra High agent and stop your turn before implementation, tests or an in-depth code audit. Wait for my explicit reply to continue, even if the model already appears correct. After that reply, Verify prerequisites, preserve existing uncommitted work, implement or repair only this step, run the required Edit Mode and Play Mode suites, provide the manual observation procedure, update the evidence/status, and stop for my review. Do not start the next step.
