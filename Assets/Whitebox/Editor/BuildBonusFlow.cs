using DragonLegend.Whitebox;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

public static class BuildBonusFlow
{
    public static void Save()
    {
        const string fieldPath="Assets/Resources/RecoveredUI/SpinPlayfield.prefab";
        var field=PrefabUtility.LoadPrefabContents(fieldPath);
        try{
            AttachNpc(field);
            PrefabUtility.SaveAsPrefabAsset(field,fieldPath);
        }finally{PrefabUtility.UnloadPrefabContents(field);}
        var root=new GameObject("BonusFlow",typeof(RectTransform),typeof(RecoveredBonusFlow));
        try{
            var rect=(RectTransform)root.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.sizeDelta=Vector2.zero;
            var window=(RecoveredBonusWindow)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<RecoveredBonusWindow>("Assets/Resources/RecoveredUI/BonusWindow.prefab"),root.transform);
            var canvas=window.GetComponent<Canvas>();canvas.overrideSorting=true;canvas.sortingOrder=300;window.gameObject.SetActive(false);
            var exit=(RecoveredBonusExit)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<RecoveredBonusExit>("Assets/Resources/RecoveredUI/BonusExit.prefab"),root.transform);
            var binding=new SerializedObject(root.GetComponent<RecoveredBonusFlow>());binding.FindProperty("window").objectReferenceValue=window;binding.FindProperty("exit").objectReferenceValue=exit;
            binding.FindProperty("transitionPrefab").objectReferenceValue=AssetDatabase.LoadAssetAtPath<RecoveredSceneTransition>("Assets/Resources/RecoveredEffects/SceneTransition.prefab");binding.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root,"Assets/Resources/RecoveredUI/BonusFlow.prefab");
        }finally{Object.DestroyImmediate(root);}
        const string entryPath="Assets/Resources/Whitebox/GameEntry.prefab";var entry=PrefabUtility.LoadPrefabContents(entryPath);
        try{
            var binding=new SerializedObject(entry.GetComponent<GameEntry>());binding.FindProperty("bonusFlowPrefab").objectReferenceValue=AssetDatabase.LoadAssetAtPath<RecoveredBonusFlow>("Assets/Resources/RecoveredUI/BonusFlow.prefab");binding.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(entry,entryPath);AssetDatabase.SaveAssets();
        }finally{PrefabUtility.UnloadPrefabContents(entry);}
    }
    public static void AttachNpc(GameObject field)
    {
        var board=field.transform.Find("QiPan");
        var npc=field.GetComponentInChildren<RecoveredNpcPresentation>(true);
        if(npc==null){
            npc=(RecoveredNpcPresentation)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<RecoveredNpcPresentation>("Assets/Resources/RecoveredUI/Npc.prefab"),board);
            var rect=(RectTransform)npc.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.sizeDelta=Vector2.zero;rect.anchoredPosition=Vector2.zero;
            npc.transform.SetAsFirstSibling();
            var shake=new SerializedObject(npc.Shake);shake.FindProperty("target").objectReferenceValue=board;shake.ApplyModifiedPropertiesWithoutUndo();
        }
        var binding=new SerializedObject(field.GetComponent<RecoveredSpinPlayfield>());binding.FindProperty("npc").objectReferenceValue=npc;binding.ApplyModifiedPropertiesWithoutUndo();
    }
}
