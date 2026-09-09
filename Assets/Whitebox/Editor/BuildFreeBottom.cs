using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using DragonLegend.Whitebox;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public static class BuildFreeBottom
{
    public static void Save()
    {
        const string material="Assets/Resources/Fonts & Materials/#1A1457_3.mat";
        string originalMaterial=File.ReadAllText("ReferenceOriginal/Resources/fonts & materials/#1A1457_3.mat");
        originalMaterial=originalMaterial.Replace("f8ccc87df5692024b9b15bc5304cfda3","68e6db2ebdc24f95958faec2be5558d6")
            .Replace("2bdab8846be2fb541972bd4c49a3dcfa",AssetDatabase.AssetPathToGUID("Assets/Resources/RecoveredArt/Res/Font/QuorumStd-Black_zitidi.com Atlas.png"));
        File.WriteAllText(material,originalMaterial);
        if(!File.Exists(material+".meta"))File.Copy("ReferenceOriginal/Resources/fonts & materials/#1A1457_3.mat.meta",material+".meta");
        AssetDatabase.Refresh();
        const string path="Assets/Resources/RecoveredUI/SpinPlayfield.prefab";
        var field=PrefabUtility.LoadPrefabContents(path);
        try {Attach(field);PrefabUtility.SaveAsPrefabAsset(field,path);AssetDatabase.SaveAssets();}
        finally {PrefabUtility.UnloadPrefabContents(field);}
    }
    public static void Attach(GameObject field)
    {
        const string temporary="Assets/Whitebox/Editor/FreeBottomSource.prefab";
        var bottom=field.transform.Find("Bottom");var prior=bottom.Find("Free");if(prior!=null)Object.DestroyImmediate(prior.gameObject);
        string source=File.ReadAllText("ReferenceOriginal/Res/ViewPrefabs/UIMainView.prefab").Replace("\r","");
        var blocks=new Dictionary<string,string>();
        foreach(Match m in Regex.Matches(source,@"^--- !u!\d+ &(\d+)\n.*?(?=^--- !u!|\z)",RegexOptions.Multiline|RegexOptions.Singleline))blocks.Add(m.Groups[1].Value,m.Value);
        var yaml=new StringBuilder("%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n");BuildTreasureCard.Append("224396801632836831","224396801632836831",blocks,yaml);
        var map=new Dictionary<string,string>();BuildTreasureCard.Script<Image>(map,"3cf5a44414476512e00c3e7a2569a919");BuildTreasureCard.Script<TextMeshProUGUI>(map,"3f96b1d166d19b209697e35b35d65c76");
        const string guid="17c564eadab8a3940aac81ae943b3097";BuildTreasureCard.Map(map,guid,"Assets/Resources/RecoveredArt/Res/UI/zhujiemian/zjm_s9g_spin_bg.png");
        string text=yaml.ToString();foreach(var pair in map)text=text.Replace(pair.Key,pair.Value);
        text=text.Replace("guid: "+map[guid]+", type: 2","guid: "+map[guid]+", type: 3");
        foreach(Match m in Regex.Matches(text,@"guid: (\w+)"))if(string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(m.Groups[1].Value)))throw new InvalidDataException("Missing Free bottom reference "+m.Groups[1].Value);
        GameObject free;
        try {File.WriteAllText(temporary,text);AssetDatabase.ImportAsset(temporary,ImportAssetOptions.ForceSynchronousImport);free=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(temporary),bottom,false);free.name="Free";}
        finally {AssetDatabase.DeleteAsset(temporary);}
        free.SetActive(false);
        var controller=bottom.GetComponent<RecoveredFreeBottom>();if(controller==null)controller=bottom.gameObject.AddComponent<RecoveredFreeBottom>();
        var settings=new SerializedObject(controller);settings.FindProperty("main").objectReferenceValue=bottom.Find("Main").gameObject;
        settings.FindProperty("free").objectReferenceValue=free;settings.FindProperty("count").objectReferenceValue=free.GetComponentInChildren<TextMeshProUGUI>(true);
        settings.FindProperty("countFormat").stringValue="FREE SPIN <material=\"#1A1457_3\"><gradient=\"free\">{0}</gradient></material> TIMES";
        settings.ApplyModifiedPropertiesWithoutUndo();
        settings=new SerializedObject(field.GetComponent<RecoveredSpinPlayfield>());settings.FindProperty("freeBottom").objectReferenceValue=controller;settings.ApplyModifiedPropertiesWithoutUndo();
    }
}
