# More Spin branch preparation

Main OnClickButton 23bd4e8 checks live SpinCount at 23bd814; count <= 0 branches
to 23bdc34 and shows UIMoreSpinView. Current RecoveredSpinEntry emits
MoreSpinsRequested, but production still has no subscriber. This stage prepares
the actual window/claim behavior; production binding and the limit-tip consumer
remain required next steps. Do not claim the zero-spin path is fixed yet.

UIMoreSpinView source methods:

- OnBeforeShow 23d55d4 resets isClick, plays remind, and formats +{0} from
  ConfigManager.GetAddSpins 236a700. That getter reads Qonrii.OppGping[0].
- OnClickButton 23d5720 latches before name dispatch. ClaimBtn plays click then
  calls PlayAd; CloseBtn plays click and hides. A pending claim blocks close.
- PlayAd 23d5864 compares live PlayerData.LimitSpinCount with GetLimitMaxSpinCount.
  At or above the limit, non-default config types show UITipsView without starting
  an ad. The original latch is not reset on this branch.
- Otherwise requests RewardAd with placement and scene extraspin. Failure 23d5c2c
  clears the latch. Success 23d5b2c reads current SpinCount and current GetAddSpins,
  adds as signed int, calls the original SpinCount setter, then hides. The existing
  setter clamps storage but not its notification argument, and saves before notice.
- ELF relocation entries were resolved to script metadata: 4f1bd70 ->
  ShowWindow<UITipsView>; 4f1c510 -> "The ad isn't ready yet, please wait.";
  4f1e950 -> +{0}; 4f1e958 -> remind; 4f1e970 -> extraspin.

BuildMoreSpinWindow copies the original hierarchy/Rects/Images/TMP/Buttons and
btnanim legacy Animation. Original scripts map to official Unity/TMP assemblies;
the native window and localization behaviors are removed for the recovered
presenter. Native English art, font materials and ad sprite are retained. Other
language-specific behavior still requires verification. Popup entry/exit uses the
already recovered .3-second Back curves. Existing SDK simulation is unchanged.

Explicit profile/lifetime cancellation prevents an old ad success from crediting
the old player. This cancellation is local GM support, not an original ad outcome.

Claim tests cover failure/cancel/unavailable retries, live grant and balance reads,
setter clamping/notification/save, exact limit boundary, default exemption, click
latching, close and cancellation. The authored-window fixture uses real Buttons
and the current GameEntry ad facade, captures current-more-spin.png, retries a
failed ad, credits once, closes and reopens. It deliberately instantiates the
prepared prefab; it does not pretend the missing production subscriber exists.

Next: add native UITipsView prefab/behavior (source Res/Prefabs/UITipsView.prefab),
then bind MoreSpinsRequested to the actual MoreSpin window and the limit branch to
the tip. UITipsView.CloseWindow MoveNext 23db094 waits 2 scaled seconds at original
PlayerLoopTiming 8, then hides. Preserve the original latch behavior on the cap.

Validation: more-spin-claim.xml passes 6/6; more-spin-regression.xml passes 364/364.
The fresh current-more-spin.png was inspected for title, +10 label, instruction,
ad-icon GET NOW button and close button. This is component-level visual validation;
global window masking/stacking and the production invocation remain to be connected.
