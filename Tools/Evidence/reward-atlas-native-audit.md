# Reward font atlas: original APK and actual imported texture

Baseline 36ddd9d. The source is the current APK extraction's Texture2D object,
not the exported PNG TextureImporter defaults. Tools/audit_reward_atlas_native.py
resolves both named atlas objects through assets/inventory.json, records their
complete metadata and hashes, and decodes their inline image data.

Both original copies (sharedassets0 path 25 and font bundle path
-2850944667414898811) have identical 1048576-byte payloads:
SHA256 2a4211a1f2d98d05cf095cdaddcac7a1ea1abb78fe40bb0ff887634585b89464.
Metadata says 1024x1024, TextureFormat 1/Alpha8, one mip, readable, no streaming,
bilinear filtering, anisotropy 1, mip bias zero, and Repeat U/V/W.

RecoveredRewardFontTextureTests loads the actual current Resources texture in
Unity and checks those settings plus the SHA256 of GetRawTextureData<byte>().
PID 15760 exited: Artifacts/reward-native-atlas-tests.xml passed 1/1 in
0.0623146 seconds. Its actual imported alpha bytes match the source, with no
resizing, compression or pixel conversion discrepancy in this run.

The exported PNG .meta defaults differ: sRGBTexture 1, maximum 2048 and generic
compression settings. Current .meta uses linear Alpha8, maximum 1024 and no
compression. Copying the export defaults would not establish fidelity to the
actual APK Alpha8 payload. Native metadata's m_ColorSpace=1 is retained in the
report rather than equated to the current importer flag. The active official
TMP_SDF shader samples _MainTex alpha at lines 240, 284 and 289; this comparison
does not claim that color-space tags themselves are identical.

This is imported CPU texture/parameter evidence. GPU-rendered antialiasing,
Android backend texture format, original compiled shader equivalence and
runtime dynamic atlas modification remain separate checks. No texture, shader,
runtime, SDK or prefab was changed, and no full suite or Android build was
performed. The Python audit and Unity test provide a reproducible source oracle.
