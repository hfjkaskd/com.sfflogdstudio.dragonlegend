using System;
using System.IO;
using UnityEditor;
using UnityEngine;

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
}
