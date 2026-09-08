using DragonLegend.Whitebox;
using UnityEditor;
using UnityEngine;

public static class BuildBonusCashRewards
{
    public static void Save()
    {
        var root=new GameObject("BonusCashRewards",typeof(RectTransform),typeof(RecoveredBonusCashRewards));root.layer=5;
        try {
            var settings=new SerializedObject(root.GetComponent<RecoveredBonusCashRewards>());
            settings.FindProperty("popupDelay").floatValue=.2f;settings.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root,"Assets/Resources/RecoveredUI/BonusCashRewards.prefab");AssetDatabase.SaveAssets();
        }finally{Object.DestroyImmediate(root);}
    }
}
