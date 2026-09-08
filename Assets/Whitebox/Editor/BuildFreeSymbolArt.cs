using System.IO;
using DragonLegend.Whitebox;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

public static class BuildFreeSymbolArt
{
    public static void Save()
    {
        Build("ef_jinbi","jinbi","FreeCoinArt","zcjb_idle");
        Build("ef_longzhu","longzhu","FreeBallArt","idle_lan");
        AssetDatabase.SaveAssets();
    }
    private static void Build(string file,string directory,string name,string initial)
    {
        string folder="Assets/Resources/RecoveredSymbols/"+name;
        var source=JsonUtility.FromJson<BuildCoinAppearance.Data>(File.ReadAllText("Tools/Evidence/FreeSymbols/"+file+".json"));
        var root=new GameObject(name);root.layer=5;root.SetActive(false);
        try {
            var settings=new SerializedObject(root.AddComponent<RecoveredWorldAnimation>());
            var clips=settings.FindProperty("clips");clips.arraySize=source.animations.Length;
            GameObject main=null;
            for(int i=0;i<source.animations.Length;i++) {
                string clip=source.animations[i].name;string destination=folder+"/"+clip;
                Directory.CreateDirectory(destination);AssetDatabase.Refresh();
                var node=BuildWildWorld.Create(root.transform,"Art",file,directory,destination,true,"Artifacts/FreeSymbolAuthoring",clip);
                var entry=clips.GetArrayElementAtIndex(i);entry.FindPropertyRelative("name").stringValue=clip;
                entry.FindPropertyRelative("data").objectReferenceValue=AssetDatabase.LoadAssetAtPath<RecoveredWorldRigData>(destination+"/"+file+".asset");
                if(main==null)main=node;
                else {main.GetComponent<Animation>().AddClip(node.GetComponent<Animation>().clip,clip);Object.DestroyImmediate(node);}
            }
            settings.FindProperty("initialClip").stringValue=initial;
            settings.FindProperty("player").objectReferenceValue=main.GetComponent<Animation>();
            settings.FindProperty("rig").objectReferenceValue=main.GetComponent<RecoveredWorldRig>();
            settings.ApplyModifiedPropertiesWithoutUndo();
            root.SetActive(true);PrefabUtility.SaveAsPrefabAsset(root,folder+".prefab");
        } finally {Object.DestroyImmediate(root);}
    }
}
