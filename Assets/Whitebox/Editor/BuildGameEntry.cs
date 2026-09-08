using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Retain the batch entry point without regenerating authored layout and settings.
public static class BuildGameEntry
{
    [MenuItem("Dragon Legend/Open Authored Entry")]
    public static void Build()
    {
        const string scenePath = "Assets/Whitebox/Scenes/GameEntry.unity";
        if (!File.Exists(scenePath)) throw new FileNotFoundException("Authored entry scene is missing.", scenePath);
        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        Debug.Log("GAME_ENTRY_OPENED: authored camera, Canvas and render settings retained.");
    }
}
