# More Spin branch

Main OnClickButton 23bd4e8 checks live SpinCount at 23bd814; count <= 0 branches
to 23bdc34 and shows UIMoreSpinView. Current RecoveredSpinEntry emits
MoreSpinsRequested. Production CoreRoundFlow now subscribes the actual authored
MoreSpinWindow, binds the existing ad facade/player, and connects its limit event
to the authored TipsWindow. Core teardown removes these subscriptions and cancels
both windows before replacing the player.

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
prepared prefab; the separate integration fixture verifies the production subscriber.

TipsWindow copies Res/Prefabs/UITipsView.prefab, including its original full-width
Image/TMP, VerticalLayoutGroup and ContentSizeFitter. Their original GUIDs were
resolved using delivery/Assets/original-guid-map.json. UITipsView.CloseWindow
MoveNext 23db094 waits 2 scaled seconds at original PlayerLoopTiming 8 (Update,
confirmed in the original enum), then hides. The shared recovered Update wait
runner provides this boundary; normal visibility changes do not cancel its wait.
Explicit GM cancellation does. The limit message is serialized on CoreRoundFlow.

Validation: more-spin-claim.xml passes 6/6; more-spin-regression.xml passes 364/364.
The fresh current-more-spin.png was inspected for title, +10 label, instruction,
ad-icon GET NOW button and close button. That initial validation was component-level;
the production follow-up below extends it to the actual invocation.

Production follow-up: RecoveredMoreSpinIntegrationTests now clicks the actual Spin
Button at zero, verifies no debit/reel start, fails/retries the real simulated ad,
then grants and starts a real subsequent spin. After GM rebuilding the actual US
snapshot (config type organic), it sets the native limit boundary and confirms the
real tip message, no ad dispatch, a paused timer, eventual hide and retained claim
latch. Finally it rebuilds while an ad is pending and verifies an old success cannot
credit the retired player. more-spin-integration.xml passes 1/1. The fresh
current-more-spin-limit.png was inspected; the source text/background render above
the More Spin window. The former missing invocation is now connected; general
window-manager masking/stacking and other not-yet-restored input branches still
require broader work and must not be claimed complete from this test.
Full connected regression: more-spin-connected-regression.xml passes 365/365.

## Native mask and Tips depth audit

UIMoreSpinView.OnInitProperty 23d55b4 loads the ELF literal at dbb670:
two little-endian integers (300, 2), i.e. Popup / Black. The property constructor
23f99b0 writes alpha 0x3f266666 (0.65). UITipsView.OnInitProperty 23daf60 loads
dbba90: (2000, 0), i.e. Top3 / None. Tips was incorrectly authored at 400;
the More Spin prefab was missing its window background entirely.

BaseUIManager.AddColliderBgForWindow 33be790 maps mode Black to the non-transparent
argument of UITools.AddBgColliderToTarget 23fd9dc. ARM 23fdb88 places the child
first, 23fdb9c adds Image, and 23fdba4..23fdbc8 sets black RGB with the property
alpha. 23fdbd8 fills the parent canvas through FillInCanvas 23fb154. Relocation
4f1fba8 resolves to the exact name `_WindowBg`; 4f1fba0 resolves to AddComponent<Image>.
AfterAddBgMask 23f8ffc adds the standard Button and disables transitions at
23f90c4. BaseWindow.OnClickBgMask 23f9334 returns without action; More Spin has no
override, so clicking the background must not dismiss or claim.

BuildMoreSpinWindow now authors this static first-child Image/Button mask with
the source alpha in the prefab, per the project Prefab-First constraint. It sits
outside animated Content and follows the window's active lifetime. Tips uses
the source 2000 sorting tier and receives no added mask. This is limited to the
zero-Spin branch; it does not establish a complete native window stack manager.

The window fixture now uses EventSystem.RaycastAll at the real Spin, claim and
close positions, executes a background pointer click, and checks that hiding
the popup restores Spin's hit. It also checks Tips' corrected tier. Captures use
the current project after authoring.

Validation: more-spin-mask-fixed.xml passes 1/1 and more-spin-mask-regression.xml
passes 365/365. The first raycast fixture used Transform.position, which is the
original CloseBtn's top-right pivot (1,1), and missed its rectangle. It now uses
RectTransform.rect.center transformed to world/screen coordinates; no source
button geometry was changed. The fresh current-more-spin.png was inspected:
the full background is dimmed while the popup and its buttons remain undimmed.
