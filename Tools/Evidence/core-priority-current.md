# Core gameplay priority: current baseline

User direction: prioritize core gameplay before further peripheral UI work.
Current review baseline: 95a118e294073a7eaec8aa1c5436cb67f09fc30b.

## Current state and next core work

The older run below is historical. It is not the current missing-feature list.
Current runtime includes RecoveredCoreAudio and its native AudioSource consumer;
see core-audio.md, core-reel-audio.md and spin-entry-audio.md. Cash prompt/entry
integration also exists; see core-cash-prompt.md and top-withdraw-entry.md.

Mixed BigWin -> ready Bonus -> Free coverage now includes all six awarded Free
rounds, four coins, six balls, total and Base return. The latest targeted run
passed 3/3 (full-mixed-free-ledger-tests.xml); see mixed-bigwin-free.md. That
fixture still sets IsBonusGame before the Base Spin. It does not prove the
preceding actual Base coin collection causes Bonus readiness in that same
continuous mixed path. This is the next concrete core coverage target: start
from defined collection progress, use actual generated Base coins and arrival
callbacks to complete it, and observe Bonus consumption before the pending Free
branch without setting the readiness flag or replacing board cells.

Spin routing now exercises 21 real scene-raycast click-handler deliveries with
one debit/start and no busy-click state mutation (spin-input-routing.md).
This does not cover physical multi-touch or long press. Main scene/camera
hierarchy and symbol-catalog source comparisons are recorded separately in
main-ui-hierarchy.md, main-scene-audit.md and symbol-catalog-audit.md.

Core branch coverage, native timing/visual comparison and actual generated
collection transitions remain ahead of peripheral focus refreshes. The missing
focus event remains documented in application-focus.md. Complete region/AB
coverage, current original-APK visual comparison and full lifecycle equivalence
remain unproven; passing selected fixtures does not finish the user objective.

## Native Unity implementation check at current baseline

- RecoveredReelView instantiates the authored symbol prefab; RecoveredSymbolView
  uses SpriteRenderer for symbol and cover, with SpriteMask clipping. The main
  reel symbols are not UI Image gameplay elements. This observation does not
  classify every object in every minigame.
- Packages/manifest.json has 12 direct dependencies; packages-lock.json has
  21 entries. Every locked name starts with com.unity.; sources are builtin
  or registry, and every registry URL is https://packages.unity.com.
- No .dll, .so, .aar, .jar, .a or .dylib files were found under Assets using
  rg --files. Runtime asmdef references Unity.ugui and Unity.TextMeshPro;
  the test asmdef additionally references the project runtime and official URP.
  This checks declared dependencies and these binary extensions, not source-code
  provenance or every possible native-plugin packaging format.
- Manifest SHA256: 48d3da14412c699b0078096b0592f6e583f12af8bad5df6fa20c68f29d5ef360.
- Lock SHA256: 11ec4420bd0cf11e21b694791d4371ce05afbbfe37488778078fbad6f8bcd90c.

No new Unity run or production mutation was needed for this read-only audit.
SDK handling remains unchanged.

## Historical baseline at 227c00a

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

At that historical revision, the next work was mixed Bonus/Free sequences and cancellation/retry through
the existing advertisement facade; recover the missing common audio consumer.
That historical Whitebox runtime emitted/forwarded sound events but contained no AudioSource
or PlayOneShot implementation, and GameEntry did not subscribe to those sound
requests. This gap has since been implemented; see the current state above.
SDK behavior stays unchanged. Cash-window presentation work is deferred behind
these core priorities; its outstanding end-flow integration remains documented.

Older evidence files describe their own implementation dates. Statements there
that all four minigames or the core continuation are absent are historical and
must not be used as the current backlog without checking later evidence/runtime.
