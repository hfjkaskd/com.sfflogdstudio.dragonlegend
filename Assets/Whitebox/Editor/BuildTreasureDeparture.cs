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

public static class BuildTreasureDeparture
{
    private const string Temporary = "Assets/Whitebox/Editor/TreasureDestinationImport.prefab";
    public static void Save()
    {
        BuildJackpotPopupArt.Create("ef_shoucangicon", "按钮/shoucang", new Vector2(305.9997f, 264.99994f),
            new Vector2(.5113022f, .543063f), "Tools/Evidence/");
        SaveEntry();
    }
    public static void SaveEntry()
    {
        var map = new Dictionary<string, string>();
        BuildTreasureCard.Script<Image>(map, "3cf5a44414476512e00c3e7a2569a919");
        BuildTreasureCard.Script<Image>(map, "e0b4d57e8f658b407a2ad25df85fb0d6");
        BuildTreasureCard.Script<Button>(map, "18d0a90695249463551c00f45766e642");
        BuildTreasureCard.Script<TextMeshProUGUI>(map, "3f96b1d166d19b209697e35b35d65c76");
        const string cardPath = "Assets/Resources/RecoveredUI/TreasureImg.prefab";
        string card = File.ReadAllText("ReferenceOriginal/Res/Prefabs/TreasureImg.prefab");
        File.WriteAllText(cardPath, card.Replace("3cf5a44414476512e00c3e7a2569a919", map["3cf5a44414476512e00c3e7a2569a919"]));
        AssetDatabase.ImportAsset(cardPath, ImportAssetOptions.ForceSynchronousImport);
        string source = File.ReadAllText("ReferenceOriginal/Res/ViewPrefabs/UIMainView.prefab").Replace("\r", "");
        var blocks = new Dictionary<string, string>();
        foreach (Match m in Regex.Matches(source, @"^--- !u!\d+ &(\d+)\n.*?(?=^--- !u!|\z)", RegexOptions.Multiline | RegexOptions.Singleline))
            blocks.Add(m.Groups[1].Value, m.Value);
        const string rootId = "224587083680832752";
        // Preserve Tubiao's original Rect, keeping the Treasure branch in this prefab bundle.
        blocks[rootId] = Regex.Replace(blocks[rootId], @"  m_Children:\n.*?  m_Father:",
            "  m_Children:\n  - {fileID: 224163425435797775}\n  m_Father:", RegexOptions.Singleline);
        var text = new StringBuilder("%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n");
        BuildTreasureCard.Append(rootId, rootId, blocks, text);
        string yaml = text.ToString(); var removed = new List<string>();
        yaml = Regex.Replace(yaml, @"--- !u!114 &(\d+)\n.*?(?=\n--- !u!|\z)", m => {
            if (m.Value.Contains("ff326197253ae4cec0b46632391386ec")) { removed.Add(m.Groups[1].Value); return ""; }
            return m.Value.Contains("e0b4d57e8f658b407a2ad25df85fb0d6") ?
                m.Value.Replace("m_Color: {r: 1, g: 1, b: 1, a: 1}", "m_Color: {r: 1, g: 1, b: 1, a: 0}") : m.Value;
        }, RegexOptions.Singleline);
        foreach (string id in removed) yaml = yaml.Replace("  - component: {fileID: " + id + "}\n", "");
        foreach (var pair in map) yaml = yaml.Replace(pair.Key, pair.Value);
        foreach (Match m in Regex.Matches(yaml, @"guid: (\w+)"))
            if (string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(m.Groups[1].Value))) throw new InvalidDataException("Unmapped Treasure destination " + m.Groups[1].Value);
        File.WriteAllText(Temporary, yaml); AssetDatabase.ImportAsset(Temporary, ImportAssetOptions.ForceSynchronousImport);
        var root = new GameObject("CollectEntry", typeof(RectTransform), typeof(RecoveredCollectEntry), typeof(RecoveredScreenAdapt)); root.layer = 5;
        try
        {
            var rect = (RectTransform)root.transform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.sizeDelta = Vector2.zero;
            var icons = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Temporary), root.transform);
            PrefabUtility.UnpackPrefabInstance(icons, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction); icons.name = "Tubiao";
            var old = (RectTransform)icons.transform.Find("Treasure/SkeletonGraphic (ef_shoucangicon)");
            var art = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/RecoveredUI/JackpotPopupArt/ef_shoucangicon.prefab"), old.parent);
            var target = (RectTransform)art.transform;
            target.anchorMin = old.anchorMin; target.anchorMax = old.anchorMax; target.pivot = old.pivot; target.sizeDelta = old.sizeDelta;
            target.anchoredPosition3D = old.anchoredPosition3D; target.localRotation = old.localRotation; target.localScale = old.localScale;
            target.SetSiblingIndex(old.GetSiblingIndex()); art.name = old.name; art.layer = old.gameObject.layer; Object.DestroyImmediate(old.gameObject);
            var settings = new SerializedObject(root.GetComponent<RecoveredCollectEntry>());
            settings.FindProperty("destination").objectReferenceValue = target;
            settings.FindProperty("button").objectReferenceValue = icons.transform.Find("Treasure").GetComponent<Button>();
            settings.FindProperty("windowPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<RecoveredCollectWindow>("Assets/Resources/RecoveredUI/CollectWindow.prefab");
            settings.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, "Assets/Resources/RecoveredUI/CollectEntry.prefab");
        }
        finally { Object.DestroyImmediate(root); AssetDatabase.DeleteAsset(Temporary); }
        root = new GameObject("TreasureDeparture", typeof(RectTransform), typeof(RecoveredTreasureDeparture)); root.layer = 5;
        try {
            var rect = (RectTransform)root.transform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.sizeDelta = Vector2.zero;
            var settings = new SerializedObject(root.GetComponent<RecoveredTreasureDeparture>());
            settings.FindProperty("cardPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Image>(cardPath);
            settings.FindProperty("duration").floatValue = .6f; settings.FindProperty("arcHeightRatio").floatValue = .3f;
            settings.FindProperty("initialScale").vector3Value = Vector3.one; settings.FindProperty("endScale").vector3Value = Vector3.one * .3f;
            settings.FindProperty("scaleEase").animationCurveValue = new AnimationCurve(new Keyframe(0, 0, 2, 2), new Keyframe(1, 1, 0, 0));
            settings.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, "Assets/Resources/RecoveredUI/TreasureDeparture.prefab"); AssetDatabase.SaveAssets();
        }
        finally { Object.DestroyImmediate(root); }
        BuildFreeTreasureGame.Save();
        const string mainPath = "Assets/Resources/Whitebox/GameEntry.prefab";
        var main = PrefabUtility.LoadPrefabContents(mainPath);
        try {
            var binding = new SerializedObject(main.GetComponent<GameEntry>());
            binding.FindProperty("collectEntryPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<RecoveredCollectEntry>("Assets/Resources/RecoveredUI/CollectEntry.prefab");
            binding.ApplyModifiedPropertiesWithoutUndo(); PrefabUtility.SaveAsPrefabAsset(main, mainPath); AssetDatabase.SaveAssets();
        }
        finally { PrefabUtility.UnloadPrefabContents(main); }
    }
}
