using System;
using System.Collections.Generic;
using System.IO;
using DragonLegend.Whitebox;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Editor observer of existing Play-mode objects; only invokes the visual click method.
[InitializeOnLoad]
public static class ApprovedIconMotionRuntimeValidation
{
    private const string Output = "Artifacts/ApprovedIconMotion";
    private const string Request = Output + "/runtime.request";
    private const string RotorPath = "Visual/root/dx/diabn/Slot1/Image/ApprovedBaguaRotor";
    private const string WalletPath = "Tubiao/CashOutb/ApprovedSideIcon";
    private static Report report;
    private static int phase;
    private static double began, next, captureAt;
    private static DateTime captureUtc;
    private static RecoveredSpinButton spin;
    private static Transform rotor, wallet;
    private static Image glow;
    private static Animation spinAnimation, walletAnimation;
    private static Scene observedScene;
    [Serializable] private sealed class Sample
    {
        public float seconds, rotorAngle, walletScale, glowAlpha;
        public bool spinPlaying, walletPlaying;
    }
    [Serializable] private sealed class Report
    {
        public string status = "RUNNING", scene, spinName, walletName, error;
        public bool passed, clickStarted, clickReturnedToIdle, sceneUnchanged, screenshotSaved;
        public int walletRises;
        public float rotorTravel, walletMinimum = 999, walletMaximum, glowMinimum = 999, glowMaximum;
        public float observationSeconds, clickSeconds;
        public List<Sample> samples = new List<Sample>();
    }
    static ApprovedIconMotionRuntimeValidation() { EditorApplication.update += Tick; }
    [MenuItem("Tools/Approved Art/Observe Running Icon Motion")]
    public static void Run()
    {
        if (report != null) throw new InvalidOperationException("Icon runtime observation is already running.");
        Directory.CreateDirectory(Output); report = new Report(); phase = 0;
        began = EditorApplication.timeSinceStartup; next = began;
        if (!EditorApplication.isPlaying) Finish("Requires existing Play mode; no mode change was performed.");
    }
    private static void Tick()
    {
        double now = EditorApplication.timeSinceStartup;
        if (now < next || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        next = now + .1;
        try
        {
            if (report == null)
            {
                if (!File.Exists(Request)) return;
                File.Delete(Request); Run(); return;
            }
            if (!EditorApplication.isPlaying) { Finish("Play mode stopped during observation."); return; }
            if (phase == 0)
            {
                if (!FindExisting())
                {
                    if (now - began >= 30) Finish("No active approved SpinButton and wallet found within 30 seconds.");
                    else next = now + .5;
                    return;
                }
                observedScene = SceneManager.GetActiveScene(); report.scene = observedScene.name;
                report.spinName = spin.name; report.walletName = wallet.name;
                began = now; phase = 1;
            }
            if (spin == null || rotor == null || wallet == null || glow == null || !spin.gameObject.activeInHierarchy || !wallet.gameObject.activeInHierarchy)
            { Finish("Observed icons were removed or hidden."); return; }
            if (SceneManager.GetActiveScene().handle != observedScene.handle)
            { Finish("Active scene changed during observation."); return; }
            if (phase == 1)
            {
                var sample = new Sample { seconds = (float)(now - began), rotorAngle = rotor.localEulerAngles.z,
                    walletScale = wallet.localScale.x, glowAlpha = glow.color.a,
                    spinPlaying = spinAnimation.enabled && spinAnimation.IsPlaying("idle"),
                    walletPlaying = walletAnimation.enabled && walletAnimation.IsPlaying(walletAnimation.clip.name) };
                if (!sample.spinPlaying || !sample.walletPlaying) { Finish("Expected idle animations are not playing."); return; }
                if (report.samples.Count > 0)
                {
                    var previous = report.samples[report.samples.Count - 1];
                    report.rotorTravel += Mathf.Abs(Mathf.DeltaAngle(previous.rotorAngle, sample.rotorAngle));
                    if (previous.glowAlpha < .5f && sample.glowAlpha >= .5f) report.walletRises++;
                }
                report.samples.Add(sample); report.observationSeconds = sample.seconds;
                report.walletMinimum = Mathf.Min(report.walletMinimum, sample.walletScale); report.walletMaximum = Mathf.Max(report.walletMaximum, sample.walletScale);
                report.glowMinimum = Mathf.Min(report.glowMinimum, sample.glowAlpha); report.glowMaximum = Mathf.Max(report.glowMaximum, sample.glowAlpha);
                if (now - began < 8) return;
                if (report.samples.Count < 30 || report.rotorTravel < 500 || report.walletRises < 3 ||
                    report.walletMinimum > 1.01f || report.walletMaximum < 1.05f || report.glowMinimum > .1f || report.glowMaximum < .9f)
                { Finish("Live samples did not show repeated rotation and wallet breathing/glow cycles."); return; }
                // Calls animation presentation only: never Button.onClick or SpinEntry.
                spin.PlayAcceptedClick(); report.clickStarted = spin.IsClickAnimationPlaying && spinAnimation.IsPlaying("dianji");
                if (!report.clickStarted) { Finish("Visual accepted-click animation did not start."); return; }
                began = now; phase = 2; return;
            }
            if (phase == 2)
            {
                report.clickSeconds = (float)(now - began);
                if (now - began < 1.25) return;
                report.clickReturnedToIdle = !spin.IsClickAnimationPlaying && spinAnimation.IsPlaying("idle");
                if (!report.clickReturnedToIdle)
                {
                    if (now - began > 4) Finish("LateUpdate did not return dianji to idle within 4 seconds.");
                    return;
                }
                captureUtc = DateTime.UtcNow; captureAt = now;
                ScreenCapture.CaptureScreenshot(Output + "/runtime.png"); phase = 3; return;
            }
            if (phase == 3)
            {
                report.screenshotSaved = File.Exists(Output + "/runtime.png") && File.GetLastWriteTimeUtc(Output + "/runtime.png") >= captureUtc;
                if (report.screenshotSaved || now - captureAt > 5) Finish(null);
            }
        }
        catch (Exception error) { if (report != null) Finish(error.ToString()); else Debug.LogException(error); }
    }
    private static bool FindExisting()
    {
        spin = null; rotor = null; wallet = null; glow = null;
        foreach (var candidate in UnityEngine.Object.FindObjectsOfType<RecoveredSpinButton>())
        {
            if (!Live(candidate.gameObject)) continue;
            var center = candidate.transform.Find(RotorPath);
            var animation = candidate.GetComponent<Animation>();
            if (center == null || animation == null || candidate.Button == null || candidate.IsClickAnimationPlaying) continue;
            if (candidate.Button.onClick.GetPersistentEventCount() != 0) throw new InvalidOperationException("Persistent SpinButton callbacks.");
            spin = candidate; rotor = center; spinAnimation = animation; break;
        }
        foreach (var candidate in UnityEngine.Object.FindObjectsOfType<RecoveredCashOutEntry>())
        {
            if (!Live(candidate.gameObject)) continue;
            var icon = candidate.transform.Find(WalletPath);
            if (icon == null || !icon.gameObject.activeInHierarchy || candidate.Button == null || candidate.FingerTarget == null) continue;
            var animation = icon.GetComponent<Animation>(); var child = icon.Find("ApprovedWalletGlow");
            if (animation == null || animation.clip == null || child == null || child.GetComponent<Image>() == null) continue;
            if (candidate.Button.onClick.GetPersistentEventCount() != 0) throw new InvalidOperationException("Persistent wallet Button callbacks.");
            wallet = icon; walletAnimation = animation; glow = child.GetComponent<Image>(); break;
        }
        return spin != null && wallet != null;
    }
    private static bool Live(GameObject value)
        => value.activeInHierarchy && value.scene.IsValid() && value.scene.isLoaded && !EditorSceneManager.IsPreviewScene(value.scene);
    private static void Finish(string error)
    {
        report.error = error; report.passed = error == null;
        report.status = report.passed ? "PASS" : "FAIL";
        report.sceneUnchanged = observedScene.IsValid() && SceneManager.GetActiveScene().handle == observedScene.handle;
        File.WriteAllText(Output + "/runtime-validation.json", JsonUtility.ToJson(report, true));
        if (report.passed) Debug.Log("APPROVED_ICON_MOTION_RUNTIME_PASS");
        else Debug.LogError("APPROVED_ICON_MOTION_RUNTIME_FAIL: " + error);
        report = null; spin = null; rotor = wallet = null; glow = null; spinAnimation = walletAnimation = null;
    }
}
