using DragonLegend.Whitebox;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public static class BuildFreeLuckyGame
{
    public static void Save()
    {
        var root=new GameObject("FreeLuckyGame",typeof(RectTransform),typeof(RecoveredFreeLuckyGame));root.layer=5;
        try {
            var rect=(RectTransform)root.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.sizeDelta=Vector2.zero;
            var icon=new GameObject("RewardPrefab",typeof(RectTransform),typeof(Image));icon.layer=5;icon.transform.SetParent(root.transform,false);
            var iconRect=(RectTransform)icon.transform;iconRect.sizeDelta=new Vector2(498,368);iconRect.localScale=Vector3.one*.5f;
            icon.GetComponent<Image>().sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/RecoveredArt/Res/UI/pop-up/ty_hb_meijing.png");
            icon.GetComponent<Image>().raycastTarget=false;
            icon.SetActive(false);
            var popup=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/RecoveredUI/BonusRewardPopup.prefab"),root.transform);
            popup.SetActive(false);
            var settings=new SerializedObject(root.GetComponent<RecoveredFreeLuckyGame>());
            settings.FindProperty("icon").objectReferenceValue=iconRect;settings.FindProperty("popup").objectReferenceValue=popup.GetComponent<RecoveredBonusRewardPopup>();
            settings.FindProperty("scaleMultiplier").floatValue=1.3f;settings.FindProperty("scaleDuration").floatValue=.3f;
            settings.FindProperty("popupDelay").floatValue=.6f;settings.ApplyModifiedPropertiesWithoutUndo();
            ConfigureRewardLayer(root);
            PrefabUtility.SaveAsPrefabAsset(root,"Assets/Resources/RecoveredUI/FreeLuckyGame.prefab");AssetDatabase.SaveAssets();
        } finally {Object.DestroyImmediate(root);}
    }
    public static void ConfigureRewardLayer(GameObject root)
    {
        var settings=new SerializedObject(root.GetComponent<RecoveredFreeLuckyGame>());
        var icon=(RectTransform)settings.FindProperty("icon").objectReferenceValue;
        var layer=root.transform.Find("RewardLayer");
        if(layer==null)
        {
            var created=new GameObject("RewardLayer",typeof(RectTransform),typeof(Canvas));
            created.layer=root.layer;created.transform.SetParent(root.transform,false);layer=created.transform;
        }
        var rect=(RectTransform)layer;
        rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;
        rect.offsetMin=rect.offsetMax=Vector2.zero;rect.localScale=Vector3.one;
        icon.SetParent(layer,false);icon.GetComponent<Image>().raycastTarget=false;
        var canvas=layer.GetComponent<Canvas>();canvas.overrideSorting=true;canvas.sortingOrder=50;
        settings.FindProperty("rewardCanvas").objectReferenceValue=canvas;
        settings.FindProperty("rewardSortingOffset").intValue=50;
        settings.ApplyModifiedPropertiesWithoutUndo();
    }
}
