# Collection item display

`RecoveredCollectItem` restores `CollectItem.InitUI` (RVA 0x23ac03c), and
`RecoveredPlayerProgress.GetPlayerCollectData` restores the query at 0x236f708.
The actual `ReferenceOriginal/Res/Prefabs/CollectItem.prefab` is imported with
official Image/TMP scripts and the recovered controller. Its root remains
279x328 with original background, icon, red badge, label, positions, font and
material. It is a display item and contains no Button or claim interaction.

The original sprite names are `t_icon_0{0}` below ID 10 and `t_icon_{0}` for
other IDs (ELF relocations 0x4f1dbf8/0x4f1dc00). The serialized ID-to-Resources
paths retain all fifteen corresponding original sprites. Init assigns the
sprite and calls SetNativeSize before querying the player record.

The query returns null for a null collection list or absent ID, otherwise the
first matching record. It does not filter count, isRecieve, or duplicates and
does not save. Missing items use RGB .14509804546833038, alpha 1 (ELF float
0xdbc5bc, equivalent to RGB 37/255), then hide the red badge without resetting
its text. Existing items use white, show the badge and format the first
record's raw count with `{0}` (0x4f1dbf0), including zero or negative values.
Received records retain the same acquired presentation. No grayscale shader,
invented reward availability, forced-positive count or additional save is used.

## Verification scope

The query test covers null/empty lists, duplicate IDs, first-record identity,
received/zero/negative records and no saves. The prefab test loads real
GameEntry configuration, reuses the item across all 15 source icons, checks
native image dimensions/tint/badge/text, and renders three actual item prefabs
showing missing, count 1 and received count 5. That display fixture is not a
restored collection window or its virtualized list.

Full PlayMode regression `Artifacts/collect-item-tests.xml` passed 338/339.
The failing check assumed the original atlas sprite name instead of the
extracted asset's ID-suffixed filename. Correcting only that test expectation
gave 2/2 passing item tests in `Artifacts/collect-item-focused.xml`; runtime
and prefab data were unchanged. The latest
`Artifacts/current-collect-items.png` was inspected and shows the missing,
owned and received fixtures with the original item art/tints/count badge.

## Located collection-window behavior for the next integration

`UICollectView` is PopupWindow, with original prefab
`ReferenceOriginal/Res/ViewPrefabs/UICollectView.prefab`. Its CollectItem
reference is source GUID 84470bc761418094bb01fbd82c86a65f / component
114243736805655561. OnInit 0x23ac2fc shows Gold/hides Green in A and returns
early; only normal profile sets GreenTxt from GetCollectReward and captures
FillImg.rect.width at +0xb0. OnBeforeShow 0x23ac498 calls InitCollectCard, writes
raw PlayerCollectDatas.Count / GetCollectInfos.Count, and sets FillImg.sizeDelta
to (cached width * raw record count / config count, current rect.height),
without clamping or de-duplicating. The A early return leaves width at its
default for a new window; do not accidentally normalize this native branch.

OnClickButton 0x23acab0 handles only `CloseBtn` (0x4f1cf30), plays `click`
(0x4f1cf38) and hides. There is no direct collection reward claim in this
window. Complete-set rewards belong to a different Main branch.

InitCollectCard 0x23ac6ec creates the original ListView once with cell 310x360,
Direction.Vertical (1), repeat count 3, actual config item count, ScRect as
parent and auto positioning enabled. Reopening only sets its data-dirty flag
at +0x11c; it does not recreate the list or reset scrolling. Callback
0x23acbec gets config[index] and calls InitUI; 0x23accec and 0x23accf0 do not
add item-specific behavior. CollectItemData.ResetItem 0x23acd58 is empty.

The native ListView uses official ScrollRect with visible-item reuse, not a
fully instantiated grid. The separate CreateCacheItemData helper at 0x237ec7c
computes min(total, repeat * truncate(view height / cell height)), but is NOT
called by the initial CreateList/Init path. Initial cells are allocated on
demand in the first refresh. AddItemToCache 0x237f4dc calls ResetItem,
moves to the configured offscreen hide position, and appends if absent;
GetItemFromCache 0x237f788 takes index 0 (FIFO), allocating when empty.
CheckSingleItem 0x237f83c compares each positioned cell to the viewport using
half extents, hides/recycles offscreen cells, positions newly shown cells and
refreshes their data; data-dirty refreshes already visible cells. Remaining
ListView geometry, ScrollRect initialization and refresh scheduling are
now documented in `collect-list.md` with their recovered prefab/controller.

The collection window and Close are now restored separately in collect-window.md.
Main entry binding, complete-set reward branch, production Free lifecycle and
whole-screen visual fidelity remain unfinished. These item checks do not
establish those requirements.
