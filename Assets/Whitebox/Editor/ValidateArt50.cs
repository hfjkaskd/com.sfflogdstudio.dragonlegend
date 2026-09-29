using System;
using System.Collections.Generic;
using System.IO;
using DragonLegend.Whitebox;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// One-shot authoring validation only. No game state, inputs or runtime paths are overridden.
[InitializeOnLoad]
public static class ValidateArt50
{
    private const string Prefix = "Art50.Capture.";
    private const string Folder = "Artifacts/Art50";
    private const string Request = Folder + "/capture.request";
    private const string Screenshot = Folder + "/main-screen.png";
    private const string ScenePath = "Assets/Whitebox/Scenes/GameEntry.unity";
    private static double nextCheck;
    private static GameEntry entry;

    [Serializable] private sealed class ButtonLayout
    {
        public string asset, path;
        public Vector2 anchoredPosition, sizeDelta, anchorMin, anchorMax, pivot;
        public Vector3 localPosition, localScale;
    }

    [Serializable] private sealed class Report
    {
        public string startedUtc, completedUtc, screenshot;
        public int importedAssets, prefabs, inspectedButtons, runtimeButtons;
        public bool runtimeReady, captured, enteredPlayMode, completed;
        public List<string> errors = new List<string>();
        public List<ButtonLayout> buttonLayouts = new List<ButtonLayout>();
    }

    [Serializable] private sealed class SavedScene
    {
        public string path;
        public bool loaded, active;
    }

    [Serializable] private sealed class SavedSetup
    {
        public List<SavedScene> scenes = new List<SavedScene>();
    }

    static ValidateArt50()
    {
        EditorApplication.update += Tick;
    }

    private static int Phase
    {
        get { return SessionState.GetInt(Prefix + "phase", 0); }
        set { SessionState.SetInt(Prefix + "phase", value); }
    }

    private static Report ReadReport()
    {
        return JsonUtility.FromJson<Report>(SessionState.GetString(Prefix + "report", "{}"));
    }

    private static void SaveReport(Report report)
    {
        SessionState.SetString(Prefix + "report", JsonUtility.ToJson(report));
        Directory.CreateDirectory(Folder);
        File.WriteAllText(Folder + "/unity-validation.json", JsonUtility.ToJson(report, true));
    }

    private static void SetTimer()
    {
        SessionState.SetFloat(Prefix + "started", (float)EditorApplication.timeSinceStartup);
    }

    private static double Elapsed
    {
        get { return EditorApplication.timeSinceStartup - SessionState.GetFloat(Prefix + "started", 0); }
    }

    private static void Tick()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
            EditorApplication.timeSinceStartup < nextCheck) return;
        nextCheck = EditorApplication.timeSinceStartup + 0.2;
        try
        {
            if (Phase == 0)
            {
                if (!File.Exists(Request) || EditorApplication.isPlayingOrWillChangePlaymode != EditorApplication.isPlaying) return;
                Begin();
                return;
            }
            if (Phase == 4)
            {
                if (!EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode) Finish();
                return;
            }
            if (Phase == 1)
            {
                if (!EditorApplication.isPlaying)
                {
                    if (Elapsed > 60) Fail("Timed out while entering Play Mode.");
                    return;
                }
                Phase = 2;
                SetTimer();
            }
            if (!EditorApplication.isPlaying)
            {
                Fail("Play Mode ended before the screenshot completed.");
                return;
            }
            if (Phase == 2)
            {
                // Read at 5 Hz only while this one-shot request is active.
                if (entry == null) entry = UnityEngine.Object.FindObjectOfType<GameEntry>();
                if (entry != null && entry.Playfield != null)
                {
                    if (entry.Playfield.Error != null) { Fail(entry.Playfield.Error.ToString()); return; }
                    if (!SessionState.GetBool(Prefix + "ready", false))
                    {
                        SessionState.SetBool(Prefix + "ready", true);
                        SessionState.SetFloat(Prefix + "readyAt", (float)EditorApplication.timeSinceStartup);
                    }
                    // Allow ordinary startup presentation a moment to settle.
                    if (EditorApplication.timeSinceStartup - SessionState.GetFloat(Prefix + "readyAt", 0) < 2) return;
                    var report = ReadReport();
                    report.runtimeReady = true;
                    report.runtimeButtons = entry.GetComponentsInChildren<Button>(true).Length;
                    report.screenshot = Screenshot;
                    SaveReport(report);
                    ScreenCapture.CaptureScreenshot(Path.GetFullPath(Screenshot));
                    SessionState.SetInt(Prefix + "captureFrame", Time.frameCount);
                    Phase = 3;
                    SetTimer();
                    return;
                }
                if (Elapsed > 60) Fail("GameEntry.Playfield was not ready within 60 seconds; no buttons or spins were invoked.");
                return;
            }
            if (Phase == 3)
            {
                if (File.Exists(Screenshot) && new FileInfo(Screenshot).Length > 0 &&
                    Time.frameCount >= SessionState.GetInt(Prefix + "captureFrame", 0) + 2)
                {
                    var report = ReadReport();
                    report.captured = true;
                    SaveReport(report);
                    StopOwnedPlayMode();
                }
                else if (Elapsed > 20) Fail("Screenshot was not written within 20 seconds.");
            }
        }
        catch (Exception error) { Fail(error.ToString()); }
    }

    private static void Begin()
    {
        var report = new Report { startedUtc = DateTime.UtcNow.ToString("o") };
        SaveReport(report);
        Phase = 1;
        SessionState.SetBool(Prefix + "ready", false);
        SessionState.SetString(Prefix + "scenes", "");
        entry = null;
        File.Delete(Request);
        if (File.Exists(Screenshot)) File.Delete(Screenshot);
        ValidateChangedAssets(report);
        SaveReport(report);

        if (!EditorApplication.isPlaying)
        {
            // Never discard or save user changes, including additive scenes.
            if (HasDirtyScene()) throw new InvalidOperationException("A loaded scene is dirty. Save or revert it, then create Artifacts/Art50/capture.request again.");
            if (SceneManager.GetActiveScene().path != ScenePath)
            {
                var saved = new SavedSetup();
                foreach (var scene in EditorSceneManager.GetSceneManagerSetup())
                {
                    if (string.IsNullOrEmpty(scene.path))
                        throw new InvalidOperationException("An untitled scene is open. Save it or open GameEntry, then request capture again.");
                    saved.scenes.Add(new SavedScene { path = scene.path, loaded = scene.isLoaded, active = scene.isActive });
                }
                SessionState.SetString(Prefix + "scenes", JsonUtility.ToJson(saved));
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }
            report.enteredPlayMode = true;
            SaveReport(report);
            SetTimer();
            EditorApplication.isPlaying = true;
        }
        else SetTimer();
    }

    private static void ValidateChangedAssets(Report report)
    {
        string list = Folder + "/changed-assets.txt";
        if (!File.Exists(list)) throw new FileNotFoundException("Missing changed asset manifest.", list);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (string line in File.ReadAllLines(list))
        {
            string path = line.Trim().Replace('\\', '/');
            if (path.Length == 0 || path.StartsWith("#") || path.EndsWith(".meta") || !seen.Add(path)) continue;
            if (!path.StartsWith("Assets/", StringComparison.Ordinal) || path.Contains("../"))
                throw new InvalidOperationException("Manifest entry must be a project-relative asset path: " + path);
            if (AssetDatabase.IsValidFolder(path)) continue;
            var asset = AssetDatabase.LoadMainAssetAtPath(path);
            if (asset == null) throw new InvalidOperationException("Asset failed to import: " + path);
            report.importedAssets++;
            if (asset is Shader shader && ShaderUtil.ShaderHasError(shader))
                throw new InvalidOperationException("Shader failed: " + path);
            if (!(asset is GameObject prefab)) continue;
            report.prefabs++;
            foreach (var component in prefab.GetComponentsInChildren<Component>(true))
                if (component == null) throw new InvalidOperationException("Missing script in " + path);
            foreach (var button in prefab.GetComponentsInChildren<Button>(true))
            {
                report.inspectedButtons++;
                if (button.onClick.GetPersistentEventCount() != 0)
                    throw new InvalidOperationException("Serialized Button event in " + path);
                var rect = button.transform as RectTransform;
                if (rect == null) throw new InvalidOperationException("Button has no RectTransform: " + path);
                report.buttonLayouts.Add(new ButtonLayout
                {
                    asset = path, path = AnimationUtility.CalculateTransformPath(rect, prefab.transform),
                    anchoredPosition = rect.anchoredPosition, sizeDelta = rect.sizeDelta,
                    anchorMin = rect.anchorMin, anchorMax = rect.anchorMax, pivot = rect.pivot,
                    localPosition = rect.localPosition, localScale = rect.localScale
                });
            }
        }
        if (report.importedAssets == 0) throw new InvalidOperationException("No assets were listed for validation.");
    }

    private static bool HasDirtyScene()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty) return true;
        return false;
    }

    private static void Fail(string error)
    {
        var report = ReadReport();
        if (report.errors == null) report.errors = new List<string>();
        report.errors.Add(error);
        SaveReport(report);
        StopOwnedPlayMode();
    }

    private static void StopOwnedPlayMode()
    {
        if (ReadReport().enteredPlayMode && EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Phase = 4;
            EditorApplication.isPlaying = false;
        }
        else Finish();
    }

    private static void Finish()
    {
        var report = ReadReport();
        string original = SessionState.GetString(Prefix + "scenes", "");
        // Stop accepting this run before restoration can raise a new editor error.
        Phase = 0;
        SessionState.SetString(Prefix + "scenes", "");
        if (!EditorApplication.isPlaying && original.Length > 0)
        {
            if (HasDirtyScene()) report.errors.Add("Previous scene setup was not restored because a loaded scene is now dirty.");
            else
            {
                var saved = JsonUtility.FromJson<SavedSetup>(original);
                var setup = new SceneSetup[saved.scenes.Count];
                for (int i = 0; i < setup.Length; i++)
                    setup[i] = new SceneSetup { path = saved.scenes[i].path, isLoaded = saved.scenes[i].loaded, isActive = saved.scenes[i].active };
                try { EditorSceneManager.RestoreSceneManagerSetup(setup); }
                catch (Exception error) { report.errors.Add("Scene restoration failed: " + error.Message); }
            }
        }
        report.completed = true;
        report.completedUtc = DateTime.UtcNow.ToString("o");
        SaveReport(report);
        entry = null;
        Debug.Log("Art50 validation completed: " + report.importedAssets + " imported assets, captured=" + report.captured + ", errors=" + report.errors.Count);
    }
}
