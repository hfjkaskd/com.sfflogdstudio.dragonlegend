# Core rendering boundary audit

Inspected baseline b693a50c683050247b097a99096f17b249eb9cfd.
This audit is not a declaration that all gameplay objects satisfy the
no-UI-gameplay requirement.

## Confirmed current structures

- Main Base/Free symbol cells: RecoveredReelView instantiates configured
  RecoveredSymbolView prefabs. Symbol/cover are SpriteRenderer components;
  clipping uses SpriteMask. These are not UI Image symbols.
- Lucky Spin amount columns: RecoveredLuckySpinColumn uses authored Text
  and RectTransform components. Original dump class LuckySpinColumn at
  line 679620 has those same field types. RecoveredLuckySpinWindow receives
  an already generated reward and formats reward/100 into the target digits;
  the moving digits do not determine the payout. Existing native evidence is
  lucky-spin-columns.md (StartLuckySpin MoveNext 0x23b6960).
- Bonus card bodies and glows: RecoveredBonusItemTurn references
  RecoveredRegionAnimator, which drives RecoveredRegionRig, a MaskableGraphic
  with CanvasRenderer. Thus the selectable card body is still UI geometry.
  The Button is attached to that visible body and targets that same Graphic;
  BuildBonusCard.SaveItem does not create a separate invisible click proxy.
  This satisfies the same-object Button structure but does not by itself
  settle the separate no-UI-gameplay requirement for selectable card objects.
- Wheel reward labels/icons: RecoveredWheelItem uses Image/TMP text to show
  the configured reward. Rendering labels is distinct from selecting the
  wheel's result. This inspection does not classify the whole wheel hierarchy.

## Concrete Bonus migration dependencies still outstanding

RecoveredWorldRig provides native MeshFilter/MeshRenderer, shared clip data,
per-instance reusable geometry buffers and SelectClipData. It is a candidate
for card geometry, not a drop-in replacement for RecoveredRegionRig:

- RecoveredRegionAnimator stores region rigs and animation pose assets; the
  world rig consumes a different RecoveredWorldRigData representation.
- BonusItemTurn's conceal, turn, hold and glow clips must retain source
  attachment changes, colors and timing when converted.
- The source-shaped card bounds, current raycastPadding and standard Button
  target must remain a coherent authored interaction. A transparent proxy
  with separately positioned gameplay art is not an acceptable shortcut.
- Mesh geometry must preserve popup ordering, clipping, parent transforms and
  show/hide behavior, including when later reward windows overlap the cards.
- Validate actual rendered card poses and physical raycast delivery through
  the current production scene before replacing the existing prefab.

Recommended implementation reuses shared authored world-rig assets and each
instance's existing mesh buffers. Rebuilding skeleton data or instantiating
new geometry every frame would increase CPU/GC and is not recommended.
The existing world path still requires measured validation with all visible
cards and glows; this audit is not a performance acceptance result.

Original BonusItem dump at line 677404 has SpineUtils body/light, Text reward
and Image ad fields; retaining source appearance with official Unity components
requires conversion rather than restoring the original external Spine plugin.
No runtime or prefab changes were made, no new Unity suite was run, and SDK
handling was untouched. The preceding completed regression remains historical
evidence, not verification of an as-yet unimplemented Mesh card migration.
