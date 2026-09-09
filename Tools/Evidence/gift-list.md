# Gift page content and shared selection frame

Native sources inspected:
- UICashOutView constructor 23aa4a8 creates exactly one GiftConfig: id 0, name Amazon (GOT 4f1db68), iconName ipad (4f1db70). GiftItem.InitUI does not read iconName or change its image; the original prefab contains Amazon artwork.
- GiftItem.InitUI 23a6a44 sets both name labels, calls GetCollectReward, formats raw PlayerCollectDatas.Count / GetCollectInfos.Count with {0}/{1} (4f1d9c8), and sets fill width to count * 715 / total. ARM literal dbc4e8 is float 715. Height comes from the current fill rect. There is no clamp, uniqueness filter, receipt-state filter or count-value filter.
- RefreshKuang 23a6cfc only acts when selection equals gift.id; it reparents the frame, activates it and zeroes local position.
- InitGiftItems 23a9a3c creates the original ListView once, using 1080 x 310 cells; later calls only mark data dirty. The per-item callback 23ab450 passes the window's ItemKuang (+0xc0), shared with cash cards, and selection 0.

Implementation:
- GiftItem.prefab imports the original complete hierarchy, images, sprite borders and TMP font references. BuildGiftItem maps exact exported sprite names, avoiding prefix collisions such as tx_bak versus tx_bak_01. Runtime only changes texts, fill width and frame parent.
- GiftList.prefab reuses the original ListView-authored ScrollRect/viewport/content structure. The runtime preserves the source's one-entry catalog, lazily instantiates and reuses its one cell, uses the original visibility calculation/hide position/movement threshold, and refreshes on tab reentry.
- CashOutModeView binds gift data and the cash list's existing selection frame. Switching to gift refreshes its real cell; switching back marks an already-created cash list dirty so the same frame returns to the cash selection. Payment/account header and existing tab behaviors remain in place.
- BuildCashOutWindow attaches the gift list as part of full authoring; BuildGiftList.Save updates the existing current window independently.

Validation:
- Initial gift-list.xml: 5/5 passed before the shared-frame correction.
- Tests use actual prefab tab Buttons, verify first creation/reuse, both Amazon labels and reward, raw duplicate/zero/negative-count record handling (3 records / 2 configs produces width 1072.5), refresh back to 1/2, and the shared selection frame moving between real cash and gift cells.
- Current controlled fixture screenshot: Artifacts/current-gift-list.png, 1080 x 1920, current CashOutWindow prefab rendered with a test camera. It shows the Amazon card, reward and half-filled collection bar; it is not a fresh original-APK comparison.

Still incomplete: cash list first initialization by the window owner, popup show/hide lifecycle and account destinations, followed by the main settlement CashPrompt route. The existing region/SDK handling is not changed. This commit does not establish completed withdrawal or whole-game parity.

Final focused verification: Artifacts/gift-list-shared-frame.xml passed 8/8; Unity PID 39412 is terminal. This includes cash-list virtualization/selection, both page buttons, payment header, entrance animation and the new gift content/shared-frame tests.
