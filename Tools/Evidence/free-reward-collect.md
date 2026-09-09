# Free reward collection

`RecoveredFreeRewardCollect` implements Main.CheckRewardCollect (wrapper 23be8c4,
MoveNext 23ceeb4). It is serialized on FreeReels; its pooled DownWin flights are
children of the existing FreeResult. Bind supplies the actual Main DownWin text,
Bottom, window order, player/result, and optionally FreeEntryFlow. BallScan.Completed
starts collection; collection.Completed now connects to RecoveredFreeExitFlow's
Free spin-end chain when bound. This does not make the production Main Free lifecycle
complete: GameEntry still needs the board/mode/counter/entry/exit consumers.

## Native evidence

- Outer loop reads FreeRells length, inner loop visits three rows. Symbol ID is
  queried when each cell is reached. Only 9 (coin) and 11 (ball) are collected.
- RollReel.GetFreeBonus 23774f8 returns element zero of current result coin list
  (+a0) or ball list (+a8). The recovered stopped binding has the single current
  result object; initial/rolling pool entries are excluded.
- Scale to one over .2, default OutQuad. Completion 23bfca8 repeats GetFreeBonus
  and scales that current object to .7 (coin) or .8 (ball) over .2. Original ELF
  floats at dbb928 are .8/.7; dbc540 is .2; dbc664 is .3.
- Callback 23bfcec spawns PoolManager +38 under FreeResult, copies the reel world
  position, activates, moves to last sibling and applies Main order. FlyAnimUtils
  goes to DownWinText world position over .3, automatic arc (-1), ease 5 (InQuad).
- Arrival 23bfeb0 returns the flight first, then spawns DownEfWin under Bottom,
  plays `coinBrust`, vibrates 200 ms, zeroes anchored position, moves to last sibling,
  resets the animation state and plays `animation` once, returning it on completion.
  ELF RELA 04f1e430 -> 0505eca8 (`coinBrust`), 04f1be48 -> 0505e010 (`animation`).
  Existing native Unity DownWinFlight/WinBurst resources provide this presentation;
  their audio/vibration events retain the current application integration behavior.
- Independently of the flight callback, wait .3 scaled seconds (UniTask timing 8),
  then query the shared FreeSpinRewards dictionary with the actual reel GameObject.
  Missing, zero, negative and NaN rewards do not accumulate. They still incur the
  second .3 wait before advancing to the next cell.
- A positive reward kills the previous numeric tween without completing it, captures
  old curFreeSpinReward, immediately increments that field, and starts .5 OutQuad
  counting from the captured old total to the new total. Setter 23bf690 formats two
  currency decimals without changing the field. ID 9 also increments curCoinSpinReward;
  ID 11 does not. TotalFreeSpinWin was already mutated when the ledger was recorded.
- At the end, ARM 23cfb34..23cfb48 explicitly gets GreenCount, adds curCoinSpinReward,
  and calls set_GreenCount, including when the increment is zero. Its existing
  recovered setter emits the pre-mutation event and saves twice. Then wait .2 and finish.
- Do not reset either counter in Begin or on each reel round. CheckFreeGame ARM
  23c95bc..23c95c4 writes eight zero bytes at Main +24c (both floats) on Free entry.
  ResetSession is bound to the existing RewardCountersResetRequested entry event.
  Repeated collection preserves cumulative counters and passes the cumulative coin
  field to the setter, including the original repeated-credit behavior.

## Verification scope

The new PlayMode test uses the actual FreeReels, stopped coin/ball prefabs, DownWin
text and pooled flight/burst resources. It checks live ledger insertion after Begin,
scaled pause, absent/nonpositive rows, flight release before burst, column/row rewards,
no credit before the full scan, coin-only credit, two setter saves, retained counters
across two collections, currency display, restored symbol scales and flight reuse.
It does not prove complete Main rendering or the unbound production lifecycle.

The existing two-ball scan integration test now binds this collector as well. It
delivers both external small-game callbacks, verifies automatic handoff, displays
61 in FreeReward with zero CoinReward, preserves the previously recorded total,
and verifies the zero-increment GreenCount setter still performs its two saves.

Validation: `Artifacts/free-collect-focused.xml` passed 1/1; after the callback
requery timing correction and automatic ball-scan handoff test, full PlayMode
`Artifacts/free-collect-all.xml` passed 344/344 with graphics enabled.
