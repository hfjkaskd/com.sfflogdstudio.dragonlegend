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

public static class BuildTreasureWindow
{
    private const string Path="Assets/Resources/RecoveredUI/TreasureWindow.prefab";
    public static void Save()
    {
        var map=new Dictionary<string,string>();
        BuildTreasureCard.Script<Text>(map,"04f84fc2003509a5e7e068ec1271cc40");
        BuildTreasureCard.Script<Image>(map,"3cf5a44414476512e00c3e7a2569a919");
        BuildTreasureCard.Script<Image>(map,"e0b4d57e8f658b407a2ad25df85fb0d6");
        BuildTreasureCard.Script<Button>(map,"18d0a90695249463551c00f45766e642");
        BuildTreasureCard.Script<Slider>(map,"1522d51f2e596a7eb84e5007260f43ac");
        BuildTreasureCard.Script<TextMeshProUGUI>(map,"3f96b1d166d19b209697e35b35d65c76");
        BuildTreasureCard.Script<GraphicRaycaster>(map,"86fe8f3fc59dc06ea6b45a1bbee64682");
        BuildTreasureCard.Map(map,"36977c4faccb97c4ebe0b4fdeea88b25","Assets/Resources/RecoveredUI/CoinRewardText/Green.asset");
        BuildTreasureCard.Map(map,"183b0e4b7b3c5a34fa991aebe104ca31","Assets/Resources/RecoveredUI/BonusRewardPopup/tc_btn_bofang.asset");
        BuildTreasureCard.Map(map,"39211f061913f054f84255431c8dce45","Assets/Resources/RecoveredUI/BonusRewardPopup/#0A5902_4.mat");
        BuildTreasureCard.Map(map,"b02dda94bf0cfd148b568608d58490f9","Assets/Resources/RecoveredUI/BonusRewardPopup/btnanim.anim");
        string[] ids={"27b6ea552c8080340b36ee5350e3df1d","41b145ed777ebf543b76b37fbe73d627","f4b25a1c65682f9409a101f75b641504","cfe6ee8dfa40fbb47b0b515bf707d2c6","d3b5f5689331d58468651692a05e4947","dbd5a01e349cc9d4c99f05a855f81631","6cb24883074c2f34b851df577163ae9a","390cf1973ff4aec44bdfc3b3d7ba0dda","3694a020e04ab824f977ebc5b2f3f415"};
        string[] paths={"Res/UI/pop-up/tc_sc_bak02.png","Res/UI/pop-up/tc_sc_bak01.png","Res/UI/pop-up/tc_btn_01.png","Res/UI/pop-up/tc_bak_xinxi.png","Res/UI/pop-up/tc_bak_bar02.png","Res/UI/pop-up/tc_bak_bar01.png","Res/UI/pop-up/sc_luckycard_txt.png","IndividualSprites/t_icon_02_6968590429478003173.png","IndividualSprites/t_hb_amazon_4890283666839968588.png"};
        for(int i=0;i<ids.Length;i++)BuildTreasureCard.Map(map,ids[i],"Assets/Resources/RecoveredArt/"+paths[i]);
        var iconPaths=new List<string>(Directory.GetFiles("Assets/Resources/RecoveredArt/IndividualSprites","t_icon_*.png"));iconPaths.Sort();
        foreach(string path in iconPaths){var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.spritePixelsPerUnit=100;importer.mipmapEnabled=false;importer.npotScale=TextureImporterNPOTScale.None;
            importer.alphaIsTransparency=true;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();}
        string source=File.ReadAllText("ReferenceOriginal/Res/ViewPrefabs/UITreasureView.prefab").Replace("\r","");
        var removed=new List<string>();
        string yaml=Regex.Replace(source,@"--- !u!114 &(\d+)\n.*?(?=\n--- !u!|\z)",m=>{
            if(Regex.IsMatch(m.Value,@"guid: (ff326197253ae4cec0b46632391386ec|132b6501dc1746e5b20bc04c0ad3cc98|367bd284c466d26a1e86c12585ce5b4b|a508405cf377cc62f2132160b9bdc78f|e7323dc639d7fd510722a736279a6d7e)")){removed.Add(m.Groups[1].Value);return "";}
            return m.Value.Contains("e0b4d57e8f658b407a2ad25df85fb0d6")?m.Value.Replace("m_Color: {r: 1, g: 1, b: 1, a: 1}","m_Color: {r: 1, g: 1, b: 1, a: 0}"):m.Value;
        },RegexOptions.Singleline);
        foreach(string id in removed)yaml=yaml.Replace("  - component: {fileID: "+id+"}\n","");
        foreach(var pair in map)yaml=yaml.Replace(pair.Key,pair.Value);
        foreach(string id in ids)yaml=yaml.Replace("guid: "+map[id]+", type: 2","guid: "+map[id]+", type: 3");
        foreach(Match m in Regex.Matches(yaml,@"guid: (\w+)"))if(string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(m.Groups[1].Value)))throw new InvalidDataException("Unmapped Treasure GUID "+m.Groups[1].Value);
        File.WriteAllText(Path,yaml,new UTF8Encoding(false));AssetDatabase.ImportAsset(Path,ImportAssetOptions.ForceSynchronousImport);
        var root=PrefabUtility.LoadPrefabContents(Path);
        try {
            var content=root.transform.Find("Content");
            var card=Replace(content.Find("TreasureCard"),"Assets/Resources/RecoveredUI/TreasureCard.prefab").GetComponent<RecoveredTreasureCard>();
            var tip=Replace(content.Find("CollectTip"),"Assets/Resources/RecoveredUI/CollectTip.prefab").GetComponent<RecoveredCollectTip>();
            var light=Replace(content.Find("Light"),"Assets/Resources/RecoveredUI/JackpotPopupArt/ef_shoucanggl.prefab");
            var confetti=Replace(content.Find("SkeletonGraphic (ef_caidai)"),"Assets/Resources/RecoveredUI/JackpotPopupArt/ef_xjpl.prefab");
            var title=content.Find("Title");var shine=new SerializedObject(title.gameObject.AddComponent<RecoveredTitleShine>());
            shine.FindProperty("template").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/RecoveredUI/BonusRewardPopup/TitleShine.mat");
            shine.FindProperty("effectFactor").floatValue=.31216258f;shine.ApplyModifiedPropertiesWithoutUndo();
            var window=root.AddComponent<RecoveredTreasureWindow>();var p=new SerializedObject(window);
            Set(p,"content",content);Set(p,"title",title);Set(p,"light",light.transform);Set(p,"confetti",confetti.transform);
            Set(p,"card",card);Set(p,"collectTip",tip);Set(p,"blackBackground",content.Find("bLACK").gameObject);
            Set(p,"endPosition",card.Back.transform.Find("EndPos"));Set(p,"cardImage",card.Front.transform.Find("Image").GetComponent<Image>());
            Set(p,"rewardText",card.Front.transform.Find("Info/Text (Legacy)").GetComponent<Text>());
            var claim=card.Front.transform.Find("Info/Btn/ClaimBtn");var plain=card.Front.transform.Find("Info/Btn/UnPlayBtn");
            Set(p,"claimButton",claim.GetComponent<Button>());Set(p,"plainButton",plain.GetComponent<Button>());
            Set(p,"claimText",claim.GetComponentInChildren<TMP_Text>(true));Set(p,"plainText",plain.GetComponentInChildren<TMP_Text>(true));
            p.FindProperty("claimFormat").stringValue="<sprite name=\"tc_btn_bofang\">CLAIMx{0}";
            p.FindProperty("claimPlainFormat").stringValue="<sprite name=\"tc_btn_bofang\">CLAIM";p.FindProperty("plainFormat").stringValue="Only {0}";
            p.FindProperty("countDuration").floatValue=.5f;p.FindProperty("exitDuration").floatValue=.3f;
            p.FindProperty("fromScale").floatValue=0;p.FindProperty("toScale").floatValue=1;
            p.FindProperty("resetFlightScale").vector3Value=Vector3.one*.4f;
            p.FindProperty("countEase").animationCurveValue=new AnimationCurve(new Keyframe(0,0,2,2),new Keyframe(1,1,0,0));
            p.FindProperty("exitEase").animationCurveValue=new AnimationCurve(new Keyframe(0,0,0,0),new Keyframe(1,1,4.70158f,4.70158f));
            var icons=p.FindProperty("icons");icons.arraySize=iconPaths.Count;
            for(int i=0;i<iconPaths.Count;i++){var value=icons.GetArrayElementAtIndex(i);value.FindPropertyRelative("id").intValue=int.Parse(Regex.Match(iconPaths[i],@"t_icon_(\d+)_").Groups[1].Value);value.FindPropertyRelative("path").stringValue=iconPaths[i].Replace('\\','/').Replace("Assets/Resources/","").Replace(".png","");}
            p.ApplyModifiedPropertiesWithoutUndo();
            // Normal window mask (300,1): transparent blocker, authored black background handles dimming.
            var mask=new GameObject("_WindowBg",typeof(RectTransform),typeof(Image),typeof(Button));mask.layer=5;mask.transform.SetParent(root.transform,false);mask.transform.SetAsFirstSibling();
            var mr=(RectTransform)mask.transform;mr.anchorMin=Vector2.zero;mr.anchorMax=Vector2.one;mr.sizeDelta=Vector2.zero;
            mask.GetComponent<Image>().color=Color.clear;mask.GetComponent<Button>().transition=Selectable.Transition.None;
            root.GetComponent<Canvas>().sortingOrder=300;root.GetComponent<Canvas>().overrideSorting=true;
            root.name="TreasureWindow";root.SetActive(false);PrefabUtility.SaveAsPrefabAsset(root,Path);
        } finally {PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();
    }
    private static void Set(SerializedObject p,string name,Object value)=>p.FindProperty(name).objectReferenceValue=value;
    private static GameObject Replace(Transform prior,string path)
    {
        if(prior==null)throw new InvalidDataException("Missing source node for "+path);
        var node=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),prior.parent);
        var a=(RectTransform)node.transform;var b=(RectTransform)prior;
        a.anchorMin=b.anchorMin;a.anchorMax=b.anchorMax;a.pivot=b.pivot;a.sizeDelta=b.sizeDelta;a.anchoredPosition3D=b.anchoredPosition3D;a.localRotation=b.localRotation;a.localScale=b.localScale;
        a.SetSiblingIndex(b.GetSiblingIndex());node.name=prior.name;node.SetActive(prior.gameObject.activeSelf);Object.DestroyImmediate(prior.gameObject);return node;
    }
}
