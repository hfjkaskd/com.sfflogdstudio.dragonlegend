# Core round integration

User priority for this stage: core gameplay first. Production GameEntry now
instantiates CoreRoundFlow after the playfield, Bonus flow, cash flight and
collection destination are bound, before publishing Ready.

The previous production graph ended at Bonus.Completed, leaving Base IsBusy set.
The new continuation checks the Base round's ScatterCount through the existing
FreeEntryFlow. No-Free completion releases the core round. A Free entry keeps the
Base lock through its entire session and actual return transition.

Core links:

- Base symbol sequence -> existing Bonus -> CheckFreeGame.
- Entry cover -> actual mode ApplyCurrent, InitializeFreeReels, RefreshFreeCount.
- Entry second callback -> existing debit/generation -> actual Free controller.
- Free stop -> coin scan -> existing Bonus -> ball scan -> actual Slot, Wheel,
  Treasure or Lucky entry selected by the recovered router -> reward collection.
- Collection complete -> existing Free exit check -> another debit/generation or
  actual end window. Continue launches the original transition; only the return
  completion clears the Free-end flag and releases the core round.

The destination reproduces source QiPan/LongzhuPos Rect 224539572024531195:
(-1.6201172,100), size 100x100, centered in QiPan at (-.003418,652). Its equivalent
root-relative authored destination is (-1.6235352,752) with bottom-center anchors.
Arrival still reparents to the actual NPC PlayFire; Treasure targets the actual
production CollectEntry destination. All static hierarchy is prefab-authored.

InitReels 23bce8c guards Main +1ed before initializing Base reels. The controller
now preserves this guard so the return callback 23bf600 invokes initialization
without recreating/resetting already initialized symbols. Free return callback
23bf618 clears +1e0 before restoring normal music; the core binding clears the
entry flow's corresponding flag at this callback.

Important remaining scope: this is the core-gameplay continuation, not the full
native CheckBaseEnd implementation. Its later upgrade/cash-out/peripheral popup
checks and waits are still absent. They must be inserted before final unlock for
complete lifecycle fidelity. CoreRoundCompleted currently records core completion;
it does not claim the peripheral checks ran. Common audio event playback also
remains pending. SDK facades are unchanged.

Focused test drives the actual production Spin button (including duplicate-click
debit protection), claims the ordinary win, starts another spin with the existing
ForceFreeSpin switch, accepts the actual start window, auto-plays a Free round,
collects, continues the actual end window and waits for return before spinning
again. For test duration only, the live count is set to one at Free music change
and RNG selects a no-ball round; this is not production logic. Existing isolated
suites still cover all four ball branches. Full mixed production branches require
additional end-to-end verification.

Validation: focused core-round-focused.xml passes 1/1; full
Artifacts/core-round-regression.xml passes 352/352 PlayMode tests. Ordinary,
BigWin and Bonus integration tests now expect the production no-Free continuation
to release IsBusy after settlement. Two component fixtures explicitly unbind the
production core before installing their own entry/counter event harness; otherwise
they would drive duplicate Free controllers without the normal cover initialization.
The final full run also passes existing RNG pause and wheel timing assertions.

The no-ball shortened session test does not cover ball branches. The additional
production-scene branch test below covers each branch in a separate Free session;
the repeated-session test below additionally covers two rounds with multiple balls.
Larger sessions, all Bonus interleavings and advertisement outcomes remain unverified
as complete production sequences.

GM teardown follow-up: free-gm-before.xml reproduced a running old Free controller
after the actual SelectUS button rebuilt the profile. FreeColumn now retains the
three startup and three stop operations, preserving the synchronous first-child
AccCall ordering. Explicit profile teardown cancels those operations and resets
movement before destroying the old playfield. The controller also cancels its
five-column completion wait. This is local GM lifetime handling, not a claim that
the original game exposes this GM profile switch. Ordinary mode visibility changes
do not cancel the operations or change normal reel timing.

free-gm-after.xml passes the production-scene test in both acceleration and partial
column-stop phases: immediate stopped state, no later old stop/sound callbacks,
no old balance/count mutation, and a working Spin button after rebuilding.
Full follow-up regression: Artifacts/free-gm-regression.xml passes 353/353 PlayMode
tests, including normal Base/Free round completion and existing reel timing tests.

## Production ball branch verification

RecoveredCoreBallBranchesTests drives the actual Spin button through the initial
ordinary round and four further ForceFreeSpin rounds. Each waits for the actual
start window, Free reels, coin/Bonus scan, ball flight/activation and production
router. Slot, Wheel, Treasure and Lucky each open their real bound prefab and
claim through their actual plain Button. The test checks one ledger entry, matching
Free reward collection/session total, one balance credit, zero remaining spins,
the end window, the return transition lock, and a subsequent working Spin button.
Duplicate Spin clicks also consume only one spin.

Fixture-only controls: Free music callback sets one remaining spin and selects a
zero-coin/one-ball generation seed. Once the actual ball scan starts, the test
selects a seed whose first reward draw for the stopped ball's type chooses the
requested branch. It does not replace the router, callbacks, claim/flight code,
windows or production configuration. This validates four real single-ball sessions,
not all country distributions, advertisement outcomes or mixed-ball combinations.

An initial balance assertion exposed the preceding BigWin flight still arriving
after the Free intro appeared (648 units in the diagnostic run). This is not a
duplicate ball reward: native UIBigWinView.OnAfterHide 2395548 invokes the main
callback before dispatching the independent event-1 cash flight. The fixture now
waits for that existing flight to drain before taking its per-ball balance baseline;
runtime ordering is unchanged. Another fixture correction gates its RNG selection
on an active ball scan, since the Base Bonus completion can enter Free before later
subscribers run. No runtime fixes were needed for these four verified branches.

Focused validation: Artifacts/core-ball-final.xml passes 1/1, containing all four
branches and their actual Spin-to-return sequences.
Full validation: Artifacts/core-ball-regression.xml passes 354/354 PlayMode tests.

## Repeated Free session

RecoveredCoreRepeatedFreeTests drives the actual initial Spin, then ForceFreeSpin,
start window, two automatically connected Free rounds and final return. Each round
contains one coin and two dragon balls. All four ball rewards pass through actual
production minigames and claim Buttons; only one minigame runs at a time. The second
round begins from RewardCollect -> FreeExit.Check -> FreeSpinEntry -> result generation
-> PresentationRequested without a test call to restart the controller.

At each RoundStarted, the per-round dictionary is empty and the prior session totals
remain intact. At each BallScan.Completed, the three stopped special symbols each
have one reward entry. Final verification compares four observed game entries, two
round starts/scans, four ball rewards, session total, reward collector counters and
the actual wallet. Spin stays locked through both rounds and the return transition.

Native coin accounting is deliberately retained: ARM 23cfb34..23cfb48 credits the
current cumulative CoinReward at each collection end. For round coin amounts c1/c2
and total ball awards b, the two-round wallet increment is b+c1+(c1+c2), while the
session reward display records b+c1+c2. The test computes this from actual ledger
entries; it does not normalize the source behavior into per-round-only credit.

Fixture controls set two remaining spins at Free music change and seed a 1-coin,
2-ball board. During reward collection the fixture restores that seed for the next
generation. No runtime configuration, generation callbacks or reward consumers are
replaced. This checks the selected repeated path, not every possible mixed outcome.
Focused Artifacts/core-repeated-free.xml passes 1/1; no runtime fix was required.
Full Artifacts/core-repeated-regression.xml passes 355/355 PlayMode tests.
