using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using DragonLegend.Whitebox;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public static class BuildMoreWildEntry
{
    public static void Save()
    {
        BuildJackpotPopupArt.Create("ef_morewildicon","按钮/morewild",Vector2.zero,new Vector2(.5f,.5f),"Tools/Evidence/","Artifacts/MoreWildAuthoring/");
        const string path="Assets/Resources/RecoveredUI/SpinPlayfield.prefab";
        var field=PrefabUtility.LoadPrefabContents(path);
        try {Attach(field);PrefabUtility.SaveAsPrefabAsset(field,path);AssetDatabase.SaveAssets();}
        finally {PrefabUtility.UnloadPrefabContents(field);}
    }
    public static void Attach(GameObject field)
    {
        const string temporary="Assets/Whitebox/Editor/MoreWildEntrySource.prefab";
        var previous=field.transform.Find("MoreWildEntry");if(previous!=null)Object.DestroyImmediate(previous.gameObject);
        string source=File.ReadAllText("ReferenceOriginal/Res/ViewPrefabs/UIMainView.prefab").Replace("\r", "");
        var blocks=new Dictionary<string,string>();
        foreach(Match m in Regex.Matches(source,@"^--- !u!\d+ &(\d+)\n.*?(?=^--- !u!|\z)",RegexOptions.Multiline|RegexOptions.Singleline))blocks.Add(m.Groups[1].Value,m.Value);
        // Keep the original Tubiao parent rectangle, but only its recovered MoreWild child here.
        blocks["224587083680832752"]=Regex.Replace(blocks["224587083680832752"],@"  m_Children:\n.*?  m_Father:","  m_Children:\n  - {fileID: 224534964404139401}\n  m_Father:",RegexOptions.Singleline);
        var yaml=new StringBuilder("%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n");BuildTreasureCard.Append("224587083680832752","224587083680832752",blocks,yaml);
        var map=new Dictionary<string,string>();BuildTreasureCard.Script<Image>(map,"3cf5a44414476512e00c3e7a2569a919");
        BuildTreasureCard.Script<TextMeshProUGUI>(map,"3f96b1d166d19b209697e35b35d65c76");BuildTreasureCard.Script<Button>(map,"18d0a90695249463551c00f45766e642");
        BuildTreasureCard.Script<RecoveredEmptyRaycastGraphic>(map,"e0b4d57e8f658b407a2ad25df85fb0d6");
        string text=yaml.ToString();var removed=new List<string>();
        text=Regex.Replace(text,@"--- !u!\d+ &(\d+)\n.*?(?=\n--- !u!|\z)",m=>{var script=Regex.Match(m.Value,@"m_Script:.*guid: (\w+)");if(script.Success&&!map.ContainsKey(script.Groups[1].Value)){removed.Add(m.Groups[1].Value);return "";}return m.Value;},RegexOptions.Singleline);
        foreach(string id in removed)text=text.Replace("  - component: {fileID: "+id+"}\n", "");
        foreach(var pair in new Dictionary<string,string>{{"fdd2db0b370eb144181979f8343d735a","zjm_wild_bar01"},{"223dd2e68a004e4468076113818725b9","zjm_wild_bar02"}}){BuildTreasureCard.Map(map,pair.Key,"Assets/Resources/RecoveredArt/Res/UI/more_wild/"+pair.Value+".png");text=text.Replace("guid: "+pair.Key+", type: 2","guid: "+pair.Key+", type: 3");}
        foreach(var pair in map)text=text.Replace(pair.Key,pair.Value);
        foreach(Match m in Regex.Matches(text,@"guid: (\w+)"))if(string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(m.Groups[1].Value)))throw new InvalidDataException("Missing MoreWild entry reference "+m.Groups[1].Value);
        GameObject root;
        try {File.WriteAllText(temporary,text);AssetDatabase.ImportAsset(temporary,ImportAssetOptions.ForceSynchronousImport);root=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(temporary),field.transform,false);root.name="MoreWildEntry";}
        finally {AssetDatabase.DeleteAsset(temporary);}
        var entry=root.transform.Find("MoreWild");var old=(RectTransform)entry.Find("SkeletonGraphic (ef_morewildicon)");
        var icon=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/RecoveredUI/JackpotPopupArt/ef_morewildicon.prefab"),entry);
        var rect=(RectTransform)icon.transform;rect.anchorMin=old.anchorMin;rect.anchorMax=old.anchorMax;rect.pivot=old.pivot;rect.sizeDelta=old.sizeDelta;rect.anchoredPosition=old.anchoredPosition;rect.localScale=old.localScale;
        rect.SetSiblingIndex(old.GetSiblingIndex());icon.layer=old.gameObject.layer;icon.name=old.name;Object.DestroyImmediate(old.gameObject);
        var view=root.AddComponent<RecoveredMoreWildEntry>();var settings=new SerializedObject(view);
        settings.FindProperty("button").objectReferenceValue=entry.GetComponent<Button>();
        settings.FindProperty("progress").objectReferenceValue=entry.Find("Progress").gameObject;
        settings.FindProperty("fill").objectReferenceValue=entry.Find("Progress/Image").GetComponent<Image>();
        settings.FindProperty("label").objectReferenceValue=entry.Find("Progress/Text (TMP) (1)").GetComponent<TMP_Text>();
        settings.FindProperty("word").objectReferenceValue=entry.Find("Text (TMP)").GetComponent<TMP_Text>();
        settings.FindProperty("countFormat").stringValue="{0}/{1}";settings.ApplyModifiedPropertiesWithoutUndo();
        settings=new SerializedObject(field.GetComponent<RecoveredSpinPlayfield>());settings.FindProperty("moreWildEntry").objectReferenceValue=view;settings.ApplyModifiedPropertiesWithoutUndo();
    }
}
