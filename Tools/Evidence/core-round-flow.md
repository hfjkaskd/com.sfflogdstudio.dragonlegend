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

Remaining core verification includes mixed production ball branches; the no-ball
shortened session test does not cover those cases.

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
