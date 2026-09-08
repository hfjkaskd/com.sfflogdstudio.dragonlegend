# Free column startup, stop aggregation and result layer

Native evidence:

- FreeRoll.StarSpin 0x23b85c0 iterates its three serialized RollReels, passing a
  shared AccCall closure and null child stopCall. Its own acc parameter is unused.
- First child AccCall 0x23b8c7c sets isCall before scheduling StopSpin. Delay is
  0.5 + column * ELF float 0xdbc520 (0.15000000596046448). The remaining child
  callbacks do nothing. Scheduling occurs before the other two children start.
- FreeRoll.StopSpin 0x23b87c4 schedules the same scaled delay on all three rows.
  Callback 0x23b8d68 increments its count and calls ShowFreeEffect every time;
  only count == 3 invokes the outer column callback.
- ShowFreeEffect 0x23b89bc iterates all three reels, calling 0x2376184. Every
  registered landed effect is parented to the result Transform with
  worldPositionStays=false, then placed at its first symbol child's world position.
  This happens even for a sibling whose return tween is still running.
- Free StarSpin 0x23789bc calls AccCall synchronously, then queues Update
  WaitUntil using 0x2377800: !isStop. It does not wait for !isStartSpin. Without a
  requested stop, its startup task can complete while the reel is spinning.

RecoveredFreeColumn is authored on each of the five original column groups.
Its row array, column index, .5/.15 delays, pool and result layer references are
prefab fields. RecoveredFreeReelSpinOperation uses the existing native Update
runner and preserves the stop-flag predicate. Forgotten startup/stop operation
faults reach Unity logging; normal native debug-log strings are not gameplay
events. No SDK path is changed.

The shared FreeResult world Transform is outside the mini reel masks. The source
FreeResult RectTransform 224056435115414594 has identity rotation and scale;
its anchored position is (.003418,335.14). World effects here use a neutral
container origin because each reparent operation explicitly sets world position.
The final mode UI hierarchy remains a separate pending integration. Effect-local
scale is preserved on reparent. Per-mini clipping is removed on result-layer
placement and restored when ResetRellShow reparents the effect to its first slot.
Slot cleanup skips effects outside the slot, matching GetComponentInChildren;
the registered-effect cleanup still owns and returns them on the next wrap.

Tests cover synchronous child AccCall and premature startup completion, the
unused column acc callback, all five delays, exactly one outer stop callback per
column, and all fifteen effects arriving in the result layer. A controlled test
finishes the rows separately: the first callback moves all registered effects,
later moving-node changes do not drag the reparented effect, and each next row
callback refreshes all three positions. It also verifies slot cleanup cannot
despawn a result-layer effect and that restart restores parenting/clipping before
wrap cleanup. The fresh whole-board capture is
Artifacts/current-free-columns-stopped.png.

PlayFreeStopAnim and the five-column reel-phase controller are now connected
(see free-stop-presentation.md). Still pending: per-spin rewards and collection/flight,
FreeAutoSpin accounting/continuations, full Free mode UI and production FreeEntry
binding. A complete Free lifecycle or full game parity is not established here.

Validation: full Unity 2022.3.62f3 PlayMode suite **288/288 passed** in
Artifacts/free-columns-full-tests.xml. The fresh 1080x720 whole-board stop
capture was inspected at original resolution. The last column still has its
appearance/scale pulse and extends beyond the former mini-reel clipping bounds,
while all three landed effects in each column are aligned in the result layer.
