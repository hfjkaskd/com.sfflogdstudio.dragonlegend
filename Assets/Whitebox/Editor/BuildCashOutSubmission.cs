using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using DragonLegend.Whitebox;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public static class BuildCashOutSubmission
{
    public static void Save()
    {
        var map=new Dictionary<string,string>();
        BuildTreasureCard.Script<Image>(map,"3cf5a44414476512e00c3e7a2569a919");
        BuildTreasureCard.Script<Button>(map,"18d0a90695249463551c00f45766e642");
        BuildTreasureCard.Script<TextMeshProUGUI>(map,"3f96b1d166d19b209697e35b35d65c76");
        BuildTreasureCard.Script<TMP_InputField>(map,"dfb118be96e34ebd31373e9193059731");
        BuildTreasureCard.Script<GraphicRaycaster>(map,"86fe8f3fc59dc06ea6b45a1bbee64682");
        BuildTreasureCard.Script<RectMask2D>(map,"69dacd7a039c12e90cde90bbae65247a");
        BuildTreasureCard.Script<LayoutElement>(map,"4293fd42559bc8bb1c81715179adf536");
        string source=File.ReadAllText("ReferenceOriginal/Res/ViewPrefabs/UIAccountView.prefab").Replace("\r","");
        var removed=new List<string>();
        source=Regex.Replace(source,@"--- !u!114 &(\d+)\n.*?(?=\n--- !u!|\z)",m=>{
            var script=Regex.Match(m.Value,@"m_Script:.*guid: (\w+)");
            if(script.Success&&!map.ContainsKey(script.Groups[1].Value)){removed.Add(m.Groups[1].Value);return "";}return m.Value;
        },RegexOptions.Singleline);
        foreach(var id in removed)source=source.Replace("  - component: {fileID: "+id+"}\n","");
        foreach(var meta in Directory.GetFiles("ReferenceOriginal/Res/UI","*.asset.meta",SearchOption.AllDirectories))
        {
            string guid=Regex.Match(File.ReadAllText(meta),@"guid: (\w+)").Groups[1].Value;
            if(!source.Contains(guid)||!string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(guid)))continue;
            string original=File.ReadAllText(meta.Substring(0,meta.Length-5));
            string name=Regex.Match(original,@"m_Name: (.*)").Groups[1].Value.Trim();
            var sprites=Directory.GetFiles("Assets/Resources/RecoveredArt/IndividualSprites",name+"_*.png");
            if(sprites.Length!=1)throw new InvalidDataException("Account sprite "+name);
            string spritePath=sprites[0].Replace('\\','/');
            var border=Regex.Match(original,@"m_Border: \{x: ([^,]+), y: ([^,]+), z: ([^,]+), w: ([^}]+)\}");
            if(border.Success)
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(spritePath);
                var expected=new Vector4(float.Parse(border.Groups[1].Value,CultureInfo.InvariantCulture),float.Parse(border.Groups[2].Value,CultureInfo.InvariantCulture),float.Parse(border.Groups[3].Value,CultureInfo.InvariantCulture),float.Parse(border.Groups[4].Value,CultureInfo.InvariantCulture));
                importer.spriteBorder=expected;
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
                importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.textureCompression=TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            map[guid]=AssetDatabase.AssetPathToGUID(spritePath);
        }
        foreach(var pair in map)source=source.Replace(pair.Key,pair.Value);
        source=Regex.Replace(source,@"(m_Sprite:.*)type: 2","$1type: 3");
        foreach(Match match in Regex.Matches(source,@"guid: (\w+)"))
            if(string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(match.Groups[1].Value)))throw new InvalidDataException("Account missing GUID "+match.Groups[1].Value);
        const string temporary="Assets/Whitebox/Editor/AccountSource.prefab";
        File.WriteAllText(temporary,source);AssetDatabase.ImportAsset(temporary,ImportAssetOptions.ForceSynchronousImport);
        var root=PrefabUtility.LoadPrefabContents(temporary);
        try
        {
            var content=root.transform.Find("Content");
            var view=root.AddComponent<RecoveredAccountWindow>();var settings=new SerializedObject(view);
            settings.FindProperty("nameInput").objectReferenceValue=content.Find("Name").GetComponent<TMP_InputField>();
            settings.FindProperty("emailInput").objectReferenceValue=content.Find("Email").GetComponent<TMP_InputField>();
            TMP_Text prompt=null;
            foreach(Transform child in content){var text=child.GetComponent<TMP_Text>();if(text!=null&&text.text.StartsWith("Please enter")){prompt=text;break;}}
            if(prompt==null)throw new InvalidDataException("Account prompt missing");
            prompt.name="AccountPrompt";settings.FindProperty("detail").objectReferenceValue=prompt;
            settings.FindProperty("accountPlaceholder").objectReferenceValue=content.Find("Email/Text Area/Placeholder").GetComponent<TMP_Text>();
            settings.FindProperty("accountPlaceholderFormat").stringValue="Your {0} Account";
            settings.FindProperty("submit").objectReferenceValue=content.Find("SubmitBtn").GetComponent<Button>();
            settings.FindProperty("close").objectReferenceValue=content.Find("CloseBtn").GetComponent<Button>();
            settings.FindProperty("promptFormat").stringValue="Please enter your {0} account";
            settings.FindProperty("requiredMessage").stringValue="Please enter your full name and account.";
            var providers=settings.FindProperty("providers");providers.arraySize=4;
            var names=new[]{"PayPal","CashApp","CoinBase","Zelle"};for(int i=0;i<4;i++)providers.GetArrayElementAtIndex(i).stringValue=names[i];
            settings.ApplyModifiedPropertiesWithoutUndo();
            // Authored modal input blocker, underneath the original account controls.
            var blocker=new GameObject("ModalBackground",typeof(RectTransform),typeof(Image),typeof(Button));
            blocker.layer=5;blocker.transform.SetParent(root.transform,false);blocker.transform.SetAsFirstSibling();
            var rect=(RectTransform)blocker.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.sizeDelta=Vector2.zero;
            var image=blocker.GetComponent<Image>();image.color=new Color(0,0,0,.65f);
            blocker.GetComponent<Button>().targetGraphic=image;
            root.GetComponent<Canvas>().overrideSorting=true;root.GetComponent<Canvas>().sortingOrder=400;
            foreach(var button in root.GetComponentsInChildren<Button>(true))button.onClick=new Button.ButtonClickedEvent();
            foreach(var graphic in content.GetComponentsInChildren<Image>(true))
                if(graphic.sprite==null)throw new InvalidDataException("Account image missing sprite: "+graphic.name);
            foreach(var input in root.GetComponentsInChildren<TMP_InputField>(true)){input.readOnly=false;input.interactable=true;}
            content.localScale=Vector3.one;root.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(root,"Assets/Resources/RecoveredUI/AccountWindow.prefab");
        }
        finally{PrefabUtility.UnloadPrefabContents(root);AssetDatabase.DeleteAsset(temporary);}
        AttachWindow();AssetDatabase.SaveAssets();
    }
    public static void AttachWindow()
    {
        const string path="Assets/Resources/RecoveredUI/CashOutWindow.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            var settings=new SerializedObject(root.GetComponent<RecoveredCashOutWindow>());
            settings.FindProperty("accountPrefab").objectReferenceValue=AssetDatabase.LoadAssetAtPath<RecoveredAccountWindow>("Assets/Resources/RecoveredUI/AccountWindow.prefab");
            settings.FindProperty("tipsPrefab").objectReferenceValue=AssetDatabase.LoadAssetAtPath<RecoveredTipsWindow>("Assets/Resources/RecoveredUI/TipsWindow.prefab");
            const string pending="Withdrawal request pending.";
            settings.FindProperty("pendingMessage").stringValue=pending;
            settings.FindProperty("failedMessage").stringValue="Unable to submit. Please try again.";
            settings.FindProperty("giftUnavailableMessage").stringValue="Gift delivery is not available yet.";
            settings.ApplyModifiedPropertiesWithoutUndo();
            var bottom=new SerializedObject(root.GetComponentInChildren<RecoveredCashOutBottom>(true));
            bottom.FindProperty("pendingOrderText").stringValue=pending;bottom.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
}
