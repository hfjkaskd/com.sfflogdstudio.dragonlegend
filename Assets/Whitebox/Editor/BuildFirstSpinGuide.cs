using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using DragonLegend.Whitebox;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public static class BuildFirstSpinGuide
{
    public static void Save()
    {
        const string temporary="Assets/Whitebox/Editor/FirstSpinGuideSource.prefab";
        var map=new Dictionary<string,string>();BuildTreasureCard.Script<Image>(map,"3cf5a44414476512e00c3e7a2569a919");
        BuildTreasureCard.Script<Button>(map,"18d0a90695249463551c00f45766e642");BuildTreasureCard.Script<TextMeshProUGUI>(map,"3f96b1d166d19b209697e35b35d65c76");
        BuildTreasureCard.Script<RecoveredEmptyRaycastGraphic>(map,"e0b4d57e8f658b407a2ad25df85fb0d6");
        string source=File.ReadAllText("ReferenceOriginal/Res/Prefabs/Guide.prefab").Replace("\r","");var removed=new List<string>();
        source=Regex.Replace(source,@"--- !u!\d+ &(\d+)\n.*?(?=\n--- !u!|\z)",m=>{
            var script=Regex.Match(m.Value,@"m_Script:.*guid: (\w+)");
            if(script.Success&&!map.ContainsKey(script.Groups[1].Value)){removed.Add(m.Groups[1].Value);return "";}return m.Value;
        },RegexOptions.Singleline);
        foreach(string id in removed)source=source.Replace("  - component: {fileID: "+id+"}\n","");
        foreach(var pair in new Dictionary<string,string>{{"0f7b96a110c9e514cb759acee2570d26","zy_s9g01"},{"695a30e040639c144a8da84cafa4667b","zy_s9g02"}}){
            BuildTreasureCard.Map(map,pair.Key,"Assets/Resources/RecoveredArt/Res/UI/zhujiemian/"+pair.Value+".png");
            source=source.Replace("guid: "+pair.Key+", type: 2","guid: "+pair.Key+", type: 3");
        }
        foreach(var pair in map)source=source.Replace(pair.Key,pair.Value);
        foreach(Match m in Regex.Matches(source,@"guid: (\w+)"))if(string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(m.Groups[1].Value)))throw new InvalidDataException("Unmapped Guide reference "+m.Groups[1].Value);
        File.WriteAllText(temporary,source);AssetDatabase.ImportAsset(temporary,ImportAssetOptions.ForceSynchronousImport);
        var root=PrefabUtility.LoadPrefabContents(temporary);
        try {
            var content=root.transform.Find("Content");
            var originalDragon=(RectTransform)content.Find("SkeletonGraphic (ef_long)");
            var dragon=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/RecoveredUI/JackpotPopupArt/ef_long.prefab"),content);
            var rect=(RectTransform)dragon.transform;rect.anchorMin=originalDragon.anchorMin;rect.anchorMax=originalDragon.anchorMax;
            rect.pivot=originalDragon.pivot;rect.sizeDelta=originalDragon.sizeDelta;rect.anchoredPosition=originalDragon.anchoredPosition;rect.localScale=originalDragon.localScale;
            dragon.layer=originalDragon.gameObject.layer;dragon.transform.SetSiblingIndex(originalDragon.GetSiblingIndex());
            Object.DestroyImmediate(originalDragon.gameObject);dragon.name="SkeletonGraphic (ef_long)";
            var originalText=root.GetComponentInChildren<TextMeshProUGUI>();var textParent=originalText.transform.parent;int textIndex=originalText.transform.GetSiblingIndex();
            Object.DestroyImmediate(originalText.gameObject);
            var text=(RecoveredGuideText)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<RecoveredGuideText>("Assets/Resources/RecoveredUI/GuideText.prefab"),textParent);
            text.transform.SetSiblingIndex(textIndex);
            var view=root.AddComponent<RecoveredFirstSpinGuide>();var settings=new SerializedObject(view);
            settings.FindProperty("node").objectReferenceValue=content;settings.FindProperty("text").objectReferenceValue=text;
            settings.FindProperty("background").objectReferenceValue=root.transform.Find("black").GetComponent<Button>();settings.ApplyModifiedPropertiesWithoutUndo();
            root.SetActive(false);PrefabUtility.SaveAsPrefabAsset(root,"Assets/Resources/RecoveredUI/FirstSpinGuide.prefab");AssetDatabase.SaveAssets();
        } finally {PrefabUtility.UnloadPrefabContents(root);AssetDatabase.DeleteAsset(temporary);}
        BuildCoreRoundFlow.Save();
    }
}
