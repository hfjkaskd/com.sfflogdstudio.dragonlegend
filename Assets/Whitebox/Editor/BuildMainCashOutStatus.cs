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

public static class BuildMainCashOutStatus
{
    public static void Save()
    {
        const string output="Assets/Resources/RecoveredUI/MainCashOutStatus.prefab",temporary="Assets/Whitebox/Editor/MainCashOutStatusSource.prefab";
        var blocks=new Dictionary<string,string>();
        string source=File.ReadAllText("ReferenceOriginal/Res/ViewPrefabs/UIMainView.prefab").Replace("\r","");
        foreach(Match m in Regex.Matches(source,@"^--- !u!\d+ &(\d+)\n.*?(?=^--- !u!|\z)",RegexOptions.Multiline|RegexOptions.Singleline))blocks.Add(m.Groups[1].Value,m.Value);
        const string rootId="224901155395665742";
        blocks[rootId]=Regex.Replace(blocks[rootId],@"  m_Children:\n.*?  m_Father:","  m_Children:\n  - {fileID: 224440584996359664}\n  m_Father:",RegexOptions.Singleline);
        var text=new StringBuilder("%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n");BuildTreasureCard.Append(rootId,rootId,blocks,text);
        var map=new Dictionary<string,string>();BuildTreasureCard.Script<Image>(map,"3cf5a44414476512e00c3e7a2569a919");BuildTreasureCard.Script<TextMeshProUGUI>(map,"3f96b1d166d19b209697e35b35d65c76");
        BuildTreasureCard.Script<RecoveredScreenAdapt>(map,"3991d2bd099e12203576b2cf89b113da");
        BuildTreasureCard.Map(map,"17c564eadab8a3940aac81ae943b3097","Assets/Resources/RecoveredArt/Res/UI/zhujiemian/zjm_s9g_spin_bg.png");
        string yaml=text.ToString();foreach(var pair in map)yaml=yaml.Replace(pair.Key,pair.Value);
        string spriteGuid=map["17c564eadab8a3940aac81ae943b3097"];
        yaml=yaml.Replace("guid: "+spriteGuid+", type: 2","guid: "+spriteGuid+", type: 3");
        foreach(Match m in Regex.Matches(yaml,@"guid: (\w+)"))if(string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(m.Groups[1].Value)))throw new InvalidDataException("Unmapped Main status "+m.Groups[1].Value);
        GameObject root=null;
        try{
            File.WriteAllText(temporary,yaml);AssetDatabase.ImportAsset(temporary);root=PrefabUtility.LoadPrefabContents(temporary);root.name="MainCashOutStatus";
            var settings=new SerializedObject(root.AddComponent<RecoveredMainCashOutStatus>());var panel=root.transform.Find("CashOutTip");
            settings.FindProperty("panel").objectReferenceValue=panel;settings.FindProperty("label").objectReferenceValue=panel.GetComponentInChildren<TMP_Text>();
            settings.FindProperty("readyText").stringValue="You Can Cash Out Now!";
            settings.FindProperty("remainingFormat").stringValue="Earn <material=\"#003815_3\"><gradient=\"cash\">{0}</gradient></material> To Withdraw <material=\"#003815_3\"><gradient=\"cash\">{1}</gradient></material>";
            settings.FindProperty("delay").floatValue=12;settings.FindProperty("scaleDuration").floatValue=.5f;settings.FindProperty("holdDuration").floatValue=5;
            settings.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(root,output);
        }finally{if(root!=null)PrefabUtility.UnloadPrefabContents(root);AssetDatabase.DeleteAsset(temporary);}
        const string core="Assets/Resources/RecoveredUI/CoreRoundFlow.prefab";root=PrefabUtility.LoadPrefabContents(core);
        try{
            var settings=new SerializedObject(root.GetComponent<RecoveredCoreRoundFlow>());var old=settings.FindProperty("cashOutStatus").objectReferenceValue as Component;
            if(old!=null)Object.DestroyImmediate(old.gameObject);
            settings.FindProperty("cashOutStatus").objectReferenceValue=PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<RecoveredMainCashOutStatus>(output),root.transform);
            settings.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(root,core);AssetDatabase.SaveAssets();
        }finally{PrefabUtility.UnloadPrefabContents(root);}
    }
}
