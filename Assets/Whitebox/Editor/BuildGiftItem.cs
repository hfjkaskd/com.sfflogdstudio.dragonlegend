using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Globalization;
using DragonLegend.Whitebox;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class BuildGiftItem
{
    public static void Save()
    {
        var map=new Dictionary<string,string>();BuildTreasureCard.Script<Image>(map,"3cf5a44414476512e00c3e7a2569a919");
        BuildTreasureCard.Script<TextMeshProUGUI>(map,"3f96b1d166d19b209697e35b35d65c76");
        string source=File.ReadAllText("ReferenceOriginal/Res/Prefabs/GiftItem.prefab").Replace("\r","");
        foreach(var folder in new[]{"tixian","treasures"})foreach(string meta in Directory.GetFiles("ReferenceOriginal/Res/UI/"+folder,"*.asset.meta",SearchOption.AllDirectories))
        {
            string guid=Regex.Match(File.ReadAllText(meta),@"guid: (\w+)").Groups[1].Value;if(!source.Contains(guid))continue;
            string original=File.ReadAllText(meta.Substring(0,meta.Length-5));string name=Regex.Match(original,@"m_Name: (.*)").Groups[1].Value.Trim();
            var files=System.Array.FindAll(Directory.GetFiles("Assets/Resources/RecoveredArt/IndividualSprites",name+"_*.png"),path=>Regex.IsMatch(Path.GetFileName(path),"^"+Regex.Escape(name)+@"_-?\d+\.png$"));if(files.Length!=1)throw new InvalidDataException("Gift sprite "+name);
            string path=files[0].Replace('\\','/');var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            var border=Regex.Match(original,@"m_Border: \{x: ([^,]+), y: ([^,]+), z: ([^,]+), w: ([^}]+)\}");
            importer.spriteBorder=new Vector4(Parse(border.Groups[1].Value),Parse(border.Groups[2].Value),Parse(border.Groups[3].Value),Parse(border.Groups[4].Value));
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();BuildTreasureCard.Map(map,guid,path);
        }
        var removed=new List<string>();
        source=Regex.Replace(source,@"--- !u!114 &(\d+)\n.*?(?=\n--- !u!|\z)",m=>{var script=Regex.Match(m.Value,@"m_Script:.*guid: (\w+)");if(script.Success&&!map.ContainsKey(script.Groups[1].Value)){removed.Add(m.Groups[1].Value);return "";}return m.Value;},RegexOptions.Singleline);
        foreach(string id in removed)source=source.Replace("  - component: {fileID: "+id+"}\n","");
        foreach(var pair in map)source=source.Replace(pair.Key,pair.Value);
        source=Regex.Replace(source,@"(m_Sprite:.*)type: 2","$1type: 3");
        foreach(Match m in Regex.Matches(source,@"guid: (\w+)"))if(string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(m.Groups[1].Value)))throw new InvalidDataException("Gift GUID "+m.Groups[1].Value);
        const string temp="Assets/Whitebox/Editor/GiftItemSource.prefab";File.WriteAllText(temp,source);AssetDatabase.ImportAsset(temp,ImportAssetOptions.ForceSynchronousImport);
        var root=PrefabUtility.LoadPrefabContents(temp);
        try {
            var item=root.AddComponent<RecoveredGiftItem>();var settings=new SerializedObject(item);
            foreach(var pair in new Dictionary<string,string>{{"rewardText","Green/Text (TMP)"},{"nameText","1/Text (TMP)"},{"alternateNameText","2/Text (TMP) (1)"},{"progressText","1/Progress/Text (TMP)"}})settings.FindProperty(pair.Key).objectReferenceValue=root.transform.Find(pair.Value).GetComponent<TMP_Text>();
            settings.FindProperty("fill").objectReferenceValue=root.transform.Find("1/Progress/Fill");
            settings.FindProperty("displayName").stringValue="Amazon";settings.FindProperty("progressFormat").stringValue="{0}/{1}";
            settings.FindProperty("id").intValue=0;settings.FindProperty("progressWidth").floatValue=715;settings.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root,"Assets/Resources/RecoveredUI/GiftItem.prefab");AssetDatabase.SaveAssets();
        }finally{PrefabUtility.UnloadPrefabContents(root);AssetDatabase.DeleteAsset(temp);}
    }
    private static float Parse(string value)=>float.Parse(value,CultureInfo.InvariantCulture);
}
