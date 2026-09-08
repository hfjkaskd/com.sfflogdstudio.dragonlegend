using System.IO;
using DragonLegend.Whitebox;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

public static class BuildScatter
{
    private const string Folder="Assets/Resources/RecoveredSymbols/Scatter";
    public static void SaveAndConnect()
    {
        Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
        var root=new GameObject("Scatter",typeof(SortingGroup));root.layer=5;root.SetActive(false);
        try {
            var effect=root.AddComponent<RecoveredScatterEffect>();var s=new SerializedObject(effect);
            GameObject main=null;
            foreach(string clip in new[]{"idle","png","start"}) {
                string folder=Folder+"/"+clip;Directory.CreateDirectory(folder);AssetDatabase.Refresh();
                var node=BuildWildWorld.Create(root.transform,"Symbol","ef_scatter","scatter",folder,true,"Artifacts/SymbolAuthoring",clip);
                var data=AssetDatabase.LoadAssetAtPath<RecoveredWorldRigData>(folder+"/ef_scatter.asset");
                s.FindProperty(clip).objectReferenceValue=data;
                if(main==null)main=node;
                else {main.GetComponent<Animation>().AddClip(node.GetComponent<Animation>().clip,clip);Object.DestroyImmediate(node);}
            }
            s.FindProperty("player").objectReferenceValue=main.GetComponent<Animation>();
            s.FindProperty("rig").objectReferenceValue=main.GetComponent<RecoveredWorldRig>();
            s.FindProperty("sorting").objectReferenceValue=root.GetComponent<SortingGroup>();s.ApplyModifiedPropertiesWithoutUndo();
            root.SetActive(true);PrefabUtility.SaveAsPrefabAsset(root,Folder+".prefab");AssetDatabase.SaveAssets();
        } finally {Object.DestroyImmediate(root);}
        const string fieldPath="Assets/Resources/RecoveredUI/SpinPlayfield.prefab";
        var field=PrefabUtility.LoadPrefabContents(fieldPath);
        try {Attach(field);PrefabUtility.SaveAsPrefabAsset(field,fieldPath);}
        finally {PrefabUtility.UnloadPrefabContents(field);}
    }
    public static void Attach(GameObject field)
    {
        var result=field.transform.Find("QiPan/Result");var prior=result.Find("ScatterEffects");
        var node=prior==null?new GameObject("ScatterEffects",typeof(RecoveredScatterPresenter)):prior.gameObject;
        node.layer=5;node.transform.SetParent(result,false);node.transform.localScale=Vector3.one*100;
        var presenter=node.GetComponent<RecoveredScatterPresenter>();var s=new SerializedObject(presenter);
        s.FindProperty("prefab").objectReferenceValue=AssetDatabase.LoadAssetAtPath<RecoveredScatterEffect>(Folder+".prefab");s.ApplyModifiedPropertiesWithoutUndo();
        s=new SerializedObject(field.GetComponent<RecoveredSpinPlayfield>());s.FindProperty("scatters").objectReferenceValue=presenter;s.ApplyModifiedPropertiesWithoutUndo();
    }
}
