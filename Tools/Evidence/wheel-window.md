# Wheel window and actual claim flow

This implements the window following `wheel-rotor.md`. Source is the current
`ReferenceOriginal/Res/ViewPrefabs/UIWheelView.prefab` and arm64 native functions;
no historical screenshots were used. The production Free entry/routing is still
incomplete and is not implied by these window tests.

## Recovered execution

`UIWheelView.OnBeforeShow` (0x23dd774) plays jump, restarts frame and pointer
`start` loops, selects/initializes the rotor and initializes the three jackpot
meters. Original meter transforms, child labels, bitmap font and TMP settings
come from this window, while their already-recovered native icon rigs are reused.
The authored entrance/exit is the inherited PopupWindow .3-second OutBack/InBack.
`DelaySpin.MoveNext` (0x23dee54) starts the rotor .5 scaled seconds after entrance.

`PlayEnd.MoveNext` (0x23df1fc) branches at spin completion:

* Cash requests wheelWin.
* Jackpot increments task(2,1), pauses music, requests Sound1 ring, maps to
  JakpotWinType and dispatches the jackpot animation. Both Main and Wheel receive
  it. Main's existing `PlayJackpotWin` is now public for this cross-window event;
  its implementation is unchanged, including resetting JpAddCount in the native
  icon completion callback before refreshing its displayed/stored amount.
  Wheel's callback is empty; its own meter still refreshes when its icon ends.

Both frame and pointer loop `win`, wait 1 second, then the winning item uniformly
scales to 1.2 in .3 seconds and back to 1 in .3 seconds using default OutQuad.
Scale constants were read from ELF 0xdbc4a0 and 0xdbc664. Callbacks are at
0x23de8a0 and 0x23de9b8. Cash hides Wheel and immediately opens actual UIRewardView
(`RecoveredBonusRewardPopup`) with the configured reward. Jackpot reads the
current Grand/Major/Mini balance at this later boundary, hides Wheel, stops Sound1
and opens actual UIJackpotView. Both popups overlap Wheel's exit animation.

Both existing popup controllers use actual configured claim multipliers and the
unchanged LocalAdFacade. Their FlyCoinRequested events bind to the existing
`RecoveredCashFlightPresenter` with Main placement. The originating callback
executes on completed cash arrival **before** the shared presenter credits the
player balance. No fake payout or replacement SDK implementation is introduced.

## Visual conversion

* Frame source `Res/Spine/zhuanpandi/ef_slzhuanpandi.skel.bytes`: 71,477 bytes
  fully parsed, 44 bones, 67 slots, 16 meshes, two 2-second clips (start/win).
* Pointer source `Res/Spine/zhuanpanzz/ef_slzhuanpanzz.skel.bytes`: 5,929 bytes
  fully parsed, 9 bones/slots, region attachments, two 2-second clips.
* `Tools/Evidence/ef_slzhuanpandi.json` and `ef_slzhuanpanzz.json` retain source
  hashes and complete parsed data. `prepare_wheel_art.py` emits flattened mesh
  inputs and independently computes 372 geometry samples around source keyframes.
* `BuildWheelWindow.Save` converts these with the existing native Unity rig and
  Animation pipeline, preserves the source window subtree and replaces the
  legacy Spine components. Textures use uncompressed PMA-compatible import.
* The frame and pointer retain source (0,-268), size 100x100, pivot (.5,.5).
  Main/mini/grand meter and title transforms are copied directly from the source.
* Title UIShiny is restored through existing RecoveredTitleShine: factor .5,
  width .25, rotation 135, softness/brightness/gloss 1, 2-second looping scaled
  clock, zero initial/loop delay, same original RectTransform mode.
* Static structure is authored in WheelWindow.prefab; runtime performs state,
  animation, data and event work. No static UI layout is created at runtime.

## Verification

Full PlayMode run `Artifacts/wheel-window-tests.xml`: **316/316 passed**.
`RecoveredWheelWindowTests` loads actual GameEntry, selects reachable native
weighted outcomes for Cash, Grand, Major and Mini using seeds, and checks .3+.5
autostart, scaled pause, both native icon recipients, frame/pointer start-to-win,
1-second delay, .3+.3 item pulse, exit/popup overlap, first-free and plain claims,
real fly-cash completion and credit order. During each jackpot return pulse the
test changes the live jackpot balance and verifies that the actual popup captures
that value rather than the earlier wheel-config amount. All four claims complete
once through the actual shared presenter.

The added geometry case validates all 372 source samples and zero allocations
in repeated pose sampling. In this batch run, 1,000 samples took about 12–14 ms
for each frame clip and 2 ms for each pointer clip (sampling only, not rendering).

Fresh 1080x1920 renders from this test were viewed:
`Artifacts/current-wheel-window.png`, `current-wheel-grand-win.png`, and
`current-wheel-grand-reward.png`. The Grand reward screenshot intentionally shows
the test's changed live amount. Background is the current reconstruction scene,
whose full visual fidelity remains unfinished.

## Outstanding integration

The original Free ball Wheel entry animation and actual ball-scan integration are
now implemented and tested in `free-wheel-game.md`. Production router binding,
Treasure and the larger Free lifecycle still need completion. This
window exposes the recovered music/sound and cash-out refresh events; production
audio/event consumers across all recovered windows remain part of the broader
unfinished integration. Global pool ownership and unexpected offscreen tween
lifetime still require audit. Passing these tests proves this recovered window
path, not the whole game's 1:1 completion. SDK handling remains unchanged.
