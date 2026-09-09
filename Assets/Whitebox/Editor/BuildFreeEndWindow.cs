using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using DragonLegend.Whitebox;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class BuildFreeEndWindow
{
    public static void Save()
    {
        const string output = "Assets/Resources/RecoveredUI/FreeEndWindow.prefab";
        var map = new Dictionary<string,string>();
        BuildTreasureCard.Script<Text>(map,"04f84fc2003509a5e7e068ec1271cc40");
        BuildTreasureCard.Script<Image>(map,"3cf5a44414476512e00c3e7a2569a919");
        BuildTreasureCard.Script<Button>(map,"18d0a90695249463551c00f45766e642");
        BuildTreasureCard.Script<TextMeshProUGUI>(map,"3f96b1d166d19b209697e35b35d65c76");
        BuildTreasureCard.Script<GraphicRaycaster>(map,"86fe8f3fc59dc06ea6b45a1bbee64682");
        BuildTreasureCard.Map(map,"36977c4faccb97c4ebe0b4fdeea88b25","Assets/Resources/RecoveredUI/CoinRewardText/Green.asset");
        BuildTreasureCard.Map(map,"39211f061913f054f84255431c8dce45","Assets/Resources/RecoveredUI/BonusRewardPopup/#0A5902_4.mat");
        BuildTreasureCard.Map(map,"183b0e4b7b3c5a34fa991aebe104ca31","Assets/Resources/RecoveredUI/BonusRewardPopup/tc_btn_bofang.asset");
        string[] ids={"12fc056f3de0bd04fa9fd40c729b3775","6a6ff8ac99648f148a154f197c0b7b9f","f4b25a1c65682f9409a101f75b641504"};
        string[] paths={"free_game/mfyx_congatulations02_txt","free_game/mfyx_youwin_txt","pop-up/tc_btn_01"};
        for(int i=0;i<ids.Length;i++) BuildTreasureCard.Map(map,ids[i],"Assets/Resources/RecoveredArt/Res/UI/"+paths[i]+".png");
        string source=File.ReadAllText("ReferenceOriginal/Res/ViewPrefabs/UIFreeSpinEndView.prefab").Replace("\r","");
        var removed=new List<string>();
        source=Regex.Replace(source,@"--- !u!114 &(\d+)\n.*?(?=\n--- !u!|\z)",m=>{
            if(Regex.IsMatch(m.Value,@"guid: (a0a63690ed1094fb843398a28c301683|da904f91a28860280fb976ac34b27df4|ff326197253ae4cec0b46632391386ec|132b6501dc1746e5b20bc04c0ad3cc98)")){removed.Add(m.Groups[1].Value);return "";}return m.Value;
        },RegexOptions.Singleline);
        foreach(string id in removed) source=source.Replace("  - component: {fileID: "+id+"}\n","");
        foreach(var pair in map) source=source.Replace(pair.Key,pair.Value);
        foreach(string id in ids)source=source.Replace("guid: "+map[id]+", type: 2","guid: "+map[id]+", type: 3");
        foreach(Match match in Regex.Matches(source,@"guid: (\w+)"))if(string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(match.Groups[1].Value)))throw new InvalidDataException("Unmapped FreeEnd GUID "+match.Groups[1].Value);
        File.WriteAllText(output,source);AssetDatabase.ImportAsset(output,ImportAssetOptions.ForceSynchronousImport);
        var root=PrefabUtility.LoadPrefabContents(output);
        try {
            var content=root.transform.Find("Content");
            var art=ReplaceArt(content.Find("SkeletonGraphic (ef_overtc)"),"ef_overtc");
            var confetti=ReplaceArt(content.Find("SkeletonGraphic (ef_xjpl)"),"ef_xjpl");
            foreach(string title in new[]{"Title","YouWin"}) {
                var shine=new SerializedObject(content.Find(title).gameObject.AddComponent<RecoveredTitleShine>());
                shine.FindProperty("template").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/RecoveredUI/BonusRewardPopup/TitleShine.mat");
                shine.FindProperty("effectFactor").floatValue=.78734106f;shine.ApplyModifiedPropertiesWithoutUndo();
            }
            var window=root.AddComponent<RecoveredFreeEndWindow>();var settings=new SerializedObject(window);
            settings.FindProperty("totalText").objectReferenceValue=content.Find("Text (Legacy)").GetComponent<Text>();
            settings.FindProperty("tipText").objectReferenceValue=content.Find("Text (TMP)").GetComponent<TMP_Text>();
            settings.FindProperty("continueButton").objectReferenceValue=content.Find("Btn/ContinueBtn").GetComponent<Button>();
            settings.FindProperty("artwork").objectReferenceValue=art;settings.FindProperty("confetti").objectReferenceValue=confetti.gameObject;
            settings.FindProperty("tipFormat").stringValue="IN <material=\"#003815_3\"><gradient=\"cash\">{0}</gradient></material> FREE SPINS";
            settings.FindProperty("startClip").intValue=1;settings.FindProperty("idleClip").intValue=0;
            settings.FindProperty("startDelay").floatValue=.8f;settings.FindProperty("countDuration").floatValue=.5f;
            settings.FindProperty("buttonDelay").floatValue=.5f;settings.FindProperty("buttonDuration").floatValue=.3f;
            var curve=new AnimationCurve(new Keyframe(0,0,2,2),new Keyframe(1,1,0,0));
            settings.FindProperty("countCurve").animationCurveValue=curve;settings.FindProperty("buttonCurve").animationCurveValue=curve;
            settings.ApplyModifiedPropertiesWithoutUndo();
            var mask=new GameObject("_WindowBg",typeof(RectTransform),typeof(Image),typeof(Button));mask.layer=5;
            mask.transform.SetParent(root.transform,false);mask.transform.SetAsFirstSibling();
            var mr=(RectTransform)mask.transform;mr.anchorMin=Vector2.zero;mr.anchorMax=Vector2.one;mr.sizeDelta=Vector2.zero;
            var image=mask.GetComponent<Image>();image.color=new Color(0,0,0,.65f);
            var button=mask.GetComponent<Button>();button.targetGraphic=image;button.transition=Selectable.Transition.None;
            var canvas=root.GetComponent<Canvas>();canvas.sortingOrder=300;canvas.overrideSorting=true;
            root.name="FreeEndWindow";root.SetActive(false);PrefabUtility.SaveAsPrefabAsset(root,output);
        } finally {PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();
    }
    private static RecoveredRegionAnimator ReplaceArt(Transform prior,string name)
    {
        var node=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/RecoveredUI/JackpotPopupArt/"+name+".prefab"),prior.parent);
        var a=(RectTransform)node.transform;var b=(RectTransform)prior;
        a.anchorMin=b.anchorMin;a.anchorMax=b.anchorMax;a.pivot=b.pivot;a.sizeDelta=b.sizeDelta;
        a.anchoredPosition3D=b.anchoredPosition3D;a.localRotation=b.localRotation;a.localScale=b.localScale;
        node.SetActive(prior.gameObject.activeSelf);a.SetSiblingIndex(b.GetSiblingIndex());node.name=prior.name;Object.DestroyImmediate(prior.gameObject);
        return node.GetComponent<RecoveredRegionAnimator>();
    }
}
