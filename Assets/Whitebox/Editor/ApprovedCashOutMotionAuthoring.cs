using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Edit-mode authoring for the approved wallet. Runtime playback uses Unity Animation only.
public static class ApprovedCashOutMotionAuthoring
{
    private const string Folder = "Assets/Resources/MainSkin/Animations";
    private const string ClipName = "CashOutWalletIdle";
    private const string GlowName = "ApprovedWalletGlow";
    private const string EvidencePath = "Tools/Evidence/ef_tixianicon.json";
    private const string AtlasPath = "Assets/Resources/RecoveredArt/Res/Spine/按钮/tixian/ef_tixianicon.png";
    private const string AdditiveMaterialPath = "Assets/Resources/RecoveredUI/SpinButton/PmaAdditive.mat";
    private const float Duration = 2f;

    public static void Apply(Image image)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Wallet motion authoring requires Edit mode.");
        if (image == null || image.name != "ApprovedSideIcon" || image.sprite == null)
            throw new InvalidOperationException("Wallet motion requires the approved wallet Image.");

        var source = JsonUtility.FromJson<BuildCoinAppearance.Data>(File.ReadAllText(EvidencePath));
        var region = Array.Find(source.regions, r => r.name == "digl0");
        var attachment = Array.Find(source.attachments, a => a.key == "digl0");
        var background = Array.Find(source.attachments, a => a.key == "db");
        var idle = Array.Find(source.animations, a => a.name == "idle");
        if (region == null || attachment == null || background == null || idle == null ||
            region.rotate != 0 || !Mathf.Approximately(idle.duration, Duration))
            throw new InvalidDataException("The original wallet glow evidence has changed.");
        var alphaTimeline = Array.Find(idle.timelines,
            t => t.domain == "slot" && t.index == attachment.slot && t.kind == 1);
        if (alphaTimeline == null)
            throw new InvalidDataException("The original wallet glow alpha timeline is missing.");
        var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasPath);
        var additive = AssetDatabase.LoadAssetAtPath<Material>(AdditiveMaterialPath);
        if (atlas == null || additive == null)
            throw new InvalidDataException("The original wallet atlas or additive UI material is missing.");

        EnsureFolder(Folder);
        var bounds = region.bounds;
        var spriteRect = new Rect(bounds[0], atlas.height - bounds[1] - bounds[3], bounds[2], bounds[3]);
        if (spriteRect.xMin < 0 || spriteRect.yMin < 0 ||
            spriteRect.xMax > atlas.width || spriteRect.yMax > atlas.height)
            throw new InvalidDataException("The wallet glow region is outside the original atlas.");
        var sprite = Sprite.Create(atlas, spriteRect, new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect);
        sprite.name = "CashOutWalletGlow";
        sprite = SaveAsset(sprite, Folder + "/CashOutWalletGlow.asset");

        var existing = image.transform.Find(GlowName);
        Image glow;
        if (existing == null)
        {
            var node = new GameObject(GlowName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            node.layer = image.gameObject.layer;
            node.transform.SetParent(image.transform, false);
            glow = node.GetComponent<Image>();
        }
        else
        {
            glow = existing.GetComponent<Image>();
            if (glow == null) throw new InvalidOperationException("Unexpected wallet glow child.");
        }

        // Preserve the trimmed source region's displayed bounds. Only the generated
        // Image and its child move; the Button, text and finger target stay fixed.
        var bone = source.bones[source.slots[attachment.slot].bone];
        var values = attachment.values;
        var offsets = region.offsets;
        var parentRect = image.rectTransform.rect;
        float xUnit = values[5] / offsets[2] * values[3] * bone.values[3] * parentRect.width / background.values[5];
        float yUnit = values[6] / offsets[3] * values[4] * bone.values[4] * parentRect.height / background.values[6];
        var rect = glow.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.sizeDelta = new Vector2(bounds[2] * xUnit, bounds[3] * yUnit);
        rect.anchoredPosition3D = new Vector3(
            (offsets[0] + bounds[2] * .5f - offsets[2] * .5f) * xUnit,
            (offsets[1] + bounds[3] * .5f - offsets[3] * .5f) * yUnit, 0);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        glow.sprite = sprite;
        glow.material = additive;
        glow.type = Image.Type.Simple;
        glow.preserveAspect = false;
        glow.raycastTarget = false;
        glow.color = new Color(attachment.color[0], attachment.color[1], attachment.color[2], 0);
        glow.enabled = true;
        glow.gameObject.SetActive(true);

        var clip = new AnimationClip { name = ClipName, legacy = true, frameRate = 30, wrapMode = WrapMode.Loop };
        var breathing = new AnimationCurve(new Keyframe(0, 1, 0, 0),
            new Keyframe(Duration * .5f, 1.06f, 0, 0), new Keyframe(Duration, 1, 0, 0));
        clip.SetCurve("", typeof(Transform), "m_LocalScale.x", breathing);
        clip.SetCurve("", typeof(Transform), "m_LocalScale.y", breathing);
        clip.SetCurve("", typeof(Transform), "m_LocalScale.z", AnimationCurve.Constant(0, Duration, 1));
        // Reuse the original 0 -> 1 -> 0 alpha timing and Bezier interpolation.
        clip.SetCurve(GlowName, typeof(Image), "m_Color.a", BuildCoinAppearance.Curve(alphaTimeline.frames, 3, 0, 1));
        clip = SaveAsset(clip, Folder + "/" + ClipName + ".anim");

        var player = image.GetComponent<Animation>();
        if (player == null) player = image.gameObject.AddComponent<Animation>();
        if (player.GetClip(ClipName) != null) player.RemoveClip(ClipName);
        player.AddClip(clip, ClipName);
        player.clip = clip;
        player.wrapMode = WrapMode.Loop;
        player.playAutomatically = true;
        player.cullingType = AnimationCullingType.AlwaysAnimate;
        player.enabled = true;
        image.rectTransform.localScale = Vector3.one;
        Persist(rect);
        Persist(glow);
        Persist(image.rectTransform);
        Persist(player);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int separator = path.LastIndexOf('/');
        string parent = path.Substring(0, separator);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, path.Substring(separator + 1));
    }

    private static T SaveAsset<T>(T value, string path) where T : Object
    {
        var existing = AssetDatabase.LoadAssetAtPath<T>(path);
        if (existing == null)
        {
            AssetDatabase.CreateAsset(value, path);
            return value;
        }
        // Keep GUID/local-file identity stable so repeated authoring does not break references.
        EditorUtility.CopySerialized(value, existing);
        Object.DestroyImmediate(value);
        EditorUtility.SetDirty(existing);
        return existing;
    }

    private static void Persist(Component component)
    {
        EditorUtility.SetDirty(component);
        if (PrefabUtility.IsPartOfPrefabInstance(component))
            PrefabUtility.RecordPrefabInstancePropertyModifications(component);
    }
}
