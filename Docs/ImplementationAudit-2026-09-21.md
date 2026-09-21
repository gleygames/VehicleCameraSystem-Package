# Implementation audit — 2026-09-21

This is a dated baseline, not a replacement for fresh task verification. The audit reviewed the design, original plan, decision log, working-tree runtime and tests, including uncommitted Step 7 work. No Unity suite was rerun and no user acceptance was inferred.

## Confirmed findings

1. **Step 2 sampler defect.** [ClosedBezierOrbit](../Runtime/ClosedBezierOrbit.cs) interpolates a previous segment's t=1 with a new segment's first sampled t, then evaluates the new segment. The equivalent code is present in [LinearOrbitComposer](../Runtime/LinearOrbitComposer.cs). An isolated managed execution compiled current geometry source against Unity's math library: on the existing 20-by-10 rectangular fixture, distance 20.01 returned approximately (20,0,9.69), while the analytic expected position is (20,0,0.01). This diagnostic is not a Unity suite pass.
2. **Step 4 orientation-axis defect.** [CameraSystemController](../Runtime/CameraSystemController.cs) uses vehicleBody.up for height while the curve uses OrientationAdjustment. An adjustment that changes the orbit plane's up direction makes height non-perpendicular to that plane.
3. **Step 6 incomplete behavior.** Attach/detach applies remapped distance and pose immediately. Retained-position and fallback helpers exist, but smooth removed-section movement and authored-default-relative zoom/height transfer are missing.
4. **Step 7 incomplete integration.** Three-body and pair-override helpers exist, but the controller only stores one rear body, rejects another and constructs a two-body orbit without accepting an override.
5. **Missing schema and later systems.** VehicleProfile holds one orbit and fixed/orbit preset references. Named orbits, full view defaults, body-instance aim binding, automatic commands, Driving/Interior behavior, seat motion, collision, editor authoring and input implementations are absent.

## Existing foundation

Steps 0/1 have assembly, ownership and Fixed-view code. Step 3 has marker interpolation. Steps 2/4/5/6/7 have partial implementations. Step 20 contains unscaled ticking only. Step 21 contains basic target assignment only.

The source inventory had 32 Edit Mode tests and 18 Play Mode tests. These are counts of declared tests, not pass counts. Several integration expectations use production helpers, and the basic sampling test checks distances 15 and 25 instead of the failing boundary interval.

The outer project uses Unity 2022.3.62f3. The package is a Git submodule with tracked modifications and new untracked connector-override files. Preserve those changes. No current test report or historical manual acceptance was established by the audit.

## Recovery

Start with [Step 02](Steps/Step02-single-body-orbit.md), including existing core/Fixed smoke checks. Revalidate 03–05, complete 06–07, then proceed through the remaining plan. Reopen earlier steps if evidence warrants it; do not restart functioning foundations wholesale.
