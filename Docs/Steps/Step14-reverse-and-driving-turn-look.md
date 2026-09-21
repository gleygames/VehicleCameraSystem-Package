# Step 14 — Reverse and Driving turn look

- **Difficulty:** Hard
- **Recommended agent:** Astra High (gpt-6-astra, high)
- **Why:** Reverse command state, default-pose transitions, turn signals, and interruptions.
- **Audit baseline:** Not implemented
- **Prerequisites:** [Step 13](./Step13-driving-follow.md)
- **Requirement IDs:** DATA-01, CMD-02, VIEW-01, VIEW-02, VIEW-05, MOVE-04. Shared IDs cover this step's slice only; see [coverage](../RequirementCoverage.md).

## Startup checkpoint

**MANDATORY HARD CHECKPOINT — STOP FIRST.** Read this task's guidance and difficulty, then say:

> This is a Hard task: Step 14 — Reverse and Driving turn look. Recommended agent: Astra High. Please set the agent you want to use, then explicitly tell me to continue. I am paused before implementation and tests.

End the turn. Do not implement, run tests, inspect implementation code in depth or delegate until the user explicitly replies to proceed with this task. This is required even if Astra High appears selected. Plan approval, the starter prompt, silence and another task's approval do not release the checkpoint. Once released in this task, record it and proceed without repeated model-gate requests.

## Required reading and entry evidence

Read [workflow](../ImplementationWorkflow.md), [status](../ImplementationStatus.md), [master plan](../ImplementationPlan.md), [design](../VehicleCameraSystemDesign.md) sections 5, 6, 9, 12, [decision log](../../DecisionLog.md), and [C# conventions](../CSharpConventions.md). Read the [dated audit](../ImplementationAudit-2026-09-21.md) for recovery context.

After any required checkpoint release, verify current source and prerequisites rather than assuming the audit is still current. Start at [CameraSystemController](../../Runtime/CameraSystemController.cs), [VehicleProfile](../../Runtime/VehicleProfile.cs), [CameraViewPreset](../../Runtime/CameraViewPreset.cs), the relevant Runtime/Editor/Input implementations, and the existing [Edit Mode](../../Tests/EditMode) and [Play Mode](../../Tests/PlayMode) suites. These are entry points, not a prescribed class design or a restriction against focused new files.

No completed implementation was established by the audit. Inspect for intervening changes before adding behavior. Preserve and build on accepted prerequisites.

**Unresolved decision:** Ask whether clearing reverse returns smoothly to the saved rear default or preserves the player's current orbit pose. Record the answer before implementing reverse exit.

## Original plan slice

Add explicit reverse request and gentle Driving turn look; disable speed distance in reverse by default. Resolve the reverse-clear behavior before implementation.

Original Edit Mode check: Validate reverse/view rules, default/optional reverse distance, direction-hint settings, excluded views, and manual interruption.

Original Play Mode check/observation: Reverse from Driving, then from other views; observe only Driving turn to its front default and test the agreed reverse-clear behavior.

## Scope and acceptance outcomes

- Implement host-triggered reverse only in Driving, traveling smoothly to the independent player front default with its zoom and height.
- Disable speed-distance adaptation in reverse by default and allow explicit configuration to enable it.
- Add gentle Driving turn look from angular motion with optional direction hint; no automatic reverse inference.
- Apply the approved reverse-exit behavior and common manual-interruption/player-lock policy.

Do not implement later steps, add excluded features or silently reduce a requirement. Runtime changes must not mutate shared profiles/presets or vehicle prefabs. Use explicit elapsed time, per-instance state and the public command contracts. A helper or configuration field without runtime/editor consumption does not meet acceptance.

## Edit Mode validation

- Test reverse requests for every available view, distinct front/rear defaults, default/optional reverse speed-distance and the approved exit policy.
- Test angular motion and direction hints, independent turn configuration, and no turn assistance in Fixed/Presentation.
- Complete CMD-02 integration checks across POI, smooth view and reverse travel.

Use independently defined expected outcomes. Add/update meaningful tests for the changed behavior and run the package Edit Mode suite after the final change.

## Play Mode validation

- Reverse a Driving Camera and verify smooth travel to the exact saved front pose, forward-to-rear road framing and optional speed adaptation.
- Repeat reverse requests in Fixed, Interior when available, and Presentation; verify no automatic view change. Step 15 completes Interior coverage.
- Manually interrupt reverse, then test lock and host commands; exercise reverse exit and turning with/without a supplied hint.

Exercise the actual Camera through the public runtime API wherever this slice has camera behavior. Add/update meaningful Play Mode tests and run the package Play Mode suite after the final change. A failed or unavailable suite leaves this step incomplete.

## Manual observation

Use widely separated front/rear defaults. Toggle reverse while driving, interrupt its travel, compare the reverse speed option, then repeat in other available views and test turn anticipation.

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

> Work on Step 14 only. Read Assets/Gley/VehicleCameraSystem/AGENTS.md, Docs/ImplementationWorkflow.md, Docs/ImplementationStatus.md and Docs/Steps/Step14-reverse-and-driving-turn-look.md in the package, plus the linked design and decisions. This is Hard: announce the recommended Astra High agent and stop your turn before implementation, tests or an in-depth code audit. Wait for my explicit reply to continue, even if the model already appears correct. After that reply, Verify prerequisites, preserve existing uncommitted work, implement or repair only this step, run the required Edit Mode and Play Mode suites, provide the manual observation procedure, update the evidence/status, and stop for my review. Do not start the next step.
