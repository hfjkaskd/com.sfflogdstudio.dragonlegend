# Current four-branch reward captures

Baseline 9a19253. The production core-ball integration fixture now captures
each branch before its first claim through the authored camera at 1080x1920.
Eight current PNGs (four plain, four advertised) are generated under Artifacts,
with SHA256 hashes in core-ball-current-captures.json. No historical image was
used as the current rendering reference.

The first capture implementation ran immediately after the first clickable
frame. Visual inspection showed very small ordinary-claim labels in Reward
and Jackpot, plus incomplete Treasure title/card entrance. Current presenter
Reveal.Tick activates the ordinary-claim parent at scale zero and animates to
one; BuildJackpotPopup authors one-second delay and .3-second reveal. Thus an
active/raycastable Button alone does not establish a stable visual pose.

Captures now wait .6 scaled seconds after an ordinary-claim Button becomes
active, then render BEFORE dispatching the first claim. The render target and
temporary Texture2D are released, and the prior active target is restored.
This delay is test observation only; no runtime animation or font was altered.

Inspected all four fresh plain captures at full portrait resolution. Ordinary
labels are full size after the reveal. Slot and Lucky show Lucky Reward art;
the selected Wheel outcome is Minor; Treasure shows Lucky Card title, card,
cash amount and both choices after its flip/presentation completes. The
initial small-label observation therefore did not justify changing font size.

MoreSpins remains deliberately open behind every branch for the existing
popup-depth/input test. Its visible text behind the dimmed reward window is
part of that injected overlap scenario, not evidence of the ordinary screen
composition or complete source parity. GM toggle is also visible. These
captures do not compare every font/material/pose to original rendering, and
they are not Android physical-input or natural branch evidence.

Initial capture run PID 2308 exited: 2/2, 34.7597334 seconds.
Stable pre-claim capture run PID 2904 exited: 2/2, 36.8547293 seconds,
Artifacts/core-ball-stable-capture-tests.xml. Both plain and advertised retry
paths still check all four reward ledgers and return to Base. No runtime,
prefab, SDK or full-suite change was made this round.
