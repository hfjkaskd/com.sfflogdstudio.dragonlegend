using System;
using System.IO;
using DragonLegend.Whitebox;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Temporary isolated Editor fixture: no game scene, SDK, player data or network.
public static class ApprovedIconMotionValidation
{
    private const string SpinPath = "Assets/Resources/MainSkin/Assets/RecoveredUI/SpinButton.prefab";
    private const string WalletPath = "Assets/Resources/RecoveredUI/CashOutEntry.prefab";
    private const string Clips = "Assets/Resources/MainSkin/Animations/SpinButton/";
    private const string RotorPath = "Visual/root/dx/diabn/Slot1/Image/ApprovedBaguaRotor";
    private const string WalletVisual = "Tubiao/CashOutb/ApprovedSideIcon";
    private const string Output = "Artifacts/ApprovedIconMotion";
    [Serializable] private sealed class Report
    {
        public bool passed, sceneUnchanged, playModeUnchanged, clickThenIdle;
        public float idleDuration, clickDuration, walletDuration;
        public float[] rotorIdleAngles, rotorClickAngles, walletScale, walletGlowAlpha;
        public int persistentEvents;
        public string idleClip, clickClip, setupClip, walletClip, error;
    }
    public static void Run()
    {
        Directory.CreateDirectory(Output);
        var report = new Report(); var originalScene = SceneManager.GetActiveScene();
        bool playing = EditorApplication.isPlaying; Scene preview = default;
        RenderTexture target = null; var previous = RenderTexture.active;
        try
        {
            var spinAsset = AssetDatabase.LoadAssetAtPath<GameObject>(SpinPath);
            var walletAsset = AssetDatabase.LoadAssetAtPath<GameObject>(WalletPath);
            Require(spinAsset != null && walletAsset != null, "Both prefab assets exist");
            var spinController = spinAsset.GetComponent<RecoveredSpinButton>();
            var walletController = walletAsset.GetComponent<RecoveredCashOutEntry>();
            Require(spinController != null && spinController.Button != null, "Spin Button reference");
            Require(walletController != null && walletController.Button != null && walletController.FingerTarget != null, "Wallet Button and FingerTarget references");
            report.persistentEvents = spinController.Button.onClick.GetPersistentEventCount() + walletController.Button.onClick.GetPersistentEventCount();
            Require(report.persistentEvents == 0, "No serialized Button callbacks");
            var idle = AssetDatabase.LoadAssetAtPath<AnimationClip>(Clips + "idle.anim");
            var click = AssetDatabase.LoadAssetAtPath<AnimationClip>(Clips + "dianji.anim");
            var setup = AssetDatabase.LoadAssetAtPath<AnimationClip>(Clips + "setup.anim");
            Require(idle != null && click != null && setup != null, "MainSkin clips exist");
            var assetPlayer = spinAsset.GetComponent<Animation>();
            Require(assetPlayer != null && assetPlayer.GetClip("idle") == idle && assetPlayer.GetClip("dianji") == click, "Spin animation uses approved clips");
            Require(new SerializedObject(spinController).FindProperty("setup").objectReferenceValue == setup, "Controller setup uses approved clip");
            report.idleClip = AssetDatabase.GetAssetPath(idle); report.clickClip = AssetDatabase.GetAssetPath(click);
            report.setupClip = AssetDatabase.GetAssetPath(setup); report.idleDuration = idle.length; report.clickDuration = click.length;
            Require(Mathf.Abs(idle.length - 4) < .001f && Mathf.Abs(click.length - 1) < .001f, "Idle 4s and click 1s");
            Require(idle.wrapMode == WrapMode.Loop, "Idle loops");
            CheckRotorBinding(idle); CheckRotorBinding(click); CheckRotorBinding(setup);
            preview = EditorSceneManager.NewPreviewScene();
            var host = Hidden("Approved icon preview Canvas", preview); var rect = host.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(512, 512); var canvas = host.AddComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace;
            var cameraHost = Hidden("Approved icon preview Camera", preview); var camera = cameraHost.AddComponent<Camera>();
            camera.transform.position = new Vector3(0, 0, -10); camera.orthographic = true; camera.orthographicSize = 256;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.055f, .012f, .012f, 1);
            camera.cullingMask = 1 << 5; camera.scene = preview; camera.overrideSceneCullingMask = EditorSceneManager.GetSceneCullingMask(preview);
            camera.enabled = false; canvas.worldCamera = camera;
            var wrapper = Hidden("2x preview scale", preview); wrapper.AddComponent<RectTransform>();
            wrapper.transform.SetParent(host.transform, false); wrapper.transform.localScale = Vector3.one * 2;
            var spin = (GameObject)PrefabUtility.InstantiatePrefab(spinAsset, preview);
            spin.transform.SetParent(wrapper.transform, false); Center((RectTransform)spin.transform);
            var rotor = spin.transform.Find(RotorPath); Require(rotor != null && rotor.GetComponent<Image>().sprite != null, "Approved rotor sprite exists");
            var player = spin.GetComponent<Animation>(); player.enabled = false;
            var controller = spin.GetComponent<RecoveredSpinButton>();
            controller.PlayAcceptedClick(); Require(controller.IsClickAnimationPlaying, "Accepted click starts controller state");
            controller.PlayIdle(); report.clickThenIdle = !controller.IsClickAnimationPlaying;
            Require(report.clickThenIdle, "PlayIdle resets accepted click state");
            target = new RenderTexture(512, 512, 24, RenderTextureFormat.ARGB32); target.Create(); camera.targetTexture = target;
            Layer(host.transform); report.rotorIdleAngles = new float[2]; report.rotorClickAngles = new float[2];
            for (int i = 0; i < 2; i++)
            {
                setup.SampleAnimation(spin, 0); idle.SampleAnimation(spin, i);
                report.rotorIdleAngles[i] = rotor.localEulerAngles.z; Capture(camera, target, "spin-" + i + ".png");
                setup.SampleAnimation(spin, 0); click.SampleAnimation(spin, i * .25f);
                report.rotorClickAngles[i] = rotor.localEulerAngles.z;
            }
            Require(Mathf.Abs(Mathf.DeltaAngle(report.rotorIdleAngles[0], report.rotorIdleAngles[1])) > 1, "Idle actually rotates new center");
            Require(Mathf.Abs(Mathf.DeltaAngle(report.rotorClickAngles[0], report.rotorClickAngles[1])) > 1, "Click actually rotates new center");
            spin.SetActive(false);
            var walletSource = walletAsset.transform.Find(WalletVisual); Require(walletSource != null, "Approved wallet visual exists");
            var wallet = Object.Instantiate(walletSource.gameObject, wrapper.transform, false); Center((RectTransform)wallet.transform);
            var walletPlayer = wallet.GetComponent<Animation>(); Require(walletPlayer != null && walletPlayer.clip != null, "Wallet has default animation");
            var walletClip = walletPlayer.clip; walletPlayer.enabled = false;
            report.walletClip = AssetDatabase.GetAssetPath(walletClip); report.walletDuration = walletClip.length;
            Require(Mathf.Abs(walletClip.length - 2) < .001f && walletClip.wrapMode == WrapMode.Loop, "Wallet breath loops every 2s");
            var glowNode = wallet.transform.Find("ApprovedWalletGlow"); Require(glowNode != null, "Original glow is bound to wallet");
            var glow = glowNode.GetComponent<Image>(); Require(glow != null && glow.sprite != null, "Wallet glow Image and sprite");
            report.walletScale = new float[3]; report.walletGlowAlpha = new float[3]; Layer(wallet.transform);
            for (int i = 0; i < 3; i++)
            {
                walletClip.SampleAnimation(wallet, i); report.walletScale[i] = wallet.transform.localScale.x;
                report.walletGlowAlpha[i] = glow.color.a;
                Require(Mathf.Abs(wallet.transform.localScale.x - wallet.transform.localScale.y) < .001f, "Uniform wallet scale");
                if (i < 2) Capture(camera, target, "wallet-" + i + ".png");
            }
            Require(Near(report.walletScale[0], 1) && Near(report.walletScale[1], 1.06f) && Near(report.walletScale[2], 1), "Wallet scale 1/1.06/1");
            Require(Near(report.walletGlowAlpha[0], 0) && Near(report.walletGlowAlpha[1], 1) && Near(report.walletGlowAlpha[2], 0), "Wallet glow alpha 0/1/0");
            report.sceneUnchanged = SceneManager.GetActiveScene().handle == originalScene.handle;
            report.playModeUnchanged = EditorApplication.isPlaying == playing;
            Require(report.sceneUnchanged && report.playModeUnchanged, "Existing scene and Play mode preserved");
            report.passed = true; Debug.Log("APPROVED_ICON_MOTION_VALIDATION_PASS");
        }
        catch (Exception error) { report.error = error.ToString(); throw; }
        finally
        {
            RenderTexture.active = previous;
            if (target != null) { target.Release(); Object.DestroyImmediate(target); }
            if (preview.IsValid()) EditorSceneManager.ClosePreviewScene(preview);
            File.WriteAllText(Output + "/validation.json", JsonUtility.ToJson(report, true));
        }
    }
    private static void Capture(Camera camera, RenderTexture target, string name)
    {
        Canvas.ForceUpdateCanvases();
        if (GraphicsSettings.currentRenderPipeline != null) RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
        else camera.Render();
        var texture = new Texture2D(512, 512, TextureFormat.RGBA32, false);
        try
        {
            RenderTexture.active = target; texture.ReadPixels(new Rect(0, 0, 512, 512), 0, 0); texture.Apply();
            int visible = 0;
            foreach (var pixel in texture.GetPixels32()) if (pixel.r > 70 || pixel.g > 70 || pixel.b > 70) visible++;
            Require(visible > 1000, "Nonblank actual rendered icon " + name);
            File.WriteAllBytes(Output + "/" + name, texture.EncodeToPNG());
        }
        finally { Object.DestroyImmediate(texture); }
    }
    private static void CheckRotorBinding(AnimationClip clip)
    {
        foreach (var binding in AnimationUtility.GetCurveBindings(clip))
            if (binding.path == RotorPath && binding.propertyName == "localEulerAnglesRaw.z") return;
        throw new InvalidOperationException("Missing approved rotor rotation binding in " + clip.name);
    }
    private static bool Near(float a, float b) => Mathf.Abs(a - b) < .002f;
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Center(RectTransform rect)
    { rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f); rect.anchoredPosition3D = Vector3.zero; rect.localRotation = Quaternion.identity; }
    private static void Layer(Transform root)
    { foreach (var child in root.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 5; }
    private static GameObject Hidden(string name, Scene scene)
    { var go = new GameObject(name) { hideFlags = HideFlags.HideAndDontSave }; SceneManager.MoveGameObjectToScene(go, scene); return go; }
}
