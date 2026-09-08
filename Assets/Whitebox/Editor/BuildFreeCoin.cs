using DragonLegend.Whitebox;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

public static class BuildFreeCoin
{
    public static void Save()
    {
        BuildFreeSymbolArt.Save();
        var root=new GameObject("FreeCoin",typeof(SortingGroup));root.layer=5;root.SetActive(false);
        try {
            root.transform.localScale=Vector3.one*.7f;
            var settings=new SerializedObject(root.AddComponent<RecoveredFreeCoin>());
            var source=AssetDatabase.LoadAssetAtPath<RecoveredWorldAnimation>("Assets/Resources/RecoveredSymbols/FreeCoinArt.prefab");
            var art=(RecoveredWorldAnimation)PrefabUtility.InstantiatePrefab(source,root.transform);art.name="SkeletonGraphic (ef_qizijinli)";
            var glow=(RecoveredWorldAnimation)PrefabUtility.InstantiatePrefab(source,root.transform);glow.name="glow";
            var glowSettings=new SerializedObject(glow);glowSettings.FindProperty("initialClip").stringValue="__setup";
            glowSettings.ApplyModifiedPropertiesWithoutUndo();
            glow.GetComponentInChildren<MeshRenderer>().sortingOrder=1;
            var label=(RecoveredCoinRewardText)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<RecoveredCoinRewardText>("Assets/Resources/RecoveredUI/CoinRewardText.prefab"),root.transform);
            var text=label.Label;text.gameObject.SetActive(true);
            settings.FindProperty("art").objectReferenceValue=art;
            settings.FindProperty("glow").objectReferenceValue=glow;
            settings.FindProperty("reward").objectReferenceValue=text;
            Object.DestroyImmediate(label);
            settings.FindProperty("scaleDuration").floatValue=.2f;
            settings.FindProperty("peakScale").floatValue=1;
            settings.FindProperty("restingScale").floatValue=.7f;
            settings.ApplyModifiedPropertiesWithoutUndo();
            root.SetActive(true);PrefabUtility.SaveAsPrefabAsset(root,"Assets/Resources/RecoveredSymbols/FreeCoin.prefab");
            AssetDatabase.SaveAssets();
        } finally {Object.DestroyImmediate(root);}
    }
}
