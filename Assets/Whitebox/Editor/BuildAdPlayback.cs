using DragonLegend.Whitebox;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class BuildAdPlayback
{
    public static void Save()
    {
        const string path="Assets/Resources/Whitebox/GameEntry.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            var controls=root.GetComponentInChildren<RecoveredAdSimulationControls>(true);
            var settings=new SerializedObject(controls);
            settings.FindProperty("automaticCompletion").boolValue=true;
            settings.FindProperty("simulatedDuration").floatValue=2;
            settings.FindProperty("tipsPrefab").objectReferenceValue=AssetDatabase.LoadAssetAtPath<RecoveredTipsWindow>("Assets/Resources/RecoveredUI/TipsWindow.prefab");
            settings.FindProperty("startedText").stringValue="Simulated ad started";
            settings.FindProperty("completedText").stringValue="Simulated ad completed";
            settings.FindProperty("failedText").stringValue="Ad failed - please try again";
            settings.FindProperty("interstitialStartedText").stringValue="Simulated interstitial started";
            settings.FindProperty("interstitialCompletedText").stringValue="Simulated interstitial completed";
            settings.ApplyModifiedPropertiesWithoutUndo();
            controls.RewardButton.GetComponentInChildren<Text>(true).text="Test ad: finish now";
            controls.FailureButton.GetComponentInChildren<Text>(true).text="Test ad: simulate failure";
            PrefabUtility.SaveAsPrefabAsset(root,path);AssetDatabase.SaveAssets();
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
}
