# Collection window

BuildCollectWindow imports ReferenceOriginal/Res/ViewPrefabs/UICollectView.prefab
with its complete original RectTransform hierarchy, Image/Text/TMP/Button and
WorldSpace Canvas fields. The root belongs below Main's Canvas. The original
Gold bitmap font, glyph metrics and material are copied; its material uses
the existing original font_a texture and Unity's built-in shader. TMP font
and material references retain existing source identities.

The two ef_shoucanggl nodes use the recovered native Animation/mesh prefabs at
their exact source positions, pivots and sibling indices. TitleBg uses recovered
UIShiny with source effectFactor .20301695 and the original two-second loop.
Sprite imports retain 100 pixels/unit and source nine-slice borders: bar_01
(30,0,28,0), bar_02 (22,0,23,0), bg02 (0,0,0,1273); the other images have zero
borders. These values come from the corresponding ReferenceOriginal sprite
assets, not approximations from screenshots.

## Native controller

- OnInitProperty 0x23ac2dc selects popup/black mask (300,2). The prefab authors
  the existing framework's first-sibling .65 black input blocker and depth 300.
  General multi-window stack ordering is still a separate integration gap.
- OnInit 0x23ac2fc shows Gold/hides Green for A, returning before initializing
  reward text or cached fill width. Normal mode does the reverse, formats
  GetCollectReward(language), and caches Fill.rect.width. Progress is actually
  inside Green in the original hierarchy, so A hides the entire progress area.
- OnBeforeShow 0x23ac498 creates the ListView once or sets its data-dirty flag.
  It formats raw PlayerCollectDatas.Count and GetCollectInfos.Count with
  `{0}/{1}` (ELF relocation 0x4f1d9c8), then sets width to cachedWidth*count/total
  with the current rect height. No duplicate/received/unknown-ID filtering or
  clamping is added; a newly initialized A window keeps cached width zero.
- OnClickButton 0x23acab0 handles CloseBtn, plays click and hides. There is no
  per-card action, claim, ad request, award or save in this window.
- PopupWindow ctor 0x23f9ea0 and animation methods 0x23f9afc/0x23f9ccc use .3
  seconds, fromScale 0/toScale 1, OutBack (27) entry and InBack (26) exit. Each
  transition resets its initial scale; the shared animation runner uses scaled
  time and tolerates owner destruction. Closing retains the same list and offset.
  CreateList initializes the unparented prefab before SetParent(parent,false),
  matching dependency/26616f0.c and ResManager.InstantiateGameObject 0x238e810.

## Safe area

RecoveredSafeArea restores Adapt Awake 0x238c094, CacheScaler 0x238c0fc,
AdaptScreen 0x238c210, OnEnable 0x238c4d4 and Start 0x238c4d8. Raw ARM confirms
Awake caches RectTransform/scaler and tail-calls AdaptScreen. OnEnable and Start
also call it; this class has no native Update or resize callback. Scaler lookup
prefers GetComponentInParent<CanvasScaler>, then the UI root scaler. The current
window Bind supplies the latter explicitly from Main instead of introducing a
second UIManager singleton. This covers this window's parented lifecycle; a
global manager for arbitrary detached windows remains outside this integration.

For screen W,H, reference resolution Rx,Ry and match m, native factor is
`m*Ry/H - Rx*(m-1)/W`, a linear combination, not CanvasScaler's logarithmic
scale factor. The anchors become (0,0)/(1,1). Offsets are the safe-area pixel
insets multiplied by that factor, retaining the native operation order in
code. Missing rect/scaler, screen dimensions <=1, safe-area dimensions <=1,
or reentrant application return without applying. There is no Editor bypass.

## Verification and remaining integration

RecoveredCollectWindowTests checks normal/A presentation, raw duplicate and
unknown record progress (including 18/15), source sprite borders, OutBack
overshoot, paused scaled time, actual Close Button binding, retained list
identity/scroll offset, refreshed item counts and no saves. Separate geometry
checks cover asymmetric safe insets at width/height match 0, .5 and 1, invalid
dimensions and the detached root-scaler path. Captures are generated from the
current GameEntry and the actual collection window prefab.

Full PlayMode regression passed 342/342 in Artifacts/collect-window-tests.xml.
Visual inspection exposed a fixture error despite green tests: assigning the
portrait render target only after opening resized Main but left the native
fixed ListView viewport at its previous size. The fixture now establishes the
1080x1920 output before opening and asserts viewport height 1176. Both checks
passed in collect-window-focused.xml, then 2/2 passed again after matching the
native initialize-before-parent order in collect-window-parent-order.xml.
The fresh normal/A captures were inspected. The normal fixture intentionally
retains scroll offset 150 and shows unclamped 18/15 progress; this is a raw-data
edge-case fixture, not an assertion that ordinary players have duplicate records.

Main's native Treasure click is located at 0x23bd4e8: string Treasure
(0x4f1e2e0), PlaySound(click), ShowWindow<UICollectView> (0x4f1e2b0). Connecting
that production entry requires sharing the original icon/destination currently
inside the callable FreeTreasure bundle, plus auditing original visibility and
window ownership. That binding, complete-set award branch, production Free
lifecycle and remaining Main visual/layout gaps are not claimed complete here.
