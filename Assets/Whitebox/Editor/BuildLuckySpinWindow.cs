using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using DragonLegend.Whitebox;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public static class BuildLuckySpinWindow
{
    private const string Temporary="Assets/Whitebox/Editor/LuckySpinSourceImport.prefab";
    public static void Save()
    {
        BuildJackpotPopupArt.Create("ef_slotsspin","slotsspin",new Vector2(1010.0239f,583.99994f),new Vector2(.4678107f,.5f),"Tools/Evidence/");
        var map=new Dictionary<string,string>();
        Script<Text>(map,"04f84fc2003509a5e7e068ec1271cc40");Script<Image>(map,"3cf5a44414476512e00c3e7a2569a919");
        Script<Button>(map,"18d0a90695249463551c00f45766e642");Script<TextMeshProUGUI>(map,"3f96b1d166d19b209697e35b35d65c76");
        Script<GraphicRaycaster>(map,"86fe8f3fc59dc06ea6b45a1bbee64682");Script<RectMask2D>(map,"69dacd7a039c12e90cde90bbae65247a");
        Script<RecoveredLuckySpinColumn>(map,"38862240cc02cff2d334471a5c7e518c");
        map["36977c4faccb97c4ebe0b4fdeea88b25"]=AssetDatabase.AssetPathToGUID("Assets/Resources/RecoveredUI/CoinRewardText/Green.asset");
        map["39211f061913f054f84255431c8dce45"]=AssetDatabase.AssetPathToGUID("Assets/Resources/RecoveredUI/BonusRewardPopup/#0A5902_4.mat");
        map["183b0e4b7b3c5a34fa991aebe104ca31"]=AssetDatabase.AssetPathToGUID("Assets/Resources/RecoveredUI/BonusRewardPopup/tc_btn_bofang.asset");
        map["03ab24337201b0c42a07a2691ebee66a"]=AssetDatabase.AssetPathToGUID("Assets/Resources/RecoveredArt/Res/UI/wanfa_03/sltos_lucktspin_txt.png");
        map["f4b25a1c65682f9409a101f75b641504"]=AssetDatabase.AssetPathToGUID("Assets/Resources/RecoveredArt/Res/UI/pop-up/tc_btn_01.png");
        string source=File.ReadAllText("ReferenceOriginal/Res/ViewPrefabs/UILuckySpinView.prefab").Replace("\r","");
        var removed=new List<string>();
        source=Regex.Replace(source,@"--- !u!\d+ &(\d+)\n.*?(?=\n--- !u!|\z)",m=>{
            if(Regex.IsMatch(m.Value,@"guid: (7c0389ff87115257908c0c471a51e0ff|132b6501dc1746e5b20bc04c0ad3cc98|da904f91a28860280fb976ac34b27df4|ff326197253ae4cec0b46632391386ec)")){removed.Add(m.Groups[1].Value);return "";}return m.Value;
        },RegexOptions.Singleline);
        foreach(var id in removed)source=Regex.Replace(source,@"  - component: \{fileID: "+id+@"\}\n","");
        foreach(var pair in map){if(string.IsNullOrEmpty(pair.Value))throw new InvalidDataException(pair.Key);source=source.Replace(pair.Key,pair.Value);}
        foreach(string id in new[]{"03ab24337201b0c42a07a2691ebee66a","f4b25a1c65682f9409a101f75b641504"})source=source.Replace("guid: "+map[id]+", type: 2","guid: "+map[id]+", type: 3");
        source=source.Replace("_rotNode:","rotNode:").Replace("_digitTexts:","digitTexts:").Replace("_itemHeight:","itemHeight:")
            .Replace("  _visibleRowIndex: 0","  maxSpeed: 10000\n  accelTime: 0.08\n  minSnapTime: 0.05\n  maxSnapTime: 0.3\n  bounceHeight: 35\n  bounceUpTime: 0.12\n  bounceDownTime: 0.08");
        foreach(Match match in Regex.Matches(source,@"guid: (\w+)"))if(string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(match.Groups[1].Value)))throw new InvalidDataException("Unmapped LuckySpin GUID "+match.Groups[1].Value);
        File.WriteAllText(Temporary,source);AssetDatabase.ImportAsset(Temporary,ImportAssetOptions.ForceSynchronousImport);
        var bundle=new GameObject("LuckySpinWindow",typeof(RectTransform),typeof(RecoveredLuckySpinWindow));bundle.layer=5;
        try {
            var bundleRect=(RectTransform)bundle.transform;bundleRect.anchorMin=Vector2.zero;bundleRect.anchorMax=Vector2.one;bundleRect.sizeDelta=Vector2.zero;
            var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Temporary),bundle.transform);
            PrefabUtility.UnpackPrefabInstance(root,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            var content=(RectTransform)root.transform.Find("Content");var old=(RectTransform)content.Find("SkeletonGraphic (ef_slotsspin)");
            var art=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/RecoveredUI/JackpotPopupArt/ef_slotsspin.prefab"),content);
            var rect=(RectTransform)art.transform;rect.anchorMin=old.anchorMin;rect.anchorMax=old.anchorMax;rect.pivot=old.pivot;rect.sizeDelta=old.sizeDelta;
            rect.anchoredPosition3D=old.anchoredPosition3D;rect.localRotation=old.localRotation;rect.localScale=old.localScale;rect.SetSiblingIndex(old.GetSiblingIndex());art.name=old.name;
            while(old.childCount>0)old.GetChild(0).SetParent(rect,false);Object.DestroyImmediate(old.gameObject);
            var start=Shake(root.transform,rect,"StartShake",1.1f);var bounce=Shake(root.transform,rect,"BounceShake",.3f);
            var popup=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/RecoveredUI/BonusRewardPopup.prefab"),bundle.transform);
            popup.GetComponent<Canvas>().overrideSorting=true;popup.GetComponent<Canvas>().sortingOrder=301;popup.SetActive(false);
            root.GetComponent<Canvas>().overrideSorting=true;root.GetComponent<Canvas>().sortingOrder=300;
            var mask=new GameObject("_WindowBg",typeof(RectTransform),typeof(Image),typeof(Button));mask.layer=5;mask.transform.SetParent(root.transform,false);mask.transform.SetAsFirstSibling();
            var mr=(RectTransform)mask.transform;mr.anchorMin=Vector2.zero;mr.anchorMax=Vector2.one;mr.sizeDelta=Vector2.zero;
            var image=mask.GetComponent<Image>();image.color=new Color(0,0,0,.65f);var button=mask.GetComponent<Button>();button.targetGraphic=image;button.transition=Selectable.Transition.None;
            var settings=new SerializedObject(bundle.GetComponent<RecoveredLuckySpinWindow>());
            settings.FindProperty("window").objectReferenceValue=root;settings.FindProperty("content").objectReferenceValue=content;settings.FindProperty("art").objectReferenceValue=art.GetComponent<RecoveredRegionAnimator>();
            settings.FindProperty("startShake").objectReferenceValue=start;settings.FindProperty("bounceShake").objectReferenceValue=bounce;
            settings.FindProperty("popup").objectReferenceValue=popup.GetComponent<RecoveredBonusRewardPopup>();
            var columns=settings.FindProperty("columns");columns.arraySize=4;for(int i=0;i<4;i++)columns.GetArrayElementAtIndex(i).objectReferenceValue=rect.Find("Col"+(i+1)).GetComponent<RecoveredLuckySpinColumn>();
            settings.FindProperty("windowDuration").floatValue=.3f;settings.FindProperty("openingDelay").floatValue=.5f;settings.FindProperty("startDelay").floatValue=.5f;
            settings.FindProperty("columnStartInterval").floatValue=.05f;settings.FindProperty("spinDelay").floatValue=.15f;settings.FindProperty("columnStopInterval").floatValue=.3f;settings.FindProperty("rewardDelay").floatValue=.7f;
            settings.FindProperty("enterEase").animationCurveValue=new AnimationCurve(new Keyframe(0,0,4.70158f,4.70158f),new Keyframe(1,1,0,0));
            settings.FindProperty("exitEase").animationCurveValue=new AnimationCurve(new Keyframe(0,0,0,0),new Keyframe(1,1,4.70158f,4.70158f));settings.ApplyModifiedPropertiesWithoutUndo();
            root.SetActive(false);PrefabUtility.SaveAsPrefabAsset(bundle,"Assets/Resources/RecoveredUI/LuckySpinWindow.prefab");AssetDatabase.SaveAssets();
        } finally {Object.DestroyImmediate(bundle);AssetDatabase.DeleteAsset(Temporary);}
    }
    private static RecoveredBoardShake Shake(Transform parent,RectTransform target,string name,float duration)
    {
        var node=new GameObject(name,typeof(RecoveredBoardShake));node.transform.SetParent(parent,false);var shake=node.GetComponent<RecoveredBoardShake>();var s=new SerializedObject(shake);
        s.FindProperty("target").objectReferenceValue=target;s.FindProperty("duration").floatValue=duration;s.FindProperty("intensity").floatValue=20;s.FindProperty("frequency").floatValue=20;
        s.FindProperty("falloff").animationCurveValue=AnimationCurve.EaseInOut(0,1,1,0);s.ApplyModifiedPropertiesWithoutUndo();return shake;
    }
    private static void Script<T>(Dictionary<string,string> map,string original) where T:MonoBehaviour
    {
        var host=new GameObject("Script identity",typeof(RectTransform));
        try {map[original]=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(MonoScript.FromMonoBehaviour(host.AddComponent<T>())));}
        finally {Object.DestroyImmediate(host);}
    }
}
