# Free ball Treasure entry

`RecoveredFreeTreasureGame` and the authored `FreeTreasureGame.prefab` restore
the Treasure arm of `UIMainView.CheckSmallGame` (RVA 0x23cfe28), using the real
TreasureWindow, collection persistence, claim logic and cash-flight presenter.

## Native evidence

- Main field +0x158 is `CardPrefab`: source GO 1220697728379336, RectTransform
  224909888333327498, CanvasRenderer 222571634487674524 and Image
  114057666396558467 in `ReferenceOriginal/Res/ViewPrefabs/UIMainView.prefab`.
  It is inactive, centered, 390x459, scale (.4,.4,.4), with sprite GUID
  27b6ea552c8080340b36ee5350e3df1d (`Res/UI/pop-up/tc_sc_bak02`).
- The native branch sets world position, resets all three scale components to
  .4, and activates the icon. It calls RandomCollectIndex repeatedly while the
  returned ID equals PlayerData.RandomIndex, then SetCollectData(id,1) before
  showing the window. It does not mutate Slot task 4 or wait for a pulse.
  RandomIndex's ordinal initialization versus returned collection IDs is an
  original mismatch, deliberately retained. No retry limit or fallback draw is
  introduced.
- Callback 0x23bf8ec captures the supplied source and EndPos's current world
  position, then calls FlyAnimUtils.Fly (0x238c74c) with duration .6, height -1,
  ease 4 (InOutSine), and zero side offsets. ELF float 0xdbc450 is .6;
  negative height uses distance*.3 (constant 0xdbc664). The control point is
  midpoint + world-up * distance*.3, with a quadratic Bezier path.
- It separately registers DOScale(1,.6).SetEase(4) after the position tween.
  Callback 0x23bfa78 dispatches event `11` with the original icon to
  RefreshCollectCard (ELF relocation 0x4f1e410 -> 0x504afc8, ScriptString `11`). The
  window resets/hides it and starts the real flip. Position and scale run as
  independent jobs in that registration order, so the final scale write occurs
  after the reset. No persistent UnityEvent or third-party tween package is used.
- Main's caller remains pending through flip, claim/count, exit and actual cash
  arrival. The Free ball scan records the paid amount and resumes through its
  existing callback; this wrapper does not credit cash itself.

The authoring command copies the original icon YAML and remaps its Image/sprite
to official Unity assets. Static hierarchy, geometry, scale, timing and arc
height ratio are configured in the prefab. Runtime only manipulates the
configured objects and schedules animation jobs, including while disabled.

## Verification

`RecoveredFreeTreasureGameTests` verifies rejection draws and subsequent RNG
state, saved JSON already present at window jump/show, source icon geometry,
paused animation, frame-by-frame flight/scale samples through the midpoint,
arrival/reset/flip, collection tip, real claim and caller-before-credit order.
It also runs two actual stopped Free balls through NPC flight/activation, the
real branch router, Treasure entry/window, cash arrival, paid-reel ledger and
TotalFreeSpinWin, and checks one final scan completion. Branch selection is a
deterministic fixture; rewards and collection rules use GameEntry's real config.
The fresh entry render is `Artifacts/current-free-treasure-entry.png`.

The full PlayMode run (`Artifacts/free-treasure-tests.xml`) passed 335/336;
the remaining test caught Unity replacing the imported root name with the
temporary asset filename. The authoring command now explicitly restores native
`CardPrefab`. After regenerating the prefab, both Treasure integration tests
passed (`Artifacts/free-treasure-focused.xml`, 2/2). No runtime changes followed
the full run. The fresh 1080x1920 midpoint capture was inspected and shows the
native card back above the current Main scene; the oversized dragon is still
an existing Main presentation discrepancy, not evidence of complete fidelity.

## Remaining lifecycle work

This branch bundle is callable by the actual Free router; full production Main
Free-board lifecycle wiring remains incomplete. Treasure departure event 14
now uses the real pooled image and original animated destination in
`RecoveredTreasureDeparture`; see [treasure-departure.md](treasure-departure.md).
Its expanded full regression passed 337/337. The collection Button action,
unlock/profile visibility, event 7(4,1) consumer, collection redemption, Free
end flow and the broader Main visual/layout discrepancies remain unfinished.
