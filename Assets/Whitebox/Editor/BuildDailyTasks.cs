using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using DragonLegend.Whitebox;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

// Offline authoring only: all layout, sprites and Button components are saved in prefabs.
public static class BuildDailyTasks
{
    const string Folder="Assets/Resources/RecoveredUI/DailyTasks";
    static Dictionary<string,string> originals;
    public static void Save()
    {
        Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
        originals=new Dictionary<string,string>();
        foreach(string file in Directory.GetFiles("ReferenceOriginal/Res/UI","*.meta",SearchOption.AllDirectories)) {
            var m=Regex.Match(File.ReadAllText(file),@"guid: (\w+)");if(m.Success)originals[m.Groups[1].Value]=file.Substring(0,file.Length-5);
        }
        BuildItem();BuildReward();BuildWindow();BuildEntry();
        BuildFreeVisibility.Save();
        const string path="Assets/Resources/Whitebox/GameEntry.prefab";var root=PrefabUtility.LoadPrefabContents(path);
        try {var p=new SerializedObject(root.GetComponent<GameEntry>());Set(p,"dailyTaskPrefab",AssetDatabase.LoadAssetAtPath<RecoveredDailyTaskEntry>(Folder+"/Entry.prefab"));p.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(root,path);}
        finally{PrefabUtility.UnloadPrefabContents(root);}AssetDatabase.SaveAssets();
    }
    public static GameObject Import(string source,string output)
    {
        if(originals==null){originals=new Dictionary<string,string>();foreach(string file in Directory.GetFiles("ReferenceOriginal/Res/UI","*.meta",SearchOption.AllDirectories)){var m=Regex.Match(File.ReadAllText(file),@"guid: (\w+)");if(m.Success)originals[m.Groups[1].Value]=file.Substring(0,file.Length-5);}}
        var map=new Dictionary<string,string>();
        BuildTreasureCard.Script<ScrollRect>(map,"2cbaf7f938a676aaf8fab3181b92a517");
        BuildTreasureCard.Script<Mask>(map,"16c0ec1e295995dd9a2d2df2d7b6cc7e");
        BuildTreasureCard.Script<ContentSizeFitter>(map,"21c7954052da7655d96bff866c2b7662");
        BuildTreasureCard.Script<Image>(map,"3cf5a44414476512e00c3e7a2569a919");BuildTreasureCard.Script<Button>(map,"18d0a90695249463551c00f45766e642");
        BuildTreasureCard.Script<TextMeshProUGUI>(map,"3f96b1d166d19b209697e35b35d65c76");BuildTreasureCard.Script<GraphicRaycaster>(map,"86fe8f3fc59dc06ea6b45a1bbee64682");
        BuildTreasureCard.Map(map,"39211f061913f054f84255431c8dce45","Assets/Resources/RecoveredUI/BonusRewardPopup/#0A5902_4.mat");
        BuildTreasureCard.Map(map,"183b0e4b7b3c5a34fa991aebe104ca31","Assets/Resources/RecoveredUI/BonusRewardPopup/tc_btn_bofang.asset");
        BuildTreasureCard.Map(map,"636692ea04314474e8e2cc85012b4525","Assets/Resources/Color Gradient Presets/spin.asset");
        string yaml=File.ReadAllText(source).Replace("\r","");var removed=new List<string>();
        yaml=Regex.Replace(yaml,@"--- !u!114 &(\d+)\n.*?(?=\n--- !u!|\z)",m=>{
            var script=Regex.Match(m.Value,@"m_Script:.*guid: (\w+)");
            if(script.Success&&!map.ContainsKey(script.Groups[1].Value)){removed.Add(m.Groups[1].Value);return "";}return m.Value;
        },RegexOptions.Singleline);
        foreach(string id in removed)yaml=yaml.Replace("  - component: {fileID: "+id+"}\n","");
        var spriteIds=new HashSet<string>();foreach(Match m in Regex.Matches(yaml,@"m_Sprite:.*guid: (\w+)"))if(!m.Groups[1].Value.StartsWith("0000000000000000"))spriteIds.Add(m.Groups[1].Value);
        foreach(string id in spriteIds) {
            string original=originals[id];string name=Path.GetFileNameWithoutExtension(original);
            string relative=original.Replace('\\','/').Replace("ReferenceOriginal/","");
            string png="Assets/Resources/RecoveredArt/"+Path.ChangeExtension(relative,"png");
            if(!File.Exists(png)) {
                string[] candidates=Directory.GetFiles("Assets/Resources/RecoveredArt/IndividualSprites",name+"_*.png");
                if(candidates.Length!=1)throw new InvalidDataException("Cannot map task sprite "+original);png=candidates[0].Replace('\\','/');
            }
            var importer=(TextureImporter)AssetImporter.GetAtPath(png);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.SaveAndReimport();map[id]=AssetDatabase.AssetPathToGUID(png);
        }
        foreach(var pair in map)yaml=yaml.Replace(pair.Key,pair.Value);
        foreach(string id in spriteIds)yaml=yaml.Replace("guid: "+map[id]+", type: 2","guid: "+map[id]+", type: 3");
        foreach(Match m in Regex.Matches(yaml,@"guid: (\w+)"))if(string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(m.Groups[1].Value)))throw new InvalidDataException("Unmapped task asset "+m.Groups[1].Value);
        File.WriteAllText(output,yaml);AssetDatabase.ImportAsset(output,ImportAssetOptions.ForceSynchronousImport);return PrefabUtility.LoadPrefabContents(output);
    }
    static void BuildItem()
    {
        string path=Folder+"/Item.prefab";var root=Import("ReferenceOriginal/Res/Prefabs/TaskItem.prefab",path);
        try {
            var p=new SerializedObject(root.AddComponent<RecoveredDailyTaskItem>());
            Set(p,"reward",root.transform.Find("Image/GreenTxt").GetComponent<TMP_Text>());Set(p,"description",root.transform.Find("Text (TMP)").GetComponent<TMP_Text>());
            Set(p,"progress",root.transform.Find("Progress/Text (TMP)").GetComponent<TMP_Text>());
            var fill=root.transform.Find("Progress/Image").GetComponent<Image>();fill.type=Image.Type.Filled;fill.fillMethod=Image.FillMethod.Horizontal;fill.fillOrigin=0;Set(p,"fill",fill);
            Set(p,"claim",root.transform.Find("ClaimBtn").GetComponent<Button>());Set(p,"finished",root.transform.Find("Image").gameObject);Set(p,"black",root.transform.Find("black").gameObject);
            root.transform.Find("ClaimBtn").GetComponent<Button>().transition=Selectable.Transition.ColorTint;
            // The finish tick is the Image following ClaimBtn, not the background decoration.
            foreach(var image in root.GetComponentsInChildren<Image>(true))if(image.sprite!=null&&image.sprite.name=="re_gou")Set(p,"finished",image.gameObject);
            p.ApplyModifiedPropertiesWithoutUndo();foreach(var graphic in root.GetComponentsInChildren<Graphic>(true))graphic.raycastTarget=graphic.gameObject==root||graphic.GetComponent<Button>()!=null;
            root.SetActive(true);PrefabUtility.SaveAsPrefabAsset(root,path);
        }finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    static void BuildReward()
    {
        var root=PrefabUtility.LoadPrefabContents("Assets/Resources/RecoveredUI/BonusRewardPopup.prefab");
        try {
            var prior=root.GetComponent<RecoveredBonusRewardPopup>();var p=new SerializedObject(root.AddComponent<RecoveredTaskRewardWindow>());
            Set(p,"popup",prior);
            p.ApplyModifiedPropertiesWithoutUndo();root.SetActive(false);PrefabUtility.SaveAsPrefabAsset(root,Folder+"/Reward.prefab");
        }finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    static void BuildWindow()
    {
        string path=Folder+"/Window.prefab";var root=Import("ReferenceOriginal/Res/ViewPrefabs/UIDailyTaskView.prefab",path);
        try {
            var p=new SerializedObject(root.AddComponent<RecoveredDailyTaskWindow>());var content=root.transform.Find("Content");
            Set(p,"close",content.Find("CloseBtn").GetComponent<Button>());Set(p,"timer",content.Find("Text (TMP)").GetComponent<TMP_Text>());
            var viewport=(RectTransform)content.Find("Rect");viewport.gameObject.AddComponent<RectMask2D>();
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.viewport=viewport;scroll.movementType=ScrollRect.MovementType.Clamped;
            var list=new GameObject("TaskList",typeof(RectTransform));list.transform.SetParent(viewport,false);var rect=(RectTransform)list.transform;
            rect.anchorMin=new Vector2(0,1);rect.anchorMax=Vector2.one;rect.pivot=new Vector2(.5f,1);rect.sizeDelta=new Vector2(0,6*250);scroll.content=rect;
            var rows=p.FindProperty("items");rows.arraySize=6;
            for(int i=0;i<6;i++) {
                var row=(RecoveredDailyTaskItem)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<RecoveredDailyTaskItem>(Folder+"/Item.prefab"),rect);
                var r=(RectTransform)row.transform;r.anchorMin=r.anchorMax=new Vector2(.5f,1);r.pivot=new Vector2(.5f,1);r.anchoredPosition=new Vector2(0,-i*250);r.localScale=Vector3.one;rows.GetArrayElementAtIndex(i).objectReferenceValue=row;
            }
            var reward=(RecoveredTaskRewardWindow)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<RecoveredTaskRewardWindow>(Folder+"/Reward.prefab"),root.transform);Set(p,"rewardWindow",reward);p.ApplyModifiedPropertiesWithoutUndo();
            var bg=new GameObject("_WindowBg",typeof(RectTransform),typeof(Image),typeof(Button));bg.transform.SetParent(root.transform,false);bg.transform.SetAsFirstSibling();
            var b=(RectTransform)bg.transform;b.anchorMin=Vector2.zero;b.anchorMax=Vector2.one;b.sizeDelta=Vector2.zero;var img=bg.GetComponent<Image>();img.color=new Color(0,0,0,.65f);bg.GetComponent<Button>().targetGraphic=img;
            root.GetComponent<Canvas>().sortingOrder=300;root.SetActive(false);PrefabUtility.SaveAsPrefabAsset(root,path);
        }finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    static void BuildEntry()
    {
        string texturePath="Assets/Resources/RecoveredArt/Res/Spine/按钮/task/ef_taskicon.png";var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        Sprite Make(string name,Rect pixels){string path=Folder+"/"+name+".asset";var old=AssetDatabase.LoadAssetAtPath<Sprite>(path);if(old!=null)return old;
            var sprite=Sprite.Create(texture,pixels,new Vector2(.5f,.5f),100);sprite.name=name;AssetDatabase.CreateAsset(sprite,path);return sprite;}
        var root=new GameObject("Task",typeof(RectTransform),typeof(Image),typeof(Button),typeof(RecoveredDailyTaskEntry));root.layer=5;
        try {
            var r=(RectTransform)root.transform;r.anchorMin=r.anchorMax=new Vector2(1,1);r.pivot=new Vector2(1,1);r.sizeDelta=new Vector2(202,250);r.localScale=new Vector3(.85f,.85f,1);r.anchoredPosition=new Vector2(-14,-233.625f);
            var bg=root.GetComponent<Image>();bg.sprite=Make("TaskRing",new Rect(159,2,130,130));bg.preserveAspect=true;root.GetComponent<Button>().targetGraphic=bg;
            void Art(string name,Sprite sprite,Vector2 size,Vector2 position,float rotation=0){var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(root.transform,false);var t=(RectTransform)go.transform;t.sizeDelta=size;t.anchoredPosition=position;t.localEulerAngles=new Vector3(0,0,rotation);var image=go.GetComponent<Image>();image.sprite=sprite;image.raycastTarget=false;}
            Art("Book",Make("TaskBook",new Rect(291,2,102,128)),new Vector2(125,155),new Vector2(-8.176471f,1.9117647f));
            Art("Pen",Make("TaskPen",new Rect(395,79,78,56)),new Vector2(95,68),new Vector2(39.82353f,-26.088236f),-90);
            var label=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI));label.transform.SetParent(root.transform,false);var lr=(RectTransform)label.transform;lr.sizeDelta=new Vector2(200,45);lr.anchoredPosition=new Vector2(-.1764706f,-91.088234f);
            var text=label.GetComponent<TextMeshProUGUI>();text.font=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/Item.prefab").GetComponentInChildren<TMP_Text>().font;text.text="TASK";text.fontSize=32;text.fontStyle=FontStyles.Bold;text.color=new Color(1,1,.3f);text.alignment=TextAlignmentOptions.Center;text.raycastTarget=false;
            var p=new SerializedObject(root.GetComponent<RecoveredDailyTaskEntry>());Set(p,"button",root.GetComponent<Button>());Set(p,"windowPrefab",AssetDatabase.LoadAssetAtPath<RecoveredDailyTaskWindow>(Folder+"/Window.prefab"));p.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(root,Folder+"/Entry.prefab");
        }finally{Object.DestroyImmediate(root);}
    }
    static void Set(SerializedObject p,string name,Object value)=>p.FindProperty(name).objectReferenceValue=value;
}
