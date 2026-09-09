# Reward-window shared font and material payload audit

Baseline 8d1f7f6. Tools/audit_reward_font_assets.py reads the current reverse
export and active Unity resources, compares complete payloads, writes input
hashes/remaps to reward-font-assets-audit.json, and fails on differences.

Eight checks pass:

- QuorumStd-Black_zitidi.com OTF bytes match the current exported original.
- Its Atlas PNG bytes match the current exported original.
- The entire SDF font serialization matches after two explicit mappings:
  unavailable exported TMP_FontAsset script to installed official 3.0.9 script;
  editor m_SourceFontFileGUID to the actual imported OTF GUID. Both source and
  current m_SourceFontFile object references already use that same OTF GUID.
  All face metrics, glyph/character tables, kerning/features, atlas settings,
  population mode and remaining fields compare without filtering.
- Atlas Material, shared #000000_3, and the BonusRewardPopup/JackpotPopup copies
  of #0A5902_4 compare in full after the exported shader GUID is mapped to the
  actual TextMeshPro/Distance Field shader. Texture references, keywords,
  outline/face colors, widths, stencil, rendering flags and saved properties
  are preserved by this full-text comparison.
- That active TMP_SDF.shader is byte-identical to its asset entry inside the
  installed official TextMeshPro 3.0.9 TMP Essential Resources.unitypackage.
  No package extraction or runtime plugin was installed.

The companion typography audit now also compares m_fontAsset,
m_sharedMaterial and m_fontColorGradientPreset on all resolved text nodes.
It validates the known #0A5902_4 resource GUID remap using actual material
metadata (Treasure references the BonusRewardPopup copy). Its result expands
from 765 to 810 equal fields, zero differences. Thus the resources above are
connected to the inspected window text, rather than merely existing on disk.

Limits: identical PNG bytes do not prove identical GPU texture import settings
or sampled pixels. The original compiled font shader has not been compared
instruction-for-instruction with Unity's official shader; matching material
properties is not that proof. Runtime dynamic glyph additions, fallback fonts,
inline material-tag resources, legacy green-number font and every language
are outside this audit. The editor source-font GUID correction is explicit,
not silently classified as an identical source field.

Both local Python audits exited zero. No runtime, prefab, package or SDK was
changed, so no Unity regression was rerun this round. Complete visual parity
remains unproven.
