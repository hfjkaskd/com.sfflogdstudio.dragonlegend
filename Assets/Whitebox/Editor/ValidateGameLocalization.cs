using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using DragonLegend.Whitebox;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DragonLegend.Whitebox.EditorTools
{
    // Isolated presentation validation: no GameEntry, SDK, user store or Play Mode.
    public static class ValidateGameLocalization
    {
        private const string Output = "Artifacts/Localization";
        [Serializable] private sealed class TextReport
        {
            public string path, text, font;
            public bool overflow;
            public float fontSize, boxWidth, boxHeight;
            public List<string> missing = new List<string>();
        }
        [Serializable] private sealed class Frame
        {
            public string image, error;
            public List<TextReport> texts = new List<TextReport>();
            public List<ClaimLayoutReport> claims = new List<ClaimLayoutReport>();
        }
        [Serializable] private sealed class ClaimLayoutReport
        {
            public string path, text, button;
            public int lineCount;
            public bool active, interactable, overflow, expectsMultiplier, multiplierOnSameLine, boundsInsideButton, passed;
            public Rect renderedBounds, buttonBounds;
            public List<string> errors = new List<string>();
        }
        [Serializable] private sealed class Report
        {
            public bool completed;
            public string error;
            public int bindingCount;
            public List<string> bindingErrors = new List<string>();
            public List<Frame> frames = new List<Frame>();
        }

        [MenuItem("Dragon Legend/Localization/Capture English and Portuguese")]
        public static void Capture()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Use localization captures outside Play Mode.");
            Directory.CreateDirectory(Output);
            int originalLanguage = GameLocalization.CurrentLanguage;
            var report = new Report();
            try
            {
                AuditBindings(report);
                for (int language = 0; language <= 1; language++)
                {
                    GameLocalization.SetLanguage(language);
                    CapturePrefab("MainUtility/Settings", "settings", language, 0, report);
                    for (int page = 1; page <= 3; page++) CapturePrefab("MainUtility/Help", "help-" + page, language, page, report);
                    CapturePrefab("GuideText", "guide", language, 0, report);
                    CapturePrefab("BonusRewardPopup", "bonus-reward", language, 0, report);
                    CapturePrefab("AccountWindow", "account", language, 0, report);
                }
                int failures = report.bindingErrors.Count;
                foreach (var frame in report.frames)
                {
                    if (!string.IsNullOrEmpty(frame.error)) failures++;
                    foreach (var text in frame.texts) if (text.missing.Count != 0) failures++;
                }
                if (failures != 0) throw new InvalidOperationException("Localization capture failed with "
                    + failures + " binding, frame or glyph errors. See " + Output + "/capture-report.json");
                report.completed = true;
            }
            catch (Exception error) { report.completed = false; report.error = error.ToString(); throw; }
            finally
            {
                GameLocalization.SetLanguage(originalLanguage);
                File.WriteAllText(Output + "/capture-report.json", JsonUtility.ToJson(report, true));
            }
            Debug.Log("GAME_LOCALIZATION_CAPTURE_COMPLETE: " + report.frames.Count + " frames, "
                + report.bindingCount + " bindings; " + Output + "/capture-report.json");
        }

        [MenuItem("Dragon Legend/Localization/Capture Claim Button Layouts")]
        public static void CaptureClaimButtonLayouts()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Use localization captures outside Play Mode.");
            Directory.CreateDirectory(Output);
            int originalLanguage = GameLocalization.CurrentLanguage;
            var report = new Report();
            try
            {
                for (int language = 0; language <= 1; language++)
                {
                    GameLocalization.SetLanguage(language);
                    CapturePrefab("BigWinPopup", "big-win", language, 0, report, true);
                    CapturePrefab("JackpotPopup", "jackpot", language, 0, report, true);
                    CapturePrefab("BonusRewardPopup", "bonus-reward", language, 0, report, true);
                    CapturePrefab("DailyTasks/Item", "daily-task-item", language, 0, report, true);
                }
                int failures = 0;
                foreach (var frame in report.frames)
                {
                    if (!string.IsNullOrEmpty(frame.error)) failures++;
                    if (frame.claims.Count != 1) failures++;
                    foreach (var claim in frame.claims) if (!claim.passed) failures++;
                    // Other controls keep their authored overflow behavior. This report
                    // asserts layout only for the specific claim label on each prefab.
                    foreach (var text in frame.texts)
                        foreach (var claim in frame.claims)
                            if (text.path == claim.path && text.missing.Count != 0) failures++;
                }
                if (failures != 0) throw new InvalidOperationException("Claim button capture failed with "
                    + failures + " frame, claim layout or glyph errors. See " + Output + "/claim-layout-report.json");
                report.completed = true;
            }
            catch (Exception error) { report.completed = false; report.error = error.ToString(); throw; }
            finally
            {
                GameLocalization.SetLanguage(originalLanguage);
                File.WriteAllText(Output + "/claim-layout-report.json", JsonUtility.ToJson(report, true));
            }
            Debug.Log("CLAIM_BUTTON_LAYOUT_CAPTURE_COMPLETE: " + report.frames.Count + " frames; " + Output + "/claim-layout-report.json");
        }

        private static void AuditBindings(Report report)
        {
            var catalog = JsonUtility.FromJson<GameTextCatalog>(File.ReadAllText("Assets/Resources/Localization/GameText.json"));
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var entry in catalog.entries) keys.Add(entry.key);
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Resources/RecoveredUI", "Assets/Resources/Whitebox", "Assets/Resources/MainSkin" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                foreach (var binding in prefab.GetComponentsInChildren<LocalizedGameText>(true))
                {
                    report.bindingCount++;
                    if (!keys.Contains(binding.Key)) report.bindingErrors.Add(path + ": Missing catalog key " + binding.Key);
                    var properties = new SerializedObject(binding);
                    if (properties.FindProperty("tmpText").objectReferenceValue == null && properties.FindProperty("legacyText").objectReferenceValue == null)
                        report.bindingErrors.Add(path + ": Missing text target for " + binding.Key);
                }
            }
        }

        private static void CapturePrefab(string resource, string sample, int language, int page, Report report, bool claimLayout = false)
        {
            string filename = Output + "/" + (language == 1 ? "pt-BR-" : "en-US-") + sample + ".png";
            var frame = new Frame { image = filename };
            report.frames.Add(frame);
            Scene scene = EditorSceneManager.NewPreviewScene();
            var disposable = new List<Object>();
            var fonts = new Dictionary<TMP_FontAsset, TMP_FontAsset>();
            RenderTexture target = null;
            try
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/RecoveredUI/" + resource + ".prefab");
                if (prefab == null) throw new InvalidDataException("Missing capture prefab: " + resource);
                bool taskCard = resource == "DailyTasks/Item";
                Vector2 authoredSize = ((RectTransform)prefab.transform).rect.size;
                int width = taskCard ? Mathf.CeilToInt(authoredSize.x + 192) : 1080;
                int height = taskCard ? Mathf.CeilToInt(authoredSize.y + 192) : resource == "GuideText" ? 420 : 1920;
                target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32); target.Create();
                var cameraObject = new GameObject("Localization preview camera", typeof(Camera), typeof(UniversalAdditionalCameraData));
                SceneManager.MoveGameObjectToScene(cameraObject, scene);
                var camera = cameraObject.GetComponent<Camera>(); camera.enabled = false;
                camera.transform.position = new Vector3(0, 0, -1000);
                camera.orthographic = true; camera.orthographicSize = height * .5f;
                camera.nearClipPlane = .1f; camera.farClipPlane = 2000;
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.12f, .025f, .02f, 1);
                camera.targetTexture = target; camera.scene = scene;
                camera.overrideSceneCullingMask = EditorSceneManager.GetSceneCullingMask(scene); camera.cullingMask = 1 << 5;
                var urp = cameraObject.GetComponent<UniversalAdditionalCameraData>();
                urp.renderPostProcessing = false; urp.requiresDepthTexture = false; urp.requiresColorTexture = false;

                var host = new GameObject("Localization preview canvas", typeof(RectTransform), typeof(Canvas));
                SceneManager.MoveGameObjectToScene(host, scene); host.layer = 5;
                var canvas = host.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera; canvas.planeDistance = 1000; canvas.scaleFactor = 1;
                ((RectTransform)host.transform).sizeDelta = new Vector2(width, height);
                Canvas.ForceUpdateCanvases();
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                instance.transform.SetParent(host.transform, false);
                foreach (var behaviour in instance.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (behaviour == null) continue;
                    var script = MonoScript.FromMonoBehaviour(behaviour);
                    if (script != null && AssetDatabase.GetAssetPath(script).StartsWith("Assets/Whitebox/Runtime/", StringComparison.Ordinal)) behaviour.enabled = false;
                }
                var rect = (RectTransform)instance.transform;
                rect.localScale = Vector3.one;
                if (resource != "GuideText" && !taskCard) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
                else
                {
                    rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f); rect.anchoredPosition = Vector2.zero;
                    if (taskCard) rect.sizeDelta = authoredSize;
                }
                var content = instance.transform.Find("Content"); if (content != null) content.localScale = Vector3.one;
                foreach (var popupCanvas in instance.GetComponentsInChildren<Canvas>(true))
                {
                    popupCanvas.worldCamera = camera;
                    if (popupCanvas.renderMode == RenderMode.ScreenSpaceOverlay) popupCanvas.renderMode = RenderMode.ScreenSpaceCamera;
                }
                CloneFonts(instance, fonts, disposable);
                instance.SetActive(true);
                if (page > 0)
                    for (int i = 1; i <= 3; i++) instance.transform.Find("Content/" + i).gameObject.SetActive(page == i);
                foreach (var binding in instance.GetComponentsInChildren<LocalizedGameText>(true)) binding.Refresh();
                foreach (var layout in instance.GetComponentsInChildren<LocalizedTextLayout>(true)) layout.Refresh();
                PrepareDynamicPreviews(instance, language);
                TMP_Text claimLabel = null;
                Button claimButton = null;
                if (claimLayout) PrepareClaimPreview(instance, language, out claimLabel, out claimButton);
                Canvas.ForceUpdateCanvases();
                foreach (var text in instance.GetComponentsInChildren<TMP_Text>(false)) text.ForceMeshUpdate(true, true);
                Canvas.ForceUpdateCanvases();
                SaveImage(camera, target, filename);
                foreach (var text in instance.GetComponentsInChildren<TMP_Text>(false)) InspectText(text, frame);
                if (claimLayout) InspectClaimLayout(claimLabel, claimButton, !taskCard, frame);
            }
            catch (Exception error) { frame.error = error.ToString(); Debug.LogException(error); }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
                if (target != null) { target.Release(); Object.DestroyImmediate(target); }
                for (int i = disposable.Count - 1; i >= 0; i--) if (disposable[i] != null) Object.DestroyImmediate(disposable[i]);
            }
        }

        private static void PrepareClaimPreview(GameObject instance, int language, out TMP_Text label, out Button button)
        {
            // Populate presentation fields only; Show/Bind would start gameplay,
            // ads, persistence and animation coroutines and are intentionally unused.
            var bigWin = instance.GetComponent<RecoveredBigWinPopup>();
            var jackpot = instance.GetComponent<RecoveredJackpotPopup>();
            var bonus = instance.GetComponent<RecoveredBonusRewardPopup>();
            var task = instance.GetComponent<RecoveredDailyTaskItem>();
            Text amount = null;
            TMP_Text plain = null;
            if (bigWin != null)
            { label = bigWin.AdvertisedText; button = bigWin.ClaimButton; amount = bigWin.RewardText; plain = bigWin.PlainText; }
            else if (jackpot != null)
            { label = jackpot.AdvertisedText; button = jackpot.ClaimButton; amount = jackpot.RewardText; plain = jackpot.PlainText; }
            else if (bonus != null)
            { label = bonus.AdvertisedText; button = bonus.ClaimButton; amount = bonus.RewardText; plain = bonus.PlainText; }
            else if (task != null)
            {
                button = task.ClaimButton;
                label = button.GetComponentInChildren<TMP_Text>(true);
                var fields = new SerializedObject(task);
                SetText(fields, "reward", RecoveredCurrency.Format(600f, language));
                SetText(fields, "description", GameLocalization.Format("Spin the Slots {0}/{1} times.", language, 10, 10));
                SetText(fields, "progress", "10/10");
                ((Image)fields.FindProperty("fill").objectReferenceValue).fillAmount = 1;
                ((GameObject)fields.FindProperty("finished").objectReferenceValue).SetActive(false);
                ((GameObject)fields.FindProperty("black").objectReferenceValue).SetActive(false);
            }
            else throw new InvalidDataException("Missing supported claim presenter: " + instance.name);

            if (label == null || button == null) throw new InvalidDataException("Missing claim text or button: " + instance.name);
            button.gameObject.SetActive(true); button.enabled = true; button.interactable = true;
            label.gameObject.SetActive(true); label.enabled = true; label.maxVisibleCharacters = int.MaxValue;
            label.text = task != null ? GameLocalization.Text("CLAIM", language)
                : GameLocalization.Format("<sprite name=\"tc_btn_bofang\">CLAIMx{0}", language, 2);
            if (amount != null) amount.text = RecoveredCurrency.Format(10010f, language);
            if (plain != null)
            {
                plain.gameObject.SetActive(true);
                plain.text = GameLocalization.Format("Only {0}", language, RecoveredCurrency.Format(5005f, language));
            }
        }

        private static void InspectClaimLayout(TMP_Text text, Button button, bool expectsMultiplier, Frame frame)
        {
            var buttonRect = (RectTransform)button.transform;
            var result = new ClaimLayoutReport {
                path = AnimationUtility.CalculateTransformPath(text.transform, null), text = text.text,
                button = AnimationUtility.CalculateTransformPath(button.transform, null),
                lineCount = text.textInfo.lineCount, overflow = text.isTextOverflowing,
                active = text.isActiveAndEnabled && button.isActiveAndEnabled,
                interactable = button.IsInteractable(), expectsMultiplier = expectsMultiplier,
                buttonBounds = buttonRect.rect
            };
            frame.claims.Add(result);
            var min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            var max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            int visibleCount = 0, xIndex = -1, twoIndex = -1;
            for (int i = 0; i < text.textInfo.characterCount; i++)
            {
                var character = text.textInfo.characterInfo[i];
                if (character.character == 'x') xIndex = i;
                if (character.character == '2') twoIndex = i;
                if (!character.isVisible) continue;
                visibleCount++;
                // Include both glyphs and the embedded ad sprite in button-local
                // coordinates, so transforms cannot hide a text overflow.
                IncludeClaimCorner(character.bottomLeft, text.transform, buttonRect, ref min, ref max);
                IncludeClaimCorner(character.topLeft, text.transform, buttonRect, ref min, ref max);
                IncludeClaimCorner(character.topRight, text.transform, buttonRect, ref min, ref max);
                IncludeClaimCorner(character.bottomRight, text.transform, buttonRect, ref min, ref max);
            }
            result.multiplierOnSameLine = !expectsMultiplier || (xIndex >= 0 && twoIndex == xIndex + 1
                && text.textInfo.characterInfo[xIndex].isVisible && text.textInfo.characterInfo[twoIndex].isVisible
                && text.textInfo.characterInfo[xIndex].lineNumber == text.textInfo.characterInfo[twoIndex].lineNumber);
            if (visibleCount != 0)
            {
                result.renderedBounds = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
                const float tolerance = .5f;
                result.boundsInsideButton = min.x >= result.buttonBounds.xMin - tolerance
                    && min.y >= result.buttonBounds.yMin - tolerance && max.x <= result.buttonBounds.xMax + tolerance
                    && max.y <= result.buttonBounds.yMax + tolerance;
            }
            if (!result.active || !result.interactable) result.errors.Add("Claim button must be visible and interactable.");
            if (visibleCount == 0) result.errors.Add("Claim label has no visible glyphs.");
            if (result.lineCount != 1) result.errors.Add("Claim label must remain on one line.");
            if (result.overflow) result.errors.Add("Claim label reports TMP overflow.");
            if (!result.multiplierOnSameLine) result.errors.Add("The complete x2 multiplier must be visible on the same line.");
            if (!result.boundsInsideButton) result.errors.Add("Rendered claim glyph or sprite bounds exceed the button rectangle.");
            result.passed = result.errors.Count == 0;
        }

        private static void IncludeClaimCorner(Vector3 corner, Transform text, RectTransform button, ref Vector2 min, ref Vector2 max)
        {
            Vector2 point = button.InverseTransformPoint(text.TransformPoint(corner));
            min = Vector2.Min(min, point); max = Vector2.Max(max, point);
        }

        private static void PrepareDynamicPreviews(GameObject instance, int language)
        {
            // Only presentation samples: the same translated templates as the runtime,
            // without invoking gameplay coroutines, account login or persistence.
            var guide = instance.GetComponent<RecoveredGuideText>();
            if (guide != null)
            {
                var fields = new SerializedObject(guide);
                guide.Label.text = GameLocalization.Text(fields.FindProperty("firstSpinText").stringValue, language);
                guide.Label.maxVisibleCharacters = int.MaxValue;
            }
            var account = instance.GetComponent<RecoveredAccountWindow>();
            if (account != null)
            {
                var fields = new SerializedObject(account);
                SetText(fields, "detail", GameLocalization.Format(fields.FindProperty("promptFormat").stringValue, language, "PayPal"));
                SetText(fields, "accountPlaceholder", GameLocalization.Format(fields.FindProperty("accountPlaceholderFormat").stringValue, language, "PayPal"));
            }
            var reward = instance.GetComponent<RecoveredBonusRewardPopup>();
            if (reward != null)
            {
                var fields = new SerializedObject(reward);
                SetText(fields, "rewardText", RecoveredCurrency.Format(10010f, language));
                SetText(fields, "advertisedText", GameLocalization.Format("<sprite name=\"tc_btn_bofang\">CLAIMx{0}", language, 2));
                SetText(fields, "plainText", GameLocalization.Format("Only {0}", language, RecoveredCurrency.Format(10010f, language)));
                foreach (var tip in instance.GetComponentsInChildren<RecoveredCashOutTip>(true))
                {
                    var tipFields = new SerializedObject(tip);
                    SetText(tipFields, "tips", GameLocalization.Format(tipFields.FindProperty("remainingFormat").stringValue,
                        language, RecoveredCurrency.Format(39990f, language), RecoveredCurrency.Format(50000, language, 0)));
                    SetText(tipFields, "progressText", RecoveredCurrency.Format(10010f, language) + "/" + RecoveredCurrency.Format(50000, language, 0));
                }
            }
        }

        private static void SetText(SerializedObject fields, string field, string value)
        {
            var target = fields.FindProperty(field).objectReferenceValue;
            if (target is TMP_Text text) { text.text = value; text.maxVisibleCharacters = int.MaxValue; }
            else if (target is Text legacy) legacy.text = value;
        }

        private static void CloneFonts(GameObject root, Dictionary<TMP_FontAsset, TMP_FontAsset> fonts, List<Object> disposable)
        {
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.font == null) continue;
                var original = text.font;
                var font = CloneFont(original, fonts, disposable);
                var material = Object.Instantiate(text.fontSharedMaterial != null ? text.fontSharedMaterial : original.material);
                material.hideFlags = HideFlags.HideAndDontSave;
                material.mainTexture = font.atlasTextures[0]; disposable.Add(material);
                text.font = font; text.fontSharedMaterial = material;
            }
        }

        private static TMP_FontAsset CloneFont(TMP_FontAsset source, Dictionary<TMP_FontAsset, TMP_FontAsset> fonts, List<Object> disposable)
        {
            if (fonts.TryGetValue(source, out var clone)) return clone;
            if (source.atlasTextures == null || source.atlasTextures.Length == 0 || source.material == null)
                throw new InvalidDataException("Font is missing its atlas or material: " + source.name);
            clone = ScriptableObject.CreateInstance<TMP_FontAsset>(); EditorUtility.CopySerialized(source, clone);
            clone.hideFlags = HideFlags.HideAndDontSave; clone.name = source.name + " Preview";
            clone.atlasPopulationMode = AtlasPopulationMode.Static;
            disposable.Add(clone); fonts.Add(source, clone);
            var atlases = new Texture2D[source.atlasTextures.Length];
            for (int i = 0; i < atlases.Length; i++)
            {
                if (source.atlasTextures[i] == null) continue;
                atlases[i] = Object.Instantiate(source.atlasTextures[i]); atlases[i].hideFlags = HideFlags.HideAndDontSave; disposable.Add(atlases[i]);
            }
            clone.atlasTextures = atlases;
            clone.material = Object.Instantiate(source.material); clone.material.mainTexture = atlases[0]; disposable.Add(clone.material);
            clone.fallbackFontAssetTable = new List<TMP_FontAsset>();
            if (source.fallbackFontAssetTable != null)
                foreach (var fallback in source.fallbackFontAssetTable)
                    if (fallback != null) clone.fallbackFontAssetTable.Add(CloneFont(fallback, fonts, disposable));
            clone.ReadFontAssetDefinition();
            return clone;
        }

        private static void InspectText(TMP_Text text, Frame frame)
        {
            var rect = text.rectTransform.rect;
            var item = new TextReport { path = AnimationUtility.CalculateTransformPath(text.transform, null), text = text.text,
                font = text.font != null ? text.font.name : "", overflow = text.isTextOverflowing,
                fontSize = text.fontSize, boxWidth = rect.width, boxHeight = rect.height };
            var missing = new HashSet<char>();
            foreach (char character in Regex.Replace(text.text, "<[^>]+>", string.Empty))
                if (!char.IsWhiteSpace(character) && (text.font == null || !text.font.HasCharacter(character, true, false))) missing.Add(character);
            foreach (char character in missing) item.missing.Add("U+" + ((int)character).ToString("X4") + " " + character);
            frame.texts.Add(item);
        }

        private static void SaveImage(Camera camera, RenderTexture target, string path)
        {
            var previous = RenderTexture.active; Texture2D capture = null;
            var renderErrors = new List<string>();
            Application.LogCallback onLog = (message, stack, type) => {
                if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                    renderErrors.Add(message + "\n" + stack);
            };
            Application.logMessageReceived += onLog;
            try
            {
                if (GraphicsSettings.currentRenderPipeline != null)
                    RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                else camera.Render();
                if (renderErrors.Count != 0) throw new InvalidOperationException("Rendering " + path + " failed:\n" + string.Join("\n", renderErrors));
                RenderTexture.active = target;
                capture = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
                capture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); capture.Apply();
                var pixels = capture.GetPixels32();
                int different = 0;
                for (int i = 97; i < pixels.Length; i += 97)
                    if (pixels[i].r != pixels[0].r || pixels[i].g != pixels[0].g || pixels[i].b != pixels[0].b) different++;
                if (different < 8) throw new InvalidOperationException("Rendered frame is blank or uniform: " + path);
                File.WriteAllBytes(path, capture.EncodeToPNG());
            }
            finally
            {
                Application.logMessageReceived -= onLog;
                RenderTexture.active = previous;
                if (capture != null) Object.DestroyImmediate(capture);
            }
        }
    }
}
