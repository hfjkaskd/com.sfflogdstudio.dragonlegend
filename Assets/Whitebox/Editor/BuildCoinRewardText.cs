using System.IO;
using DragonLegend.Whitebox;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class BuildCoinRewardText
{
    public static void Save()
    {
        const string folder = "Assets/Resources/RecoveredUI/CoinRewardText";
        Directory.CreateDirectory(folder);
        foreach (var name in new[] { "Green.asset", "Green.asset.meta", "Green_Material.mat", "Green_Material.mat.meta" }) {
            string destination = folder + "/" + name;
            if (!File.Exists(destination)) File.Copy("ReferenceOriginal/Res/Font/" + name, destination);
        }
        AssetDatabase.Refresh();
        var root = new GameObject("CoinRewardText", typeof(RectTransform), typeof(Canvas), typeof(RecoveredCoinRewardText));
        try {
            root.layer = 5; var rect = (RectTransform)root.transform; rect.sizeDelta = Vector2.zero; rect.localScale = Vector3.one * .01f;
            var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace; canvas.overrideSorting = true; canvas.sortingOrder = 100;
            var node = new GameObject("Text (Legacy)", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            node.layer = 5; node.transform.SetParent(root.transform, false);
            rect = (RectTransform)node.transform; rect.sizeDelta = Vector2.zero; rect.anchoredPosition = new Vector2(0, 4.6f);
            var label = node.GetComponent<Text>(); label.font = AssetDatabase.LoadAssetAtPath<Font>(folder + "/Green.asset");
            label.fontSize = 0; label.fontStyle = FontStyle.Normal; label.resizeTextForBestFit = false;
            label.resizeTextMinSize = 0; label.resizeTextMaxSize = 95; label.alignment = TextAnchor.MiddleCenter;
            label.alignByGeometry = false; label.supportRichText = true; label.lineSpacing = 1;
            label.horizontalOverflow = HorizontalWrapMode.Overflow; label.verticalOverflow = VerticalWrapMode.Truncate;
            label.color = Color.white; label.raycastTarget = true; label.maskable = true; label.text = "96.3";
            node.SetActive(false);
            var config = new SerializedObject(root.GetComponent<RecoveredCoinRewardText>());
            config.FindProperty("label").objectReferenceValue = label;
            config.FindProperty("showDelay").floatValue = .2f; config.FindProperty("scaleDuration").floatValue = .2f;
            config.FindProperty("peakScale").floatValue = 1.2f; config.FindProperty("restingScale").floatValue = 1;
            config.FindProperty("presentationDelay").floatValue = .4f; config.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, "Assets/Resources/RecoveredUI/CoinRewardText.prefab"); AssetDatabase.SaveAssets();
        } finally { Object.DestroyImmediate(root); }
        BuildCoinStopEffect.Save();
    }
}
