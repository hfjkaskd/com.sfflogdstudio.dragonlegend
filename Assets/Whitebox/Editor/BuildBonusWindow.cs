using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using DragonLegend.Whitebox;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public static class BuildBonusWindow
{
    public static void SaveFinger()
    {
        const string path="Assets/Resources/RecoveredUI/BonusWindow.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        try {
            var settings=new SerializedObject(root.GetComponent<RecoveredBonusWindow>());
            settings.FindProperty("fingerPrefab").objectReferenceValue=AssetDatabase.LoadAssetAtPath<RectTransform>("Assets/Resources/RecoveredUI/Finger.prefab");
            settings.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(root,path);AssetDatabase.SaveAssets();
        } finally {PrefabUtility.UnloadPrefabContents(root);}
    }
    private const string Temporary="Assets/Whitebox/Editor/BonusSourceImport.prefab";
    public static void Save()
    {
        var map=new Dictionary<string,string>();
        Script<Text>(map,"04f84fc2003509a5e7e068ec1271cc40");Script<Image>(map,"3cf5a44414476512e00c3e7a2569a919");
        Script<Button>(map,"18d0a90695249463551c00f45766e642");Script<TextMeshProUGUI>(map,"3f96b1d166d19b209697e35b35d65c76");
        Script<GraphicRaycaster>(map,"86fe8f3fc59dc06ea6b45a1bbee64682");Script<ContentSizeFitter>(map,"21c7954052da7655d96bff866c2b7662");
        Script<LayoutElement>(map,"4293fd42559bc8bb1c81715179adf536");
        map["36977c4faccb97c4ebe0b4fdeea88b25"]=AssetDatabase.AssetPathToGUID("Assets/Resources/RecoveredUI/CoinRewardText/Green.asset");
        string[] sprites={"3008572a0bbfdf44faed15b37312eb4e|pop-up/ty_btn_cha","5ab6d99eea9534a41ad85ae8377d9734|wanfa_01/fb_bak",
            "5d8c77f51c2f2a9479c8dff6ebd40b98|wanfa_01/jb_01","640f8e49cd3bd0846a7592c13619b669|wanfa_01/deng_bao",
            "9c3c1db2782e5f4428207bf073977e7a|bank/b_btn_bofan","9e9ebd7014f5126499aa01a3716fcea0|wanfa_01/wf_bg",
            "b7f8fa2a2c6e1f64b9f5ba6aeabb3fb0|wanfa_01/zz_zhao","b8b6068d2f3f50c4aa4fca448436567e|wanfa_01/zz_bao",
            "cf1330298040b9f4f8d25405216ef341|wanfa_01/deng_cai","d3fa82caba79647409b7f31ad3629051|wanfa_01/deng_jing",
            "dc40f6cf31f25c34aae0261f9871a367|wanfa_01/deng_zhao","e68b837f6dc086640b8de03b2207c31c|wanfa_01/zz_jing",
            "f00526d078ae79c42a0b95d60c93fa9a|wanfa_01/zz_cai","febd0a469995d5c4aa1fc32439de482c|wanfa_01/jb_bao"};
        foreach(var row in sprites){var pair=row.Split('|');string path="Assets/Resources/RecoveredArt/Res/UI/"+pair[1]+".png";
            if(AssetDatabase.LoadAssetAtPath<Sprite>(path)==null)throw new InvalidDataException(path);map[pair[0]]=AssetDatabase.AssetPathToGUID(path);}
        string source=File.ReadAllText("ReferenceOriginal/Res/ViewPrefabs/UIBonusView.prefab").Replace("\r","");
        var removed=new List<string>();
        source=Regex.Replace(source,@"--- !u!\d+ &(\d+)\n.*?(?=\n--- !u!|\z)",m=>{
            if(Regex.IsMatch(m.Value,@"guid: (301db11ccc54339cbc4e194390420f39|5b873ac4db023d7630dab38ad536f44a|c3dbb9d2854a0fe0b24ee18984913956|da904f91a28860280fb976ac34b27df4|ff326197253ae4cec0b46632391386ec)")){removed.Add(m.Groups[1].Value);return "";}return m.Value;
        },RegexOptions.Singleline);
        foreach(var id in removed)source=Regex.Replace(source,@"  - component: \{fileID: "+id+@"\}\n","");
        foreach(var pair in map)source=source.Replace(pair.Key,pair.Value);
        foreach(var row in sprites){string guid=map[row.Split('|')[0]];source=source.Replace("guid: "+guid+", type: 2","guid: "+guid+", type: 3");}
        foreach(Match match in Regex.Matches(source,@"guid: (\w+)"))if(string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(match.Groups[1].Value)))throw new InvalidDataException("Unmapped Bonus GUID "+match.Groups[1].Value);
        File.WriteAllText(Temporary,source);AssetDatabase.ImportAsset(Temporary,ImportAssetOptions.ForceSynchronousImport);
        var root=PrefabUtility.LoadPrefabContents(Temporary);
        try {
            var content=root.transform.Find("Content");var node=Find(root,"BonusNode");
            var cards=new RecoveredBonusItemTurn[node.childCount];
            for(int i=0;i<cards.Length;i++){
                var old=node.GetChild(i);var replacement=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/RecoveredUI/BonusItem.prefab"),node);
                CopyRect((RectTransform)replacement.transform,(RectTransform)old);replacement.name=old.name;replacement.transform.SetSiblingIndex(i);
                Object.DestroyImmediate(old.gameObject);cards[i]=replacement.GetComponent<RecoveredBonusItemTurn>();
            }
            var meters=new RecoveredJackpotMeter[3];var original=AssetDatabase.LoadAssetAtPath<RecoveredJackpotMeters>("Assets/Resources/RecoveredUI/JackpotMeters.prefab");
            string[] meterNames={"Grand","Major","Minior"};
            for(int i=0;i<3;i++){
                var parent=Find(root,meterNames[i]);var old=parent.Find("Spine");
                var icon=Object.Instantiate(original.At(i).Icon,parent);CopyRect((RectTransform)icon.transform,(RectTransform)old);icon.name=old.name;icon.transform.SetSiblingIndex(old.GetSiblingIndex());Object.DestroyImmediate(old.gameObject);
                var meter=parent.gameObject.AddComponent<RecoveredJackpotMeter>();meters[i]=meter;var ms=new SerializedObject(meter);
                ms.FindProperty("icon").objectReferenceValue=icon;ms.FindProperty("rewardText").objectReferenceValue=parent.GetComponentInChildren<TextMeshProUGUI>(true);
                ms.FindProperty("greenRewardText").objectReferenceValue=parent.GetComponentInChildren<Text>(true);ms.FindProperty("rewardDuration").floatValue=.3f;ms.ApplyModifiedPropertiesWithoutUndo();
            }
            var group=root.AddComponent<RecoveredJackpotMeters>();var gs=new SerializedObject(group);Array(gs.FindProperty("meters"),meters);gs.ApplyModifiedPropertiesWithoutUndo();
            var window=root.AddComponent<RecoveredBonusWindow>();var s=new SerializedObject(window);
            s.FindProperty("fingerPrefab").objectReferenceValue=AssetDatabase.LoadAssetAtPath<RectTransform>("Assets/Resources/RecoveredUI/Finger.prefab");
            s.FindProperty("content").objectReferenceValue=content;s.FindProperty("canvas").objectReferenceValue=root.GetComponent<Canvas>();
            s.FindProperty("meters").objectReferenceValue=group;Array(s.FindProperty("cards"),cards);
            foreach(var pair in new[]{"grandTargets|GrandP","majorTargets|MajorP","minorTargets|MiniorP"}){var p=pair.Split('|');var parent=Find(root,p[1]);var targets=new Transform[parent.childCount];for(int i=0;i<targets.Length;i++)targets[i]=parent.GetChild(i);Array(s.FindProperty(p[0]),targets);}
            foreach(var label in root.GetComponentsInChildren<TMP_Text>(true))if(label.text.Contains("LUCKY DRAW")){s.FindProperty("chances").objectReferenceValue=label;break;}
            s.FindProperty("chanceFormat").stringValue="LUCKY DRAW CHANCES(<gradient=\"spin\">{0}/{1}</gradient>)";
            s.FindProperty("closeButton").objectReferenceValue=Find(root,"CloseBtn").GetComponent<Button>();
            s.FindProperty("characters").objectReferenceValue=Add<RecoveredBonusCharacterRewards>(root.transform,"BonusCharacterRewards");
            s.FindProperty("cash").objectReferenceValue=Add<RecoveredBonusCashRewards>(root.transform,"BonusCashRewards");
            s.FindProperty("rewardPopup").objectReferenceValue=Popup<RecoveredBonusRewardPopup>(root.transform,"BonusRewardPopup");
            s.FindProperty("jackpotPopup").objectReferenceValue=Popup<RecoveredJackpotPopup>(root.transform,"JackpotPopup");
            s.FindProperty("enterDuration").floatValue=.3f;s.FindProperty("hintInterval").floatValue=1.83f;
            s.FindProperty("enterEase").animationCurveValue=new AnimationCurve(new Keyframe(0,0,4.70158f,4.70158f),new Keyframe(1,1,0,0));s.ApplyModifiedPropertiesWithoutUndo();
            s.FindProperty("exitDuration").floatValue=.3f;
            s.FindProperty("exitEase").animationCurveValue=new AnimationCurve(new Keyframe(0,0,0,0),new Keyframe(1,1,4.70158f,4.70158f));s.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root,"Assets/Resources/RecoveredUI/BonusWindow.prefab");AssetDatabase.SaveAssets();
        }finally{PrefabUtility.UnloadPrefabContents(root);AssetDatabase.DeleteAsset(Temporary);}
        var exitRoot=new GameObject("BonusExit");
        try{exitRoot.AddComponent<RecoveredBonusExit>();PrefabUtility.SaveAsPrefabAsset(exitRoot,"Assets/Resources/RecoveredUI/BonusExit.prefab");}
        finally{Object.DestroyImmediate(exitRoot);}
    }
    private static T Add<T>(Transform parent,string name) where T:Component=>((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/RecoveredUI/"+name+".prefab"),parent)).GetComponent<T>();
    private static T Popup<T>(Transform parent,string name) where T:Component
    {
        var popup=Add<T>(parent,name);var canvas=popup.GetComponent<Canvas>();canvas.overrideSorting=true;canvas.sortingOrder=301;popup.gameObject.SetActive(false);return popup;
    }
    private static Transform Find(GameObject root,string name){foreach(var t in root.GetComponentsInChildren<Transform>(true))if(t.name==name)return t;throw new InvalidDataException(name);}
    private static void CopyRect(RectTransform a,RectTransform b){a.anchorMin=b.anchorMin;a.anchorMax=b.anchorMax;a.pivot=b.pivot;a.sizeDelta=b.sizeDelta;a.anchoredPosition3D=b.anchoredPosition3D;a.localScale=b.localScale;a.localRotation=b.localRotation;}
    private static void Array(SerializedProperty field,Object[] values){field.arraySize=values.Length;for(int i=0;i<values.Length;i++)field.GetArrayElementAtIndex(i).objectReferenceValue=values[i];}
    private static void Script<T>(Dictionary<string,string> map,string guid) where T:MonoBehaviour
    {
        var host=new GameObject("Script identity",typeof(RectTransform));try{map[guid]=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(MonoScript.FromMonoBehaviour(host.AddComponent<T>())));}finally{Object.DestroyImmediate(host);}
    }
}
