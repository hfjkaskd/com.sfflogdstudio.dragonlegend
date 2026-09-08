# Free ball Wheel entry

The original `UIMainView.CheckSmallGame` state machine at RVA 0x23cfe28 selects
the weighted branch before invoking this presentation. Its Wheel arm uses
`WheelPrefab` at main-view offset +0x150; unlike Slot, it does not increment
task 4. The branch controller is now `RecoveredFreeWheelGame`, with a configured
`FreeWheelGame.prefab` containing the original icon and the actual WheelWindow.

## Source behavior and assets

* Current `ReferenceOriginal/Res/ViewPrefabs/UIMainView.prefab`:
  WheelPrefab GO 1708180620337799, RectTransform 224626119028408548, initially
  inactive, size 208x217, centered anchors/pivot, local scale (1,1,1).
  Sprite GUID 97a397da935533d4cb54640cdb10fb17 resolves to
  `Res/UI/free_game/mfyx_icon_zhuanpan.asset`.
* `BuildFreeWheelGame.Save` copies those exact source YAML objects, replaces only
  the Image script/sprite references and reparents the icon under the authored
  bundle. No runtime construction of static UI or layout is used.
* Entry places the icon at the supplied world position and activates it. It
  captures the current X/Y scale, then scales X/Y by 1.5 with Z=0 in .3 seconds.
  The completion callback at 0x23bfbf0 starts a second .3-second scale back to
  captured X/Y, again with Z=0. Both use default OutQuad.
* An independent scaled .6-second wait hides the icon and opens UIWheelView with
  the caller's Action<float>. It does not await the second tween completion,
  draw a separate reward, increment a task, or credit the player.
* Wheel's existing OnBeforeShow-equivalent owns RandomWheelWeight; the result
  draw therefore happens after the icon delay. WheelWindow supplies the actual
  cash/jackpot popup and shared cash-flight completion callback, which is passed
  back to the waiting Free ball scan.

The wrapper preserves the independently running pulse/wait and does not reset
the current scale on repeated entry. All timing/scale parameters and prefab
references are serialized. No third-party tween or asynchronous assembly is used.

## Verification coverage

`RecoveredFreeWheelGameTests.EntryUsesOriginalIconAndDefersWheelSelectionUntilAfterItsPulse`
loads actual GameEntry and FreeWheelGame, dispatches through the real router with
a deterministic Wheel branch configuration, and checks source placement/size,
the unchanged Slot task count, paused clock/RNG, the half-pulse OutQuad sample,
peak scale/Z, hidden entry, deferred native Wheel selection, actual cash claim,
and caller-before-credit ordering. Its fresh render is
`Artifacts/current-free-wheel-entry.png`.

`TwoStoppedBallsClaimCashThenGrandAndResumeTheActualFreeScan` uses a two-ball
generation/branch fixture with real FreeReels, NPC, ball flight/activation, router,
FreeWheelGame, WheelWindow, both reward popup types and the shared cash presenter.
Only selection is made deterministic using seeds; reward rules are actual
GameEntry rules. It verifies release of each flown ball before branch entry,
Cash then Grand outcomes, scan blocking until each claim, per-reel paid ledger,
balance/TotalFreeSpinWin updates, original stopped-ball reward text animation,
and exactly one final scan completion. There are no synthetic payouts or
immediate reward callbacks in this integration.

Full PlayMode run `Artifacts/free-wheel-tests.xml`: **318/318 passed** with
graphics enabled. The current 1080x1920 entry capture was inspected after this
passing run. Initial authoring was refused because another instance held the
project; after that instance exited and its lock disappeared, authoring completed
successfully (`Artifacts/free-wheel-authoring-retry.log`). No game logic was
changed to work around the editor lock, and other projects were left untouched.

## Scope remaining

This makes the real Wheel branch available to the existing Free small-game router
and verifies its end-to-end consumers. It does not yet bind all branches into the
production Main/Free lifecycle: Treasure and that complete routing remain to be
implemented, together with the broader previously documented visual/lifecycle
gaps. SDK handling is unchanged.
