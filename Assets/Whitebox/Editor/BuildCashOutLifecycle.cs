using DragonLegend.Whitebox;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class BuildCashOutLifecycle
{
    public static void Save()
    {
        const string path="Assets/Resources/RecoveredUI/CashOutWindow.prefab";var root=PrefabUtility.LoadPrefabContents(path);
        try{Attach(root);PrefabUtility.SaveAsPrefabAsset(root,path);AssetDatabase.SaveAssets();}finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    public static void Attach(GameObject root)
    {
        if(root.transform.Find("_WindowBg")==null){
            var mask=new GameObject("_WindowBg",typeof(RectTransform),typeof(Image),typeof(Button));mask.layer=5;mask.transform.SetParent(root.transform,false);mask.transform.SetAsFirstSibling();
            var rect=(RectTransform)mask.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.sizeDelta=Vector2.zero;
            var image=mask.GetComponent<Image>();var button=mask.GetComponent<Button>();button.targetGraphic=image;button.transition=Selectable.Transition.None;
        }
        // UICashOutView.OnInitProperty: Popup + Normal (transparent input blocker).
        root.transform.Find("_WindowBg").GetComponent<Image>().color=Color.clear;
        root.GetComponent<Canvas>().sortingOrder=300;root.GetComponent<Canvas>().overrideSorting=true;
        var view=root.GetComponent<RecoveredCashOutWindow>();if(view==null)view=root.AddComponent<RecoveredCashOutWindow>();var settings=new SerializedObject(view);
        settings.FindProperty("content").objectReferenceValue=root.transform.Find("Content");settings.FindProperty("listParent").objectReferenceValue=root.transform.Find("Content/Node/Rect");
        settings.FindProperty("adapt").objectReferenceValue=root.GetComponentInChildren<RecoveredScreenAdapt>(true);
        settings.FindProperty("mode").objectReferenceValue=root.GetComponent<RecoveredCashOutModeView>();settings.FindProperty("header").objectReferenceValue=root.GetComponent<RecoveredCashOutPaymentHeader>();
        settings.FindProperty("list").objectReferenceValue=root.GetComponentInChildren<RecoveredCashOutList>(true);settings.FindProperty("bottom").objectReferenceValue=root.GetComponentInChildren<RecoveredCashOutBottom>(true);
        settings.FindProperty("entrance").objectReferenceValue=root.GetComponent<RecoveredCashOutEntrance>();settings.FindProperty("backButton").objectReferenceValue=root.transform.Find("Content/Top/backBtn").GetComponent<Button>();
        settings.FindProperty("fromScale").floatValue=0;settings.FindProperty("toScale").floatValue=1;settings.FindProperty("duration").floatValue=.3f;
        settings.FindProperty("enterEase").animationCurveValue=new AnimationCurve(new Keyframe(0,0,4.70158f,4.70158f),new Keyframe(1,1,0,0));
        settings.FindProperty("exitEase").animationCurveValue=new AnimationCurve(new Keyframe(0,0,0,0),new Keyframe(1,1,4.70158f,4.70158f));settings.ApplyModifiedPropertiesWithoutUndo();
    }
}
