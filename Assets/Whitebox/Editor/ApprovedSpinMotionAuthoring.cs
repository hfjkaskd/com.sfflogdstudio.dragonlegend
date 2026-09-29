using System;
using System.IO;
using Unity.Collections;
using DragonLegend.Whitebox;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.U2D;
using UnityEngine.UI;

// Offline sprite geometry and clip binding only; the approved PNG stays intact.
public static class ApprovedSpinMotionAuthoring
{
    private const string PrefabPath = "Assets/Resources/MainSkin/Assets/RecoveredUI/SpinButton.prefab";
    private const string Folder = "Assets/Resources/MainSkin/Animations/SpinButton";
    private const string BodyPath = "Visual/root/dx/diabn/Slot1/Image";
    public const string RotorName = "ApprovedBaguaRotor";
    private const int Segments = 64;

    public static void Apply()
    {
        Directory.CreateDirectory(Folder);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var source = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/MainSkin/Assets/RecoveredUI/SpinButton/diban.asset");
        if (source == null) throw new InvalidOperationException("Approved Bagua base sprite is missing.");
        Rect rect = source.rect;
        float radius = Mathf.Min(rect.width, rect.height) * .175f;
        var frame = CreateFrame(source, radius);
        var rotor = CreateRotor(source, radius + .35f);
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var body = root.transform.Find(BodyPath).GetComponent<Image>();
            var button = root.GetComponent<RecoveredSpinButton>();
            var originalTarget = button.Button.targetGraphic;
            body.sprite = frame;
            body.useSpriteMesh = true;
            var prior = body.transform.Find(RotorName);
            Image icon;
            if (prior == null)
            {
                var node = new GameObject(RotorName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                node.layer = root.layer;
                node.transform.SetParent(body.transform, false);
                icon = node.GetComponent<Image>();
            }
            else icon = prior.GetComponent<Image>();
            if (icon == null) throw new InvalidOperationException("Unexpected Bagua rotor hierarchy.");
            var transform = icon.rectTransform;
            transform.anchorMin = transform.anchorMax = transform.pivot = new Vector2(.5f, .5f);
            transform.anchoredPosition = Vector2.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            transform.sizeDelta = new Vector2(body.rectTransform.rect.width * rotor.rect.width / rect.width,
                body.rectTransform.rect.height * rotor.rect.height / rect.height);
            icon.sprite = rotor; icon.useSpriteMesh = true; icon.material = body.material;
            icon.color = Color.white; icon.raycastTarget = false; icon.enabled = true;

            string path = AnimationUtility.CalculateTransformPath(transform, root.transform);
            var player = root.GetComponent<Animation>();
            foreach (string name in new[] { "idle", "dianji" })
            {
                var clip = BuildClip(name, path, false);
                player.RemoveClip(name); player.AddClip(clip, name);
            }
            var serialized = new SerializedObject(button);
            serialized.FindProperty("setup").objectReferenceValue = BuildClip("setup", path, true);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            if (button.Button.targetGraphic != originalTarget || !body.raycastTarget)
                throw new InvalidOperationException("Spin Button interaction changed.");
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
    }

    private static Sprite CreateFrame(Sprite source, float radius)
    {
        var sprite = Sprite.Create(source.texture, source.rect, new Vector2(.5f, .5f), source.pixelsPerUnit, 0, SpriteMeshType.FullRect);
        var vertices = new Vector2[Segments * 2];
        var triangles = new ushort[Segments * 6];
        Vector2 half = source.rect.size / 2f;
        for (int i = 0; i < Segments; i++)
        {
            float angle = i * Mathf.PI * 2f / Segments;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            float edge = 1f / Mathf.Max(Mathf.Abs(direction.x) / half.x, Mathf.Abs(direction.y) / half.y);
            vertices[i * 2] = direction * edge / source.pixelsPerUnit;
            vertices[i * 2 + 1] = direction * radius / source.pixelsPerUnit;
            int next = (i + 1) % Segments;
            int t = i * 6;
            triangles[t] = (ushort)(i * 2); triangles[t + 1] = (ushort)(next * 2); triangles[t + 2] = (ushort)(i * 2 + 1);
            triangles[t + 3] = (ushort)(i * 2 + 1); triangles[t + 4] = (ushort)(next * 2); triangles[t + 5] = (ushort)(next * 2 + 1);
        }
        return SaveSprite(sprite, "BaguaFrame", vertices, triangles);
    }

    private static Sprite CreateRotor(Sprite source, float radius)
    {
        Vector2 center = source.rect.center;
        var rect = new Rect(center.x - radius, center.y - radius, radius * 2, radius * 2);
        var sprite = Sprite.Create(source.texture, rect, new Vector2(.5f, .5f), source.pixelsPerUnit, 0, SpriteMeshType.FullRect);
        var vertices = new Vector2[Segments + 1];
        var triangles = new ushort[Segments * 3];
        for (int i = 0; i < Segments; i++)
        {
            float angle = i * Mathf.PI * 2f / Segments;
            vertices[i + 1] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius / source.pixelsPerUnit;
            triangles[i * 3] = 0; triangles[i * 3 + 1] = (ushort)(i + 1); triangles[i * 3 + 2] = (ushort)((i + 1) % Segments + 1);
        }
        return SaveSprite(sprite, "BaguaRotor", vertices, triangles);
    }

    private static Sprite SaveSprite(Sprite generated, string name, Vector2[] vertices, ushort[] triangles)
    {
        generated.name = name;
        string path = Folder + "/" + name + ".asset";
        var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing == null) { AssetDatabase.CreateAsset(generated, path); existing = generated; }
        else { EditorUtility.CopySerialized(generated, existing); UnityEngine.Object.DestroyImmediate(generated); }
        // Author persistent native vertex channels; OverrideGeometry is restricted
        // in Edit Mode. Coordinates here are Sprite local units, UVs use the atlas.
        var positions = new NativeArray<Vector3>(vertices.Length, Allocator.Temp);
        var uv = new NativeArray<Vector2>(vertices.Length, Allocator.Temp);
        var indices = new NativeArray<ushort>(triangles, Allocator.Temp);
        try
        {
            for (int i = 0; i < vertices.Length; i++)
            {
                positions[i] = vertices[i];
                Vector2 pixel = existing.rect.position + existing.pivot + vertices[i] * existing.pixelsPerUnit;
                uv[i] = new Vector2(pixel.x / existing.texture.width, pixel.y / existing.texture.height);
            }
            existing.SetVertexCount(vertices.Length);
            existing.SetVertexAttribute(VertexAttribute.Position, positions);
            existing.SetVertexAttribute(VertexAttribute.TexCoord0, uv);
            existing.SetIndices(indices);
        }
        finally { positions.Dispose(); uv.Dispose(); indices.Dispose(); }
        EditorUtility.SetDirty(existing);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing.vertices.Length != vertices.Length || existing.triangles.Length != triangles.Length)
            throw new InvalidOperationException("Authored Bagua geometry did not persist: " + name);
        return existing;
    }

    private static AnimationClip BuildClip(string name, string rotorPath, bool setup)
    {
        var original = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Resources/RecoveredUI/SpinButton/" + name + ".anim");
        if (original == null) throw new InvalidOperationException("Original Spin animation missing: " + name);
        var clip = UnityEngine.Object.Instantiate(original); clip.name = name;
        AnimationCurve curve = setup ? AnimationCurve.Constant(0, 0, 0) : null;
        if (!setup)
            foreach (var binding in AnimationUtility.GetCurveBindings(original))
                if (binding.path == "Visual/root/dx/yezi" && binding.propertyName == "localEulerAnglesRaw.z")
                    curve = AnimationUtility.GetEditorCurve(original, binding);
        if (curve == null) throw new InvalidOperationException("Original Spin rotation curve missing: " + name);
        clip.SetCurve(rotorPath, typeof(Transform), "localEulerAnglesRaw.z", curve);
        string path = Folder + "/" + name + ".anim";
        var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (existing == null) { AssetDatabase.CreateAsset(clip, path); return clip; }
        EditorUtility.CopySerialized(clip, existing); UnityEngine.Object.DestroyImmediate(clip);
        EditorUtility.SetDirty(existing); return existing;
    }
}
