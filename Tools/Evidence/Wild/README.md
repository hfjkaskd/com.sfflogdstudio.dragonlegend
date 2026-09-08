# Three-Wild-column presentation: recovered source contract

The persistent Wild3 and clipped Wild3Light prefabs are converted to native world-space
meshes and connected to the actual post-coin scan, with board shake and pool ownership.
The current playfield stops at the new `JackpotCheckRequested` continuation. Jackpot
presentation and the subsequent reward stages remain unfinished; the Wild verification
does not establish completion of the full game lifecycle.

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

Callbacks: `23c226c.c` (spawn/play), `23c24d4.c` (despawn). The sound key is **`change`**.
The original ELF `.rela.dyn` has R_AARCH64_RELATIVE (`0x403`) at `0x4f1e4e8` with addend
`0x505ea78`, which `analysis/script.json` maps to `change`. The animation pointer at
`0x4f1e4e0` relocates to `0x5064fd8`, mapped to `wild3`. Read relocation records as well
as file bytes: both pointer locations contain zero in the file before loader relocation.

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

## Native persistent Wild3 implementation

`Assets/Resources/RecoveredSymbols/Wild3.prefab` has four Transform objects and two
MeshRenderers, with a SortingGroup at the root. The two original skeletons use separate
native Animation clocks (`idle` 3 seconds and `animation` 20/30 seconds, looping).
Their serialized data preserves every region/mesh attachment, influence, setup pose and
deform frame. No CanvasRenderer is used for these core symbol visuals.

`RecoveredWorldRig` evaluates the original normal, OnlyTranslation, NoScale and
NoScaleOrReflection modes, then blends weighted vertices after applying the original
per-influence deformation. Unweighted deformation values are absolute, as extracted;
weighted values are offsets. A single per-frame interpolation curve is shared by all
coordinates in a deform frame, rather than expanding thousands of coordinate curves.
Each instance keeps its own pose and mesh buffers; the definition and AnimationCurves
are shared immutable ScriptableObjects. Textures load by Resources path on first use.
Slot order and PMA/additive colors are preserved in one mesh per skeleton.

The scale is 100 pixels per world unit, matching the existing native reel coordinate
system. The original GUI skeletons used scale .01 multiplied by Canvas reference pixels
per unit 100; the target reel's world-space transform supplies the corresponding conversion.
The source prefabs have no additional local offsets or scales in their Wild3 hierarchy.

Reproduce authoring from the target project:

```powershell
& 'C:/Users/pc/AppData/Local/Programs/Python/Python314/python.exe' -X utf8 Tools/prepare_wild_mesh.py
& 'C:/Program Files/Unity/Hub/Editor/2022.3.62f3/Editor/Unity.exe' -batchmode -projectPath C:/Projects/com.sfflogdstudio.dragonlegend -executeMethod BuildWildWorld.Save -quit -logFile C:/Projects/com.sfflogdstudio.dragonlegend/Artifacts/wild-world-author.log
```

`Tools/sample_wild_reference.py` independently evaluates the extracted source in Python
and writes `Tools/Evidence/wild-world-samples.json`. The PlayMode test compares every
visible vertex at six times for the dragon and four times for the border, including
non-keyframe times .173, .73 and 2.91. It also checks a rotated/trimmed mesh UV against
the original atlas numbers, instance isolation, native Animation pause/resume, current
rendered pixels and steady-state managed allocations. Fresh images are written to
`Artifacts/current-wild-world-0.png` and `current-wild-world-1.png`.

While checking the original rotation rules, an existing WinBurst region UV error was
identified and fixed: a region rotated 90 degrees maps geometric BL/TL/TR/BR to packed
BR/BL/TL/TR after Unity's V flip. Its previous mapping was reversed by 180 degrees.
The authored burst asset has been regenerated, and its bkbai0 UVs now have explicit
source-number assertions. Reference geometry/UV rules were checked against official
[Bone.cs](https://raw.githubusercontent.com/EsotericSoftware/spine-runtimes/4.1/spine-csharp/src/Bone.cs),
[MeshAttachment.cs](https://raw.githubusercontent.com/EsotericSoftware/spine-runtimes/4.1/spine-csharp/src/Attachments/MeshAttachment.cs), and
[RegionAttachment.cs](https://raw.githubusercontent.com/EsotericSoftware/spine-runtimes/4.1/spine-csharp/src/Attachments/RegionAttachment.cs).

Remaining: jackpot and the other awaited post-spin branches. The authoring path explicitly
rejects unsupported bone/attachment formats instead of silently discarding them.

## Native Wild3Light clipping and one-shot lifecycle

`RecoveredSymbols/Wild3Light.prefab` uses the same world-space renderer with all 38 bones,
26 slots and 153 attachments from `ef_wild1_3.json`. Its sole mesh and clipping quad each
keep their original deformation. `RecoveredConvexClipper` clips individual triangles to
the transformed original quad and interpolates UVs at intersections. Slot 24 is processed
inside the clipping range, then clipping ends even if that slot's attachment is hidden.
Slot 25 (`kuang`) remains outside the clip, as in the original renderer.

The source quad and its deformed pose are validated for convexity during authoring.
This is an exact geometric conversion for the present source; concave, nested and weighted
clipping are not silently approximated. Transformed geometry and both polygon work buffers
are retained between frames. Native Mesh vertex/index lists are preallocated for the
maximum seven-vertex polygon resulting from clipping a triangle against four half-planes.

`RecoveredWildLight.Play` resets the pose, plays the native `wild3` Animation once, and
emits `Completed` at its independent 24/30-second completion. The owner can return it to
its pool from that callback. Disabling it cancels the callback and retains mesh buffers
for reuse. The prefab does not auto-play; the eventual scan callback starts it explicitly.
Its animation is saved as `wild3.anim`, because Animation clip aliases assigned during
editor authoring do not persist under a different serialized clip name.

Author it after running `Tools/prepare_wild_mesh.py` with Unity execute method
`BuildWildWorld.SaveLight`. `Tools/sample_wild_light.py` independently calculates clipped
coverage and first moments at nine times directly from the original data. Tests compare
these to the native mesh, check UV interpolation analytically in both windings, reject
empty masks, and verify one-shot completion, pause, cancellation and mesh reuse.
Fresh `Artifacts/current-wild-light-clipped.png` shows the real light over the persistent
Wild. `current-wild-light-unclipped-control.png` is explicitly a same-frame QA control with
only the clipping disabled; it is not an original-game reference or a desired final frame.

The clipping integration changes the common world renderer, so the full persistent Wild
numeric/render tests and the existing gameplay PlayMode suite must pass before committing.
The scan connection is described below. Jackpot/bonus/free-game stages remain active goal work.

## Actual post-spin Wild chain

`RecoveredSpinPlayfield.BeginWilds` now follows the real `RecoveredBonusCoinSequence`
completion. `RecoveredWildSequence` reads all three live symbols in each column, counts
each complete ID-7 column (including gaps), invokes the presenter synchronously, then
waits .36 scaled seconds including the final match. A no-Wild scan completes immediately.
Profile cancellation inside a callback stops further reads, waits and completion.

The presenter hides rows 0, 2, then 1 using a common shown-slot set owned by
`RecoveredReelView`. Coin stop presentation uses this same set, preserving the original
cross-effect duplicate suppression. A null-effect outer-row call still hides its symbol.
The set resets on native wrap/Clear before the following SetImg pass; despawning an effect
does not itself reactivate its source symbol.

For a newly shown middle row, the presenter pool-spawns the persistent Wild at the middle
symbol's world position, then the entry light at that position. It requests sound `change`
and vibration 200 before starting `wild3` once. These remain request events, consistent
with the current external audio/vibration handling; SDK implementation is unchanged.
The light's completion returns it to its own official Unity ObjectPool. It does not block
the .36-second scan. Persistent Wild instances belong to their reel's Clear event.

`RecoveredWildColumn` stores both native Animation phases before deactivation and restores
them after activation, matching the original skeleton tracks retaining time while pooled.
Original [SkeletonGraphic.OnDisable](https://raw.githubusercontent.com/EsotericSoftware/spine-runtimes/4.1/spine-unity/Assets/Spine/Runtime/spine-unity/Components/SkeletonGraphic.cs)
clears rendering without clearing the animation state. The persistent idle and border
clocks are independent; the entry light explicitly restarts at zero instead.

The original UIMainView binding resolves ShakeNode to QiPan and Result to its child:
Result anchored position (.003418,335.14), size (1080,1919.72), centered anchors/pivot.
The authored target prefab now contains that Result node. A configured scale-100 child
bridges its pixel coordinate system to the world-space mesh units. Both Wild effects
inherit QiPan motion. Creation-order sorting preserves overlap ordering between newly
spawned persistent columns and entry lights; cached buffers and pool instances are reused.

`RecoveredBoardShake` uses the original coroutine algorithm: .3 seconds, intensity 20,
frequency 20, EaseInOut falloff 1 to 0, subtract-delta-before-sample, global replacement of
an existing shake, absolute Time.time Perlin sampling and restoration of the cached initial
anchored position. The Wild presenter calls it after the light spawn/play, even when the
middle row was already shown. Bind/unbind cancellation restores the original position.
Other pre-existing shake request sites are not automatically presumed equivalent.

When the scan completes, `JackpotCheckRequested(count)` runs while any remaining entry
lights continue. The actual jackpot popup/task progression is not connected yet. Spin
stays locked and AwaitingRewards stays true; neither reel stop nor Wild completion credits
the balance or substitutes for CheckBaseEnd.

Validation: `Artifacts/wild-chain-all-tests.xml`, 160/160 passed. The actual Unity Button
starts a spin; the test applies the original guaranteed-five-Wild board stage before reel
stops, then checks all five presentations, .36-second waits, exact shake samples, pause,
five sound/vibration requests, independent light completion, source hiding, phase-preserving
pool reuse, duplicate suppression shared with coin effects and profile unbind cleanup.
`Artifacts/current-wild-chain.png` is the fresh actual main-camera capture at the third
column's presentation. Rebuild with Unity execute method `BuildWildWorld.SaveAndConnect`.
