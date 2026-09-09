# Current full PlayMode regression

Runtime/test baseline 8ebaae2. Unity 2022.3.62f3 ran the entire current PlayMode
suite with graphics enabled, using the production project and no test filter.
Process 26036 exited. Artifacts/core-current-full-regression.xml reports
474 total, 474 passed, zero failed, 189.2218009 seconds.

This supersedes the historical 469-case full result for current suite coverage.
Recent integrated cases include two naturally collected Bonus sessions in the
same scene, Bonus twelve-card automatic return and failed extra-card ad exit,
Free intro/return scene clicks, all four Free ball reward ad failure/retry
paths, and original-APK font atlas raw-byte validation. Existing scene/core,
save/load, presentation, source-parameter and earlier controller tests also ran.

The log's Already Start error is deliberately exercised by
RecoveredBaseReelMotionTests; the final suite result is the authoritative test
outcome. No unexpected failure required changing runtime or timing thresholds.

During this run only Tools were edited: the font audit additionally verified
the Green legacy font, its material and atlas against current reverse export.
All 11 resource payload checks pass. No Assets edits were made while Unity was
running. No SDK, build settings, prefab or runtime code changed this round.

Scope remains the assertions actually implemented in these tests. This does
not prove all country/AB outcomes, every stochastic branch, all device lifecycle
interruptions or complete pixel/animation parity. The full reconstruction goal
is still active; no new Android APK was built or installed for this regression.
