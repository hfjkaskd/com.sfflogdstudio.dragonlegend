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

public static class BuildSpinRecovery
{
    public static void Save()
    {
        const string path="Assets/Resources/RecoveredUI/SpinPlayfield.prefab";
        var field=PrefabUtility.LoadPrefabContents(path);
        try {Attach(field);PrefabUtility.SaveAsPrefabAsset(field,path);AssetDatabase.SaveAssets();}
        finally {PrefabUtility.UnloadPrefabContents(field);}
    }
    public static void Attach(GameObject field)
    {
        const string temporary="Assets/Whitebox/Editor/SpinRecoverySource.prefab";
        var parent=field.transform.Find("Bottom/Main");var prior=parent.Find("SpinShow");if(prior!=null)Object.DestroyImmediate(prior.gameObject);
        string source=File.ReadAllText("ReferenceOriginal/Res/ViewPrefabs/UIMainView.prefab").Replace("\r","");
        var blocks=new Dictionary<string,string>();
        foreach(Match m in Regex.Matches(source,@"^--- !u!\d+ &(\d+)\n.*?(?=^--- !u!|\z)",RegexOptions.Multiline|RegexOptions.Singleline))blocks.Add(m.Groups[1].Value,m.Value);
        var yaml=new StringBuilder("%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n");BuildTreasureCard.Append("224643520764344681","224643520764344681",blocks,yaml);
        var map=new Dictionary<string,string>();BuildTreasureCard.Script<Image>(map,"3cf5a44414476512e00c3e7a2569a919");
        BuildTreasureCard.Script<TextMeshProUGUI>(map,"3f96b1d166d19b209697e35b35d65c76");BuildTreasureCard.Script<Button>(map,"18d0a90695249463551c00f45766e642");
        var sprites=new Dictionary<string,string>{{"17c564eadab8a3940aac81ae943b3097","zjm_s9g_spin_bg"},{"6895199e3023c3945ba80398981a312b","zjm_btn_jia"}};
        string text=yaml.ToString();
        foreach(var pair in sprites){BuildTreasureCard.Map(map,pair.Key,"Assets/Resources/RecoveredArt/Res/UI/zhujiemian/"+pair.Value+".png");text=text.Replace("guid: "+pair.Key+", type: 2","guid: "+pair.Key+", type: 3");}
        foreach(var pair in map)text=text.Replace(pair.Key,pair.Value);
        foreach(Match m in Regex.Matches(text,@"guid: (\w+)"))if(string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(m.Groups[1].Value)))throw new InvalidDataException("Missing Spin recovery reference "+m.Groups[1].Value);
        GameObject root;
        try {File.WriteAllText(temporary,text);AssetDatabase.ImportAsset(temporary,ImportAssetOptions.ForceSynchronousImport);root=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(temporary),parent,false);root.name="SpinShow";}
        finally {AssetDatabase.DeleteAsset(temporary);}
        var view=field.GetComponent<RecoveredSpinRecoveryView>();if(view==null)view=field.AddComponent<RecoveredSpinRecoveryView>();
        var settings=new SerializedObject(view);settings.FindProperty("label").objectReferenceValue=root.GetComponentInChildren<TextMeshProUGUI>();
        settings.FindProperty("moreSpinButton").objectReferenceValue=root.GetComponentInChildren<Button>();
        settings.FindProperty("countFormat").stringValue="<gradient=\"spin\">SPIN {0}</gradient>";
        settings.FindProperty("timeFormat").stringValue="{0:D2}:{1:D2}";
        settings.FindProperty("countdownFormat").stringValue="<gradient=\"spin\">SPIN {0}</gradient> <size=#48>{1}</size>";
        settings.ApplyModifiedPropertiesWithoutUndo();settings=new SerializedObject(field.GetComponent<RecoveredSpinPlayfield>());
        settings.FindProperty("spinRecovery").objectReferenceValue=view;settings.ApplyModifiedPropertiesWithoutUndo();
    }
}
