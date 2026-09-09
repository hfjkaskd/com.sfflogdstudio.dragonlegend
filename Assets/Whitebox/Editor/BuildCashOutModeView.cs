using DragonLegend.Whitebox;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class BuildCashOutModeView
{
    public static void Save()
    {
        const string path="Assets/Resources/RecoveredUI/CashOutWindow.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        try {Attach(root);PrefabUtility.SaveAsPrefabAsset(root,path);AssetDatabase.SaveAssets();}
        finally {PrefabUtility.UnloadPrefabContents(root);}
    }
    public static void Attach(GameObject root)
    {
        var view=root.GetComponent<RecoveredCashOutModeView>();if(view==null)view=root.AddComponent<RecoveredCashOutModeView>();
        var settings=new SerializedObject(view);var top=root.transform.Find("Content/Top");var node=root.transform.Find("Content/Node");
        var cash=top.Find("CashBtn");var gift=top.Find("GiftBtn");
        settings.FindProperty("cashButton").objectReferenceValue=cash.GetComponent<Button>();
        settings.FindProperty("giftButton").objectReferenceValue=gift.GetComponent<Button>();
        settings.FindProperty("cashSelection").objectReferenceValue=cash.GetChild(2).gameObject;
        settings.FindProperty("giftSelection").objectReferenceValue=gift.GetChild(2).gameObject;
        settings.FindProperty("cashRect").objectReferenceValue=node.Find("Rect").gameObject;
        settings.FindProperty("giftRect").objectReferenceValue=node.Find("GiftRect").gameObject;
        settings.FindProperty("bottom").objectReferenceValue=root.GetComponentInChildren<RecoveredCashOutBottom>(true).gameObject;
        settings.FindProperty("header").objectReferenceValue=root.GetComponent<RecoveredCashOutPaymentHeader>();
        settings.ApplyModifiedPropertiesWithoutUndo();
    }
}
