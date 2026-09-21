# Step 19 — Collision

- **Difficulty:** Hard
- **Recommended agent:** Astra High (gpt-6-astra, high)
- **Why:** Physics queries, legal-pose search, body classification, and failure reporting.
- **Audit baseline:** Not implemented
- **Prerequisites:** [Step 18](./Step18-cushion-module.md)
- **Requirement IDs:** DATA-01, COL-01, COL-02, COL-03. Shared IDs cover this step's slice only; see [coverage](../RequirementCoverage.md).

## Startup checkpoint

**MANDATORY HARD CHECKPOINT — STOP FIRST.** Read this task's guidance and difficulty, then say:

> This is a Hard task: Step 19 — Collision. Recommended agent: Astra High. Please set the agent you want to use, then explicitly tell me to continue. I am paused before implementation and tests.

End the turn. Do not implement, run tests, inspect implementation code in depth or delegate until the user explicitly replies to proceed with this task. This is required even if Astra High appears selected. Plan approval, the starter prompt, silence and another task's approval do not release the checkpoint. Once released in this task, record it and proceed without repeated model-gate requests.

## Required reading and entry evidence

Read [workflow](../ImplementationWorkflow.md), [status](../ImplementationStatus.md), [master plan](../ImplementationPlan.md), [design](../VehicleCameraSystemDesign.md) sections 6, 8, 12, [decision log](../../DecisionLog.md), and [C# conventions](../CSharpConventions.md). Read the [dated audit](../ImplementationAudit-2026-09-21.md) for recovery context.

After any required checkpoint release, verify current source and prerequisites rather than assuming the audit is still current. Start at [CameraSystemController](../../Runtime/CameraSystemController.cs), [VehicleProfile](../../Runtime/VehicleProfile.cs), [CameraViewPreset](../../Runtime/CameraViewPreset.cs), the relevant Runtime/Editor/Input implementations, and the existing [Edit Mode](../../Tests/EditMode) and [Play Mode](../../Tests/PlayMode) suites. These are entry points, not a prescribed class design or a restriction against focused new files.

No completed implementation was established by the audit. Inspect for intervening changes before adding behavior. Preserve and build on accepted prerequisites.

**Unresolved decision:** Present legal-correction tie-break priorities and the proposed no-clear-pose result for user decision before implementing those public behaviors.

## Original plan slice

Add separate collision mask and legal correction with failure result, without a watch-target or road-visibility solver.

Original Edit Mode check: Validate mask, zoom/height limits, inward/outward/upward choices, and unchanged watch targets.

Original Play Mode check/observation: Place tunnel, own-body, and aim-point obstructions; observe legal correction and no-clear-pose result.

## Scope and acceptance outcomes

- Implement a separate collision mask and classify environmental versus own-body obstructions.
- Prefer inward correction for world obstruction; allow outward/upward correction for own body, always inside legal zoom/height bounds.
- Choose the least obstructed legal pose under the approved policy and report when no clear pose exists.
- Do not change authored watch targets, solve road visibility, pathfind transitions or escape bounds.

Do not implement later steps, add excluded features or silently reduce a requirement. Runtime changes must not mutate shared profiles/presets or vehicle prefabs. Use explicit elapsed time, per-instance state and the public command contracts. A helper or configuration field without runtime/editor consumption does not meet acceptance.

## Edit Mode validation

- Test legal candidate ranges, environment/own-body classification, collision mask independent of render mask, approved tie-break and explicit failure status.
- Verify collision correction preserves intended orbit/aim and player preferences instead of overwriting them.

Use independently defined expected outcomes. Add/update meaningful tests for the changed behavior and run the package Edit Mode suite after the final change.

## Play Mode validation

- Use temporary tunnel, own-body protrusion, ceiling and fully enclosed fixtures; verify inward/outward/upward choices where legal and reported failure when all legal poses are blocked.
- Place an obstruction only on the watch-target line; verify no unrequested visibility solver changes aim.
- Switch views smoothly and by snap near obstacles; verify no pathfinding and no illegal final correction.

Exercise the actual Camera through the public runtime API wherever this slice has camera behavior. Add/update meaningful Play Mode tests and run the package Play Mode suite after the final change. A failed or unavailable suite leaves this step incomplete.

## Manual observation

Move through a narrow tunnel, near an own-body protrusion and under a low ceiling. Display legal envelope and collision status, then obstruct only the watch point and compare smooth/snap switches.

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

> Work on Step 19 only. Read Assets/Gley/VehicleCameraSystem/AGENTS.md, Docs/ImplementationWorkflow.md, Docs/ImplementationStatus.md and Docs/Steps/Step19-collision.md in the package, plus the linked design and decisions. This is Hard: announce the recommended Astra High agent and stop your turn before implementation, tests or an in-depth code audit. Wait for my explicit reply to continue, even if the model already appears correct. After that reply, Verify prerequisites, preserve existing uncommitted work, implement or repair only this step, run the required Edit Mode and Play Mode suites, provide the manual observation procedure, update the evidence/status, and stop for my review. Do not start the next step.
