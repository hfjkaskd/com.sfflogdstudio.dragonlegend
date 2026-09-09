using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using DragonLegend.Whitebox;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

public static class BuildMainBallDestination
{
    public static void Attach(GameObject field)
    {
        var board=field.transform.Find("QiPan");var old=board.Find("LongzhuPos");if(old!=null)Object.DestroyImmediate(old.gameObject);
        var blocks=new Dictionary<string,string>();
        string source=File.ReadAllText("ReferenceOriginal/Res/ViewPrefabs/UIMainView.prefab").Replace("\r","");
        foreach(Match m in Regex.Matches(source,@"^--- !u!\d+ &(\d+)\n.*?(?=^--- !u!|\z)",RegexOptions.Multiline|RegexOptions.Singleline))blocks.Add(m.Groups[1].Value,m.Value);
        var text=new StringBuilder("%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n");BuildTreasureCard.Append("224539572024531195","224539572024531195",blocks,text);
        const string temporary="Assets/Whitebox/Editor/MainBallDestinationSource.prefab";
        try{
            File.WriteAllText(temporary,text.ToString());AssetDatabase.ImportAsset(temporary,ImportAssetOptions.ForceSynchronousImport);
            var target=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(temporary),board,false);target.name="LongzhuPos";
            var settings=new SerializedObject(field.GetComponent<RecoveredSpinPlayfield>());settings.FindProperty("ballDestination").objectReferenceValue=target.transform;settings.ApplyModifiedPropertiesWithoutUndo();
        }finally{AssetDatabase.DeleteAsset(temporary);}
    }
    public static void Save()
    {
        const string path="Assets/Resources/RecoveredUI/SpinPlayfield.prefab";var root=PrefabUtility.LoadPrefabContents(path);
        try{Attach(root);PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}
        const string core="Assets/Resources/RecoveredUI/CoreRoundFlow.prefab";root=PrefabUtility.LoadPrefabContents(core);
        try{var old=root.transform.Find("LongzhuPos");if(old!=null)Object.DestroyImmediate(old.gameObject);PrefabUtility.SaveAsPrefabAsset(root,core);AssetDatabase.SaveAssets();}
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
}
