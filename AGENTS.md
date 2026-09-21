# Vehicle Camera System agent instructions

This package is a separate Git repository inside the user-provided Unity project. The current work area is Assets/Gley/VehicleCameraSystem. Read [Docs/ImplementationWorkflow.md](Docs/ImplementationWorkflow.md) and the explicitly requested [step file](Docs/ImplementationStatus.md) before implementation.

## Mandatory startup gate

For every Hard implementation/repair step or slice, first announce the step is Hard, recommend Astra High, and **stop the turn** so the user can set the agent. Continue only after the user explicitly replies to proceed with that task. Do not implement, run tests, perform an in-depth code audit or delegate before that reply. This applies even when Astra High appears already selected. Approval of the plan or an earlier task does not release the gate.

Easy steps recommend Terra High and have no model-switch gate. If Easy work proves Hard, stop at the same gate before the expanded work. See the workflow for exact wording and continuation rules.

## Required guidance

- Product behavior: [design](Docs/VehicleCameraSystemDesign.md), [plan](Docs/ImplementationPlan.md), [decision log](DecisionLog.md).
- Current evidence and next task: [status](Docs/ImplementationStatus.md), [audit](Docs/ImplementationAudit-2026-09-21.md), [requirement coverage](Docs/RequirementCoverage.md).
- C# rules: [full conventions](Docs/CSharpConventions.md), plus the user's csharp-code-conventions skill when available. Read before C# edits/design. No comments, ternaries, LINQ/lambdas, public fields, or new static classes/methods; always braces; explicit deltaTime and the prescribed member ordering.
- Work only on the requested step. After its gate is released, the step request authorizes its files; present a brief approach and do not request repeated per-file approvals.
- Preserve existing dirty files. No unrelated rewrites, new Unity projects, game-specific dependencies, automatic follow-on steps, or unrequested agents/tasks.
- Each implementation step needs meaningful Edit Mode and Play Mode tests, both run, plus a repeatable manual observation procedure. Public Camera behavior must be verified; helpers alone do not count.
- Missing/failed checks leave the step incomplete. Only explicit user acceptance may mark it Accepted. Update its validation record/status and stop for review.

These instructions apply to implementation tasks. The documentation-only plan refactor establishing them is not itself a numbered Hard implementation task.
