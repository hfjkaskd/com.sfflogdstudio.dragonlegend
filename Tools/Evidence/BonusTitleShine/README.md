# Native title sweep conversion

The original exported uishiny.shader contains DummyShaderTextExporter output
and cannot establish the effect. gles-0/1.glsl are the actual platform-9 programs
decompressed from the original serialized Shader object; provenance.json records
its SHA256 and render state. Both variants use SrcAlpha/OneMinusSrcAlpha.

Native instruction evidence is in native.txt (UIShiny.ModifyMesh, parameter writes,
EffectPlayer.OnEnable/OnWillRenderCanvases) and packer.txt (443c67c). The current
Title uses RectTransform effect area 0. Its axis is normalized
(cos(rotation)*height/width, sin(rotation)); the second transformed coordinate
is packed with the texture row index. UVs and effect coordinates clamp to [0,1],
floor at 4095 and combine into two 12-bit fields. ParameterTexture writes clamp,
multiply by 255 and truncate to byte for factor/width/softness/brightness/gloss.

The recovered shader retains the compiled equations: center=factor*2-.5;
band=saturate((1-min(abs((coordinate-center)/width),1))/softness);
band=band*band*(3-2*band)*.5. Add to RGB
band*clippedAlpha*brightness*(1+gloss*(RGB*7-1)), preserving the original alpha,
UI clipping, optional alpha test and blend state.

RecoveredTitleShine uses official BaseMeshEffect, Material and a 2x1 RGBA32
parameter texture. This title needs one parameter row, so the recovered component
uses one row rather than the third-party shared registry. The packed coordinate
and sampled byte values are preserved. Parameters are authored in the prefab:
factor .5, width .25, rotation 135, softness/brightness/gloss 1, play/loop true,
duration 2, initial/loop delays 0. On Canvas.willRenderCanvases, scaled time is
advanced; reaching duration emits that frame's final ratio and resets the next
cycle to minus loop delay. On enable the clock resets to minus initial delay.

The runtime reuses its pixel array and updates two texels; it does not rebuild
the title's mesh each frame. Material/texture allocation occurs on enable and
is released on disable. No plugin assembly or Editor-only runtime path is used.

The current popup test pauses time to freeze the background, captures factor 0
and .5, and checks that the latter actually brightens the rendered image. This
checks that the shader runs; full board compositing and device precision still
require the overall lifecycle comparison.

Verified full PlayMode run: `Artifacts/bonus-shine-verified-tests.xml`, 241 passed,
zero failed. Both fresh 1080x1920 captures `current-bonus-reward-shine-0.png` and
`current-bonus-reward-shine-1.png` were inspected; the second shows the brighter
central title band with the remaining frozen image unchanged. The initial pause
assertion was moved past the timeScale change frame because its cached deltaTime
can still be consumed by Canvas.willRenderCanvases in that frame.
