using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using DragonLegend.Whitebox;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class BuildTreasureCard
{
    private const string Path = "Assets/Resources/RecoveredUI/TreasureCard.prefab";
    public static void Save()
    {
        var map = new Dictionary<string,string>();
        Script<Text>(map,"04f84fc2003509a5e7e068ec1271cc40");
        Script<Image>(map,"3cf5a44414476512e00c3e7a2569a919");
        Script<Image>(map,"e0b4d57e8f658b407a2ad25df85fb0d6");
        Script<Button>(map,"18d0a90695249463551c00f45766e642");
        Script<TextMeshProUGUI>(map,"3f96b1d166d19b209697e35b35d65c76");
        Script<RecoveredTreasureCard>(map,"367bd284c466d26a1e86c12585ce5b4b");
        Map(map,"36977c4faccb97c4ebe0b4fdeea88b25","Assets/Resources/RecoveredUI/CoinRewardText/Green.asset");
        Map(map,"183b0e4b7b3c5a34fa991aebe104ca31","Assets/Resources/RecoveredUI/BonusRewardPopup/tc_btn_bofang.asset");
        Map(map,"39211f061913f054f84255431c8dce45","Assets/Resources/RecoveredUI/BonusRewardPopup/#0A5902_4.mat");
        Map(map,"b02dda94bf0cfd148b568608d58490f9","Assets/Resources/RecoveredUI/BonusRewardPopup/btnanim.anim");
        string[] ids={"27b6ea552c8080340b36ee5350e3df1d","41b145ed777ebf543b76b37fbe73d627","f4b25a1c65682f9409a101f75b641504","390cf1973ff4aec44bdfc3b3d7ba0dda"};
        string[] paths={"Res/UI/pop-up/tc_sc_bak02.png","Res/UI/pop-up/tc_sc_bak01.png","Res/UI/pop-up/tc_btn_01.png","IndividualSprites/t_icon_02_6968590429478003173.png"};
        // The separately extracted collection icon had remained an unconfigured texture.
        string iconPath="Assets/Resources/RecoveredArt/"+paths[3];
        var iconImporter=(TextureImporter)AssetImporter.GetAtPath(iconPath);
        iconImporter.textureType=TextureImporterType.Sprite;iconImporter.spriteImportMode=SpriteImportMode.Single;
        iconImporter.spritePixelsPerUnit=100;iconImporter.mipmapEnabled=false;
        iconImporter.npotScale=TextureImporterNPOTScale.None;iconImporter.alphaIsTransparency=true;
        iconImporter.textureCompression=TextureImporterCompression.Uncompressed;iconImporter.SaveAndReimport();
        for(int i=0;i<ids.Length;i++) {
            string path="Assets/Resources/RecoveredArt/"+paths[i];
            if(AssetDatabase.LoadAssetAtPath<Sprite>(path)==null)throw new InvalidDataException("Missing card sprite "+path);
            Map(map,ids[i],path);
        }
        string source=File.ReadAllText("ReferenceOriginal/Res/ViewPrefabs/UITreasureView.prefab").Replace("\r","");
        var blocks=new Dictionary<string,string>();
        foreach(Match m in Regex.Matches(source,@"^--- !u!\d+ &(\d+)\n.*?(?=^--- !u!|\z)",RegexOptions.Multiline|RegexOptions.Singleline))blocks.Add(m.Groups[1].Value,m.Value);
        var result=new StringBuilder("%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n");
        Append("224841364053301648","224841364053301648",blocks,result);
        string yaml=result.ToString();
        // Original no-geometry Graphic is retained as a transparent standard Button target.
        yaml=Regex.Replace(yaml,@"--- !u!114 &\d+\n.*?(?=\n--- !u!|\z)",m=>m.Value.Contains("e0b4d57e8f658b407a2ad25df85fb0d6")?
            m.Value.Replace("m_Color: {r: 1, g: 1, b: 1, a: 1}","m_Color: {r: 1, g: 1, b: 1, a: 0}"):m.Value,RegexOptions.Singleline);
        foreach(var p in map)yaml=yaml.Replace(p.Key,p.Value);
        foreach(string id in ids)yaml=yaml.Replace("guid: "+map[id]+", type: 2","guid: "+map[id]+", type: 3");
        foreach(Match m in Regex.Matches(yaml,@"guid: (\w+)"))if(string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(m.Groups[1].Value)))throw new InvalidDataException("Unmapped treasure GUID "+m.Groups[1].Value);
        File.WriteAllText(Path,yaml,new UTF8Encoding(false));AssetDatabase.ImportAsset(Path,ImportAssetOptions.ForceSynchronousImport);
        var root=PrefabUtility.LoadPrefabContents(Path);
        try {
            var settings=new SerializedObject(root.GetComponent<RecoveredTreasureCard>());
            if(!Regex.IsMatch(source,@"closeEase: 5\n  openEase: 27"))throw new InvalidDataException("Unexpected card easing");
            settings.FindProperty("closeCurve").animationCurveValue=new AnimationCurve(new Keyframe(0,0,0,0),new Keyframe(1,1,2,2));
            settings.FindProperty("openCurve").animationCurveValue=new AnimationCurve(
                new Keyframe(0,0,0,0),new Keyframe(4f/11,1,5.5f,-2.75f),
                new Keyframe(8f/11,1,2.75f,-1.375f),new Keyframe(10f/11,1,1.375f,-.6875f),new Keyframe(1,1,.6875f,.6875f));
            settings.ApplyModifiedPropertiesWithoutUndo();
            foreach(var button in root.GetComponentsInChildren<Button>(true))if(button.onClick.GetPersistentEventCount()!=0)throw new InvalidDataException("Unexpected persistent card button binding");
            PrefabUtility.SaveAsPrefabAsset(root,Path);
        } finally {PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();
    }
    private static void Append(string rect,string root,Dictionary<string,string> blocks,StringBuilder output)
    {
        string transform=blocks[rect],go=Regex.Match(transform,@"m_GameObject: \{fileID: (\d+)").Groups[1].Value;
        output.Append(blocks[go]);
        foreach(Match m in Regex.Matches(blocks[go],@"component: \{fileID: (\d+)")) {
            string id=m.Groups[1].Value,b=blocks[id];
            if(id==root)b=Regex.Replace(b,@"m_Father: \{fileID: \d+\}","m_Father: {fileID: 0}");
            output.Append(b);
        }
        string children=Regex.Match(transform,@"m_Children:\n(.*?)  m_Father:",RegexOptions.Singleline).Groups[1].Value;
        foreach(Match m in Regex.Matches(children,@"fileID: (\d+)"))Append(m.Groups[1].Value,root,blocks,output);
    }
    private static void Map(Dictionary<string,string> map,string id,string path)
    {string guid=AssetDatabase.AssetPathToGUID(path);if(string.IsNullOrEmpty(guid))throw new InvalidDataException(path);map[id]=guid;}
    private static void Script<T>(Dictionary<string,string> map,string id) where T:MonoBehaviour
    {
        var host=new GameObject("Script lookup",typeof(RectTransform));
        try {Map(map,id,AssetDatabase.GetAssetPath(MonoScript.FromMonoBehaviour(host.AddComponent<T>())));}
        finally {Object.DestroyImmediate(host);}
    }
}
