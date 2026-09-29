using UnityEditor;
using UnityEngine;

public static class BuildFreeVisibility
{
    public static void Save()
    {
        const string path="Assets/Resources/RecoveredUI/SpinPlayfield.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        try {
            var host=root.transform.Find("QiPan/FreeRoll");
            foreach(var child in host.GetComponentsInChildren<Transform>(true))child.gameObject.layer=host.gameObject.layer;
            PrefabUtility.SaveAsPrefabAsset(root,path);AssetDatabase.SaveAssets();
        } finally {PrefabUtility.UnloadPrefabContents(root);}
    }
}
