using DragonLegend.Whitebox;
using UnityEditor;
using UnityEngine;

public static class BuildFreeCoinReward
{
    public static void Save()
    {
        const string path="Assets/Resources/RecoveredSymbols/FreeCoin.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        try {Configure(root);PrefabUtility.SaveAsPrefabAsset(root,path);AssetDatabase.SaveAssets();}
        finally {PrefabUtility.UnloadPrefabContents(root);}
        BuildFreeReels.Save();
    }
    public static void Configure(GameObject root)
    {
        var coin=root.GetComponent<RecoveredFreeCoin>();
        var label=coin.Reward;var labelRoot=label.transform.parent.gameObject;
        var text=labelRoot.GetComponent<RecoveredCoinRewardText>();if(text==null)text=labelRoot.AddComponent<RecoveredCoinRewardText>();
        var original=AssetDatabase.LoadAssetAtPath<RecoveredCoinRewardText>("Assets/Resources/RecoveredUI/CoinRewardText.prefab");
        EditorUtility.CopySerialized(original,text);
        var settings=new SerializedObject(text);settings.FindProperty("label").objectReferenceValue=label;settings.ApplyModifiedPropertiesWithoutUndo();
        var reward=root.GetComponent<RecoveredFreeCoinReward>();if(reward==null)reward=root.AddComponent<RecoveredFreeCoinReward>();
        settings=new SerializedObject(reward);
        settings.FindProperty("coin").objectReferenceValue=coin;settings.FindProperty("rewardText").objectReferenceValue=text;
        settings.FindProperty("revealClip").stringValue="zcjb_b_chun";settings.FindProperty("idleClip").stringValue="idle_chun";
        settings.FindProperty("glowClip").stringValue="glow";settings.FindProperty("revealSound").stringValue="coinReveal";
        settings.FindProperty("revealSpeed").floatValue=3;settings.ApplyModifiedPropertiesWithoutUndo();
        settings=new SerializedObject(coin);settings.FindProperty("rewardPresentation").objectReferenceValue=reward;settings.ApplyModifiedPropertiesWithoutUndo();
    }
}
