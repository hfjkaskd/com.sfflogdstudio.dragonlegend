# Main Spin count and recovery

Source: UIMainView.InitSpinCount 23bb488, DeleteTime 23bd044,
display-class callback 23c37d0, OnSpinCountChanged 23bacc0 and Spin/MoreSpinBtn
branches of OnClickButton 23bd4e8. Read alongside their ARM listings; pseudocode
contains inlined tail pollution and is not the original C#.

InitSpinCount shows count only when already at the maximum or config type is not
`default`. Otherwise it reads signed 32-bit UTC seconds minus LastSpinTime,
divides by the live level's SpinCD and preserves the remainder. Positive whole
intervals write LastSpinTime = now - remainder, invoke SetSpinCount(current +
intervals), then invoke SetSpinCount(clamp(current, 0, max)) again. Both setters
save and notify. If this fills the count, it shows count and writes LastSpinTime
= now **after** those saves, with no additional save. Otherwise it starts a timer
for CD minus remainder. A future timestamp is not clamped: negative remainders
extend the initial timer. ARM signed division returns zero for a zero divisor.

DeleteTime returns unchanged for count <= 0 or config != default, otherwise
renders the starting timer and replaces CountDownSeq with a sequence of one-second
interval/callback pairs. No independent time setting is applied: use scaled game
time. Each callback decrements then displays the live count and remaining time.
At zero it writes current UTC, performs the same two SetSpinCount calls, then
starts a fresh live-level cooldown if still below maximum, or shows count only.
OnSpinCountChanged ignores the event argument and acts only when the stored
count equals maximum: cancel sequence and show stored count. Other changes are
picked up by the next timer tick. Accepted non-default Spin separately refreshes
count text in OnClickButton before result generation; default Spin starts a new
timer only when leaving maximum. These are separate subscriptions in the port.

Resolved ELF relocations against script.json:

- 4f1e190: CountDownSeq
- 4f1e198: `<gradient="spin">SPIN {0}</gradient>`
- 4f1e290: `{0:D2}:{1:D2}`
- 4f1e298: `<gradient="spin">SPIN {0}</gradient> <size=#48>{1}</size>`
- 4f1e340: MoreSpinBtn. Its branch plays click then joins the same ShowWindow
  call at 23bdc34 as a zero-count Spin; it has no Spin busy guard.

BuildSpinRecovery extracts original Main/SpinShow subtree (Rect
224643520764344681), including background, TMP and MoreSpinBtn. Source Rects,
font 60, material, gradient, plus sprite and standard Button are retained. The
component lives on the playfield root so hiding Bottom/Main during Free does
not suspend the timer. CoreRoundFlow binds the original-count initialization,
Spin countdown/display events and actual More Spin window; teardown unsubscribes
and disposes recovery before replacing the player. No runtime UI construction,
reflection or third-party assembly was added. SDK behavior is unchanged.

RecoveredSpinRecovery retains the source callback boundaries with a constant-size
scaled timer instead of allocating one tween node per configured second. A newly
created cycle starts on the next Update, as with the source callback-created
sequence. Full native window/application resume lifecycle remains a broader audit;
no speculative focus/pause-time offline refill was added.

Unit cases cover offline remainder, double-save order and full timestamp timing,
live count/level changes, scaled pause, refill-to-full cancellation, organic/full
early exits, future timestamps and disposal. Production integration uses the actual
GameEntry prefab and loader, count bar, plus/Spin Buttons, timer Update and GM
teardown. Both shipped US and A comparison snapshots currently resolve to organic;
the default branch integration therefore uses a temporary synthetic copy of the
US snapshot with only Ripg[0] changed to default. It deletes that file and restores
preferences after the test. This is test evidence for a native configuration
branch, not evidence of original region routing or a change to shipped profiles.

Validation: spin-recovery.xml passes the initial five model cases;
spin-recovery-default.xml passes all six model/integration cases; full
spin-recovery-regression.xml passes 371/371. The initial integration assumption
that A_Test was default was contradicted by the loaded snapshot and corrected in
the fixture, not in production data. Fresh current-spin-recovery.png was inspected:
the source bottom count bar renders `SPIN 9 01:30` and its plus button below the
GOOD LUCK label beside the Spin button. Common sound playback remains part of the
broader unfinished audio work; this gate does not certify every lifecycle branch.


## Plus-button click audio audit

The actual MoreSpinBtn branch (resolved 4f1e340 in Main.OnClickButton 23bd4e8) plays click before showing the More Spin window, without a busy guard. RecoveredSpinRecoveryView previously invoked only the show callback. The window's BeforeShow already played remind, so the first-open sound masked the missing click in a simple isPlaying check.

The initial Artifacts/spin-plus-audio-before.xml test passed 1/1 but was too weak: it heard remind. The isolated check clicks again with the window already open after clearing the audio source. Before the fix, Artifacts/spin-plus-audio-isolated.xml failed 0/1 at that check (Unity PID 52228 exited). No runtime bypass or expected-error suppression was used.

RecoveredSpinRecoveryView now emits click before its show callback. CoreAudio subscribes/unsubscribes the view's sound event with the rest of the current playfield graph. The empty paid-Spin branch keeps its own existing click event; the More Spin window itself still emits remind on first show and does not add a second click.

Artifacts/spin-plus-audio-fixed.xml passed 9/9 in 5.2515535 seconds; PID 5940 exited. Tests assert exact plus opening order click/remind, click again without a second remind, muted playback, opening during active Spin without consuming an extra count, old button detachment during GM replacement, and exact empty-Spin click/remind without duplicate plus audio. Existing count recovery cases, actual More Spin ad failure/success/limit flow, window lifecycle and Base/Free return also passed. This is affected-suite validation, not a new complete-project regression.

The same audit checked the presumed player bet-switch operation against the supplied original: GameData.SetBet 236f98c obtains GetBet(IsA), assigns element zero only when IsA is false, and is called from GManager.Start 237110c. The supplied UIMainView hierarchy and click handler have no bet-switch control; no invented bet button or new bet-change behavior was added. This supports the current startup assignment only, not a blanket proof of all reward calculation/routing.
