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
