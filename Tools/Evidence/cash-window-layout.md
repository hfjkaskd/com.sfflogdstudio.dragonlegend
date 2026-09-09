# Full UICashOutView layout recovery

BuildCashOutWindow.Save imports the current ReferenceOriginal/Res/ViewPrefabs/
UICashOutView.prefab into Resources/RecoveredUI/CashOutWindow.prefab. All original
Top/Node/Bottom RectTransforms are retained. The bottom subtree is replaced with
the tested CashOutBottom prefab while copying its original transform properties.
The tested CashOutList prefab is nested under original Node/Rect. Its own shared
frame replaces the duplicate source Node/ItemKuang.

Original tixian sprite assets are resolved by source GUID. Each extracted sprite
keeps its original border and uses official single-sprite import. Fonts/materials
reuse recovered msyhbd and Quorum assets. Standard Image, Button, TMP_InputField,
TMP labels, layout groups, RectMask2D and GraphicRaycaster replace their extracted
script GUIDs. Persistent button events must remain empty.

The original Canvas is WorldSpace. Root is inactive until a caller opens it.
Account input remains native non-interactable, readOnly=false, limit=0; the account
button is separate as authored. No automatic account edits or submissions occur.

This asset does not yet have the full window controller. Original UICashOutView,
CashOutBottom and Adapt scripts are removed during import; the bottom is replaced
by recovered behaviour, while root lifecycle and Adapt sizing still require
integration. There is no Editor-only runtime fallback. This prefab is not yet
bound to the production core flow, so it is not an assertion that its unbound
header controls or gift branch are complete.

Additional native evidence for next implementation:
- OnInit 23a6de4 stores Bottom initial anchoredPosition and registers payment,
  selection and refresh events. It hides payment Layout when config type differs
  from the constant at GOT4f1bf98 (resolve before implementing region routing).
- OnBeforeShow 23a7b28 ends by setting openType=0, CheckAccount(1), CheckAll.
- CheckAll 23a980c calls CheckAccount(type), CheckLayOut, cash/gift CheckTag, then
  chooses cash or gift list initialization.
- CheckLayOut 23a9858 shows Bottom and CashOutRect only for openType0, GiftRect
  only for openType1; payment Layout additionally requires the config type match.
- CheckTag 23a99e8 toggles button child2 according to openType.
- CheckAccount 23a7820: cash path stores type and finds first Accounts record of
  matching type; no record builds the provider prompt, a record uses account text.
  Gift path reads GiftAccount, with its distinct absent/null-string handling.

Validation: Unity 2022.3.62f3 PlayMode `cash-window-art.xml`, 1/1 passed,
PID8184 exited. The test hosts the original world-space canvas in a 1080x1920
RectTransform, supplies live list/bottom data, previews the cash branch, verifies
font/material/atlas references, input properties, empty persistent events and
actual raycast hits on all four payment buttons. Clicking a card moves its frame
and updates bottom target text. It does not claim payment-tab callbacks exist.
`Artifacts/current-cash-window.png` was regenerated and visually inspected.

Remaining: root lifecycle/Adapt, payment and account event bindings, close flow,
gift list/account branch, child windows, ordinary task continuation and production
core/main-cash entry. Existing SDK facade is unchanged.
