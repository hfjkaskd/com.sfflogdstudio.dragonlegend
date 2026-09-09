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

public static class BuildTopWithdrawEntry
{
    public static void Save()
    {
        const string temporary="Assets/Whitebox/Editor/TopWithdrawSource.prefab";
        const string balancePath="Assets/Resources/RecoveredUI/BalancePanel.prefab";
        var source=File.ReadAllText("ReferenceOriginal/Res/ViewPrefabs/UIMainView.prefab").Replace("\r", "");
        var blocks=new Dictionary<string,string>();
        foreach(Match match in Regex.Matches(source,@"^--- !u!\d+ &(\d+)\n.*?(?=^--- !u!|\z)",RegexOptions.Multiline|RegexOptions.Singleline))
            blocks.Add(match.Groups[1].Value,match.Value);
        var text=new StringBuilder("%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n");
        BuildTreasureCard.Append("224293198502218400","224293198502218400",blocks,text);
        var map=new Dictionary<string,string>();
        BuildTreasureCard.Script<Image>(map,"3cf5a44414476512e00c3e7a2569a919");
        BuildTreasureCard.Script<Image>(map,"e0b4d57e8f658b407a2ad25df85fb0d6");
        BuildTreasureCard.Script<Button>(map,"18d0a90695249463551c00f45766e642");
        BuildTreasureCard.Script<TextMeshProUGUI>(map,"3f96b1d166d19b209697e35b35d65c76");
        string yaml=Regex.Replace(text.ToString(),@"--- !u!114 &114132299442727904\n.*?(?=\n--- !u!|\z)","",RegexOptions.Singleline);
        yaml=yaml.Replace("  - component: {fileID: 114132299442727904}\n","");
        yaml=Regex.Replace(yaml,@"--- !u!114 &114112815651697755\n.*?(?=\n--- !u!|\z)",
            m=>m.Value.Replace("m_Color: {r: 1, g: 1, b: 1, a: 1}","m_Color: {r: 1, g: 1, b: 1, a: 0}"),RegexOptions.Singleline);
        foreach(var pair in map)yaml=yaml.Replace(pair.Key,pair.Value);
        yaml=yaml.Replace("guid: 97bc2aee59c95cc41a67970734e7dcc1, type: 2",
            "guid: "+AssetDatabase.AssetPathToGUID("Assets/Resources/RecoveredArt/Res/UI/zhujiemian/zjm_tx_01.png")+", type: 3");
        foreach(Match match in Regex.Matches(yaml,@"guid: (\w+)"))
            if(string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(match.Groups[1].Value)))throw new InvalidDataException("Unmapped top withdrawal asset "+match.Groups[1].Value);
        GameObject balance=null;
        try {
            File.WriteAllText(temporary,yaml);AssetDatabase.ImportAsset(temporary,ImportAssetOptions.ForceSynchronousImport);
            balance=PrefabUtility.LoadPrefabContents(balancePath);
            var previous=balance.transform.Find("CashOut");if(previous!=null)Object.DestroyImmediate(previous.gameObject);
            var entry=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(temporary),balance.transform,false);
            entry.name="CashOut";entry.transform.SetSiblingIndex(2);
            var old=(RectTransform)entry.transform.Find("SkeletonGraphic (ef_sltxan)");
            var art=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/RecoveredUI/JackpotPopupArt/ef_sltxan.prefab"),entry.transform);
            var rect=(RectTransform)art.transform;
            rect.anchorMin=old.anchorMin;rect.anchorMax=old.anchorMax;rect.pivot=old.pivot;rect.sizeDelta=old.sizeDelta;
            rect.anchoredPosition3D=old.anchoredPosition3D;rect.localScale=old.localScale;rect.localRotation=old.localRotation;
            rect.SetSiblingIndex(old.GetSiblingIndex());art.name=old.name;Object.DestroyImmediate(old.gameObject);
            var button=entry.transform.Find("WithdrawBtn").GetComponent<Button>();
            // Author visuals beneath the standard Button, preserving source world geometry.
            // Runtime only binds its callback; it does not create or rearrange the visuals.
            var visuals=new List<Transform>();
            foreach(Transform child in entry.transform)if(child!=button.transform)visuals.Add(child);
            foreach(var child in visuals)child.SetParent(button.transform,true);
            foreach(var graphic in button.GetComponentsInChildren<Graphic>())graphic.raycastTarget=graphic==button.targetGraphic;
            var settings=new SerializedObject(balance.GetComponent<RecoveredBalancePanel>());
            settings.FindProperty("withdrawButton").objectReferenceValue=button;settings.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(balance,balancePath);
        } finally {
            if(balance!=null)PrefabUtility.UnloadPrefabContents(balance);
            AssetDatabase.DeleteAsset(temporary);
        }
        const string gamePath="Assets/Resources/Whitebox/GameEntry.prefab";
        var game=PrefabUtility.LoadPrefabContents(gamePath);
        try {
            var toggle=(RectTransform)game.transform.Find("GmToggle");
            toggle.anchorMin=toggle.anchorMax=toggle.pivot=new Vector2(0,1);
            toggle.anchoredPosition=new Vector2(8,-125);
            PrefabUtility.SaveAsPrefabAsset(game,gamePath);AssetDatabase.SaveAssets();
        } finally {PrefabUtility.UnloadPrefabContents(game);}
    }
}
