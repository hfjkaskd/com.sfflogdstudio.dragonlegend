using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using DragonLegend.Whitebox;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class BuildWheelWindow
{
    private const string Temporary = "Assets/Whitebox/Editor/WheelSourceImport.prefab";
    public static void Save()
    {
        BuildJackpotPopupArt.Create("ef_slzhuanpandi", "zhuanpandi", new Vector2(100, 100), new Vector2(.5f, .5f), "Tools/Evidence/");
        BuildJackpotPopupArt.Create("ef_slzhuanpanzz", "zhuanpanzz", new Vector2(100, 100), new Vector2(.5f, .5f), "Tools/Evidence/");
        var map = new Dictionary<string, string>();
        Script<Text>(map, "04f84fc2003509a5e7e068ec1271cc40");
        Script<Image>(map, "3cf5a44414476512e00c3e7a2569a919");
        Script<TextMeshProUGUI>(map, "3f96b1d166d19b209697e35b35d65c76");
        Script<GraphicRaycaster>(map, "86fe8f3fc59dc06ea6b45a1bbee64682");
        map["36977c4faccb97c4ebe0b4fdeea88b25"] = AssetDatabase.AssetPathToGUID("Assets/Resources/RecoveredUI/CoinRewardText/Green.asset");
        map["5a9b5b2b3b06f5f4aa8255e3d62b034c"] = AssetDatabase.AssetPathToGUID("Assets/Resources/RecoveredArt/Res/UI/wanfa_02/fortune_wheel_txt.png");
        map["d609a1a0a0803fc469b8b03dc5646585"] = AssetDatabase.AssetPathToGUID("Assets/Resources/RecoveredArt/Res/UI/wanfa_02/fw_bg01.png");
        string source = File.ReadAllText("ReferenceOriginal/Res/ViewPrefabs/UIWheelView.prefab").Replace("\r", "");
        var removed = new List<string>();
        source = Regex.Replace(source, @"--- !u!\d+ &(\d+)\n.*?(?=\n--- !u!|\z)", m => {
            if (Regex.IsMatch(m.Value, @"guid: (874f75c0e89492a932b7eaadfc44c414|132b6501dc1746e5b20bc04c0ad3cc98|5b873ac4db023d7630dab38ad536f44a|da904f91a28860280fb976ac34b27df4|ff326197253ae4cec0b46632391386ec)"))
            { removed.Add(m.Groups[1].Value); return ""; }
            return m.Value;
        }, RegexOptions.Singleline);
        foreach (string id in removed) source = Regex.Replace(source, @"  - component: \{fileID: " + id + @"\}\n", "");
        foreach (var pair in map) { if (string.IsNullOrEmpty(pair.Value)) throw new InvalidDataException(pair.Key); source = source.Replace(pair.Key, pair.Value); }
        foreach (string id in new[] { "5a9b5b2b3b06f5f4aa8255e3d62b034c", "d609a1a0a0803fc469b8b03dc5646585" }) source = source.Replace("guid: " + map[id] + ", type: 2", "guid: " + map[id] + ", type: 3");
        foreach (Match m in Regex.Matches(source, @"guid: (\w+)")) if (string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(m.Groups[1].Value))) throw new InvalidDataException("Unmapped Wheel GUID " + m.Groups[1].Value);
        File.WriteAllText(Temporary, source); AssetDatabase.ImportAsset(Temporary, ImportAssetOptions.ForceSynchronousImport);
        var bundle = new GameObject("WheelWindow", typeof(RectTransform), typeof(RecoveredWheelWindow)); bundle.layer = 5;
        try
        {
            var bundleRect = (RectTransform)bundle.transform; bundleRect.anchorMin = Vector2.zero; bundleRect.anchorMax = Vector2.one; bundleRect.sizeDelta = Vector2.zero;
            var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Temporary), bundle.transform);
            PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            var content = root.transform.Find("Content");
            var rotor = Replace(content.Find("Roll"), AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/RecoveredUI/WheelRotor.prefab")).GetComponent<RecoveredWheelRotor>();
            var frame = Replace(content.Find("Kuang"), Art("ef_slzhuanpandi")).GetComponent<RecoveredRegionAnimator>();
            var pointer = Replace(content.Find("Zhen"), Art("ef_slzhuanpanzz")).GetComponent<RecoveredRegionAnimator>();
            var originalMeters = AssetDatabase.LoadAssetAtPath<RecoveredJackpotMeters>("Assets/Resources/RecoveredUI/JackpotMeters.prefab");
            var meters = new RecoveredJackpotMeter[3];
            string[] names = { "Grand", "Major", "Minior" };
            for (int i = 0; i < 3; i++)
            {
                var meterRoot = content.Find(names[i]);
                var icon = Replace(meterRoot.Find("Spine"), originalMeters.At(i).Icon.gameObject).GetComponent<RecoveredJackpotIcon>();
                meters[i] = meterRoot.gameObject.AddComponent<RecoveredJackpotMeter>();
                var settings = new SerializedObject(meters[i]);
                settings.FindProperty("icon").objectReferenceValue = icon;
                settings.FindProperty("rewardText").objectReferenceValue = meterRoot.GetComponentInChildren<TMP_Text>(true);
                settings.FindProperty("greenRewardText").objectReferenceValue = meterRoot.GetComponentInChildren<Text>(true);
                settings.FindProperty("rewardDuration").floatValue = .3f; settings.ApplyModifiedPropertiesWithoutUndo();
            }
            // The original UIShiny values match this existing native effect.
            var shine = content.Find("Title").gameObject.AddComponent<RecoveredTitleShine>();
            var shineSettings = new SerializedObject(shine);
            shineSettings.FindProperty("template").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/RecoveredUI/BonusRewardPopup/TitleShine.mat");
            shineSettings.FindProperty("effectFactor").floatValue = .5f;
            shineSettings.FindProperty("width").floatValue = .25f; shineSettings.FindProperty("rotation").floatValue = 135;
            shineSettings.FindProperty("softness").floatValue = 1; shineSettings.FindProperty("brightness").floatValue = 1; shineSettings.FindProperty("gloss").floatValue = 1;
            shineSettings.FindProperty("play").boolValue = true; shineSettings.FindProperty("loop").boolValue = true; shineSettings.FindProperty("duration").floatValue = 2;
            shineSettings.FindProperty("initialPlayDelay").floatValue = 0; shineSettings.FindProperty("loopDelay").floatValue = 0;
            shineSettings.ApplyModifiedPropertiesWithoutUndo();
            var cash = Popup("BonusRewardPopup", bundle.transform);
            var jackpot = Popup("JackpotPopup", bundle.transform);
            root.GetComponent<Canvas>().overrideSorting = true; root.GetComponent<Canvas>().sortingOrder = 300;
            var mask = new GameObject("_WindowBg", typeof(RectTransform), typeof(Image), typeof(Button)); mask.layer = 5;
            mask.transform.SetParent(root.transform, false); mask.transform.SetAsFirstSibling();
            var maskRect = (RectTransform)mask.transform; maskRect.anchorMin = Vector2.zero; maskRect.anchorMax = Vector2.one; maskRect.sizeDelta = Vector2.zero;
            var image = mask.GetComponent<Image>(); image.color = new Color(0, 0, 0, .65f);
            var button = mask.GetComponent<Button>(); button.targetGraphic = image; button.transition = Selectable.Transition.None;
            var s = new SerializedObject(bundle.GetComponent<RecoveredWheelWindow>());
            s.FindProperty("window").objectReferenceValue = root; s.FindProperty("content").objectReferenceValue = content;
            s.FindProperty("rotor").objectReferenceValue = rotor; s.FindProperty("frame").objectReferenceValue = frame; s.FindProperty("pointer").objectReferenceValue = pointer;
            var array = s.FindProperty("meters"); array.arraySize = 3;
            for (int i = 0; i < 3; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = meters[i];
            s.FindProperty("cashPopup").objectReferenceValue = cash.GetComponent<RecoveredBonusRewardPopup>();
            s.FindProperty("jackpotPopup").objectReferenceValue = jackpot.GetComponent<RecoveredJackpotPopup>();
            s.FindProperty("windowDuration").floatValue = .3f; s.FindProperty("openingDelay").floatValue = .5f;
            s.FindProperty("winDelay").floatValue = 1; s.FindProperty("pulseDuration").floatValue = .3f; s.FindProperty("pulseScale").floatValue = 1.2f;
            s.FindProperty("enterEase").animationCurveValue = new AnimationCurve(new Keyframe(0, 0, 4.70158f, 4.70158f), new Keyframe(1, 1, 0, 0));
            s.FindProperty("exitEase").animationCurveValue = new AnimationCurve(new Keyframe(0, 0, 0, 0), new Keyframe(1, 1, 4.70158f, 4.70158f));
            s.FindProperty("pulseEase").animationCurveValue = new AnimationCurve(new Keyframe(0, 0, 2, 2), new Keyframe(1, 1, 0, 0)); s.ApplyModifiedPropertiesWithoutUndo();
            root.SetActive(false); PrefabUtility.SaveAsPrefabAsset(bundle, "Assets/Resources/RecoveredUI/WheelWindow.prefab"); AssetDatabase.SaveAssets();
        }
        finally { Object.DestroyImmediate(bundle); AssetDatabase.DeleteAsset(Temporary); }
    }
    private static GameObject Art(string name) => AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/RecoveredUI/JackpotPopupArt/" + name + ".prefab");
    private static GameObject Replace(Transform old, GameObject prefab)
    {
        var node = Object.Instantiate(prefab, old.parent, false); node.name = old.name;
        var source = (RectTransform)old; var rect = (RectTransform)node.transform;
        rect.anchorMin = source.anchorMin; rect.anchorMax = source.anchorMax; rect.sizeDelta = source.sizeDelta; rect.pivot = source.pivot;
        rect.anchoredPosition3D = source.anchoredPosition3D; rect.localRotation = source.localRotation; rect.localScale = source.localScale;
        rect.SetSiblingIndex(source.GetSiblingIndex()); Object.DestroyImmediate(old.gameObject); return node;
    }
    private static GameObject Popup(string name, Transform parent)
    {
        var popup = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/RecoveredUI/" + name + ".prefab"), parent);
        popup.GetComponent<Canvas>().overrideSorting = true; popup.GetComponent<Canvas>().sortingOrder = 301; popup.SetActive(false); return popup;
    }
    private static void Script<T>(Dictionary<string, string> map, string original) where T : MonoBehaviour
    {
        var host = new GameObject("Script identity", typeof(RectTransform));
        try { map[original] = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(MonoScript.FromMonoBehaviour(host.AddComponent<T>()))); }
        finally { Object.DestroyImmediate(host); }
    }
}
