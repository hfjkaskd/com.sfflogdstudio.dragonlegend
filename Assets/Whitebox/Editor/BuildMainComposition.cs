using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using DragonLegend.Whitebox;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public static class BuildMainComposition
{
    public static void Save()
    {
        const string path="Assets/Resources/RecoveredUI/SpinPlayfield.prefab";
        var field=PrefabUtility.LoadPrefabContents(path);
        try {Attach(field);PrefabUtility.SaveAsPrefabAsset(field,path);AssetDatabase.SaveAssets();}
        finally {PrefabUtility.UnloadPrefabContents(field);}
    }
    public static void Attach(GameObject field)
    {
        const string temporary="Assets/Whitebox/Editor/MainBoardSource.prefab";
        var board=field.transform.Find("QiPan");
        var old=board.Find("Bg");if(old!=null)Object.DestroyImmediate(old.gameObject);
        string source=File.ReadAllText("ReferenceOriginal/Res/ViewPrefabs/UIMainView.prefab").Replace("\r","");
        var blocks=new Dictionary<string,string>();
        foreach(Match m in Regex.Matches(source,@"^--- !u!\d+ &(\d+)\n.*?(?=^--- !u!|\z)",RegexOptions.Multiline|RegexOptions.Singleline))blocks.Add(m.Groups[1].Value,m.Value);
        var yaml=new StringBuilder("%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n");
        BuildTreasureCard.Append("224208269818966246","224208269818966246",blocks,yaml);
        var map=new Dictionary<string,string>();BuildTreasureCard.Script<Image>(map,"3cf5a44414476512e00c3e7a2569a919");
        const string guid="e6e46f6773a726a4bbbcaa70e2f95c73";
        BuildTreasureCard.Map(map,guid,"Assets/Resources/RecoveredArt/Res/UI/zhujiemian/zjm_bg_qipan.png");
        string text=yaml.ToString();foreach(var pair in map)text=text.Replace(pair.Key,pair.Value);
        text=text.Replace("guid: "+map[guid]+", type: 2","guid: "+map[guid]+", type: 3");
        GameObject background;
        try {
            File.WriteAllText(temporary,text);AssetDatabase.ImportAsset(temporary,ImportAssetOptions.ForceSynchronousImport);
            background=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(temporary),board,false);background.name="Bg";
        } finally {AssetDatabase.DeleteAsset(temporary);}
        background.transform.SetSiblingIndex(1);
        var boardCanvas=background.AddComponent<Canvas>();boardCanvas.overrideSorting=true;boardCanvas.sortingOrder=-1;
        var npc=field.GetComponentInChildren<RecoveredNpcPresentation>(true);
        if(PrefabUtility.IsAnyPrefabInstanceRoot(npc.gameObject))
            PrefabUtility.UnpackPrefabInstance(npc.gameObject,PrefabUnpackMode.OutermostRoot,InteractionMode.AutomatedAction);
        var layer=npc.transform.Find("DragonLayer");
        var dragon=layer==null||layer.childCount==0?npc.transform.Find("SkeletonGraphic (ef_long) (1)"):layer.GetChild(0);
        var oldCanvas=dragon.GetComponent<Canvas>();if(oldCanvas!=null)Object.DestroyImmediate(oldCanvas);
        if(layer==null){
            var host=new GameObject("DragonLayer",typeof(RectTransform),typeof(Canvas));host.layer=5;
            layer=host.transform;layer.SetParent(npc.transform,false);layer.SetAsFirstSibling();
            var rect=(RectTransform)layer;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.sizeDelta=Vector2.zero;
        }
        dragon.SetParent(layer,false);
        var dragonCanvas=layer.GetComponent<Canvas>();
        dragonCanvas.overrideSorting=true;dragonCanvas.sortingOrder=-2;
        var composition=field.GetComponent<RecoveredMainComposition>();if(composition==null)composition=field.AddComponent<RecoveredMainComposition>();
        var settings=new SerializedObject(composition);
        settings.FindProperty("dragonCanvas").objectReferenceValue=dragonCanvas;settings.FindProperty("boardCanvas").objectReferenceValue=boardCanvas;
        settings.FindProperty("boardBackground").objectReferenceValue=background.GetComponent<Image>();settings.ApplyModifiedPropertiesWithoutUndo();
        settings=new SerializedObject(field.GetComponent<RecoveredSpinPlayfield>());settings.FindProperty("composition").objectReferenceValue=composition;settings.ApplyModifiedPropertiesWithoutUndo();
    }
}
