# Main scene environment, lighting and cameras

Baseline: 50fb719688c59b41c7ebf1b406fc0e598d471c2a. The audit compares current
ReferenceOriginal/Scenes/Main.unity and its LightingData with current
Assets/Whitebox/Scenes/GameEntry.unity and Whitebox/Rendering/LightingData.asset.
No historical screenshots are used.

Tools/audit_main_scene.py passed 12 complete serialized-record comparisons:
RenderSettings, LightmapSettings, LightingData, both camera GameObjects,
both camera Transforms, both Camera components, both UniversalAdditionalCameraData
components, and the UI camera ancestor's pose excluding its differing child list.
The output main-scene-audit.json stores source/current file hashes and normalized
record hashes. Whitespace normalization, known local fileID remapping and the
explicitly verified GUID replacements are the only transformations applied.

Environment: fog off, no skybox or sun, ambient mode 3/intensity 1, and the source
sky/equator/ground colors preserved. Realtime and baked lightmaps are both disabled.
The lightmap settings are otherwise identical after resolving LightingData GUIDs.
LightingData differs only in its verified scene back-reference: no lightmaps,
AO textures, light probes, baked reflection cubemaps or lightmapped renderers;
all 27 ambient coefficients are zero and the Enlighten payload bytes match.
There is no source baked lighting result to recreate by running a new bake.

World camera retains perspective FOV 60, near/far .3/1000, depth 0, all-layer
culling; UI camera retains orthographic size 5, depth 1 and layer mask 32.
Their transforms, enabled state, HDR/MSAA flags, clearing and viewport fields
match. The URP data makes UI the Base camera with world camera in its stack;
world is Overlay. Postprocessing, antialiasing and dithering are disabled in
both source camera records and current records.

The exported URP script GUID b15cdfbb15089e87868c39e7284c9c96 is identified as
UniversalAdditionalCameraData by the reverse delivery original-guid-map.json.
The current a79441f348de89743a2939f4d699eac1 GUID is verified against the installed
official com.unity.render-pipelines.universal@14.0.12 source .meta. All remaining
serialized camera-data fields compare without filtering.

## Explicit remaining hierarchy difference

Original Transform 12 ([UI]Main), at (100,0,-10), has UICanvas/23, UICamera/13
and EventSystem/11 as children. Current corresponding Transform 4000000012 has
only UICamera/4000000013; the current Canvas and EventSystem live in separate
roots. The script records both child lists rather than silently treating them
as equal. Its ancestor pose comparison excludes only that child-list block.
This turn does not establish that the whole scene hierarchy matches or that
this structural difference is behaviorally irrelevant in every lifecycle state.

No scene, game logic, SDK or rendering asset was changed. No Unity run was
necessary for these serialized-record comparisons. Runtime camera changes,
project-wide quality/pipeline settings, material/shader behavior and full visual
equivalence need separate evidence; passing these 12 records does not prove them.

Follow-up: main-ui-hierarchy.md restores the Canvas/EventSystem parent and order
in the actual scene. The audit now compares 14 records, including the full UI
ancestor child list and EventSystem hierarchy. Unity save ordering of the root
serializedVersion property and numeric negative zero are normalized without
dropping their values. The hierarchy-difference section above describes the
pre-fix baseline; the current report no longer excludes the child list.
