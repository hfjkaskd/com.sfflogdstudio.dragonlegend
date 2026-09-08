using DragonLegend.Whitebox;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public static class BuildFreeSlotGame
{
    public static void Save()
    {
        var root=new GameObject("FreeSlotGame",typeof(RectTransform),typeof(RecoveredFreeSlotGame));root.layer=5;
        try {
            var rect=(RectTransform)root.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.sizeDelta=Vector2.zero;
            var icon=new GameObject("SlotPrefab",typeof(RectTransform),typeof(Image));icon.layer=5;icon.transform.SetParent(root.transform,false);
            var iconRect=(RectTransform)icon.transform;iconRect.sizeDelta=new Vector2(244,186);
            icon.GetComponent<Image>().sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/RecoveredArt/Res/UI/free_game/mfyx_icon_slots.png");icon.SetActive(false);
            var window=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/RecoveredUI/LuckySpinWindow.prefab"),root.transform);
            var settings=new SerializedObject(root.GetComponent<RecoveredFreeSlotGame>());
            settings.FindProperty("icon").objectReferenceValue=iconRect;settings.FindProperty("window").objectReferenceValue=window.GetComponent<RecoveredLuckySpinWindow>();
            settings.FindProperty("scaleMultiplier").floatValue=1.5f;settings.FindProperty("scaleDuration").floatValue=.3f;settings.FindProperty("windowDelay").floatValue=.6f;
            settings.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(root,"Assets/Resources/RecoveredUI/FreeSlotGame.prefab");AssetDatabase.SaveAssets();
        } finally {Object.DestroyImmediate(root);}
    }
}
