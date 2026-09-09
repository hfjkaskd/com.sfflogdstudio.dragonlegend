using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using DragonLegend.Whitebox;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public static class BuildCashPromptWindow
{
    public static void Save()
    {
        BuildJackpotPopupArt.Create("ef_tixiantc","tixiantc",new Vector2(1015,1032.2754f),new Vector2(.49442595f,.38436967f),"Tools/Evidence/","Artifacts/CashPromptAuthoring/");
        var map=new Dictionary<string,string>();
        BuildTreasureCard.Script<Image>(map,"3cf5a44414476512e00c3e7a2569a919");BuildTreasureCard.Script<Button>(map,"18d0a90695249463551c00f45766e642");
        BuildTreasureCard.Script<TextMeshProUGUI>(map,"3f96b1d166d19b209697e35b35d65c76");BuildTreasureCard.Script<GraphicRaycaster>(map,"86fe8f3fc59dc06ea6b45a1bbee64682");
        BuildTreasureCard.Script<HorizontalLayoutGroup>(map,"19fbb4a32b59286cd89b624f52ff7943");BuildTreasureCard.Script<ContentSizeFitter>(map,"21c7954052da7655d96bff866c2b7662");BuildTreasureCard.Script<Text>(map,"04f84fc2003509a5e7e068ec1271cc40");
        string source=File.ReadAllText("ReferenceOriginal/Res/ViewPrefabs/UICashOutTipView.prefab").Replace("\r","");var removed=new List<string>();
        source=Regex.Replace(source,@"--- !u!\d+ &(\d+)\n.*?(?=\n--- !u!|\z)",m=>{var script=Regex.Match(m.Value,@"m_Script:.*guid: (\w+)");if(script.Success&&!map.ContainsKey(script.Groups[1].Value)){removed.Add(m.Groups[1].Value);return "";}return m.Value;},RegexOptions.Singleline);
        foreach(var id in removed)source=source.Replace("  - component: {fileID: "+id+"}\n","");
        foreach(var pair in new Dictionary<string,string>{{"e2c5f1980f2df614283d536887099d4c","IndividualSprites/tx_9congratulations_txt_-3940978220648715811"},{"345f74445a0eb594cae2596a8d0f1f2c","IndividualSprites/tx_9icon_01_-1457144487873908340"},{"f4b25a1c65682f9409a101f75b641504","Res/UI/pop-up/tc_btn_01"}})
        {string imagePath="Assets/Resources/RecoveredArt/"+pair.Value+".png";if(pair.Value.StartsWith("IndividualSprites/")){var importer=(TextureImporter)AssetImporter.GetAtPath(imagePath);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();}BuildTreasureCard.Map(map,pair.Key,imagePath);source=source.Replace("guid: "+pair.Key+", type: 2","guid: "+pair.Key+", type: 3");}
        BuildTreasureCard.Map(map,"48b6da1693bf90e4faa7954e01184c75","Assets/Resources/RecoveredUI/CollectWindowArt/Gold.asset");
        BuildTreasureCard.Map(map,"39211f061913f054f84255431c8dce45","Assets/Resources/RecoveredUI/BonusRewardPopup/#0A5902_4.mat");BuildTreasureCard.Map(map,"183b0e4b7b3c5a34fa991aebe104ca31","Assets/Resources/RecoveredUI/BonusRewardPopup/tc_btn_bofang.asset");
        foreach(var pair in map)source=source.Replace(pair.Key,pair.Value);
        foreach(Match m in Regex.Matches(source,@"guid: (\w+)"))if(string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(m.Groups[1].Value)))throw new InvalidDataException("Missing cash prompt GUID "+m.Groups[1].Value);
        const string temp="Assets/Whitebox/Editor/CashPromptWindowSource.prefab";File.WriteAllText(temp,source);AssetDatabase.ImportAsset(temp,ImportAssetOptions.ForceSynchronousImport);
        var root=PrefabUtility.LoadPrefabContents(temp);
        try
        {
            var content=root.transform.Find("Content");var view=root.AddComponent<RecoveredCashPromptWindow>();var data=new SerializedObject(view);
            data.FindProperty("content").objectReferenceValue=content;data.FindProperty("claim").objectReferenceValue=content.Find("ClaimBtn").GetComponent<Button>();
            data.FindProperty("cashText").objectReferenceValue=content.Find("Layout/Text (Legacy)").GetComponent<Text>();
            data.FindProperty("fingerPrefab").objectReferenceValue=AssetDatabase.LoadAssetAtPath<RectTransform>("Assets/Resources/RecoveredUI/Finger.prefab");
            data.FindProperty("showSound").stringValue="congrats";data.FindProperty("duration").floatValue=.3f;
            var old=(RectTransform)content.Find("SkeletonGraphic (ef_tixiantc)");
            var art=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/RecoveredUI/JackpotPopupArt/ef_tixiantc.prefab"),content);
            var artRect=(RectTransform)art.transform;artRect.anchorMin=old.anchorMin;artRect.anchorMax=old.anchorMax;artRect.pivot=old.pivot;artRect.sizeDelta=old.sizeDelta;artRect.anchoredPosition=old.anchoredPosition;artRect.localScale=old.localScale;artRect.SetSiblingIndex(old.GetSiblingIndex());art.layer=old.gameObject.layer;art.name=old.name;Object.DestroyImmediate(old.gameObject);
            data.FindProperty("enterEase").animationCurveValue=new AnimationCurve(new Keyframe(0,0,4.70158f,4.70158f),new Keyframe(1,1,0,0));data.FindProperty("exitEase").animationCurveValue=new AnimationCurve(new Keyframe(0,0,0,0),new Keyframe(1,1,4.70158f,4.70158f));data.ApplyModifiedPropertiesWithoutUndo();
            var shine=new SerializedObject(content.Find("Title").gameObject.AddComponent<RecoveredTitleShine>());shine.FindProperty("template").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/RecoveredUI/BonusRewardPopup/TitleShine.mat");shine.ApplyModifiedPropertiesWithoutUndo();
            root.GetComponent<Canvas>().sortingOrder=300;
            var bg=new GameObject("_WindowBg",typeof(RectTransform),typeof(Image),typeof(Button));bg.layer=root.layer;bg.transform.SetParent(root.transform,false);bg.transform.SetAsFirstSibling();
            var rect=(RectTransform)bg.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            var image=bg.GetComponent<Image>();image.color=new Color(0,0,0,.65f);var button=bg.GetComponent<Button>();button.targetGraphic=image;button.transition=Selectable.Transition.None;
            root.SetActive(false);PrefabUtility.SaveAsPrefabAsset(root,"Assets/Resources/RecoveredUI/CashPromptWindow.prefab");AssetDatabase.SaveAssets();
        }
        finally{PrefabUtility.UnloadPrefabContents(root);AssetDatabase.DeleteAsset(temp);}

    }
}
