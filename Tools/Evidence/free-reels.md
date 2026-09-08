# Native Free reel layout and initialization

`extract_free_reel_layout.py` reads the current recovered UIMainView prefab by
file ID. It follows `FreeRells`, then each `FreeRoll.RollReels` list, rather than
sorting GameObject names. `free-reel-layout.json` preserves source IDs, names,
positions and rectangles. The native five-column order is Roll1 through Roll5;
each column lists Mask3, Mask2, Mask1, which correspond to bottom, middle, top.
Columns are at x = -380, -190, 0, 190, 380 and y = -1 pixels. Each independent
mask is 188x172 pixels, at local y = -172, 0, 172, with zero padding/softness.
The outer FreeRoll rectangle is centered at (-1.62,-71), size 948x515, under
QiPan; that UI parent placement still belongs to the pending Free view switch.

Native `RollReel..ctor` 0x23775c4 stores seven slots, 172-pixel item height and
initial fake-effect interval 2. `RollReel.Init` 0x23747c4 positions slots at
anchored y = 172*i. The shared SymbolItem prefab anchors to its parent's bottom
with a bottom pivot; its centered child image lies 86 pixels above that pivot.
Therefore the recovered world-space mini reel starts each slot at -86+172*i,
with the first visible image centered at zero. Reusing the Base reel's -258
offset would put the wrong slot at the visible center.

`FreeMiniReel.prefab` uses the shared native SpriteRenderer symbol prefab,
seven slots, four-slot wrapping, and one native SpriteMask in its own
SortingGroup. Group isolation is required: a sibling mask must not reveal
another mini reel's upper hidden symbols. It carries no Base reel motion
component. `FreeReels.prefab` saves all fifteen native mini reel instances in
the extracted order; static geometry and settings are authored, not generated
by runtime layout code.

`RecoveredFreeReels.Initialize` implements the once-only guard from
`UIMainView.InitFreeReels` 0x23bd420 and ordered initialization from
`FreeRoll.Init` 0x23b8440. After each reel's seven random symbol draws, it reads
the actual initial Free result. For IDs 9 and 11 the initial effect callback
runs immediately, before the next reel initializes. Other IDs follow the
ordinary branch of `CheckFakeCoin(true)`: replace slot zero when that ID exists
in the Free catalog, leaving the other six slots unchanged. The consumer must
provide the special-effect callback; the component does not silently omit it.
The result must have finished generation before initialization starts.

Raw ARM64 `023757dc.asm`, 0x2375bbc..0x2375c20, proves the normal branch stores
the matching definition in slot zero and tail-calls `SetImg` with w3=1, w4=0.
Thus the sharp Free sprite is shown with its cover active. It must not retain
the initial random sprite or use the uncovered Base presentation. This branch
does not draw any additional random numbers.

Tests check the authored positions and row order, first-image alignment,
all 105 initial symbol draws, actual ordinary results and their cover state,
interleaved special-effect callback RNG, and the
initialization guard. The camera test leaves sibling masks active while only
one bottom reel's symbols are visible, proving hidden slots do not leak into
neighboring cells. It then renders all fifteen cells into a fresh
`Artifacts/current-free-reels.png` preview of the current prefab.

This is a reel-layout preview, not a full Free gameplay screenshot. Initial
coin/ball effect creation, independent Free stop motion, mode UI/background
switching, auto-spin/reward/exit and production FreeEntry binding remain open.
The SDK path has not changed.

Validation: Unity 2022.3.62f3 full PlayMode suite **261/261 passed** in
`Artifacts/free-reels-full-tests.xml`. The fresh 1080x720 Free reel prefab
capture was inspected at original resolution; all fifteen cells are visible.
That initial layout-only run predates the actual-result initialization fix;
its preview has been superseded by the current initialization test capture.

Actual-result initialization validation: **261/261 passed** in
`Artifacts/free-initial-result-tests.xml`; the updated 1080x720 preview was
inspected at original resolution. Ordinary symbols now show the native dark
cover required by SetImg's true argument. The mixed-result test uses five coins,
five balls and five ordinary cells, and verifies that only the ten special
cells call the effect consumer while all fifteen reels consume their seven
initial random symbols in the original order.
