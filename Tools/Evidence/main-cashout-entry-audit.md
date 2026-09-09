# Main withdrawal entry: verified route and remaining implementation

This audit uses the current native ARM/ELF/dump and original prefab. It does not use historical screenshots.

UIMainView.OnClickButton RVA 23bd4e8 compares CashOutb (Ghidra GOT 0501e330) and WithdrawBtn (0501e318), then joins the same branch. It plays click (0501cf38), obtains GameData, and reads byte +0x58. The dump identifies this field as NeedWithDrawOpne; IsA is +0x10. Therefore implementing this branch as a direct A/B switch would be incorrect.

When NeedWithDrawOpne is false, the branch constructs UIWindowContextData containing an empty List<object> and calls UIManager.ShowWindow<UICashOutView> (0501bcf0). When true, it opens UIWaitingView (0501bcf8) and invokes Publish.Payout through the SDK interface. SDK functionality must retain the project's current handling under the user instruction; this audit does not authorize or implement new SDK calls.

The original UIMainView prefab contains a standard Button on CashOutb, GameObject 1007513189690300, RectTransform 224689876809886907. The serialized CashOutBtn field points to child Transform 224368669867904354, not to a separate button. Its visual hierarchy includes SkeletonGraphic (ef_tixianicon). The current recovered resources contain the atlas image but no recovered ef_tixianicon animation prefab. The actual main entry has not yet been authored or connected; the existing CashPrompt claim is a different entry into the cached cash-out window.

Next implementation must preserve the original button subtree and icon animation, audit SetInitShow/version visibility and any initialization of NeedWithDrawOpne, then connect the non-SDK path to the existing window lifecycle and popup group. It must not add an invisible replacement click area to the balance icon or conflate the two original button names with IsA.

Reproduction: run Tools/audit_main_click_references.py with the reverse-project root and an output JSON path. It parses ELF64 R_AARCH64_RELATIVE relocations, subtracts the Ghidra image base 0x100000, resolves strings/types/generic methods through script.json, and records input SHA-256 values. The checked-in main-click-references.json resolves 61 references. Assertions verify both button names and the concrete UICashOutView generic target. The script completed successfully during this audit.

Related ordering inspection: the current BigWinPopup, JackpotPopup, CollectWindow and BonusRewardPopup prefab files each contain one Canvas sorting-order declaration. No second independently sorted Canvas was found in those four files; this narrow inspection is not a game-wide UIOrder audit.

No gameplay or prefab changes were made in this audit. The authoritative result is the decoded route, which prevents connecting the missing main entry to an incorrect version branch; full visual and functional fidelity remains incomplete.
