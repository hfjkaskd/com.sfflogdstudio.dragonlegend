using DragonLegend.Whitebox;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

public static class BuildFreeBall
{
    public static void Save()
    {
        var root=new GameObject("FreeBall",typeof(SortingGroup));root.layer=5;root.SetActive(false);
        try {
            root.transform.localScale=Vector3.one*.8f;
            var settings=new SerializedObject(root.AddComponent<RecoveredFreeBall>());
            settings.FindProperty("flightDuration").floatValue=.3f;
            settings.FindProperty("flightArcRatio").floatValue=.3f;
            settings.FindProperty("activationDelay").floatValue=.7f;
            var art=(RecoveredWorldAnimation)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<RecoveredWorldAnimation>("Assets/Resources/RecoveredSymbols/FreeBallArt.prefab"),root.transform);
            art.name="SkeletonGraphic (ef_longzhu)";
            var artSettings=new SerializedObject(art);artSettings.FindProperty("initialClip").stringValue="huo_lan";
            artSettings.ApplyModifiedPropertiesWithoutUndo();
            var label=(RecoveredCoinRewardText)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<RecoveredCoinRewardText>("Assets/Resources/RecoveredUI/CoinRewardText.prefab"),root.transform);
            var text=label.Label;text.gameObject.SetActive(true);text.rectTransform.anchoredPosition=Vector2.zero;
            text.rectTransform.sizeDelta=new Vector2(160,30);text.resizeTextMaxSize=40;text.text="$8.99";
            PrefabUtility.RecordPrefabInstancePropertyModifications(text);
            PrefabUtility.RecordPrefabInstancePropertyModifications(text.rectTransform);
            settings.FindProperty("art").objectReferenceValue=art;settings.FindProperty("reward").objectReferenceValue=text;
            Object.DestroyImmediate(label);
            string[] suffixes={"zi","lan","lv"};var types=settings.FindProperty("types");types.arraySize=3;
            for(int i=0;i<3;i++) {
                var type=types.GetArrayElementAtIndex(i);type.FindPropertyRelative("idle").stringValue="idle_"+suffixes[i];
                type.FindPropertyRelative("start").stringValue="start_"+suffixes[i];
                type.FindPropertyRelative("activate").stringValue="huo_"+suffixes[i];
            }
            settings.ApplyModifiedPropertiesWithoutUndo();
            var presentation=root.AddComponent<RecoveredFreeBallReward>();var rewardSettings=new SerializedObject(presentation);
            rewardSettings.FindProperty("label").objectReferenceValue=root.GetComponent<RecoveredFreeBall>().Reward;
            rewardSettings.FindProperty("delay").floatValue=.1f;rewardSettings.FindProperty("duration").floatValue=.3f;
            rewardSettings.FindProperty("scaleDuration").floatValue=.2f;rewardSettings.FindProperty("peakScale").floatValue=1.2f;
            rewardSettings.FindProperty("restingScale").floatValue=1;rewardSettings.ApplyModifiedPropertiesWithoutUndo();
            settings=new SerializedObject(root.GetComponent<RecoveredFreeBall>());settings.FindProperty("rewardPresentation").objectReferenceValue=presentation;
            settings.ApplyModifiedPropertiesWithoutUndo();
            BuildFreeClipping.Configure(root);
            root.SetActive(true);PrefabUtility.SaveAsPrefabAsset(root,"Assets/Resources/RecoveredSymbols/FreeBall.prefab");AssetDatabase.SaveAssets();
        } finally {Object.DestroyImmediate(root);}
    }
}
