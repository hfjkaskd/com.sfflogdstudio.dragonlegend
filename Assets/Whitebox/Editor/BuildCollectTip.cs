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

public static class BuildCollectTip
{
    private const string Path="Assets/Resources/RecoveredUI/CollectTip.prefab";
    public static void Save()
    {
        var map=new Dictionary<string,string>();
        BuildTreasureCard.Script<Image>(map,"3cf5a44414476512e00c3e7a2569a919");
        BuildTreasureCard.Script<Slider>(map,"1522d51f2e596a7eb84e5007260f43ac");
        BuildTreasureCard.Script<TextMeshProUGUI>(map,"3f96b1d166d19b209697e35b35d65c76");
        BuildTreasureCard.Script<RecoveredCollectTip>(map,"a508405cf377cc62f2132160b9bdc78f");
        string[] ids={"cfe6ee8dfa40fbb47b0b515bf707d2c6","d3b5f5689331d58468651692a05e4947","dbd5a01e349cc9d4c99f05a855f81631","390cf1973ff4aec44bdfc3b3d7ba0dda","3694a020e04ab824f977ebc5b2f3f415"};
        string[] paths={"Res/UI/pop-up/tc_bak_xinxi.png","Res/UI/pop-up/tc_bak_bar02.png","Res/UI/pop-up/tc_bak_bar01.png","IndividualSprites/t_icon_02_6968590429478003173.png","IndividualSprites/t_hb_amazon_4890283666839968588.png"};
        string amazon="Assets/Resources/RecoveredArt/"+paths[4];
        var importer=(TextureImporter)AssetImporter.GetAtPath(amazon);
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
        importer.spritePixelsPerUnit=100;importer.mipmapEnabled=false;importer.npotScale=TextureImporterNPOTScale.None;
        importer.alphaIsTransparency=true;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
        for(int i=0;i<ids.Length;i++) {
            string path="Assets/Resources/RecoveredArt/"+paths[i];
            if(AssetDatabase.LoadAssetAtPath<Sprite>(path)==null)throw new InvalidDataException(path);
            BuildTreasureCard.Map(map,ids[i],path);
        }
        string source=File.ReadAllText("ReferenceOriginal/Res/ViewPrefabs/UITreasureView.prefab").Replace("\r","");
        var blocks=new Dictionary<string,string>();
        foreach(Match m in Regex.Matches(source,@"^--- !u!\d+ &(\d+)\n.*?(?=^--- !u!|\z)",RegexOptions.Multiline|RegexOptions.Singleline))blocks.Add(m.Groups[1].Value,m.Value);
        var text=new StringBuilder("%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n");
        BuildTreasureCard.Append("224574304967836189","224574304967836189",blocks,text);
        string yaml=text.ToString();var removed=new List<string>();
        yaml=Regex.Replace(yaml,@"--- !u!114 &(\d+)\n.*?(?=\n--- !u!|\z)",m=>{
            if(m.Value.Contains("ff326197253ae4cec0b46632391386ec")){removed.Add(m.Groups[1].Value);return "";}return m.Value;
        },RegexOptions.Singleline);
        foreach(string id in removed)yaml=yaml.Replace("  - component: {fileID: "+id+"}\n","");
        yaml=yaml.Replace("  TipsTxt:","  tips:").Replace("  ProgressSlider:","  progressSlider:")
            .Replace("  CollectImg:","  collectImage:").Replace("  RewardTxt:","  rewardText:").Replace("  ProgressTxt:","  progressText:");
        foreach(var pair in map)yaml=yaml.Replace(pair.Key,pair.Value);
        foreach(string id in ids)yaml=yaml.Replace("guid: "+map[id]+", type: 2","guid: "+map[id]+", type: 3");
        foreach(Match m in Regex.Matches(yaml,@"guid: (\w+)"))if(string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(m.Groups[1].Value)))throw new InvalidDataException("Unmapped CollectTip GUID "+m.Groups[1].Value);
        File.WriteAllText(Path,yaml,new UTF8Encoding(false));AssetDatabase.ImportAsset(Path,ImportAssetOptions.ForceSynchronousImport);
        var root=PrefabUtility.LoadPrefabContents(Path);
        try {
            var old=(RectTransform)root.transform.Find("SkeletonGraphic (ef_shoucanggl)");
            var art=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/RecoveredUI/JackpotPopupArt/ef_shoucanggl.prefab"),old.parent);
            var rect=(RectTransform)art.transform;
            rect.anchorMin=old.anchorMin;rect.anchorMax=old.anchorMax;rect.pivot=old.pivot;rect.sizeDelta=old.sizeDelta;
            rect.anchoredPosition3D=old.anchoredPosition3D;rect.localRotation=old.localRotation;rect.localScale=old.localScale;
            rect.SetSiblingIndex(old.GetSiblingIndex());art.name=old.name;art.SetActive(old.gameObject.activeSelf);Object.DestroyImmediate(old.gameObject);
            var settings=new SerializedObject(root.GetComponent<RecoveredCollectTip>());
            settings.FindProperty("remainingFormat").stringValue="Collect <material=\"#003815_3\"><gradient=\"cash\">{0}</gradient></material> treasures to redeem the rewards.";
            settings.FindProperty("progressFormat").stringValue="{0}/{1}";
            settings.FindProperty("revealDelay").floatValue=.5f;settings.FindProperty("revealDuration").floatValue=.3f;
            settings.FindProperty("revealEase").animationCurveValue=new AnimationCurve(new Keyframe(0,0,2,2),new Keyframe(1,1,0,0));
            settings.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(root,Path);
        } finally {PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();
    }
}
