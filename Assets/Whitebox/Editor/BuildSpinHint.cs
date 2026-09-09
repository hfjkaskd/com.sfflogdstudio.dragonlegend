using DragonLegend.Whitebox;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

public static class BuildSpinHint
{
    public static void Save()
    {
        BuildJackpotPopupArt.Create("ef_shouzhi","shouzhi",new Vector2(160.88226f,159.71727f),Vector2.one*.5f,"Tools/Evidence/","Artifacts/SpinFingerAuthoring/");
        var root=new GameObject("Finger",typeof(RectTransform));root.layer=5;
        try {
            var rect=(RectTransform)root.transform;rect.sizeDelta=Vector2.one*100;
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/RecoveredUI/JackpotPopupArt/ef_shouzhi.prefab"),root.transform);
            PrefabUtility.SaveAsPrefabAsset(root,"Assets/Resources/RecoveredUI/Finger.prefab");
        } finally {Object.DestroyImmediate(root);}
        const string path="Assets/Resources/RecoveredUI/SpinPlayfield.prefab";
        var field=PrefabUtility.LoadPrefabContents(path);
        try {Attach(field);PrefabUtility.SaveAsPrefabAsset(field,path);AssetDatabase.SaveAssets();}
        finally {PrefabUtility.UnloadPrefabContents(field);}
    }
    public static void Attach(GameObject field)
    {
        var hint=field.GetComponent<RecoveredSpinHint>();if(hint==null)hint=field.AddComponent<RecoveredSpinHint>();
        var settings=new SerializedObject(hint);
        settings.FindProperty("fingerPrefab").objectReferenceValue=AssetDatabase.LoadAssetAtPath<RectTransform>("Assets/Resources/RecoveredUI/Finger.prefab");
        // Native SpineUtils lives below SpinBtn. The recovered component owns SpinBtn itself.
        settings.FindProperty("destination").objectReferenceValue=field.GetComponent<RecoveredSpinPlayfield>().SpinButton.transform;
        settings.FindProperty("delay").floatValue=2;settings.ApplyModifiedPropertiesWithoutUndo();
        settings=new SerializedObject(field.GetComponent<RecoveredSpinPlayfield>());
        settings.FindProperty("spinHint").objectReferenceValue=hint;settings.ApplyModifiedPropertiesWithoutUndo();
    }
}
