using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using DragonLegend.Whitebox;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class BuildCollectWindow
{
    public static void Save()
    {
        const string output = "Assets/Resources/RecoveredUI/CollectWindow.prefab";
        const string fontFolder = "Assets/Resources/RecoveredUI/CollectWindowArt";
        Directory.CreateDirectory(fontFolder);
        foreach (string name in new[] { "Gold.asset", "Gold.asset.meta", "Gold_Material.mat", "Gold_Material.mat.meta" }) {
            if (!File.Exists(fontFolder + "/" + name)) File.Copy("ReferenceOriginal/Res/Font/" + name, fontFolder + "/" + name);
        }
        AssetDatabase.Refresh();
        var map = new Dictionary<string, string>();
        BuildTreasureCard.Script<Image>(map, "3cf5a44414476512e00c3e7a2569a919");
        BuildTreasureCard.Script<Text>(map, "04f84fc2003509a5e7e068ec1271cc40");
        BuildTreasureCard.Script<Button>(map, "18d0a90695249463551c00f45766e642");
        BuildTreasureCard.Script<TextMeshProUGUI>(map, "3f96b1d166d19b209697e35b35d65c76");
        BuildTreasureCard.Script<GraphicRaycaster>(map, "86fe8f3fc59dc06ea6b45a1bbee64682");
        string[] ids = {"1db5e0034867e2542bccb036825f12c6", "1e744280f516c5b4b93445d19c920a4d", "3008572a0bbfdf44faed15b37312eb4e", "3694a020e04ab824f977ebc5b2f3f415", "8f8d560997a1e4f43b323f80fa5bc152", "bf892e5732864bf49a5e9a1d6f2e98e8", "c8f3a72e1477ac94d9f29eacfcb2a21e", "fa8cf4410b4beae4281db66ef02f45e2"};
        string[] sprites = {"IndividualSprites/t_bar_01_4189293795293543357.png", "IndividualSprites/t_bg01_3592521700422912920.png", "Res/UI/pop-up/ty_btn_cha.png", "IndividualSprites/t_hb_amazon_4890283666839968588.png", "IndividualSprites/t_bar_02_8915098488435045803.png", "IndividualSprites/t_bg02_8815561202762729472.png", "IndividualSprites/t_hb_jingbi_2611533919808204360.png", "IndividualSprites/t_treasures_txt_2411717305829263836.png"};
        Vector4[] borders = { new Vector4(30,0,28,0), Vector4.zero, Vector4.zero, Vector4.zero, new Vector4(22,0,23,0), new Vector4(0,0,0,1273), Vector4.zero, Vector4.zero };
        for (int i = 0; i < ids.Length; i++) {
            string path = "Assets/Resources/RecoveredArt/" + sprites[i];
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100; importer.mipmapEnabled = false; importer.npotScale = TextureImporterNPOTScale.None;
            importer.spriteBorder = borders[i];
            importer.alphaIsTransparency = true; importer.textureCompression = TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
            BuildTreasureCard.Map(map, ids[i], path);
        }
        string source = File.ReadAllText("ReferenceOriginal/Res/ViewPrefabs/UICollectView.prefab").Replace("\r", "");
        var removed = new List<string>();
        string yaml = Regex.Replace(source, @"--- !u!114 &(\d+)\n.*?(?=\n--- !u!|\z)", m => {
            if (Regex.IsMatch(m.Value, @"guid: (fdb85647eee1c32249313b1ac79992b9|3991d2bd099e12203576b2cf89b113da|ff326197253ae4cec0b46632391386ec|132b6501dc1746e5b20bc04c0ad3cc98)")) { removed.Add(m.Groups[1].Value); return ""; }
            return m.Value;
        }, RegexOptions.Singleline);
        foreach (string id in removed) yaml = yaml.Replace("  - component: {fileID: " + id + "}\n", "");
        foreach (var pair in map) yaml = yaml.Replace(pair.Key, pair.Value);
        foreach (string id in ids) yaml = yaml.Replace("guid: " + map[id] + ", type: 2", "guid: " + map[id] + ", type: 3");
        foreach (Match m in Regex.Matches(yaml, @"guid: (\w+)")) if (string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(m.Groups[1].Value))) throw new InvalidDataException("Unmapped Collect GUID " + m.Groups[1].Value);
        File.WriteAllText(output, yaml, new UTF8Encoding(false)); AssetDatabase.ImportAsset(output, ImportAssetOptions.ForceSynchronousImport);
        var root = PrefabUtility.LoadPrefabContents(output);
        try {
            var content = root.transform.Find("Content"); var top = content.Find("Top");
            foreach (string section in new[] { "Gold", "Green" }) {
                var prior = top.Find(section + "/SkeletonGraphic (ef_shoucanggl)");
                var node = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/RecoveredUI/JackpotPopupArt/ef_shoucanggl.prefab"), prior.parent);
                var a = (RectTransform)node.transform; var b = (RectTransform)prior;
                a.anchorMin = b.anchorMin; a.anchorMax = b.anchorMax; a.pivot = b.pivot; a.sizeDelta = b.sizeDelta;
                a.anchoredPosition3D = b.anchoredPosition3D; a.localScale = b.localScale; a.localRotation = b.localRotation;
                a.SetSiblingIndex(b.GetSiblingIndex()); node.name = prior.name; node.SetActive(prior.gameObject.activeSelf); Object.DestroyImmediate(prior.gameObject);
            }
            var shine = new SerializedObject(top.Find("TitleBg").gameObject.AddComponent<RecoveredTitleShine>());
            Set(shine, "template", AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/RecoveredUI/BonusRewardPopup/TitleShine.mat"));
            shine.FindProperty("effectFactor").floatValue = .20301695f; shine.ApplyModifiedPropertiesWithoutUndo();
            var adapt = content.gameObject.AddComponent<RecoveredSafeArea>();
            var window = root.AddComponent<RecoveredCollectWindow>(); var p = new SerializedObject(window);
            Set(p, "content", content); Set(p, "fill", top.Find("Green/Progress/Fill")); Set(p, "listParent", content.Find("Rect"));
            Set(p, "gold", top.Find("Gold").gameObject); Set(p, "green", top.Find("Green").gameObject);
            Set(p, "rewardText", top.Find("Green/Text (TMP)").GetComponent<TMP_Text>());
            Set(p, "progressText", top.Find("Green/Progress/Text (TMP)").GetComponent<TMP_Text>());
            Set(p, "closeButton", top.Find("CloseBtn").GetComponent<Button>()); Set(p, "safeArea", adapt);
            Set(p, "listPrefab", AssetDatabase.LoadAssetAtPath<RecoveredCollectList>("Assets/Resources/RecoveredUI/CollectList.prefab"));
            p.FindProperty("progressFormat").stringValue = "{0}/{1}";
            p.FindProperty("fromScale").floatValue = 0; p.FindProperty("toScale").floatValue = 1; p.FindProperty("duration").floatValue = .3f;
            p.FindProperty("enterEase").animationCurveValue = new AnimationCurve(new Keyframe(0, 0, 4.70158f, 4.70158f), new Keyframe(1, 1, 0, 0));
            p.FindProperty("exitEase").animationCurveValue = new AnimationCurve(new Keyframe(0, 0, 0, 0), new Keyframe(1, 1, 4.70158f, 4.70158f));
            p.ApplyModifiedPropertiesWithoutUndo();
            var mask = new GameObject("_WindowBg", typeof(RectTransform), typeof(Image), typeof(Button)); mask.layer = 5;
            mask.transform.SetParent(root.transform, false); mask.transform.SetAsFirstSibling();
            var maskRect = (RectTransform)mask.transform; maskRect.anchorMin = Vector2.zero; maskRect.anchorMax = Vector2.one; maskRect.sizeDelta = Vector2.zero;
            var image = mask.GetComponent<Image>(); image.color = new Color(0, 0, 0, .65f);
            var button = mask.GetComponent<Button>(); button.targetGraphic = image; button.transition = Selectable.Transition.None;
            root.GetComponent<Canvas>().sortingOrder = 300; root.GetComponent<Canvas>().overrideSorting = true;
            root.name = "CollectWindow"; root.SetActive(false); PrefabUtility.SaveAsPrefabAsset(root, output);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
    }
    private static void Set(SerializedObject p, string name, Object value) => p.FindProperty(name).objectReferenceValue = value;
}
