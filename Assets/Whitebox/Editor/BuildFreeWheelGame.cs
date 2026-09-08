using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using DragonLegend.Whitebox;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class BuildFreeWheelGame
{
    private const string Temporary = "Assets/Whitebox/Editor/FreeWheelIconImport.prefab";
    public static void Save()
    {
        string source = File.ReadAllText("ReferenceOriginal/Res/ViewPrefabs/UIMainView.prefab").Replace("\r", "");
        var result = new StringBuilder("%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n");
        foreach (string id in new[] { "1708180620337799", "224626119028408548", "222989086511197016", "114104119009767140" })
        {
            string block = Regex.Match(source, @"^--- !u!\d+ &" + id + @"\n.*?(?=^--- !u!|\z)", RegexOptions.Multiline | RegexOptions.Singleline).Value;
            if (string.IsNullOrEmpty(block)) throw new InvalidDataException("Missing WheelPrefab source " + id);
            if (id == "224626119028408548") block = Regex.Replace(block, @"m_Father: \{fileID: \d+\}", "m_Father: {fileID: 0}");
            result.Append(block);
        }
        var lookup = new GameObject("Image script", typeof(RectTransform), typeof(Image));
        string script;
        try { script = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(MonoScript.FromMonoBehaviour(lookup.GetComponent<Image>()))); }
        finally { Object.DestroyImmediate(lookup); }
        string sprite = AssetDatabase.AssetPathToGUID("Assets/Resources/RecoveredArt/Res/UI/free_game/mfyx_icon_zhuanpan.png");
        if (string.IsNullOrEmpty(sprite)) throw new InvalidDataException("Missing Wheel entry sprite");
        string yaml = result.ToString().Replace("3cf5a44414476512e00c3e7a2569a919", script)
            .Replace("97a397da935533d4cb54640cdb10fb17, type: 2", sprite + ", type: 3");
        File.WriteAllText(Temporary, yaml); AssetDatabase.ImportAsset(Temporary, ImportAssetOptions.ForceSynchronousImport);
        var root = new GameObject("FreeWheelGame", typeof(RectTransform), typeof(RecoveredFreeWheelGame)); root.layer = 5;
        try
        {
            var rect = (RectTransform)root.transform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.sizeDelta = Vector2.zero;
            var icon = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Temporary), root.transform);
            PrefabUtility.UnpackPrefabInstance(icon, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            var window = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/RecoveredUI/WheelWindow.prefab"), root.transform);
            var settings = new SerializedObject(root.GetComponent<RecoveredFreeWheelGame>());
            settings.FindProperty("icon").objectReferenceValue = icon.GetComponent<RectTransform>();
            settings.FindProperty("window").objectReferenceValue = window.GetComponent<RecoveredWheelWindow>();
            settings.FindProperty("scaleMultiplier").floatValue = 1.5f; settings.FindProperty("scaleDuration").floatValue = .3f;
            settings.FindProperty("windowDelay").floatValue = .6f; settings.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, "Assets/Resources/RecoveredUI/FreeWheelGame.prefab"); AssetDatabase.SaveAssets();
        }
        finally { Object.DestroyImmediate(root); AssetDatabase.DeleteAsset(Temporary); }
    }
}
