# Continuous collection from zero

Baseline 63369cdac8bc074846351f2855c39ce5ef254399.
RecoveredZeroCollectionSessionTests loads the current production GameEntry
scene with a fresh player preference record, verifies all five collection
counts are zero and IsBonusGame is false, and performs actual paid Spins
until Bonus has appeared and its owning core round finishes.

The fixture skips the already-separately-tested guide by setting GuideStep=3
and hiding its presentation. It seeds Unity RNG once (71) before scene load
and accelerates frame time with captureDeltaTime=.05. It does not modify
collection counts, readiness, generated cells, result callbacks, awarded Free
counts, rules or balances, and does not reseed between Spins. Therefore this
is a post-guide zero-collection session, not an unmodified first-install run.

The driver claims active/interactable standard Button callbacks for encountered
reward windows, handles Free and its minigames if encountered, chooses Bonus
free cards and closes the window, and handles end-of-round prompts. These are
callback-driven inputs, not a physical-device touch or occlusion test. The
driver can request MoreSpin and complete the existing local ad facade if
needed; the observed eight-spin run did not exhaust the initial ten Spins.
No external SDK or financial actions are involved.

At Bonus appearance the fixture checks the original consumed state: all five
counts reset and readiness false. At the end it verifies no busy latch,
completed-round count equals accepted paid Spins, Base mode and persisted
player JSON equals the current record. Core/playfield/Bonus errors fail it.
The session has explicit 120-second and 60-paid-spin bounds.

Unity process 6316 exited. Artifacts/zero-collection-tests.xml passed 1/1 in
3.5524019 seconds: 8 paid Spins, 8 core completions, 1 Bonus window.
This closes the zero-progress continuity coverage gap for this observed run;
it does not establish all RNG outcomes, every fallback driver branch, fresh
install guidance, every region profile or visual parity. Runtime and SDK
implementation did not require changes for this case.

## Fresh guide connected to the same continuous session

Follow-up baseline 642212b9512e8ab88dcb6a385504bf55b1afbf5e adds a second
case without the GuideStep/guide-visibility override. It verifies the actual
initial step 1 and visible Spin guide, the first paid Spin changing it to 2
and hiding that guide, and the actual free MoreWild guide after the first
core completion. It claims that Button exactly once, checks step 3 and the
configured MoreWild value without an ad request, and waits for the window to
close before accepting subsequent Spins. No count/board/readiness overrides
were introduced. The first post-guide comparison case remains unchanged.

The current native sequence is preserved: FinishCoreRound shows free Wild
before releasing the core busy flag, without waiting for the claim. The test
driver nevertheless respects the active modal window before another Spin.
MoreWild's free claim increments the guide step and sets configured Wild.
This does not add a new runtime lock or force a stronger latch than the source.

Unity process 31592 exited: Artifacts/fresh-guide-session-tests.xml passed
2/2 in 11.7664484 seconds. The post-guide case recorded 8 paid/8 completed
Spins and 1 Bonus; the intact-guide case recorded 27 paid/27 completed Spins
and 1 Bonus, ending at guide step 3 with the MoreWild guide hidden, Base mode
and matching saved JSON. The existing driver replenishes exhausted Spins
through MoreSpin and the existing local ad facade; no direct grants are made.

The fresh case covers the current scene's guide-to-collection session, not
the original app's SDK boot screens, physical taps, every random result or
all fresh-install region profiles. It keeps the single seed and accelerated
frame timing described above. No production/SDK changes were needed.
