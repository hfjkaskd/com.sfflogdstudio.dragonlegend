# Collection vertical ListView

RecoveredCollectList implements the fixed-size vertical path selected by
UICollectView.InitCollectCard (0x23ac6ec). BuildCollectList imports the actual
ReferenceOriginal/Res/Prefabs/ListView.prefab, preserving the transparent
raycast Image, viewport RectMask2D and top-left Content anchors/pivot. Official
ScrollRect retains Elastic movement, elasticity .1, inertia, deceleration .135
and sensitivity 1, with no scrollbars. Init selects vertical only. The original
CollectItem prefab is the template; runtime creates only visible copies.

## Native evidence

- Generic CreateList implementations are in dependency/26616f0.c and
  dependency/2661a90.c. Init (0x237e8a4) calculates geometry and creates inner
  records; neither initial path calls CreateCacheItemData (0x237ec7c).
- CalcRowsCols (0x237d26c), CalcContentSize (0x237d580), FreshAccumulation
  (0x237eedc) and FreshInnerPos (0x237f02c) give three columns of 310x360 cells,
  centers ((column+.5)*310, -(row+.5)*360), and content dimensions at least the
  viewport dimensions. Collection uses zero corner offsets and fixed sizing.
- Constructor (0x237faa8) sets hidePosition to Vector3.one*9999 and previous
  position zero. Update (0x237df78) refreshes when squared movement exceeds 2
  or data is dirty. No movement and no dirty flag means no item refresh.
- CheckInnerItem (0x237f118) first processes previously shown cells, returning
  offscreen cells, then processes previously hidden cells. CheckSingleItem
  (0x237f83c) uses inclusive cell/viewport edges, including cells touching the
  viewport boundary. Already shown cells refresh data only when dirty.
- AddItemToCache (0x237f4dc) moves cells offscreen without deactivating them.
  CollectItemData.ResetItem is empty. GetItemFromCache (0x237f788) takes index
  zero FIFO or creates a copy under Content at scale one. Newly shown items
  go first if the preceding index is hidden/absent, otherwise last.
- Reopening UICollectView marks data dirty without resetting scroll offset.
  Optional ListView strategies and drag callbacks are absent in this path;
  the actual ScrollRect handles dragging and inertia.

## Verification scope

RecoveredCollectListTests uses the real GameEntry configuration and prefab.
It probes exact boundary visibility, the squared movement threshold, FIFO
identity reuse, dirty-only count refresh and absence of saves. It then invokes
the actual ScrollRect drag lifecycle and verifies refresh preserves offset.
The current-collect-list.png capture is a list fixture inside current Main,
not proof of the complete collection window or whole-screen fidelity.

Full PlayMode regression passed 340/340 in Artifacts/collect-list-tests.xml.
The freshly generated capture was inspected: three-column cards scroll and
clip at the viewport top/bottom. Main's existing dominant dragon/background
and layout gaps remain visible behind this deliberately isolated fixture.

The collection window now includes Adapt safe-area behavior, popup animation,
A/normal header/progress branches and Close; see collect-window.md. Main entry
is connected in collect-entry.md. Treasure-set eligibility and production Free lifecycle remain separate
unfinished integrations. This implementation does not claim to restore every
generic ListView direction, dynamic sizing or optional strategy.
