# Treasure claim flow

## Authoritative native path

ARM64 files under `C:/Projects/Nut Sort Relax/reconstruction/mumu-current/native/game/`:

- `023dc0a8.asm` OnBeforeShow resets isClick, reads the matching CollectInfo and claim multipliers 0/1. It does not store reward (+0xbc), unlike UIRewardView. A reused Treasure window retains the prior reward until the next successful claim calculation.
- `023dc94c.asm` OnClickButton locks before examining the name. Repeated clicks return; an unknown name leaves the lock set. ClaimBtn plays click and requests a rewarded ad. UnPlayBtn plays click and interstitial, then assigns `(float)info.worth * unPlayAd` and invokes the finish action.
- Relocation 04f1ebd0 resolves to `treasure`, used as rewarded placement/scene and interstitial scene. Interstitial placement 04f1d030 is `iv_close`. The existing IAdFacade remains unchanged.
- `023dd460.asm` rewarded success assigns `(float)info.worth * playAd`, reading the current CollectInfo rather than multiplying an old reward. `023dd4b0.asm` failure only clears isClick.
- `023dd198.asm` finish compares the float-converted current info.worth with reward. Equality directly hides Treasure. Otherwise it plays count and creates a .5-second default OutQuad numeric tween. `023dd3e0.asm` getter reads current info.worth at tween startup; `023dd408.asm` setter only formats RewardTxt at precision 2, and does not mutate stored reward. `023dd448.asm` completion hides Treasure. There is no HideWheel call.
- `023dcca0.asm` after the actual exit dispatches event `1` with `[reward, callback]`, then event `14` with `[EndPos.position, CardImg.sprite]`. `023dd178.asm` arrival callback reads current stored reward and invokes the caller. Card departure is requested after cash flight starts, without waiting for cash arrival.

RecoveredTreasureClaim expresses these exact data/ad/callback transitions. IRecoveredTreasureClaimView exposes deferred numeric getter startup and the separate cash/card departure operations. It does not change SDK handling, award cash itself, mutate collection records, or substitute the ordinary reward popup's behavior.

## Verification and limits

RecoveredTreasureClaimTests checks cancelled/unavailable/failed ads, retry and duplicate clicks, live worth changes while the ad is pending and before tween startup, exact placements, fractional normal claims, no-count equality/zero cases, persistent reward across reopen, and unknown-name locking. The existing RecoveredRewardBranches.CompleteFlyCoin path verifies caller-before-balance update and persistence, with cash/card dispatch ordering checked separately.

Full Unity 2022.3.62f3 PlayMode regression passed **333/333**, `Artifacts/treasure-claim-tests.xml`. No visual asset was changed in this increment.

The tests use a claim-view recorder; they do not prove the actual Treasure window animation or flight. The native prefab presenter, its .5-second count and .3-second InBack exit, and real cash/card consumers still need connecting. Existing standalone card and collection-tip prefabs remain the required presentation components.

## Window continuation evidence

`023dcf84.asm` EnterAnimation only invokes its completion callback immediately. Do not copy the ordinary popup's .3-second entrance. `023dcf9c.asm` ExitAnimation resets Content to toScale, then tweens to fromScale with closEase. Constructor `023dd13c.asm` sets closEase=26 (InBack) and reads the same `(1,.3)` constants as PopupWindow. OnAfterShow resets EndPos.localPosition and invokes FlyCall; flight completion invokes RefreshCollectCard, which activates the card/black background and starts the flip. The containing window must invoke CollectTip on flip completion.
