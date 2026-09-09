# Separate native coin-display and whole-round totals

Baseline 4afbce9. Continued the existing Android verification installation
without modifying its saved record or random results. The second paid round's
ordinary $0.58 claim completed through the visible Only button at (720,1930),
then the actual free MoreWild guide appeared. Its CLAIM at (720,2100) saved
GuideStep=3, MoreWild=5, SpinCount=8 and GreenCount=482. A third paid Spin at
(1280,2420) saved MoreWild=4, SpinCount=7 and LimitSpinCount=3.

The third round exposed a real display discrepancy: the single cash symbol
showed $0.11 and the balance increased from $4.82 to $4.93, but bottom text
showed $0.22. A later independent disk read still showed GreenCount=493.
Artifacts/android-current-wild-spent.png is the freshly captured pre-fix
device evidence. The preceding guide and claim captures are
android-current-wild-guide.png and android-current-wild-claimed.png.

## Native cause

The original UIMainView dump distinguishes three floats:

- tempDownWinCount at +0x1e8: whole-round temporary amount;
- temp_down_win at +0x230: delayed coin presentation accumulator;
- DownWinCount at +0x248: stored displayed amount.

CheckPlayBonusAnim MoveNext 0x23cb808 resets +0x230 at 0x23cba40.
CheckPlaySymbolAnim MoveNext 0x23cce68 stores the whole-round amount at
+0x1e8 at 0x23ce098, separately comparing the bonus amount at +0x224.
DownWinTextCoin MoveNext 0x23d3fb4 reads the dictionary reward at 0x23d4210,
adds +0x230 at 0x23d4214..0x23d421c, then writes +0x230 and +0x248 at
0x23d4220..0x23d4224. It does not add to or overwrite +0x1e8.
These statements were checked in the current raw ARM files, not inferred
solely from the old evidence documents or from the visible mismatch.

RecoveredDownWinText previously used one temporaryTotal for both native
temporary fields. A delayed coin callback could therefore add its reward
to the whole-round amount already stored by ordinary settlement. The fix
keeps temporaryTotal for +0x1e8 and adds coinPresentationTotal for +0x230.
BeginScan clears only the latter; presentation completion updates the latter
and Total, preserving the whole-round amount. Credit logic is unchanged.

## Verification

RecoveredDownWinTextTests now verifies that BeginScan preserves a preexisting
whole-round amount, storing a round amount during a pending coin callback
does not double the displayed coin amount, and later presentation callbacks
do not overwrite that round field. Existing duplicate-callback and cancellation
semantics remain checked.

Unity process 50796 exited: separate-coin-total-tests.xml passed 7/7 in
16.8259326 seconds (DownWinText, OrdinaryWin, CoreRoundFlow and both continuous
zero-collection sessions). This is targeted coverage, not a new full-suite run.
SDK behavior and Bonus renderer selection are unchanged.

The fixed ARM64 Development build also succeeded: Unity process 19512 exited,
zero errors/warnings, duration 00:01:41.1969572. Actual APK size 129416825 bytes;
SHA256 af071891c01772a257aa5f954f8c799ab82bb0c2f41294aa7f80bc2cc2b6ec38.
It replaced the verification package using install -r, preserving its record.
After startup the balance was $4.93, Spin 7 and MoreWild 4/5. A single visible
Spin tap at (1280,2420) produced a $0.10 coin and a J line; after settlement the
balance was $5.04 and the bottom displayed $0.11, with Spin 6 and MoreWild 3/5.
Fresh screenshots: android-fixed-before-spin.png and android-fixed-after-spin.png.
This mixed coin/line device result is consistent with the separated counters;
it is not a replay of the exact pre-fix board or exhaustive device coverage.
Generated URP/player-setting serialization changes were inspected and restored
after build exit; only the intended runtime/test/evidence files are committed.
