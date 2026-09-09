# Current reward-window typography and local text rectangles

Baseline b3188f5. Run with local Python:
`python -B -X utf8 Tools/audit_reward_typography.py`.

Compared UIRewardView -> BonusRewardPopup, UIJackpotView -> JackpotPopup and
UITreasureView -> TreasureWindow. The three ReferenceOriginal input prefabs
are byte-for-byte verified against the current reverse project's
reconstruction/mumu-current/reference-unity/ExportedProject/Assets/Res/ViewPrefabs
copies before the comparison. No screenshot supplies the expected values.

The script maps text components by authored hierarchy path, ignoring root name
and Unity file-ID changes. It compares 765 serialized fields with zero
differences: explicit TMP typography, color, alignment, spacing, wrapping,
overflow and margin fields; complete legacy FontData blocks; and each text
object's local rotation/position/scale, anchors, anchored position, size and
pivot. The exact field allowlist is visible in the script. Report records
every successful field and hashes all consumed prefab files.

An initial direct-record scan reported seven missing Treasure text paths.
Those paths belong to the actual TreasureCard and CollectTip prefab instances.
The script now verifies each nested source GUID has exactly one instance in
TreasureWindow and reads those authored child records. It rejects overrides
targeting the compared text component or its RectTransform rather than
silently ignoring them. With those references resolved, all seven paths match.
No missing-object or font-size runtime fix was indicated by this evidence.

Limits: this is a declared field comparison, not full prefab equivalence.
Ancestor transforms (including nested-root overrides), dynamic text, font
atlas/glyphs, TMP shared materials, textures, shaders, sorting, masks, whole
animation curves and actual screen pixels are not proved by these checks.
Comparison of local text rectangles alone does not prove final world bounds.
The script exits nonzero on differences, missing paths or unresolved text
overrides. No Unity runtime, SDK, prefab or scene file changed; no Unity suite
rerun was necessary for these read-only serialized comparisons.
