# Step 09 — Point-of-interest travel

- **Difficulty:** Hard
- **Recommended agent:** Astra High (gpt-6-astra, high)
- **Why:** Route selection, timed eased motion, target lifecycle, and exact stopping.
- **Audit baseline:** Not implemented
- **Prerequisites:** [Step 08](./Step08-body-bound-aim.md)
- **Requirement IDs:** DATA-01, AIM-02, AIM-04, AIM-05, VIEW-04. Shared IDs cover this step's slice only; see [coverage](../RequirementCoverage.md).

## Startup checkpoint

**MANDATORY HARD CHECKPOINT — STOP FIRST.** Read this task's guidance and difficulty, then say:

> This is a Hard task: Step 09 — Point-of-interest travel. Recommended agent: Astra High. Please set the agent you want to use, then explicitly tell me to continue. I am paused before implementation and tests.

End the turn. Do not implement, run tests, inspect implementation code in depth or delegate until the user explicitly replies to proceed with this task. This is required even if Astra High appears selected. Plan approval, the starter prompt, silence and another task's approval do not release the checkpoint. Once released in this task, record it and proceed without repeated model-gate requests.

## Required reading and entry evidence

Read [workflow](../ImplementationWorkflow.md), [status](../ImplementationStatus.md), [master plan](../ImplementationPlan.md), [design](../VehicleCameraSystemDesign.md) sections 4, 6, 9, 12, [decision log](../../DecisionLog.md), and [C# conventions](../CSharpConventions.md). Read the [dated audit](../ImplementationAudit-2026-09-21.md) for recovery context.

After any required checkpoint release, verify current source and prerequisites rather than assuming the audit is still current. Start at [CameraSystemController](../../Runtime/CameraSystemController.cs), [VehicleProfile](../../Runtime/VehicleProfile.cs), [CameraViewPreset](../../Runtime/CameraViewPreset.cs), the relevant Runtime/Editor/Input implementations, and the existing [Edit Mode](../../Tests/EditMode) and [Play Mode](../../Tests/PlayMode) suites. These are entry points, not a prescribed class design or a restriction against focused new files.

No completed implementation was established by the audit. Inspect for intervening changes before adding behavior. Preserve and build on accepted prerequisites.

**Unresolved decision:** Before next-point behavior is implemented, ask whether the final authored point wraps or reports no next point. Record the user's answer; do not choose silently.

## Original plan slice

Add named/next destinations, shortest/directed route, preset speed or per-call speed/duration, precise stop, and command results.

Original Edit Mode check: Validate route, destination selection, and each terminal result.

Original Play Mode check/observation: Request wheel → number plate → engine and next, including destination replacement and an unreachable target.

## Scope and acceptance outcomes

- Expose named and next point-of-interest requests through the public API without building a presentation sequence.
- Support shortest valid and explicitly directed routes, preset speed, per-call speed and fixed duration, eased start/end and exact stop.
- Report completion, replacement, interruption, target disconnection and unreachable destination exactly once per operation. Step 10 connects generic manual arbitration.

Do not implement later steps, add excluded features or silently reduce a requirement. Runtime changes must not mutate shared profiles/presets or vehicle prefabs. Use explicit elapsed time, per-instance state and the public command contracts. A helper or configuration field without runtime/editor consumption does not meet acceptance.

## Edit Mode validation

- Test arbitrary named order, duplicate/missing names, next ordering and the approved final-point policy; verify shortest/directed routes and invalid timing arguments.
- Test lifecycle transitions and replacement starting from the current pose rather than the previous origin; establish a documented interruption route for commands.
- Add integration coverage for Step 8 target disconnection during travel.

Use independently defined expected outcomes. Add/update meaningful tests for the changed behavior and run the package Edit Mode suite after the final change.

## Play Mode validation

- Request wheel, plate, engine, then next using public commands; assert exact final positions and observable terminal results.
- Sample travel at several delta times with default/overridden speed and fixed duration, including replacement midway, explicit interruption, disconnection and unreachable targets.
- Verify eased start/end and bounded traversal across all sampler/connector boundaries.

Exercise the actual Camera through the public runtime API wherever this slice has camera behavior. Add/update meaningful Play Mode tests and run the package Play Mode suite after the final change. A failed or unavailable suite leaves this step incomplete.

## Manual observation

Use temporary labeled POIs. Request them out of order, replace a request during travel, request next at the last point, and detach a selected target. Display command results alongside the camera.

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

> Work on Step 09 only. Read Assets/Gley/VehicleCameraSystem/AGENTS.md, Docs/ImplementationWorkflow.md, Docs/ImplementationStatus.md and Docs/Steps/Step09-point-of-interest-travel.md in the package, plus the linked design and decisions. This is Hard: announce the recommended Astra High agent and stop your turn before implementation, tests or an in-depth code audit. Wait for my explicit reply to continue, even if the model already appears correct. After that reply, Verify prerequisites, preserve existing uncommitted work, implement or repair only this step, run the required Edit Mode and Play Mode suites, provide the manual observation procedure, update the evidence/status, and stop for my review. Do not start the next step.
