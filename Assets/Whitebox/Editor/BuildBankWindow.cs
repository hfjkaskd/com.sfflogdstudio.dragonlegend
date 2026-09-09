using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using DragonLegend.Whitebox;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public static class BuildBankWindow
{
    public static void Save()
    {
        var map=new Dictionary<string,string>();
        BuildTreasureCard.Script<Image>(map,"3cf5a44414476512e00c3e7a2569a919");
        BuildTreasureCard.Script<Button>(map,"18d0a90695249463551c00f45766e642");
        BuildTreasureCard.Script<TextMeshProUGUI>(map,"3f96b1d166d19b209697e35b35d65c76");
        BuildTreasureCard.Script<Text>(map,"04f84fc2003509a5e7e068ec1271cc40");
        BuildTreasureCard.Script<GraphicRaycaster>(map,"86fe8f3fc59dc06ea6b45a1bbee64682");
        string source=File.ReadAllText("ReferenceOriginal/Res/ViewPrefabs/UIBankView.prefab").Replace("\r","");
        var removed=new List<string>();
        source=Regex.Replace(source,@"--- !u!\d+ &(\d+)\n.*?(?=\n--- !u!|\z)",m=>{
            var script=Regex.Match(m.Value,@"m_Script:.*guid: (\w+)");
            if(script.Success&&!map.ContainsKey(script.Groups[1].Value)){removed.Add(m.Groups[1].Value);return "";}return m.Value;
        },RegexOptions.Singleline);
        foreach(var id in removed)source=source.Replace("  - component: {fileID: "+id+"}\n","");
        foreach(var pair in new Dictionary<string,string>{
            {"1d47a370dfffde042a3a22d6d2b3d893","bank/hou_yun"},{"21699d62cfe5c324fafeb964d40995c5","bank/b_luckybonus_txt"},
            {"deeb1147e3346884d8b26004fefaef1d","bank/qian_yun"},{"9c3c1db2782e5f4428207bf073977e7a","bank/b_btn_bofan"},
            {"34837b9c28c8b2a4f86a768fd659213d","pop-up/ty_hb_meijing"},{"f4b25a1c65682f9409a101f75b641504","pop-up/tc_btn_01"}})
        {BuildTreasureCard.Map(map,pair.Key,"Assets/Resources/RecoveredArt/Res/UI/"+pair.Value+".png");source=source.Replace("guid: "+pair.Key+", type: 2","guid: "+pair.Key+", type: 3");}
        BuildTreasureCard.Map(map,"36977c4faccb97c4ebe0b4fdeea88b25","Assets/Resources/RecoveredUI/CoinRewardText/Green.asset");
        BuildTreasureCard.Map(map,"39211f061913f054f84255431c8dce45","Assets/Resources/RecoveredUI/BonusRewardPopup/#0A5902_4.mat");
        BuildTreasureCard.Map(map,"71fce5925b6d578448b06d587acf8e25","Assets/Resources/Fonts & Materials/#003815_3.mat");
        BuildTreasureCard.Map(map,"183b0e4b7b3c5a34fa991aebe104ca31","Assets/Resources/RecoveredUI/BonusRewardPopup/tc_btn_bofang.asset");
        BuildTreasureCard.Map(map,"b02dda94bf0cfd148b568608d58490f9","Assets/Resources/RecoveredUI/BonusRewardPopup/btnanim.anim");
        foreach(var pair in map)source=source.Replace(pair.Key,pair.Value);
        foreach(Match m in Regex.Matches(source,@"guid: (\w+)"))if(string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(m.Groups[1].Value)))throw new InvalidDataException("Unmapped Bank GUID "+m.Groups[1].Value);
        const string temp="Assets/Whitebox/Editor/BankWindowSource.prefab";
        File.WriteAllText(temp,source);AssetDatabase.ImportAsset(temp,ImportAssetOptions.ForceSynchronousImport);
        var root=PrefabUtility.LoadPrefabContents(temp);
        try
        {
            var content=root.transform.Find("Content");
            Replace((RectTransform)content.Find("Image"),AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/RecoveredUI/JackpotPopupArt/ef_long.prefab"));
            var items=new RecoveredBankItem[3];var floats=new RecoveredBankFloat[3];
            string[] names={"BankItem (1)","BankItem (2)","BankItem"};
            for(int i=0;i<3;i++){
                var item=Replace((RectTransform)content.Find(names[i]),AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/RecoveredUI/BankItem.prefab"));
                items[i]=item.GetComponent<RecoveredBankItem>();floats[i]=item.GetComponent<RecoveredBankFloat>();
            }
            var group=content.gameObject.AddComponent<RecoveredJackpotMeters>();var groupSettings=new SerializedObject(group);var array=groupSettings.FindProperty("meters");array.arraySize=3;
            var originalMeters=AssetDatabase.LoadAssetAtPath<RecoveredJackpotMeters>("Assets/Resources/RecoveredUI/JackpotMeters.prefab");
            names=new[]{"Grand","Major","Minior"};
            for(int i=0;i<3;i++){
                var node=content.Find(names[i]);var icon=Replace((RectTransform)node.Find("Spine"),originalMeters.At(i).Icon.gameObject).GetComponent<RecoveredJackpotIcon>();
                var meter=node.gameObject.AddComponent<RecoveredJackpotMeter>();var settings=new SerializedObject(meter);
                settings.FindProperty("icon").objectReferenceValue=icon;settings.FindProperty("rewardText").objectReferenceValue=node.Find("GreenTxt").GetComponent<TMP_Text>();
                settings.FindProperty("greenRewardText").objectReferenceValue=node.Find("Text (Legacy)").GetComponent<Text>();settings.FindProperty("rewardDuration").floatValue=.3f;settings.ApplyModifiedPropertiesWithoutUndo();array.GetArrayElementAtIndex(i).objectReferenceValue=meter;
            }
            groupSettings.ApplyModifiedPropertiesWithoutUndo();
            var shine=new SerializedObject(content.Find("Title").gameObject.AddComponent<RecoveredTitleShine>());
            shine.FindProperty("effectFactor").floatValue=.75020033f;
            shine.FindProperty("template").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/RecoveredUI/BonusRewardPopup/TitleShine.mat");shine.ApplyModifiedPropertiesWithoutUndo();
            var view=root.AddComponent<RecoveredBankWindow>();var data=new SerializedObject(view);
            data.FindProperty("content").objectReferenceValue=content;data.FindProperty("buttons").objectReferenceValue=content.Find("Btn");
            data.FindProperty("openButton").objectReferenceValue=content.Find("Btn/OpenBtn").GetComponent<Button>();data.FindProperty("leaveButton").objectReferenceValue=content.Find("Btn/UnPlayBtn").GetComponent<Button>();
            data.FindProperty("jackpots").objectReferenceValue=group;
            var itemArray=data.FindProperty("items");var floatArray=data.FindProperty("floats");itemArray.arraySize=floatArray.arraySize=3;
            for(int i=0;i<3;i++){itemArray.GetArrayElementAtIndex(i).objectReferenceValue=items[i];floatArray.GetArrayElementAtIndex(i).objectReferenceValue=floats[i];}
            data.FindProperty("fingerPrefab").objectReferenceValue=AssetDatabase.LoadAssetAtPath<RectTransform>("Assets/Resources/RecoveredUI/Finger.prefab");
            data.FindProperty("duration").floatValue=.3f;data.FindProperty("floatHeight").vector2Value=new Vector2(20,30);data.FindProperty("floatDuration").vector2Value=new Vector2(1,2);data.FindProperty("showSound").stringValue="jump";
            data.FindProperty("enterEase").animationCurveValue=new AnimationCurve(new Keyframe(0,0,4.70158f,4.70158f),new Keyframe(1,1,0,0));
            data.FindProperty("exitEase").animationCurveValue=new AnimationCurve(new Keyframe(0,0,0,0),new Keyframe(1,1,4.70158f,4.70158f));
            data.FindProperty("buttonEase").animationCurveValue=new AnimationCurve(new Keyframe(0,0,2,2),new Keyframe(1,1,0,0));data.ApplyModifiedPropertiesWithoutUndo();
            root.GetComponent<Canvas>().sortingOrder=300;
            var bg=new GameObject("_WindowBg",typeof(RectTransform),typeof(Image),typeof(Button));bg.layer=root.layer;bg.transform.SetParent(root.transform,false);bg.transform.SetAsFirstSibling();
            var rect=(RectTransform)bg.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            var image=bg.GetComponent<Image>();image.color=new Color(0,0,0,.65f);var button=bg.GetComponent<Button>();button.targetGraphic=image;button.transition=Selectable.Transition.None;
            root.SetActive(false);PrefabUtility.SaveAsPrefabAsset(root,"Assets/Resources/RecoveredUI/BankWindow.prefab");AssetDatabase.SaveAssets();
        }
        finally{PrefabUtility.UnloadPrefabContents(root);AssetDatabase.DeleteAsset(temp);}
        BuildCoreRoundFlow.Save();
    }
    private static GameObject Replace(RectTransform old,GameObject template)
    {
        var obj=Object.Instantiate(template,old.parent,false);var rect=(RectTransform)obj.transform;
        rect.anchorMin=old.anchorMin;rect.anchorMax=old.anchorMax;rect.pivot=old.pivot;rect.sizeDelta=old.sizeDelta;rect.anchoredPosition=old.anchoredPosition;rect.localScale=old.localScale;rect.localRotation=old.localRotation;
        rect.SetSiblingIndex(old.GetSiblingIndex());obj.name=old.name;obj.layer=old.gameObject.layer;Object.DestroyImmediate(old.gameObject);return obj;
    }
}
