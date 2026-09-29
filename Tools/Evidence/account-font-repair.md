# Account input font repair — 2026-09-16

The AccountWindow name and email fields used a dynamic TextMeshPro font whose atlas was an imported, compressed ETC2_RGBA8 PNG. Its character table omitted 20 printable ASCII characters, including lowercase j. Adding missing glyphs at runtime produced invalid atlas pixels and modified existing d pixels. Neither input filters nor clipping caused this issue.

The shared Microsoft YaHei Bold font was rebuilt from the bundled msyhbd.ttf into a native, writable 1024x1024 Alpha8 atlas. All original 78 characters were retained; all printable ASCII is now pre-baked (98 total stored characters). Font and material GUIDs are unchanged. Dynamic multi-atlas support remains enabled. CashOutItem authoring now preserves existing repaired assets instead of overwriting them with the recovered originals.

## Validation

Unity 2022.3.62f3, Android active target, actual AccountWindow prefab rendered in an isolated preview scene at 1080x1920 and 1080x2400. No SDK, account submission, or player-data writes.

- Before: reproduced missing/sliver j and malformed d; adding text changed d pixels.
- After: all 10 render cases passed; j/d, alphabet, digits, money and email symbols render correctly.
- All printable ASCII was present before input.
- Adding é dynamically succeeded and left j/d pixels unchanged.
- Every tested non-space character had a visible glyph and the correct material atlas.
- Font/material GUIDs, bundled source references and atlas bounds verified.
- Editor authoring/validation code compiled successfully.

Reports: account-font-before.json and account-font-after.json in this directory.
Renders: Artifacts/AccountFont/ before-* and after-* PNG files (local generated evidence).
Validation source: Tools/Validation/ValidateAccountFont.cs. To rerun, temporarily copy it into Assets/Whitebox/Editor and use Tools > Validation > Account Font > After, then remove the temporary copy. The permanent repair command is Tools > Repair > Cash Out Font.

No APK was built or installed. Export/rebuild the Android package to include the repaired asset; the already-installed APK is unchanged.