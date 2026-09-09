using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildCurrentAndroidPlayer
{
    public static void Build()
    {
        const string output="Artifacts/DragonLegend-current-arm64.apk";
        var target=NamedBuildTarget.Android;
        string identifier=PlayerSettings.GetApplicationIdentifier(target);
        var backend=PlayerSettings.GetScriptingBackend(target);
        var architectures=PlayerSettings.Android.targetArchitectures;
        bool bundle=EditorUserBuildSettings.buildAppBundle;
        try
        {
            // Keep the verification player separate from the installed original APK.
            PlayerSettings.SetApplicationIdentifier(target,"com.sfflogdstudio.dragonlegend.reconstruction");
            PlayerSettings.SetScriptingBackend(target,ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
            EditorUserBuildSettings.buildAppBundle=false;
            var scenes=new List<string>();
            foreach(var scene in EditorBuildSettings.scenes)if(scene.enabled)scenes.Add(scene.path);
            Directory.CreateDirectory("Artifacts");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes=scenes.ToArray(),locationPathName=output,
                target=BuildTarget.Android,options=BuildOptions.Development
            });
            var summary=report.summary;
            File.WriteAllText("Artifacts/current-android-build.txt",
                "Result: "+summary.result+"\nErrors: "+summary.totalErrors+
                "\nWarnings: "+summary.totalWarnings+"\nBytes: "+summary.totalSize+
                "\nDuration: "+summary.totalTime+"\nOutput: "+summary.outputPath+"\n");
            if(summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Current Android build failed: "+summary.result);
        }
        finally
        {
            PlayerSettings.SetApplicationIdentifier(target,identifier);
            PlayerSettings.SetScriptingBackend(target,backend);
            PlayerSettings.Android.targetArchitectures=architectures;
            EditorUserBuildSettings.buildAppBundle=bundle;
            AssetDatabase.SaveAssets();
        }
    }
}
