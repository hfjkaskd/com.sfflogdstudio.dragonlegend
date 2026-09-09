# Full regression after separating the native totals

Baseline 10323e5. The first full run, Unity process 35316, exited with
468/469 passing in 129.2537147 seconds. The only failure was the ordinary
flight test's elapsed-time assertion: subtracting two float Time.time values
late in the suite returned 0.799804688 versus the 0.799899995 lower bound.

RecoveredOrdinaryWinTests now records the same scaled begin/completion times
using double Time.timeAsDouble. The threshold, wait, pause checks and production
timing code are unchanged. This avoids subtracting large single-precision
timestamps; it does not shorten the required completion delay.

The subsequent full run, Unity process 38604, exited with 469/469 passing,
zero failures, in 129.3229731 seconds:
Artifacts/coin-total-full-final-tests.xml. This includes the separated display
counter assertions, ordinary/BigWin flows, continuous collection sessions,
profile tests and scene-reload tests. It does not prove complete source parity.

## Continuing the current Android installation

The already installed fixed ARM64 APK from coin-display-accumulators.md was
used without modifying its save or random results. Visible Spin button taps
at (1280,2420) advanced paid rounds five through eight, inspecting fresh device
captures between taps:

- Round five: cash symbol $0.17 plus line award; bottom $0.18 and balance
  $5.04 -> $5.22. MoreWild 2/5 and Spin 5.
- Round six: one cash symbol $0.15, no line award; bottom $0.15 and balance
  $5.22 -> $5.37. This directly checks the pure-coin case that previously
  doubled the displayed amount. MoreWild 1/5 and Spin 4.
- Round seven: no reward, GOOD LUCK, balance unchanged; MoreWild reaches zero
  and the entry returns to its ordinary MORE WILD label. Spin 3.
- Round eight: no reward, GOOD LUCK, balance unchanged; Spin 2.

Fresh captures are Artifacts/android-spin-five.png through android-spin-eight.png.
The actual app-private snapshot android-eight-spins-prefs.xml contains
SpinCount=2, GreenCount=537, MoreWild=0, GuideStep=3, LimitSpinCount=8 and
BonusArea=[2,1,1,2,2]. Bonus has not yet been reached in this device continuation;
its slots must not be forced full and described as natural collection evidence.

No runtime or SDK changes were needed this round. The installed APK remains
the preceding production revision because this commit changes only a test and
this evidence. Remaining country/AB, original visual and lifecycle coverage
is not discharged by these results.
