# Compiled symbol shader evidence

The previous export files under `reference-unity/.../Spine-SkeletonGraphic.shader`
are dummy shaders. This directory instead contains GLES programs recovered from the
serialized Shader object's compressed program blob. Run:

```
python -B Tools/extract_symbol_shader.py "C:/Projects/Nut Sort Relax"
```

The script reads the original object tree, decompresses the GLES blob and preserves
both complete source programs. `provenance.json` records the source and blob hashes
and the original blend/depth/stencil state. Analysis dependencies remain outside the
Unity project; none are added to the game runtime.

The normal fragment program samples the atlas, adds `_TextureSampleAdd`, multiplies
by the interpolated vertex color and rectangle coverage, then outputs it. It does not
multiply texture RGB by texture alpha. Blend factors are One / OneMinusSrcAlpha.
The second variant additionally discards output alpha below .001. The original
SkeletonGraphicDefault material has no valid keywords and `_UseUIAlphaClip=0`.
The shader source therefore does not justify globally switching this atlas to straight
alpha or clearing transparent-pixel RGB to remove the observed Gui purple region.
A current original-runtime comparison is still needed to establish its effective
mask/material state and whether the same artifact is visible there. The reconstruction
keeps the source texture unchanged pending that comparison.
