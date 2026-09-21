# Step 07 — Longer chains

- **Difficulty:** Hard
- **Recommended agent:** Astra High (gpt-6-astra, high)
- **Why:** Arbitrary linear chains, instance identity, pair overrides, and runtime remapping.
- **Audit baseline:** Partial; helpers are not integrated into the camera
- **Prerequisites:** [Step 06](./Step06-attach-and-remap.md)
- **Requirement IDs:** DATA-01, ORBIT-03. Shared IDs cover this step's slice only; see [coverage](../RequirementCoverage.md).

## Startup checkpoint

**MANDATORY HARD CHECKPOINT — STOP FIRST.** Read this task's guidance and difficulty, then say:

> This is a Hard task: Step 07 — Longer chains. Recommended agent: Astra High. Please set the agent you want to use, then explicitly tell me to continue. I am paused before implementation and tests.

End the turn. Do not implement, run tests, inspect implementation code in depth or delegate until the user explicitly replies to proceed with this task. This is required even if Astra High appears selected. Plan approval, the starter prompt, silence and another task's approval do not release the checkpoint. Once released in this task, record it and proceed without repeated model-gate requests.

## Required reading and entry evidence

Read [workflow](../ImplementationWorkflow.md), [status](../ImplementationStatus.md), [master plan](../ImplementationPlan.md), [design](../VehicleCameraSystemDesign.md) sections 2, 3, 6, [decision log](../../DecisionLog.md), and [C# conventions](../CSharpConventions.md). Read the [dated audit](../ImplementationAudit-2026-09-21.md) for recovery context.

After any required checkpoint release, verify current source and prerequisites rather than assuming the audit is still current. Start at [CameraSystemController](../../Runtime/CameraSystemController.cs), [VehicleProfile](../../Runtime/VehicleProfile.cs), [CameraViewPreset](../../Runtime/CameraViewPreset.cs), the relevant Runtime/Editor/Input implementations, and the existing [Edit Mode](../../Tests/EditMode) and [Play Mode](../../Tests/PlayMode) suites. These are entry points, not a prescribed class design or a restriction against focused new files.

ThreeBodyClosedBezierOrbit and pair-override helpers exist, including uncommitted work. CameraSystemController still permits only one rear attachment and accepts no pair override. The internal list composer alone does not satisfy the public chain contract.

**Product decisions:** Follow the decision log. No additional unresolved choice is predeclared for this slice; surface any consequential conflict before implementing dependent behavior.

## Original plan slice

Extend composition to linear chains and ordered pair connector overrides.

Original Edit Mode check: Validate several body counts and interchangeable profile pairs.

Original Play Mode check/observation: Connect three bodies and observe the runtime curve and one overridden connector.

## Scope and acceptance outcomes

- Connect linear chains through the public camera attachment API, including three and more than three body instances.
- Resolve or explicitly accept ordered profile-pair connector overrides during actual attachment. Validate pairs/endpoints and preserve generated connectors as default.
- Implement the recorded Step 7 mapping decision: preserve exact body instance plus surviving source location; otherwise map to the terminal rear midpoint in the root component.
- Keep branch attachments outside scope and preserve root-only behavior.

Do not implement later steps, add excluded features or silently reduce a requirement. Runtime changes must not mutate shared profiles/presets or vehicle prefabs. Use explicit elapsed time, per-instance state and the public command contracts. A helper or configuration field without runtime/editor consumption does not meet acceptance.

## Edit Mode validation

- Test two-, three- and four-body chains, repeat instances of one profile, several interchangeable profile pairs, wrong-order overrides, wrong endpoints and invalid topology.
- Test chain split/removal and connector replacement mapping using exact body identity rather than profile identity.
- Use a valid curved override with an independently measured length; confirm remapping uses its actual length.

Use independently defined expected outcomes. Add/update meaningful tests for the changed behavior and run the package Edit Mode suite after the final change.

## Play Mode validation

- Use public commands to attach three bodies, apply an override, append a fourth, then split/detach while the Camera is on retained, removed and connector sections.
- Verify actual camera traversal uses the override, the root component remains selected, smooth remaps complete, and shared assets remain unchanged.
- Repeat with a second camera instance and duplicate profile instances to detect state or identity leakage.

Exercise the actual Camera through the public runtime API wherever this slice has camera behavior. Add/update meaningful Play Mode tests and run the package Play Mode suite after the final change. A failed or unavailable suite leaves this step incomplete.

## Manual observation

Attach a three-body temporary chain through the fixture, highlight one curved override, then append a fourth body. Orbit the camera and detach at several positions, observing retained poses and smooth fallback.

Use temporary internal fixtures, not a product sample scene. At handoff, give exact setup/activation actions and expected observations for the final implementation; do not merely repeat this outline. Record whether the agent observed it and whether the user verified it. Do not claim a manual pass without performing or receiving confirmation of the check.

## Exit and handoff

Follow the [shared validation and completion rules](../ImplementationWorkflow.md). Report requirement IDs delivered, pending shared checks, changed files, public API integration, actual Edit/Play counts, Unity version, exact invocation or runner procedure, result/log paths, source revision/dirty state, manual instructions, limitations and next proposed step. Update this record and the [status tracker](../ImplementationStatus.md). Stop for review; do not begin another step.

## Validation record

- Implementation: Partial; helpers are not integrated into the camera
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

> Work on Step 07 only. Read Assets/Gley/VehicleCameraSystem/AGENTS.md, Docs/ImplementationWorkflow.md, Docs/ImplementationStatus.md and Docs/Steps/Step07-longer-chains.md in the package, plus the linked design and decisions. This is Hard: announce the recommended Astra High agent and stop your turn before implementation, tests or an in-depth code audit. Wait for my explicit reply to continue, even if the model already appears correct. After that reply, Verify prerequisites, preserve existing uncommitted work, implement or repair only this step, run the required Edit Mode and Play Mode suites, provide the manual observation procedure, update the evidence/status, and stop for my review. Do not start the next step.
