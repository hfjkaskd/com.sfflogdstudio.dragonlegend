# Bonus card artwork conversion

The current original `UIBonusView.prefab` identifies both body and glow with
SkeletonData GUID `6775d68dc5bae0149bb430c2e73366fc`. The original GUID map resolves
this to `Res/Spine/棋子/jinbi/ef_jinbi_SkeletonData.asset`, despite the GameObject
being named `SkeletonGraphic (ef_qizijinli)`. Do not select artwork by that name.
Both source RectTransforms are 187 x 172, pivot (0.49999967,0.5), position zero.
Their source components use scaled time and the same atlas.
`BonusItem.Init` (`23959f4`) explicitly starts `zcjb_idle`, overriding the serialized
`idle_chun` value. The complete item presenter must use that initialization.

`Assets/Whitebox/Editor/RecoveredCoinEffect.json` already contains the fully
consumed, hash-verified original binary. Earlier CoinAppearance conversion covered
only zcjb_idle and zcjb_chuxian. Bonus needs these additional animations:

| Type | Turn | Hold |
| --- | --- | --- |
| Reward | zcjb_b_chun | idle_chun |
| ZHAO | zcjb_b_zhao | idle_zhao |
| CAI | zcjb_b_cai | idle_cai |
| JIN | zcjb_b_jin | idle_jin |
| BAO | zcjb_b_bao | idle_bao |

Independent glow is the `glow` clip. The full native prefab preserves all 13 clips,
29 bones and 59 attachments, including the two unweighted deforming meshes
`coin_c` (70 vertices) and `ringadd` (45 vertices). No runtime Spine assembly is
introduced. `BuildBonusCard.Save` uses the same original PMA atlas and the existing
native UI region/mesh renderer. This is window presentation, not reel gameplay.

ELF RELA plus `analysis/script.json` resolve Ghidra pointer slots (subtract 0x100000
for ELF addresses): turns 0501d300/310/2f0/2e8/308 = chun/zhao/cai/jin/bao;
holds 0501d1d8/1e0/1e8/1f0/1f8 in the same order; 0501d2f8 = glow;
0501d1a8 = zcjb_idle. This establishes the table independently of file naming.

Deform frame values in the verified extraction are absolute mesh positions for
unweighted attachments and additive offsets for weighted attachments. The author
converts the source between-frame percent curve using the original nine Bezier
samples. The runtime interpolates into reusable per-instance buffers. Inactive
attachments and clip changes return to setup geometry, preventing turn deformation
from leaking into hold animations or another instance. The native Animation
component remains the animation clock.

`Tools/prepare_bonus_card.py` prepares offline authoring data and independently
samples all 13 source clips at 370 times, including either side of attachment and
deform boundaries. `RecoveredJackpotPopupArtTests` compares every active vertex to
these values and checks warmed pose sampling allocations. `RecoveredBonusCardArtTests`
renders the five faces, turn midpoint and turn end into the fresh
`Artifacts/current-bonus-card-faces.png`; it also exercises threefold speed,
global pause, completion-to-idle and restoration to normal speed.

Full Unity PlayMode suite: `Artifacts/bonus-card-tests.xml`, **232/232 passed**.
All source geometry cases passed at 0.00008 world-unit tolerance. Warmed ef_jinbi
pose sampling measured roughly 5–6 ms per 1000 poses and 0 managed allocated bytes;
this excludes Canvas rebuild and GPU rendering. The contact sheet is an isolated
current-artwork check, not a screenshot of the still-incomplete Bonus window.
After enlarging only the verification canvas to preserve glow margins,
`Artifacts/bonus-card-capture-tests.xml` passed **1/1**. The resulting 1280 x 800
PNG was inspected at original resolution.

## Native business timing observed so far

`BonusItem.PlayBonusAnim` MoveNext `2396d54`:

- Advances task (2,1) immediately if the accepted card completes a jackpot.
- Requests sound, sets body SkeletonGraphic timeScale to 3, starts the type's
  one-shot turn clip. Completion callbacks `2395dbc` through `2395f7c` start the
  matching looping hold clip and set isTurn=true.
- For Reward, an independent scaled 0.2 second wait precedes showing RewardTxt
  and calling ConfigManager.GetReward. A scale tween then runs independently.
- Waits until isTurn, sets body timeScale back to 1, activates the independent
  glow one-shot. Glow completion `239602c` hides its GameObject.
- A non-jackpot-completing character with a destination invokes the input-release
  callback **before** starting the pooled flight. A completing character defers it.
- A character with no destination invokes the callback without that flight.

Remaining required work: author the complete Bonus item/window hierarchy with
native Buttons, bind these clips to the actual item sequence, finish the reward
text/pooled-flight/jackpot claim/close paths and integrate the window source with
the main Bonus transition. The artwork prefab alone does not complete this branch.
