using DragonLegend.Whitebox;
using UnityEditor;
using UnityEngine;

public static class BuildCoreCashPrompt
{
    public static void Assign(SerializedObject settings)
    {
        settings.FindProperty("popupBaseDepth").intValue=300;
        var owner=(RecoveredCoreRoundFlow)settings.targetObject;
        var group=owner.transform.Find("PopupRoot") as RectTransform;
        if(group==null)
        {
            var node=new GameObject("PopupRoot",typeof(RectTransform));node.layer=5;group=(RectTransform)node.transform;
            group.SetParent(owner.transform,false);group.anchorMin=Vector2.zero;group.anchorMax=Vector2.one;group.sizeDelta=Vector2.zero;
        }
        foreach(var field in new[]{"moreSpins","moreWild","bank"})
            ((Component)settings.FindProperty(field).objectReferenceValue).transform.SetParent(group,false);
        settings.FindProperty("popupRoot").objectReferenceValue=group;
        settings.FindProperty("cashPromptPrefab").objectReferenceValue=AssetDatabase.LoadAssetAtPath<RecoveredCashPromptWindow>("Assets/Resources/RecoveredUI/CashPromptWindow.prefab");
        settings.FindProperty("cashOutPrefab").objectReferenceValue=AssetDatabase.LoadAssetAtPath<RecoveredCashOutWindow>("Assets/Resources/RecoveredUI/CashOutWindow.prefab");
    }
    public static void Save()
    {
        const string path="Assets/Resources/RecoveredUI/CoreRoundFlow.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        try {
            var settings=new SerializedObject(root.GetComponent<RecoveredCoreRoundFlow>());
            Assign(settings);settings.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root,path);AssetDatabase.SaveAssets();
        } finally {PrefabUtility.UnloadPrefabContents(root);}
    }
}
