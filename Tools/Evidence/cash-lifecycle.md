# CashOut window lifecycle

Source: UICashOutView.OnBeforeShow local tail 23a9168..23a91dc resets openType=0, checks account type 1, runs CheckAll, reparents provider frame through root to Layout child 0, then starts LoadAnim. InitCashOutItems 23a7390 creates its list once and subsequently refreshes it. The SDK payout-order branches preceding the local tail remain outside this port under the requested SDK exclusion.

Back button branch 23aa318..23aa35c plays click then BaseWindow.Hide. Source prefab has fromScale=0, toScale=1. PopupWindow ctor 23f9ea0 reads dbbee0=(1,.3) and dbb8d0=(27,26): .3 seconds, OutBack enter and InBack exit. The existing recovered popup convention supplies the UI-manager black .65 blocker and sorting order 300; this is not a claim that every native window-stack sorting scenario is audited.

RecoveredCashOutWindow now binds context/camera/root scaler, lazily initializes the cash list, resets the payment/account and cash tab on Show, invokes the existing independent card/bottom entrance, and drives scaled popup enter/exit. Duplicate Show while active does not restart. Back uses an authored standard Button listener. Child click sounds forward through one window event. Explicit Unbind/Cancel cancels entrance and card/bottom timers and detaches child audio events for disposal. Full authoring and incremental BuildCashOutLifecycle.Save both preserve this component and blocker in the prefab.

Verification:
- cash-lifecycle.xml: 8/9 passed; the new lifecycle fixture failed while artificially holding the opening at exactly zero scale.
- cash-lifecycle-diagnostic.xml showed a valid 1080 x 1042.47 list viewport, two created cells, but ScrollRect had moved content to y=638.92. Unity's local com.unity.ugui@1.0.0 ScrollRect.cs line 836 uses Time.unscaledDeltaTime, so freezing scaled popup motion at a degenerate zero-size transform does not freeze scrolling.
- The fixture now begins opening normally and checks pause at a nonzero animation scale. Production scrolling was not bypassed or disabled. cash-lifecycle-opening.xml passes 1/1 with exact first-card assertions, enter/exit pause checks, cash/provider reset, both list-instance reuse, shared frame return, duplicate Show behavior and unbind audio detachment.
- Fresh Artifacts/current-cash-window.png was inspected in the current main scene: account prompt, cash tiers, selection and bottom condition are rendered. This controlled current-project screenshot is not a fresh APK comparison or pixel-perfect proof.

Still pending: account-entry/claim continuations and the main settlement CashPrompt-to-window route. The window is exercised with the actual GameEntry context by the test but is not yet reachable from the production main button or prompt. Whole-game 1:1 completion remains unproven.
