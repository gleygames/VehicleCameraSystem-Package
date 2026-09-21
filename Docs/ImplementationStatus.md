# Vehicle Camera System — implementation status

Updated 2026-09-21 during the approved plan refactor. These assessments come from the [audit](ImplementationAudit-2026-09-21.md), not fresh Unity suite runs. Existing code is not automatically accepted. All task agents must follow the [workflow](ImplementationWorkflow.md).

## Start here

**Next task: [Step 02 — Single-body orbit](Steps/Step02-single-body-orbit.md), Hard, Astra High.** Its startup checkpoint must be shown and explicitly released before implementation/tests. Run existing Step 00/01 smoke checks as its recovery prerequisites; report any failures before expanding scope.

Recovery order: 02, then revalidate 03, 04, 05, then complete 06 and 07. Continue with 08–27 sequentially after review. Steps 00/01 remain independently available if their entry checks reveal missing behavior; no historical acceptance is inferred.

## Step tracker

| Step / plan | Difficulty | Agent | Implementation baseline | Automated validation | User acceptance |
| --- | --- | --- | --- | --- | --- |
| [00 — Package skeleton](Steps/Step00-package-skeleton.md) | Easy | Terra High | Code present; unverified | Not freshly run | Not recorded |
| [01 — Fixed view](Steps/Step01-fixed-view.md) | Easy | Terra High | Code present; unverified | Not freshly run | Not recorded |
| [02 — Single-body orbit](Steps/Step02-single-body-orbit.md) | Hard | Astra High | Partial; confirmed sampler defect | Not freshly run | Not recorded |
| [03 — Watch-marker aim](Steps/Step03-watch-marker-aim.md) | Easy | Terra High | Code present; dependent revalidation needed | Not freshly run | Not recorded |
| [04 — Height and zoom](Steps/Step04-height-and-zoom.md) | Easy | Terra High | Partial; orientation-axis defect | Not freshly run | Not recorded |
| [05 — Two-body geometry](Steps/Step05-two-body-geometry.md) | Hard | Astra High | Partial; composed geometry needs revalidation | Not freshly run | Not recorded |
| [06 — Attach and remap](Steps/Step06-attach-and-remap.md) | Hard | Astra High | Partial; smooth remap and default-relative transfer missing | Not freshly run | Not recorded |
| [07 — Longer chains](Steps/Step07-longer-chains.md) | Hard | Astra High | Partial; helpers are not integrated into the camera | Not freshly run | Not recorded |
| [08 — Body-bound aim](Steps/Step08-body-bound-aim.md) | Hard | Astra High | Not implemented; straight-orbit prerequisite exists | Not freshly run | Not recorded |
| [09 — Point-of-interest travel](Steps/Step09-point-of-interest-travel.md) | Hard | Astra High | Not implemented | Not freshly run | Not recorded |
| [10 — Manual arbitration](Steps/Step10-manual-arbitration.md) | Hard | Astra High | Not implemented | Not freshly run | Not recorded |
| [11 — View switching](Steps/Step11-view-switching.md) | Hard | Astra High | Not implemented; current selection releases the camera | Not freshly run | Not recorded |
| [12 — Reset and recenter](Steps/Step12-reset-and-recenter.md) | Hard | Astra High | Not implemented | Not freshly run | Not recorded |
| [13 — Driving follow](Steps/Step13-driving-follow.md) | Hard | Astra High | Not implemented | Not freshly run | Not recorded |
| [14 — Reverse and Driving turn look](Steps/Step14-reverse-and-driving-turn-look.md) | Hard | Astra High | Not implemented | Not freshly run | Not recorded |
| [15 — Interior head and turn look](Steps/Step15-interior-head-and-turn-look.md) | Hard | Astra High | Not implemented | Not freshly run | Not recorded |
| [16 — Acceleration/braking module](Steps/Step16-acceleration-and-braking.md) | Hard | Astra High | Not implemented | Not freshly run | Not recorded |
| [17 — Cornering module](Steps/Step17-cornering-module.md) | Easy | Terra High | Not implemented | Not freshly run | Not recorded |
| [18 — Cushion module](Steps/Step18-cushion-module.md) | Hard | Astra High | Not implemented | Not freshly run | Not recorded |
| [19 — Collision](Steps/Step19-collision.md) | Hard | Astra High | Not implemented | Not freshly run | Not recorded |
| [20 — Rendering and time](Steps/Step20-rendering-and-time.md) | Easy | Terra High | Partial; unscaled ticking only | Not freshly run | Not recorded |
| [21 — Target and teleport](Steps/Step21-target-and-teleport.md) | Hard | Astra High | Not implemented as specified; basic assignment exists | Not freshly run | Not recorded |
| [22 — Guided profile creation](Steps/Step22-guided-profile-creation.md) | Hard | Astra High | Not implemented | Not freshly run | Not recorded |
| [23 — Scene editing handles](Steps/Step23-scene-editing-handles.md) | Hard | Astra High | Not implemented | Not freshly run | Not recorded |
| [24 — Connected preview and validation](Steps/Step24-connected-preview-and-validation.md) | Hard | Astra High | Not implemented | Not freshly run | Not recorded |
| [25 — PC input companion](Steps/Step25-pc-input-companion.md) | Easy | Terra High | Not implemented; conditional assembly only | Not freshly run | Not recorded |
| [26 — Touch gestures](Steps/Step26-touch-gestures.md) | Hard | Astra High | Not implemented | Not freshly run | Not recorded |
| [27 — Touch buttons](Steps/Step27-touch-buttons.md) | Easy | Terra High | Not implemented | Not freshly run | Not recorded |

## Updating this tracker

- Use the status vocabulary in the workflow and put the detailed evidence in the step file's Validation record.
- Record the date, exact tested revision/working-tree state and actual result counts. Source-test inventory counts are not passing results.
- Record automated completion as Awaiting review until the user explicitly accepts it. A new user request for another task is not automatically evidence of acceptance unless it clearly says so.
- Keep manual validation and test blockers visible in the step record. Reopen a step when a confirmed regression invalidates its prior checks.
- Update [RequirementCoverage.md](RequirementCoverage.md) when an entire shared outcome has passed all owner checks. A slice passing does not complete a multi-step requirement.
- Difficulty is based on the current planned scope. An agent encountering Hard work in an Easy step must announce the change and wait at the mandatory Hard checkpoint.
- If the user authorizes a split, add slice files linked from the parent and record their prerequisites/status here. Preserve the original 0–27 numbering; parent acceptance requires every slice and its integration.

## Evidence baseline

Unity project version at audit: 2022.3.62f3. Test source inventory: 32 Edit Mode and 18 Play Mode test methods. No fresh suite pass, historical manual acceptance or completed release verification is claimed.

The package working tree already contains uncommitted geometry/tests/decision changes and new connector-override files. The plan refactor does not authorize discarding them.
