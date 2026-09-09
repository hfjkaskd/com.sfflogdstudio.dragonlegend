using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using TMPro;
using DragonLegend.Whitebox;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class BuildCashOutItem
{
    const string Folder="Assets/Resources/RecoveredUI/CashOutItemArt";
    public static void Save()
    {
        Directory.CreateDirectory(Folder);
        var map=new Dictionary<string,string>();
        BuildTreasureCard.Script<Image>(map,"3cf5a44414476512e00c3e7a2569a919");
        BuildTreasureCard.Script<Button>(map,"18d0a90695249463551c00f45766e642");
        BuildTreasureCard.Script<TextMeshProUGUI>(map,"3f96b1d166d19b209697e35b35d65c76");
        var fontProbe=ScriptableObject.CreateInstance<TMP_FontAsset>();try{map["eef129d5e40c07af534a8e24da6b5c16"]=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(fontProbe)));}finally{Object.DestroyImmediate(fontProbe);}
        BuildTreasureCard.Map(map,"17a01df8ef8f5404eb09f6c4c8e3f0bb","Assets/Resources/RecoveredArt/Res/Font/msyhbd Atlas.png");
        BuildTreasureCard.Map(map,"f411b38e2e8dc7c43a9f5969cbf1e5b8","Assets/Resources/RecoveredArt/Res/Font/msyhbd.ttf");
        BuildTreasureCard.Map(map,"a495248e59fa6714793dae46034b0996",AssetDatabase.GetAssetPath(Shader.Find("TextMeshPro/Distance Field")));
        foreach(string name in new[]{"msyhbd SDF.asset","msyhbd Atlas Material.mat"})
        {
            string text=File.ReadAllText("ReferenceOriginal/Res/Font/"+name);
            foreach(var pair in map)text=text.Replace(pair.Key,pair.Value);
            File.WriteAllText(Folder+"/"+name,text);
            if(!File.Exists(Folder+"/"+name+".meta"))File.Copy("ReferenceOriginal/Res/Font/"+name+".meta",Folder+"/"+name+".meta");
        }
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        foreach(var pair in new Dictionary<string,string>{{"822980e6139ec634894588a92a1e6156","tx_bak_01"},{"2d39f6264686a6f498145cd0c362bd38","tx_icon_01"},{"5ff2838999cac104081c5e1293a8d19a","tx_bar_03"},{"d1ad976336cb2664a93492918178644e","tx_bar_04"},{"30f6a4894c7dfb3418ff2598011fe030","tx_progressing02"}})
        {
            var files=Directory.GetFiles("Assets/Resources/RecoveredArt/IndividualSprites",pair.Value+"_*.png");
            if(files.Length!=1)throw new InvalidDataException("Ambiguous cash item sprite: "+pair.Value);
            string path=files[0].Replace('\\','/');var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            if(pair.Value=="tx_bar_03")importer.spriteBorder=new Vector4(24,0,24,0);
            if(pair.Value=="tx_bar_04")importer.spriteBorder=new Vector4(18,0,18,0);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            BuildTreasureCard.Map(map,pair.Key,path);
        }
        string source=File.ReadAllText("ReferenceOriginal/Res/Prefabs/CashOutItem.prefab").Replace("\r","");var removed=new List<string>();
        source=Regex.Replace(source,@"--- !u!\d+ &(\d+)\n.*?(?=\n--- !u!|\z)",m=>{var script=Regex.Match(m.Value,@"m_Script:.*guid: (\w+)");if(script.Success&&!map.ContainsKey(script.Groups[1].Value)){removed.Add(m.Groups[1].Value);return "";}return m.Value;},RegexOptions.Singleline);
        foreach(var id in removed)source=source.Replace("  - component: {fileID: "+id+"}\n","");
        foreach(var pair in map)source=source.Replace(pair.Key,pair.Value);
        source=Regex.Replace(source,@"(m_Sprite:.*)type: 2","$1type: 3");
        foreach(Match m in Regex.Matches(source,@"guid: (\w+)"))if(string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(m.Groups[1].Value)))throw new InvalidDataException("Missing cash item GUID "+m.Groups[1].Value);
        const string temp="Assets/Whitebox/Editor/CashOutItemSource.prefab";File.WriteAllText(temp,source);AssetDatabase.ImportAsset(temp,ImportAssetOptions.ForceSynchronousImport);
        var root=PrefabUtility.LoadPrefabContents(temp);
        try
        {
            var button=root.transform.Find("Btn").GetComponent<Button>();
            var children=new List<Transform>();foreach(Transform child in root.transform)if(child!=button.transform)children.Add(child);
            // Preserve authored positions while placing all card visuals under its standard Button.
            foreach(var child in children)child.SetParent(button.transform,true);
            var view=root.AddComponent<RecoveredCashOutItem>();var settings=new SerializedObject(view);
            settings.FindProperty("button").objectReferenceValue=button;
            foreach(var pair in new Dictionary<string,string>{{"background","Image"},{"icon","Img"}})settings.FindProperty(pair.Key).objectReferenceValue=button.transform.Find(pair.Value).GetComponent<Image>();
            foreach(var pair in new Dictionary<string,string>{{"amountText","Text (TMP)"},{"progressText","Progress/Text (TMP)"},{"taskTip","Task/tip/Text (TMP)"},{"detailText","Detail/Text (TMP) (1)"}})settings.FindProperty(pair.Key).objectReferenceValue=button.transform.Find(pair.Value).GetComponent<TMP_Text>();
            settings.FindProperty("taskText").objectReferenceValue=button.transform.Find("Task").GetChild(1).GetComponent<TMP_Text>();
            settings.FindProperty("timeText").objectReferenceValue=button.transform.Find("Task").GetChild(2).GetComponent<TMP_Text>();
            foreach(var pair in new Dictionary<string,string>{{"progress","Progress"},{"fill","Progress/Fill"},{"task","Task"}})settings.FindProperty(pair.Key).objectReferenceValue=button.transform.Find(pair.Value);
            settings.FindProperty("progressWidth").floatValue=726;
            settings.FindProperty("progressing").stringValue="Progressing";
            settings.FindProperty("timeFormat").stringValue="Pending Review {0:D2}:{1:D2}:{2:D2}";
            settings.FindProperty("expiredInitial").stringValue="Pending Review00:00:00";settings.FindProperty("expiredRefresh").stringValue="Pending Review 00:00:00";
            var formats=settings.FindProperty("taskFormats");var descriptions=new[]{"Spin {0}/{1} times","Watch {0}/{1} ads","Claim {0}/{1} Jackpots","Claim {0}/{1} BigWins","Claim {0}/{1} Treasures","Play {0}/{1} FreeGames","Collect {0}/{1} Treasures"};formats.arraySize=descriptions.Length;for(int i=0;i<descriptions.Length;i++)formats.GetArrayElementAtIndex(i).stringValue=descriptions[i];
            foreach(var kind in new[]{"backgroundPaths","iconPaths"})
            {
                var paths=settings.FindProperty(kind);paths.arraySize=5;
                for(int i=0;i<5;i++)
                {
                    string prefix=(kind=="backgroundPaths"?"tx_bak_0":"tx_icon_0")+i;var files=Directory.GetFiles("Assets/Resources/RecoveredArt/IndividualSprites",prefix+"_*.png");
                    if(files.Length==0){paths.GetArrayElementAtIndex(i).stringValue="";continue;}if(files.Length!=1)throw new InvalidDataException(prefix);
                    string path=files[0].Replace('\\','/');var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
                    paths.GetArrayElementAtIndex(i).stringValue=path.Substring("Assets/Resources/".Length).Replace(".png","");
                }
            }
            settings.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root,"Assets/Resources/RecoveredUI/CashOutItem.prefab");AssetDatabase.SaveAssets();
        }
        finally{PrefabUtility.UnloadPrefabContents(root);AssetDatabase.DeleteAsset(temp);}
        // An unquoted empty first sequence entry is lost on nested prefab import
        // after trailing whitespace normalization. Preserve the explicit empty string.
        const string output="Assets/Resources/RecoveredUI/CashOutItem.prefab";
        File.WriteAllText(output,Regex.Replace(File.ReadAllText(output),@"(  iconPaths:\r?\n)  -[ \t]*\r?\n","$1  - ''\n"));
        AssetDatabase.ImportAsset(output,ImportAssetOptions.ForceSynchronousImport);
    }
}
