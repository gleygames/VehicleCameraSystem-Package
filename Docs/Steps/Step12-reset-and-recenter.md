# Step 12 — Reset and recenter

- **Difficulty:** Hard
- **Recommended agent:** Astra High (gpt-6-astra, high)
- **Why:** Persistent per-instance defaults, reset scopes, motion gating, and command policy.
- **Audit baseline:** Not implemented
- **Prerequisites:** [Step 11](./Step11-view-switching.md)
- **Requirement IDs:** DATA-01, VIEW-05, RESET-01. Shared IDs cover this step's slice only; see [coverage](../RequirementCoverage.md).

## Startup checkpoint

**MANDATORY HARD CHECKPOINT — STOP FIRST.** Read this task's guidance and difficulty, then say:

> This is a Hard task: Step 12 — Reset and recenter. Recommended agent: Astra High. Please set the agent you want to use, then explicitly tell me to continue. I am paused before implementation and tests.

End the turn. Do not implement, run tests, inspect implementation code in depth or delegate until the user explicitly replies to proceed with this task. This is required even if Astra High appears selected. Plan approval, the starter prompt, silence and another task's approval do not release the checkpoint. Once released in this task, record it and proceed without repeated model-gate requests.

## Required reading and entry evidence

Read [workflow](../ImplementationWorkflow.md), [status](../ImplementationStatus.md), [master plan](../ImplementationPlan.md), [design](../VehicleCameraSystemDesign.md) sections 6, 9, 12, [decision log](../../DecisionLog.md), and [C# conventions](../CSharpConventions.md). Read the [dated audit](../ImplementationAudit-2026-09-21.md) for recovery context.

After any required checkpoint release, verify current source and prerequisites rather than assuming the audit is still current. Start at [CameraSystemController](../../Runtime/CameraSystemController.cs), [VehicleProfile](../../Runtime/VehicleProfile.cs), [CameraViewPreset](../../Runtime/CameraViewPreset.cs), the relevant Runtime/Editor/Input implementations, and the existing [Edit Mode](../../Tests/EditMode) and [Play Mode](../../Tests/PlayMode) suites. These are entry points, not a prescribed class design or a restriction against focused new files.

No completed implementation was established by the audit. Inspect for intervening changes before adding behavior. Preserve and build on accepted prerequisites.

**Unresolved decision:** Ask whether the timed recenter countdown pauses or restarts while stationary. Record the answer before implementing that behavior.

## Original plan slice

Add front/rear player default poses with independent orbit position, zoom, and height; add orbit-only/full reset plus persistent and stationary-safe timed recenter modes.

Original Edit Mode check: Validate distinct defaults, reset scopes, and both recenter settings.

Original Play Mode check/observation: Compare reset levels and front/rear distances; verify persistent mode, then stop the body during the timed delay and observe no stationary reset.

## Scope and acceptance outcomes

- Expose distinct front/rear player defaults with independent orbit position, zoom and height using the default/remapping data established in Step 6.
- Implement orbit-only reset and full active-view reset, persistent framing and delayed recenter gated on actual motion.
- Integrate player lock and interruption policy; keep preferences instance-local and leave saving to the host.
- Define the motion-gating input needed here without requiring Rigidbody; Step 13 shares/extends motion-source behavior.

Do not implement later steps, add excluded features or silently reduce a requirement. Runtime changes must not mutate shared profiles/presets or vehicle prefabs. Use explicit elapsed time, per-instance state and the public command contracts. A helper or configuration field without runtime/editor consumption does not meet acceptance.

## Edit Mode validation

- Use deliberately different authored and player front/rear poses to verify selection and both reset scopes; orbit-only must preserve zoom/height.
- Test the approved stationary timer policy, persistent mode and invalid settings with deterministic elapsed time.
- Verify player reset commands respect lock while host resets work, and shared preset/profile assets remain unchanged.

Use independently defined expected outcomes. Add/update meaningful tests for the changed behavior and run the package Edit Mode suite after the final change.

## Play Mode validation

- Move orbit/zoom/height, compare both resets, and verify independent front/rear defaults through available public commands; reverse activation integration follows in Step 14.
- Stop the vehicle during the recenter delay and keep advancing time: no recenter while stationary. Resume motion and verify the approved timer behavior.
- Run two instances with different preferences and exercise reset during automatic movement.

Exercise the actual Camera through the public runtime API wherever this slice has camera behavior. Add/update meaningful Play Mode tests and run the package Play Mode suite after the final change. A failed or unavailable suite leaves this step incomplete.

## Manual observation

Set clearly different front/rear defaults. Compare orbit-only/full reset, enable timed recenter, stop during its delay, then move again. Repeat in persistent mode.

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

> Work on Step 12 only. Read Assets/Gley/VehicleCameraSystem/AGENTS.md, Docs/ImplementationWorkflow.md, Docs/ImplementationStatus.md and Docs/Steps/Step12-reset-and-recenter.md in the package, plus the linked design and decisions. This is Hard: announce the recommended Astra High agent and stop your turn before implementation, tests or an in-depth code audit. Wait for my explicit reply to continue, even if the model already appears correct. After that reply, Verify prerequisites, preserve existing uncommitted work, implement or repair only this step, run the required Edit Mode and Play Mode suites, provide the manual observation procedure, update the evidence/status, and stop for my review. Do not start the next step.
