# One paid Spin: BigWin followed by Free

Baseline runtime: 931dc7fe3cd00e2e34f1db77f1173bd19a71f2a1.
RecoveredBigWinFreeChainTests loads the current production GameEntry scene.
It searches with the existing result generator and then executes the selected
seed through the real Spin Button, with the existing ForceFreeSpin control.
No board cells, generation callbacks, reward values or rules are replaced.
The verified mixed Base seed is 2578: actual ScatterCount >= 3 and actual
settlement classifies as BigWin.

Covered chain:

- BigWin appears while Free has not started and balance is unchanged.
- Rewarded ad fails, clears its latch and causes no flight/credit; retry succeeds.
- Exactly one Base cash flight is requested; the subsequent Free intro remains
  active while that flight completes, and balance receives the Base award once.
- The paid Spin remains locked at the Free intro, with exactly one debit.
- Free intro count comes from actual ScatterCount. Failed rewarded ad preserves
  that count; successful retry writes initial + configured extra spins.
- One real Free round reaches the actual total window, remains locked until
  Continue, returns to Base with normalBg, completes once, and accepts next Spin.

The later Free loop is explicitly shortened at ChangeMusicRequested to one round
with no ball (seed 0), after checking the intro's actual configured counts. This
fixture covers the combined BigWin/Free boundary, not all awarded Free spins,
Bonus combinations, ball routes, all region profiles or visual parity. Existing
SDK/ad facade behavior is unchanged. PlayerPrefs, RNG and clock settings restore
in finally; the production scene unloads after the fixture.

## Native timing finding

The first attempt incorrectly required cash already credited when Free intro
appeared. It observed 0 rather than the expected 112 and failed at that assertion.
This is not evidence that the production chain should await cash:

- BigWin.AfterHide 2395548 calls the main continuation before event 1, whose
  payload has a null cash-flight continuation (bigwin-window.md).
- Main PlayFlyCoin 23d440c schedules departures at i * .03 with unscaled time,
  after a .3 scaled wait; individual flights use scaled time
  (JackpotPopup/flight.md).
- Direct ARM in native/game/023c306c.asm confirms callback at 23c30d4, title reset
  at 23c30f8, and GreenCount read/add/set at 23c313c..23c3150. There is no mode
  guard between reset and credit.
- ClickSpin 23d1f30 sequences Bonus, Free and BaseEnd; the BigWin continuation
  is not an all-cash-arrived callback.

Accelerated captureDeltaTime makes the two clocks' overlap particularly visible.
The test now independently waits for the real cash queue to empty with a bounded
wall-clock timeout, checks one credit and keeps the Free intro open. It does not
change production ordering to satisfy an incorrect test assumption.

Initial process 49384 exited: 0/1, 1.7875616 seconds. Corrected targeted process
16272 exited: Artifacts/mixed-bigwin-free-final.xml passed 1/1, 2.515786 seconds.
This turn changes only the fixture and this evidence. The prior full 461-case
runtime baseline passed in top-withdraw-final-tests.xml; that is historical
baseline evidence, not a claim of a new 462-case full run.

## Follow-up: Bonus between BigWin and Free

The fixture now retains the direct case and adds a second case with the existing
IsBonusGame readiness flag set before the same actual mixed Spin. This represents
a Bonus already due; it does not claim to verify the earlier collection that
sets that flag. The actual seed, board generation and settlement are unchanged.

The second case checks that Bonus appears before Free, consumes its readiness
flag, resets its collection area, and stays in Base mode. It selects every
configured free card through its Button and resolves real reward/Jackpot popups.
After every selection it checks the original ScatterCount is retained and Free
has not started. After all cash arrives it closes the real Bonus window, checks
the Bonus flow/window have finished, then verifies the same ScatterCount drives
the Free intro without another cash mutation. The existing Free ad retry, count,
shortened round, exit and subsequent paid Spin assertions run in both cases.

Direct current ARM inspection in native/game/023d1f30.asm confirms sequential
calls: 23d2c14 -> CheckBonusGame 23bee60, 23d2dd8 -> CheckFreeGame 23bef04,
23d2f9c -> CheckBaseEnd 23befb4. Current RecoveredBonusFlow and CoreRound follow
that order; bonus-exit.md records its distinct hide/source-completion timings.

Unity process 22092 exited. Artifacts/mixed-bonus-free-tests.xml passed both
cases, 2/2, 5.8129854 seconds. No production implementation or SDK change was
needed. This expands actual combined-path coverage; it does not establish
all possible mixed rewards or complete 1:1 lifecycle equivalence.
