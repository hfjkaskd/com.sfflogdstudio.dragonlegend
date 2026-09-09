using DragonLegend.Whitebox;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

public static class BuildCoreRoundFlow
{
    public static void Save()
    {
        const string path="Assets/Resources/RecoveredUI/CoreRoundFlow.prefab";
        var root=new GameObject("CoreRoundFlow",typeof(RectTransform),typeof(RecoveredCoreRoundFlow));root.layer=5;
        try {
            var rect=(RectTransform)root.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.sizeDelta=Vector2.zero;
            var settings=new SerializedObject(root.GetComponent<RecoveredCoreRoundFlow>());
            Add<RecoveredFreeEntryFlow>(settings,"entry","FreeEntryFlow",root.transform);
            Add<RecoveredFreeExitFlow>(settings,"exit","FreeExitFlow",root.transform);
            Add<RecoveredFreeSlotGame>(settings,"slot","FreeSlotGame",root.transform);
            Add<RecoveredFreeWheelGame>(settings,"wheel","FreeWheelGame",root.transform);
            Add<RecoveredFreeTreasureGame>(settings,"treasure","FreeTreasureGame",root.transform);
            Add<RecoveredFreeLuckyGame>(settings,"lucky","FreeLuckyGame",root.transform);
            Add<RecoveredMoreSpinWindow>(settings,"moreSpins","MoreSpinWindow",root.transform);
            Add<RecoveredTipsWindow>(settings,"tips","TipsWindow",root.transform);
            settings.FindProperty("moreSpinLimitMessage").stringValue="The ad isn't ready yet, please wait.";
            var target=new GameObject("LongzhuPos",typeof(RectTransform));target.layer=5;target.transform.SetParent(root.transform,false);
            var destination=(RectTransform)target.transform;destination.anchorMin=destination.anchorMax=new Vector2(.5f,0);
            destination.anchoredPosition=new Vector2(-.003418f-1.6201172f,652+100);destination.sizeDelta=new Vector2(100,100);
            settings.FindProperty("ballDestination").objectReferenceValue=destination;settings.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root,path);
        } finally {Object.DestroyImmediate(root);}
        const string main="Assets/Resources/Whitebox/GameEntry.prefab";var game=PrefabUtility.LoadPrefabContents(main);
        try {
            var settings=new SerializedObject(game.GetComponent<GameEntry>());settings.FindProperty("coreRoundPrefab").objectReferenceValue=AssetDatabase.LoadAssetAtPath<RecoveredCoreRoundFlow>(path);
            settings.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(game,main);AssetDatabase.SaveAssets();
        } finally {PrefabUtility.UnloadPrefabContents(game);}
    }
    private static void Add<T>(SerializedObject settings,string field,string name,Transform parent) where T:Component
    {
        var prefab=AssetDatabase.LoadAssetAtPath<T>("Assets/Resources/RecoveredUI/"+name+".prefab");
        settings.FindProperty(field).objectReferenceValue=PrefabUtility.InstantiatePrefab(prefab,parent);
    }
}
