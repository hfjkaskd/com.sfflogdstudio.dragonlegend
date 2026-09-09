# Main Base/Free background restoration

Production GameEntry previously instantiated the recovered foreground/playfield
without the source Main BaseBg and FreeBg images. Both source backgrounds are
direct children of UIMainView. BuildMainBackground now copies their complete
Image/RectTransform blocks into a prefab and binds that prefab in GameEntry.
At launch it instantiates behind the other Main children and applies the current
GameSlotType; GM profile rebuild destroys the old instance and creates one new
instance. Default US normal mode uses the Base background.

The background owns a nested native Canvas with overrideSorting at Main's order
minus three, on Main's sorting layer and camera. Source sibling ordering placed it
behind the original UI reels. Since recovered gameplay uses native world
Sprite/Mesh objects, sibling order on a shared Canvas cannot preserve that
relationship: the first integration obscured the reels and failed the existing
coin-effect rendered-pixel test. The separate background draw order preserves
the source composition without changing image geometry or gameplay scale. The
later board-composition recovery reserves -2 for the dragon body and -1 for the
source board background; see main-board-composition.md.

Source UIMainView fields and original GUIDs:

| Field | Source Rect ID | Sprite | Original GUID |
| --- | --- | --- | --- |
| BaseBg +1a8 | 224271925019662038 | Res/UI/zhujiemian/zjm_bg | daecfff7d58f2de49aeabc06ece194cd |
| FreeBg +1a0 | 224320592835458227 | Res/UI/free_game/mfyx_bg | d4c44dc20add14f49a08a4c3e2aca6cd |

Both rects are centered at (0,0), with center anchors/pivot, scale one and size
1080.4688 x 2400. Both Images are white, Simple, preserveAspect false. The image
GUIDs resolve to the current recovered original textures. No new image generation,
old screenshot, inferred crop or arbitrary resizing is used.

ARM SetInitShow 23bca34 checks GameSlotType for zero. Base shows BaseBg and hides
FreeBg; any nonzero follows the Free path and reverses the pair. The new Apply
method preserves this exact branch. It is explicit, not a per-frame mode poll.

The current-scene test checks the production binding, both source layouts/sprites,
mutual visibility, the real Spin button raycast above the backgrounds, and single
instance restoration after GM profile switching. It captures both current
background selections. The Free selection capture only tests the background;
it is not evidence of a complete Free foreground/board mode transition.

Validation: `Artifacts/main-background-final-tests.xml` reports 348/348 PlayMode
tests passing, including the pre-existing coin-effect rendered-pixel test. Current
Base/Free background captures were inspected after this run: reel symbols render
in front of both backgrounds. The runtime Bind enables overrideSorting after
parenting, since the standalone prefab serializes that flag off. Spin hit testing
and GM rebuild pass. The dragon still overlaps the jackpot and board in these
captures; overall foreground composition remains unverified and needs correction
against source transforms/render ordering rather than arbitrary dragon resizing.
The subsequent main-board-composition.md change restores that missing board layer
and body order; its newer captures supersede this initial composition observation.

## Remaining SetInitShow and lifecycle work

The same native function selects the mode's symbol definitions and switches
Yanhua, Main/Free bottom sections, MainRoll/FreeRoll and Result/FreeResult. It sets
DownWinText to GOOD LUCK in Free mode and in Base mode with zero tempDownWinCount,
otherwise formats tempDownWinCount to two decimals. It kills CashOutTipSeq, hides
the CashOutTip text's parent, and calls ShowCashOutTip only on the Base path.
Those consumers still need complete production binding; changing the background
alone is not a recovered SetInitShow implementation.

Native scene mapping for that next work:

- Yanhua 224521597976012101 = Node/SkeletonGraphic (ef_slyanhua), anchored (0,83),
  size (50,50), references Res/Spine/yanhua/ef_slyanhua_SkeletonData.
- Main +a8 is Bottom/Main; Free +b0 is Bottom/Free (Rect 224396801632836831),
  center anchors, position zero, size (100,100). Free/Bg/Text (TMP) is the
  FreeSpinCountTxt component 114712810534613357.
- MainRoll is Roll GameObject 1833703512093738; FreeRoll GameObject
  1689364072382846. FreeResult Rect 224056435115414594, Result Rect
  224031355914570132 are separately switched source containers.
- InitFreeSpinTimes 23bf060 formats the current FreeSpinCount. ELF relocation
  04f1e3d0 resolves to `FREE SPIN <material="#1A1457_3"><gradient="free">{0}</gradient></material> TIMES`.
  Relocation 04f1e260 resolves to the literal `GOOD LUCK` used by SetInitShow.

The raw SetInitShow ARM ends at 23bce88. Its decompiler expands the ShowCashOutTip
tail call into unrelated-looking additional loops; do not implement those loops
as a Free-mode side effect. The background recovery does not alter SDK handling.
