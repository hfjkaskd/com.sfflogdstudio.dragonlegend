using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using DragonLegend.Whitebox;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public static class BuildMainBackground
{
    public static void Save()
    {
        string source=File.ReadAllText("ReferenceOriginal/Res/ViewPrefabs/UIMainView.prefab").Replace("\r","");
        var blocks=new Dictionary<string,string>();
        foreach(Match m in Regex.Matches(source,@"^--- !u!\d+ &(\d+)\n.*?(?=^--- !u!|\z)",RegexOptions.Multiline|RegexOptions.Singleline))blocks.Add(m.Groups[1].Value,m.Value);
        var map=new Dictionary<string,string>();BuildTreasureCard.Script<Image>(map,"3cf5a44414476512e00c3e7a2569a919");
        BuildTreasureCard.Map(map,"daecfff7d58f2de49aeabc06ece194cd","Assets/Resources/RecoveredArt/Res/UI/zhujiemian/zjm_bg.png");
        BuildTreasureCard.Map(map,"d4c44dc20add14f49a08a4c3e2aca6cd","Assets/Resources/RecoveredArt/Res/UI/free_game/mfyx_bg.png");
        string[] ids={"224271925019662038","224320592835458227"};
        const string temporary="Assets/Whitebox/Editor/MainBackgroundSource.prefab";
        const string output="Assets/Resources/RecoveredUI/MainBackground.prefab";
        var root=new GameObject("MainBackground",typeof(RectTransform),typeof(Canvas),typeof(RecoveredMainBackground));root.layer=5;
        try {
            var rect=(RectTransform)root.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.sizeDelta=Vector2.zero;
            var settings=new SerializedObject(root.GetComponent<RecoveredMainBackground>());
            var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.overrideSorting=true;canvas.sortingOrder=-1;
            settings.FindProperty("backgroundCanvas").objectReferenceValue=canvas;
            for(int i=0;i<ids.Length;i++) {
                var yaml=new StringBuilder("%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n");BuildTreasureCard.Append(ids[i],ids[i],blocks,yaml);
                string text=yaml.ToString();foreach(var pair in map)text=text.Replace(pair.Key,pair.Value);
                foreach(string id in new[]{"daecfff7d58f2de49aeabc06ece194cd","d4c44dc20add14f49a08a4c3e2aca6cd"})text=text.Replace("guid: "+map[id]+", type: 2","guid: "+map[id]+", type: 3");
                File.WriteAllText(temporary,text);AssetDatabase.ImportAsset(temporary,ImportAssetOptions.ForceSynchronousImport);
                var child=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(temporary),root.transform,false);
                child.name=i==0?"Bg":"FreeBg";child.SetActive(i==0);
                settings.FindProperty(i==0?"baseBackground":"freeBackground").objectReferenceValue=child.GetComponent<Image>();
                AssetDatabase.DeleteAsset(temporary);
            }
            settings.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(root,output);
        } finally {Object.DestroyImmediate(root);if(File.Exists(temporary))AssetDatabase.DeleteAsset(temporary);}
        const string mainPath="Assets/Resources/Whitebox/GameEntry.prefab";var main=PrefabUtility.LoadPrefabContents(mainPath);
        try {
            var settings=new SerializedObject(main.GetComponent<GameEntry>());
            settings.FindProperty("backgroundPrefab").objectReferenceValue=AssetDatabase.LoadAssetAtPath<RecoveredMainBackground>(output);
            settings.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(main,mainPath);AssetDatabase.SaveAssets();
        } finally {PrefabUtility.UnloadPrefabContents(main);}
    }
}
