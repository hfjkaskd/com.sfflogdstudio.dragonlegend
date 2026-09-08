# Free mini reel effect clipping

The recovered UIMainView prefab's first bottom mini reel is component
114232415838589296 on GameObject 1743393880019028. Its RectMask2D component
114360591974699550 has zero padding and zero softness. The authored rectangle
is 188 x 172 pixels; all fifteen rectangles and their serialized order are
recorded in free-reel-layout.json. Effects are children of the moving slots,
while the mask remains on the stationary mini reel.

Previously the converted SpriteMask clipped ordinary sprites only. Native
MeshRenderer coin/ball art and the separate world-space Canvas reward text
could leak into neighbouring reels. Each FreeMiniReel now serializes its mask
reference and assigns it to its seven instantiated slots. Pool placement binds
the effect to that slot's mask, including when reusing an effect in another reel.

RecoveredWorldRectClip is authored on FreeCoin and FreeBall. Its serialized
renderer/text references cover the main art, coin glow and reward label. Mesh
fragments use the mask sprite's rectangle in the mask's local coordinate frame,
transformed from world space. Existing texture property blocks and premultiplied
alpha blending are preserved. Reward labels retain their original font/material
and use Unity CanvasRenderer.EnableRectClipping in their authored canvas frame.
The existing common parent orientation is preserved; no independently rotated
label Canvas is introduced. Both paths retain hard, zero-softness boundaries.

No runtime material cloning, mesh reconstruction for external clipping, hierarchy
searches or per-frame managed buffer allocation is added. Each pooled effect
reuses one property block. The matrix/rectangle updates follow current transforms
in LateUpdate and before Canvas rendering. Standalone unbound effect previews
retain their original unclipped presentation.

The moving-board camera test freezes actual art at .173 seconds, keeps all
fifteen sibling SpriteMasks enabled and disables ordinary sprites to isolate
specials. It checks initial visibility, half-cell clipping at 86 pixels, and
zero visible pixels after moving the slots 344 pixels outside their own masks.
It separately renders only the original coin reward labels, then checks complete
clipping after board translation, nonuniform scaling and rotation. Pool reuse
assertions verify rebinding to the destination reel's boundary.

Fresh captures are under Artifacts/current-free-clipping-*.png (1200 x 1000).
The half-cell and labels-only captures were inspected at original resolution.
This verifies the standalone Free board's effect clipping, not production Free
mode completion. Independent stop motion is now connected (free-reel-motion.md);
column coordination is connected (free-columns.md); auto-spin/reward/exit, mode UI and production FreeEntry
binding remain unfinished. SDK handling is unchanged.

Validation: final Unity 2022.3.62f3 full PlayMode suite **281/281 passed** in
Artifacts/free-clipping-final-tests.xml; focused special-pool/camera checks
**4/4 passed** in Artifacts/free-clipping-focused.xml.
