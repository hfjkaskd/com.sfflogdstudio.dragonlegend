using DragonLegend.Whitebox;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public static class BuildGiftList
{
    public static void Save()
    {
        var root=PrefabUtility.LoadPrefabContents("Assets/Resources/RecoveredUI/CashOutList.prefab");
        try {
            var cash=root.GetComponent<RecoveredCashOutList>();var frame=cash.SelectionFrame;var scroll=cash.Scroll;
            Object.DestroyImmediate(cash);Object.DestroyImmediate(frame.gameObject);Object.DestroyImmediate(root.GetComponentInChildren<RecoveredCashOutItem>(true).gameObject);
            var template=(RecoveredGiftItem)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<RecoveredGiftItem>("Assets/Resources/RecoveredUI/GiftItem.prefab"),root.transform);
            template.transform.localPosition=Vector3.one*9999;
            var view=root.AddComponent<RecoveredGiftList>();var settings=new SerializedObject(view);
            settings.FindProperty("scroll").objectReferenceValue=scroll;
            settings.FindProperty("template").objectReferenceValue=template;settings.FindProperty("cellSize").vector2Value=new Vector2(1080,310);
            settings.FindProperty("hidePosition").vector3Value=Vector3.one*9999;settings.FindProperty("movementThresholdSquared").floatValue=2;settings.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root,"Assets/Resources/RecoveredUI/GiftList.prefab");
        }finally{PrefabUtility.UnloadPrefabContents(root);}
        const string path="Assets/Resources/RecoveredUI/CashOutWindow.prefab";root=PrefabUtility.LoadPrefabContents(path);
        try{Attach(root);PrefabUtility.SaveAsPrefabAsset(root,path);AssetDatabase.SaveAssets();}
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    public static void Attach(GameObject root)
    {
        var parent=root.transform.Find("Content/Node/GiftRect");var old=parent.GetComponentInChildren<RecoveredGiftList>(true);if(old!=null)Object.DestroyImmediate(old.gameObject);
        var list=(RecoveredGiftList)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<RecoveredGiftList>("Assets/Resources/RecoveredUI/GiftList.prefab"),parent);
        list.name="GiftList";list.transform.localScale=Vector3.one;((RectTransform)list.transform).anchoredPosition=Vector2.zero;
        var settings=new SerializedObject(root.GetComponent<RecoveredCashOutModeView>());settings.FindProperty("giftList").objectReferenceValue=list;settings.ApplyModifiedPropertiesWithoutUndo();
        settings.FindProperty("cashList").objectReferenceValue=root.GetComponentInChildren<RecoveredCashOutList>(true);settings.ApplyModifiedPropertiesWithoutUndo();
    }
}
