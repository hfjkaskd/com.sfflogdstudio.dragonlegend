using DragonLegend.Whitebox;
using UnityEditor;

public static class BuildCoreCashPrompt
{
    public static void Assign(SerializedObject settings)
    {
        settings.FindProperty("popupBaseDepth").intValue=300;
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
