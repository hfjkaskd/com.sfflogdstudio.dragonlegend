# Three-Wild-column presentation: recovered source contract

This is implementation input, not a claim that the presentation is connected or rendered.
The current playfield stops at `WildColumnsCheckRequested`. Keep that gap explicit until
the mesh, clipping, shake and downstream reward stages are actually implemented.

## Native control flow

Evidence root: `C:/Projects/Nut Sort Relax/reconstruction/mumu-current`.
`native-functions/game/23d1520.c` is `UIMainView.CheckWild3.MoveNext`.

1. Clear `UIMainView + 0x234` (complete Wild column count).
2. Visit columns 0 through 4. Read all three current board IDs. A match requires all three
   to be ID 7. Gaps between matching columns do not terminate the scan.
3. For each match, increment the count before presentation. Find symbol definition ID 7.
4. Call `RollReel.ShowSymbolEffect(null, 0, null)`, then `(null, 2, null)`, then
   `(info.SymbolEffects[1], 1, callback)`. The first two calls really hide symbols:
   they are not no-ops just because the prefab is null.
5. The callback immediately spawns `info.SymbolEffects[2]` under `UIMainView + 0x60`
   and sets its world position to the middle symbol visual transform. It requests sound
   and vibration 200, then plays `wild3` once and despawns this light on animation completion.
   This completion does not finish the column scan. The spawned persistent middle-column
   effect is separately retained by the reel's effect dictionary.
6. Shake `UIMainView + 0x78`, using cached original position at `+0x1c8/+0x1cc`,
   duration 0.3, intensity 20, frequency 20.
7. Await **0.36 scaled seconds per matching column**, including the last match. This is
   independent of the light's 0.8-second animation. There is no wait for nonmatching columns.
8. Complete the task after the scan (the final awaited task is already completed), allowing
   the caller to proceed to `CheckJackPot`. Do not unlock Spin here.

Timing constants verified directly in the ARM64 ELF PT_LOAD mapping:
RVA `0xdbc524` = `0.36000001430511475`, RVA `0xdbc664` = `0.30000001192092896`.
Ghidra references these with a `0x100000` image-base offset.

`native-functions/game/2376750.c` verifies ShowSymbolEffect ownership: check whether the
result object has already been handled, record it, disable its first child, return for
null prefab; otherwise pool-spawn under the reel's `+0x70` parent, set world position to
that disabled child, insert into the dictionary, then invoke the callback. Repeated calls
for the same result object return without playing again.

Callbacks: `23c226c.c` (spawn/play), `23c24d4.c` (despawn). The exact sound string pointer
is not resolved in this packet; do not invent a sound key from an asset filename.

Shake: `238f144.c`, `238f428.c`, `238f3bc.c`. The original uses a coroutine, subtracts
deltaTime before evaluating the falloff, and samples PerlinNoise(Time.time * frequency, 0)
for x and PerlinNoise(0, Time.time * frequency) for y. Each component is remapped to [-1,1],
multiplied by intensity and `AnimationCurve.EaseInOut(0,1,1,0)` at elapsed/duration,
then added to the supplied original position. It restores that original position after
the loop. The coroutine handle is static; starting another shake stops the previous handle.

## Prefabs and complete animation data

Original `Res/Prefabs/Wild3.prefab` contains two independently looping skeletons. Resolve
GUIDs, not misleading child names such as `SkeletonGraphic (ef_qizijinli)`:

| Prefab component | Original SkeletonData GUID | JSON | Contents |
| --- | --- | --- | --- |
| Wild3 first child, `idle` loop | 3e0ab536780f86c4ba75e670bcc63bd9 | ef_wild3.json | 170435 source bytes; 84 bones; 37 slots; 101 regions + 14 meshes; 138 timelines; 3 s |
| Wild3/Win3 child, `animation` loop | fa7a130506e652a42ae1bf17f5510d0a | ef_slwin3.json | 3567 bytes; 4 bones; 4 slots; 60 region attachments; 3 timelines; 20/30 s |
| Wild3Light, callback plays `wild3` once | dd40765e5ea89314e9eb1c1f03d4c006 | ef_wild1_3.json | 13132 bytes; 38 bones; 26 slots; 151 regions + 1 mesh + 1 clip; 111 timelines; 24/30 s |

The persistent Wild3 includes 12 weighted meshes, 2 unweighted meshes, and 12 deform
timelines. Bone inheritance modes are 77 normal, 4 NoScale, 1 OnlyTranslation and
2 NoScaleOrReflection. The existing region-only UI renderer cannot reproduce this data.
Use native world-space Mesh/SkinnedMeshRenderer structures for the core symbol effect,
in accordance with this project's prohibition on UI gameplay objects. Preserve original
slot order and PMA/additive blending; merely displaying a large Wild sprite is insufficient.

Wild3Light has a four-vertex unweighted clipping attachment `qty` at slot 1 ending at
slot 24, with its own deform timeline. The other deform timeline controls the four-vertex
`dizi_02` mesh. Clipping must affect the proper intervening slots and end at the specified
slot. Do not replace it with a generic rectangular UI mask or discard it during conversion.

All three JSON files contain the complete consumed binary data, source SHA256, atlas
regions, setup poses, weights, UVs, triangle indices and animation curves. They are stored
outside Unity Assets until an appropriate authoring path exists, so they add no player
startup resources and do not claim runtime support for clipping.

## Verification and reproduction

Run from the target project:

```powershell
& 'C:/Users/pc/AppData/Local/Programs/Python/Python314/python.exe' -X utf8 Tools/verify_wild_extraction.py 'C:/Projects/Nut Sort Relax'
```

The verifier re-extracts all three Wild assets and three already-used coin/lamp/burst
assets into a temporary directory, compares their complete JSON contents, checks clipping
deformation/end-slot data and asserts source files were not changed. It also compares
the Wild3 and Slwin3 reference atlases byte-for-byte against separately exported original
TextAssets. Both match. The extractor now rejects extra positional arguments and output
paths inside the input asset directory before touching any files.

The clipping binary layout was checked against the official
[Spine 4.1 SkeletonBinary reader](https://raw.githubusercontent.com/EsotericSoftware/spine-runtimes/4.1/spine-csharp/src/SkeletonBinary.cs).
No third-party runtime assembly was added. This verification covers extraction only;
it does not replace future Unity pose, clipping, lifecycle and fresh-frame visual tests.
