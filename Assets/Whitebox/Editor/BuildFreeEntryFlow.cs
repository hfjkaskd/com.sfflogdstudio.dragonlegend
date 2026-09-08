using DragonLegend.Whitebox;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

public static class BuildFreeEntryFlow
{
    public static void Save()
    {
        var root=new GameObject("FreeEntryFlow",typeof(RectTransform),typeof(RecoveredFreeEntryFlow));root.layer=5;
        try {
            var rect=(RectTransform)root.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.sizeDelta=Vector2.zero;
            var popup=(RecoveredFreeStartPopup)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<RecoveredFreeStartPopup>("Assets/Resources/RecoveredUI/FreeStartPopup.prefab"),root.transform);
            popup.GetComponent<Canvas>().sortingOrder=300;popup.GetComponent<Canvas>().overrideSorting=true;popup.gameObject.SetActive(false);
            var s=new SerializedObject(root.GetComponent<RecoveredFreeEntryFlow>());s.FindProperty("window").objectReferenceValue=popup;
            s.FindProperty("transitionPrefab").objectReferenceValue=AssetDatabase.LoadAssetAtPath<RecoveredSceneTransition>("Assets/Resources/RecoveredEffects/SceneTransition.prefab");
            s.FindProperty("ringDuration").floatValue=1.8f;s.FindProperty("generationStepsPerFrame").intValue=256;s.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root,"Assets/Resources/RecoveredUI/FreeEntryFlow.prefab");AssetDatabase.SaveAssets();
        } finally {Object.DestroyImmediate(root);}
    }
}
