# Native cash-flight continuation

Recovered from the Dragon Legend binary under `reconstruction/mumu-current`, not the
unrelated Nut Sort dump. This describes the required next presenter; the actual entry
still has no consumer for `RecoveredSpinPlayfield.FlyCoinRequested`.

## Timing and geometry

`UIMainView.PlayFlyCoin.MoveNext` (`23d440c`) spawns ten objects from PoolManager's
`_flyCoinPool` (+0xa8). Every spawn uses the current top window as parent and
worldPositionStays=false. It sets localScale to one. An optional source Transform
overrides the object's world position; without one, it retains the pool spawn position.
Each item becomes the last sibling, draws two integer Random.Range(-150,150) values,
and starts a .3-second local-position scatter tween to its current local position plus
those x/y offsets. The upper random bound is exclusive.

After a .3-second scaled UniTask wait, the method schedules ten DOVirtual.DelayedCall
callbacks at index * .03 seconds with ignoreTimeScale=true. Scheduling finishes the
async method; it does not itself signify arrival or release the main Spin wait.

Both constants were read directly from the ELF PT_LOAD mapping:

| ELF RVA | IEEE float value | Use |
| --- | --- | --- |
| dbc664 | 0.30000001192092896 | scatter, wait, flight duration, automatic arc ratio |
| dbc67c | 0.029999999329447746 | per-item departure stagger |

Departure callback `23c3188` snapshots the object's current world position and
TopTitle.CashImg.transform.position, then calls FlyAnimUtils.Fly (`238c74c`). Direct
ARM64 instructions `23c329c..23c32d8` prove duration=.3 (s6), height=-1 (s7), ease=4
(w2), and zero lateralOffset/impactForce (stack stores at sp and sp+8).
The utility resolves negative height to distance * .3 and uses the midpoint plus
Vector3.up * height as the quadratic Bezier control point. Ease 4 is InQuad.
The destination is captured at departure, not continually tracked during flight.
Flight tween updates use scaled time, separately from the unscaled departure delays.

## Arrival and completion order

Per-item arrival `23c3300` performs these operations in order:

1. Increment the shared completed count.
2. Despawn the cash item through pool +0xa8.
3. Play sound `fly`.
4. Spawn a collection effect from pool +0xb0 under TopTitle.CashImg, with
   worldPositionStays=false.
5. Play its `shouji` animation with loop=false and a cleanup callback.
6. When completed >= list.Count, invoke the all-arrived callback.

The effect cleanup (`23c3554`) despawns only its effect through pool +0xb0. It does
not delay the cash credit or main-flow callback until the effect has finished.

ELF RELATIVE relocations, resolved against analysis/script.json:

| Pointer RVA | Relocated metadata address | Meaning |
| --- | --- | --- |
| 4f1e548 | 50636d8 | string `shouji` |
| 4f1e550 | 50602a0 | string `fly` |
| 4f1e940 | 5010eb0 | UIMainView_TypeInfo |

All-arrived callback `23c306c` invokes the caller first, resets TopTitle second, and
only then reads the latest GreenCount and applies GreenCount + amount. The existing
`RecoveredRewardBranches.CompleteFlyCoin` preserves this ordering and setter behavior.
Do not move credit to popup close or wait for all collection-effect animations.

## Pool assets and version-dependent appearance

PoolManager.InitFlyCoinPool (`238a9c8`) binds serialized FlyCoin (+0x40) to pool +0xa8;
InitShoujiPool (`238aadc`) binds Ef_Shouji (+0x48) to pool +0xb0. Both set persist=true
(+0x41), preload=10 (+0x38), and preload the objects. These offsets were checked against
the actual LeanGameObjectPool field declarations. They do not specify capacity=10.
Unity's official pooling API should replace LeanPool while preserving this behavior.

The exported `Res/Prefabs/FlyCoinItem.prefab` has centered anchors/pivot, zero local
position, scale one, Image and FlyCoinItem components. Its authored size is 73x76.
FlyCoinItem.Start (`23ae84c`) chooses sprites[0] for GameData.isA=true and sprites[1]
otherwise, followed by an Image virtual call whose target still needs verification.
Do not assume the final displayed size remains 73x76 until that call is resolved.

Original sprite GUID mappings:

- 3bd2980dcd235f44ab2c2ea76f284da8: Res/UI/zhujiemian/zjm_hb_a.asset
- 9b082740e1e07b545873b2fcd0099bbf: Res/UI/zhujiemian/zjm_hb_b.asset

`Res/Prefabs/Ef_Shouji.prefab` uses a centered 50x50 RectTransform, scale one,
CanvasRenderer.cullTransparentMesh=false and the skeleton asset GUID
cfb3a290e733f5b4f9e6519ba499cdd2, mapped to
`Res/Spine/shouji/ef_slshouji_SkeletonData.asset`. Its authored starting animation loops,
but the arrival explicitly plays `shouji` once. It is a different resource from the
already converted jackpot popup `ef_shoucanggl`; do not substitute that effect.

## TopTitle placement

The field at TopTitle +0x28 is CashImg, not GreenTxt. If the current top window is
not UIMainView, flight preparation reparents TopTitle to that window with
worldPositionStays=false, makes it last sibling and calls SetPosition (`23b9280`).
Start (`23b8e5c`) obtains its saved position through UI-world-to-screen conversion and
ScreenPointToWorldPointInRectangle against MainViewNode with the UI camera. Preserve
that coordinate conversion rather than caching an arbitrary local position.

ResetToptitle (`23b960c`) reparents to serialized MainViewNode (+0x30), false,
restores saved world position (+0x58), then SetSiblingIndex(2). The final index is proven
by `mov w1,#2` and tail call `4967428` at `23b9660..23b966c`.

## Remaining implementation and verification

Resolve the FlyCoinItem Image virtual call and exact pool spawn reset behavior;
convert the actual ef_slshouji skeleton with the existing offline native-Unity pipeline;
author the two pooled prefabs and timing parameters; wire the presenter to the real
entry and balance target. Verify both A/B visuals, pause behavior across stagger/flight,
overlapping effect lifetimes, cancellation on GM profile changes, callback/reset/credit
ordering and new actual-entry captures. The existing 187-pass suite covers the jackpot
boundary only and must not be cited as evidence that this flight has been implemented.
