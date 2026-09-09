using DragonLegend.Whitebox;
using UnityEditor;
using UnityEngine;

public static class BuildFreeExitFlow
{
    public static void Save()
    {
        BuildFreeEndWindow.Save();
        var root=new GameObject("FreeExitFlow",typeof(RectTransform),typeof(RecoveredFreeExitFlow));root.layer=5;
        try {
            var rect=(RectTransform)root.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.sizeDelta=Vector2.zero;
            var window=(RecoveredFreeEndWindow)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<RecoveredFreeEndWindow>("Assets/Resources/RecoveredUI/FreeEndWindow.prefab"),root.transform);
            var data=new SerializedObject(root.GetComponent<RecoveredFreeExitFlow>());
            data.FindProperty("window").objectReferenceValue=window;
            data.FindProperty("transitionPrefab").objectReferenceValue=AssetDatabase.LoadAssetAtPath<RecoveredSceneTransition>("Assets/Resources/RecoveredEffects/SceneTransition.prefab");
            data.FindProperty("generationStepsPerFrame").intValue=256;data.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root,"Assets/Resources/RecoveredUI/FreeExitFlow.prefab");AssetDatabase.SaveAssets();
        } finally {Object.DestroyImmediate(root);}
    }
}
