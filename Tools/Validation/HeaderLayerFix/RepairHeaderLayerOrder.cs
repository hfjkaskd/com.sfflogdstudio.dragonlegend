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

// Temporary one-shot Editor fixture. The preview never binds gameplay or player data.
[InitializeOnLoad]
public static class RepairHeaderLayerOrder
{
    private const string Output = "Artifacts/HeaderLayerFix";
    private const string Request = Output + "/apply.request";
    private const string Resume = "HeaderLayerFix.ResumePlay";
    private const string Prefab = "Assets/Resources/MainSkin/Assets/RecoveredUI/BalancePanel.prefab";
    private static double readyAt;
    [Serializable] private sealed class Report
    {
        public bool passed, idempotent, sceneUnchanged, resumePlayRequested;
        public int[] plateFillBadgeLevelProgressOrder;
        public string preview = Output + "/level10-progress80.png", error = "", completedUtc;
    }
    static RepairHeaderLayerOrder() { EditorApplication.update += Tick; }
    private static void Tick()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        { readyAt = EditorApplication.timeSinceStartup + .5; return; }
        if (EditorApplication.timeSinceStartup < readyAt || !File.Exists(Request)) return;
        readyAt = EditorApplication.timeSinceStartup + .25;
        if (EditorApplication.isPlaying)
        { SessionState.SetBool(Resume, true); EditorApplication.isPlaying = false; return; }
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        File.Delete(Request);
        bool resume = SessionState.GetBool(Resume, false);
        try { Run(resume); }
        finally
        {
            SessionState.SetBool(Resume, false);
            if (resume) EditorApplication.isPlaying = true;
        }
    }
    private static void Run(bool resume)
    {
        Directory.CreateDirectory(Output);
        var report = new Report { resumePlayRequested = resume };
        var originalScene = SceneManager.GetActiveScene();
        try
        {
            string first = null;
            for (int pass = 0; pass < 2; pass++)
            {
                var root = PrefabUtility.LoadPrefabContents(Prefab);
                try { ApplyApprovedHeader.ApplyProgressLayers(root); PrefabUtility.SaveAsPrefabAsset(root, Prefab); }
                finally { PrefabUtility.UnloadPrefabContents(root); }
                AssetDatabase.SaveAssets();
                string saved = File.ReadAllText(Prefab);
                if (pass == 0) first = saved;
                else report.idempotent = first == saved;
            }
            if (!report.idempotent) throw new InvalidOperationException("Repeated header authoring changed the saved prefab.");
            RenderPreview(report);
            report.sceneUnchanged = SceneManager.GetActiveScene().handle == originalScene.handle;
            if (!report.sceneUnchanged) throw new InvalidOperationException("The active scene changed during preview.");
            report.passed = true;
            Debug.Log("HEADER_LAYER_FIX_PASS: level 10, experience 8/10, original scene preserved.");
        }
        catch (Exception error) { report.error = error.ToString(); Debug.LogException(error); }
        finally
        {
            report.completedUtc = DateTime.UtcNow.ToString("o");
            File.WriteAllText(Output + "/report.json", JsonUtility.ToJson(report, true));
        }
    }
    private static void RenderPreview(Report report)
    {
        Scene preview = EditorSceneManager.NewPreviewScene();
        RenderTexture target = null; Texture2D capture = null; var previous = RenderTexture.active;
        try
        {
            var host = Hidden("Header layer preview Canvas", preview);
            var canvasRect = host.AddComponent<RectTransform>(); canvasRect.pivot = new Vector2(0, 1);
            canvasRect.sizeDelta = new Vector2(1080, 227);
            var canvas = host.AddComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace;
            var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab), preview);
            root.transform.SetParent(host.transform, false);
            var rootRect = (RectTransform)root.transform;
            rootRect.anchorMin = rootRect.anchorMax = rootRect.pivot = new Vector2(0, 1);
            rootRect.anchoredPosition3D = Vector3.zero; rootRect.localScale = Vector3.one;
            var panel = root.GetComponent<RecoveredBalancePanel>(); panel.enabled = false;
            var so = new SerializedObject(panel);
            var level = (TMP_Text)so.FindProperty("levelText").objectReferenceValue;
            var progress = (TMP_Text)so.FindProperty("progressText").objectReferenceValue;
            var fill = (Image)so.FindProperty("progressFill").objectReferenceValue;
            var plate = root.transform.Find("_ApprovedProgressPlate").GetComponent<Image>();
            var badge = root.transform.Find("_ApprovedLevelBadge").GetComponent<Image>();
            report.plateFillBadgeLevelProgressOrder = new[] { plate.transform.GetSiblingIndex(), fill.transform.GetSiblingIndex(), badge.transform.GetSiblingIndex(), level.transform.GetSiblingIndex(), progress.transform.GetSiblingIndex() };
            for (int i = 1; i < report.plateFillBadgeLevelProgressOrder.Length; i++)
                if (report.plateFillBadgeLevelProgressOrder[i] <= report.plateFillBadgeLevelProgressOrder[i - 1]) throw new InvalidOperationException("Header drawing order is incorrect.");
            foreach (var graphic in root.GetComponentsInChildren<Graphic>(true))
                graphic.enabled = graphic == level || graphic == progress || graphic == fill || graphic == plate || graphic == badge;
            foreach (var player in root.GetComponentsInChildren<Animation>(true)) player.enabled = false;
            level.text = "10"; progress.text = "8/10"; fill.fillAmount = .8f;
            foreach (var node in host.GetComponentsInChildren<Transform>(true)) node.gameObject.layer = 5;
            var camera = Hidden("Header layer preview Camera", preview).AddComponent<Camera>();
            camera.transform.position = new Vector3(248, -181, -10);
            camera.orthographic = true; camera.orthographicSize = 48;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.11f, .008f, .003f, 1);
            camera.cullingMask = 1 << 5; camera.scene = preview;
            camera.overrideSceneCullingMask = EditorSceneManager.GetSceneCullingMask(preview);
            camera.enabled = false; canvas.worldCamera = camera;
            target = new RenderTexture(1158, 288, 24, RenderTextureFormat.ARGB32); target.Create(); camera.targetTexture = target;
            Canvas.ForceUpdateCanvases(); level.ForceMeshUpdate(); progress.ForceMeshUpdate();
            if (GraphicsSettings.currentRenderPipeline != null)
                RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
            else camera.Render();
            capture = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
            RenderTexture.active = target; capture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); capture.Apply();
            File.WriteAllBytes(report.preview, capture.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previous;
            if (capture != null) Object.DestroyImmediate(capture);
            if (target != null) { target.Release(); Object.DestroyImmediate(target); }
            if (preview.IsValid()) EditorSceneManager.ClosePreviewScene(preview);
        }
    }
    private static GameObject Hidden(string name, Scene scene)
    { var go = new GameObject(name) { hideFlags = HideFlags.HideAndDontSave }; SceneManager.MoveGameObjectToScene(go, scene); return go; }
}
