using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using DragonLegend.Whitebox;

public static class BuildBonusWorld
{
    [Serializable] private sealed class Source { public Clip[] animations; }
    [Serializable] private sealed class Clip { public string name; }
    // Author the complete source clip set before connecting the gameplay prefab.
    public static void SaveClips()
    {
        const string sourceFolder="Assets/Whitebox/Editor";
        const string sourceFile="RecoveredCoinEffect";
        const string destination="Assets/Resources/RecoveredSymbols/BonusWorld";
        var source=JsonUtility.FromJson<Source>(File.ReadAllText(sourceFolder+"/"+sourceFile+".json"));
        foreach(var clip in source.animations)
        {
            string folder=destination+"/"+clip.name;
            Directory.CreateDirectory(folder);AssetDatabase.Refresh();
            var node=BuildWildWorld.Create(null,clip.name,sourceFile,"jinbi",folder,false,
                sourceFolder,clip.name,"RecoveredArt/Res/Spine/棋子/jinbi/ef_jinbi",false);
            try { PrefabUtility.SaveAsPrefabAsset(node,folder+"/"+clip.name+".prefab"); }
            finally { UnityEngine.Object.DestroyImmediate(node); }
        }
        AssetDatabase.SaveAssets();
    }
    public static void SaveAnimated()
    {
        const string folder="Assets/Resources/RecoveredSymbols/BonusWorld";
        var source=JsonUtility.FromJson<Source>(File.ReadAllText("Assets/Whitebox/Editor/RecoveredCoinEffect.json"));
        var template=AssetDatabase.LoadAssetAtPath<GameObject>(folder+"/zcjb_idle/zcjb_idle.prefab");
        var node=UnityEngine.Object.Instantiate(template);node.name="BonusWorld";node.SetActive(false);
        try
        {
            var player=node.GetComponent<Animation>();
            var driver=node.AddComponent<RecoveredWorldAnimation>();
            var settings=new SerializedObject(driver);
            settings.FindProperty("player").objectReferenceValue=player;
            settings.FindProperty("rig").objectReferenceValue=node.GetComponent<RecoveredWorldRig>();
            settings.FindProperty("initialClip").stringValue="zcjb_idle";
            var clips=settings.FindProperty("clips");clips.arraySize=source.animations.Length;
            for(int i=0;i<source.animations.Length;i++)
            {
                string name=source.animations[i].name;
                var animation=AssetDatabase.LoadAssetAtPath<AnimationClip>(folder+"/"+name+"/"+name+".anim");
                player.AddClip(animation,name);
                var clip=clips.GetArrayElementAtIndex(i);clip.FindPropertyRelative("name").stringValue=name;
                clip.FindPropertyRelative("data").objectReferenceValue=AssetDatabase.LoadAssetAtPath<RecoveredWorldRigData>(folder+"/"+name+"/RecoveredCoinEffect.asset");
            }
            settings.ApplyModifiedPropertiesWithoutUndo();node.SetActive(true);
            PrefabUtility.SaveAsPrefabAsset(node,folder+"/BonusWorld.prefab");AssetDatabase.SaveAssets();
        }
        finally {UnityEngine.Object.DestroyImmediate(node);}
    }
}
