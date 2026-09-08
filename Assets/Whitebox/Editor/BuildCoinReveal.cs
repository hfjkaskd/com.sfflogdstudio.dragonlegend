using System;
using System.IO;
using DragonLegend.Whitebox;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;
using Data = BuildCoinAppearance.Data;
using Clip = BuildCoinAppearance.Clip;

// Original unweighted coin_c deform frames become native blend shapes. No runtime JSON parser.
public static class BuildCoinReveal
{
    [Serializable] private class MeshData { public MeshAttachment[] attachments; }
    [Serializable] private class MeshAttachment {
        public int slot, kind; public string key; public bool weighted;
        public float[] vertices, uvs; public int[] triangles;
    }
    private static readonly string[] Channels = { "r", "g", "b", "a" };

    public static void Save()
    {
        Build(false);
    }
    public static void SaveGlowAndConnect()
    {
        Build(true);
        BuildCoinStopEffect.Save();
    }
    private static void Build(bool glow)
    {
        string Folder = "Assets/Resources/RecoveredSymbols/" + (glow ? "CoinGlow" : "CoinReveal");
        Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
        string json = File.ReadAllText("Assets/Whitebox/Editor/RecoveredCoinEffect.json");
        var data = JsonUtility.FromJson<Data>(json);
        var meshData = JsonUtility.FromJson<MeshData>(json);
        var reveal = Array.Find(data.animations, c => c.name == (glow ? "glow" : "zcjb_b_chun"));
        var idle = Array.Find(data.animations, c => c.name == "idle_chun");
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/RecoveredArt/Res/Spine/棋子/jinbi/ef_jinbi.png");
        var normal = Material(Folder, "Normal", texture, 10); var additive = Material(Folder, "Additive", texture, 1);
        var root = Node(glow ? "CoinGlow" : "CoinReveal", null); root.AddComponent<SortingGroup>();
        int[] visible = glow ? new[] { 27, 28, 29, 30, 42, 43, 44, 45 } : new[] { 1, 23, 24, 25, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41 };
        try {
            var bones = new Transform[data.bones.Length];
            for (int i = 0; i < bones.Length; i++) {
                var b = data.bones[i]; var v = b.values;
                bones[i] = Node(b.name, b.parent < 0 ? root.transform : bones[b.parent]).transform;
                bones[i].localPosition = new Vector3(v[1], v[2], 0) * .01f;
                bones[i].localEulerAngles = new Vector3(0, 0, v[0]);
                bones[i].localScale = new Vector3(v[3], v[4], 1);
            }
            var renderers = new Renderer[data.slots.Length];
            foreach (var a in data.attachments) {
                if (Array.IndexOf(visible, a.slot) < 0) continue;
                if (renderers[a.slot] != null) throw new InvalidOperationException("Multiple attachments need switching.");
                var slot = data.slots[a.slot]; var region = Array.Find(data.regions, r => r.name == a.name);
                var node = Node("Slot" + a.slot, bones[slot.bone]);
                if (a.slot == 1 || a.slot == 45) {
                    var m = Array.Find(meshData.attachments, x => x.slot == a.slot);
                    if (m.kind != 2 || m.weighted || region.rotate != 0)
                        throw new InvalidOperationException("Unsupported coin mesh format.");
                    var deform = Array.Find(reveal.timelines, t => t.domain == "deform" && t.index == a.slot);
                    var mesh = new Mesh { name = a.name };
                    var vertices = new Vector3[m.vertices.Length / 2]; var uv = new Vector2[vertices.Length];
                    var colors = new Color[vertices.Length];
                    for (int i = 0; i < vertices.Length; i++) {
                        vertices[i] = new Vector3(m.vertices[i * 2], m.vertices[i * 2 + 1], 0) * .01f;
                        // Mesh UVs cover the original untrimmed region, unlike a Sprite rectangle.
                        uv[i] = new Vector2((region.bounds[0] - region.offsets[0] + m.uvs[i * 2] * region.offsets[2]) / texture.width,
                            1 - (region.bounds[1] - (region.offsets[3] - region.offsets[1] - region.bounds[3]) + m.uvs[i * 2 + 1] * region.offsets[3]) / texture.height);
                        colors[i] = Color.white;
                    }
                    mesh.vertices = vertices; mesh.uv = uv; mesh.colors = colors; mesh.triangles = m.triangles;
                    for (int f = 0; f < deform.frames.Length; f++) {
                        if (f + 1 < deform.frames.Length && deform.frames[f].curve != 0)
                            throw new InvalidOperationException("Coin deform interpolation changed.");
                        var delta = new Vector3[vertices.Length]; var values = deform.frames[f].values;
                        for (int i = 0; i < delta.Length; i++)
                            delta[i] = new Vector3(values[i * 2], values[i * 2 + 1], 0) * .01f - vertices[i];
                        mesh.AddBlendShapeFrame("Frame" + f, 100, delta, null, null);
                    }
                    mesh.RecalculateBounds(); mesh = SaveAsset(mesh, Folder + "/" + a.name + ".asset");
                    var renderer = node.AddComponent<SkinnedMeshRenderer>(); renderer.sharedMesh = mesh;
                    renderer.sharedMaterial = slot.blend == 1 ? additive : normal; renderer.updateWhenOffscreen = true;
                    if (glow) {
                        var tint = new SerializedObject(node.AddComponent<RecoveredMeshTint>());
                        tint.FindProperty("target").objectReferenceValue = renderer;
                        tint.FindProperty("color").colorValue = ColorOf(slot.color) * ColorOf(a.color);
                        tint.ApplyModifiedPropertiesWithoutUndo();
                    }
                    renderers[a.slot] = renderer;
                } else {
                    var b = region.bounds; var o = region.offsets; var v = a.values; bool rotated = region.rotate == 90;
                    int width = rotated ? b[3] : b[2], height = rotated ? b[2] : b[3];
                    var sprite = Sprite.Create(texture, new Rect(b[0], texture.height - b[1] - height, width, height),
                        new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect);
                    sprite.name = region.name; sprite = SaveAsset(sprite, Folder + "/Slot" + a.slot + ".asset");
                    node.transform.localPosition = new Vector3(v[1], v[2], 0) * .01f;
                    node.transform.localEulerAngles = new Vector3(0, 0, v[0]);
                    node.transform.localScale = new Vector3(v[3], v[4], 1);
                    var visual = Node("Sprite", node.transform); float sx = v[5] / o[2], sy = v[6] / o[3];
                    visual.transform.localPosition = new Vector3((o[0] + b[2] * .5f - o[2] * .5f) * sx,
                        (o[1] + b[3] * .5f - o[3] * .5f) * sy, 0) * .01f;
                    visual.transform.localEulerAngles = new Vector3(0, 0, rotated ? -90 : 0);
                    visual.transform.localScale = rotated ? new Vector3(sy, sx, 1) : new Vector3(sx, sy, 1);
                    var renderer = visual.AddComponent<SpriteRenderer>(); renderer.sprite = sprite;
                    renderer.color = ColorOf(slot.color) * ColorOf(a.color);
                    renderer.sharedMaterial = slot.blend == 1 ? additive : normal; renderers[a.slot] = renderer;
                }
                renderers[a.slot].sortingOrder = a.slot; renderers[a.slot].enabled = false;
            }
            var setup = new AnimationClip { name = "setup", legacy = true };
            for (int i = 0; i < bones.Length; i++) {
                var v = data.bones[i].values; string path = AnimationUtility.CalculateTransformPath(bones[i], root.transform);
                Constant(setup, path, typeof(Transform), "localEulerAnglesRaw.z", v[0]);
                Constant(setup, path, typeof(Transform), "m_LocalPosition.x", v[1] * .01f);
                Constant(setup, path, typeof(Transform), "m_LocalPosition.y", v[2] * .01f);
                Constant(setup, path, typeof(Transform), "m_LocalScale.x", v[3]);
                Constant(setup, path, typeof(Transform), "m_LocalScale.y", v[4]);
            }
            foreach (var r in renderers) if (r != null) {
                string path = AnimationUtility.CalculateTransformPath(r.transform, root.transform);
                Constant(setup, path, r.GetType(), "m_Enabled", 0);
                if (r is SpriteRenderer sr) for (int c = 0; c < 4; c++) Constant(setup, path, typeof(SpriteRenderer), "m_Color." + Channels[c], sr.color[c]);
                if (r is SkinnedMeshRenderer sk) for (int f = 0; f < sk.sharedMesh.blendShapeCount; f++) Constant(setup, path, typeof(SkinnedMeshRenderer), "blendShape.Frame" + f, 0);
                var tint = r.GetComponent<RecoveredMeshTint>();
                if (tint != null) for (int c = 0; c < 4; c++) Constant(setup, path, typeof(RecoveredMeshTint), "color." + Channels[c], tint.Color[c]);
            }
            setup = SaveAsset(setup, Folder + "/setup.anim");
            var player = root.AddComponent<Animation>(); player.playAutomatically = false;
            foreach (var source in glow ? new[] { reveal } : new[] { reveal, idle }) {
                var clip = Convert(source, data, bones, renderers, root.transform);
                clip = SaveAsset(clip, Folder + "/" + source.name + ".anim"); player.AddClip(clip, source.name);
            }
            var config = new SerializedObject(glow ? (Component)root.AddComponent<RecoveredCoinGlow>() : root.AddComponent<RecoveredCoinReveal>());
            config.FindProperty("animationPlayer").objectReferenceValue = player;
            config.FindProperty("setup").objectReferenceValue = setup;
            if (!glow) config.FindProperty("revealSpeed").floatValue = 3;
            config.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, Folder + ".prefab"); AssetDatabase.SaveAssets();
        } finally { Object.DestroyImmediate(root); }
    }

    private static AnimationClip Convert(Clip source, Data data, Transform[] bones, Renderer[] renderers, Transform root)
    {
        var clip = new AnimationClip { name = source.name, legacy = true, frameRate = 30, wrapMode = source.name == "idle_chun" ? WrapMode.Loop : WrapMode.Once };
        foreach (var t in source.timelines) {
            bool slot = t.domain == "slot", deform = t.domain == "deform";
            if ((slot || deform) && renderers[t.index] == null) throw new InvalidOperationException("Missing native slot.");
            string path = AnimationUtility.CalculateTransformPath(slot || deform ? renderers[t.index].transform : bones[t.index], root);
            if (deform) {
                for (int f = 0; f < t.frames.Length; f++) {
                    var keys = new Keyframe[t.frames.Length];
                    for (int k = 0; k < keys.Length; k++) keys[k] = new Keyframe(t.frames[k].time, k == f ? 100 : 0);
                    var curve = new AnimationCurve(keys);
                    for (int k = 0; k < keys.Length; k++) {
                        AnimationUtility.SetKeyLeftTangentMode(curve, k, AnimationUtility.TangentMode.Linear);
                        AnimationUtility.SetKeyRightTangentMode(curve, k, AnimationUtility.TangentMode.Linear);
                    }
                    if (keys[0].time > 0) curve.AddKey(new Keyframe(0, 0, float.PositiveInfinity, float.PositiveInfinity));
                    clip.SetCurve(path, typeof(SkinnedMeshRenderer), "blendShape.Frame" + f, curve);
                }
            } else if (slot && t.kind == 0) {
                var keys = new Keyframe[t.frames.Length];
                var attachment = Array.Find(data.attachments, a => a.slot == t.index);
                for (int k = 0; k < keys.Length; k++) {
                    if (t.frames[k].attachment != null && t.frames[k].attachment != attachment.key) throw new InvalidOperationException("Attachment switch.");
                    keys[k] = new Keyframe(t.frames[k].time, t.frames[k].attachment == null ? 0 : 1, float.PositiveInfinity, float.PositiveInfinity);
                }
                // Spine leaves delayed attachments in setup pose until their first key.
                var curve = new AnimationCurve(keys);
                if (keys[0].time > 0) curve.AddKey(new Keyframe(0, 0, float.PositiveInfinity, float.PositiveInfinity));
                clip.SetCurve(path, renderers[t.index].GetType(), "m_Enabled", curve);
            } else {
                for (int c = 0; c < t.frames[0].values.Length; c++) {
                    bool meshTint = slot && renderers[t.index] is SkinnedMeshRenderer;
                    string property = slot ? (meshTint ? "color." : "m_Color.") + Channels[c] : t.kind == 0 ? "localEulerAnglesRaw.z" : (t.kind == 4 ? "m_LocalScale." : "m_LocalPosition.") + (c == 0 ? "x" : "y");
                    float offset = slot || t.kind == 4 ? 0 : data.bones[t.index].values[t.kind == 0 ? 0 : c + 1];
                    float factor = slot ? Array.Find(data.attachments, a => a.slot == t.index).color[c] : t.kind == 4 ? data.bones[t.index].values[c + 3] : 1;
                    if (!slot && t.kind == 1) { offset *= .01f; factor *= .01f; }
                    var curve = BuildCoinAppearance.Curve(t.frames, c, offset, factor);
                    if (t.frames[0].time > 0) {
                        float baseline = slot ? (meshTint ? renderers[t.index].GetComponent<RecoveredMeshTint>().Color[c] : ((SpriteRenderer)renderers[t.index]).color[c]) : t.kind == 4 ? data.bones[t.index].values[c + 3] : offset;
                        curve.AddKey(new Keyframe(0, baseline, float.PositiveInfinity, float.PositiveInfinity));
                    }
                    clip.SetCurve(path, slot ? (meshTint ? typeof(RecoveredMeshTint) : typeof(SpriteRenderer)) : typeof(Transform), property, curve);
                }
            }
        }
        return clip;
    }

    private static GameObject Node(string name, Transform parent) { var go = new GameObject(name); go.layer = 5; go.transform.SetParent(parent, false); return go; }
    private static Color ColorOf(float[] c) => new Color(c[0], c[1], c[2], c[3]);
    private static void Constant(AnimationClip clip, string path, Type type, string property, float value) => clip.SetCurve(path, type, property, AnimationCurve.Constant(0, 0, value));
    private static Material Material(string Folder, string name, Texture texture, int blend) {
        var m = new Material(Shader.Find("DragonLegend/Recovered PMA Sprite")) { name = name, mainTexture = texture };
        m.SetFloat("_DestinationBlend", blend); return SaveAsset(m, Folder + "/" + name + ".mat");
    }
    private static T SaveAsset<T>(T value, string path) where T : Object {
        var existing = AssetDatabase.LoadAssetAtPath<T>(path);
        if (existing == null) { AssetDatabase.CreateAsset(value, path); return value; }
        EditorUtility.CopySerialized(value, existing); Object.DestroyImmediate(value); return existing;
    }
}
