# Free entry: original start window and native artwork

Source: current reverse `reconstruction/mumu-current`, original
`ReferenceOriginal/Res/ViewPrefabs/UIFreeSpinStart.prefab`, ARM64 methods below.
This is a prerequisite for the still-pending actual Free entry flow. The window,
its buttons/count font/finger and the Scatter → Free transition are not yet wired.

## Verified resource identity and native conversion

The node named `SkeletonGraphic (ef_caidai)` references GUID
`ef817fd5a8e939f439a934d4bf9e8ec7`, which the original GUID map resolves to
`Res/Spine/xianjinpl/ef_xjpl_SkeletonData.asset`. Its clip is `idle`, looping at
speed 1 on scaled time, duration 5.000000476837158 seconds. Its RectTransform
size is (99.02252,70.73037), pivot (.5,.5), position (0,-5.5012817).

The main art references `7bb743abb54731747ae3573179012757`,
`Res/Spine/ef_starttc/ef_starttc_SkeletonData.asset`, clip `starttc`, looping at
speed 1 on scaled time, duration 2 seconds. Its size is (953.99994,838.45105),
pivot (.47798738,.3499919), position (0,0).

`ef_starttc.json` and `ef_xjpl.json` are complete binary extractions; the parser
consumes 15111 and 12681 bytes respectively. `verify_wild_extraction.py` verifies
both against original binary hashes and re-extraction without modifying inputs.
`prepare_free_start.py` flattens weighted mesh authoring data and independently
samples 268 geometry frames, including both sides of every timeline key.
`BuildFreeStartArt.Save` generates native Animation clips, pose assets and
CanvasRenderer mesh prefabs using the existing native rig. No Spine runtime or
third-party assembly is introduced. Texture loading remains path-based.

## Next implementation: behavior verified from actual native code

- BeforeShow `23b1478`: fsstart sound; reset isClick=false; read extraCount from
  GetExtraFreeSpins `236bf34` (`Rrggiomg.GjrroRrggGping[0]`); take initial spinCount
  and completion source from context; display count, starttc loop,
  `<sprite name="tc_btn_bofang">FREE+{0}`, START; show finger under AdTxt.
- OnClickButton `23b17f0`: BaseWindow.OnClickButton is empty. If isClick is true,
  return; otherwise store **false**, then hide finger. Do not reuse a claim
  controller that locks the flag to true.
- **Both ClaimBtn and UnPlayBtn play click.** ARM64 `23b1934` and `23b1a34`
  each call SoundManager.PlaySound using the same string pointer. This corrects
  an earlier reading that missed ClaimBtn's sound. ClaimBtn calls existing SDK
  reward-ad facade with `freespin` for both placement fields.
  UnPlayBtn hides directly, with no interstitial.
- Ad success `23b1b24` starts PlayAd `23b1bf8`; failure `23b1b38` resets false.
  PlayAd immediately assigns FreeSpinCount=initial spinCount+extraCount. It starts
  a .3-second integer label tween, separately waits .5 scaled seconds, then
  calls BaseWindow.Hide. Getter `23b1b40` returns initial spinCount; setter
  `23b1b48` changes only text. No accumulating increment or save is present.
- AfterHide `23b1af4` resolves the supplied completion source after BaseAfterHide.
  Integer tween rounding/default easing and shared window lifetime still need
  exact confirmation before implementing those parts.
- Count font is original `Res/UI/free_game/FreeCountFont.asset`, GUID
  `fdad6d938793c5345a902a4a73582c27`, with custom digit UV/advance data and material
  `079cd9e8e97f3a647a7c098897b6117f`. Do not substitute a system font.

The native CheckFreeGame `23c90bc` waits 1.8 seconds after Scatter/NPC/ring,
then awaits this start window. After its source resolves it initializes the Free
result and launches GManager's transition without awaiting transition completion.
The following BaseEnd state must be verified before releasing Spin busy state.

## Validation

`RecoveredJackpotPopupArtTests` checks all 268 source geometry frames and steady
state allocation. `RecoveredFreeStartArtTests` renders the two layers with the
original relative positions, verifies clip duration, looping and timeScale pause.
Fresh captures are `Artifacts/current-free-start-art-{173,1270,3700}.png`; these
show the converted artwork only, not an implemented complete Free start window.

Unity 2022.3.62f3 full PlayMode regression: **251/251 passed** on 2026-09-08
(`Artifacts/free-start-art-full-tests.xml`). The three fresh 1080x1920 captures
were inspected at original resolution. Both clips retain looping and scaled
pause behavior; 1000 warmed pose samples per clip allocate 0 bytes. This does
not establish complete window layout, color/material parity or full lifecycle
parity, which require the remaining source UI and flow integration.
