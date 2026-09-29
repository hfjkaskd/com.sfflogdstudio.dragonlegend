using System;
using System.IO;
using DragonLegend.Whitebox;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// One-shot prefab authoring and isolated preview; never invokes a cash-out event.
[InitializeOnLoad]
public static class RepairMainPayPalButton
{
    private const string Output = "Artifacts/MainPayPalButton";
    private const string Request = Output + "/apply.request";
    private const string Resume = "MainPayPalButton.ResumePlay";
    private const string Prefab = "Assets/Resources/MainSkin/Assets/RecoveredUI/BalancePanel.prefab";
    private static double readyAt;
    [Serializable] private sealed class Report
    {
        public bool passed, buttonPreserved, targetGraphicPreserved, sceneUnchanged, resumePlayRequested, childRaycastsDisabled, oldCaptionHidden;
        public int persistentEvents;
        public float left, top, right, width, height;
        public string rootSprite, preview = Output + "/header.png", closeup = Output + "/button.png", error, completedUtc;
    }
    static RepairMainPayPalButton() { EditorApplication.update += Tick; }
    private static void Tick()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) { readyAt = EditorApplication.timeSinceStartup + .5; return; }
        if (EditorApplication.timeSinceStartup < readyAt || !File.Exists(Request)) return;
        readyAt = EditorApplication.timeSinceStartup + .25;
        if (EditorApplication.isPlaying) { SessionState.SetBool(Resume, true); EditorApplication.isPlaying = false; return; }
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        File.Delete(Request); bool resume = SessionState.GetBool(Resume, false);
        try { ApplyAndPreview(resume); }
        finally { SessionState.SetBool(Resume, false); if (resume) EditorApplication.isPlaying = true; }
    }
    public static void Run() { ApplyAndPreview(false); }
    private static void ApplyAndPreview(bool resume)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Authoring requires Edit mode.");
        Directory.CreateDirectory(Output); var report = new Report { resumePlayRequested = resume };
        var originalScene = SceneManager.GetActiveScene();
        try
        {
            var root = PrefabUtility.LoadPrefabContents(Prefab);
            try
            {
                var panel = root.GetComponent<RecoveredBalancePanel>();
                var button = panel.WithdrawButton; Require(button != null, "Existing withdraw Button missing.");
                var graphic = button.targetGraphic; var buttonObject = button.gameObject;
                ApplyApprovedHeader.ApplyPayPalWithdrawButton(root);
                report.buttonPreserved = panel.WithdrawButton == button && button.gameObject == buttonObject && buttonObject.GetComponent<Button>() == button;
                report.targetGraphicPreserved = button.targetGraphic == graphic && graphic != null && graphic.gameObject == buttonObject;
                Require(report.buttonPreserved && report.targetGraphicPreserved, "Existing Button or root targetGraphic changed.");
                Validate(root, report);
                PrefabUtility.SaveAsPrefabAsset(root, Prefab);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets(); RenderPreview(report);
            report.sceneUnchanged = SceneManager.GetActiveScene().handle == originalScene.handle;
            Require(report.sceneUnchanged, "Preview changed the active scene."); report.passed = true;
            Debug.Log("MAIN_PAYPAL_BUTTON_PASS: existing Button retained; full-header and close-up renders saved.");
        }
        catch (Exception error) { report.error = error.ToString(); Debug.LogException(error); }
        finally { report.completedUtc = DateTime.UtcNow.ToString("o"); File.WriteAllText(Output + "/report.json", JsonUtility.ToJson(report, true)); }
    }
    private static void Validate(GameObject root, Report report)
    {
        var button = root.GetComponent<RecoveredBalancePanel>().WithdrawButton;
        report.persistentEvents = button.onClick.GetPersistentEventCount(); Require(report.persistentEvents == 0, "Unexpected persistent callbacks.");
        Require(button.targetGraphic != null && button.targetGraphic.raycastTarget, "Button root raycast Graphic missing.");
        report.childRaycastsDisabled = true; report.oldCaptionHidden = true;
        foreach (var graphic in button.GetComponentsInChildren<Graphic>(true))
            if (graphic.gameObject != button.gameObject && graphic.raycastTarget) report.childRaycastsDisabled = false;
        foreach (var text in button.GetComponentsInChildren<TMP_Text>(true))
            if (text.enabled && text.gameObject.activeInHierarchy && text.text.Replace(" ", "").IndexOf("CASHOUT", StringComparison.OrdinalIgnoreCase) >= 0) report.oldCaptionHidden = false;
        Require(report.childRaycastsDisabled && report.oldCaptionHidden, "Child raycasts or obsolete CASHOUT caption remain.");
        var corners = new Vector3[4]; ((RectTransform)button.transform).GetWorldCorners(corners);
        Vector3 bottomLeft = root.transform.InverseTransformPoint(corners[0]), topRight = root.transform.InverseTransformPoint(corners[2]);
        Rect rootBounds = ((RectTransform)root.transform).rect;
        report.left = bottomLeft.x - rootBounds.xMin; report.right = topRight.x - rootBounds.xMin; report.top = rootBounds.yMax - topRight.y;
        report.width = topRight.x - bottomLeft.x; report.height = topRight.y - bottomLeft.y;
        Require(Near(report.right, 1056) && Near(report.top, 106) && Near(report.height, 111), "Button right/top/height must stay 1056/106/111.");
        Require(Mathf.Abs(report.width - 271) < 3, "Button width should preserve the original artwork aspect ratio.");
        var image = button.GetComponent<Image>(); Require(image != null && image.sprite != null, "Root button sprite missing.");
        report.rootSprite = AssetDatabase.GetAssetPath(image.sprite);
    }
    private static void RenderPreview(Report report)
    {
        var preview = EditorSceneManager.NewPreviewScene(); var previous = RenderTexture.active;
        try
        {
            var host = Hidden("PayPal header preview Canvas", preview); var rect = host.AddComponent<RectTransform>();
            rect.pivot = new Vector2(0, 1); rect.sizeDelta = new Vector2(1080, 227);
            var canvas = host.AddComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace;
            var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab), preview);
            root.transform.SetParent(host.transform, false); var rootRect = (RectTransform)root.transform;
            rootRect.anchorMin = rootRect.anchorMax = rootRect.pivot = new Vector2(0, 1);
            rootRect.anchoredPosition3D = Vector3.zero; rootRect.localScale = Vector3.one;
            root.GetComponent<RecoveredBalancePanel>().enabled = false;
            foreach (var animation in root.GetComponentsInChildren<Animation>(true)) animation.enabled = false;
            foreach (var node in host.GetComponentsInChildren<Transform>(true)) node.gameObject.layer = 5;
            var camera = Hidden("PayPal header preview Camera", preview).AddComponent<Camera>();
            camera.orthographic = true; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.11f, .008f, .003f, 1);
            camera.cullingMask = 1 << 5; camera.scene = preview; camera.overrideSceneCullingMask = EditorSceneManager.GetSceneCullingMask(preview);
            camera.enabled = false; canvas.worldCamera = camera;
            Canvas.ForceUpdateCanvases();
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true)) if (text.enabled && text.gameObject.activeInHierarchy) text.ForceMeshUpdate();
            Validate(root, report);
            // Render every authored Graphic as-is; no isolation by hiding neighboring UI.
            Capture(camera, new Vector2(540, -113.5f), 113.5f, 1080, 227, report.preview);
            var button = root.GetComponent<RecoveredBalancePanel>().WithdrawButton;
            var center = button.transform.TransformPoint(((RectTransform)button.transform).rect.center);
            int height = Mathf.CeilToInt((report.height + 24) * 2), width = Mathf.CeilToInt((report.width + 24) * 2);
            Capture(camera, center, (report.height + 24) / 2, width, height, report.closeup);
        }
        finally { RenderTexture.active = previous; if (preview.IsValid()) EditorSceneManager.ClosePreviewScene(preview); }
    }
    private static void Capture(Camera camera, Vector2 center, float size, int width, int height, string path)
    {
        var previous = RenderTexture.active;
        var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32); Texture2D capture = null;
        try
        {
            target.Create(); camera.targetTexture = target; camera.transform.position = new Vector3(center.x, center.y, -10); camera.orthographicSize = size;
            Canvas.ForceUpdateCanvases();
            if (GraphicsSettings.currentRenderPipeline != null) RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target }); else camera.Render();
            RenderTexture.active = target; capture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            capture.ReadPixels(new Rect(0, 0, width, height), 0, 0); capture.Apply(); File.WriteAllBytes(path, capture.EncodeToPNG());
        }
        finally { camera.targetTexture = null; RenderTexture.active = previous; if (capture != null) Object.DestroyImmediate(capture); target.Release(); Object.DestroyImmediate(target); }
    }
    private static bool Near(float a, float b) => Mathf.Abs(a - b) < .1f;
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static GameObject Hidden(string name, Scene scene)
    { var go = new GameObject(name) { hideFlags = HideFlags.HideAndDontSave }; SceneManager.MoveGameObjectToScene(go, scene); return go; }
}
