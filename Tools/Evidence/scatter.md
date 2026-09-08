# Scatter stop / Free-entry visual

Original `ReferenceOriginal/Res/Prefabs/Scatter.prefab` uses a misleading child
name `SkeletonGraphic (ef_qizijinli)`: its skeleton GUID
`e0412f8fe2a39b94fa077d864bdadd61` resolves to
`Res/Spine/棋子/scatter/ef_scatter_SkeletonData.asset`. Source child size is
187x172, pivot (.49999967,.5), position zero, scale one. The full 7256-byte
binary is extracted into `Symbols/ef_scatter.json`, with clips `idle` (1 second),
`png` (zero duration) and `start` (.6666666865 seconds).

## Native behavior

- RollReel.PlayStopAnim `2376a78` clears scaterAnims (+b0) and BonusAnims (+b8),
  then visits rows 0..2 in order, showing effects for IDs 9/10.
- ShowSymbolEffect `2376750` claims a row in shown-effects before hiding its
  source symbol, spawning the pooled prefab under the effects root and assigning
  source world position. An already claimed row returns before its callback.
- Callback `2377960` adds ID10's ScatterEffect to scaterAnims, calls
  StarAnim(null), requests vibration 200 and sound `scatterShow`.
  Metadata pointer 0501c378 resolves through ELF RELA to 05063308.
- ScatterEffect.StarAnim `23dae44` plays `start` once and forwards completion;
  callback `23daf44` does not switch to idle. The last pose remains displayed.
- RollReel.PlayScatterAnim `2377260` iterates the current scaterAnims list and
  calls IdleAnim(true) (`23dade0`). Native pointers 0501cf58 and 0501dd00 resolve
  to `idle` and `start`. Repeating PlayStopAnim clears the list but the original
  shown-row set prevents respawning, so those active effects are no longer
  included in the subsequent PlayScatterAnim call until a reel Clear.
- Clear returns pooled effects without reactivating source symbols; subsequent
  SetImg/column refresh restores the source graphics.

## Implementation

`BuildScatter.SaveAndConnect` creates a standard GameObject/MeshRenderer/
Animation/SortingGroup prefab with one reusable mesh. The three clip definitions
share the original skeleton topology. `RecoveredWorldRig.SelectClipData` changes
the pose definition while keeping its allocated buffers and mesh. Original
image loading remains by Resources path; no third-party runtime is included.

`RecoveredScatterPresenter` owns the pooled effects and the separate native
current-list lookup. It is serialized into the actual SpinPlayfield and bound
by GameEntry's existing playfield setup. The existing stop pass calls it in row
order interleaved with coin effects, preserving native sound/vibration ordering.
`BuildSpinPlayfield.Save` also attaches it so reauthoring retains the binding.
The original pixel scale is represented through the existing world-effects
parent's 100 scale and 100 pixels per unit; source world placement is retained.
Scatter sound requests feed the playfield's existing SoundRequested event.

## Verification and remaining scope

`sample_scatter_reference.py` independently computes 105 source geometry frames,
including key boundaries. Tests check all three clips against these vertices,
single Mesh reuse, zero CanvasRenderers and zero allocations across 1000 warmed
pose samples. The actual GameEntry prefab test verifies interleaved stop
callbacks, count/row ownership, looping, paused native time, completion once,
the native repeated-stop list behavior, and pool reuse after reel refresh.

Final Unity 2022.3.62f3 full PlayMode regression: **255/255 passed**
(`Artifacts/scatter-final-full-tests.xml`). The initial run's single failure was
an exact Vector3 equality across transformed parents; the final assertion uses
the existing world-coordinate tolerance .0001, with no positioning workaround.
Fresh `Artifacts/current-scatter-clips-{0,1}.png` were inspected at original
1200x400 resolution: left start, middle idle, right png.

The actual CheckFreeGame consumer still needs to invoke PlayScatterAnim before
NPC/ring/start-window/transition. Full Free/BaseEnd continuation, shared audio
binding and UIManager lifetime remain pending. The all-Wild1 column's three
scatterShow requests inside the row loop have now also been restored; see
`free-entry-flow.md` for the follow-up verification.
These tests establish Scatter geometry and the implemented stop behavior, not
complete gameplay or visual parity across the full lifecycle. SDKs are unchanged.
