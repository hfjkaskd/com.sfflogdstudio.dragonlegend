using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using DragonLegend.Whitebox;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class BuildReviewWindow
{
    public static void Save()
    {
        var map=new Dictionary<string,string>();
        BuildTreasureCard.Script<Image>(map,"3cf5a44414476512e00c3e7a2569a919");BuildTreasureCard.Script<Button>(map,"18d0a90695249463551c00f45766e642");
        BuildTreasureCard.Script<TextMeshProUGUI>(map,"3f96b1d166d19b209697e35b35d65c76");BuildTreasureCard.Script<GraphicRaycaster>(map,"86fe8f3fc59dc06ea6b45a1bbee64682");
        BuildTreasureCard.Script<HorizontalLayoutGroup>(map,"19fbb4a32b59286cd89b624f52ff7943");BuildTreasureCard.Script<LayoutElement>(map,"4293fd42559bc8bb1c81715179adf536");
        string source=File.ReadAllText("ReferenceOriginal/Res/ViewPrefabs/UIReViewUsView.prefab").Replace("\r","");var removed=new List<string>();
        source=Regex.Replace(source,@"--- !u!\d+ &(\d+)\n.*?(?=\n--- !u!|\z)",m=>{var script=Regex.Match(m.Value,@"m_Script:.*guid: (\w+)");if(script.Success&&!map.ContainsKey(script.Groups[1].Value)){removed.Add(m.Groups[1].Value);return "";}return m.Value;},RegexOptions.Singleline);
        foreach(var id in removed)source=source.Replace("  - component: {fileID: "+id+"}\n","");
        foreach(var pair in new Dictionary<string,string>{{"01db24e7c1f408f4ba0ffe9666e1844f","hp_reviewus_txt"},{"3008572a0bbfdf44faed15b37312eb4e","ty_btn_cha"},{"5bdb7896f8867ce4e91fcac492fe353d","hp_xin_02"},{"a9a1003f8be4205429a60216cadc9812","ty_bg02"},{"f4b25a1c65682f9409a101f75b641504","tc_btn_01"},{"fefb5b7700ed89047b0aa457f2a72c0f","hp_xin_01"}})
        {BuildTreasureCard.Map(map,pair.Key,"Assets/Resources/RecoveredArt/Res/UI/pop-up/"+pair.Value+".png");source=source.Replace("guid: "+pair.Key+", type: 2","guid: "+pair.Key+", type: 3");}
        BuildTreasureCard.Map(map,"39211f061913f054f84255431c8dce45","Assets/Resources/RecoveredUI/BonusRewardPopup/#0A5902_4.mat");BuildTreasureCard.Map(map,"183b0e4b7b3c5a34fa991aebe104ca31","Assets/Resources/RecoveredUI/BonusRewardPopup/tc_btn_bofang.asset");
        foreach(var pair in map)source=source.Replace(pair.Key,pair.Value);
        foreach(Match m in Regex.Matches(source,@"guid: (\w+)"))if(string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(m.Groups[1].Value)))throw new InvalidDataException("Missing Review GUID "+m.Groups[1].Value);
        const string temp="Assets/Whitebox/Editor/ReviewWindowSource.prefab";File.WriteAllText(temp,source);AssetDatabase.ImportAsset(temp,ImportAssetOptions.ForceSynchronousImport);
        var root=PrefabUtility.LoadPrefabContents(temp);
        try
        {
            var content=root.transform.Find("Content");var view=root.AddComponent<RecoveredReviewWindow>();var data=new SerializedObject(view);
            data.FindProperty("content").objectReferenceValue=content;data.FindProperty("claim").objectReferenceValue=content.Find("Btn/ClaimBtn").GetComponent<Button>();data.FindProperty("close").objectReferenceValue=content.Find("CloseBtn").GetComponent<Button>();
            var stars=data.FindProperty("stars");var highlights=data.FindProperty("highlights");stars.arraySize=highlights.arraySize=5;
            for(int i=0;i<5;i++){var star=content.Find("Layout").GetChild(i);stars.GetArrayElementAtIndex(i).objectReferenceValue=star.GetComponent<Button>();highlights.GetArrayElementAtIndex(i).objectReferenceValue=star.Find("S").gameObject;}
            data.FindProperty("storePrefix").stringValue="market://details?id=";data.FindProperty("showSound").stringValue="remind";data.FindProperty("duration").floatValue=.3f;
            data.FindProperty("enterEase").animationCurveValue=new AnimationCurve(new Keyframe(0,0,4.70158f,4.70158f),new Keyframe(1,1,0,0));data.FindProperty("exitEase").animationCurveValue=new AnimationCurve(new Keyframe(0,0,0,0),new Keyframe(1,1,4.70158f,4.70158f));data.ApplyModifiedPropertiesWithoutUndo();
            var shine=new SerializedObject(content.Find("Title").gameObject.AddComponent<RecoveredTitleShine>());shine.FindProperty("template").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/RecoveredUI/BonusRewardPopup/TitleShine.mat");shine.ApplyModifiedPropertiesWithoutUndo();
            root.GetComponent<Canvas>().sortingOrder=300;
            var bg=new GameObject("_WindowBg",typeof(RectTransform),typeof(Image),typeof(Button));bg.layer=root.layer;bg.transform.SetParent(root.transform,false);bg.transform.SetAsFirstSibling();
            var rect=(RectTransform)bg.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            var image=bg.GetComponent<Image>();image.color=new Color(0,0,0,.65f);var button=bg.GetComponent<Button>();button.targetGraphic=image;button.transition=Selectable.Transition.None;
            root.SetActive(false);PrefabUtility.SaveAsPrefabAsset(root,"Assets/Resources/RecoveredUI/ReviewWindow.prefab");AssetDatabase.SaveAssets();
        }
        finally{PrefabUtility.UnloadPrefabContents(root);AssetDatabase.DeleteAsset(temp);}
        BuildCoreRoundFlow.Save();
    }
}
