# Vehicle Camera System — task workflow

Approved workflow: 2026-09-21. This applies to implementation/repair tasks, not to the documentation refactor that established this workflow.

## Authority and task boundaries

- The user's latest explicit decisions take precedence. Product behavior comes from [VehicleCameraSystemDesign.md](VehicleCameraSystemDesign.md), as amended by [DecisionLog.md](../DecisionLog.md).
- [ImplementationPlan.md](ImplementationPlan.md) retains the requirement IDs, delivery scope and original step definitions. [ImplementationStatus.md](ImplementationStatus.md) records progress. The files in [Steps](Steps) turn each numbered step into an executable handoff.
- Shared rules live here and in [CSharpConventions.md](CSharpConventions.md). Step-specific rules may add detail but must not silently weaken a requirement or change an approved decision.
- Work only in the user-provided Unity project. Product code/docs live in the VehicleCameraSystem package repository at Assets/Gley/VehicleCameraSystem. The outer Unity project and the package are separate Git repositories; inspect the correct working trees.
- One fresh task per authorized step or approved slice, sequentially. Fix review feedback in that task until accepted. Do not start the next step automatically, create other tasks, or delegate work without explicit user authorization.
- A fresh task receives a step-file link or its starter prompt. It must read that file, this workflow, the status tracker, the requirement map and the relevant design/decision material rather than relying on an old conversation.

## Mandatory Hard-task checkpoint

This is an explicit user requirement, not an optional recommendation.

1. At task start, read only the guidance, selected step file and status needed to identify difficulty and scope.
2. If the selected step is **Hard**, immediately tell the user:

   > This is a Hard task: Step NN — TITLE. Recommended agent: Astra High. Please set the agent you want to use, then explicitly tell me to continue. I am paused before implementation and tests.

3. End the turn and wait. Before the reply, do not inspect implementation code in depth, make edits, run tests, launch implementation tools, or delegate. A request to “start Step NN” is not the required continuation.
4. Continue only after an explicit user reply to proceed with this named task. “Continue”, “proceed”, or equivalent unambiguous authorization in response to this checkpoint is sufficient. Do not infer permission from elapsed time, an apparent model change, or a missing reply.
5. The checkpoint is required even if the agent appears to already be Astra High. Do not silently switch the model or claim its setting is verified when it is not.
6. Approval of this workflow, approval of a prior step, or continuation in another task does not release this checkpoint. Each new Hard step/slice/task has its own checkpoint.
7. Once the user explicitly releases this checkpoint in the current task, record that fact in the step handoff and do not repeatedly ask for it during the same task. Separate unresolved product decisions still need their own answers.
8. If an Easy task reveals Hard work, explain the new difficulty and stop at the same checkpoint before that work. Do not downgrade a Hard step to bypass the checkpoint.

**Easy tasks:** recommended agent Terra High. The user's request to implement that Easy step authorizes its scoped work; there is no mandatory model-switch checkpoint.

## After the checkpoint, or at the start of an Easy step

1. Verify the project path, Unity version, package/outer working trees, source files and available test runner. The audited project uses Unity 2022.3.62f3 with Test Framework 1.1.33; read current settings rather than assuming those versions persist.
2. Preserve existing uncommitted work. Record baseline commits and relevant dirty files. Do not reset, discard, commit, or move another agent's changes without authorization.
3. Confirm prerequisites and read their validation records. If required behavior is broken or evidence is missing, report the exact issue. Do not silently bundle a different unfinished step into this task.
4. Recovery exception: Step 02 is the next task. Steps 00/01 have code but no fresh acceptance evidence; run their existing relevant smoke tests as entry checks. If those fail and need separate work, report the dependency before expanding scope. After Step 02, revalidate Steps 03–05 in their own tasks, then finish 06–07.
5. Give a brief implementation approach and proceed within authorized scope. The step request plus the Hard checkpoint release where applicable supplies permission for its files. Do not ask for repeated per-file approval.
6. Ask only unresolved consequential product questions named by the plan or exposed by conflicting requirements. Record answers before implementing dependent behavior.
7. If a step is too large, propose named slices such as 06A/06B with requirements, integration points and validation for each. The user must authorize the selected slice. A Hard slice has the Hard checkpoint. Parent completion still requires all its checks and cross-slice integration.
8. Each behavior must reach the public camera API and actual Camera where applicable. New types, enum values, settings, helpers and helper tests are not sufficient by themselves.

## Validation required for every implementation step

- Add/update at least one meaningful Unity Edit Mode test and one meaningful Unity Play Mode test. Reuse existing tests when they already cover the contract; fix gaps instead of duplicating formulas.
- Test independently observable outcomes. Analytic geometry, independently constructed expected poses, invariants and public-command traces are appropriate. Calling the production resolver to compute the expected result does not independently verify that resolver.
- Run both package suites after the final change. Include the relevant prerequisite regressions and all new tests. Report actual passed/failed/skipped counts, Unity version, exact invocation or Editor Test Runner procedure, and result/log locations.
- Store raw test outputs outside Assets, for example Temp/VehicleCameraSystemTests/StepNN/. In the step validation record, summarize results and identify the source revision/working-tree state, date and command. Do not rely on an ignored XML path as the only durable evidence.
- Use supported tooling for the project's installed Unity version. Discover the available runner rather than assuming Unity 6 Pipeline commands work in Unity 2022.3.
- If the project is already open in Unity, use an available compatible live Test Runner path. Do not close the user's Editor or launch a competing batch process against the same open project. If neither live nor batch execution is available, report the concrete blocker and leave automated validation pending.
- For a closed project, the installed Unity Editor supports batch Test Runner execution using -batchmode -projectPath, -runTests, -testPlatform EditMode or PlayMode, -testResults and -logFile. Run platforms sequentially; do not add -quit because it can terminate before results are written. Resolve the installed executable first. Use hidden windows for background helpers.
- Missing dependencies must be reported before changing project-wide configuration. Do not create another Unity project to obtain a passing test result.
- A managed math harness, static review or external compilation is useful diagnostic evidence but is not a Unity Edit Mode or Play Mode suite pass.
- Provide a repeatable manual observation procedure with temporary fixtures, exact actions and visible expected behavior. Manual checks supplement automated checks, especially for smoothness, framing, input feel and editor UX.
- Keep product sample scenes/user documentation out of scope. Internal fixtures may be retained for review if clearly separated, disposable and not required by the runtime.
- Review the diff for unintended dependencies, changes to shared authoring assets/prefabs, excluded features and unrelated edits.
- If any required test cannot run, fails, or is skipped without an accepted reason, the step remains incomplete. Never label “tests exist” as “tests passed”.

## Shared requirements and later integration

[RequirementCoverage.md](RequirementCoverage.md) maps every original requirement to its owner steps. A step can finish its own declared slice while a multi-step requirement remains pending later integration. Its handoff must name those later checks.

Examples: Step 08 establishes exact-body disconnection, Step 09 verifies POI disconnection during travel; Step 10 establishes command policy, Steps 11/12/14 verify it for view/reset/reverse commands. Step 06 owns authored-default-relative remapping now, Step 12 exposes player defaults/reset, and Step 21 rechecks transfer with the complete preference system. Do not use later ownership to omit behavior already required in the current slice.

At the final owner step for a shared requirement, verify the entire outcome. If earlier evidence is missing, report that rather than claiming full coverage.

## Status and completion

Keep implementation status separate from acceptance:

- Implementation: Not started / Code present; unverified / Partial / In progress / Implemented.
- Automated validation: Not run / Blocked / Failed / Passed.
- Manual validation: Pending / Agent observed (not user acceptance) / User verified / Failed.
- Acceptance: Not recorded / Awaiting review / Accepted / Reopened.

Only explicit user acceptance establishes Accepted. Passing tests alone moves the step to Awaiting review. A detected regression reopens the affected step/requirement; preserve prior evidence with a dated note instead of deleting it. Do not infer historical acceptance from Git history.

Record results in the selected step's Validation record section and summarize them in the status tracker. Record implementation decisions in DecisionLog.md. When a later step fulfills shared checks, update the earlier handoff/coverage record as well.

The final handoff must state: scope delivered; requirement IDs and pending cross-step checks; changed files; public API integration evidence; Edit/Play counts and version; result/log locations; manual procedure/outcome; unresolved decisions/blockers; and the proposed next step. Stop for user review without starting it.
