# Bonus window binding

The current `ReferenceOriginal/Res/ViewPrefabs/UIBonusView.prefab` is the
authoring source for `RecoveredUI/BonusWindow.prefab`. `BuildBonusWindow.Save`
preserves the source hierarchy, RectTransforms, static images, labels and layout
components, then replaces recovered game/plugin components with native Unity
components and the already converted card/Jackpot prefabs. Unknown external
GUIDs abort authoring. Button listeners are code-bound.

The 12 visible cards keep serialized sibling order, including `Bonus (11)` at
index 7. The randomized reward pool can contain more than 12 entries; the native
window completes on its visible child count, not the pool count. Free chances
come from the active `Ronig.RrggRimgg[0]`, not a fixed 9.

`RecoveredBonusWindow` connects the recovered selection, cash-card and character
actors to actual nested Bonus reward/Jackpot popups and the shared GameEntry
cash-flight service. Normal cash releases selection before starting flight at
the card transform. Jump and Jackpot cards retain selection until popup payout
flight completion. No direct balance credit is added to the window.

Hint timing follows `239aaf8`, `239bdfc`, `239bea8`: rejection-sample an unselected
visible card, wait 1.83 scaled seconds, show, wait 1.83, hide and sample again.
The constant at Ghidra `00ebc728` / ELF `00dbc728` is
`1.8300000429153442f`. Keeping this sampling matters to later Unity RNG draws.
Actual pooled finger art is still pending; the window exposes the show/hide
events and preserves the selection/timing behavior.

## Validation

`RecoveredBonusWindowTests` instantiates the actual authored window and actual
GameEntry services. It checks native Button bindings, missing components,
12-card order, active configuration text, all 12 ordinary cash reveals including
the ad threshold and final-card release, the nested jump popup/ad claim, and
character collection through the nested Jackpot popup. Payout assertions use
the shared flight and real progress balance, not a stub credit callback.

Unity 2022.3.62f3 PlayMode full regression: **247/247 passed** in
`Artifacts/bonus-window-full-tests.xml` (2026-09-08). Current base-window and
nested Jackpot renders were inspected at their original 1080x1920 resolution.

Current 1080x1920 renders are written to
`Artifacts/current-bonus-window.png` and
`Artifacts/current-bonus-window-jackpot.png`. These are freshly rendered from
the current prefab; they do not constitute a full device-to-device visual parity
claim. Deterministic test configs exercise payout branches; the initial window
capture uses GameEntry's active config.

## Remaining main-flow work

This prefab is not yet connected to the main `CheckBonus` entry/completion
source. Close/exit are explicit requests, not a fabricated hide or spin unlock.
The source exit methods still need their coordinator: `239b3a4` (close click),
`239d1e8` (HideBonusView state machine), `239ccc4` (CloseBonusView state machine),
`239d5f4` (WaitZhuanChang), and callbacks `239bc88/90/98`.

Nested popup sorting 301 is an integration setting; complete UIManager window
stack/sorting parity remains unverified. Full source finger conversion, audio
event binding, main entry/exit transitions, Free/BaseEnd continuation and the
broader lifecycle audit remain required. This is not complete 1:1 reconstruction.

Existing untracked official PackageManager/URP project settings are retained in
the checkpoint, rather than deleting local project state.
