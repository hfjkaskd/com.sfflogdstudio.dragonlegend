# Core settlement window groups and mask modes

The previous depth helper searched every active Canvas under CoreRoundFlow. This included Tips at 2000, so showing Wild while Tips was visible incorrectly assigned Wild depth 2001. More Spin, Bank and Review also bypassed the helper, so their authored depth 300 could remain behind an already open popup.

Source verification uses current native ARM listings and ELF literals, not the constructor defaults alone:

- OnInitProperty: Bank 23914d0, MoreSpin 23d55b4, Review 23d74bc, CashPrompt 23abc98, MoreWild 23d5c34 load the 8-byte pair at dbb670 into UIWindowProperty +0x10/+0x14. ELF PT_LOAD mapping resolves this to (300, 2): Popup group, Black mask.
- CashOut OnInitProperty 23a6dc4 loads dbc1f0 = (300, 1): same Popup group, Normal mask.
- Tips OnInitProperty 23daf60 loads dbba90 = (2000, 0): Top3 group, no mask.
- dump.cs UIWindowType confirms Popup=300 and Top3=2000. UIManager.InitWindowRoot 23fa8fc stores each enum value as that group's base depth. GetTargetWindowRoot 23fadd0 creates a distinct stretched RectTransform group under the UI root. BaseUIManager.AdjustWindowDepth 33be5f4 queries the active maximum within that type's root and uses max(base, maximum+1). Already shown windows bypass show-property adjustment in PrepareToShowWindow 33bdeb8.
- ApplyWindowShowProperty 33be08c sends Normal mask as isTransparent=true and Black as false. UITools.AddBgColliderToTarget 23fd9dc uses alpha zero for transparent, otherwise the window's alpha (.65 here). Therefore the CashOut lifecycle's earlier black .65 blocker was incorrect.

CoreRoundFlow.prefab now has a stretched PopupRoot containing the authored More Spin, Wild and Bank windows. Lazy Review, CashPrompt and CashOut instances use that same root. Tips remains outside it. These six core windows share the depth helper, including both More Spin entry routes. The helper queries only this group, ignores inactive cached windows, and keeps duplicate Show unchanged. BuildCoreCashPrompt.Assign constructs the group during authoring and is also called by the full BuildCoreRoundFlow author. Runtime code instantiates configured prefabs and does not build static hierarchy. CashOut's authored input blocker is now transparent, retaining its standard Button, raycast and empty event list.

The grouping covers these connected core-settlement windows. Other separately owned Bonus/Free/collection window groups and their complete UIOrder integration still require audit; this is not a claim that a full game-wide UIManager has been recovered.

Before: Artifacts/core-modal-depth-before.xml failed 0/1, expected Wild depth 300 but observed 2001 (PID 43704 exited). The new regression also checks More Spin opens above Wild, already shown windows keep depth, and reopening after both close ignores their inactive cached depths. The existing actual Bank/review test now leaves More Spin open during Spin, then checks both pending Bank and Review receive higher depths; its real Review star/submit raycasts remain active. CashOut lifecycle asserts a transparent, raycastable standard Button blocker.

Final validation: Artifacts/core-popup-groups-regression.xml passed the full PlayMode suite, 457/457, zero failures, in 90.5470026 seconds. Unity PID 39376 exited. The fresh current-project captures Artifacts/current-review-window.png and Artifacts/current-core-cash-guide.png were visually inspected: Review stays in front of the open More Spin window, and the Wild guide stays in front of CashOut. These captures verify the reconstructed scene's current presentation; no new original-APK pixel comparison was performed in this round.
