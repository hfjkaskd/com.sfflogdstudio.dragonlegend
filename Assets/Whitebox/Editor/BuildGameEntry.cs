using System.IO;
using DragonLegend.Whitebox;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Offline authoring only. Saves all static structure to Prefab/Scene before runtime.
public static class BuildGameEntry
{
    public static void Build()
    {
        Directory.CreateDirectory("Assets/Whitebox/Settings");
        Directory.CreateDirectory("Assets/Whitebox/Scenes");
        Directory.CreateDirectory("Assets/Resources/Whitebox");
        var primary = Profile("US_Default", "US", "cp_default_1", true,
            "Local US cash/ad test selection. Cached cp_default_1 is verified; country routing is not yet verified.");
        var alternative = Profile("A_Test", "US", "cp_default", false,
            "Local comparison profile. This is not evidence that cp_default is the original server's A bucket.");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var root = new GameObject("GameEntry", typeof(RectTransform));
        root.SetActive(false);
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(720,1280);
        scaler.matchWidthOrHeight = 0.5f;
        root.AddComponent<GraphicRaycaster>();
        Label(root.transform, "Title", "Dragon Legend\nReconstruction entry", new Vector2(0,420), new Vector2(640,150), 36);
        var status = Label(root.transform, "Status", "Loading...", new Vector2(0,120), new Vector2(640,400), 24);
        var us = Button(root.transform,"SelectUS", "US cash + ads (mock)", new Vector2(0,-220));
        var alt = Button(root.transform,"SelectAlternative", "GM: comparison profile", new Vector2(0,-330));
        Label(root.transform, "Scope", "Configuration entry only. Main gameplay and original visuals are still being restored.", new Vector2(0,-490), new Vector2(640,160), 22);
        root.AddComponent<GameEntry>().Configure(primary,alternative,us,alt,status);
        root.SetActive(true);
        var prefab = PrefabUtility.SaveAsPrefabAsset(root,"Assets/Resources/Whitebox/GameEntry.prefab");
        Object.DestroyImmediate(root);
        PrefabUtility.InstantiatePrefab(prefab);
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        EditorSceneManager.SaveScene(scene,"Assets/Whitebox/Scenes/GameEntry.unity");
        EditorBuildSettings.scenes = new[] {
            new EditorBuildSettingsScene("Assets/Whitebox/Scenes/GameEntry.unity",true),
            new EditorBuildSettingsScene("Assets/Whitebox/Scenes/MockFlow.unity",true)
        };
        PlayerSettings.productName = "Dragon Legend Reconstruction";
        PlayerSettings.companyName = "Reconstruction";
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        AssetDatabase.SaveAssets();
        Debug.Log("GAME_ENTRY_CREATED");
    }
    private static LaunchProfile Profile(string name, string country, string snapshot, bool cash, string note)
    {
        string path = "Assets/Whitebox/Settings/" + name + ".asset";
        var profile = AssetDatabase.LoadAssetAtPath<LaunchProfile>(path);
        if (!profile) { profile = ScriptableObject.CreateInstance<LaunchProfile>(); AssetDatabase.CreateAsset(profile,path); }
        profile.profileId = name;
        profile.countryCode = country;
        profile.snapshotPath = "RecoveredConfig/Remote/" + snapshot + ".json";
        profile.cashPresentationEnabled = cash;
        profile.advertisementPresentationEnabled = cash;
        profile.evidenceNote = note;
        EditorUtility.SetDirty(profile);
        return profile;
    }
    private static Text Label(Transform parent, string name, string text, Vector2 position, Vector2 size, int fontSize)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(parent,false);
        var rect = (RectTransform)go.transform;
        rect.sizeDelta = size; rect.anchoredPosition = position;
        var label = go.GetComponent<Text>();
        label.text = text;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = fontSize; label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white; label.raycastTarget = false;
        return label;
    }
    private static Button Button(Transform parent, string name, string caption, Vector2 position)
    {
        var go = new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(Button));
        go.transform.SetParent(parent,false);
        var rect = (RectTransform)go.transform;
        rect.sizeDelta = new Vector2(600,86); rect.anchoredPosition = position;
        var image = go.GetComponent<Image>(); image.color = new Color(0.22f,0.15f,0.38f,1);
        var button = go.GetComponent<Button>(); button.targetGraphic = image;
        Label(go.transform,"Caption",caption,Vector2.zero,rect.sizeDelta,26);
        return button;
    }
}
