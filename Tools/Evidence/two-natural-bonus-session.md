# Two naturally collected Bonus sessions

Baseline 76464fc. Extended RecoveredZeroCollectionSessionTests with two Bonus
entries in one GameEntry scene. Seed 71 is set once at startup; collection
starts at zero and no board, collection or Bonus-ready state is injected.
The new case skips the initial guide as the existing post-guide case does.

Twenty paid Spins complete, reaching Bonus twice. Both entries clear the five
collection counters. The second entry uses the same window and selection
objects, resets revealed cards and retains NeedsAd, matching the ordinary
cached-window source path documented in bonus-window-cache-audit.md.
Its first six cards each wait for the existing bonusCoin advertisement facade
and are accepted through the existing GM reward Button. Both sessions close
normally, release the core round, and persist the completed player record.

The initial run passed both original cases but failed the old endpoint
assumption that Spins must remain after reload: this session used exactly 20.
The test now exercises the real zero-Spin window after reload, claims its
existing simulated advertisement reward, then verifies the next paid Spin
debits exactly once. No runtime or SDK behavior was changed.

Final Unity 2022.3.62f3 PlayMode run: PID 27556 exited, 3/3 passed,
51.7655931 seconds, Artifacts/two-natural-bonus-final-tests.xml.
Original post-guide case: 8 paid/completed rounds and one Bonus.
Original fresh-guide case: 27 paid/completed rounds and one Bonus.
New repeat case: 20 paid/completed rounds and two Bonuses.

These tests invoke production Button callbacks, not physical touchscreen or
EventSystem raycasts. This establishes the reconstructed scene's continuous
state flow, not a second natural Android session or complete visual parity.
Original external lifecycle listeners and explicit window destruction remain
outside the source-cache audit. The full suite was not rerun this round.
