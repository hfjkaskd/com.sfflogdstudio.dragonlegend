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

// Isolated visual regression. This helper never saves or repairs production assets.
public static class ValidateFreeLuckyDepth
{
    const string Output = "Artifacts/FreeLuckyDepth";
    const int Width = 640, Height = 480;
    [Serializable] sealed class ModeReport
    {
        public string mode;
        public bool passed, sourcePreserved, spritePreserved, sizePreserved, scalePreserved, nonInteractive, popupSeparate, otherBindingsPassed;
        public int canvasOrder, symbolOrder, coverOrder, reelGroupOrder, testedPixels;
        public float beforeError, symbolError, coverError, afterError, popupError;
    }

    sealed class NoAds : IAdFacade
    {
        public void PlayRewardAd(Action successfulBack, Action failedBack, string posId, string sceneId) => throw new InvalidOperationException("Preview must not request an ad.");
        public void PlayInterAd(string posId, string sceneId) => throw new InvalidOperationException("Preview must not request an ad.");
    }
    [Serializable] sealed class Report
    {
        public bool passed;
        public bool allRewardPrefabsHaveSeparateCanvas;
        public ModeReport screenSpaceCamera, worldSpace;
        public string error;
    }

    [MenuItem("Tools/Validation/Free Lucky Depth")]
    public static void Run()
    {
        Directory.CreateDirectory(Output);
        var report = new Report();
        var loop = UnityEngine.LowLevel.PlayerLoop.GetCurrentPlayerLoop();
        var random = UnityEngine.Random.state;
        try
        {
            VerifyStructure("FreeLuckyGame", typeof(RecoveredFreeLuckyGame));
            VerifyStructure("FreeWheelGame", typeof(RecoveredFreeWheelGame));
            VerifyStructure("FreeSlotGame", typeof(RecoveredFreeSlotGame));
            VerifyStructure("FreeTreasureGame", typeof(RecoveredFreeTreasureGame));
            report.allRewardPrefabsHaveSeparateCanvas = true;
            report.screenSpaceCamera = Verify(RenderMode.ScreenSpaceCamera, "screen-camera");
            report.worldSpace = Verify(RenderMode.WorldSpace, "world-space");
            report.passed = report.screenSpaceCamera.passed && report.worldSpace.passed;
            Require(report.passed, "A rendering mode failed.");
            Debug.Log("FREE_LUCKY_DEPTH_PASS");
        }
        catch (Exception error) { report.error = error.ToString(); Debug.LogException(error); }
        finally
        {
            UnityEngine.LowLevel.PlayerLoop.SetPlayerLoop(loop);
            UnityEngine.Random.state = random;
            File.WriteAllText(Output + "/report.json", JsonUtility.ToJson(report, true));
        }
        if (Application.isBatchMode) EditorApplication.Exit(report.passed ? 0 : 1);
    }

    static void VerifyStructure(string name, Type type)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/RecoveredUI/" + name + ".prefab");
        Require(asset != null, name + " prefab missing.");
        var settings = new SerializedObject(asset.GetComponent(type));
        var icon = settings.FindProperty("icon").objectReferenceValue as RectTransform;
        var layerField = settings.FindProperty("rewardCanvas");
        var offsetField = settings.FindProperty("rewardSortingOffset");
        Require(layerField != null && offsetField != null, name + " has no authored reward sorting fields.");
        var layer = layerField.objectReferenceValue as Canvas;
        Require(layer != null && icon != null && icon.IsChildOf(layer.transform), name + " reward is outside its Canvas.");
        Require(layer.overrideSorting && layer.sortingOrder == 50 && offsetField.intValue == 50, name + " incorrect authored reward order.");
        Require(layer.transform != asset.transform && layer.GetComponentsInChildren<Canvas>(true).Length == 1, name + " reward Canvas includes a nested popup.");
        Require(layer.GetComponentsInChildren<GraphicRaycaster>(true).Length == 0, name + " reward layer captures input.");
        foreach (var graphic in layer.GetComponentsInChildren<Graphic>(true)) Require(!graphic.raycastTarget, name + " reward image captures input.");
    }

    static ModeReport Verify(RenderMode mode, string label)
    {
        var report = new ModeReport { mode = label };
        var preview = EditorSceneManager.NewPreviewScene();
        var target = new RenderTexture(Width, Height, 24);
        try
        {
            target.Create();
            var camera = Node("Camera", preview, typeof(Camera)).GetComponent<Camera>();
            camera.orthographic = true; camera.orthographicSize = 120;
            camera.transform.position = new Vector3(0, 0, -100);
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.16f, .08f, .025f);
            camera.scene = preview; camera.overrideSceneCullingMask = EditorSceneManager.GetSceneCullingMask(preview);
            camera.cullingMask = (1 << 5) | 1; camera.enabled = false; camera.targetTexture = target;
            var host = Node("Main Canvas", preview, typeof(RectTransform), typeof(Canvas));
            var main = host.GetComponent<Canvas>(); main.renderMode = mode; main.worldCamera = camera; main.planeDistance = 100;
            ((RectTransform)host.transform).sizeDelta = new Vector2(320, 240);
            VerifyOtherBindings(preview, host.transform, main);
            report.otherBindingsPassed = true;
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/RecoveredUI/FreeLuckyGame.prefab");
            var authored = asset.GetComponent<RecoveredFreeLuckyGame>();
            var authoredImage = authored.Icon.GetComponent<Image>();
            var game = (GameObject)PrefabUtility.InstantiatePrefab(asset, preview);
            game.transform.SetParent(host.transform, false);
            var lucky = game.GetComponent<RecoveredFreeLuckyGame>();
            lucky.Bind(null, null, null, null, host.transform, false, 0);
            var layer = lucky.Icon.GetComponentInParent<Canvas>(true);
            Require(layer != null && layer != main, "Reward has no dedicated authored Canvas.");
            report.canvasOrder = layer.sortingOrder;
            report.popupSeparate = !lucky.Popup.transform.IsChildOf(layer.transform);
            Require(report.popupSeparate && layer.overrideSorting && layer.sortingOrder == main.sortingOrder + 50, "Incorrect reward hierarchy or order.");
            Require(layer.worldCamera == camera && layer.sortingLayerID == main.sortingLayerID, "Reward Canvas did not inherit main camera/layer.");
            Canvas.ForceUpdateCanvases();
            var source = new Vector3(0, 0, 0);
            lucky.Begin(source, null);
            Canvas.ForceUpdateCanvases();
            var icon = lucky.Icon; var image = icon.GetComponent<Image>();
            report.sourcePreserved = Vector3.Distance(icon.position, source) < .001f;
            report.spritePreserved = image.sprite == authoredImage.sprite;
            report.sizePreserved = (icon.sizeDelta - authored.Icon.sizeDelta).sqrMagnitude < .0001f;
            report.scalePreserved = (icon.localScale - authored.Icon.localScale).sqrMagnitude < .0001f;
            report.nonInteractive = !image.raycastTarget;
            Require(report.sourcePreserved && report.spritePreserved && report.sizePreserved && report.scalePreserved && report.nonInteractive, "Reward appearance, position, or input changed.");

            // Exercise the real FreeSymbolItem renderer/material/alpha, with its K sprite.
            var symbolObject = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/RecoveredSymbols/FreeSymbolItem.prefab"), preview);
            var view = symbolObject.GetComponent<RecoveredSymbolView>();
            var reelAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/RecoveredSymbols/FreeMiniReel.prefab");
            var authoredGroup = reelAsset.GetComponentInChildren<SortingGroup>(true);
            Require(authoredGroup != null, "FreeMiniReel sorting group missing.");
            var reelGroup = symbolObject.AddComponent<SortingGroup>();
            reelGroup.sortingLayerID = authoredGroup.sortingLayerID; reelGroup.sortingOrder = authoredGroup.sortingOrder;
            report.reelGroupOrder = reelGroup.sortingOrder;
            var catalog = AssetDatabase.LoadAssetAtPath<RecoveredSymbolCatalog>("Assets/Resources/MainSkin/Assets/RecoveredSymbols/OriginalSymbolCatalog.asset");
            view.Show(catalog, 4, RecoveredSlotType.Free, false, false);
            var symbol = view.Symbol; var cover = view.Cover;
            report.symbolOrder = symbol.sortingOrder; report.coverOrder = cover.sortingOrder;
            var corners = new Vector3[4]; icon.GetWorldCorners(corners);
            float cashWidth = Vector3.Distance(corners[0], corners[3]), cashHeight = Vector3.Distance(corners[0], corners[1]);
            symbol.transform.position = source;
            symbol.transform.localScale = new Vector3(cashWidth * .68f / symbol.sprite.bounds.size.x, cashHeight * .88f / symbol.sprite.bounds.size.y, 1);
            cover.gameObject.SetActive(true); cover.transform.position = source;
            cover.transform.localScale = new Vector3(cashWidth / cover.sprite.bounds.size.x, cashHeight / cover.sprite.bounds.size.y, 1);

            symbol.enabled = false; cover.enabled = false;
            var reference = Capture(camera, target, label + "-reference.png");
            layer.overrideSorting = false; symbol.enabled = true;
            var symbolOnly = Capture(camera, target, label + "-before-symbol.png");
            symbol.enabled = false; cover.enabled = true;
            var coverOnly = Capture(camera, target, label + "-before-cover.png");
            symbol.enabled = true;
            var before = Capture(camera, target, label + "-before.png");
            layer.overrideSorting = true;
            var after = Capture(camera, target, label + "-after.png");

            var popup = Node("Normal Popup Mask", preview, typeof(RectTransform), typeof(Canvas), typeof(CanvasRenderer), typeof(Image));
            popup.transform.SetParent(host.transform, false);
            var popupRect = (RectTransform)popup.transform; popupRect.anchorMin = Vector2.zero; popupRect.anchorMax = Vector2.one;
            popupRect.offsetMin = Vector2.zero; popupRect.offsetMax = Vector2.zero;
            var popupCanvas = popup.GetComponent<Canvas>(); popupCanvas.overrideSorting = true; popupCanvas.sortingOrder = 300;
            popupCanvas.sortingLayerID = main.sortingLayerID; popupCanvas.worldCamera = camera;
            popup.GetComponent<Image>().color = new Color(0, 0, 0, .65f); popup.GetComponent<Image>().raycastTarget = false;
            var underPopup = Capture(camera, target, label + "-popup.png");
            for (int i = 0; i < reference.Length; i++)
            {
                var p = reference[i];
                if (p.g < 120 || p.g < p.r + 30 || p.g < p.b + 40) continue;
                report.testedPixels++;
                report.beforeError += Difference(p, before[i]); report.symbolError += Difference(p, symbolOnly[i]);
                report.coverError += Difference(p, coverOnly[i]); report.afterError += Difference(p, after[i]);
                report.popupError += Difference(after[i], underPopup[i]);
            }
            Require(report.testedPixels > 100, "No green reward pixels rendered.");
            report.beforeError /= report.testedPixels; report.symbolError /= report.testedPixels;
            report.coverError /= report.testedPixels; report.afterError /= report.testedPixels; report.popupError /= report.testedPixels;
            File.WriteAllText(Output + "/" + label + "-report.json", JsonUtility.ToJson(report, true));
            Require(report.symbolError > 5, "Fixture failed to reproduce the K overlay.");
            Require(report.coverError > 15 && report.beforeError > 15, "Fixture failed to reproduce the dark cover.");
            Require(report.afterError < 1 && report.afterError < report.beforeError * .05f, "Reward is still covered by the reel.");
            Require(report.popupError > 20, "Reward escaped the popup mask.");
            report.passed = true;
            File.WriteAllText(Output + "/" + label + "-report.json", JsonUtility.ToJson(report, true));
            return report;
        }
        finally
        {
            // Destroying Lucky cancels its real RecoveredReelWait before any update can award money.
            EditorSceneManager.ClosePreviewScene(preview); target.Release(); Object.DestroyImmediate(target);
        }
    }

    static void VerifyOtherBindings(Scene scene, Transform mainWindow, Canvas main)
    {
        var ads = new NoAds();
        var wheelObject = Instance("FreeWheelGame", scene, mainWindow);
        var wheel = wheelObject.GetComponent<RecoveredFreeWheelGame>();
        wheel.Bind(null, null, ads, null, mainWindow, null, false, 0);
        VerifyBoundIcon(wheel.Icon, wheel.Window.transform, main, "Wheel");
        var slotObject = Instance("FreeSlotGame", scene, mainWindow);
        var slot = slotObject.GetComponent<RecoveredFreeSlotGame>();
        slot.Bind(null, null, ads, null, mainWindow, false, 0);
        VerifyBoundIcon(slot.Icon, slot.Window.transform, main, "Slot");
        var treasureObject = Instance("FreeTreasureGame", scene, mainWindow);
        var treasure = treasureObject.GetComponent<RecoveredFreeTreasureGame>();
        treasure.Bind(null, null, ads, null, mainWindow, false, 0, (RectTransform)mainWindow);
        VerifyBoundIcon(treasure.Icon, treasure.Window.transform, main, "Treasure");
        Object.DestroyImmediate(wheelObject); Object.DestroyImmediate(slotObject); Object.DestroyImmediate(treasureObject);
    }
    static GameObject Instance(string name, Scene scene, Transform parent)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/RecoveredUI/" + name + ".prefab"), scene);
        go.transform.SetParent(parent, false); return go;
    }
    static void VerifyBoundIcon(RectTransform icon, Transform window, Canvas main, string name)
    {
        var layer = icon.GetComponentInParent<Canvas>(true);
        Require(layer != null && layer != main && !window.IsChildOf(layer.transform), name + " reward/window hierarchy mismatch.");
        Require(layer.overrideSorting && layer.sortingOrder == main.sortingOrder + 50 && layer.worldCamera == main.worldCamera && layer.sortingLayerID == main.sortingLayerID, name + " bound sorting/camera mismatch.");
        foreach (var graphic in icon.GetComponentsInChildren<Graphic>(true)) Require(!graphic.raycastTarget, name + " icon intercepts input.");
    }

    static GameObject Node(string name, Scene scene, params Type[] components)
    { var go = new GameObject(name, components) { layer = 5, hideFlags = HideFlags.HideAndDontSave }; SceneManager.MoveGameObjectToScene(go, scene); return go; }
    static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    static float Difference(Color32 a, Color32 b) => (Math.Abs(a.r - b.r) + Math.Abs(a.g - b.g) + Math.Abs(a.b - b.b)) / 3f;
    static Color32[] Capture(Camera camera, RenderTexture target, string name)
    {
        var previous = RenderTexture.active; Texture2D capture = null;
        try
        {
            Canvas.ForceUpdateCanvases();
            if (GraphicsSettings.currentRenderPipeline != null) RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
            else camera.Render();
            RenderTexture.active = target; capture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            capture.ReadPixels(new Rect(0, 0, Width, Height), 0, 0); capture.Apply();
            File.WriteAllBytes(Output + "/" + name, capture.EncodeToPNG()); return capture.GetPixels32();
        }
        finally { RenderTexture.active = previous; if (capture != null) Object.DestroyImmediate(capture); }
    }
}
