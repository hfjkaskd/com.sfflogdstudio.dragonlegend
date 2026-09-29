using System;
using System.IO;
using DragonLegend.Whitebox;
using UnityEditor;
using UnityEditor.Build.Player;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class BuildStartupLoading
{
    private const string Phase = "Artifacts/Loading/phase.txt";
    private const string PrefabPath = "Assets/Resources/Loading/StartupLoading.prefab";
    static BuildStartupLoading()
    {
        if (File.Exists(Phase)) EditorApplication.delayCall += RunPending;
    }
    private static void RunPending()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
        { EditorApplication.delayCall += RunPending; return; }
        if (File.ReadAllText(Phase) != "build") return;
        File.WriteAllText(Phase, "building");
        try
        {
            Save();
            var output = "Artifacts/Loading/AndroidScripts";
            Directory.CreateDirectory(output);
            var compiled = PlayerBuildInterface.CompilePlayerScripts(new ScriptCompilationSettings {
                group = BuildTargetGroup.Android, target = BuildTarget.Android,
                options = ScriptCompilationOptions.None }, output);
            if (compiled.assemblies == null || compiled.assemblies.Count == 0)
                throw new InvalidOperationException("Android compilation produced no assemblies.");
            File.WriteAllText("Artifacts/Loading/validation.txt", "PASS: 1080x1920 artwork, saved loading prefab, GameEntry binding, Editor and Android compilation.");
            File.WriteAllText(Phase, "passed");
        }
        catch (Exception error)
        {
            File.WriteAllText("Artifacts/Loading/error.txt", error.ToString());
            File.WriteAllText(Phase, "failed");
            Debug.LogException(error);
        }
    }

    [MenuItem("Dragon Legend/Build/Startup Loading")]
    public static void Save()
    {
        const string imagePath = "Assets/Resources/Loading/GildedDragonLoading.png";
        var importer = (TextureImporter)AssetImporter.GetAtPath(imagePath);
        importer.textureType = TextureImporterType.Default;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.mipmapEnabled = false;
        importer.isReadable = false;
        importer.maxTextureSize = 2048;
        importer.textureCompression = TextureImporterCompression.Compressed;
        importer.SaveAndReimport();
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(imagePath);
        if (texture.width != 1080 || texture.height != 1920)
            throw new InvalidOperationException("Loading artwork must be 1080x1920.");
        var root = new GameObject("StartupLoading", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(StartupLoadingView));
        root.layer = 5;
        try
        {
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 32000;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920); scaler.matchWidthOrHeight = 0.5f;
            var backdrop = Node("Backdrop", root.transform, Vector2.zero, Vector2.one);
            backdrop.gameObject.AddComponent<Image>().color = new Color(.16f,.035f,.02f,1);
            var artRect = Node("Artwork", root.transform, new Vector2(.5f,.5f), new Vector2(.5f,.5f));
            artRect.sizeDelta = new Vector2(1080,1920);
            var art = artRect.gameObject.AddComponent<RawImage>(); art.raycastTarget = false; art.enabled = false;
            var fit = artRect.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent; fit.aspectRatio = 1080f/1920f;
            var footer = Node("LoadingStatus", root.transform, Vector2.zero, new Vector2(1,0));
            footer.pivot = new Vector2(.5f,0); footer.sizeDelta = new Vector2(0,240);
            var labelRect = Node("StageLabel",footer,new Vector2(.08f,.45f),new Vector2(.92f,.87f));
            var label = labelRect.gameObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 32; label.fontStyle = FontStyle.Bold; label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(1,.89f,.64f); label.text = "Loading..."; label.raycastTarget = false;
            var track = Node("StageTrack",footer,new Vector2(.12f,.24f),new Vector2(.88f,.36f));
            track.gameObject.AddComponent<Image>().color = new Color(.18f,.055f,.015f);
            var fill = Node("StageFill",track,Vector2.zero,new Vector2(0,1)).gameObject.AddComponent<Image>();
            fill.color = new Color(1,.68f,.16f); fill.type = Image.Type.Simple;
            fill.raycastTarget = false;
            var serialized = new SerializedObject(root.GetComponent<StartupLoadingView>());
            serialized.FindProperty("artwork").objectReferenceValue = art;
            serialized.FindProperty("stageLabel").objectReferenceValue = label;
            serialized.FindProperty("stageFill").objectReferenceValue = fill;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
        const string entryPath = "Assets/Resources/Whitebox/GameEntry.prefab";
        var entry = PrefabUtility.LoadPrefabContents(entryPath);
        try
        {
            var serialized = new SerializedObject(entry.GetComponent<GameEntry>());
            serialized.FindProperty("loadingScreenPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<StartupLoadingView>(PrefabPath);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(entry,entryPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(entry); }
        AssetDatabase.SaveAssets();
    }

    private static RectTransform Node(string name, Transform parent, Vector2 min, Vector2 max)
    {
        var node = new GameObject(name,typeof(RectTransform)); node.layer=5;
        var rect = (RectTransform)node.transform; rect.SetParent(parent,false);
        rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=rect.offsetMax=Vector2.zero;
        return rect;
    }
}
