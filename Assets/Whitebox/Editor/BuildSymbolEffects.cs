using System.IO;
using DragonLegend.Whitebox;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

public static class BuildSymbolEffects
{
    public static void SaveAndConnect() {Save();BuildSpinPlayfield.Save();}
    public static void Save()
    {
        string[] names={"A","10","J","Q","K","Yu","Gui","Wild1"};
        string[] clips={"a_idle","10_idle","j_idle","q_idle","k_idle","idle","idle","idle"};
        for(int i=0;i<names.Length;i++) {
            string folder="Assets/Resources/RecoveredSymbols/Winning/"+names[i];
            Directory.CreateDirectory(folder);AssetDatabase.Refresh();
            var root=new GameObject(names[i],typeof(SortingGroup),typeof(RecoveredWildColumn));root.layer=5;
            try {
                string file=i<5?"ef_qizizimu":i==5?"ef_qizijinli":i==6?"ef_qizibixi":"ef_wild1";
                string directory=i<5?"qizidijie":i==5?"qizijinli":i==6?"qizibixi":"wild1";
                var symbol=BuildWildWorld.Create(root.transform,"Symbol",file,directory,folder,true,"Artifacts/SymbolAuthoring",clips[i]);
                symbol.GetComponent<MeshRenderer>().sortingOrder=0;
                var win=new GameObject("Win1");win.layer=5;win.transform.SetParent(root.transform,false);
                BuildWildWorld.Create(win.transform,"Glow","ef_slwin1","slwin1",folder,true,"Artifacts/SymbolAuthoring","animation");
                var settings=new SerializedObject(root.GetComponent<RecoveredWildColumn>());
                var players=root.GetComponentsInChildren<Animation>();var array=settings.FindProperty("players");array.arraySize=players.Length;
                for(int j=0;j<players.Length;j++)array.GetArrayElementAtIndex(j).objectReferenceValue=players[j];
                settings.FindProperty("sorting").objectReferenceValue=root.GetComponent<SortingGroup>();settings.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root,folder+".prefab");
            } finally {Object.DestroyImmediate(root);}
        }
        AssetDatabase.SaveAssets();
    }
}
