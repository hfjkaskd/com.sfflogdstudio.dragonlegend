# Free reel-phase controller and stop presentation

RollReel.PlayFreeStopAnim 0x2376dd0 traverses freeBonusList first, then
freeLongZhuList. Each coin replays PlayShowAnim, emits `coinshow`, then vibrates
200ms. Each ball plays PlayStartAnim, emits `scatterShow`, then vibrates 200ms.
The names were resolved from ELF RELA pointers 0x4f1c3a0 -> 0x505ecb8 and
0x4f1c378 -> 0x5063308. FreeRoll.PlayStopAnim 0x23b8b44 runs this on its three
serialized rows. Rolling/initial specials are not entries in these result lists.

The Free pool now retains these current-stop references separately from its
registered-effect dictionary. Landing clears both current lists before reading
the actual cell and records the newly created result before Dictionary.Add.
Wrapping still clears the registered ownership separately. This preserves the
native distinction instead of playing every active pooled special.

AutoFreeSpin callback 0x23c3cc8 increments its column count, plays that column's
stop presentation, emits `reelstop` (0x4f1e438 -> 0x5062ed0), then requests the
board shake. The new prefab-authored RecoveredFreeReelController connects this
reel phase. It starts all five columns synchronously with .2s acceleration
(UIMainView ctor 0x23bf300, low word at +0x21c = 0x3e4ccccd), then uses the shared
Update runner to wait for exactly five callbacks (predicate 0x23c3ddc).
ReelsStopped is the continuation boundary for subsequent reward processing.

Stop-time coin replay can overlap the appearance pulse created at landing.
PlayShowAnim 0x23d95e0 does not kill its earlier scale tween; its completion
0x23d99dc appends a separate return tween. RecoveredFreeCoin now retains active
scale jobs in creation order instead of replacing a single phase. Each job
captures its starting scale lazily on first advancing update. Callback-created
return jobs begin next update: native TweenManager.Update 0x243b0dc loads
_maxActiveLookupId at 0x243b18c and snapshots +1 into x24 at 0x243b1b0 before
iterating. Jobs use the original .2s OutQuad legs and reusable struct storage.
The explicit overlap test samples .925, 1, .775, .7, .7 after a replay at .1s.

Tests also verify that initial specials produce no stop events, that the six
coins and seven balls emit sound/vibration in row order, and that the full
five-column controller reaches its continuation only after all stop callbacks.
The fresh mixed-board stop capture is Artifacts/current-free-stop-presentation.png.

Not yet completed: the surrounding AutoFreeSpin initialization/count display,
reward-list accounting, CheckFreeSpinEnd/bonus/ball/collection continuations,
production FreeEntry/UI binding, and these event hooks' actual audio, device
vibration and board-shake consumers. Inactive pooled-object scale-tween lifetime
still follows the existing disable cleanup and needs separate source alignment;
the active stop-time overlap is now covered. SDK handling is unchanged.

Validation: full Unity 2022.3.62f3 PlayMode suite **291/291 passed** in
Artifacts/free-stop-presentation-tests.xml. The fresh 1080x720 mixed-board stop
capture was inspected at original resolution: coin replays and ball starts are
visible in the result layer, with the ordinary Free symbols retaining their cover.
