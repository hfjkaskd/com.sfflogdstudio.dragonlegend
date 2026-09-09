using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using DragonLegend.Whitebox;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class BuildCashOutBottom
{
    public static void Save()
    {
        var map=new Dictionary<string,string>();
        BuildTreasureCard.Script<Image>(map,"3cf5a44414476512e00c3e7a2569a919");BuildTreasureCard.Script<Button>(map,"18d0a90695249463551c00f45766e642");
        BuildTreasureCard.Script<TextMeshProUGUI>(map,"3f96b1d166d19b209697e35b35d65c76");BuildTreasureCard.Script<VerticalLayoutGroup>(map,"f18f2cc2fe71a3a6d76a570588d5047c");
        BuildTreasureCard.Script<LayoutElement>(map,"4293fd42559bc8bb1c81715179adf536");BuildTreasureCard.Script<ContentSizeFitter>(map,"21c7954052da7655d96bff866c2b7662");
        foreach(var pair in new Dictionary<string,string>{{"6fb47d078cca81c45ab9f8bd7454517b","tx_bg03"},{"a7967581a6eba844485ca407557feb89","tx_btn01"},{"bfd9dea221d36124f84a8ec587e997a5","tx_bg02"},{"d332c3bf0a5b1af498f053974e65e9cf","tx_btn_gou01"},{"f28026e4031a5784dbf8e35735635a38","tx_btn_gou02"}})
        {
            var files=Directory.GetFiles("Assets/Resources/RecoveredArt/IndividualSprites",pair.Value+"_*.png");if(files.Length!=1)throw new InvalidDataException(pair.Value);
            string path=files[0].Replace('\\','/');var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.spriteBorder=pair.Value=="tx_bg02"?new Vector4(36,8,38,40):Vector4.zero;importer.SaveAndReimport();BuildTreasureCard.Map(map,pair.Key,path);
        }
        string source=File.ReadAllText("ReferenceOriginal/Res/ViewPrefabs/UICashOutView.prefab").Replace("\r","");var blocks=new Dictionary<string,string>();
        foreach(Match m in Regex.Matches(source,@"^--- !u!\d+ &(\d+)\n.*?(?=^--- !u!|\z)",RegexOptions.Multiline|RegexOptions.Singleline))blocks.Add(m.Groups[1].Value,m.Value);
        var output=new StringBuilder("%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n");BuildTreasureCard.Append("224592460329389196","224592460329389196",blocks,output);string yaml=output.ToString();
        var removed=new List<string>();yaml=Regex.Replace(yaml,@"--- !u!114 &(\d+)\n.*?(?=\n--- !u!|\z)",m=>{if(m.Value.Contains("82882cd81b13d4d0e20dd6ac2839123e")){removed.Add(m.Groups[1].Value);return "";}return m.Value;},RegexOptions.Singleline);
        foreach(var id in removed)yaml=yaml.Replace("  - component: {fileID: "+id+"}\n","");foreach(var pair in map)yaml=yaml.Replace(pair.Key,pair.Value);
        yaml=Regex.Replace(yaml,@"(m_Sprite:.*)type: 2","$1type: 3");
        foreach(Match m in Regex.Matches(yaml,@"guid: (\w+)"))if(string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(m.Groups[1].Value)))throw new InvalidDataException("Missing bottom GUID "+m.Groups[1].Value);
        const string temp="Assets/Whitebox/Editor/CashBottomSource.prefab";File.WriteAllText(temp,yaml);AssetDatabase.ImportAsset(temp,ImportAssetOptions.ForceSynchronousImport);
        var root=PrefabUtility.LoadPrefabContents(temp);
        try
        {
            var view=root.AddComponent<RecoveredCashOutBottom>();var so=new SerializedObject(view);var layout=root.transform.Find("Layout");
            for(int i=1;i<=4;i++)so.FindProperty("line"+i).objectReferenceValue=layout.Find("Line"+i);
            so.FindProperty("button").objectReferenceValue=layout.Find("Line3/CtnBtn").GetComponent<Button>();
            so.FindProperty("taskText").objectReferenceValue=layout.Find("Line1").GetChild(0).GetComponent<TMP_Text>();so.FindProperty("timeText").objectReferenceValue=layout.Find("Line2").GetChild(0).GetComponent<TMP_Text>();so.FindProperty("detailText").objectReferenceValue=layout.Find("Line4").GetChild(0).GetComponent<TMP_Text>();
            so.FindProperty("taskCheck").objectReferenceValue=layout.Find("Line1/Image/Gou").gameObject;so.FindProperty("waitCheck").objectReferenceValue=layout.Find("Line2/Image/Gou").gameObject;
            foreach(var p in new Dictionary<string,string>{{"needPrefix","Need "},{"needMiddle"," more to withdraw "},{"readyText","You Can Cash Out Now!"},{"timeFormat","Pending Review {0:D2}:{1:D2}:{2:D2}"},{"refreshTimeFormat","{0:D2}:{1:D2}:{2:D2}"},{"expiredText","Pending Review 00:00:00"},{"incompleteText","Please complete the task first"},{"waitPrefix","Please wait for "},{"missingFormat","You might just earn <color=#32b555>{0}</color> more to withdraw"}})so.FindProperty(p.Key).stringValue=p.Value;
            var formats=so.FindProperty("taskFormats");var values=new[]{"Spin {0}/{1} times","Watch {0}/{1} ads","Claim {0}/{1} Jackpots","Claim {0}/{1} BigWins","Claim {0}/{1} Treasures","Play {0}/{1} FreeGames","Collect {0}/{1} Treasures"};formats.arraySize=values.Length;for(int i=0;i<values.Length;i++)formats.GetArrayElementAtIndex(i).stringValue=values[i];
            so.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(root,"Assets/Resources/RecoveredUI/CashOutBottom.prefab");AssetDatabase.SaveAssets();
        }
        finally{PrefabUtility.UnloadPrefabContents(root);AssetDatabase.DeleteAsset(temp);}
    }
}
