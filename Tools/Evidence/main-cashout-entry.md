# Main CashOutb entry integration

BuildMainCashOutEntry.Save extracts the original Main Tubiao rectangle with its CashOutb subtree, preserving the standard Button, 200x205 rectangle, child caption, source TMP font/material/gradient, icon position, and finger target. It replaces only the original Spine component with the previously verified native Unity ef_tixianicon animation prefab. The original non-rendering Image subclass is represented by a transparent standard Image on the same Button; its visual children and interaction stay together. All event binding is code-owned, with no persistent UnityEvent calls.

CashOutEntry.prefab is embedded outside PopupRoot in CoreRoundFlow.prefab; the full BuildCoreRoundFlow author also includes it. RecoveredCashOutEntry binds a named click listener and removes it on Unbind. CoreRoundFlow plays the original click sound and calls its existing cached CashOut lifecycle, so the main entry and CashPrompt claim share the same window and group ordering. Reopening reuses the instance; duplicate active shows retain the existing lifecycle behavior. SDK routing remains skipped under the user's instruction; no new Payout/Waiting integration or fabricated SDK state is added.

Source route: Main.OnClickButton 23bd4e8, with CashOutb/WithdrawBtn joining the NeedWithDrawOpne branch, documented in main-cashout-entry-audit.md. This implementation connects the non-SDK CashOutb entry. It does not claim that the separate WithdrawBtn subtree, all SDK-state-driven visibility, or every payout continuation has been recovered. The current entry uses the original active prefab state; version-specific entry visibility still needs complete native initialization audit.

Unity author PID 43212 exited successfully. The new PlayMode test uses actual EventSystem raycasts for opening, Back, and reopening; checks the original Button dimensions/finger ownership, empty persistent events, click audio event, shared popup parent, cached instance, duplicate-show depth, and old-binding removal during GM replacement. It captures the current main scene before opening as Artifacts/current-main-cashout-entry.png.

Final validation: Artifacts/main-cashout-entry-tests.xml passed the full PlayMode suite, 459/459, zero failures, in 95.8528307 seconds. Unity PID 38444 exited. The fresh main-entry capture was inspected: the animated payment icon and CASH OUT caption occupy the original right-side entry position and do not cover the reel board. No new original-APK pixel comparison was performed.

## Original Node safe-area parent

The follow-up hierarchy audit found that the entry extraction began at Tubiao (224587083680832752), omitting its source Node parent (224901155395665742) and Adapt component. BuildMainCashOutEntry now extracts Node with only Tubiao/CashOutb retained, maps Adapt to RecoveredScreenAdapt, and keeps the source icon-container and button transforms unchanged. Runtime click/finger bindings remain on the entry root. This covers the cash-out entry only; other extracted Main branches still require their own safe-area hierarchy audit.

The real-scene test now applies a notched 1080x1920 safe area to the authored Node, checks its offsets, opens CashOut through an actual EventSystem raycast while inset, closes it, restores the actual screen safe area, and continues the existing finger, cached-window and GM checks. Author PID 22124 exited successfully. Validation pending.

Focused validation: PID 15796 terminated with 7/7 passing in 3.2051892 seconds (Artifacts/main-entry-adapt-tests.xml), covering MainCashOutEntry, MainCashOutStatus and ScreenAdapt. No new full-suite or physical-device parity claim is made.
