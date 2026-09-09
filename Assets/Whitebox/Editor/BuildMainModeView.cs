using DragonLegend.Whitebox;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

public static class BuildMainModeView
{
    public static void Save()
    {
        BuildJackpotPopupArt.Create("ef_slyanhua","yanhua",new Vector2(50,50),new Vector2(.5f,.5f),"Tools/Evidence/","Artifacts/MainFireworksAuthoring/");
        const string path="Assets/Resources/RecoveredUI/SpinPlayfield.prefab";
        var field=PrefabUtility.LoadPrefabContents(path);
        try {Attach(field);PrefabUtility.SaveAsPrefabAsset(field,path);AssetDatabase.SaveAssets();}
        finally {PrefabUtility.UnloadPrefabContents(field);}
    }
    public static void Attach(GameObject field)
    {
        var board=field.transform.Find("QiPan");var prior=board.Find("FreeRoll");if(prior!=null)Object.DestroyImmediate(prior.gameObject);
        prior=field.transform.Find("SkeletonGraphic (ef_slyanhua)");if(prior!=null)Object.DestroyImmediate(prior.gameObject);
        prior=field.transform.Find("MainFireworks");if(prior!=null)Object.DestroyImmediate(prior.gameObject);
        var host=new GameObject("FreeRoll",typeof(RectTransform));host.layer=5;host.transform.SetParent(board,false);
        var rect=(RectTransform)host.transform;rect.sizeDelta=new Vector2(948,515);rect.anchoredPosition=new Vector2(-1.62f,-71);
        var reels=(RecoveredFreeReels)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<RecoveredFreeReels>("Assets/Resources/RecoveredSymbols/FreeReels.prefab"),host.transform);
        reels.transform.localScale=Vector3.one*100;host.SetActive(false);
        var effects=new GameObject("MainFireworks",typeof(RectTransform),typeof(Canvas));effects.layer=5;effects.transform.SetParent(field.transform,false);effects.transform.SetAsFirstSibling();
        var effectsRect=(RectTransform)effects.transform;effectsRect.anchorMin=Vector2.zero;effectsRect.anchorMax=Vector2.one;effectsRect.sizeDelta=Vector2.zero;
        var effectsCanvas=effects.GetComponent<Canvas>();effectsCanvas.overrideSorting=true;effectsCanvas.sortingOrder=-3;
        var fireworks=(RecoveredRegionAnimator)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<RecoveredRegionAnimator>("Assets/Resources/RecoveredUI/JackpotPopupArt/ef_slyanhua.prefab"),effects.transform);
        fireworks.name="SkeletonGraphic (ef_slyanhua)";fireworks.gameObject.layer=0;
        ((RectTransform)fireworks.transform).anchoredPosition=new Vector2(0,83);fireworks.transform.SetAsFirstSibling();fireworks.gameObject.SetActive(false);
        var mode=field.GetComponent<RecoveredMainModeView>();if(mode==null)mode=field.AddComponent<RecoveredMainModeView>();
        var settings=new SerializedObject(mode);
        settings.FindProperty("baseRoll").objectReferenceValue=board.Find("Roll").gameObject;
        settings.FindProperty("baseResult").objectReferenceValue=board.Find("Result").gameObject;
        settings.FindProperty("freeRoll").objectReferenceValue=host;settings.FindProperty("freeReels").objectReferenceValue=reels;
        settings.FindProperty("fireworks").objectReferenceValue=fireworks;
        settings.FindProperty("fireworksCanvas").objectReferenceValue=effectsCanvas;
        var playfield=field.GetComponent<RecoveredSpinPlayfield>();
        settings.FindProperty("bottom").objectReferenceValue=playfield.FreeBottom;
        settings.FindProperty("downWin").objectReferenceValue=playfield.DownWin;settings.ApplyModifiedPropertiesWithoutUndo();
        settings=new SerializedObject(playfield);settings.FindProperty("modeView").objectReferenceValue=mode;settings.ApplyModifiedPropertiesWithoutUndo();
    }
}
