using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using DragonLegend.Whitebox;
using TMPro;
using UnityEditor;
using UnityEngine;

public static class BuildGuideText
{
    public static void Save()
    {
        const string temporary="Assets/Whitebox/Editor/GuideTextSource.prefab";
        string source=File.ReadAllText("ReferenceOriginal/Res/Prefabs/Guide.prefab").Replace("\r","");
        var blocks=new Dictionary<string,string>();
        foreach(Match m in Regex.Matches(source,@"^--- !u!\d+ &(\d+)\n.*?(?=^--- !u!|\z)",RegexOptions.Multiline|RegexOptions.Singleline))blocks.Add(m.Groups[1].Value,m.Value);
        var yaml=new StringBuilder("%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n");
        BuildTreasureCard.Append("224632457200066912","224632457200066912",blocks,yaml);
        string text=yaml.ToString().Replace("  - component: {fileID: 114969106005120503}\n","");
        text=Regex.Replace(text,@"--- !u!114 &114969106005120503\n.*?(?=--- !u!|\z)","",RegexOptions.Singleline);
        var map=new Dictionary<string,string>();BuildTreasureCard.Script<TextMeshProUGUI>(map,"3f96b1d166d19b209697e35b35d65c76");
        foreach(var pair in map)text=text.Replace(pair.Key,pair.Value);
        foreach(Match m in Regex.Matches(text,@"guid: (\w+)"))if(string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(m.Groups[1].Value)))throw new InvalidDataException("Unmapped Guide text "+m.Groups[1].Value);
        File.WriteAllText(temporary,text);AssetDatabase.ImportAsset(temporary,ImportAssetOptions.ForceSynchronousImport);
        var root=PrefabUtility.LoadPrefabContents(temporary);
        try {
            root.name="GuideText";var view=root.AddComponent<RecoveredGuideText>();var settings=new SerializedObject(view);
            settings.FindProperty("label").objectReferenceValue=root.GetComponent<TextMeshProUGUI>();
            settings.FindProperty("charsPerSecond").floatValue=100;
            settings.FindProperty("firstSpinText").stringValue="Click SPIN and let the dragon breathe fire into your wins!";
            settings.FindProperty("extraWildText").stringValue="Tap here to unleash extra Wilds!";
            settings.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root,"Assets/Resources/RecoveredUI/GuideText.prefab");AssetDatabase.SaveAssets();
        } finally {PrefabUtility.UnloadPrefabContents(root);AssetDatabase.DeleteAsset(temporary);}
    }
}
