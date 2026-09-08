using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using DragonLegend.Whitebox;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class BuildJackpotPopup
{
    const string Source = "C:/Projects/Nut Sort Relax/reconstruction/mumu-current/reference-unity/ExportedProject/Assets/";
    const string Folder = "Assets/Resources/RecoveredUI/JackpotPopup";
    const string Temporary = "Assets/Whitebox/Editor/JackpotSourceImport.prefab";
    public static void Save()
    {
        Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
        var map = new Dictionary<string, string>();
        MapScript<Text>(map, "04f84fc2003509a5e7e068ec1271cc40");
        MapScript<Image>(map, "3cf5a44414476512e00c3e7a2569a919");
        MapScript<Button>(map, "18d0a90695249463551c00f45766e642");
        MapScript<Slider>(map, "1522d51f2e596a7eb84e5007260f43ac");
        MapScript<TextMeshProUGUI>(map, "3f96b1d166d19b209697e35b35d65c76");
        MapScript<GraphicRaycaster>(map, "86fe8f3fc59dc06ea6b45a1bbee64682");
        Map(map, "36977c4faccb97c4ebe0b4fdeea88b25", "Assets/Resources/RecoveredUI/CoinRewardText/Green.asset");
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath("31628181d58311344bb283c127fc9aba"));
        var matText = File.ReadAllText(Source + "TextMesh Pro/Resources/Fonts & Materials/#0A5902_4.mat");
        var shaderPath = AssetDatabase.GetAssetPath(font.material.shader);
        matText = matText.Replace("a495248e59fa6714793dae46034b0996", AssetDatabase.AssetPathToGUID(shaderPath));
        File.WriteAllText(Folder + "/#0A5902_4.mat", matText);
        File.Copy(Source + "Res/Anim/btnanim.anim", Folder + "/btnanim.anim", true);
        AssetDatabase.Refresh();
        Map(map, "39211f061913f054f84255431c8dce45", Folder + "/#0A5902_4.mat");
        Map(map, "b02dda94bf0cfd148b568608d58490f9", Folder + "/btnanim.anim");
        var spriteAsset = ScriptableObject.CreateInstance<TMP_SpriteAsset>(); spriteAsset.name = "tc_btn_bofang";
        spriteAsset.hashCode = -2029773876;
        var spriteSettings = new SerializedObject(spriteAsset);
        spriteSettings.FindProperty("m_Version").stringValue = "1.1.0";
        spriteSettings.ApplyModifiedPropertiesWithoutUndo();
        spriteAsset.spriteSheet = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/RecoveredArt/Res/UI/font_img/tc_btn_bofang.png");
        var spriteMaterial = new Material(Shader.Find("TextMeshPro/Sprite")) { name = "tc_btn_bofang", mainTexture = spriteAsset.spriteSheet };
        spriteMaterial = SaveAsset(spriteMaterial, Folder + "/tc_btn_bofang.mat"); spriteAsset.material = spriteMaterial;
        var glyph = new TMP_SpriteGlyph(0, new GlyphMetrics(77,77,-5.4f,59.54f,77), new GlyphRect(0,0,77,77),1.7f,0);
        glyph.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/RecoveredArt/Res/UI/font_img/tc_btn_bofang.png");
        spriteAsset.spriteGlyphTable.Add(glyph);
        spriteAsset.spriteCharacterTable.Add(new TMP_SpriteCharacter(65534, spriteAsset, glyph) { name = "tc_btn_bofang", scale = 1 });
        spriteAsset.UpdateLookupTables(); SaveAsset(spriteAsset, Folder + "/tc_btn_bofang.asset");
        Map(map, "183b0e4b7b3c5a34fa991aebe104ca31", Folder + "/tc_btn_bofang.asset");
        var guids = new[]{"f4b25a1c65682f9409a101f75b641504","211d03330aec8f24d910ac008c0e6b3a","34837b9c28c8b2a4f86a768fd659213d","cfe6ee8dfa40fbb47b0b515bf707d2c6","d3b5f5689331d58468651692a05e4947","dbd5a01e349cc9d4c99f05a855f81631"};
        var names = new[]{"tc_btn_01","tc_tx_01","ty_hb_meijing","tc_bak_xinxi","tc_bak_bar02","tc_bak_bar01"};
        for(int i=0;i<guids.Length;i++)
        {
            string path="Assets/Resources/RecoveredArt/Res/UI/pop-up/"+names[i]+".png";
            if(AssetDatabase.LoadAssetAtPath<Sprite>(path)==null) throw new InvalidDataException("Missing single sprite "+path);
            Map(map,guids[i],path);
        }
        // Preserve the source's entire native UI serialization, including all TMP
        // fields, child order, slider references and nonuniform RectTransforms.
        // Remove only the four unavailable original runtime component types.
        string text=File.ReadAllText(Source+"Res/ViewPrefabs/UIJackpotView.prefab").Replace("\r", "");
        var removed=new List<string>();
        text=Regex.Replace(text,@"--- !u!\d+ &(\d+)\n(.*?)(?=\n--- !u!|\z)",m=>{
            if(Regex.IsMatch(m.Value,@"guid: (5e3487c565f1d21538077d3c35a0f678|84e667d034d69ee8fdb17a866ff648f4|da904f91a28860280fb976ac34b27df4|ff326197253ae4cec0b46632391386ec)"))
            {removed.Add(m.Groups[1].Value);return "";}return m.Value;
        },RegexOptions.Singleline);
        foreach(string id in removed) text=Regex.Replace(text,@"  - component: \{fileID: "+id+@"\}\n", "");
        foreach(var pair in map)text=text.Replace(pair.Key,pair.Value);
        // Source sprites were standalone native .asset files (type 2); recovered
        // single-sprite PNG importers expose the same local id as imported assets (3).
        foreach(string guid in guids) text=text.Replace("guid: "+map[guid]+", type: 2", "guid: "+map[guid]+", type: 3");
        foreach(Match match in Regex.Matches(text,@"guid: (\w+)"))
            if(string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(match.Groups[1].Value)))throw new InvalidDataException("Unmapped source GUID "+match.Groups[1].Value);
        File.WriteAllText(Temporary,text);AssetDatabase.ImportAsset(Temporary,ImportAssetOptions.ForceSynchronousImport);
        var root=PrefabUtility.LoadPrefabContents(Temporary);
        try
        {
            var content=(RectTransform)root.transform.Find("Content");
            SetSprite(content.Find("Btn/ClaimBtn"),"tc_btn_01");
            SetSprite(content.Find("CashOutTip/Bg"),"tc_bak_xinxi");
            SetSprite(content.Find("CashOutTip").GetChild(2),"ty_hb_meijing");
            SetSprite(content.Find("CashOutTip/Slider/Background"),"tc_bak_bar01");
            SetSprite(content.Find("CashOutTip/Slider/Fill Area/Fill"),"tc_bak_bar02");
            SetSprite(content.Find("CashOutTip").GetChild(5),"tc_tx_01");
            var dragon=ReplaceArt(content.Find("SkeletonGraphic (ef_jackpottc)"),"ef_jackpottc");
            ReplaceArt(content.Find("SkeletonGraphic (ef_slpenqian)"),"ef_slpenqian");
            var tipRoot=content.Find("CashOutTip");ReplaceArt(tipRoot.Find("SkeletonGraphic (ef_shoucanggl)"),"ef_shoucanggl");
            var tip=tipRoot.gameObject.AddComponent<RecoveredCashOutTip>();var ts=new SerializedObject(tip);
            ts.FindProperty("tips").objectReferenceValue=tipRoot.Find("Text (TMP)").GetComponent<TMP_Text>();
            ts.FindProperty("progressSlider").objectReferenceValue=tipRoot.Find("Slider").GetComponent<Slider>();
            ts.FindProperty("progressText").objectReferenceValue=tipRoot.Find("Slider/ProgressTxt").GetComponent<TMP_Text>();
            ts.FindProperty("readyFormat").stringValue="You Can Cash Out <material=\"#003815_3\"><gradient=\"cash\">{0}</gradient></material> Now!";
            ts.FindProperty("remainingFormat").stringValue="Earn <material=\"#003815_3\"><gradient=\"cash\">{0}</gradient></material>  more to withdrawl <material=\"#003815_3\"><gradient=\"cash\">{1}</gradient></material>.";
            ts.ApplyModifiedPropertiesWithoutUndo();
            var popup=root.AddComponent<RecoveredJackpotPopup>();var settings=new SerializedObject(popup);
            settings.FindProperty("content").objectReferenceValue=content;
            settings.FindProperty("dragon").objectReferenceValue=dragon;
            settings.FindProperty("rewardText").objectReferenceValue=content.Find("Text (Legacy)").GetComponent<Text>();
            settings.FindProperty("advertisedText").objectReferenceValue=content.Find("Btn/ClaimBtn/Text (TMP)").GetComponent<TMP_Text>();
            settings.FindProperty("plainText").objectReferenceValue=content.Find("Btn/UnPlayBtn/Text (TMP)").GetComponent<TMP_Text>();
            settings.FindProperty("claimButton").objectReferenceValue=content.Find("Btn/ClaimBtn").GetComponent<Button>();
            settings.FindProperty("plainButton").objectReferenceValue=content.Find("Btn/UnPlayBtn").GetComponent<Button>();
            settings.FindProperty("cashOutTip").objectReferenceValue=tip;
            settings.FindProperty("fromScale").floatValue=0;settings.FindProperty("toScale").floatValue=1;
            settings.FindProperty("windowDuration").floatValue=.3f;settings.FindProperty("countDuration").floatValue=.5f;
            settings.FindProperty("enterEase").animationCurveValue=Curve(4.70158f,0);
            settings.FindProperty("exitEase").animationCurveValue=Curve(0,4.70158f);
            settings.FindProperty("countEase").animationCurveValue=Curve(2,0);
            Reveal(settings.FindProperty("plainReveal"),content.Find("Btn/UnPlayBtn"),1);
            Reveal(settings.FindProperty("tipReveal"),tipRoot,.5f);
            settings.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root,Folder+".prefab");AssetDatabase.SaveAssets();
        }
        finally {PrefabUtility.UnloadPrefabContents(root);AssetDatabase.DeleteAsset(Temporary);}
    }
    static void Reveal(SerializedProperty p,Transform target,float delay)
    {p.FindPropertyRelative("target").objectReferenceValue=target;p.FindPropertyRelative("delay").floatValue=delay;p.FindPropertyRelative("duration").floatValue=.3f;p.FindPropertyRelative("ease").animationCurveValue=Curve(2,0);}
    static AnimationCurve Curve(float start,float end)=>new AnimationCurve(new Keyframe(0,0,start,start),new Keyframe(1,1,end,end));
    static void SetSprite(Transform node,string name)
    {
        var sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/RecoveredArt/Res/UI/pop-up/"+name+".png");
        node.GetComponent<Image>().sprite=sprite;
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite,out string guid,out long id);
        Debug.Log("Jackpot sprite "+name+" "+guid+"/"+id);
    }
    static RecoveredRegionAnimator ReplaceArt(Transform prior,string name)
    {
        var node=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/RecoveredUI/JackpotPopupArt/"+name+".prefab"),prior.parent);
        var a=(RectTransform)node.transform;var b=(RectTransform)prior;
        a.anchorMin=b.anchorMin;a.anchorMax=b.anchorMax;a.pivot=b.pivot;a.sizeDelta=b.sizeDelta;a.anchoredPosition3D=b.anchoredPosition3D;a.localRotation=b.localRotation;a.localScale=b.localScale;
        a.SetSiblingIndex(b.GetSiblingIndex());node.name=prior.name;Object.DestroyImmediate(prior.gameObject);return node.GetComponent<RecoveredRegionAnimator>();
    }
    static void MapScript<T>(Dictionary<string,string> map,string original) where T:MonoBehaviour
    {
        var host=new GameObject("Script identity",typeof(RectTransform));
        try{var component=host.AddComponent<T>();Map(map,original,AssetDatabase.GetAssetPath(MonoScript.FromMonoBehaviour(component)));}
        finally{Object.DestroyImmediate(host);}
    }
    static void Map(Dictionary<string,string> map,string original,string path)
    {string guid=AssetDatabase.AssetPathToGUID(path);if(string.IsNullOrEmpty(guid))throw new InvalidDataException(path);map.Add(original,guid);}
    static T SaveAsset<T>(T value,string path) where T:Object
    {var old=AssetDatabase.LoadAssetAtPath<T>(path);if(old==null){AssetDatabase.CreateAsset(value,path);return value;}EditorUtility.CopySerialized(value,old);Object.DestroyImmediate(value);return old;}
}
