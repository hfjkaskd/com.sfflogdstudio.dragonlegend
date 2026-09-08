using DragonLegend.Whitebox;
using UnityEditor;
using UnityEngine;

// Asset authoring only: layout and timings are persisted in the native prefab.
public static class BuildBaseReelController
{
    public static void Save()
    {
        var root = new GameObject("BaseReels");
        try {
            var controller = root.AddComponent<RecoveredBaseReelController>();
            var data = new SerializedObject(controller);
            var views = data.FindProperty("reels"); var motions = data.FindProperty("motions");
            views.arraySize = 5; motions.arraySize = 5;
            var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/RecoveredSymbols/Reel.prefab");
            for (int i = 0; i < 5; i++) {
                var child = (GameObject)PrefabUtility.InstantiatePrefab(source, root.transform);
                child.name = "Reel" + i;
                child.transform.localPosition = new Vector3(-3.8f + 1.9f * i, -0.01f, 0);
                views.GetArrayElementAtIndex(i).objectReferenceValue = child.GetComponent<RecoveredReelView>();
                motions.GetArrayElementAtIndex(i).objectReferenceValue = child.GetComponent<RecoveredBaseReelMotion>();
            }
            // UIMainView .ctor 0x23bf3ec stores 0x3e19999a3e4ccccd across these two floats.
            data.FindProperty("accelerationSeconds").floatValue = 0.2f;
            data.FindProperty("startInterval").floatValue = 0.15f;
            data.FindProperty("anticipationSpeed").floatValue = 10000;
            data.FindProperty("anticipationDelay").floatValue = 0.5f;
            data.FindProperty("anticipationStopDelay").floatValue = 0.5f;
            data.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, "Assets/Resources/RecoveredSymbols/BaseReels.prefab");
        } finally { Object.DestroyImmediate(root); }
    }
}
