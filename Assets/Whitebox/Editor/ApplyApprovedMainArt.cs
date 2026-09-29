using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ApplyApprovedMainArt
{
    [Serializable] private sealed class Report
    {
        public string startedUtc, completedUtc;
        public bool completed;
        public List<string> steps = new List<string>();
        public string error;
    }

    public static void Apply()
    {
        const string folder = "Artifacts/ApprovedArt";
        Directory.CreateDirectory(folder);
        var report = new Report { startedUtc = DateTime.UtcNow.ToString("o") };
        try
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Authoring requires Edit Mode.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("A scene has unsaved changes; no authoring was started.");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ApplyApprovedHeader.Apply();
            report.steps.Add("Applied independent header visuals and preserved dynamic values.");
            ApprovedSideIconsAuthoring.Apply();
            report.steps.Add("Applied four side entry visuals with existing buttons and targets.");
            ApprovedFooterInstaller.Apply();
            ApprovedSpinMotionAuthoring.Apply();
            report.steps.Add("Applied board, Bagua, chest and lower control visuals.");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            // Reload the saved scene so any prefab instance refresh occurs before Play Mode.
            EditorSceneManager.OpenScene("Assets/Whitebox/Scenes/GameEntry.unity", OpenSceneMode.Single);
            Directory.CreateDirectory("Artifacts/Art50");
            var paths = new List<string>();
            foreach (var guid in AssetDatabase.FindAssets("", new[] { "Assets/Resources/MainSkin" })) {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!AssetDatabase.IsValidFolder(path)) paths.Add(path);
            }
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Resources" })) paths.Add(AssetDatabase.GUIDToAssetPath(guid));
            File.WriteAllLines("Artifacts/Art50/changed-assets.txt", paths);
            File.WriteAllText(folder + "/runtime.request", "Validate changed visuals and utility buttons without spending or triggering ads.");
            File.WriteAllText("Artifacts/Art50/capture.request", "capture");
            report.completed = true;
        }
        catch (Exception error) { report.error = error.ToString(); Debug.LogException(error); }
        report.completedUtc = DateTime.UtcNow.ToString("o");
        File.WriteAllText(folder + "/authoring-report.json", JsonUtility.ToJson(report, true));
    }
}
