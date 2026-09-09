using System.Collections.Generic;
using System.IO;
using DragonLegend.Whitebox;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class BuildCollectList
{
    public static void Save()
    {
        const string output = "Assets/Resources/RecoveredUI/CollectList.prefab";
        var map = new Dictionary<string, string>();
        BuildTreasureCard.Script<Image>(map, "3cf5a44414476512e00c3e7a2569a919");
        BuildTreasureCard.Script<ScrollRect>(map, "2cbaf7f938a676aaf8fab3181b92a517");
        BuildTreasureCard.Script<RectMask2D>(map, "69dacd7a039c12e90cde90bbae65247a");
        string yaml = File.ReadAllText("ReferenceOriginal/Res/Prefabs/ListView.prefab");
        foreach (var pair in map) yaml = yaml.Replace(pair.Key, pair.Value);
        File.WriteAllText(output, yaml); AssetDatabase.ImportAsset(output, ImportAssetOptions.ForceSynchronousImport);
        var root = PrefabUtility.LoadPrefabContents(output);
        try {
            root.name = "ListView";
            var scroll = root.GetComponent<ScrollRect>(); scroll.horizontal = false; scroll.vertical = true;
            var template = (RecoveredCollectItem)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<RecoveredCollectItem>("Assets/Resources/RecoveredUI/CollectItem.prefab"), root.transform);
            template.transform.localPosition = Vector3.one * 9999;
            var controller = root.AddComponent<RecoveredCollectList>(); var settings = new SerializedObject(controller);
            settings.FindProperty("scroll").objectReferenceValue = scroll; settings.FindProperty("template").objectReferenceValue = template;
            settings.FindProperty("cellSize").vector2Value = new Vector2(310, 360); settings.FindProperty("columns").intValue = 3;
            settings.FindProperty("hidePosition").vector3Value = Vector3.one * 9999; settings.FindProperty("movementThresholdSquared").floatValue = 2;
            settings.ApplyModifiedPropertiesWithoutUndo(); PrefabUtility.SaveAsPrefabAsset(root, output); AssetDatabase.SaveAssets();
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
}
