using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using DragonLegend.Whitebox;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class BuildMoreWildWindow
{
    const string Folder = "Assets/Resources/RecoveredUI/MoreWildWindow";
    public static void Save()
    {
        Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
        BuildJackpotPopupArt.Create("ef_wild3", "棋子/wild3", new Vector2(216.00003f,544), new Vector2(.5f,.49816173f), "Tools/Evidence/Wild/", "Artifacts/MoreWildAuthoring/");
        var map = new Dictionary<string,string>();
        BuildTreasureCard.Script<Image>(map,"3cf5a44414476512e00c3e7a2569a919");
        BuildTreasureCard.Script<Button>(map,"18d0a90695249463551c00f45766e642");
        BuildTreasureCard.Script<TextMeshProUGUI>(map,"3f96b1d166d19b209697e35b35d65c76");
        BuildTreasureCard.Script<GraphicRaycaster>(map,"86fe8f3fc59dc06ea6b45a1bbee64682");
        BuildTreasureCard.Script<HorizontalLayoutGroup>(map,"19fbb4a32b59286cd89b624f52ff7943");
        string source = File.ReadAllText("ReferenceOriginal/Res/ViewPrefabs/UIMoreWildView.prefab").Replace("\r", "");
        var removed = new List<string>();
        source = Regex.Replace(source,@"--- !u!\d+ &(\d+)\n.*?(?=\n--- !u!|\z)", m => {
            var script=Regex.Match(m.Value,@"m_Script:.*guid: (\w+)");
            if(script.Success&&!map.ContainsKey(script.Groups[1].Value)){removed.Add(m.Groups[1].Value);return "";}return m.Value;
        },RegexOptions.Singleline);
        foreach(var id in removed)source=source.Replace("  - component: {fileID: "+id+"}\n", "");
        foreach(var pair in new Dictionary<string,string>{
            {"3008572a0bbfdf44faed15b37312eb4e","pop-up/ty_btn_cha"},
            {"e4d5595766a46034bb594090fc17bda9","more_wild/more_wild_txt"},
            {"f4b25a1c65682f9409a101f75b641504","pop-up/tc_btn_01"},
            {"f7878738b23700a48a9c2287853f5b1b","more_wild/mw_bg"}})
        {
            BuildTreasureCard.Map(map,pair.Key,"Assets/Resources/RecoveredArt/Res/UI/"+pair.Value+".png");
            source=source.Replace("guid: "+pair.Key+", type: 2","guid: "+pair.Key+", type: 3");
        }
        BuildTreasureCard.Map(map,"39211f061913f054f84255431c8dce45","Assets/Resources/RecoveredUI/BonusRewardPopup/#0A5902_4.mat");
        BuildTreasureCard.Map(map,"183b0e4b7b3c5a34fa991aebe104ca31","Assets/Resources/RecoveredUI/BonusRewardPopup/tc_btn_bofang.asset");
        CreateSprite(); BuildTreasureCard.Map(map,"f1244a41aeffdf742ba59dde05d9374b",Folder+"/mw_wild_iocn.asset");
        foreach(var pair in map)source=source.Replace(pair.Key,pair.Value);
        foreach(Match m in Regex.Matches(source,@"guid: (\w+)"))if(string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(m.Groups[1].Value)))throw new InvalidDataException("Unmapped MoreWild GUID "+m.Groups[1].Value);
        const string temporary="Assets/Whitebox/Editor/MoreWildSource.prefab";
        File.WriteAllText(temporary,source);AssetDatabase.ImportAsset(temporary,ImportAssetOptions.ForceSynchronousImport);
        var root=PrefabUtility.LoadPrefabContents(temporary);
        try
        {
            var content=root.transform.Find("Content");
            ReplaceArt((RectTransform)content.Find("SkeletonGraphic (ef_long)"),"ef_long");
            var layout=content.Find("Layout");
            var children=new List<RectTransform>();foreach(RectTransform child in layout)children.Add(child);
            foreach(var child in children)ReplaceArt(child,"ef_wild3");
            var shine=new SerializedObject(content.Find("Title").gameObject.AddComponent<RecoveredTitleShine>());
            shine.FindProperty("template").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/RecoveredUI/BonusRewardPopup/TitleShine.mat");shine.ApplyModifiedPropertiesWithoutUndo();
            var guide=(RecoveredFirstSpinGuide)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<RecoveredFirstSpinGuide>("Assets/Resources/RecoveredUI/FirstSpinGuide.prefab"),root.transform);
            var view=root.AddComponent<RecoveredMoreWildWindow>();var settings=new SerializedObject(view);
            settings.FindProperty("content").objectReferenceValue=content;
            var claim=content.Find("Btn/ClaimBtn").GetComponent<Button>();
            settings.FindProperty("claimButton").objectReferenceValue=claim;
            settings.FindProperty("closeButton").objectReferenceValue=content.Find("CloseBtn").GetComponent<Button>();
            settings.FindProperty("tips").objectReferenceValue=content.Find("Text (TMP)").GetComponent<TMP_Text>();
            settings.FindProperty("claimText").objectReferenceValue=claim.GetComponentInChildren<TMP_Text>();
            settings.FindProperty("guide").objectReferenceValue=guide;
            settings.FindProperty("fingerPrefab").objectReferenceValue=AssetDatabase.LoadAssetAtPath<RectTransform>("Assets/Resources/RecoveredUI/Finger.prefab");
            settings.FindProperty("tipsFormat").stringValue="Get <sprite name=\"mw_wild_iocn\"> in next <material=\"#003815_3\"><gradient=\"cash\">{0}</gradient></material> SPINS";
            settings.FindProperty("freeText").stringValue="CLAIM";
            settings.FindProperty("adText").stringValue="<sprite name=\"tc_btn_bofang\">CLAIM";
            settings.FindProperty("duration").floatValue=.3f;
            settings.FindProperty("enterEase").animationCurveValue=new AnimationCurve(new Keyframe(0,0,4.70158f,4.70158f),new Keyframe(1,1,0,0));
            settings.FindProperty("exitEase").animationCurveValue=new AnimationCurve(new Keyframe(0,0,0,0),new Keyframe(1,1,4.70158f,4.70158f));
            settings.ApplyModifiedPropertiesWithoutUndo();root.GetComponent<Canvas>().sortingOrder=300;
            var bg=new GameObject("_WindowBg",typeof(RectTransform),typeof(Image),typeof(Button));bg.layer=root.layer;bg.transform.SetParent(root.transform,false);bg.transform.SetAsFirstSibling();
            var rect=(RectTransform)bg.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            var image=bg.GetComponent<Image>();image.color=new Color(0,0,0,.65f);var button=bg.GetComponent<Button>();button.targetGraphic=image;button.transition=Selectable.Transition.None;
            root.SetActive(false);PrefabUtility.SaveAsPrefabAsset(root,"Assets/Resources/RecoveredUI/MoreWildWindow.prefab");AssetDatabase.SaveAssets();
        }
        finally {PrefabUtility.UnloadPrefabContents(root);AssetDatabase.DeleteAsset(temporary);}
        BuildCoreRoundFlow.Save();
    }
    static void ReplaceArt(RectTransform original,string asset)
    {
        var obj=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/RecoveredUI/JackpotPopupArt/"+asset+".prefab"),original.parent);
        var rect=(RectTransform)obj.transform;rect.anchorMin=original.anchorMin;rect.anchorMax=original.anchorMax;rect.pivot=original.pivot;
        rect.sizeDelta=original.sizeDelta;rect.anchoredPosition=original.anchoredPosition;rect.localScale=original.localScale;
        rect.SetSiblingIndex(original.GetSiblingIndex());obj.layer=original.gameObject.layer;obj.name=original.name;Object.DestroyImmediate(original.gameObject);
    }
    static void CreateSprite()
    {
        var sprite=ScriptableObject.CreateInstance<TMP_SpriteAsset>();sprite.name="mw_wild_iocn";sprite.hashCode=1843674983;
        var settings=new SerializedObject(sprite);settings.FindProperty("m_Version").stringValue="1.1.0";settings.ApplyModifiedPropertiesWithoutUndo();
        const string path="Assets/Resources/RecoveredArt/Res/UI/more_wild/mw_wild_iocn.png";
        sprite.spriteSheet=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        var mat=new Material(Shader.Find("TextMeshPro/Sprite")){name="mw_wild_iocn",mainTexture=sprite.spriteSheet};sprite.material=SaveAsset(mat,Folder+"/mw_wild_iocn.mat");
        var glyph=new TMP_SpriteGlyph(0,new GlyphMetrics(76,69,14.4f,54,96),new GlyphRect(0,0,76,69),1.5f,0){sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path)};
        sprite.spriteGlyphTable.Add(glyph);sprite.spriteCharacterTable.Add(new TMP_SpriteCharacter(65534,sprite,glyph){name="mw_wild_iocn",scale=1});sprite.UpdateLookupTables();SaveAsset(sprite,Folder+"/mw_wild_iocn.asset");
    }
    static T SaveAsset<T>(T value,string path) where T:Object
    {
        var old=AssetDatabase.LoadAssetAtPath<T>(path);if(old==null){AssetDatabase.CreateAsset(value,path);return value;}
        EditorUtility.CopySerialized(value,old);Object.DestroyImmediate(value);return old;
    }
}
