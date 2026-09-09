# Main Bank progress, icon and hint button

The production SpinPlayfield now includes the Main Bank icon and progress under Bottom/Main. The core binds count notifications and connects the button to the existing TipsWindow. This completes the previously missing progress refresh side of the Bank core branch; review/cashout continuations and overall full-lifecycle equivalence remain pending. SDK behavior is unchanged.

## Native behavior

- Main.InitBank 23bbb5c reads BankCount and GetBankSpinCD for `string.Format("{0}/{1}", ...)`, then reads them for float division and Image.fillAmount. ARM 23bbc9c formats text; 23bbd00..23bbd14 converts both integers, divides and assigns fill. There is no integer clamp and no added zero-denominator fallback.
- Main.BankPop 23bc778 sets pending +219 then calls InitBank; below-threshold notification also refreshes. The new progress subscriber is registered after the core pending subscriber, preserving that order. The close callback clears pending then refreshes, matching 23c2f14. Bank.OnBeforeShow resets the count through SetBankCount, so the main label already shows zero while the popup is open.
- Main.OnClickButton 23be498..23be54c compares `bankBtn` (GOT 4f1e320), plays click, then opens UITipsView with `Need more spins to trigger the rewards.` (GOT 4f1e338; ShowWindow<UITipsView> metadata at 4f1bd70). It does not open Bank, alter counts or consume random values. There is no additional busy/count gate on this branch.

## Authored hierarchy and assets

Original Main RectTransform 224622690832225648 has position zero, size 100x100, center anchors. Imported children are icon 224600491667411216, Progress 224419535952149204, and bankBtn 224846667071028776. The group is placed inside the existing Bottom/Main so native Base/Free switching applies automatically.

The original icon and progress were siblings of an empty Graphic + standard Button. To follow the user's requirement that visuals belong to the actual Button, the author nests them below bankBtn while retaining their world positions and all source rectangle dimensions. No runtime UI construction or added invisible click object is used.

Progress is at (-427.7,-110.8), size 187x51; fill is 170x32 at (0,2), horizontal filled Image; TMP is 200x50 at (0,-1.7), font size 29, original #000000_3 material. Sprites are zhujiemian/zjm_bar_bak01 and zjm_bar_bak02. The button retains the original rectangle (-427.7,-17.606781), 187x237.3864 and empty serialized click events.

The icon is newly extracted from the source ef_slyinhang.skel.bytes: 11907 bytes consumed, one two-second `animation` clip with 12 timelines, two mesh attachments. `Tools/prepare_bank_entry.py` preserves the original mesh influences for the existing official Unity rig author; the resulting ef_slyinhang prefab uses the original yinhang texture through Resources.

## Verification

The actual BankWindow integration test also checks Bank progress initialization, 1/CD after assignment, full progress immediately after a threshold-crossing spin, zero on popup initialization, Base/Free visibility, actual button raycast, unchanged random state after its exact tip, and immediate old-model unsubscription plus refreshed saved count after GM reload. It captures `Artifacts/current-bank-progress.png` using the current production GameEntry scene at 1080x1920.

Full Unity 2022.3.62f3 PlayMode regression: `Artifacts/bank-progress-regression.xml`, **390/390 passed**, process 49244 exited. The new image was inspected and exposed the existing GM toggle covering the lower-left progress text. Its authored position was moved to the empty top-center header area (anchor/pivot .5,1; position 0,-12; size 96x64) without changing GM behavior. A final focused production-window test and fresh render verify this placement below. A fresh original-app image comparison remains pending; these checks do not establish complete pixel equivalence or full country routing.

Final focused PlayMode run: `Artifacts/bank-progress-final.xml`, **1/1 passed**, process 37120 exited. The regenerated current image was inspected: the Bank count is unobstructed and the GM toggle sits between the balance and level panels. The same test exercised the actual GM profile switch and refreshed Bank count after reload.
