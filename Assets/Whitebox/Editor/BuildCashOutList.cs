using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using DragonLegend.Whitebox;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class BuildCashOutList
{
    public static void Save()
    {
        const string output="Assets/Resources/RecoveredUI/CashOutList.prefab";
        var map=new Dictionary<string,string>();BuildTreasureCard.Script<Image>(map,"3cf5a44414476512e00c3e7a2569a919");BuildTreasureCard.Script<ScrollRect>(map,"2cbaf7f938a676aaf8fab3181b92a517");BuildTreasureCard.Script<RectMask2D>(map,"69dacd7a039c12e90cde90bbae65247a");
        string yaml=File.ReadAllText("ReferenceOriginal/Res/Prefabs/ListView.prefab");foreach(var pair in map)yaml=yaml.Replace(pair.Key,pair.Value);File.WriteAllText(output,yaml);AssetDatabase.ImportAsset(output,ImportAssetOptions.ForceSynchronousImport);
        string source=File.ReadAllText("ReferenceOriginal/Res/ViewPrefabs/UICashOutView.prefab").Replace("\r","");var blocks=new Dictionary<string,string>();
        foreach(Match m in Regex.Matches(source,@"^--- !u!\d+ &(\d+)\n.*?(?=^--- !u!|\z)",RegexOptions.Multiline|RegexOptions.Singleline))blocks.Add(m.Groups[1].Value,m.Value);
        var text=new StringBuilder("%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n");BuildTreasureCard.Append("224187454977326620","224187454977326620",blocks,text);
        string frameSprite=Directory.GetFiles("Assets/Resources/RecoveredArt/IndividualSprites","tx_bak_00_*.png")[0].Replace('\\','/');var importer=(TextureImporter)AssetImporter.GetAtPath(frameSprite);importer.spriteBorder=new Vector4(37,39,38,35);importer.SaveAndReimport();BuildTreasureCard.Map(map,"c83ecaef5d7ab1e44a7e7fae03ad1d10",frameSprite);
        yaml=text.ToString();foreach(var pair in map)yaml=yaml.Replace(pair.Key,pair.Value);yaml=Regex.Replace(yaml,@"(m_Sprite:.*)type: 2","$1type: 3");
        const string framePath="Assets/Whitebox/Editor/CashFrameSource.prefab";File.WriteAllText(framePath,yaml);AssetDatabase.ImportAsset(framePath,ImportAssetOptions.ForceSynchronousImport);
        var root=PrefabUtility.LoadPrefabContents(output);
        try
        {
            var scroll=root.GetComponent<ScrollRect>();scroll.horizontal=false;scroll.vertical=true;
            var template=(RecoveredCashOutItem)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<RecoveredCashOutItem>("Assets/Resources/RecoveredUI/CashOutItem.prefab"),root.transform);template.transform.localPosition=Vector3.one*9999;
            var frame=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(framePath),root.transform);PrefabUtility.UnpackPrefabInstance(frame,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            var controller=root.AddComponent<RecoveredCashOutList>();var settings=new SerializedObject(controller);
            settings.FindProperty("scroll").objectReferenceValue=scroll;settings.FindProperty("template").objectReferenceValue=template;settings.FindProperty("selectionFrame").objectReferenceValue=frame.transform;
            settings.FindProperty("cellSize").vector2Value=new Vector2(1080,310);settings.FindProperty("hidePosition").vector3Value=Vector3.one*9999;settings.FindProperty("movementThresholdSquared").floatValue=2;
            settings.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(root,output);AssetDatabase.SaveAssets();
        }
        finally{PrefabUtility.UnloadPrefabContents(root);AssetDatabase.DeleteAsset(framePath);}
    }
}
