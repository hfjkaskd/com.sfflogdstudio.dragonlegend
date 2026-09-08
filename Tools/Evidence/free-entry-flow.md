# CheckFreeGame entry coordinator

The next native branch is materially different from a Base reel restart. The
source Main prefab contains five FreeRoll groups, each with three independent
RollReels. `InitFreeReels` 23bd420 initializes those fifteen reels once, guarded
by UIMainView.isFreeInit (+1ec); FreeRoll.Init 23b8440 assigns each row and calls
CheckFakeCoin(true). Those visuals, fake coins/balls and Free roll operations are
still absent and must be implemented before enabling this coordinator as the
production BonusFlow.Completed consumer.

`RecoveredFreeEntryFlow` and its authored prefab now implement the entry
coordination using actual existing Scatter/NPC/start-popup/transition resources.
They are exercised in the current GameEntry scene by the integration test, but
**are not yet automatically instantiated/subscribed by GameEntry**. Cover events
are explicit pending consumers, not an implemented Free board or mode switch.

## Source order

Native CheckFreeGame MoveNext 23c90bc:

1. GetFreeSpins(ScatterCount); if positive, SetTaskData(3,1), event (5,1), set
   isFreeSpinEnd=true, GameSlotType=Free, FreeSpinCount=initial, TotalFreeSpinWin=0,
   curFreeSpinReward=curCoinSpinReward=0, tFreeSpinCount=initial.
2. PlayScatterAnim for every reel's current Scatter list; InitNpc(2), PauseMusic,
   ring; wait 1.8 scaled seconds, StopSound1; show actual UIFreeSpinStart and await
   its completion source (including its exit).
3. Play transform, call FreeSlotGameResult.InitFreeGameResult with current Free
   symbols and null callback; launch GManager.PlayZhuanChang.
4. The CheckFreeGame task completes after launch, without awaiting either
   transition callback. It does not itself mark the entire spin complete.

ELF RELA metadata resolves 0501e6b0 / 0501e6b8 to callback methods 23bf73c /
23bf75c. Raw ARM64 disassembly confirms their exact tail calls, avoiding expanded
decompiler output being mistaken for a larger callback:

- First callback: SetInitShow 23bca34, InitFreeReels 23bd420, tail call
  InitFreeSpinTimes 23bf060 (the displayed count is current FreeSpinCount).
- Second callback: ChangeBGM(freeBg), tail call FreeAutoSpin 23bf158. It consumes
  one FreeSpinCount and generates a **new** Free result before the existing
  FreeSpinEntry.PresentationRequested callback. The pre-transition generation
  cannot be skipped as redundant: it initializes first-show effect data and
  consumes the original random sequence.

The existing world transition invokes these callbacks at .8 and 3 seconds from
launch, while its artwork finishes independently. Entry state flags and initial
count are exposed by the coordinator; RewardCountersResetRequested represents
the two still-pending Free view counters. It never calls CompleteBaseRound or
unlocks the Base Spin button.

## Implementation and verification boundary

The coordinator uses RecoveredRewardBranches' existing task/count mutations and
the unchanged SDK facade through FreeStartPopup. It completes initial generation
before transition launch, then uses existing FreeSpinEntry for the later debit
and regeneration. It advances the existing step-based generator with an authored
256-step/frame budget; ordinary valid configurations finish synchronously in the
same call, while impossible placement configurations remain pending without
clamping data or running an infinite main-thread loop. This scheduling limit is
an existing reconstruction architecture constraint, not recovered native code.

The integration test loads the actual current GameEntry scene and explicitly
binds the coordinator. It verifies no-Free synchronous completion, Scatter/NPC,
scaled wait, actual plain Button/window exit, generation before launch,
CheckFreeGame completion before cover, callback ordering and .8/3-second timing,
the later count debit and generation completion. It captures the actual start
window with the current authored camera stack in
`Artifacts/current-free-entry-window.png`. It does not claim the pending fifteen
Free reels or the rest of Free gameplay works.

Also corrected the previously documented all-Wild1 stop branch: the actual stop
pass now requests scatterShow once per row, three times for an all-Wild1 column,
without spawning Scatter effects or adding vibration. RecoveredScatterTests
verifies this source behavior.

Remaining integration: actual Free view mode switch/backgrounds/count display,
fifteen native reels and initial coin/ball effects, auto spin presentation and
reward/exit loop, then production binding after Bonus and BaseEnd continuation.
Shared audio/finger/UIManager lifetime work remains open; SDKs are unchanged.

Unity 2022.3.62f3 full PlayMode regression: **256/256 passed**
(`Artifacts/free-entry-full-tests.xml`). The fresh 1080x1920 current-scene window
capture was inspected at original resolution. Existing GM profile selectors
still overlay its upper region; complete UI stack/layout parity remains unproven.
