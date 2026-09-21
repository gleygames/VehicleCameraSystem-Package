# Step 13 — Driving follow

- **Difficulty:** Hard
- **Recommended agent:** Astra High (gpt-6-astra, high)
- **Why:** Independent smoothing, actual-speed sources, lag limits, and view-specific framing.
- **Audit baseline:** Not implemented
- **Prerequisites:** [Step 12](./Step12-reset-and-recenter.md)
- **Requirement IDs:** DATA-01, VIEW-01, VIEW-07, VIEW-08, MOVE-03, MOVE-04, MOVE-05. Shared IDs cover this step's slice only; see [coverage](../RequirementCoverage.md).

## Startup checkpoint

**MANDATORY HARD CHECKPOINT — STOP FIRST.** Read this task's guidance and difficulty, then say:

> This is a Hard task: Step 13 — Driving follow. Recommended agent: Astra High. Please set the agent you want to use, then explicitly tell me to continue. I am paused before implementation and tests.

End the turn. Do not implement, run tests, inspect implementation code in depth or delegate until the user explicitly replies to proceed with this task. This is required even if Astra High appears selected. Plan approval, the starter prompt, silence and another task's approval do not release the checkpoint. Once released in this task, record it and proceed without repeated model-gate requests.

## Required reading and entry evidence

Read [workflow](../ImplementationWorkflow.md), [status](../ImplementationStatus.md), [master plan](../ImplementationPlan.md), [design](../VehicleCameraSystemDesign.md) sections 4, 5, 6, [decision log](../../DecisionLog.md), and [C# conventions](../CSharpConventions.md). Read the [dated audit](../ImplementationAudit-2026-09-21.md) for recovery context.

After any required checkpoint release, verify current source and prerequisites rather than assuming the audit is still current. Start at [CameraSystemController](../../Runtime/CameraSystemController.cs), [VehicleProfile](../../Runtime/VehicleProfile.cs), [CameraViewPreset](../../Runtime/CameraViewPreset.cs), the relevant Runtime/Editor/Input implementations, and the existing [Edit Mode](../../Tests/EditMode) and [Play Mode](../../Tests/PlayMode) suites. These are entry points, not a prescribed class design or a restriction against focused new files.

No completed implementation was established by the audit. Inspect for intervening changes before adding behavior. Preserve and build on accepted prerequisites.

**Product decisions:** Follow the decision log. No additional unresolved choice is predeclared for this slice; surface any consequential conflict before implementing dependent behavior.

## Original plan slice

Add Driving/Presentation behavior, independent follow/aim smoothing, lag cap, actual-speed distance, and their distinct road/detail aim tracks.

Original Edit Mode check: Validate speed thresholds, rear/side distance response, authored aim markers, and independent settings.

Original Play Mode check/observation: Supply speed, use Rigidbody speed, then estimate from Transform; observe low-speed closeness, high-speed distance, and road/wheel framing without field-of-view change.

## Scope and acceptance outcomes

- Implement Driving follow position lag, independent aim smoothing, maximum lag/catch-up and physical speed-distance response.
- Prefer host-supplied speed, then Rigidbody velocity, then Transform estimate; require no Rigidbody.
- Keep stronger rear distance adaptation and smaller nonzero side adaptation, with no speed-driven FOV or aim change.
- Use distinct authored Driving and Presentation tracks, including road-forward rear/side framing and detail views; preserve default full-circle/restricted behavior.

Do not implement later steps, add excluded features or silently reduce a requirement. Runtime changes must not mutate shared profiles/presets or vehicle prefabs. Use explicit elapsed time, per-instance state and the public command contracts. A helper or configuration field without runtime/editor consumption does not meet acceptance.

## Edit Mode validation

- Test speed thresholds, zero/high speed, source priority and fallback, rear/side distance influence and independent smoothing controls.
- Test expected authored rear/side/front targets for Driving versus Presentation, legal distance ranges, lag bounds and explicit time inputs.
- Verify Fixed/Presentation do not gain unrequested Driving assistance. Turn look is added in Step 14.

Use independently defined expected outcomes. Add/update meaningful tests for the changed behavior and run the package Edit Mode suite after the final change.

## Play Mode validation

- Drive actual Cameras using supplied speed, Rigidbody speed and Transform-only motion; confirm source priority, speed-distance behavior and unchanged FOV/aim target.
- Move/turn the body abruptly; verify position lag is bounded, catch-up works and changing aim smoothing does not silently change follow lag.
- Exercise full-circle and restricted Driving/Presentation views and inspect rear/side/front framing from actual authored markers.

Exercise the actual Camera through the public runtime API wherever this slice has camera behavior. Add/update meaningful Play Mode tests and run the package Play Mode suite after the final change. A failed or unavailable suite leaves this step incomplete.

## Manual observation

Run a temporary vehicle along a repeatable path from rest to speed and through turns. Compare rear and side distance response, alter follow and aim smoothing separately, and switch to Presentation.

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

> Work on Step 13 only. Read Assets/Gley/VehicleCameraSystem/AGENTS.md, Docs/ImplementationWorkflow.md, Docs/ImplementationStatus.md and Docs/Steps/Step13-driving-follow.md in the package, plus the linked design and decisions. This is Hard: announce the recommended Astra High agent and stop your turn before implementation, tests or an in-depth code audit. Wait for my explicit reply to continue, even if the model already appears correct. After that reply, Verify prerequisites, preserve existing uncommitted work, implement or repair only this step, run the required Edit Mode and Play Mode suites, provide the manual observation procedure, update the evidence/status, and stop for my review. Do not start the next step.
