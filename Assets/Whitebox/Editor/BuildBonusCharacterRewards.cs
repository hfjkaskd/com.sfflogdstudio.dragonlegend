using DragonLegend.Whitebox;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

public static class BuildBonusCharacterRewards
{
    public static void Save()
    {
        // The reel variant applies .01 for world reel coordinates. Bonus spawns
        // under its original UI item, preserving tuowei's original local pose.
        var flight=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/RecoveredSymbols/LampFlight.prefab"));
        try {
            flight.name="BonusFlight";flight.transform.localPosition=new Vector3(-1.87f,-2.1f,0);flight.transform.localScale=Vector3.one;
            PrefabUtility.SaveAsPrefabAsset(flight,"Assets/Resources/RecoveredUI/BonusFlight.prefab");
        }finally{Object.DestroyImmediate(flight);}
        var root=new GameObject("BonusCharacterRewards",typeof(RectTransform),typeof(RecoveredBonusCharacterRewards));root.layer=5;
        try {
            var settings=new SerializedObject(root.GetComponent<RecoveredBonusCharacterRewards>());
            settings.FindProperty("flightPrefab").objectReferenceValue=AssetDatabase.LoadAssetAtPath<RecoveredLampFlight>("Assets/Resources/RecoveredUI/BonusFlight.prefab");
            settings.FindProperty("flashPrefab").objectReferenceValue=AssetDatabase.LoadAssetAtPath<RecoveredLampFlash>("Assets/Resources/RecoveredUI/LampFlash.prefab");
            settings.FindProperty("jackpotDelay").floatValue=1.5f;settings.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root,"Assets/Resources/RecoveredUI/BonusCharacterRewards.prefab");AssetDatabase.SaveAssets();
        }finally{Object.DestroyImmediate(root);}
    }
}
