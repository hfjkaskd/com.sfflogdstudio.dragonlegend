# Treasure departure and destination presentation

## Original behavior

`UIMainView.FlyTreasureCard` RVA 0x23ba190 receives the current EndPos world
position and selected Sprite from Treasure window's event 14, after FlyCoin
has already started. Main field +0x170 references Image 114618682377417806 in
`ReferenceOriginal/Res/Prefabs/TreasureImg.prefab` (GUID
cca880bc0dff8ac4db56cec2f14346e2). It uses LeanPool.Spawn, sets the Image sprite
and native size, parents the object under Main with worldPositionStays=false,
sets local scale one, sets the supplied world position and activates it.

It registers .6-second DOScale(Vector3.one*.3) with default OutQuad before
FlyAnimUtils.Fly: .6 seconds, automatic height -1, ease 4/InOutSine, no side
offset. The quadratic control point is midpoint + world-up * distance*.3.
The destination position is captured immediately. Completion 0x23c2fe4 calls
LeanPool.Despawn(obj,0); there is no second award, popup or collection mutation.
The amount and Free continuation remain owned by the separate cash arrival.

`RecoveredTreasureDeparture` replaces LeanPool with official Unity ObjectPool,
reuses the authored original Image, and performs scale before position each
tick. Its global animation job preserves scaled-clock updates while disabled;
Unbind/owner destruction cancels jobs and releases images parented under Main.
It subscribes directly to the actual window's departure event when the real
FreeTreasureGame bundle is bound.

## Destination assets

Main field +0x178 is RectTransform 224060176988151448 at
`Node/Tubiao/Treasure/SkeletonGraphic (ef_shoucangicon)`. The destination bundle
retains the original Tubiao Rect (1080x499.17, center position -0.003418,322.62),
Treasure Rect (200x205, top-left position 0,-200), icon Rect (305.9997x264.99994,
center position -9,-10, pivot .5113022,.543063), and original TREASURE TMP label
with its font, material, gradient, 32px size and position. The original Button
is preserved as an official Unity Button; the source no-geometry Graphic is
represented by a transparent Image on that same Button object.

`Tools/extract_coin_effect.py` consumed all 18,500 original skeleton bytes for
`Res/Spine/按钮/shoucang/ef_shoucangicon.skel.bytes`. The resulting source data
contains 17 bones, 5 slots, 4 attachments (including 2 meshes), and the original
2-second idle loop with 21 timelines. `prepare_treasure_icon.py` produces the
offline flattened meshes and 39 independently sampled source geometry frames.
The Unity prefab uses native Animation, the existing recovered mesh rig and
the original atlas, loaded by Resources path with the required PMA import.
No Spine or other third-party runtime assembly was added.

## Validation and limits

Full graphics-enabled PlayMode regression: **337/337 passed**, recorded in
`Artifacts/treasure-departure-tests.xml`. The Treasure integration now observes
the real exit event after cash starts; checks sprite/native dimensions, Main
parent, initial scale, destination Rect, pause behavior, half-flight samples,
OutQuad scale, final .3 scale, pool return/reuse, cancellation cleanup, one cash
callback and one balance credit. The existing real two-ball Free test also
passes with the departure consumer connected.

All 39 source geometry samples match the native-animation rig. The existing
pose allocation test recorded 4.2899ms for 1000 samples and 0 managed bytes;
this measures pose sampling, not total rendering cost. The newly rendered
`Artifacts/current-treasure-departure.png` was visually inspected: the actual
selected treasure flies toward the animated left-hand collection icon while
cash flies separately.

The collection Button now opens the recovered window from Main, and the
destination belongs to that shared Main entry (see collect-entry.md). Full
lifecycle visibility and production Main Free wiring still need completion.
Event 7(4,1), collection redemption, Free end flow, missing
main artwork/layout and the visually dominant dragon remain incomplete.
This validation does not establish full lifecycle or whole-screen 1:1 fidelity.
