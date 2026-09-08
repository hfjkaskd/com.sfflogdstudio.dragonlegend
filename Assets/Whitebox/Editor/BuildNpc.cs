using System;
using System.IO;
using DragonLegend.Whitebox;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

public static class BuildNpc
{
    [Serializable] private class Reference {public RecoveredNpcConstraints program;}
    public static void Save()
    {
        var program=JsonUtility.FromJson<Reference>(File.ReadAllText("Tools/Evidence/npc-constraint-poses.json")).program;
        BuildJackpotPopupArt.Create("ef_long","long",new Vector2(990,1245.0002f),new Vector2(.47777772f,.3566265f),
            "Tools/Evidence/","Artifacts/NpcAuthoring/",program);
        var root=new GameObject("Npc",typeof(RectTransform));
        try {
            var rect=(RectTransform)root.transform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,0);
            rect.anchoredPosition=new Vector2(-.003418f,652);rect.sizeDelta=new Vector2(1080,770.28f);
            var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/RecoveredUI/JackpotPopupArt/ef_long.prefab");
            var dragon=(GameObject)PrefabUtility.InstantiatePrefab(source,root.transform);
            dragon.name="SkeletonGraphic (ef_long) (1)";dragon.layer=0;
            ((RectTransform)dragon.transform).anchoredPosition=new Vector2(0,355);
            var fireParent=new GameObject("PlayFire",typeof(RectTransform));fireParent.transform.SetParent(root.transform,false);
            var fireRect=(RectTransform)fireParent.transform;fireRect.anchorMin=Vector2.zero;fireRect.anchorMax=Vector2.one;
            fireRect.anchoredPosition=new Vector2(.0034179688f,335.14f);fireRect.sizeDelta=new Vector2(0,1149.72f);
            var fire=(GameObject)PrefabUtility.InstantiatePrefab(source,fireParent.transform);fire.layer=0;fire.name="SkeletonGraphic (ef_long)";
            ((RectTransform)fire.transform).anchoredPosition=new Vector2(-.0034179688f,19.859985f);
            fire.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(root,"Assets/Resources/RecoveredUI/Npc.prefab");AssetDatabase.SaveAssets();
        }finally{Object.DestroyImmediate(root);}
    }
}
