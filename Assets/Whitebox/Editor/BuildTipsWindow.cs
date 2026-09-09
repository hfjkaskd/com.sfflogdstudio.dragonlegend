using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using DragonLegend.Whitebox;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public static class BuildTipsWindow
{
    public static void Save()
    {
        const string temporary="Assets/Whitebox/Editor/TipsSourceImport.prefab";
        var map=new Dictionary<string,string>();
        Script<Image>(map,"3cf5a44414476512e00c3e7a2569a919");Script<TextMeshProUGUI>(map,"3f96b1d166d19b209697e35b35d65c76");
        Script<GraphicRaycaster>(map,"86fe8f3fc59dc06ea6b45a1bbee64682");
        Script<VerticalLayoutGroup>(map,"f18f2cc2fe71a3a6d76a570588d5047c");Script<ContentSizeFitter>(map,"21c7954052da7655d96bff866c2b7662");
        string source=File.ReadAllText("ReferenceOriginal/Res/Prefabs/UITipsView.prefab").Replace("\r","");var removed=new List<string>();
        source=Regex.Replace(source,@"--- !u!\d+ &(\d+)\n.*?(?=\n--- !u!|\z)",m=>{
            var script=Regex.Match(m.Value,@"m_Script:.*guid: (\w+)");
            if(script.Success&&!map.ContainsKey(script.Groups[1].Value)){removed.Add(m.Groups[1].Value);return "";}return m.Value;
        },RegexOptions.Singleline);
        foreach(var id in removed)source=Regex.Replace(source,@"  - component: \{fileID: "+id+@"\}\n","");
        foreach(var pair in map)source=source.Replace(pair.Key,pair.Value);
        foreach(Match match in Regex.Matches(source,@"guid: (\w+)"))if(string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(match.Groups[1].Value)))throw new InvalidDataException("Unmapped Tips GUID "+match.Groups[1].Value);
        File.WriteAllText(temporary,source);AssetDatabase.ImportAsset(temporary,ImportAssetOptions.ForceSynchronousImport);
        var root=PrefabUtility.LoadPrefabContents(temporary);
        try {
            var view=root.AddComponent<RecoveredTipsWindow>();var settings=new SerializedObject(view);
            settings.FindProperty("label").objectReferenceValue=root.GetComponentInChildren<TextMeshProUGUI>();
            settings.FindProperty("visibleSeconds").floatValue=2;settings.ApplyModifiedPropertiesWithoutUndo();
            root.GetComponent<Canvas>().sortingOrder=2000;root.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(root,"Assets/Resources/RecoveredUI/TipsWindow.prefab");AssetDatabase.SaveAssets();
        } finally {PrefabUtility.UnloadPrefabContents(root);AssetDatabase.DeleteAsset(temporary);}
        BuildCoreRoundFlow.Save();
    }
    private static void Script<T>(Dictionary<string,string> map,string original) where T:MonoBehaviour
    {
        var host=new GameObject("Script identity",typeof(RectTransform));
        try {map.Add(original,AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(MonoScript.FromMonoBehaviour(host.AddComponent<T>()))));}
        finally {Object.DestroyImmediate(host);}
    }
}
