using System.IO;
using DragonLegend.Whitebox;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

public static class BuildSceneTransition
{
    public static void Save()
    {
        const string folder="Assets/Resources/RecoveredEffects/SceneTransition";
        Directory.CreateDirectory(folder);AssetDatabase.Refresh();
        var root=new GameObject("SceneTransition",typeof(RecoveredSceneTransition));
        try {
            var effect=BuildWildWorld.Create(root.transform,"Spine GameObject (ef_slzhuanchang)",
                "ef_slzhuanchang","zhuanchang",folder,false,"Artifacts/TransitionAuthoring",null,
                "RecoveredArt/Res/Spine/zhuanchang/ef_slzhuanchang");
            effect.layer=0;effect.transform.localPosition=new Vector3(0,0,20);
            effect.GetComponent<MeshRenderer>().sortingOrder=0;
            var settings=new SerializedObject(root.GetComponent<RecoveredSceneTransition>());
            settings.FindProperty("player").objectReferenceValue=effect.GetComponent<Animation>();
            settings.FindProperty("rig").objectReferenceValue=effect.GetComponent<RecoveredWorldRig>();
            settings.FindProperty("clipName").stringValue="animation";
            settings.FindProperty("eventDelay").floatValue=.8f;
            settings.FindProperty("completionDelay").floatValue=2.2f;
            settings.ApplyModifiedPropertiesWithoutUndo();effect.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(root,folder+".prefab");AssetDatabase.SaveAssets();
        }finally{Object.DestroyImmediate(root);}
    }
}
