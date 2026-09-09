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

## Full awarded Free session after BigWin and Bonus

Runtime baseline d91b85057a051cf410c8c2d159b56ad55ac26a82; only this evidence
and RecoveredBigWinFreeChainTests change in this round. The new third case
retains all awarded initial + ad-extra Free spins. It does not install the
ChangeMusicRequested count/RNG override used by the two shorter cases, reseed
subsequent rounds, change rules or replace generated symbols. The existing
ForceFreeSpin setup and pre-Spin Bonus readiness flag remain explicit fixture
inputs, as described above.

The fixture uses active, interactable standard Button callbacks to claim
encountered ball-game rewards and handle any Free Bonus window. It observes
each Free reel start, all-reels-stopped callback and completed ball scan,
checks remaining count equals awarded count minus rounds started, keeps the
paid Spin busy until the total window is continued, and verifies exactly one
core completion and acceptance of the next paid Spin.

The independent scan ledger reads each actual special symbol's recorded
reward, then checks the complete session's FreeReward, CoinReward and
TotalFreeSpinWin at the total window. It also checks no simultaneous ball
games and no errors in the core, exit, reel, coin scan, ball scan, reward
collection, slot, wheel or lucky controllers. This preserves the native
cumulative coin-credit behavior; it does not replace that behavior with an
assumed per-round credit rule.

The first targeted process 38496 exited with 3/3 passed in 11.36897 seconds
(Artifacts/full-mixed-free-tests.xml). After adding reward-ledger checks,
process 51304 produced 3/3 passed in 11.2906468 seconds
(Artifacts/full-mixed-free-ledger-tests.xml). Its full case recorded:

- Mixed Base result seed 2578, followed by BigWin and ready Bonus.
- All 6 awarded Free rounds started, stopped and completed their scans.
- 4 coin symbols and 6 ball symbols processed; total Free ledger 129.
- Total window continued, Base music restored, next paid Spin accepted.

The two shortened cases still pass with one Free round each. This adds
continuous full-count coverage for the observed mixed session, not every
random outcome, every ball type, region profile, lifecycle interruption or
pixel-level visual result. No SDK or production implementation changed.

## Actual Base collection triggers Bonus

The fourth case starts from [2,2,2,2,1], with the final lamp off and
IsBonusGame false. Its probe requires an actual column-4 coin, BigWin and
Scatter. The first 10,000 candidates found none (process 18704: 3/4 passed,
new case failed before gameplay). Expanding the bounded search found 29858;
process 32616 passed that case in 6.107358 seconds. No rules, board cells,
callbacks or readiness flags were substituted to satisfy the search.

After the actual Spin, column 4 initially stays at 1. At BigWin it has reached
at least 2, and the real flight has lit the previously dark lamp, while
IsBonusGame remains false. CheckPlayBonusAnim increments during its scan;
the visual arrival lights the target; CheckBonusGame later tests the counts
and consumes/resets the area. The data increment is not moved to arrival.

The existing continuation then completes Bonus, preserves Scatter, and runs
all 6 awarded Free rounds without shortening or per-round reseeding. It
processes 5 coins and 4 balls with Free ledger 103, continues through the
total window, returns to Base and accepts the next paid Spin.

With the lamp assertions, process 6824 exited: 4/4 passed in 15.9393685 seconds
(Artifacts/collected-bonus-free-final-tests.xml). This verifies last-coin
collection to Bonus/Free in the observed session, not the entire collection
from zero or all outcomes/profiles. Production and SDK code are unchanged.
