# Main Spin → Bonus entry/exit integration

`RecoveredBonusFlow` is now instantiated from the GameEntry prefab and subscribes
to the actual `RecoveredSpinPlayfield.SymbolSequenceCompleted` stage. The source
ordering and native addresses are recorded in `bonus-transition.md` and
`bonus-exit.md`.

The flow calls the existing PrepareBonusGame predicate/save path, plays NPC state
2, requests PauseMusic/ring, waits 1.8 scaled seconds, stops Sound1, requests
transform and launches the world transition. Its cover callback opens the actual
Bonus window; the second callback refreshes main bonus-collection indicators from
the reset player area and requests bonusBg. The exit controller resolves its
source, then the flow waits another .5 scaled seconds before emitting Completed.
If no Bonus is due, Completed runs synchronously with no presentation.

GameEntry releases this flow before its cash service/playfield on profile switch.
The transition is a separately instantiated authored world prefab, never scaled
or reparented into the UI canvas. NPC is nested in the actual QiPan at the source
local layout, with its shake targeting the real board. Its previously isolated
QiPan-size wrapper stretches over that board; child coordinates remain unchanged.

## Camera evidence and fresh rendering

Current original `Scenes/Main.unity` has UI camera at (100,0,-10), orthographic 5,
layer mask 32, and a world transition at (0,0,20). The URP extra-camera data makes
UI the Base camera and Camera (world/perspective) its Overlay. The reconstructed
entry scene already preserves this stack; no camera coordinate or projection
workaround was introduced to show Bonus.

UIBonus OnInitProperty 239949c writes native tuple (300,2) at UIWindowProperty
+10/+14 (window type, background-mask mode), verified from Ghidra 00ebb670 / ELF
00dbb670. The flow currently uses Canvas order 300 for Bonus and the existing 301
for its nested rewards; full UIManager sorting/background-mask behavior still
requires its broader recovery, rather than inferring all stack behavior from the
window-type number alone.

`RecoveredBonusFlowTests` loads the actual entry scene, forces the existing
runtime IsBonusGame trigger, clicks Spin, processes the real pre-Bonus rewards,
checks area reset, NPC state, 1.8/4.8-second entry milestones, selects the configured
free cards and claims their actual popups, then clicks the real CloseBtn. It
verifies completion at approximately 3.7 seconds after close (.8 + 2.2 + .2 + .5),
and the synchronous no-Bonus path. The following Free/BaseEnd stage remains
unimplemented and Spin remains busy intentionally; no false round completion is
introduced.

Fresh current-scene renders:
`Artifacts/current-bonus-flow-cover.png` and
`Artifacts/current-bonus-flow-window.png` (1080x1920). The capture camera target
is assigned before Canvas layout, avoiding a landscape GameView layout being
cropped into a portrait output. Tests verify all twelve card centers remain in
the output viewport. Existing GM selectors are visible; these captures are not
a claim of complete source UI parity.

SDK handling remains unchanged. Sound/music requests still need the pending
native audio binding; complete finger art, UIManager lifetime/stack behavior,
Free/BaseEnd continuation and the wider game lifecycle remain outstanding.

Unity 2022.3.62f3 full PlayMode regression: **249/249 passed** on 2026-09-08
(`Artifacts/bonus-flow-full-tests.xml`). The authoring helper is also called by
BuildSpinPlayfield so rebuilding the playfield retains the required NPC binding.
After the final authoring-helper change, the actual-scene integration passed again
in `Artifacts/bonus-flow-final-tests.xml` and regenerated both current captures.
