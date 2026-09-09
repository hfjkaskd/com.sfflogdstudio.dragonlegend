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

public static class BuildBankProgress
{
    public static void Save()
    {
        BuildJackpotPopupArt.Create("ef_slyinhang","yinhang",Vector2.zero,new Vector2(.5f,.5f),"Tools/Evidence/","Artifacts/BankEntryAuthoring/");
        const string path="Assets/Resources/RecoveredUI/SpinPlayfield.prefab";
        var field=PrefabUtility.LoadPrefabContents(path);
        try{Attach(field);PrefabUtility.SaveAsPrefabAsset(field,path);AssetDatabase.SaveAssets();}
        finally{PrefabUtility.UnloadPrefabContents(field);}
    }
    public static void Attach(GameObject field)
    {
        var parent=field.transform.Find("Bottom/Main");var previous=parent.Find("BankProgress");if(previous!=null)Object.DestroyImmediate(previous.gameObject);
        var blocks=new Dictionary<string,string>();string source=File.ReadAllText("ReferenceOriginal/Res/ViewPrefabs/UIMainView.prefab").Replace("\r","");
        foreach(Match m in Regex.Matches(source,@"^--- !u!\d+ &(\d+)\n.*?(?=^--- !u!|\z)",RegexOptions.Multiline|RegexOptions.Singleline))blocks.Add(m.Groups[1].Value,m.Value);
        blocks["224622690832225648"]=Regex.Replace(blocks["224622690832225648"],@"  m_Children:\n.*?  m_Father:","  m_Children:\n  - {fileID: 224600491667411216}\n  - {fileID: 224419535952149204}\n  - {fileID: 224846667071028776}\n  m_Father:",RegexOptions.Singleline);
        var yaml=new StringBuilder("%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n");BuildTreasureCard.Append("224622690832225648","224622690832225648",blocks,yaml);
        var map=new Dictionary<string,string>();BuildTreasureCard.Script<Image>(map,"3cf5a44414476512e00c3e7a2569a919");BuildTreasureCard.Script<Button>(map,"18d0a90695249463551c00f45766e642");
        BuildTreasureCard.Script<TextMeshProUGUI>(map,"3f96b1d166d19b209697e35b35d65c76");BuildTreasureCard.Script<RecoveredEmptyRaycastGraphic>(map,"e0b4d57e8f658b407a2ad25df85fb0d6");
        string text=yaml.ToString();var removed=new List<string>();
        text=Regex.Replace(text,@"--- !u!\d+ &(\d+)\n.*?(?=\n--- !u!|\z)",m=>{var script=Regex.Match(m.Value,@"m_Script:.*guid: (\w+)");if(script.Success&&!map.ContainsKey(script.Groups[1].Value)){removed.Add(m.Groups[1].Value);return "";}return m.Value;},RegexOptions.Singleline);
        foreach(var id in removed)text=text.Replace("  - component: {fileID: "+id+"}\n","");
        foreach(var pair in new Dictionary<string,string>{{"ce718df2546af5548856804535307de8","zjm_bar_bak01"},{"239dc345f0b033147b810d4c923c643f","zjm_bar_bak02"}}){BuildTreasureCard.Map(map,pair.Key,"Assets/Resources/RecoveredArt/Res/UI/zhujiemian/"+pair.Value+".png");text=text.Replace("guid: "+pair.Key+", type: 2","guid: "+pair.Key+", type: 3");}
        BuildTreasureCard.Map(map,"56487477c1a58f648b2fdc3f2134f214","Assets/Resources/Fonts & Materials/#000000_3.mat");
        foreach(var pair in map)text=text.Replace(pair.Key,pair.Value);
        foreach(Match m in Regex.Matches(text,@"guid: (\w+)"))if(string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(m.Groups[1].Value)))throw new InvalidDataException("Missing Bank progress GUID "+m.Groups[1].Value);
        const string temp="Assets/Whitebox/Editor/BankProgressSource.prefab";GameObject root;
        try{File.WriteAllText(temp,text);AssetDatabase.ImportAsset(temp,ImportAssetOptions.ForceSynchronousImport);root=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(temp),parent,false);root.name="BankProgress";}
        finally{AssetDatabase.DeleteAsset(temp);}
        var old=(RectTransform)root.transform.Find("SkeletonGraphic (ef_slyinhang)");
        var icon=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/RecoveredUI/JackpotPopupArt/ef_slyinhang.prefab"),root.transform);
        var rect=(RectTransform)icon.transform;rect.anchorMin=old.anchorMin;rect.anchorMax=old.anchorMax;rect.pivot=old.pivot;rect.sizeDelta=old.sizeDelta;rect.anchoredPosition=old.anchoredPosition;rect.localScale=old.localScale;rect.SetSiblingIndex(old.GetSiblingIndex());icon.layer=old.gameObject.layer;icon.name=old.name;Object.DestroyImmediate(old.gameObject);
        var button=root.transform.Find("bankBtn").GetComponent<Button>();var progress=root.transform.Find("Progress");
        // Keep original positions, but nest visuals in the standard Button per project UI rules.
        icon.transform.SetParent(button.transform,true);progress.SetParent(button.transform,true);
        var view=root.AddComponent<RecoveredBankProgress>();var settings=new SerializedObject(view);
        settings.FindProperty("button").objectReferenceValue=button;settings.FindProperty("fill").objectReferenceValue=progress.Find("fill").GetComponent<Image>();settings.FindProperty("label").objectReferenceValue=progress.Find("Text (TMP)").GetComponent<TMP_Text>();
        settings.FindProperty("countFormat").stringValue="{0}/{1}";settings.FindProperty("tip").stringValue="Need more spins to trigger the rewards.";settings.ApplyModifiedPropertiesWithoutUndo();
        settings=new SerializedObject(field.GetComponent<RecoveredSpinPlayfield>());settings.FindProperty("bankProgress").objectReferenceValue=view;settings.ApplyModifiedPropertiesWithoutUndo();
    }
}
