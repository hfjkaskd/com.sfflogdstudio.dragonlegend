using DragonLegend.Whitebox;
using UnityEditor;
using UnityEngine;

public static class BuildMainSafeArea
{
    public static void Attach(GameObject field)
    {
        if(field.GetComponent<RecoveredScreenAdapt>()==null)field.AddComponent<RecoveredScreenAdapt>();
        var icons=field.transform.Find("MoreWildEntry");
        if(icons!=null){var duplicate=icons.GetComponent<RecoveredScreenAdapt>();if(duplicate!=null)Object.DestroyImmediate(duplicate);}
    }
    public static void Save()
    {
        const string path="Assets/Resources/RecoveredUI/SpinPlayfield.prefab";
        var field=PrefabUtility.LoadPrefabContents(path);
        try{Attach(field);PrefabUtility.SaveAsPrefabAsset(field,path);AssetDatabase.SaveAssets();}
        finally{PrefabUtility.UnloadPrefabContents(field);}
    }
}
