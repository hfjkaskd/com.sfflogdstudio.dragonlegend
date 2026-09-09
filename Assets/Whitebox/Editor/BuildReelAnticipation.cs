using DragonLegend.Whitebox;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public static class BuildReelAnticipation
{
    public static void Save()
    {
        BuildJackpotPopupArt.Create("ef_slliejiasu","liejiasu",new Vector2(267.50006f,606),new Vector2(.49532714f,.5f),"Tools/Evidence/","Artifacts/ReelAnticipationAuthoring/");
        const string path="Assets/Resources/RecoveredUI/SpinPlayfield.prefab";
        var field=PrefabUtility.LoadPrefabContents(path);
        try {
            var roll=field.transform.Find("QiPan/Roll");
            var settings=new SerializedObject(field.GetComponent<RecoveredMainModeView>());
            var array=settings.FindProperty("speedEffects");array.arraySize=5;
            for(int i=0;i<5;i++) {
                string name="Roll"+(i+1)+"SpeedEffect";
                var previous=roll.Find(name);if(previous!=null)Object.DestroyImmediate(previous.gameObject);
                var host=new GameObject(name,typeof(RectTransform),typeof(RectMask2D));host.layer=5;host.transform.SetParent(roll,false);
                var rect=(RectTransform)host.transform;rect.sizeDelta=new Vector2(188,518);rect.anchoredPosition=new Vector2(-380+190*i,-1);
                var effect=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/RecoveredUI/JackpotPopupArt/ef_slliejiasu.prefab"),host.transform);
                PrefabUtility.UnpackPrefabInstance(effect,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
                // Original SkeletonGraphic.startingAnimation is empty; SetActive only shows the setup pose.
                Object.DestroyImmediate(effect.GetComponent<RecoveredRegionAnimator>());
                Object.DestroyImmediate(effect.GetComponent<Animation>());
                effect.SetActive(false);array.GetArrayElementAtIndex(i).objectReferenceValue=effect;
            }
            settings.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(field,path);AssetDatabase.SaveAssets();
        } finally {PrefabUtility.UnloadPrefabContents(field);}
    }
}
