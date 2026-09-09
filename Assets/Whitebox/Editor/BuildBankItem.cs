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

public static class BuildBankItem
{
    public static void Save()
    {
        BuildJackpotPopupArt.Create("ef_longzhu","棋子/longzhu",Vector2.zero,new Vector2(.5f,.5f),"Tools/Evidence/FreeSymbols/");
        var map=new Dictionary<string,string>();BuildTreasureCard.Script<Image>(map,"3cf5a44414476512e00c3e7a2569a919");
        BuildTreasureCard.Script<TextMeshProUGUI>(map,"3f96b1d166d19b209697e35b35d65c76");BuildTreasureCard.Script<Button>(map,"18d0a90695249463551c00f45766e642");
        string source=File.ReadAllText("ReferenceOriginal/Res/ViewPrefabs/UIBankView.prefab").Replace("\r", "");var blocks=new Dictionary<string,string>();
        foreach(Match m in Regex.Matches(source,@"^--- !u!\d+ &(\d+)\n.*?(?=^--- !u!|\z)",RegexOptions.Multiline|RegexOptions.Singleline))blocks.Add(m.Groups[1].Value,m.Value);
        var yaml=new StringBuilder("%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n");BuildTreasureCard.Append("224499726237547504","224499726237547504",blocks,yaml);
        string text=yaml.ToString();var removed=new List<string>();
        text=Regex.Replace(text,@"--- !u!\d+ &(\d+)\n.*?(?=\n--- !u!|\z)",m=>{var script=Regex.Match(m.Value,@"m_Script:.*guid: (\w+)");if(script.Success&&!map.ContainsKey(script.Groups[1].Value)){removed.Add(m.Groups[1].Value);return "";}return m.Value;},RegexOptions.Singleline);
        foreach(var id in removed)text=text.Replace("  - component: {fileID: "+id+"}\n", "");
        foreach(var pair in new Dictionary<string,string>{{"9c3c1db2782e5f4428207bf073977e7a","bank/b_btn_bofan"},{"34837b9c28c8b2a4f86a768fd659213d","pop-up/ty_hb_meijing"}}){BuildTreasureCard.Map(map,pair.Key,"Assets/Resources/RecoveredArt/Res/UI/"+pair.Value+".png");text=text.Replace("guid: "+pair.Key+", type: 2","guid: "+pair.Key+", type: 3");}
        BuildTreasureCard.Map(map,"71fce5925b6d578448b06d587acf8e25","Assets/Resources/Fonts & Materials/#003815_3.mat");
        foreach(var pair in map)text=text.Replace(pair.Key,pair.Value);
        foreach(Match m in Regex.Matches(text,@"guid: (\w+)"))if(string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(m.Groups[1].Value)))throw new InvalidDataException("Missing BankItem GUID "+m.Groups[1].Value);
        const string temp="Assets/Whitebox/Editor/BankItemSource.prefab";File.WriteAllText(temp,text);AssetDatabase.ImportAsset(temp,ImportAssetOptions.ForceSynchronousImport);
        var root=PrefabUtility.LoadPrefabContents(temp);
        try
        {
            var ball=Replace((RectTransform)root.transform.Find("Longzhu"),"ef_longzhu");
            var reward=root.transform.Find("Reward");Replace((RectTransform)reward.Find("SkeletonGraphic (ef_shoucanggl)"),"ef_shoucanggl");
            root.AddComponent<RecoveredBankFloat>();
            var view=root.AddComponent<RecoveredBankItem>();var settings=new SerializedObject(view);
            settings.FindProperty("ball").objectReferenceValue=ball;
            settings.FindProperty("reward").objectReferenceValue=reward;
            settings.FindProperty("label").objectReferenceValue=reward.Find("GreenTxt").GetComponent<TMP_Text>();
            settings.FindProperty("ad").objectReferenceValue=root.transform.Find("AdImg").GetComponent<Image>();
            settings.FindProperty("button").objectReferenceValue=root.GetComponent<Button>();
            var idle=settings.FindProperty("idleClips");var fire=settings.FindProperty("fireClips");idle.arraySize=fire.arraySize=3;
            string[] suffix={"zi","lan","lv"};
            var poseSettings=new SerializedObject(ball);var poses=poseSettings.FindProperty("poses");
            for(int i=0;i<3;i++)for(int j=0;j<poses.arraySize;j++){
                string name=poses.GetArrayElementAtIndex(j).FindPropertyRelative("clip").stringValue;
                if(name=="idle_"+suffix[i])idle.GetArrayElementAtIndex(i).intValue=j;
                if(name=="huo_"+suffix[i])fire.GetArrayElementAtIndex(i).intValue=j;
            }
            settings.FindProperty("revealDelay").floatValue=.5f;settings.FindProperty("rewardDelay").floatValue=.5f;settings.FindProperty("rewardScale").floatValue=.6f;
            settings.FindProperty("rewardEase").animationCurveValue=new AnimationCurve(new Keyframe(0,0,0,0),new Keyframe(1,1,2,2));settings.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root,"Assets/Resources/RecoveredUI/BankItem.prefab");AssetDatabase.SaveAssets();
        }
        finally {PrefabUtility.UnloadPrefabContents(root);AssetDatabase.DeleteAsset(temp);}
    }
    private static RecoveredRegionAnimator Replace(RectTransform old,string name)
    {
        var obj=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/RecoveredUI/JackpotPopupArt/"+name+".prefab"),old.parent);
        var rect=(RectTransform)obj.transform;rect.anchorMin=old.anchorMin;rect.anchorMax=old.anchorMax;rect.pivot=old.pivot;rect.sizeDelta=old.sizeDelta;rect.anchoredPosition=old.anchoredPosition;rect.localScale=old.localScale;
        rect.SetSiblingIndex(old.GetSiblingIndex());obj.layer=old.gameObject.layer;obj.name=old.name;Object.DestroyImmediate(old.gameObject);return obj.GetComponent<RecoveredRegionAnimator>();
    }
}
