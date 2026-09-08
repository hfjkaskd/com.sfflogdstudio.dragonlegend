using DragonLegend.Whitebox;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class BuildSpinPlayfield
{
    public static void Save()
    {
        const string destination = "Assets/Resources/RecoveredUI/SpinPlayfield.prefab";
        var root = new GameObject("SpinPlayfield", typeof(RectTransform), typeof(RecoveredSpinPlayfield));
        try {
            root.layer = 5; var rect = (RectTransform)root.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.sizeDelta = Vector2.zero;
            var board = Rect("QiPan", root.transform, new Vector2(.5f, 0), new Vector2(.5f, .5f),
                new Vector2(-.003418f, 652), new Vector2(1080, 770.28f));
            var roll = Rect("Roll", board, new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                new Vector2(-1.62f, -71), new Vector2(948, 515));
            var reelRoot = (GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/RecoveredSymbols/BaseReels.prefab"), roll);
            // Sprite geometry is authored at 100 pixels/unit; Canvas local coordinates are pixels.
            reelRoot.transform.localScale = new Vector3(100, 100, 100);
            foreach (var child in reelRoot.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 5;
            var bottom = Rect("Bottom", root.transform, new Vector2(.5f, 0), new Vector2(.5f, 0),
                new Vector2(-.003418f, 0), new Vector2(1080, 292.24f));
            var main = Rect("Main", bottom, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(100, 100));
            var buttonRoot = (GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/RecoveredUI/SpinButton.prefab"), main);
            ((RectTransform)buttonRoot.transform).anchoredPosition = new Vector2(417, -23.793f);
            var data = new SerializedObject(root.GetComponent<RecoveredSpinPlayfield>());
            data.FindProperty("rewardDelay").floatValue = .5f;
            data.FindProperty("spinButton").objectReferenceValue = buttonRoot.GetComponent<RecoveredSpinButton>();
            data.FindProperty("reels").objectReferenceValue = reelRoot.GetComponent<RecoveredBaseReelController>();
            data.FindProperty("symbols").objectReferenceValue = AssetDatabase.LoadAssetAtPath<RecoveredSymbolCatalog>("Assets/Resources/RecoveredSymbols/OriginalSymbolCatalog.asset");
            data.ApplyModifiedPropertiesWithoutUndo();
            var saved = PrefabUtility.SaveAsPrefabAsset(root, destination);
            const string entryPath = "Assets/Resources/Whitebox/GameEntry.prefab";
            var entryRoot = PrefabUtility.LoadPrefabContents(entryPath);
            try {
                var entry = new SerializedObject(entryRoot.GetComponent<GameEntry>());
                entry.FindProperty("playfieldPrefab").objectReferenceValue = saved.GetComponent<RecoveredSpinPlayfield>();
                // Keep local GM controls accessible above the recovered board.
                var primary = (UnityEngine.UI.Button)entry.FindProperty("selectDefault").objectReferenceValue;
                var alternative = (UnityEngine.UI.Button)entry.FindProperty("selectAlternative").objectReferenceValue;
                ((RectTransform)primary.transform).anchoredPosition = new Vector2(0, 350);
                ((RectTransform)alternative.transform).anchoredPosition = new Vector2(0, 230);
                var status = (UnityEngine.UI.Text)entry.FindProperty("status").objectReferenceValue;
                status.gameObject.SetActive(false);
                entryRoot.transform.Find("Title").gameObject.SetActive(false);
                entryRoot.transform.Find("Scope").gameObject.SetActive(false);
                entry.ApplyModifiedPropertiesWithoutUndo(); PrefabUtility.SaveAsPrefabAsset(entryRoot, entryPath);
            } finally { PrefabUtility.UnloadPrefabContents(entryRoot); }
        } finally { Object.DestroyImmediate(root); }
    }
    private static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        var node = new GameObject(name, typeof(RectTransform)); node.layer = 5; node.transform.SetParent(parent, false);
        var rect = (RectTransform)node.transform; rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = pivot; rect.anchoredPosition = position; rect.sizeDelta = size; return rect;
    }
}
