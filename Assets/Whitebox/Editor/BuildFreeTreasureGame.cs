using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using DragonLegend.Whitebox;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class BuildFreeTreasureGame
{
    private const string Temporary = "Assets/Whitebox/Editor/FreeTreasureIconImport.prefab";
    public static void Save()
    {
        string source = File.ReadAllText("ReferenceOriginal/Res/ViewPrefabs/UIMainView.prefab").Replace("\r", "");
        var result = new StringBuilder("%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n");
        foreach (string id in new[] { "1220697728379336", "224909888333327498", "222571634487674524", "114057666396558467" })
        {
            string block = Regex.Match(source, @"^--- !u!\d+ &" + id + @"\n.*?(?=^--- !u!|\z)", RegexOptions.Multiline | RegexOptions.Singleline).Value;
            if (string.IsNullOrEmpty(block)) throw new InvalidDataException("Missing CardPrefab source " + id);
            if (id == "224909888333327498") block = Regex.Replace(block, @"m_Father: \{fileID: \d+\}", "m_Father: {fileID: 0}");
            result.Append(block);
        }
        var lookup = new GameObject("Image script", typeof(RectTransform), typeof(Image));
        string script;
        try { script = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(MonoScript.FromMonoBehaviour(lookup.GetComponent<Image>()))); }
        finally { Object.DestroyImmediate(lookup); }
        string sprite = AssetDatabase.AssetPathToGUID("Assets/Resources/RecoveredArt/Res/UI/pop-up/tc_sc_bak02.png");
        if (string.IsNullOrEmpty(sprite)) throw new InvalidDataException("Missing Treasure entry sprite");
        string yaml = result.ToString().Replace("3cf5a44414476512e00c3e7a2569a919", script)
            .Replace("27b6ea552c8080340b36ee5350e3df1d, type: 2", sprite + ", type: 3");
        File.WriteAllText(Temporary, yaml); AssetDatabase.ImportAsset(Temporary, ImportAssetOptions.ForceSynchronousImport);
        var root = new GameObject("FreeTreasureGame", typeof(RectTransform), typeof(RecoveredFreeTreasureGame)); root.layer = 5;
        try
        {
            var rect = (RectTransform)root.transform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.sizeDelta = Vector2.zero;
            var icon = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Temporary), root.transform);
            PrefabUtility.UnpackPrefabInstance(icon, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            icon.name = "CardPrefab"; // Unity names the imported root after the temporary asset.
            var window = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/RecoveredUI/TreasureWindow.prefab"), root.transform);
            var settings = new SerializedObject(root.GetComponent<RecoveredFreeTreasureGame>());
            settings.FindProperty("icon").objectReferenceValue = icon.GetComponent<RectTransform>();
            settings.FindProperty("window").objectReferenceValue = window.GetComponent<RecoveredTreasureWindow>();
            settings.FindProperty("entryScale").vector3Value = Vector3.one * .4f;
            settings.FindProperty("arrivalScale").vector3Value = Vector3.one;
            settings.FindProperty("flightDuration").floatValue = .6f;
            settings.FindProperty("arcHeightRatio").floatValue = .3f; settings.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, "Assets/Resources/RecoveredUI/FreeTreasureGame.prefab"); AssetDatabase.SaveAssets();
        }
        finally { Object.DestroyImmediate(root); AssetDatabase.DeleteAsset(Temporary); }
    }
}
