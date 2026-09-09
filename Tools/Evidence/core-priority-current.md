# Core gameplay priority: current baseline

User direction: prioritize core gameplay before further peripheral UI work.
Baseline revision: 227c00ae221173930aa397676cb7ddb584ed41e3.

Unity 2022.3.62f3 ran the current production GameEntry scene with graphics enabled.
Artifacts/core-priority-current.xml reports 4/4 passed (20.875391 seconds);
Unity process 50820 exited. No runtime or prefab changes were needed for these
selected paths. This is a fresh verification, not a new gameplay implementation.

- RecoveredCoreRoundFlowTests: actual Spin duplicate-click debit guard, ordinary
  settlement, Free entry, Free end/return lock, final save, subsequent Spin.
- RecoveredCoreBallBranchesTests: all four actual ball minigame routes, their
  claim Buttons, cash arrival and return to settlement.
- RecoveredCoreRepeatedFreeTests: two connected Free rounds with multiple balls,
  serial minigame entry and the native cumulative coin-credit behavior.
- RecoveredFreeGMTeardownTests: profile rebuild during acceleration and partial
  stopping cancels old reel work and leaves the replacement Spin usable.

Fixtures deliberately control RNG and shorten Free counts; these passes do not
prove all mixed Bonus/ball interleavings, all advertisement outcomes, all country
profiles, visual parity or complete game lifecycle equivalence. Fixtures restore
the original player preference record, RNG and time settings.

Next core work: verify mixed Bonus/Free sequences and cancellation/retry through
the existing advertisement facade; recover the missing common audio consumer.
Current Whitebox runtime emits/forwards sound events but contains no AudioSource
or PlayOneShot implementation, and GameEntry does not subscribe to those sound
requests. Audible core presentation therefore remains a concrete integration gap.
SDK behavior stays unchanged. Cash-window presentation work is deferred behind
these core priorities; its outstanding end-flow integration remains documented.

Older evidence files describe their own implementation dates. Statements there
that all four minigames or the core continuation are absent are historical and
must not be used as the current backlog without checking later evidence/runtime.
