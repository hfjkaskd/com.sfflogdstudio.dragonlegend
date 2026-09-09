# Free start caption and fresh render

Runtime baseline 00267f7780cf44cb0ea4575b72337d98887f9a2b.
Inspected ReferenceOriginal/Res/ViewPrefabs/UIFreeSpinStart.prefab directly.
Caption TMP record 114918264173551851 belongs to GameObject 1099118695303065;
its RectTransform is 224946936116752871.

The source deliberately stores "Watch an ad to get more free spins." in a
200x50 rect at (0,-394), centered anchors/pivot and unit scale. Its font size
is 58, Bold, centered horizontally and vertically, with zero margins,
auto-sizing disabled, wrapping disabled and Overflow mode. Its displayed
width therefore exceeds the small rect; that alone is not a recovered-layout
bug and does not justify shrinking the font or enabling wrapping.

Current FreeStartPopup retains source font GUID
31628181d58311344bb283c127fc9aba and material GUID
a178f3ea856c4ff4194cfea352ce7c98, resolving under RecoveredText/Res/Font to
QuorumStd-Black_zitidi.com SDF and its Atlas Material. This verifies references,
not a new font-atlas/shader byte comparison.

RecoveredFreeStartPopupTests now checks the caption's actual runtime text,
font size/style, auto-size/wrapping/overflow, alignment, margins, rect size,
position and scale, alongside its existing digit font, ad outcomes and hide
callback checks. Unity process 51140 exited: 1/1 passed in 0.5064349 seconds
(Artifacts/free-start-caption-tests.xml).

The run freshly generated Artifacts/current-free-start-popup-initial.png;
it was visually inspected at 1080x1920. The isolated popup has its digit 8,
dragon art, wide caption, FREE+4 Button, START and finger. These count values
are the existing fixture inputs. The initial inspection also used the current
production-scene popup-depth capture; the isolated image removes the stacked
MoreSpin/MoreWild windows from that fixture's background.

No production asset, layout, font or SDK changes were made. This comparison
establishes the listed source parameters and present rendering, not fresh
original-APK pixel parity, all resolutions or every animation frame.
