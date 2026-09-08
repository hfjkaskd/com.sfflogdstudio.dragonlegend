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

public static class BuildFreeStartPopup
{
    private const string Folder="Assets/Resources/RecoveredUI/FreeStartPopup";
    private const string Temporary="Assets/Whitebox/Editor/FreeStartSourceImport.prefab";
    public static void Save()
    {
        Directory.CreateDirectory(Folder);
        // Retain original digit UVs, advances and built-in font material.
        foreach(string name in new[]{"FreeCountFont.asset","FreeCountFont_Material.mat"}) {
            File.Copy("ReferenceOriginal/Res/UI/free_game/"+name,Folder+"/"+name,true);
            if(!File.Exists(Folder+"/"+name+".meta"))File.Copy("ReferenceOriginal/Res/UI/free_game/"+name+".meta",Folder+"/"+name+".meta");
        }
        AssetDatabase.Refresh();
        var map=new Dictionary<string,string>();
        Script<Text>(map,"04f84fc2003509a5e7e068ec1271cc40");Script<Image>(map,"3cf5a44414476512e00c3e7a2569a919");
        Script<Button>(map,"18d0a90695249463551c00f45766e642");Script<TextMeshProUGUI>(map,"3f96b1d166d19b209697e35b35d65c76");
        Script<GraphicRaycaster>(map,"86fe8f3fc59dc06ea6b45a1bbee64682");
        map["39211f061913f054f84255431c8dce45"]=AssetDatabase.AssetPathToGUID("Assets/Resources/RecoveredUI/BonusRewardPopup/#0A5902_4.mat");
        map["183b0e4b7b3c5a34fa991aebe104ca31"]=AssetDatabase.AssetPathToGUID("Assets/Resources/RecoveredUI/BonusRewardPopup/tc_btn_bofang.asset");
        string[] sprites={"d1179c6aadecdc14a94d253d79ba4def|free_game/mfyx_congatulations01_txt","f4b25a1c65682f9409a101f75b641504|pop-up/tc_btn_01"};
        foreach(var row in sprites){var pair=row.Split('|');string path="Assets/Resources/RecoveredArt/Res/UI/"+pair[1]+".png";
            if(AssetDatabase.LoadAssetAtPath<Sprite>(path)==null)throw new InvalidDataException(path);map[pair[0]]=AssetDatabase.AssetPathToGUID(path);}
        string source=File.ReadAllText("ReferenceOriginal/Res/ViewPrefabs/UIFreeSpinStart.prefab").Replace("\r","");
        var removed=new List<string>();
        source=Regex.Replace(source,@"--- !u!\d+ &(\d+)\n.*?(?=\n--- !u!|\z)",m=>{
            if(Regex.IsMatch(m.Value,@"guid: (891b2f5d44e91e79664d39889d138791|da904f91a28860280fb976ac34b27df4|ff326197253ae4cec0b46632391386ec|e0b4d57e8f658b407a2ad25df85fb0d6)")){removed.Add(m.Groups[1].Value);return "";}return m.Value;
        },RegexOptions.Singleline);
        foreach(var id in removed)source=Regex.Replace(source,@"  - component: \{fileID: "+id+@"\}\n","");
        foreach(var pair in map){if(string.IsNullOrEmpty(pair.Value))throw new InvalidDataException(pair.Key);source=source.Replace(pair.Key,pair.Value);}
        foreach(var row in sprites){string guid=map[row.Split('|')[0]];source=source.Replace("guid: "+guid+", type: 2","guid: "+guid+", type: 3");}
        foreach(Match match in Regex.Matches(source,@"guid: (\w+)"))if(string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(match.Groups[1].Value)))throw new InvalidDataException("Unmapped FreeStart GUID "+match.Groups[1].Value);
        File.WriteAllText(Temporary,source);AssetDatabase.ImportAsset(Temporary,ImportAssetOptions.ForceSynchronousImport);
        var root=PrefabUtility.LoadPrefabContents(Temporary);
        try {
            var content=(RectTransform)root.transform.Find("Content");
            ReplaceArt(content.Find("SkeletonGraphic (ef_caidai)"),"ef_xjpl");
            var dragon=ReplaceArt(content.Find("SkeletonGraphic (ef_starttc)"),"ef_starttc");
            var parent=(RectTransform)content.Find("Btn/UnPlayBtn");
            var label=parent.Find("Text (TMP)").GetComponent<TextMeshProUGUI>();
            var corners=new Vector3[4];parent.GetLocalCorners(corners);
            var min=label.transform.InverseTransformPoint(parent.TransformPoint(corners[0]));
            var max=label.transform.InverseTransformPoint(parent.TransformPoint(corners[2]));var rect=label.rectTransform.rect;
            label.raycastPadding=new Vector4(rect.xMin-min.x,rect.yMin-min.y,max.x-rect.xMax,max.y-rect.yMax)*-1;
            label.raycastTarget=true;Object.DestroyImmediate(parent.GetComponent<Button>());
            var plain=label.gameObject.AddComponent<Button>();plain.targetGraphic=label;plain.transition=Selectable.Transition.None;
            var mask=new GameObject("_WindowBg",typeof(RectTransform),typeof(Image),typeof(Button));mask.layer=5;mask.transform.SetParent(root.transform,false);mask.transform.SetAsFirstSibling();
            var mr=(RectTransform)mask.transform;mr.anchorMin=Vector2.zero;mr.anchorMax=Vector2.one;mr.sizeDelta=Vector2.zero;
            var mi=mask.GetComponent<Image>();mi.color=new Color(0,0,0,.65f);
            var mb=mask.GetComponent<Button>();mb.targetGraphic=mi;mb.transition=Selectable.Transition.None;
            var popup=root.AddComponent<RecoveredFreeStartPopup>();var s=new SerializedObject(popup);
            s.FindProperty("content").objectReferenceValue=content;s.FindProperty("dragon").objectReferenceValue=dragon;
            s.FindProperty("countText").objectReferenceValue=content.Find("Text (Legacy)").GetComponent<Text>();
            s.FindProperty("advertisedText").objectReferenceValue=content.Find("Btn/ClaimBtn/Text (TMP)").GetComponent<TMP_Text>();
            s.FindProperty("plainText").objectReferenceValue=label;s.FindProperty("plainButton").objectReferenceValue=plain;
            s.FindProperty("claimButton").objectReferenceValue=content.Find("Btn/ClaimBtn").GetComponent<Button>();
            s.FindProperty("windowDuration").floatValue=.3f;s.FindProperty("countDuration").floatValue=.3f;s.FindProperty("adHideDelay").floatValue=.5f;
            s.FindProperty("enterEase").animationCurveValue=Curve(4.70158f,0);s.FindProperty("exitEase").animationCurveValue=Curve(0,4.70158f);
            s.FindProperty("countEase").animationCurveValue=Curve(2,0);
            s.FindProperty("advertisedFormat").stringValue="<sprite name=\"tc_btn_bofang\">FREE+{0}";s.FindProperty("plainCaption").stringValue="START";
            s.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(root,Folder+".prefab");AssetDatabase.SaveAssets();
        } finally {PrefabUtility.UnloadPrefabContents(root);AssetDatabase.DeleteAsset(Temporary);}
    }
    private static AnimationCurve Curve(float start,float end)=>new AnimationCurve(new Keyframe(0,0,start,start),new Keyframe(1,1,end,end));
    private static RecoveredRegionAnimator ReplaceArt(Transform prior,string name)
    {
        var node=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/RecoveredUI/JackpotPopupArt/"+name+".prefab"),prior.parent);
        var a=(RectTransform)node.transform;var b=(RectTransform)prior;
        a.anchorMin=b.anchorMin;a.anchorMax=b.anchorMax;a.pivot=b.pivot;a.sizeDelta=b.sizeDelta;a.anchoredPosition3D=b.anchoredPosition3D;a.localRotation=b.localRotation;a.localScale=b.localScale;
        a.SetSiblingIndex(b.GetSiblingIndex());node.name=prior.name;Object.DestroyImmediate(prior.gameObject);return node.GetComponent<RecoveredRegionAnimator>();
    }
    private static void Script<T>(Dictionary<string,string> map,string original) where T:MonoBehaviour
    {
        var host=new GameObject("Script identity",typeof(RectTransform));
        try {var component=host.AddComponent<T>();map.Add(original,AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(MonoScript.FromMonoBehaviour(component))));}
        finally {Object.DestroyImmediate(host);}
    }
}
