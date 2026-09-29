using System.IO;
using System.Text.RegularExpressions;
using DragonLegend.Whitebox;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public static class BuildMainUtility
{
    const string Folder="Assets/Resources/RecoveredUI/MainUtility";
    public static void Save()
    {
        Directory.CreateDirectory(Folder);Directory.CreateDirectory("Artifacts");AssetDatabase.Refresh();
        Window("Help","UIHelpView");Window("Settings","UISettingView");Window("Privacy","UIPrivacyView");
        var root=new GameObject("MainUtility",typeof(RectTransform),typeof(RecoveredMainUtility));root.layer=5;
        try {
            var r=(RectTransform)root.transform;r.anchorMin=r.anchorMax=new Vector2(.5f,1);r.pivot=new Vector2(.5f,1);r.sizeDelta=new Vector2(1080,294.43f);
            var p=new SerializedObject(root.GetComponent<RecoveredMainUtility>());
            Set(p,"helpButton",Entry("HelpBtn",root.transform));Set(p,"settingsButton",Entry("SettingBtn",root.transform));
            Set(p,"helpPrefab",AssetDatabase.LoadAssetAtPath<RecoveredUtilityWindow>(Folder+"/Help.prefab"));
            Set(p,"settingsPrefab",AssetDatabase.LoadAssetAtPath<RecoveredUtilityWindow>(Folder+"/Settings.prefab"));
            Set(p,"privacyPrefab",AssetDatabase.LoadAssetAtPath<RecoveredUtilityWindow>(Folder+"/Privacy.prefab"));
            p.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(root,Folder+"/Entry.prefab");
        }finally{Object.DestroyImmediate(root);}
        const string path="Assets/Resources/Whitebox/GameEntry.prefab";root=PrefabUtility.LoadPrefabContents(path);
        try {var p=new SerializedObject(root.GetComponent<GameEntry>());Set(p,"mainUtilityPrefab",AssetDatabase.LoadAssetAtPath<RecoveredMainUtility>(Folder+"/Entry.prefab"));p.ApplyModifiedPropertiesWithoutUndo();
            ((RectTransform)root.transform.Find("GmToggle")).anchoredPosition=new Vector2(610,-31);
            PrefabUtility.SaveAsPrefabAsset(root,path);}
        finally{PrefabUtility.UnloadPrefabContents(root);}AssetDatabase.SaveAssets();
    }
    static Button Entry(string name,Transform parent)
    {
        string yaml=File.ReadAllText("ReferenceOriginal/Res/ViewPrefabs/UIMainView.prefab").Replace("\r","");
        var blocks=Regex.Matches(yaml,@"--- !u!\d+ &(\d+)\n.*?(?=\n--- !u!|\z)",RegexOptions.Singleline);
        string go=null;foreach(Match b in blocks)if(b.Value.StartsWith("--- !u!1 ")&&b.Value.Contains("  m_Name: "+name+"\n")){go=b.Value;break;}
        if(go==null)throw new InvalidDataException(name);
        string extracted="%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n"+go+"\n";
        foreach(Match id in Regex.Matches(go,@"component: \{fileID: (\d+)\}"))foreach(Match b in blocks)if(b.Groups[1].Value==id.Groups[1].Value)extracted+=Regex.Replace(b.Value,@"m_Father: \{fileID: \d+\}","m_Father: {fileID: 0}")+"\n";
        string source="Artifacts/"+name+".prefab";File.WriteAllText(source,extracted);
        string path=Folder+"/"+name+".prefab";var imported=BuildDailyTasks.Import(source,path);
        try{PrefabUtility.SaveAsPrefabAsset(imported,path);}finally{PrefabUtility.UnloadPrefabContents(imported);}
        return ((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),parent)).GetComponent<Button>();
    }
    static void Window(string name,string original)
    {
        string path=Folder+"/"+name+".prefab";var root=BuildDailyTasks.Import("ReferenceOriginal/Res/ViewPrefabs/"+original+".prefab",path);
        try {
            var p=new SerializedObject(root.AddComponent<RecoveredUtilityWindow>());var content=root.transform.Find("Content");
            Set(p,"close",content.Find("CloseBtn").GetComponent<Button>());
            if(name=="Help"){
                Set(p,"left",content.Find("leftBtn").GetComponent<Button>());Set(p,"right",content.Find("rightBtn").GetComponent<Button>());
                var pages=p.FindProperty("pages");pages.arraySize=3;for(int i=0;i<3;i++)pages.GetArrayElementAtIndex(i).objectReferenceValue=content.Find((i+1).ToString()).gameObject;
            }
            if(name=="Settings"){
                foreach(string pair in new[]{"help:HelpBtn","music:Music","sound:Sound"}){var s=pair.Split(':');Set(p,s[0],content.Find(s[1]).GetComponent<Button>());}
                Set(p,"musicOn",content.Find("Music/On").gameObject);Set(p,"musicOff",content.Find("Music/OFF").gameObject);
                Set(p,"soundOn",content.Find("Sound/On").gameObject);Set(p,"soundOff",content.Find("Sound/OFF").gameObject);
                // These two entries are intentionally absent from the shipped settings window.
                Object.DestroyImmediate(content.Find("ContractBtn").gameObject);
                Object.DestroyImmediate(content.Find("TermBtn").gameObject);
            }
            if(name=="Privacy")Set(p,"scroll",content.Find("Scroll View").GetComponent<ScrollRect>());
            p.ApplyModifiedPropertiesWithoutUndo();
            foreach(var graphic in root.GetComponentsInChildren<Graphic>(true))graphic.raycastTarget=graphic.GetComponent<Button>()!=null||graphic.GetComponent<Mask>()!=null||graphic.GetComponent<ScrollRect>()!=null;
            var bg=new GameObject("_WindowBg",typeof(RectTransform),typeof(Image));bg.layer=5;bg.transform.SetParent(root.transform,false);bg.transform.SetAsFirstSibling();
            var rect=(RectTransform)bg.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.sizeDelta=Vector2.zero;bg.GetComponent<Image>().color=new Color(0,0,0,.65f);
            root.GetComponent<Canvas>().sortingOrder=300;root.SetActive(false);PrefabUtility.SaveAsPrefabAsset(root,path);
        }finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    static void Set(SerializedObject p,string field,Object value){if(value==null)throw new InvalidDataException("Missing "+field);p.FindProperty(field).objectReferenceValue=value;}
}
