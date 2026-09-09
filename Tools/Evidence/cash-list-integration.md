# Cash list and bottom integration

RecoveredCashOutList uses the fixed-size vertical ListView path already recovered
for CollectList: inclusive visibility boundaries, squared movement threshold 2,
FIFO cache, hidden-but-active cells at (9999,9999,9999), dirty refresh and original
standard ScrollRect prefab with RectMask2D. Cash cell size is (1080,310), decoded
from 23a7390 CreateList float arguments 0x44870000/0x439b0000; one column.
Only visible cells are instantiated from the configured CashOutItem template.

UICashOutView evidence:
- InitCashOutItems 23a7390 stores CheckItemKuang result in selectIndex before
  creating/refreshing the list. Initializes Bottom only for selection >=0.
- Per-cell callback 23aa6d4 supplies index, saved selectIndex, type and ItemKuang
  to CashOutItem.InitUI. The original shared selection frame is extracted from
  RectTransform 224187454977326620, size1044x285, sprite tx_bak_00 with border
  (37,39,38,35), raycast disabled.
- Card click moves that same frame and initializes Bottom at the clicked index.
  RefreshCashOutItemSelectKuang 23a7a8c does not write selectIndex.
- Payment type refresh 23a7720 initializes Bottom with saved selectIndex and new
  type. Card payment listeners leave recorded payment choices alone; cached
  active cards also receive the event. Payment tab frame/account field updates
  still belong to the full window, not this list component.
- Card/bottom expiry refresh events trigger selection/data initialization and
  update the actual bottom checks. No balance debit or task mutation occurs.

Detected and fixed during current rendering: CashOutItem's iconPaths first
entry was an unquoted empty YAML sequence item. After trailing-whitespace cleanup
and subsequent import, Unity deserialized the whole iconPaths array as empty;
runtime Resources.Load of the actual PayPal sprite still succeeded independently.
The prefab and author now use an explicit quoted empty string. This preserves
native missing icon0 while retaining entries1..4 across imports and nested clones.
No reload workaround or Editor-only runtime behavior was added.

Validation: `cash-list-fixed.xml` 3/3 PlayMode tests passed, PID48768 exited.
Original card regression after reimport: `cash-item-reimport.xml` 2/2 passed,
PID34304 exited.
Tests cover card selection driving actual Bottom, initial-selection persistence
on payment changes, exact visible-cell count, FIFO reuse without extra instances,
raw-record-count frame hiding, and bottom-expiry propagation to card/checks.
The render test now requires actual non-null PayPal sprites/main textures after
instantiation. `Artifacts/current-cash-list-bottom.png` was regenerated and
inspected: three PayPal cards, second-tier frame and corresponding missing-cash
text. The composition is a test canvas, not a recovered full UICashOutView.

Remaining integration: full window hierarchy, payment tabs/account input,
account/tip child windows and continuation handlers, plus core prompt and main
cash-button binding. SDK facade stays unchanged.
