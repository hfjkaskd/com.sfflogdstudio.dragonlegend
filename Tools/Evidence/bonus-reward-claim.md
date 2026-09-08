# UIRewardView claim behavior

Native sources under the current reverse root:
23d8174 (BeforeShow), 23d8470 (click), 23d89e4 (finish),
23d8c68 / 23d8c80 (count getter/setter), 23d8cc0 (count completion),
23d8d40 / 23d8d7c (reward-ad success/failure),
23d87e0 / 23d89c4 (after-hide/flight callback).

BeforeShow snapshots winCount into curCount and writes two floats from constant
Ghidra 00ebb9a8. Reading the corresponding ELF PT_LOAD address yields 2.0 and
0.5. These are not Jackpot/BigWin claim configuration or a first-free flag.
The future authored presenter must supply these values to the claim controller.

The click latch is set before matching ClaimBtn / UnPlayBtn; unknown names also
latch. ClaimBtn plays click and requests the existing ad facade using lucky for
both placement and scene. There is no first-free bypass. Failure only clears
the latch. Success multiplies winCount by 2. UnPlayBtn plays click, requests
interstitial iv_close/lucky, and multiplies by .5. Preserve float fractions.

If reward equals its original value (including zero), hide immediately.
Otherwise play count and tween display from original to final for .5 seconds
(DOTween default OutQuad). The getter returns unchanged curCount; the setter
only writes currency text with precision 2. Completion hides UIWheelView, then
the reward window itself. There is no music-resume or sound-channel-stop call
in this window's AfterHide.

AfterHide dispatches event 1 with amount and callback. The flight callback calls
the original float recipient with winCount. It must not run at claim click,
count completion, or popup deactivation. Existing shared flight logic performs
caller -> title reset -> latest balance credit, so this controller never credits.

ELF RELA/script.json confirms 0501ea68 = lucky, 0501d180 = ClaimBtn,
0501d040 = UnPlayBtn, 0501dec8 = HideWindow<UIWheelView>.

RecoveredBonusRewardClaim implements this business sequence through a presenter
interface and the unchanged IAdFacade. Tests cover all three unsuccessful ad
outcomes, retry, duplicate clicks, fractional ordinary reward, delayed caller
and actual shared balance-credit ordering, zero, and unknown button latch reset.

The actual UIRewardView prefab/presenter, count timing and visual comparison
remain pending. Interface tests cannot prove those presentation requirements.

Full PlayMode verification: `Artifacts/bonus-reward-claim-tests.xml`,
240 passed, zero failed. This includes the five new claim test cases.
