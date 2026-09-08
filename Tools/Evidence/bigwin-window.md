# Big Win claim and window boundary

Source: reconstruction/mumu-current/native-functions/game, matching ARM64 ELF and
analysis/script.json. This is a separate window from UIJackpotView.

## Confirmed native behavior

- 2394a14 resets isClick=false and winReward=0, then reads context [type, reward,
  callback]. Pauses music, plays bigwinBg on Sound1 and bigwinm/megawinm/superwinm
  for types 1/2/3. The original effect clip uses the corresponding enum name;
  a Unity port must use explicit clip mappings without enum.ToString.
- The first-free flag is GameData +0x5a (isFirstFreeReward), not the A/B profile.
  At show, it makes adClaim=1 and hides UnAdTxt. Otherwise adClaim comes from
  GetBigWinClaim(0). freeClaim always comes from GetBigWinClaim(1).
- 236ac9c reads Qonrii +0x90 = RiikinQloim[index], converting the signed integer to
  float and dividing by 1000. It does not read JpQloim or clamp negative values.
- Non-first-free advertised text is `<sprite name="tc_btn_bofang">CLAIM` for
  adClaim <= 1, or `<sprite name="tc_btn_bofang">CLAIMx{0}` above 1. First-free
  text is `CLAIM`; plain text is `Only {0}` with reward*freeClaim formatted at
  currency precision 2. The plain text parent runs PlayBtnAnim(1).
- OnBeforeShow dispatches event "7" with [3,1], then initializes CashOutTip.
  The apparent later PlayInterAd in that decompilation is a following-method
  tail reached only through exception/type-failure pseudocode, not normal show.
- 239519c sets the click latch before matching button names. Unknown names retain
  the latch. ClaimBtn checks the LIVE first-free flag. If true, it bypasses the
  ad but uses the captured adClaim; otherwise it requests rewarded bigwin/bigwin.
  UnPlayBtn requests interstitial iv_close/bigwin then uses reward*freeClaim.
- Success 23959a4 sets winReward=reward*adClaim and invokes finishCall.
  Failure 23959dc clears only the click latch, enabling retry.
- finishCall 23956fc hides directly when reward==winReward. Otherwise it requests
  count sound and .5-second default OutQuad text count, then hides. Getter 2395938
  returns the original reward; setter 2395950 formats Text only; callback 239598c
  hides this Big Win window. No Jackpot wheel close belongs here.
- 2395548 AfterHide stops Sound1, resumes music, invokes call(winReward), THEN
  dispatches event "1" with [winReward,null]. The existing main-view cash-flight
  handler owns credit on arrival. Callback order is DIFFERENT from Jackpot.

## Resolved pointers

Ghidra pointer addresses below are resolved via ELF RELA at pointer-0x100000,
then matched to script.json. No guessing strings from neighboring methods.

| Ghidra pointer | Metadata/string address | Meaning |
| --- | --- | --- |
| 0501d160 | 05045f18 | finishCall 23956fc |
| 0501d168 | 05045f20 | success 23959a4 |
| 0501d170 | 05045f28 | failure 23959dc |
| 0501d178 | 0505e538 | bigwin |
| 0501d180 | 0504e320 | ClaimBtn |
| 0501d040 | 0505a6f8 | UnPlayBtn |
| 0501cfb8 | 0504abb8 | event 1 |
| 0501c520 | 0504b238 | event 7 |
| 0501d138 | 0505e540 | bigwinBg |
| 0501d130 | 05063dc0 | superwinm |
| 0501d120 | 05061d68 | megawinm |
| 0501d150 | 0505e548 | bigwinm |

## Port status

RecoveredBigWinClaim implements the verified claim/callback state and delegates
actual animation and flight to a dedicated view boundary. Its SDK calls use the
existing IAdFacade unchanged. It performs no balance mutation. It is not yet wired
to Spin: the original UIBigWinView prefab, ef_wintanchuang animation conversion,
pre-window transfer, show events, exit animation and main-flow continuation remain
required. Unit tests of this boundary cannot prove the popup visually or functionally
complete in the actual game.

Original prefab references: Ef_win 114918264173551851, RewardTxt
114371939378208658, AdTxt 114291844617409189, UnAdTxt 114808113985169905,
CashOutTip 114919942235722503. Preserve original hierarchy and serialized layout.

Validation: Artifacts/bigwin-claim-tests.xml passed 216/216 PlayMode tests (2026-09-08).
Nine new cases cover all three unsuccessful ad outcomes, retry and duplicate-click
latching, original-to-awarded count boundaries, no direct credit, callback-before-flight,
live first-free changes with captured multipliers, reopen reset and signed/fractional
configuration. Existing 207 tests also remain passing. No new popup capture is claimed.

## Native Unity popup implementation

RecoveredUI/BigWinPopup.prefab now preserves the original UIBigWinView hierarchy and
serialized text/button/tip layout. RecoveredBigWinPopup binds the standard Buttons
in code, explicitly selects big/mega/super, forwards the recovered audio and task
requests, animates enter/exit (.3 seconds OutBack/InBack), delays plain-button/tip
reveal, counts the actual claim award (.5 seconds OutQuad), and invokes AfterHide
only after deactivation. It uses RecoveredBigWinClaim and the unchanged local SDK
facade. Show dispatches task-refresh [3,1] before initializing CashOutTip.

BuildJackpotPopup.SaveBigWin shares the offline source-preserving importer with
Jackpot. Original EmptyRaycastGraphic GUID e0b4d57e8f658b407a2ad25df85fb0d6 is mapped
to an alpha-zero standard Image on the same Button object. Its child label and
Button target remain in the original hierarchy. The unavailable original Big Win
script is replaced with RecoveredBigWinPopup. No reflection is used at runtime.

BuildJackpotPopupArt.SaveBigWin converts ef_wintanchuang.json into native Animation
clips and RecoveredRegionRig prefabs. All 86,999 source skeleton bytes are consumed:
55 bones, 60 region attachments, three 2-second clips with 108 timelines each.
The atlas PNG is unchanged; its importer disables alpha preprocessing/compression
to preserve the premultiplied source artwork. Meshless sources need no temporary
weighted-mesh input. No Spine assembly is introduced.

Reproduce geometry fixtures with Tools/sample_bigwin_reference.py. It samples all
three original clips at eight times each, using the independent source sampler.
Tools/verify_wild_extraction.py now verifies 12 complete conversions including this
binary and checks input bytes remain unchanged. The first full popup run passed
218/218 tests, including all 24 geometry poses and 0-byte allocation per 1000 pose
samples (about 16 ms per 1000 samples on this machine). The actual prefab test
exercises each tier, Button callbacks, advertisement failure/retry, paused count,
window exit and callback-before-flight ordering. This does not yet connect Spin.

Current source-based screenshots are Artifacts/current-bigwin-window-{big,mega,super}.png.
Initial inspection found that the plain label can lose glyphs in a full-suite capture
while an isolated popup run renders all glyphs. CPU text-info reports visible glyphs
and valid scale/color, with no TMP submeshes. Shared atlas/material state is being
investigated; do not describe this visual issue or the entire popup as fully verified.
The pre-window transfer/DownEfWin effect and post-window adjusted-award continuation
still need to be connected to actual Spin before the Big Win branch is complete.

Further diagnostic runs: bigwin-popup-full2-tests.xml and bigwin-popup-font-tests.xml
both passed 218/218. The latter still reproduces missing leading plain-label glyphs
in a current Super capture. All three CPU atlas PNG snapshots have identical SHA256
EED7D06DE18557EAC0F3F36E3EAD954DE88C0F85D21B2B4D2B66B4D452D83290.
The shared white font material has no keywords and its ClipRect remains
(-32767,-32767,32767,32767). Visible character flags, UV2 SDF scale (.67) and white
vertex colors remain valid. No TMP_SubMeshUI is present. The source bundle and
sharedassets0 Alpha8 atlas payloads are byte-identical (SHA256
2a4211a1f2d98d05cf095cdaddcac7a1ea1abb78fe40bb0ff887634585b89464), which rules out
those two source atlas variants differing. The GPU/CanvasRenderer mesh path and
normal hide/reopen lifecycle remain to investigate; no speculative runtime refresh
or font/layout change was added. Diagnostic logging was removed after recording
these observations. This unresolved visual defect is retained explicitly.
