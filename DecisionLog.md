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
