# Decision Log

| Date | Step | Decision |
| --- | --- | --- |
| 2026-09-17 | 2B | Orbit movement preserves the current camera rotation until Step 3 adds authored watch-marker aiming. |
| 2026-09-18 | 3 | Watch-marker placement uses normalized orbit progress, so it retains its relative position when an orbit is reshaped. |
| 2026-09-18 | 4 | Positive zoom intent moves inward toward the vehicle; negative zoom intent moves outward. |
| 2026-09-18 | 5A | Front and rear removable sections use normalized orbit progress, retaining their relative placement when an orbit is reshaped. |
| 2026-09-18 | 5B | Vehicle profiles store explicit local front and rear connector anchors for stable straight-reference assembly. |
| 2026-09-18 | 5B | Generated connectors are straight cubic Bézier segments with one-third and two-thirds line controls. |
| 2026-09-18 | 6A | Newly authored orbits merge attached bodies by default; an orbit can explicitly remain root-only. |
| 2026-09-18 | 6C | A camera inside a removed front-rear section maps to the midpoint of the rear profile's authored rear removable section. |
| 2026-09-21 | 7 | For chain changes, preserve a camera position only when its exact body instance and retained source-orbit position survive. Map a removed section or replaced connector to the midpoint of the terminal body's rear removable section in the root component. |
| 2026-09-21 | Workflow | One fresh task per bounded implementation step; Easy uses Terra High and Hard uses Astra High. Preserve the master design/plan and use individual step files with validation and handoff records. |
| 2026-09-21 | Hard-task gate | On starting every Hard step/slice/task, announce its difficulty and recommended agent, stop, and wait for an explicit user reply to continue before implementation, tests or an in-depth code audit. The gate applies even when Astra High appears selected; this plan approval and earlier approvals do not release future gates. |
