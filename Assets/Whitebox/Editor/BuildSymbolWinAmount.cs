using DragonLegend.Whitebox;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public static class BuildSymbolWinAmount
{
    public static void Save()
    {
        var root=new GameObject("TempWin",typeof(RectTransform),typeof(Canvas),typeof(RecoveredSymbolWinAmount));root.layer=5;
        try {
            var rect=(RectTransform)root.transform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,0);
            rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=new Vector2(-.0034179688f,336.26917f);rect.sizeDelta=new Vector2(1080,130);
            var node=new GameObject("Text (Legacy)",typeof(RectTransform),typeof(CanvasRenderer),typeof(Text));node.layer=5;node.transform.SetParent(root.transform,false);
            var textRect=(RectTransform)node.transform;textRect.anchorMin=textRect.anchorMax=textRect.pivot=new Vector2(.5f,.5f);
            textRect.sizeDelta=new Vector2(160,30);textRect.localScale=Vector3.one*1.5f;
            var label=node.GetComponent<Text>();label.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Resources/RecoveredUI/CoinRewardText/Green.asset");
            label.fontSize=0;label.fontStyle=FontStyle.Normal;label.resizeTextForBestFit=false;label.resizeTextMinSize=0;label.resizeTextMaxSize=40;
            label.alignment=TextAnchor.MiddleCenter;label.alignByGeometry=false;label.supportRichText=true;label.lineSpacing=1;
            label.horizontalOverflow=HorizontalWrapMode.Overflow;label.verticalOverflow=VerticalWrapMode.Truncate;
            label.color=Color.white;label.raycastTarget=true;label.maskable=true;label.text="123";
            var canvas=root.GetComponent<Canvas>();canvas.overrideSorting=true;canvas.sortingOrder=1;
            var settings=new SerializedObject(root.GetComponent<RecoveredSymbolWinAmount>());
            settings.FindProperty("sortingCanvas").objectReferenceValue=canvas;
            settings.FindProperty("label").objectReferenceValue=label;settings.FindProperty("countDuration").floatValue=.3f;
            settings.FindProperty("waitDuration").floatValue=.5f;
            settings.FindProperty("countEase").animationCurveValue=new AnimationCurve(new Keyframe(0,0,2,2),new Keyframe(1,1,0,0));
            settings.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(root,"Assets/Resources/RecoveredUI/SymbolWinAmount.prefab");AssetDatabase.SaveAssets();
        } finally {Object.DestroyImmediate(root);}
        BuildSpinPlayfield.Save();
    }
}
