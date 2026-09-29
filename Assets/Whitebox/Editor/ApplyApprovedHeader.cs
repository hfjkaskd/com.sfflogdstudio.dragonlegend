using System;
using System.Collections.Generic;
using System.IO;
using DragonLegend.Whitebox;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Editor authoring only. Runtime keeps the original player bindings and callbacks.
public static class ApplyApprovedHeader
{
    private const string Folder = "Assets/Resources/MainSkin/ApprovedHeader";
    private const string BalancePath = "Assets/Resources/MainSkin/Assets/RecoveredUI/BalancePanel.prefab";
    private const string JackpotPath = "Assets/Resources/MainSkin/Assets/RecoveredUI/JackpotMeters.prefab";
    private const string PlayfieldPath = "Assets/Resources/MainSkin/Assets/RecoveredUI/SpinPlayfield.prefab";
    private const string UtilityPath = "Assets/Resources/RecoveredUI/MainUtility/Entry.prefab";
    private const string EntryPath = "Assets/Resources/Whitebox/GameEntry.prefab";
    private const string ScenePath = "Assets/Whitebox/Scenes/GameEntry.unity";
    private static TMP_FontAsset tmpFont;
    private static Font valueFont;
    private static Material goldMaterial, darkMaterial;

    [MenuItem("Dragon Legend/Apply Approved Red Gold Header")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Apply the approved header outside Play mode.");
        ImportAssets();
        tmpFont = Need<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath("31628181d58311344bb283c127fc9aba"));
        valueFont = Need<Font>("Assets/Resources/RecoveredArt/Font/QuorumStd-Black_zitidi.com.otf");
        goldMaterial = TextMaterial("HeaderGoldText", new Color(1f, .9f, .58f), new Color(.12f, .015f, .003f), .18f);
        darkMaterial = TextMaterial("HeaderDarkText", new Color(.27f, .012f, .005f), new Color(1f, .82f, .34f), .08f);
        Edit(BalancePath, ApplyBalance);
        Edit(JackpotPath, ApplyJackpots);
        Edit(PlayfieldPath, root => {
            var meters = root.GetComponent<RecoveredSpinPlayfield>().JackpotMeters;
            HeaderRoot((RectTransform)meters.transform, 224, 176);
            PrefabUtility.RecordPrefabInstancePropertyModifications(meters.transform);
        });
        Edit(UtilityPath, ApplyUtility);
        Edit("Assets/Resources/RecoveredUI/MainCashOutStatus.prefab", BuildMainCashOutStatus.ApplyHeaderLayout);
        Edit(EntryPath, ApplyGm);
        ApplySceneGm();
        AssetDatabase.SaveAssets();
        Directory.CreateDirectory("Artifacts/ApprovedArt");
        File.WriteAllText("Artifacts/ApprovedArt/header-authoring-report.json",
            "{\"status\":\"APPLIED\",\"runtimeScriptsChanged\":false,\"dynamicBalancePreserved\":true,\"dynamicJackpotsPreserved\":true,\"originalCurrencySpritesPreserved\":true}");
        Debug.Log("Approved red/gold header authored. Dynamic fields and original Button listeners retained.");
    }

    private static void ImportAssets()
    {
        foreach (var name in new[] { "header-top", "header-jackpots", "header-cashout", "header-progress", "header-progress-fill", "header-gm", "header-help", "header-settings" })
        {
            string path = Folder + "/" + name + ".png";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new FileNotFoundException(path);
            importer.textureType = name == "header-top" || name == "header-jackpots" ? TextureImporterType.Default : TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            // Large plates use the existing premultiplied-alpha UI shader.
            // Do not let Unity dilate transparent RGB into their PMA edge pixels.
            importer.alphaIsTransparency = name != "header-top" && name != "header-jackpots";
            importer.mipmapEnabled = false;
            importer.isReadable = false;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
        }
    }

    private static void ApplyBalance(GameObject root)
    {
        HeaderRoot((RectTransform)root.transform, 0, 227);
        var panel = root.GetComponent<RecoveredBalancePanel>();
        var so = new SerializedObject(panel);
        var balance = Ref<TextMeshProUGUI>(so, "greenText");
        var level = Ref<TextMeshProUGUI>(so, "levelText");
        var progress = Ref<TextMeshProUGUI>(so, "progressText");
        var fill = Ref<Image>(so, "progressFill");
        var cash = Ref<Image>(so, "cashImage");
        Image badge = null;
        foreach (var image in root.GetComponentsInChildren<Image>(true))
            if (image.name == "paypal") badge = image;
        if (badge == null) throw new InvalidDataException("Original currency badge was not found.");
        Sprite originalCash = cash.sprite, originalBadge = badge.sprite;
        foreach (var graphic in root.GetComponentsInChildren<Graphic>(true)) graphic.enabled = false;

        // Two large decorative quad graphics load their textures by Resources path.
        Plate(root.transform, "_ApprovedHeaderTop", "MainSkin/ApprovedHeader/header-top", 0, 0, 1080, 227);
        Place(cash.rectTransform, root.transform, 19, 27, 137, 119);
        cash.sprite = originalCash; cash.color = Color.white; cash.enabled = true; cash.raycastTarget = false; cash.preserveAspect = true;
        Place(badge.rectTransform, root.transform, 109, 87, 47, 47);
        badge.sprite = originalBadge; badge.color = Color.white; badge.enabled = true; badge.raycastTarget = false; badge.preserveAspect = true;

        Place(balance.rectTransform, root.transform, 176, 20, 383, 85);
        StyleText(balance, 80, 30, goldMaterial, TextAlignmentOptions.Left);

        var status = ImageNode(root.transform, "_ApprovedProgressPlate", Sprite("header-progress"));
        Place(status.rectTransform, root.transform, 67, 145, 362, 72);
        Place(fill.rectTransform, root.transform, 137, 163, 276, 34);
        fill.sprite = Sprite("header-progress-fill"); fill.color = Color.white; fill.enabled = true; fill.raycastTarget = false;
        fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal; fill.fillOrigin = 0;
        Place(level.rectTransform, root.transform, 76, 154, 66, 55);
        StyleText(level, 46, 22, goldMaterial, TextAlignmentOptions.Center);
        level.enableVertexGradient = false; level.color = Color.white;
        Place(progress.rectTransform, root.transform, 151, 157, 263, 48);
        StyleText(progress, 38, 20, goldMaterial, TextAlignmentOptions.Center);
        progress.enableVertexGradient = false; progress.color = Color.white;
        ApplyProgressLayers(root);

        ApplyPayPalWithdrawButton(root);
    }

    // Reuse the payment selector's authored artwork at its native proportions.
    public static void ApplyPayPalWithdrawButton(GameObject root)
    {
        var withdraw = root.GetComponent<RecoveredBalancePanel>().WithdrawButton;
        var sourceFrame = Need<Sprite>(AssetDatabase.GUIDToAssetPath("f4d8b77359ab74a4c9fb46b04de13f53"));
        var face = Need<Sprite>(AssetDatabase.GUIDToAssetPath("2150d221c27ed884f93b5ec718e7edd5"));
        // The final 15 pixels belong to the selector arrow, outside the button.
        var crop = sourceFrame.rect;
        crop.y += 15; crop.height -= 15;
        var generated = UnityEngine.Sprite.Create(sourceFrame.texture, crop, new Vector2(.5f, .5f),
            sourceFrame.pixelsPerUnit, 0, SpriteMeshType.FullRect);
        generated.name = "MainPayPalFrame";
        string framePath = Folder + "/MainPayPalFrame.asset";
        var frame = AssetDatabase.LoadAssetAtPath<Sprite>(framePath);
        if (frame == null) { AssetDatabase.CreateAsset(generated, framePath); frame = generated; }
        else { EditorUtility.CopySerialized(generated, frame); Object.DestroyImmediate(generated); EditorUtility.SetDirty(frame); }

        float scale = 111f / crop.height;
        float width = crop.width * scale;
        Place((RectTransform)withdraw.transform, root.transform, 1056 - width, 106, width, 111);
        foreach (var graphic in withdraw.GetComponentsInChildren<Graphic>(true))
        { graphic.enabled = false; graphic.raycastTarget = false; }
        var buttonImage = withdraw.GetComponent<Image>();
        if (buttonImage == null) buttonImage = withdraw.gameObject.AddComponent<Image>();
        buttonImage.sprite = frame; buttonImage.color = Color.white; buttonImage.enabled = true;
        buttonImage.raycastTarget = true; buttonImage.type = Image.Type.Simple; buttonImage.preserveAspect = false;
        withdraw.targetGraphic = buttonImage; withdraw.transition = Selectable.Transition.ColorTint;

        var logo = ImageNode(withdraw.transform, "_ApprovedPaymentLogo", face);
        Place(logo.rectTransform, withdraw.transform, 7 * scale, 6 * scale,
            face.rect.width * scale, face.rect.height * scale);
        logo.preserveAspect = true;
        logo.transform.SetAsLastSibling();
        // Keep the selector's orange/white outline in front of the blue face.
        var outline = ImageNode(withdraw.transform, "_ApprovedPaymentFrame", frame);
        Place(outline.rectTransform, withdraw.transform, 0, 0, width, 111);
        outline.transform.SetAsLastSibling();
        KeepOriginalListeners(withdraw);
    }

    // The source plate contains both the badge and the opaque progress track.
    // Only its left badge belongs above the fill; text remains in front of both.
    public static void ApplyProgressLayers(GameObject root)
    {
        var panel = new SerializedObject(root.GetComponent<RecoveredBalancePanel>());
        var level = Ref<TextMeshProUGUI>(panel, "levelText");
        var progress = Ref<TextMeshProUGUI>(panel, "progressText");
        var fill = Ref<Image>(panel, "progressFill");
        var plate = root.transform.Find("_ApprovedProgressPlate").GetComponent<Image>();
        var badge = ImageNode(root.transform, "_ApprovedLevelBadge", plate.sprite);
        var source = plate.rectTransform;
        var rect = badge.rectTransform;
        rect.anchorMin = source.anchorMin; rect.anchorMax = source.anchorMax; rect.pivot = source.pivot;
        rect.anchoredPosition = source.anchoredPosition; rect.sizeDelta = source.sizeDelta;
        rect.localScale = source.localScale; rect.localRotation = source.localRotation;
        badge.type = Image.Type.Filled; badge.fillMethod = Image.FillMethod.Horizontal;
        badge.fillOrigin = 0; badge.fillAmount = 124f / plate.sprite.rect.width;
        plate.transform.SetAsLastSibling();
        fill.transform.SetAsLastSibling();
        badge.transform.SetAsLastSibling();
        level.transform.SetAsLastSibling();
        progress.transform.SetAsLastSibling();
    }

    private static void ApplyJackpots(GameObject root)
    {
        HeaderRoot((RectTransform)root.transform, 224, 176);
        var meters = root.GetComponent<RecoveredJackpotMeters>();
        Plate(root.transform, "_ApprovedJackpotPlate", "MainSkin/ApprovedHeader/header-jackpots", 0, 0, 1080, 175);
        float[] x = { 12, 427, 741 };
        float[] widths = { 400, 300, 323 };
        string[] titles = { "GRAND", "MAJOR", "MINOR" };
        for (int i = 0; i < 3; i++)
        {
            var meter = meters.At(i);
            Place((RectTransform)meter.transform, root.transform, x[i], 6, widths[i], 136);
            // Keep animation player and callback controller active; hide only the obsolete drawing.
            meter.Icon.Rig.enabled = false;
            meter.Icon.Rig.raycastTarget = false;
            var title = TextNode(meter.transform, "_ApprovedTierTitle", titles[i]);
            Place(title.rectTransform, meter.transform, 5, 3, widths[i] - 10, 51);
            StyleText(title, i == 0 ? 54 : 45, 28, goldMaterial, TextAlignmentOptions.Center);
            var label = meter.Label;
            Place(label.rectTransform, meter.transform, 10, 55, widths[i] - 20, 79);
            // Reuse the exact dynamically updated Text component with the original Quorum font.
            // A dynamic font enables Unity's standard best-fit for real long currency values.
            label.font = valueFont; label.material = null; label.fontStyle = FontStyle.Normal;
            label.fontSize = 70; label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 20; label.resizeTextMaxSize = i == 0 ? 72 : 65;
            label.alignment = TextAnchor.MiddleCenter; label.alignByGeometry = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap; label.verticalOverflow = VerticalWrapMode.Truncate;
            label.color = new Color(1f, .87f, .47f); label.raycastTarget = false; label.enabled = true;
            var outline = label.GetComponent<Outline>();
            if (outline == null) outline = label.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(.12f, .012f, 0f, 1f); outline.effectDistance = new Vector2(2.3f, -2.3f);
            Shadow shadow = null;
            // Outline is a Shadow subclass. Reuse only the independent shadow on reruns.
            foreach (var candidate in label.GetComponents<Shadow>())
                if (!(candidate is Outline)) { shadow = candidate; break; }
            if (shadow == null) shadow = label.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(.1f, 0f, 0f, .95f); shadow.effectDistance = new Vector2(1, -4);
        }
    }

    private static void ApplyUtility(GameObject root)
    {
        HeaderRoot((RectTransform)root.transform, 0, 227);
        var utility = root.GetComponent<RecoveredMainUtility>();
        StyleUtilityButton(utility.HelpButton, root.transform, "header-help", 835, 22, 99, 83);
        StyleUtilityButton(utility.SettingsButton, root.transform, "header-settings", 946, 22, 103, 83);
    }

    private static void StyleUtilityButton(Button button, Transform parent, string sprite, float x, float y, float w, float h)
    {
        Place((RectTransform)button.transform, parent, x, y, w, h);
        var image = button.GetComponent<Image>();
        if (image == null) image = button.gameObject.AddComponent<Image>();
        image.sprite = Sprite(sprite); image.color = Color.white; image.enabled = true;
        image.type = Image.Type.Simple; image.preserveAspect = false; image.raycastTarget = true;
        button.targetGraphic = image; button.transition = Selectable.Transition.ColorTint;
        KeepOriginalListeners(button);
        PrefabUtility.RecordPrefabInstancePropertyModifications(button.transform);
        PrefabUtility.RecordPrefabInstancePropertyModifications(image);
        PrefabUtility.RecordPrefabInstancePropertyModifications(button);
    }

    private static void ApplyGm(GameObject root)
    {
        var gm = root.GetComponent<RecoveredGmPanel>();
        if (gm == null) throw new InvalidDataException("Missing existing GM controller.");
        var button = gm.ToggleButton;
        Place((RectTransform)button.transform, root.transform, 610, 31, 107, 58);
        var image = button.GetComponent<Image>();
        image.sprite = Sprite("header-gm"); image.color = Color.white; image.type = Image.Type.Simple;
        image.raycastTarget = true; button.targetGraphic = image;
        foreach (var label in button.GetComponentsInChildren<Text>(true))
        {
            label.fontSize = 28; label.resizeTextForBestFit = true; label.resizeTextMinSize = 14;
            label.resizeTextMaxSize = 28; label.alignment = TextAnchor.MiddleCenter; label.color = Color.white;
        }
        KeepOriginalListeners(button);
        PrefabUtility.RecordPrefabInstancePropertyModifications(button.transform);
        PrefabUtility.RecordPrefabInstancePropertyModifications(image);
    }

    private static void ApplySceneGm()
    {
        var scene = SceneManager.GetSceneByPath(ScenePath);
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (!opened && scene.isDirty) throw new InvalidOperationException("GameEntry scene has unsaved edits; save before authoring.");
        if (opened) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        try
        {
            foreach (var root in scene.GetRootGameObjects())
                foreach (var entry in root.GetComponentsInChildren<GameEntry>(true)) ApplyGm(entry.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
    }

    private static void Plate(Transform parent, string name, string resource, float x, float y, float w, float h)
    {
        var existing = parent.Find(name);
        var go = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RecoveredRegionRig));
        go.layer = 5;
        var rect = (RectTransform)go.transform;
        Place(rect, parent, x, y, w, h); rect.SetAsFirstSibling();
        var rig = go.GetComponent<RecoveredRegionRig>();
        rig.enabled = true; rig.raycastTarget = false; rig.color = Color.white;
        rig.bones = new[] { new RecoveredRegionRig.Bone { name = "root", parent = -1, scaleX = 1, scaleY = 1 } };
        rig.slots = new[] { new RecoveredRegionRig.Slot { bone = 0, attachment = 0, tint = Color.white } };
        rig.regions = new[] { new RecoveredRegionRig.Region {
            vertices = new[] { new Vector2(0, -h), new Vector2(0, 0), new Vector2(w, 0), new Vector2(w, -h) },
            uv = new[] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0) }, tint = Color.white
        } };
        var so = new SerializedObject(rig); so.FindProperty("atlasPath").stringValue = resource; so.ApplyModifiedPropertiesWithoutUndo();
        rig.material = Need<Material>("Assets/Resources/RecoveredUI/WinBurst/Pma.mat");
        rig.RefreshPose();
    }

    private static void HeaderRoot(RectTransform rect, float top, float height)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1);
        rect.pivot = new Vector2(.5f, 1); rect.sizeDelta = new Vector2(1080, height);
        rect.anchoredPosition = new Vector2(0, -top); rect.localScale = Vector3.one; rect.localRotation = Quaternion.identity;
    }

    private static void Place(RectTransform rect, Transform parent, float x, float y, float width, float height)
    {
        if (rect.parent != parent) rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, height);
        rect.localScale = Vector3.one; rect.localRotation = Quaternion.identity;
    }

    private static Image ImageNode(Transform parent, string name, Sprite sprite)
    {
        var node = parent.Find(name);
        var go = node != null ? node.gameObject : new GameObject(name, typeof(RectTransform), typeof(Image));
        if (go.transform.parent != parent) go.transform.SetParent(parent, false);
        go.layer = 5;
        var image = go.GetComponent<Image>(); image.sprite = sprite; image.color = Color.white;
        image.enabled = true; image.raycastTarget = false; image.type = Image.Type.Simple;
        return image;
    }

    private static TextMeshProUGUI TextNode(Transform parent, string name, string text)
    {
        var node = parent.Find(name);
        var go = node != null ? node.gameObject : new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        if (go.transform.parent != parent) go.transform.SetParent(parent, false);
        go.layer = 5;
        var label = go.GetComponent<TextMeshProUGUI>(); label.text = text; return label;
    }

    private static void StyleText(TextMeshProUGUI label, float maxSize, float minSize, Material material, TextAlignmentOptions alignment)
    {
        label.enabled = true; label.gameObject.SetActive(true); label.font = tmpFont; label.fontSharedMaterial = material;
        label.fontSize = maxSize; label.enableAutoSizing = true; label.fontSizeMin = minSize; label.fontSizeMax = maxSize;
        label.alignment = alignment; label.enableWordWrapping = false; label.overflowMode = TextOverflowModes.Truncate;
        label.color = Color.white; label.enableVertexGradient = true;
        label.colorGradient = new VertexGradient(Color.white, Color.white, new Color(1f, .74f, .24f), new Color(1f, .74f, .24f));
        label.raycastTarget = false; label.margin = Vector4.zero;
    }

    private static Material TextMaterial(string name, Color face, Color outline, float width)
    {
        string path = Folder + "/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null) { material = new Material(tmpFont.material); material.name = name; AssetDatabase.CreateAsset(material, path); }
        if (material.HasProperty("_FaceColor")) material.SetColor("_FaceColor", face);
        if (material.HasProperty("_OutlineColor")) material.SetColor("_OutlineColor", outline);
        if (material.HasProperty("_OutlineWidth")) material.SetFloat("_OutlineWidth", width);
        if (material.HasProperty("_UnderlayColor")) material.SetColor("_UnderlayColor", new Color(0, 0, 0, .8f));
        material.EnableKeyword("OUTLINE_ON"); EditorUtility.SetDirty(material);
        return material;
    }

    private static void KeepOriginalListeners(Button button)
    {
        if (button.onClick.GetPersistentEventCount() != 0)
            throw new InvalidDataException(button.name + " unexpectedly contains serialized click callbacks.");
    }

    private static void Edit(string path, Action<GameObject> action)
    {
        var root = PrefabUtility.LoadPrefabContents(path);
        try { action(root); PrefabUtility.SaveAsPrefabAsset(root, path); }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static T Ref<T>(SerializedObject so, string field) where T : Object
    {
        var property = so.FindProperty(field);
        var value = property != null ? property.objectReferenceValue as T : null;
        if (value == null) throw new InvalidDataException("Missing header field " + field);
        return value;
    }

    private static Sprite Sprite(string name) { return Need<Sprite>(Folder + "/" + name + ".png"); }
    private static T Need<T>(string path) where T : Object
    {
        var value = AssetDatabase.LoadAssetAtPath<T>(path);
        if (value == null) throw new FileNotFoundException(typeof(T).Name + " " + path);
        return value;
    }
}
