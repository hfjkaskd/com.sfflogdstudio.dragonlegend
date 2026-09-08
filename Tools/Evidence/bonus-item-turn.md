# Bonus item opening sequence

`RecoveredBonusItemTurn` and `RecoveredUI/BonusItem.prefab` implement the opening
presentation of original BonusItem.PlayBonusAnim (MoveNext `2396d54`). The typed
Bonus round already chooses the card and jackpot target; this component consumes
the accepted card type. It does not reroll the card, credit money or complete the
Bonus window.

## Authored structure

Reference: current `reference-unity/ExportedProject/Assets/Res/ViewPrefabs/UIBonusView.prefab`,
first Bonus component `114085134620244786` and descendants.

- Bonus root: 100x100, UI layer 5; the window will supply each source position.
- Body and glow: nested native ef_jinbi artwork, 187x172, position zero,
  pivot (0.49999967,0.5), original layer 0.
- Reward Text: original bitmap `Green.asset`, font size 0, middle centered,
  horizontal overflow, vertical truncate, size zero, position (0,4.6).
- Ad Image: original `b_btn_bofan` sprite, 90x92, position (0,4.6), layer 5.
- The source has a separate transparent 234x242 Button Image. To satisfy the
  project's explicit prohibition on separated visual/click regions, the standard
  Button is attached directly to the visible body Graphic with raycastPadding
  (-23.5,-35,-23.5,-35), preserving its original hit dimensions and center.
  Transition remains None. Listeners are registered in code; no persistent events.

## Timing and handoff

`Initialize` implements `23959f4`: hides glow, reward and ad and plays zcjb_idle.
The Button enable operation belongs to window InitBonusItems (`239a53c`).

`Begin` sets body playback speed 3 and plays the type's one-shot turn. Its completion
starts the corresponding looping hold and sets a private turned flag. The shared
recovered WaitUntil runner observes this flag, restores speed 1 and starts the
independent glow one-shot; the glow hides on its own completion.

For Reward only, an independent scaled 0.2-second delay precedes displaying text
and calling GetBonusReward. The resulting amount is formatted with the supplied
language and precision 2. Text scales from its **live** scale to 1.2 over 0.2 seconds
and then back to 1 over 0.2 seconds, using native default OutQuad. This scale sequence
does not gate the turned flag or the next payout stage. ELF float constants at
Ghidra 00ebc4a0 and 00ebc540 resolve to 1.2 and 0.2.

The `ready` callback marks only this visual handoff. It must not be wired directly
to original `UIBonusView` input-release callback or the Bonus completion source:
ordinary, jump-reward, non-completing character and jackpot-completing character
branches have different later release points. Selection events also remain
separate from accepted-card Begin so the window can perform its native input/ad
gate before starting animation. Cancellation stops pending waits and animation
callbacks without drawing a delayed reward.

## Remaining payout flow established by native evidence

- `2396104`: destination arrival requests sound, despawns the flying object,
  hides destination's first child, spawns the target effect, dispatches jackpot
  animation event and plays the effect; its completion is `239674c`.
- `239674c`: despawns that effect; only a jackpot-completing character reads the
  **current** relevant jackpot reward (GameData offsets 4c/50/54), then starts
  OpenJackPot.
- OpenJackPot MoveNext `239692c` waits scaled **1.5 seconds** before invoking the
  captured popup callback `23964bc`.
- `23964bc` opens the existing jackpot popup with jackpot type, captured amount
  and callback `2396714`. That callback ignores its float and invokes original
  input-release `call`.
- Reward-jump popup callback `2396730` likewise ignores its float and invokes call.

The target effect, flight, reward/jackpot popup wiring, full window and main-flow
entry/exit are still required. SDK behavior remains unchanged.

## Verification

`RecoveredBonusItemTurnTests` instantiates the authored prefab and checks hidden
initial state, standard Button/Graphic ownership and bounds, ad visibility,
pause, delayed reward RNG, type-specific text visibility, independent text scale,
turn-to-hold/glow timing and cancel-before-reward behavior. It renders the current
character and cash face to `Artifacts/current-bonus-item-reward.png`.

Unity 2022.3.62f3 full PlayMode run `Artifacts/bonus-item-tests.xml`: **233/233 passed**.
The fresh PNG was inspected at original resolution; the original bitmap font
renders `$9.63` from the configured integer amount 963 using existing currency
formatting. This is an isolated item rendering, not proof of full Bonus completion.
