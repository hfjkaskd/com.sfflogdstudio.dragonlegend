using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using DragonLegend.Whitebox;
using Object=UnityEngine.Object;

public static class BuildBonusCard
{
    public static void Save()
    {
        // UIBonusView's body and glow share this source skeleton and RectTransform.
        BuildJackpotPopupArt.Create("ef_jinbi","棋子/jinbi",new Vector2(187,172),
            new Vector2(.49999967f,.5f),"Artifacts/BonusSource/","Artifacts/BonusAuthoring/");
        AssetDatabase.SaveAssets();
    }
    public static void SaveItem()
    {
        var root=new GameObject("Bonus",typeof(RectTransform),typeof(RecoveredBonusItemTurn));root.layer=5;
        try {
            ((RectTransform)root.transform).sizeDelta=new Vector2(100,100);
            var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/RecoveredUI/JackpotPopupArt/ef_jinbi.prefab");
            var body=(GameObject)PrefabUtility.InstantiatePrefab(source,root.transform);body.name="SkeletonGraphic (ef_qizijinli)";body.layer=0;
            var rig=body.GetComponent<RecoveredRegionRig>();rig.raycastTarget=true;
            // Preserve the original 234x242 hit bounds on the visible native Graphic.
            // No separate transparent Image is needed to host the standard Button.
            rig.raycastPadding=new Vector4(-23.5f,-35,-23.5f,-35);
            var button=body.AddComponent<Button>();button.targetGraphic=rig;button.transition=Selectable.Transition.None;
            var glow=(GameObject)PrefabUtility.InstantiatePrefab(source,root.transform);glow.name="glow";glow.layer=0;glow.SetActive(false);
            var textRoot=new GameObject("Text (Legacy)",typeof(RectTransform),typeof(Text));textRoot.layer=5;textRoot.transform.SetParent(root.transform,false);
            var rect=(RectTransform)textRoot.transform;rect.sizeDelta=Vector2.zero;rect.anchoredPosition=new Vector2(0,4.6f);
            var text=textRoot.GetComponent<Text>();text.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Resources/RecoveredUI/CoinRewardText/Green.asset");
            text.fontSize=0;text.alignment=TextAnchor.MiddleCenter;text.horizontalOverflow=HorizontalWrapMode.Overflow;
            text.verticalOverflow=VerticalWrapMode.Truncate;text.raycastTarget=false;text.text="96.3";textRoot.SetActive(false);
            var adRoot=new GameObject("Ad",typeof(RectTransform),typeof(Image));adRoot.layer=5;adRoot.transform.SetParent(root.transform,false);
            rect=(RectTransform)adRoot.transform;rect.sizeDelta=new Vector2(90,92);rect.anchoredPosition=new Vector2(0,4.6f);
            var ad=adRoot.GetComponent<Image>();ad.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/RecoveredArt/Res/UI/bank/b_btn_bofan.png");ad.raycastTarget=false;adRoot.SetActive(false);
            if(text.font==null||ad.sprite==null)throw new System.IO.InvalidDataException("Missing original Bonus font/ad sprite");
            var settings=new SerializedObject(root.GetComponent<RecoveredBonusItemTurn>());
            settings.FindProperty("body").objectReferenceValue=body.GetComponent<RecoveredRegionAnimator>();
            settings.FindProperty("glow").objectReferenceValue=glow.GetComponent<RecoveredRegionAnimator>();
            settings.FindProperty("rewardText").objectReferenceValue=text;settings.FindProperty("ad").objectReferenceValue=ad;settings.FindProperty("button").objectReferenceValue=button;
            // Native DOTween default OutQuad.
            settings.FindProperty("scaleEase").animationCurveValue=new AnimationCurve(new Keyframe(0,0,2,2),new Keyframe(1,1,0,0));
            settings.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root,"Assets/Resources/RecoveredUI/BonusItem.prefab");AssetDatabase.SaveAssets();
        }finally{Object.DestroyImmediate(root);}
    }
}
