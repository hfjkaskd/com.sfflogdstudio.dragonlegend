using System;
using System.IO;
using UnityEditor;
using UnityEngine;

// Explicit one-shot Editor request; never changes gameplay initialization.
[InitializeOnLoad]
public static class RepairApprovedIconMotion
{
    private const string Folder = "Artifacts/ApprovedIconMotion";
    private const string Request = Folder + "/repair.request";
    private const string Resume = "ApprovedIconMotion.ResumePlay";
    private static double next;
    static RepairApprovedIconMotion() { EditorApplication.update += Tick; }
    private static void Tick()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.timeSinceStartup < next) return;
        next = EditorApplication.timeSinceStartup + 1;
        if (!File.Exists(Request)) return;
        if (File.ReadAllText(Request).Trim() == "repair-and-play") SessionState.SetBool(Resume, true);
        if (EditorApplication.isPlaying)
        {
            SessionState.SetBool(Resume, true);
            EditorApplication.isPlaying = false;
            return;
        }
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        File.Delete(Request);
        try
        {
            Run();
            if (SessionState.GetBool(Resume, false))
            {
                SessionState.SetBool(Resume, false);
                EditorApplication.isPlaying = true;
            }
        }
        catch (Exception error)
        {
            File.WriteAllText(Folder + "/authoring-error.txt", error.ToString());
            Debug.LogException(error);
        }
    }

    [MenuItem("Tools/Approved Art/Repair Spin and Cash-Out Motion")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Repair requires Edit mode.");
        Directory.CreateDirectory(Folder);
        // Repeat the authoring pass to verify it preserves asset identity and hierarchy.
        ApprovedSpinMotionAuthoring.Apply();
        ApprovedSideIconsAuthoring.Apply();
        ApprovedSpinMotionAuthoring.Apply();
        ApprovedSideIconsAuthoring.Apply();
        AssetDatabase.SaveAssets();
        ApprovedIconMotionValidation.Run();
        File.WriteAllText(Folder + "/authoring-pass.txt", DateTime.UtcNow.ToString("o") + "\nPASS: repeated authoring and clip/render validation\n");
        Debug.Log("APPROVED_ICON_MOTION_REPAIR_PASS");
    }
}
