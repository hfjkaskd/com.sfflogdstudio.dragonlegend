using DragonLegend.Whitebox;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class BuildFreeClipping
{
    public static void Save()
    {
        Attach("Assets/Resources/RecoveredSymbols/FreeCoin.prefab");
        Attach("Assets/Resources/RecoveredSymbols/FreeBall.prefab");
        BuildFreeReels.Save();
        AssetDatabase.SaveAssets();
    }
    private static void Attach(string path)
    {
        var root=PrefabUtility.LoadPrefabContents(path);
        try {Configure(root);PrefabUtility.SaveAsPrefabAsset(root,path);}
        finally {PrefabUtility.UnloadPrefabContents(root);}
    }
    public static void Configure(GameObject root)
    {
        var clipping=root.GetComponent<RecoveredWorldRectClip>();
        if(clipping==null)clipping=root.AddComponent<RecoveredWorldRectClip>();
        var data=new SerializedObject(clipping);
        var meshes=root.GetComponentsInChildren<MeshRenderer>(true);var meshArray=data.FindProperty("meshes");meshArray.arraySize=meshes.Length;
        for(int i=0;i<meshes.Length;i++)meshArray.GetArrayElementAtIndex(i).objectReferenceValue=meshes[i];
        var labels=root.GetComponentsInChildren<Text>(true);var texts=data.FindProperty("labels");texts.arraySize=labels.Length;
        var frames=data.FindProperty("canvasFrames");frames.arraySize=labels.Length;
        for(int i=0;i<labels.Length;i++) {
            texts.GetArrayElementAtIndex(i).objectReferenceValue=labels[i];
            frames.GetArrayElementAtIndex(i).objectReferenceValue=labels[i].GetComponentInParent<Canvas>(true).rootCanvas.transform;
        }
        data.ApplyModifiedPropertiesWithoutUndo();
        Component owner=root.GetComponent<RecoveredFreeCoin>();if(owner==null)owner=root.GetComponent<RecoveredFreeBall>();
        data=new SerializedObject(owner);data.FindProperty("clipping").objectReferenceValue=clipping;data.ApplyModifiedPropertiesWithoutUndo();
    }
}
