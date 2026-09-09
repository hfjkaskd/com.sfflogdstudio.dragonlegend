using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using DragonLegend.Whitebox;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public static class BuildMoreSpinWindow
{
    public static void Save()
    {
        const string temporary="Assets/Whitebox/Editor/MoreSpinSourceImport.prefab";
        const string folder="Assets/Resources/RecoveredUI/MoreSpinWindow";
        Directory.CreateDirectory(folder);File.Copy("ReferenceOriginal/Res/Anim/btnanim.anim",folder+"/btnanim.anim",true);AssetDatabase.Refresh();
        var map=new Dictionary<string,string>();
        Script<Image>(map,"3cf5a44414476512e00c3e7a2569a919");Script<Button>(map,"18d0a90695249463551c00f45766e642");
        Script<TextMeshProUGUI>(map,"3f96b1d166d19b209697e35b35d65c76");Script<GraphicRaycaster>(map,"86fe8f3fc59dc06ea6b45a1bbee64682");
        string source=File.ReadAllText("ReferenceOriginal/Res/ViewPrefabs/UIMoreSpinView.prefab").Replace("\r","");var removed=new List<string>();
        source=Regex.Replace(source,@"--- !u!\d+ &(\d+)\n.*?(?=\n--- !u!|\z)",m=>{
            var script=Regex.Match(m.Value,@"m_Script:.*guid: (\w+)");
            if(script.Success&&!map.ContainsKey(script.Groups[1].Value)){removed.Add(m.Groups[1].Value);return "";}return m.Value;
        },RegexOptions.Singleline);
        foreach(var id in removed)source=Regex.Replace(source,@"  - component: \{fileID: "+id+@"\}\n","");
        foreach(string name in new[]{"ty_bg01","ty_morespins_txt","ty_btn_cha","ty_spins_bg","ty_spins_txt","tc_btn_01"}) {
            string original="ReferenceOriginal/Res/UI/pop-up/"+name+".asset.meta";
            string guid=Regex.Match(File.ReadAllText(original),@"guid: (\w+)").Groups[1].Value;
            string path="Assets/Resources/RecoveredArt/Res/UI/pop-up/"+name+".png";
            if(AssetDatabase.LoadAssetAtPath<Sprite>(path)==null)throw new InvalidDataException(path);
            map[guid]=AssetDatabase.AssetPathToGUID(path);
            source=source.Replace("guid: "+guid+", type: 2","guid: "+guid+", type: 3");
        }
        map["39211f061913f054f84255431c8dce45"]=AssetDatabase.AssetPathToGUID("Assets/Resources/RecoveredUI/BonusRewardPopup/#0A5902_4.mat");
        map["183b0e4b7b3c5a34fa991aebe104ca31"]=AssetDatabase.AssetPathToGUID("Assets/Resources/RecoveredUI/BonusRewardPopup/tc_btn_bofang.asset");
        map["b02dda94bf0cfd148b568608d58490f9"]=AssetDatabase.AssetPathToGUID(folder+"/btnanim.anim");
        foreach(var pair in map)source=source.Replace(pair.Key,pair.Value);
        foreach(Match match in Regex.Matches(source,@"guid: (\w+)"))if(string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(match.Groups[1].Value)))throw new InvalidDataException("Unmapped MoreSpin GUID "+match.Groups[1].Value);
        File.WriteAllText(temporary,source);AssetDatabase.ImportAsset(temporary,ImportAssetOptions.ForceSynchronousImport);
        var root=PrefabUtility.LoadPrefabContents(temporary);
        try {
            var view=root.AddComponent<RecoveredMoreSpinWindow>();var settings=new SerializedObject(view);var content=root.transform.Find("Content");
            settings.FindProperty("content").objectReferenceValue=content;
            settings.FindProperty("claimButton").objectReferenceValue=content.Find("Btn/ClaimBtn").GetComponent<Button>();
            settings.FindProperty("closeButton").objectReferenceValue=content.Find("CloseBtn").GetComponent<Button>();
            foreach(var text in content.GetComponentsInChildren<TextMeshProUGUI>(true))if(text.text=="+10")settings.FindProperty("amount").objectReferenceValue=text;
            settings.FindProperty("duration").floatValue=.3f;
            settings.FindProperty("enterEase").animationCurveValue=new AnimationCurve(new Keyframe(0,0,4.70158f,4.70158f),new Keyframe(1,1,0,0));
            settings.FindProperty("exitEase").animationCurveValue=new AnimationCurve(new Keyframe(0,0,0,0),new Keyframe(1,1,4.70158f,4.70158f));
            settings.ApplyModifiedPropertiesWithoutUndo();root.GetComponent<Canvas>().sortingOrder=300;
            // BaseUIManager.AddColliderBgForWindow / UITools.AddBgColliderToTarget.
            // Author the native Image + Button structure in the prefab instead of rebuilding it on show.
            var background=new GameObject("_WindowBg",typeof(RectTransform),typeof(Image),typeof(Button));background.layer=root.layer;
            background.transform.SetParent(root.transform,false);background.transform.SetAsFirstSibling();
            var backgroundRect=(RectTransform)background.transform;
            backgroundRect.anchorMin=Vector2.zero;backgroundRect.anchorMax=Vector2.one;
            backgroundRect.offsetMin=backgroundRect.offsetMax=Vector2.zero;
            var backgroundImage=background.GetComponent<Image>();backgroundImage.color=new Color(0,0,0,.65f);
            var backgroundButton=background.GetComponent<Button>();backgroundButton.targetGraphic=backgroundImage;
            backgroundButton.transition=Selectable.Transition.None;
            // Native BaseWindow.OnClickBgMask is empty: absorb clicks without dismissing.
            root.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(root,"Assets/Resources/RecoveredUI/MoreSpinWindow.prefab");AssetDatabase.SaveAssets();
        } finally {PrefabUtility.UnloadPrefabContents(root);AssetDatabase.DeleteAsset(temporary);}
        BuildTipsWindow.Save();
    }
    private static void Script<T>(Dictionary<string,string> map,string original) where T:MonoBehaviour
    {
        var host=new GameObject("Script identity",typeof(RectTransform));
        try {map.Add(original,AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(MonoScript.FromMonoBehaviour(host.AddComponent<T>()))));}
        finally {Object.DestroyImmediate(host);}
    }
}
