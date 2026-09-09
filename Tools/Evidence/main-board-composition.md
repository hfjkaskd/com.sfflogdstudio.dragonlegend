# Main board composition

The current production scene omitted QiPan/Bg and drew the recovered body as UI
over native world reels. The source UIMainView Rect 224422702022971346 lists its
children in this order: LongSpine 224067212629291419, Bg 224208269818966246, text,
coin, two reel containers, Result, FreeResult, PlayFire, and the remaining child.
The source body is therefore behind the board background and reel symbols, while
the fire layer remains above them. Moving the entire NPC (including fire) to a
background sorting order would lose that relationship.

BuildMainComposition copies the complete original Bg Image/Rect blocks, replacing
only the official Image script and recovered sprite GUID references. The sprite
e6e46f6773a726a4bbbcaa70e2f95c73 maps to Res/UI/zhujiemian/zjm_bg_qipan. Original
size is 1072 x 757, centered anchors/pivot, position (0,-26), scale one, white
Simple Image, preserveAspect false. It remains a prefab-authored visual.

Native Canvas sorting preserves source order across the recovered world objects:
Main backgrounds -4, source fireworks -3, dragon body -2, board background -1, Main/reels at the
existing Main order. Each offset is relative to the actual Main Canvas and uses
its camera and sorting layer. Only the body receives the separate Canvas; the
PlayFire effect remains in its original foreground path. No geometry, bone,
animation, anchor or scale is changed. Runtime Bind enables sorting after nesting.

The body Canvas lives on a stretched Layer 5 DragonLayer parent. Attaching it
directly to the original Layer 0 body makes the Main camera's Layer 5 culling
mask suppress the entire body Canvas. The parent preserves the body's original
Layer 0 and local rect while exposing its UI draw surface to that camera. Authoring
unpacks only the NPC composition wrapper so the body can be reparented; both
large native rig prefabs remain linked rather than duplicating their mesh data.

The production test now counts visible gold pixels in the dragon region between
jackpot and board, in addition to validating the board sprite/rect and relative
Canvas orders. An unrelated cash-flight GM assertion previously assumed 100
frames completed asynchronous profile loading; it now waits for the actual new
player and CashFlight instance with a five-second deadline before inspecting it.

Source LongSpine and FireSpine both have layoutScaleMode 0, referenceScale 1,
referenceSize 990 x 1245.0002 and localScale one. Their skeleton asset has scale
0.01. The existing graphic conversion uses source pixel geometry, consistent
with the original SkeletonGraphic pixel presentation; this change does not use
an inferred dragon shrink to compensate for missing artwork or drawing order.

This restores the identified missing board layer and body ordering. It does not
prove all Main composition, screen adaptation or Free foreground branches match.
Fresh production captures and the complete PlayMode suite are the verification
scope for this change.

Validation: `Artifacts/main-composition-final-tests.xml` passes 348/348 PlayMode
tests. Fresh current-main-background-base/free.png captures were both inspected:
body visible, jackpot labels unobscured, source board background present in front
of the body, reel symbols visible above that background. Both captures retain
Base foreground; the Free background capture does not prove Free mode integration.
