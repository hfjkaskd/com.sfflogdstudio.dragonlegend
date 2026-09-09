using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using DragonLegend.Whitebox;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class BuildCashOutWindow
{
    public static void Save()
    {
        var map=new Dictionary<string,string>();
        BuildTreasureCard.Script<RecoveredScreenAdapt>(map,"3991d2bd099e12203576b2cf89b113da");
        BuildTreasureCard.Script<Image>(map,"3cf5a44414476512e00c3e7a2569a919");BuildTreasureCard.Script<Button>(map,"18d0a90695249463551c00f45766e642");
        BuildTreasureCard.Script<TextMeshProUGUI>(map,"3f96b1d166d19b209697e35b35d65c76");BuildTreasureCard.Script<TMP_InputField>(map,"dfb118be96e34ebd31373e9193059731");
        BuildTreasureCard.Script<GraphicRaycaster>(map,"86fe8f3fc59dc06ea6b45a1bbee64682");BuildTreasureCard.Script<RectMask2D>(map,"69dacd7a039c12e90cde90bbae65247a");
        BuildTreasureCard.Script<HorizontalLayoutGroup>(map,"19fbb4a32b59286cd89b624f52ff7943");BuildTreasureCard.Script<VerticalLayoutGroup>(map,"f18f2cc2fe71a3a6d76a570588d5047c");
        BuildTreasureCard.Script<LayoutElement>(map,"4293fd42559bc8bb1c81715179adf536");BuildTreasureCard.Script<ContentSizeFitter>(map,"21c7954052da7655d96bff866c2b7662");
        string source=File.ReadAllText("ReferenceOriginal/Res/ViewPrefabs/UICashOutView.prefab").Replace("\r","");
        var removed=new List<string>();
        source=Regex.Replace(source,@"--- !u!114 &(\d+)\n.*?(?=\n--- !u!|\z)",m=>{
            var script=Regex.Match(m.Value,@"m_Script:.*guid: (\w+)");
            if(script.Success&&!map.ContainsKey(script.Groups[1].Value)){removed.Add(m.Groups[1].Value);return "";}return m.Value;
        },RegexOptions.Singleline);
        foreach(var id in removed)source=source.Replace("  - component: {fileID: "+id+"}\n","");
        foreach(var metaPath in Directory.GetFiles("ReferenceOriginal/Res/UI/tixian","*.asset.meta"))
        {
            string guid=Regex.Match(File.ReadAllText(metaPath),@"guid: (\w+)").Groups[1].Value;if(!source.Contains(guid))continue;
            string original=File.ReadAllText(metaPath.Substring(0,metaPath.Length-5));string name=Regex.Match(original,@"m_Name: (.*)").Groups[1].Value.Trim();
            var matches=Directory.GetFiles("Assets/Resources/RecoveredArt/IndividualSprites",name+"_*.png");if(matches.Length!=1)throw new InvalidDataException("Cash window sprite "+name);
            string path=matches[0].Replace('\\','/');var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            var border=Regex.Match(original,@"m_Border: \{x: ([^,]+), y: ([^,]+), z: ([^,]+), w: ([^}]+)\}");if(!border.Success)throw new InvalidDataException("Sprite border "+name);
            importer.spriteBorder=new Vector4(Parse(border.Groups[1].Value),Parse(border.Groups[2].Value),Parse(border.Groups[3].Value),Parse(border.Groups[4].Value));
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();BuildTreasureCard.Map(map,guid,path);
        }
        foreach(var pair in map)source=source.Replace(pair.Key,pair.Value);source=Regex.Replace(source,@"(m_Sprite:.*)type: 2","$1type: 3");
        foreach(Match m in Regex.Matches(source,@"guid: (\w+)"))if(string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(m.Groups[1].Value)))throw new InvalidDataException("Cash window missing GUID "+m.Groups[1].Value);
        const string temp="Assets/Whitebox/Editor/CashWindowSource.prefab";File.WriteAllText(temp,source);AssetDatabase.ImportAsset(temp,ImportAssetOptions.ForceSynchronousImport);
        var root=PrefabUtility.LoadPrefabContents(temp);
        try
        {
            var content=root.transform.Find("Content");var old=(RectTransform)content.Find("Bottom");
            var bottom=(RecoveredCashOutBottom)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<RecoveredCashOutBottom>("Assets/Resources/RecoveredUI/CashOutBottom.prefab"),content);
            var rect=(RectTransform)bottom.transform;rect.name="Bottom";rect.anchorMin=old.anchorMin;rect.anchorMax=old.anchorMax;rect.pivot=old.pivot;rect.sizeDelta=old.sizeDelta;rect.anchoredPosition3D=old.anchoredPosition3D;rect.localRotation=old.localRotation;rect.localScale=old.localScale;rect.SetSiblingIndex(old.GetSiblingIndex());Object.DestroyImmediate(old.gameObject);
            var list=(RecoveredCashOutList)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<RecoveredCashOutList>("Assets/Resources/RecoveredUI/CashOutList.prefab"),content.Find("Node/Rect"));list.name="CashOutList";
            Object.DestroyImmediate(content.Find("Node/ItemKuang").gameObject);
            var header=root.AddComponent<RecoveredCashOutPaymentHeader>();var settings=new SerializedObject(header);
            settings.FindProperty("list").objectReferenceValue=list;settings.FindProperty("paymentLayout").objectReferenceValue=content.Find("Node/Layout");settings.FindProperty("paymentFrame").objectReferenceValue=content.Find("Node/Layout/PaypalBtn/kuang");
            settings.FindProperty("accountInput").objectReferenceValue=content.Find("Top/InputField (TMP)").GetComponent<TMP_InputField>();settings.FindProperty("accountButton").objectReferenceValue=content.Find("Top/InputField (TMP)/AccountBtn").GetComponent<Button>();
            var buttons=settings.FindProperty("paymentButtons");buttons.arraySize=4;var names=new[]{"PaypalBtn","CashAppBtn","CoinBaseBtn","ZelleBtn"};for(int i=0;i<4;i++)buttons.GetArrayElementAtIndex(i).objectReferenceValue=content.Find("Node/Layout/"+names[i]).GetComponent<Button>();
            var providers=settings.FindProperty("providerNames");providers.arraySize=4;var labels=new[]{"Paypal","CashApp","CoinBase","Zelle"};for(int i=0;i<4;i++)providers.GetArrayElementAtIndex(i).stringValue=labels[i];
            settings.FindProperty("visibleConfigType").stringValue="default";settings.FindProperty("promptPrefix").stringValue="Please enter your ";settings.FindProperty("promptSuffix").stringValue=" account here.";settings.FindProperty("giftPrompt").stringValue="Please enter your account here.";settings.ApplyModifiedPropertiesWithoutUndo();
            var entrance=root.AddComponent<RecoveredCashOutEntrance>();var animation=new SerializedObject(entrance);
            animation.FindProperty("list").objectReferenceValue=list;animation.FindProperty("bottom").objectReferenceValue=bottom.transform;
            animation.FindProperty("cardDuration").floatValue=.2f;animation.FindProperty("cardInterval").floatValue=.05f;animation.FindProperty("bottomDuration").floatValue=.3f;animation.FindProperty("bottomOffset").floatValue=450;
            animation.FindProperty("cardEase").animationCurveValue=new AnimationCurve(new Keyframe(0,0,4.70158f,4.70158f),new Keyframe(1,1,0,0));
            animation.FindProperty("bottomEase").animationCurveValue=new AnimationCurve(new Keyframe(0,0,2,2),new Keyframe(1,1,0,0));animation.ApplyModifiedPropertiesWithoutUndo();
            foreach(var button in root.GetComponentsInChildren<Button>(true))if(button.onClick.GetPersistentEventCount()!=0)throw new InvalidDataException("Unexpected window event "+button.name);
            BuildCashOutModeView.Attach(root);
            BuildGiftList.Attach(root);
            root.SetActive(false);PrefabUtility.SaveAsPrefabAsset(root,"Assets/Resources/RecoveredUI/CashOutWindow.prefab");AssetDatabase.SaveAssets();
        }
        finally{PrefabUtility.UnloadPrefabContents(root);AssetDatabase.DeleteAsset(temp);}
    }
    private static float Parse(string text)=>float.Parse(text,CultureInfo.InvariantCulture);
}
