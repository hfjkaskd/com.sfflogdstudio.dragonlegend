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

public static class BuildMainCashOutEntry
{
    private const string Output="Assets/Resources/RecoveredUI/CashOutEntry.prefab";
    public static void Save()
    {
        const string temporary="Assets/Whitebox/Editor/CashOutEntrySource.prefab";
        string source=File.ReadAllText("ReferenceOriginal/Res/ViewPrefabs/UIMainView.prefab").Replace("\r","");
        var blocks=new Dictionary<string,string>();
        foreach(Match m in Regex.Matches(source,@"^--- !u!\d+ &(\d+)\n.*?(?=^--- !u!|\z)",RegexOptions.Multiline|RegexOptions.Singleline))blocks.Add(m.Groups[1].Value,m.Value);
        const string rootId="224587083680832752";
        blocks[rootId]=Regex.Replace(blocks[rootId],@"  m_Children:\n.*?  m_Father:","  m_Children:\n  - {fileID: 224689876809886907}\n  m_Father:",RegexOptions.Singleline);
        var text=new StringBuilder("%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n");BuildTreasureCard.Append(rootId,rootId,blocks,text);
        var map=new Dictionary<string,string>();
        BuildTreasureCard.Script<Image>(map,"e0b4d57e8f658b407a2ad25df85fb0d6");
        BuildTreasureCard.Script<Button>(map,"18d0a90695249463551c00f45766e642");
        BuildTreasureCard.Script<TextMeshProUGUI>(map,"3f96b1d166d19b209697e35b35d65c76");
        var removed=new List<string>();
        string yaml=Regex.Replace(text.ToString(),@"--- !u!114 &(\d+)\n.*?(?=\n--- !u!|\z)",m=>{
            if(m.Value.Contains("ff326197253ae4cec0b46632391386ec")){removed.Add(m.Groups[1].Value);return "";}
            return m.Value.Contains("e0b4d57e8f658b407a2ad25df85fb0d6")?m.Value.Replace("m_Color: {r: 1, g: 1, b: 1, a: 1}","m_Color: {r: 1, g: 1, b: 1, a: 0}"):m.Value;
        },RegexOptions.Singleline);
        foreach(string id in removed)yaml=yaml.Replace("  - component: {fileID: "+id+"}\n","");
        foreach(var pair in map)yaml=yaml.Replace(pair.Key,pair.Value);
        foreach(Match m in Regex.Matches(yaml,@"guid: (\w+)"))if(string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(m.Groups[1].Value)))throw new InvalidDataException("Unmapped CashOut entry "+m.Groups[1].Value);
        GameObject root=null;
        try{
            File.WriteAllText(temporary,yaml);AssetDatabase.ImportAsset(temporary,ImportAssetOptions.ForceSynchronousImport);
            root=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(temporary));root.name="CashOutEntry";
            var button=root.transform.Find("CashOutb");var old=(RectTransform)button.Find("SkeletonGraphic (ef_tixianicon)");
            var art=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/RecoveredUI/JackpotPopupArt/ef_tixianicon.prefab"),button);
            var rect=(RectTransform)art.transform;rect.anchorMin=old.anchorMin;rect.anchorMax=old.anchorMax;rect.pivot=old.pivot;rect.sizeDelta=old.sizeDelta;rect.anchoredPosition3D=old.anchoredPosition3D;rect.localRotation=old.localRotation;rect.localScale=old.localScale;
            rect.SetSiblingIndex(old.GetSiblingIndex());art.layer=old.gameObject.layer;art.name=old.name;Object.DestroyImmediate(old.gameObject);
            var settings=new SerializedObject(root.AddComponent<RecoveredCashOutEntry>());
            settings.FindProperty("button").objectReferenceValue=button.GetComponent<Button>();
            settings.FindProperty("fingerPrefab").objectReferenceValue=AssetDatabase.LoadAssetAtPath<RectTransform>("Assets/Resources/RecoveredUI/Finger.prefab");
            settings.FindProperty("fingerTarget").objectReferenceValue=button.Find("finger");settings.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root,Output);
        }finally{if(root!=null)Object.DestroyImmediate(root);AssetDatabase.DeleteAsset(temporary);}
        const string corePath="Assets/Resources/RecoveredUI/CoreRoundFlow.prefab";
        root=PrefabUtility.LoadPrefabContents(corePath);
        try{
            var settings=new SerializedObject(root.GetComponent<RecoveredCoreRoundFlow>());
            var previous=settings.FindProperty("cashOutEntry").objectReferenceValue as Component;if(previous!=null)Object.DestroyImmediate(previous.gameObject);
            settings.FindProperty("cashOutEntry").objectReferenceValue=PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<RecoveredCashOutEntry>(Output),root.transform);
            settings.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(root,corePath);AssetDatabase.SaveAssets();
        }finally{PrefabUtility.UnloadPrefabContents(root);}
    }
}
