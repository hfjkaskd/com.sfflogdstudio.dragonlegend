using System.IO;
using DragonLegend.Whitebox;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class BuildLampFlight
{
    public static void Save()
    {
        const string folder = "Assets/Resources/RecoveredSymbols/LampFlight";
        Directory.CreateDirectory(folder);
        foreach (var name in new[] { "xx.mat", "xx.mat.meta", "wenli.mat", "wenli.mat.meta" })
            if (!File.Exists(folder + "/" + name)) File.Copy("ReferenceOriginal/Material/" + name, folder + "/" + name);
        const string destination = "Assets/Resources/RecoveredSymbols/LampFlight.prefab";
        if (!File.Exists(destination)) File.Copy("ReferenceOriginal/GameObject/tuowei.prefab", destination);
        AssetDatabase.Refresh();
        var root = PrefabUtility.LoadPrefabContents(destination);
        try {
            root.transform.localPosition = new Vector3(-1.87f, -2.1f, 0) * .01f;
            root.transform.localScale = Vector3.one * .01f;
            var group = root.GetComponent<SortingGroup>(); if (group == null) group = root.AddComponent<SortingGroup>();
            group.sortAtRoot = true;
            var motion = root.GetComponent<RecoveredLampFlight>(); if (motion == null) motion = root.AddComponent<RecoveredLampFlight>();
            var settings = new SerializedObject(motion);
            settings.FindProperty("duration").floatValue = .3f;
            settings.FindProperty("automaticArcRatio").floatValue = .3f;
            settings.FindProperty("sortingOffset").intValue = 1;
            settings.FindProperty("sortingGroup").objectReferenceValue = group;
            var renderers = root.GetComponentsInChildren<Renderer>(); var array = settings.FindProperty("renderers"); array.arraySize = renderers.Length;
            for (int i = 0; i < renderers.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = renderers[i];
            settings.ApplyModifiedPropertiesWithoutUndo(); PrefabUtility.SaveAsPrefabAsset(root, destination);
        } finally { PrefabUtility.UnloadPrefabContents(root); }
        BuildSpinPlayfield.Save(); AssetDatabase.SaveAssets();
    }
}
