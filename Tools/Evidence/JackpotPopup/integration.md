# Jackpot in the actual paid Spin chain

`RecoveredSpinPlayfield` now runs CheckJackPot immediately after its completed Wild scan.
Fewer than three complete Wild columns synchronously enter the symbol-animation boundary.
Otherwise `RecoveredJackpotSequence` uses the existing task mutation and post-save reward
snapshot, dispatches the winning meter animation, pauses music and requests Sound1
`dragon3`, waits 1.5 scaled seconds, stops Sound1 and shows the authored jackpot popup.
The original callback only sets a boolean; the existing recovered Update wait runner
continues the symbol stage when that predicate becomes true. Popup close cannot satisfy it.
GM profile changes cancel pending waits and invalidate callbacks from the old sequence.

## Previously missing runtime data links

- `SlotGameResult.InitGameResult` (`2383290`) writes GameData.isFirstFreeReward (+0x5a)
  before filling board cells. The actual entry now supplies its player runtime state to
  RecoveredSpinResult, so the tutorial sets this flag and the next ordinary result clears
  it. The value is not persisted or consumed by the popup.
- `UIMainView.JackPotAnim` (`23bc60c`) selects its Grand field at +0xf8 for enum 1, Major
  at +0x100 for enum 2, otherwise Mini at +0x108. It calls that meter's PlayAnim with
  callback `23bf84c`. The callback calls the native JpAddCount setter with zero, causing
  one save. Existing JackPot.PlayAnim returns its icon to idle, invokes that callback,
  then refreshes the selected meter. Other meters are not refreshed by this callback.
- The popup starts after 1.5 seconds while the winning icon runs for 2 seconds. Its reward
  therefore stays the captured amount even when the icon later clears the counter and
  refreshes its own label/runtime value. It must not reread the meter when claiming.

## Native mask and initial popup depth

`BaseUIManager.AddColliderBgForWindow` (`33be790`) passes bgMaskAlpha to UITools for
Black mode (2). `UITools.AddBgColliderToTarget` (`23fd9dc`) creates a first-sibling Image
with RGBA `(0,0,0,alpha)` and fills its parent Canvas. RELATIVE relocation `4f1fba8`
resolves to `_WindowBg`. `BaseWindow.AfterAddBgMask` (`23f8ffc`) adds a standard Button
with no transition. The default mask action does not dismiss UIJackpotView.

The authored mask retains native .65 alpha and standard Button hit testing. It is outside
the scaled Content object, so opening/closing content does not shrink the modal hit area.
`UIManager.InitWindowRoot` (`23fa8fc`) uses each numeric window type as its startDepth.
`AdjustWindowDepth` (`33be5f4`) chooses max(startDepth, maximum existing depth + 1).
The currently single popup therefore uses 300. Full multi-window stack ordering remains
to be implemented with the remaining native windows. The actual popup's Canvas uses the
entry's authored UI camera so its standard GraphicRaycaster has the correct event camera.

## Existing local SDK mock

GameEntry owns a LocalAdFacade per loaded GM profile. The same facade is passed to the
jackpot claim controller. A small authored GM Canvas, sorting order 5000, offers manual
reward/failure Buttons only while an advertisement is pending. It does not automatically
complete ads. These are local test controls, not recovered player-facing UI or a new SDK.
Switching profile cancels a pending mock result before releasing its old presenter.

## Outstanding continuation

The actual popup now opens from Spin and dispatches its flight request after exit.
The fly-coin presenter is not connected yet; the real flow correctly remains waiting at
that boundary. The integration test supplies the callback explicitly to verify the next
stage boundary, and does not claim to test a real flight or credit.
PlaySymbolAnim, CheckBonusGame, CheckFreeGame and CheckBaseEnd still require their actual
presentations and links. Only CheckBaseEnd may clear IsBusy/AwaitingRewards.
Sound requests are forwarded but the game's actual audio presenter remains outstanding.

Next native flight evidence: `PlayFlyCoin.MoveNext` = `23d440c`, event adapter = `23baeb4`,
completion = `23c306c`, per-coin departure = `23c3188`, per-coin arrival = `23c3300`,
collection-effect cleanup = `23c3554`. It spawns ten pooled objects, scatters with two
integer Random.Range(-150,150) draws each and a .3-second local-position tween, then
waits .3 scaled seconds before scheduling staggered DOVirtual.DelayedCall departures.
Those delayed calls explicitly use unscaled time. Each departure invokes FlyAnimUtils
with duration .3, height -1 and ease 4. Remaining timing, pool prefab, arrival effects,
TopTitle reparent/reset and complete credit ordering must be recovered before connection.

Current actual-entry captures: `Artifacts/current-jackpot-main-flow.png` and
`Artifacts/current-jackpot-gm-ad.png`. Standalone window screenshots remain separate.

Validation: `Artifacts/jackpot-integration-all-tests-2.xml` passes 187/187 PlayMode tests.
New tests cover all three jackpot tiers, the scaled delay and ordered events, cancellation
and stale callbacks, real paid Spin -> Wild -> popup, native counter reset, modal Button,
and manual GM ad failure/retry/success. Both actual-entry screenshots were inspected.
Full regression also checks disabling the entry and switching profiles. GM facade release
does not perform a camera lookup in an inactive parent hierarchy.
