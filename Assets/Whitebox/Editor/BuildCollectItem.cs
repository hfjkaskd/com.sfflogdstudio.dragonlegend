using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using DragonLegend.Whitebox;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class BuildCollectItem
{
    public static void Save()
    {
        const string output = "Assets/Resources/RecoveredUI/CollectItem.prefab";
        var map = new Dictionary<string, string>();
        BuildTreasureCard.Script<Image>(map, "3cf5a44414476512e00c3e7a2569a919");
        BuildTreasureCard.Script<TextMeshProUGUI>(map, "3f96b1d166d19b209697e35b35d65c76");
        BuildTreasureCard.Script<RecoveredCollectItem>(map, "3b2699851b9ea5478cb761c430d81fed");
        string[] ids = { "8b0bed87f68ba0942b6e08202fe647ad", "71dcdf4ff4f4dcf48a803e3aa459811a", "e906727e398cc6c4fbf3aa5dc2bd4ac4" };
        string[] names = { "t_bak_*.png", "t_icon_10_*.png", "t_hongdian_*.png" };
        for (int i = 0; i < ids.Length; i++) {
            string[] matches = Directory.GetFiles("Assets/Resources/RecoveredArt/IndividualSprites", names[i]);
            if (matches.Length != 1) throw new InvalidDataException("Ambiguous collection sprite " + names[i]);
            string path = matches[0].Replace('\\', '/');
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100; importer.mipmapEnabled = false; importer.npotScale = TextureImporterNPOTScale.None;
            importer.alphaIsTransparency = true; importer.textureCompression = TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
            BuildTreasureCard.Map(map, ids[i], path);
        }
        string yaml = File.ReadAllText("ReferenceOriginal/Res/Prefabs/CollectItem.prefab").Replace("\r", "");
        yaml = yaml.Replace("  Icon:", "  icon:").Replace("  RedPoint:", "  redPoint:").Replace("  PointTxt:", "  pointText:");
        foreach (var pair in map) yaml = yaml.Replace(pair.Key, pair.Value);
        foreach (string id in ids) yaml = yaml.Replace("guid: " + map[id] + ", type: 2", "guid: " + map[id] + ", type: 3");
        foreach (Match m in Regex.Matches(yaml, @"guid: (\w+)"))
            if (string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(m.Groups[1].Value))) throw new InvalidDataException("Unmapped CollectItem " + m.Groups[1].Value);
        File.WriteAllText(output, yaml, new UTF8Encoding(false)); AssetDatabase.ImportAsset(output, ImportAssetOptions.ForceSynchronousImport);
        var root = PrefabUtility.LoadPrefabContents(output);
        try {
            var settings = new SerializedObject(root.GetComponent<RecoveredCollectItem>());
            settings.FindProperty("missingColor").colorValue = new Color(.14509804546833038f, .14509804546833038f, .14509804546833038f, 1);
            settings.FindProperty("collectedColor").colorValue = Color.white;
            settings.FindProperty("countFormat").stringValue = "{0}";
            var paths = new List<string>(Directory.GetFiles("Assets/Resources/RecoveredArt/IndividualSprites", "t_icon_*.png")); paths.Sort();
            var icons = settings.FindProperty("icons"); icons.arraySize = paths.Count;
            for (int i = 0; i < paths.Count; i++) {
                if (AssetDatabase.LoadAssetAtPath<Sprite>(paths[i]) == null) throw new InvalidDataException("Unconfigured collection sprite " + paths[i]);
                var value = icons.GetArrayElementAtIndex(i);
                value.FindPropertyRelative("id").intValue = int.Parse(Regex.Match(paths[i], @"t_icon_(\d+)_").Groups[1].Value);
                value.FindPropertyRelative("path").stringValue = paths[i].Replace('\\', '/').Replace("Assets/Resources/", "").Replace(".png", "");
            }
            settings.ApplyModifiedPropertiesWithoutUndo(); PrefabUtility.SaveAsPrefabAsset(root, output); AssetDatabase.SaveAssets();
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
}
