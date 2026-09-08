using System;
using System.IO;
using DragonLegend.Whitebox;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

public static class BuildFreeReels
{
    [Serializable] private sealed class Rectangle
    {
        public string name;
        public float x,y,width,height;
        public Rectangle[] columns,reels;
    }

    public static void Save()
    {
        var layout=JsonUtility.FromJson<Rectangle>(File.ReadAllText("Tools/Evidence/free-reel-layout.json"));
        const string miniPath="Assets/Resources/RecoveredSymbols/FreeMiniReel.prefab";
        var mini=PrefabUtility.LoadPrefabContents("Assets/Resources/RecoveredSymbols/Reel.prefab");
        try {
            mini.name="FreeMiniReel";
            var freeMotion=mini.AddComponent<RecoveredFreeReelMotion>();
            var motionSettings=new SerializedObject(freeMotion);
            motionSettings.FindProperty("movement").objectReferenceValue=mini.GetComponent<RecoveredBaseReelMotion>();
            motionSettings.FindProperty("reel").objectReferenceValue=mini.GetComponent<RecoveredReelView>();
            motionSettings.FindProperty("speedPixels").floatValue=5000;
            motionSettings.ApplyModifiedPropertiesWithoutUndo();
            mini.AddComponent<SortingGroup>();
            var settings=new SerializedObject(mini.GetComponent<RecoveredReelView>());
            // Native anchoredPosition starts at zero. SymbolItem anchors to the bottom
            // of the 172px-high node, so world geometry starts at -86px.
            settings.FindProperty("bottomPixels").floatValue=-layout.columns[0].reels[0].height*.5f;
            settings.FindProperty("effectClip").objectReferenceValue=mini.transform.Find("Clip").GetComponent<SpriteMask>();
            settings.ApplyModifiedPropertiesWithoutUndo();
            mini.transform.Find("Clip").localScale=new Vector3(layout.columns[0].reels[0].width,layout.columns[0].reels[0].height,1);
            PrefabUtility.SaveAsPrefabAsset(mini,miniPath);
        } finally { PrefabUtility.UnloadPrefabContents(mini); }

        var root=new GameObject("FreeReels",typeof(RecoveredFreeReels));
        try {
            var settings=new SerializedObject(root.GetComponent<RecoveredFreeReels>());
            var reels=settings.FindProperty("reels");reels.arraySize=15;
            var motions=settings.FindProperty("motions");motions.arraySize=15;
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(miniPath);
            for(int col=0;col<layout.columns.Length;col++) {
                var source=layout.columns[col];
                var column=new GameObject(source.name);column.transform.SetParent(root.transform,false);
                column.transform.localPosition=new Vector3(source.x*.01f,source.y*.01f,0);
                for(int row=0;row<source.reels.Length;row++) {
                    var cell=source.reels[row];
                    var reel=(GameObject)PrefabUtility.InstantiatePrefab(prefab,column.transform);
                    reel.name=cell.name;reel.transform.localPosition=new Vector3(cell.x*.01f,cell.y*.01f,0);
                    reels.GetArrayElementAtIndex(col*3+row).objectReferenceValue=reel.GetComponent<RecoveredReelView>();
                    motions.GetArrayElementAtIndex(col*3+row).objectReferenceValue=reel.GetComponent<RecoveredFreeReelMotion>();
                }
            }
            settings.ApplyModifiedPropertiesWithoutUndo();
            BuildFreeSpecials.Attach(root);
            PrefabUtility.SaveAsPrefabAsset(root,"Assets/Resources/RecoveredSymbols/FreeReels.prefab");
        } finally { Object.DestroyImmediate(root); }
        AssetDatabase.SaveAssets();
    }
}
