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

public static class BuildWheelRotor
{
    private const string ItemPath = "Assets/Resources/RecoveredUI/WheelItem.prefab";
    private const string RotorPath = "Assets/Resources/RecoveredUI/WheelRotor.prefab";
    public static void Save()
    {
        var map = new Dictionary<string, string>();
        Script<Image>(map, "3cf5a44414476512e00c3e7a2569a919");
        Script<TextMeshProUGUI>(map, "3f96b1d166d19b209697e35b35d65c76");
        Script<RecoveredWheelItem>(map, "fd92aa3bf0e1af2a363aa2cf33fc6217");
        string[] guids = { "4c41f6f71644a9947ad6800c4f8583e1", "4f40947d7a0bf3d489011f932540b989", "5a5cbcf0c0d8cd84997457e3ab153ec8",
            "8f7ffffb9ea39b34d9db9746f417bf54", "d609a1a0a0803fc469b8b03dc5646585", "d8241e6256c0e3b4d9239bfdcb1cd5ec" };
        string[] names = { "fw_grand", "fw_jinbi", "fw_minor", "fw_major", "fw_bg01", "fw_meijing" };
        for (int i = 0; i < guids.Length; i++) map[guids[i]] = AssetDatabase.AssetPathToGUID("Assets/Resources/RecoveredArt/Res/UI/wanfa_02/" + names[i] + ".png");
        string source = File.ReadAllText("ReferenceOriginal/Res/Prefabs/WheelItem.prefab").Replace("\r", "");
        source = source.Replace("  JackPot:", "  jackpot:").Replace("  Cash:", "  cash:").Replace("  Coin:", "  coin:").Replace("  JackPotSprites:", "  jackpotSprites:");
        Write(ItemPath, source, map, guids);
        var item = PrefabUtility.LoadPrefabContents(ItemPath);
        try
        {
            var serialized = new SerializedObject(item.GetComponent<RecoveredWheelItem>());
            serialized.FindProperty("cashText").objectReferenceValue = item.transform.Find("Cash").GetChild(1).GetComponent<TMP_Text>();
            serialized.FindProperty("coinText").objectReferenceValue = item.transform.Find("Coin").GetChild(1).GetComponent<TMP_Text>();
            serialized.FindProperty("sidewaysIndex").intValue = 6;
            serialized.FindProperty("sidewaysRotation").vector3Value = new Vector3(0, 0, 90);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(item, ItemPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(item); }

        source = File.ReadAllText("ReferenceOriginal/Res/ViewPrefabs/UIWheelView.prefab").Replace("\r", "");
        var blocks = new Dictionary<string, string>();
        foreach (Match m in Regex.Matches(source, @"^--- !u!\d+ &(\d+)\n.*?(?=^--- !u!|\z)", RegexOptions.Multiline | RegexOptions.Singleline)) blocks.Add(m.Groups[1].Value, m.Value);
        var result = new StringBuilder("%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n");
        Append("224733206282342372", "224733206282342372", blocks, result);
        Write(RotorPath, result.ToString(), map, guids);
        var rotor = PrefabUtility.LoadPrefabContents(RotorPath);
        try
        {
            var serialized = new SerializedObject(rotor.AddComponent<RecoveredWheelRotor>());
            serialized.FindProperty("itemPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<RecoveredWheelItem>(ItemPath);
            var anchors = serialized.FindProperty("itemAnchors"); anchors.arraySize = 8;
            for (int i = 0; i < 8; i++) anchors.GetArrayElementAtIndex(i).objectReferenceValue = rotor.transform.Find(i.ToString());
            serialized.FindProperty("spinDuration").floatValue = float.Parse(Regex.Match(source, @"spinDuration: (\S+)").Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
            serialized.FindProperty("extraRotations").intValue = int.Parse(Regex.Match(source, @"extraRotations: (\S+)").Groups[1].Value);
            if (Regex.Match(source, @"spinEase: (\S+)").Groups[1].Value != "9") throw new InvalidDataException("Unexpected native wheel ease");
            serialized.FindProperty("segmentAngle").floatValue = 45;
            // Exact OutCubic polynomial, with its endpoint derivatives.
            serialized.FindProperty("spinEase").animationCurveValue = new AnimationCurve(new Keyframe(0, 0, 3, 3), new Keyframe(1, 1, 0, 0));
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(rotor, RotorPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(rotor); }
        AssetDatabase.SaveAssets();
    }
    private static void Write(string path, string yaml, Dictionary<string, string> map, string[] sprites)
    {
        foreach (var pair in map) { if (string.IsNullOrEmpty(pair.Value)) throw new InvalidDataException(pair.Key); yaml = yaml.Replace(pair.Key, pair.Value); }
        foreach (string id in sprites) yaml = yaml.Replace("guid: " + map[id] + ", type: 2", "guid: " + map[id] + ", type: 3");
        foreach (Match m in Regex.Matches(yaml, @"guid: (\w+)")) if (string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(m.Groups[1].Value))) throw new InvalidDataException("Unmapped wheel GUID " + m.Groups[1].Value);
        File.WriteAllText(path, yaml, new UTF8Encoding(false));
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
    }
    private static void Append(string rect, string root, Dictionary<string, string> blocks, StringBuilder result)
    {
        string transform = blocks[rect], go = Regex.Match(transform, @"m_GameObject: \{fileID: (\d+)").Groups[1].Value;
        result.Append(blocks[go]);
        foreach (Match m in Regex.Matches(blocks[go], @"component: \{fileID: (\d+)"))
        {
            string id = m.Groups[1].Value, b = blocks[id];
            if (id == root) b = Regex.Replace(b, @"m_Father: \{fileID: \d+\}", "m_Father: {fileID: 0}");
            result.Append(b);
        }
        string children = Regex.Match(transform, @"m_Children:\n(.*?)  m_Father:", RegexOptions.Singleline).Groups[1].Value;
        foreach (Match child in Regex.Matches(children, @"fileID: (\d+)")) Append(child.Groups[1].Value, root, blocks, result);
    }
    private static void Script<T>(Dictionary<string, string> map, string original) where T : MonoBehaviour
    {
        var host = new GameObject("Script lookup", typeof(RectTransform));
        try { map[original] = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(MonoScript.FromMonoBehaviour(host.AddComponent<T>()))); }
        finally { Object.DestroyImmediate(host); }
    }
}
