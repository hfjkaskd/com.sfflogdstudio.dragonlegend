# Free dragon-ball flight

LongzhuItem.FlyLongZhu 0x23aecdc invokes spawn callback 0x23af10c synchronously.
It borrows from PoolManager's dragon-ball pool (+0x90), parents the clone to
the original ball's parent with worldPositionStays false, sets localScale .8,
copies the original world position, initializes the current BallType and
moves the clone to the last sibling. The original ball remains in place.

The clone flies to a snapshot of the target's world position for .3 seconds.
FlyAnimUtils.Fly 0x238c74c resolves height -1 to distance * .3, builds the
quadratic Bezier control point from midpoint plus that world-up height, and
passes ease 4 into SetEase. The current Dragon Legend dump defines Unset=0,
Linear=1, InSine=2, OutSine=3, InOutSine=4, InQuad=5. Therefore this flight uses
(1-cos(pi*t))/2, not t squared. Arrival callback 0x23af2b0 passes the clone and
original GameObjects to its caller without despawning either.

RecoveredFreeSpecials.FlyBall now borrows from its existing shared ball pool,
applies these transforms and initializes type, and tracks the outstanding
borrow. RecoveredFreeBall performs scaled-time flight with prefab-authored
duration/arc parameters. ReleaseFlightBall is explicit for the later reward
consumer; arrival itself retains the clone. External-parented outstanding
clones are destroyed when the owning pool is destroyed. The test covers all
three types, same pooled instance reused, original retained, .15s Bezier
midpoint, pause, captured destination and explicit return after callback.

Validation: Unity 2022.3.62f3 full PlayMode suite 297/297 passed in
Artifacts/free-ball-flight-tests.xml; authoring exited successfully in
Artifacts/free-ball-flight-author.log. Inactive borrowed-object tween lifetime
still uses the existing disable cleanup and needs separate source alignment.

Still pending: CheckPlayLongzhuAnim's per-row wait, arrival reparent to NPC,
NPC state 1, its 1-second wait, LongzhuItem.PlayAnim, branch reward selection,
original-ball reward reveal and dictionary/total writes. The flight API is
not yet wired into the complete production Free continuation. SDK unchanged.

The same enum discrepancy in the cash path is now corrected: see
JackpotPopup/flight.md. Its departure call passes 4, and the runtime and
prefab now use InOutSine instead of the previously authored t-squared curve.
