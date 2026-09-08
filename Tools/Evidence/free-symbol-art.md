# Free coin and ball native animation resources

Current reverse-project `TextAsset/ef_jinbi.skel.bytes` (21,139 bytes) and
`ef_longzhu.skel.bytes` (32,659 bytes) were fully decoded into
`Tools/Evidence/FreeSymbols`. The JSON includes binary hashes and all source
timelines. The extraction verifier also locates the original Res/Spine copy
by SHA-256 and reproduces both JSON files exactly without modifying inputs.

Coin clips (source order): glow; idle_bao, idle_cai, idle_chun, idle_jin,
idle_zhao; zcjb_b_bao, zcjb_b_cai, zcjb_b_chun, zcjb_b_jin, zcjb_b_zhao;
zcjb_chuxian; zcjb_idle. Ball clips: huo_lan, huo_lv, huo_zi; idle_lan,
idle_lv, idle_zi; start_lan, start_lv, start_zi.

`prepare_free_symbol_art.py` produces native authoring data and 942 independent
source geometry samples across all 22 clips, including both sides of keyframes.
`BuildFreeSymbolArt.Save` creates FreeCoinArt and FreeBallArt prefabs using
MeshRenderer, native Animation and the existing recovered world-rig sampler.
The native clip player swaps source timeline data while reusing one mesh and
its buffers per object. Atlas textures load through resource paths; their
premultiplied-alpha pixels are imported without Unity alpha dilation or lossy
compression, following the existing recovered effect pipeline.

These are animation-art prefabs, not complete JinBiEffectItem or LongzhuItem
replacements. Their preview defaults are zcjb_idle and idle_lan. Runtime ball
type selection must explicitly select the correct source clip. Parent scale,
reward text, clipping within the mini reels, pool lifetime and collection
motion are still to be authored and bound.

Relevant verified native entry points for that integration:

- PoolManager.CreateJinBi 0x238b040: scale .7, hide the parent slot's first
  child, reset anchored position. Only isInit=false calls PlayShowAnim.
- JinBiEffectItem.PlayShowAnim 0x23d95e0: hide glow and reward text, play a
  one-shot zcjb_chuxian clip, and run a .2-second scale tween to 1 followed
  by a .2-second return to .7. The animation completion callback 0x23d997c
  starts the zcjb_idle loop.
- PoolManager.CreateLongzhu 0x238b480 (also expanded in CheckFakeCoin): scale
  .8, hide the parent slot's first child, reset anchored position, use the
  shared ball index after RandomBallInfos, initialize the selected ball type,
  then increment the index.
- LongzhuItem.Init 0x23ae940 plays the selected type's idle loop and clears
  reward text. Its isInit argument does not alter that native method.

ELF relocation/string resolution for integration: 0x4f1eac8 points to
0x50655a0 (zcjb_chuxian), 0x4f1d1a8 to 0x50655a8 (zcjb_idle). Native float
at ELF 0xdbc540 (Ghidra 0xebc540) is .20000000298023224. GetBallAnim 0x23ae9f4
uses format {0}_{1} (0x4f1cf60 -> 0x5065b00), suffix zi for type 0
(0x4f1cf68 -> 0x5065620), lan for type 1 (0x4f1cf70 -> 0x50616b0), and lv
for all other types (0x4f1cf78 -> 0x5061a10). Thus type 0 is purple, type 1
blue, and the remaining branch green; do not infer enum order from atlas order.

Validation: 19 binary extraction checks passed; Unity 2022.3.62f3 full PlayMode
suite **264/264 passed** in `Artifacts/free-symbol-art-full-tests.xml`. Tests
cover every source geometry sample, one-mesh reuse, pause, completion exactly
once and cancellation on disable. Three fresh 2400x1600 contact sheets at
.173, .55 and 1.27 seconds were inspected at original resolution in
`Artifacts/current-free-special-clips-0.png` through `-2.png`. Their row-major
order follows the clip lists above; they are isolated art previews, not game
flow parity evidence. SDK handling is unchanged.
