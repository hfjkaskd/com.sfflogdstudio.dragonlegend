using System.IO;
using DragonLegend.Whitebox;
using UnityEditor;
using UnityEngine;

public static class BuildCoreAudio
{
    public static void Save()
    {
        const string folder = "Assets/Resources/RecoveredAudio";
        Directory.CreateDirectory(folder);
        var files = Directory.GetFiles("ReferenceOriginal/AudioClip", "*.ogg");
        System.Array.Sort(files, System.StringComparer.Ordinal);
        foreach (var file in files) File.Copy(file, folder + "/" + Path.GetFileName(file), true);
        AssetDatabase.Refresh();
        var root = new GameObject("CoreAudio", typeof(RecoveredCoreAudio), typeof(RecoveredSoundManager));
        try {
            var manager = root.GetComponent<RecoveredSoundManager>();
            var settings = new SerializedObject(manager);
            string[] fields = { "bgm", "sound", "sound1" };
            for (int i = 0; i < fields.Length; i++) {
                var child = new GameObject(fields[i], typeof(AudioSource)); child.transform.SetParent(root.transform, false);
                var source = child.GetComponent<AudioSource>();
                // Loading.unity AudioSources 33/34/35: volume/pitch 1, 2D, BGM loop only.
                source.playOnAwake = false; source.loop = i == 0; source.volume = 1; source.pitch = 1;
                source.spatialBlend = 0; source.priority = 128; source.dopplerLevel = 1;
                source.minDistance = 1; source.maxDistance = 500;
                settings.FindProperty(fields[i]).objectReferenceValue = source;
            }
            settings.FindProperty("resourceRoot").stringValue = "RecoveredAudio/";
            settings.FindProperty("initialMusic").stringValue = "normalBg";
            var names = settings.FindProperty("clipNames"); names.arraySize = files.Length;
            for (int i = 0; i < files.Length; i++) names.GetArrayElementAtIndex(i).stringValue = Path.GetFileNameWithoutExtension(files[i]);
            settings.ApplyModifiedPropertiesWithoutUndo();
            var binding = new SerializedObject(root.GetComponent<RecoveredCoreAudio>());
            binding.FindProperty("clickSound").stringValue = "click";
            binding.FindProperty("spinSound").stringValue = "spin";
            string[] eventFields = { "reelStopSound", "speedupSound", "coinShowSound", "coinRevealSound", "lampArrivalSound", "coinBurstSound" };
            string[] eventClips = { "reelstop", "speedup", "coinshow", "coinReveal", "exp", "coinBrust" };
            for (int i = 0; i < eventFields.Length; i++) binding.FindProperty(eventFields[i]).stringValue = eventClips[i];
            binding.FindProperty("audioManager").objectReferenceValue = manager; binding.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, folder + "/CoreAudio.prefab");
        } finally { Object.DestroyImmediate(root); }
        const string path = "Assets/Resources/Whitebox/GameEntry.prefab";
        var game = PrefabUtility.LoadPrefabContents(path);
        try {
            var settings = new SerializedObject(game.GetComponent<GameEntry>());
            settings.FindProperty("coreAudioPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<RecoveredCoreAudio>(folder + "/CoreAudio.prefab");
            settings.ApplyModifiedPropertiesWithoutUndo(); PrefabUtility.SaveAsPrefabAsset(game, path);
        } finally { PrefabUtility.UnloadPrefabContents(game); }
        AssetDatabase.SaveAssets();
    }
}
