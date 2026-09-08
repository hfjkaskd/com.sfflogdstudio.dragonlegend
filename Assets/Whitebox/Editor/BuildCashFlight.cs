using System.IO;
using DragonLegend.Whitebox;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public static class BuildCashFlight
{
    const string Folder="Assets/Resources/RecoveredUI/CashFlight";
    public static void Save()
    {
        Directory.CreateDirectory(Folder);AssetDatabase.Refresh();BuildJackpotPopupArt.SaveCollectionEffect();
        var item=new GameObject("FlyCoinItem",typeof(RectTransform),typeof(Image),typeof(RecoveredCashFlightItem));
        try {
            item.layer=5;var rect=(RectTransform)item.transform;rect.sizeDelta=new Vector2(73,76);
            var image=item.GetComponent<Image>();image.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/RecoveredArt/Res/UI/zhujiemian/zjm_hb_a.png");
            var s=new SerializedObject(item.GetComponent<RecoveredCashFlightItem>());
            s.FindProperty("image").objectReferenceValue=image;
            s.FindProperty("spriteA").stringValue="RecoveredArt/Res/UI/zhujiemian/zjm_hb_a";
            s.FindProperty("spriteB").stringValue="RecoveredArt/Res/UI/zhujiemian/zjm_hb_b";
            s.FindProperty("scatterDuration").floatValue=.3f;s.FindProperty("flightDuration").floatValue=.3f;s.FindProperty("automaticArcRatio").floatValue=.3f;
            s.FindProperty("scatterEase").animationCurveValue=new AnimationCurve(new Keyframe(0,0,2,2),new Keyframe(1,1,0,0));
            s.FindProperty("flightEase").animationCurveValue=new AnimationCurve(new Keyframe(0,0,0,0),new Keyframe(1,1,2,2));
            s.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(item,Folder+"/FlyCoinItem.prefab");
        } finally{Object.DestroyImmediate(item);}
        var effect=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/RecoveredUI/JackpotPopupArt/ef_slshouji.prefab"));
        try {
            effect.name="Ef_Shouji";var component=effect.AddComponent<RecoveredCashCollectionEffect>();var s=new SerializedObject(component);
            s.FindProperty("animator").objectReferenceValue=effect.GetComponent<RecoveredRegionAnimator>();s.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(effect,Folder+"/Ef_Shouji.prefab");
        } finally{Object.DestroyImmediate(effect);}
        var host=new GameObject("Cash flight",typeof(RectTransform),typeof(RecoveredCashFlightPresenter));
        try {
            host.layer=5;var pool=new GameObject("Pool",typeof(RectTransform));pool.layer=5;pool.transform.SetParent(host.transform,false);
            var s=new SerializedObject(host.GetComponent<RecoveredCashFlightPresenter>());
            s.FindProperty("cashPrefab").objectReferenceValue=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/FlyCoinItem.prefab").GetComponent<RecoveredCashFlightItem>();
            s.FindProperty("collectionPrefab").objectReferenceValue=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/Ef_Shouji.prefab").GetComponent<RecoveredCashCollectionEffect>();
            s.FindProperty("poolRoot").objectReferenceValue=pool.transform;s.FindProperty("preload").intValue=10;s.FindProperty("itemCount").intValue=10;
            s.FindProperty("scatterMin").intValue=-150;s.FindProperty("scatterMax").intValue=150;
            s.FindProperty("scatterWait").floatValue=.3f;s.FindProperty("departureInterval").floatValue=.03f;
            s.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(host,Folder+"/CashFlight.prefab");
        } finally{Object.DestroyImmediate(host);}
        const string balancePath="Assets/Resources/RecoveredUI/BalancePanel.prefab";
        var balance=PrefabUtility.LoadPrefabContents(balancePath);
        try {
            var s=new SerializedObject(balance.GetComponent<RecoveredBalancePanel>());
            s.FindProperty("cashImage").objectReferenceValue=balance.transform.Find("Cash/Img").GetComponent<Image>();
            s.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(balance,balancePath);
        } finally{PrefabUtility.UnloadPrefabContents(balance);}
        const string entryPath="Assets/Resources/Whitebox/GameEntry.prefab";
        var entry=PrefabUtility.LoadPrefabContents(entryPath);
        try {
            var s=new SerializedObject(entry.GetComponent<GameEntry>());
            s.FindProperty("cashFlightPrefab").objectReferenceValue=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/CashFlight.prefab").GetComponent<RecoveredCashFlightPresenter>();
            s.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(entry,entryPath);
        } finally{PrefabUtility.UnloadPrefabContents(entry);}
        AssetDatabase.SaveAssets();
    }
}
