using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DragonLegend.Whitebox.EditorTools
{
    public static class BuildLocalizedAndroidPlayer
    {
        [MenuItem("Dragon Legend/Localization/Build Android Player")]
        public static void Build()
        {
            const string directory = "Artifacts/Localization";
            Directory.CreateDirectory(directory);
            // Use the project's real package, signing, country settings and player backend unchanged.
            bool originalExport = EditorUserBuildSettings.exportAsGoogleAndroidProject;
            bool originalBundle = EditorUserBuildSettings.buildAppBundle;
            BuildReport report;
            try
            {
                EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
                EditorUserBuildSettings.buildAppBundle = false;
                report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                    scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
                    locationPathName = directory + "/GildedDragon-localized.apk",
                    target = BuildTarget.Android,
                    options = BuildOptions.None
                });
            }
            finally
            {
                EditorUserBuildSettings.exportAsGoogleAndroidProject = originalExport;
                EditorUserBuildSettings.buildAppBundle = originalBundle;
            }
            var result = report.summary;
            File.WriteAllText(directory + "/android-build-summary.txt",
                "Result: " + result.result + "\nErrors: " + result.totalErrors + "\nWarnings: " + result.totalWarnings
                + "\nBytes: " + result.totalSize + "\nDuration: " + result.totalTime + "\nOutput: " + result.outputPath + "\n");
            if (result.result != BuildResult.Succeeded || !File.Exists(result.outputPath))
                throw new InvalidOperationException("Localized Android build failed: " + result.result);
            Debug.Log("LOCALIZED_ANDROID_BUILD_SUCCEEDED: " + result.outputPath);
        }
    }
}
