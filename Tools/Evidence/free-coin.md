# Free coin initial and appearance presentation

The current original `Res/Prefabs/Jinbi.prefab` explicitly starts the main
skeleton at idle_chun, enables the separate glow skeleton with no starting
animation, and enables a legacy Text containing 96.3. Root scale is .7. The
label uses font GUID 36977c4faccb97c4ebe0b4fdeea88b25 (the already recovered
Green bitmap font), font size 0, center alignment, anchored position (0,4.6),
zero size, horizontal overflow and vertical truncate. Initial display must
not be substituted with the zcjb_idle loop and an automatically hidden label.

`FreeCoin.prefab` authors this native world geometry and a world-space Canvas
only for the reward label. It reuses the original font configuration from
CoinRewardText but removes that separate Base reward behavior. The main and
glow each use one native MeshRenderer and Animation. The glow's `__setup`
native clip is an empty-timeline representation of the original skeleton's
setup pose, not an invented source animation. Both sides of the original
source JSON remain unchanged; the authoring script adds this pose only to its
intermediate data and independent samples. FreeCoinArt's default is corrected
to the actual prefab's idle_chun.

RecoveredFreeCoin.PlayShow implements JinBiEffectItem.PlayShowAnim 0x23d95e0:
hide the glow and reward Text without clearing text, play zcjb_chuxian once,
and start a .2-second OutQuad scale from the current scale to 1. Its scale
completion starts a .2-second OutQuad return to .7. The independent animation
completion at .5 seconds switches to zcjb_idle looping (0x23d997c). PoolManager
CreateJinBi 0x238b040 calls this only when isInit is false, so the initial
prefab state and rolling-appearance state deliberately differ.

Tests assert the authored initial state and font, pause handling, scale peak
and return, animation transition, and hidden-but-unchanged text. The fresh
`Artifacts/current-free-coin-initial-appearance.png` shows the initial state
on the left and the appearance clip at .173 seconds on the right. The root
is held at its authored .7 scale for this side-by-side image.

The shared Free coin pool and CheckFakeCoin consumer are now attached to
FreeReels (see free-specials-pool.md). Per-mini-reel clipping is connected
(see free-specials-clipping.md). Pending: rewards and collection flight, and production Free flow
binding. This prefab alone does not establish complete pool lifecycle or
Free gameplay fidelity. SDK handling is unchanged.

Validation: full Unity 2022.3.62f3 PlayMode suite **270/270 passed** in
`Artifacts/free-coin-full-tests.xml`, including all 944 geometry samples
covering the source clips and setup pose. The fresh 1200x600 initial/appearance
capture was inspected at original resolution; the initial source label 96.3
is visibly preserved and is not being presented as a calculated reward.
