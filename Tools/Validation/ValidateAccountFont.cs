using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Temporary, isolated Editor capture of the real account prefab. Fonts, materials and
// textures are copied in memory before any input can add glyphs. No SDK/store calls.
public static class ValidateAccountFont
{
    const string PrefabPath = "Assets/Resources/RecoveredUI/AccountWindow.prefab";
    const string Output = "Artifacts/AccountFont";
    [Serializable] public sealed class GlyphReport
    {
        public string character, hash, error;
        public bool present;
        public uint glyph;
        public int atlas, x, y, width, height, inkPixels, alphaSum;
    }
    [Serializable] public sealed class CharacterReport
    {
        public string character, font;
        public bool visible, hasGlyph, materialMatchesAtlas;
        public int atlas;
    }
    [Serializable] public sealed class InputReport
    {
        public string input, meshText, font;
        public bool exactInput;
        public List<CharacterReport> characters = new List<CharacterReport>();
    }
    [Serializable] public sealed class FrameReport
    {
        public string image;
        public int height;
        public InputReport name, email;
    }
    [Serializable] public sealed class Report
    {
        public string phase, font, family, atlasFormat, atlasAssetPath, error;
        public bool passed, materialMatchesAtlas, asciiInitiallyComplete, dynamicAdded, dynamicPreservedJD;
        public int atlasWidth, atlasHeight;
        public List<int> missingAscii = new List<int>();
        public List<GlyphReport> initialGlyphs = new List<GlyphReport>();
        public List<GlyphReport> renderedGlyphs = new List<GlyphReport>();
        public List<FrameReport> frames = new List<FrameReport>();
    }

    [MenuItem("Tools/Validation/Account Font/Before")]
    public static void Before() { Run("before", false); }
    [MenuItem("Tools/Validation/Account Font/After")]
    public static void After() { Run("after", true); }

    static void Run(string phase, bool strict)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Run account font captures outside Play mode.");
        Directory.CreateDirectory(Output);
        var report = new Report { phase = phase };
        var disposable = new List<Object>();
        TMP_FontAsset font = null;
        try
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null) throw new InvalidOperationException("AccountWindow prefab is missing.");
            TMP_FontAsset source = prefab.transform.Find("Content/Name").GetComponent<TMP_InputField>().textComponent.font;
            report.font = source.name; report.family = source.faceInfo.familyName;
            report.atlasFormat = source.atlasTextures[0].format.ToString();
            report.atlasAssetPath = AssetDatabase.GetAssetPath(source.atlasTextures[0]);
            report.atlasWidth = source.atlasTextures[0].width; report.atlasHeight = source.atlasTextures[0].height;
            report.materialMatchesAtlas = source.material.mainTexture == source.atlasTextures[0];
            font = CloneFont(source, disposable);
            for (int c = 32; c <= 126; c++) if (!font.HasCharacter(c)) report.missingAscii.Add(c);
            report.asciiInitiallyComplete = report.missingAscii.Count == 0;
            report.initialGlyphs.Add(InspectGlyph(font, 'j')); report.initialGlyphs.Add(InspectGlyph(font, 'd'));

            if (strict)
            {
                Require(report.asciiInitiallyComplete, "Printable ASCII was not pre-baked: " + string.Join(",", report.missingAscii));
                Require(report.materialMatchesAtlas, "The authored shared material points to a different atlas.");
                Require(font.atlasTextures[0].format == TextureFormat.Alpha8, "Dynamic font atlas must be a native Alpha8 texture.");
                Require(report.initialGlyphs[0].inkPixels > 0 && report.initialGlyphs[1].inkPixels > 0, "j/d glyph rectangles have no SDF ink.");
                string beforeJ = report.initialGlyphs[0].hash, beforeD = report.initialGlyphs[1].hash;
                string missing;
                report.dynamicAdded = font.TryAddCharacters("\u00e9", out missing) && font.HasCharacter('\u00e9');
                report.dynamicPreservedJD = beforeJ == InspectGlyph(font, 'j').hash && beforeD == InspectGlyph(font, 'd').hash;
                Require(report.dynamicAdded, "Dynamic addition of e-acute failed: " + missing);
                Require(report.dynamicPreservedJD, "Adding e-acute changed existing j/d glyph alpha.");
            }

            foreach (int height in new[] { 1920, 2400 }) Render(prefab, source, font, phase, height, strict, report);
            report.renderedGlyphs.Add(InspectGlyph(font, 'j')); report.renderedGlyphs.Add(InspectGlyph(font, 'd'));
            report.passed = true;
            Debug.Log(strict ? "ACCOUNT_FONT_AFTER_PASS" : "ACCOUNT_FONT_BEFORE_CAPTURED");
        }
        catch (Exception error) { report.error = error.ToString(); Debug.LogException(error); }
        finally
        {
            File.WriteAllText(Output + "/" + phase + "-report.json", JsonUtility.ToJson(report, true));
            // Include any dynamically allocated extra atlases in temporary cleanup.
            if (font != null) foreach (Texture2D atlas in font.atlasTextures)
                if (atlas != null && !AssetDatabase.Contains(atlas) && !disposable.Contains(atlas)) disposable.Add(atlas);
            for (int i = disposable.Count - 1; i >= 0; i--) if (disposable[i] != null) Object.DestroyImmediate(disposable[i]);
        }
    }

    static TMP_FontAsset CloneFont(TMP_FontAsset source, List<Object> disposable)
    {
        var result = ScriptableObject.CreateInstance<TMP_FontAsset>();
        EditorUtility.CopySerialized(source, result);
        result.name = source.name + " Account validation"; result.hideFlags = HideFlags.HideAndDontSave;
        disposable.Add(result);
        var atlases = new Texture2D[source.atlasTextures.Length];
        for (int i = 0; i < atlases.Length; i++)
        {
            if (source.atlasTextures[i] == null) continue;
            atlases[i] = Object.Instantiate(source.atlasTextures[i]); atlases[i].hideFlags = HideFlags.HideAndDontSave;
            disposable.Add(atlases[i]);
        }
        result.atlasTextures = atlases;
        var material = Object.Instantiate(source.material); material.hideFlags = HideFlags.HideAndDontSave;
        material.mainTexture = atlases[0]; result.material = material; disposable.Add(material);
        result.ReadFontAssetDefinition();
        return result;
    }

    static void Render(GameObject prefab, TMP_FontAsset source, TMP_FontAsset font, string phase, int height, bool strict, Report report)
    {
        Scene scene = EditorSceneManager.NewPreviewScene();
        RenderTexture target = null;
        try
        {
            target = new RenderTexture(1080, height, 24, RenderTextureFormat.ARGB32); target.Create();
            var cameraObject = new GameObject("Account font preview camera", typeof(Camera));
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            var camera = cameraObject.GetComponent<Camera>(); camera.enabled = false;
            camera.transform.position = new Vector3(0, 0, -1000); camera.orthographic = true; camera.orthographicSize = height * .5f;
            camera.nearClipPlane = .1f; camera.farClipPlane = 2000; camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.1f, .035f, .025f, 1); camera.targetTexture = target;
            camera.scene = scene; camera.overrideSceneCullingMask = EditorSceneManager.GetSceneCullingMask(scene); camera.cullingMask = 1 << 5;

            var host = new GameObject("Account font preview canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            SceneManager.MoveGameObjectToScene(host, scene); host.layer = 5;
            Canvas canvas = host.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera;
            canvas.planeDistance = 1000; canvas.scaleFactor = 1;
            CanvasScaler scaler = host.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920); scaler.matchWidthOrHeight = 0;
            // Both RTs have reference width 1080, hence exact runtime width-match factor is 1.
            // The unrelated Editor Game View must not override the preview RT dimensions.
            scaler.enabled = false; ((RectTransform)host.transform).sizeDelta = new Vector2(1080, height);
            Canvas.ForceUpdateCanvases();

            var window = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            window.transform.SetParent(host.transform, false);
            var rootRect = (RectTransform)window.transform; rootRect.anchorMin = Vector2.zero; rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = rootRect.offsetMax = Vector2.zero;
            Canvas popupCanvas = window.GetComponent<Canvas>(); popupCanvas.worldCamera = camera;
            // The prefab is a nested WorldSpace canvas at runtime; do not globally render other canvases.
            if (popupCanvas.renderMode == RenderMode.ScreenSpaceOverlay) popupCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            foreach (TMP_Text text in window.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.font != source) throw new InvalidOperationException("Unexpected account font: " + text.name);
                text.font = font; text.fontSharedMaterial = font.material;
            }
            var name = window.transform.Find("Content/Name").GetComponent<TMP_InputField>();
            var email = window.transform.Find("Content/Email").GetComponent<TMP_InputField>();
            window.SetActive(true);
            CaptureInput(name, email, "jdjd", "jd@example.test", "jd", phase, height, strict, font, camera, target, report);
            CaptureInput(name, email, "abcdefghijklmnopqrstuvwxyz", "ABCDEFGHIJKLMNOPQRSTUVWXYZ", "alphabet", phase, height, strict, font, camera, target, report);
            CaptureInput(name, email, "$20.00  $500  $1,234.56", "j d J D . @ + - _", "money-symbols", phase, height, strict, font, camera, target, report);
            CaptureInput(name, email, Ascii(32, 79), Ascii(80, 126), "ascii", phase, height, strict, font, camera, target, report);
            if (strict) CaptureInput(name, email, "Jos\u00e9 jd", "jd.\u00e9@example.test", "dynamic-unicode", phase, height, true, font, camera, target, report);
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(scene);
            if (target != null) { target.Release(); Object.DestroyImmediate(target); }
        }
    }

    static string Ascii(int first, int last)
    {
        char[] characters = new char[last - first + 1];
        for (int i = 0; i < characters.Length; i++) characters[i] = (char)(first + i);
        return new string(characters);
    }

    static void CaptureInput(TMP_InputField name, TMP_InputField email, string first, string second, string sample,
        string phase, int height, bool strict, TMP_FontAsset font, Camera camera, RenderTexture target, Report report)
    {
        name.SetTextWithoutNotify(first); email.SetTextWithoutNotify(second);
        name.ForceLabelUpdate(); email.ForceLabelUpdate(); Canvas.ForceUpdateCanvases();
        name.textComponent.ForceMeshUpdate(); email.textComponent.ForceMeshUpdate(); Canvas.ForceUpdateCanvases();
        string path = Output + "/" + phase + "-1080x" + height + "-" + sample + ".png";
        Capture(camera, target, path);
        var frame = new FrameReport { image = path, height = height,
            name = InspectInput(name, first), email = InspectInput(email, second) };
        report.frames.Add(frame);
        if (strict) { CheckInput(frame.name, first); CheckInput(frame.email, second); }
    }

    static InputReport InspectInput(TMP_InputField input, string expected)
    {
        TMP_Text text = input.textComponent;
        var result = new InputReport { input = input.text, meshText = text.text, font = text.font.name, exactInput = input.text == expected };
        for (int i = 0; i < text.textInfo.characterCount; i++)
        {
            TMP_CharacterInfo info = text.textInfo.characterInfo[i];
            TMP_FontAsset actual = info.fontAsset;
            TMP_Character mapped;
            bool has = actual != null && actual.characterLookupTable.TryGetValue(info.character, out mapped);
            int index = info.textElement != null && info.textElement.glyph != null ? (int)info.textElement.glyph.atlasIndex : -1;
            bool matching = actual != null && index >= 0 && index < actual.atlasTextures.Length && info.material != null
                && info.material.mainTexture == actual.atlasTextures[index];
            result.characters.Add(new CharacterReport { character = info.character.ToString(), font = actual != null ? actual.name : "",
                visible = info.isVisible, hasGlyph = has, atlas = index, materialMatchesAtlas = matching });
        }
        return result;
    }

    static void CheckInput(InputReport input, string expected)
    {
        Require(input.exactInput, "The input text changed unexpectedly.");
        foreach (char character in expected)
        {
            if (char.IsWhiteSpace(character)) continue;
            bool found = false;
            foreach (CharacterReport glyph in input.characters)
                if (glyph.character.Length == 1 && glyph.character[0] == character && glyph.visible && glyph.hasGlyph && glyph.materialMatchesAtlas) { found = true; break; }
            Require(found, "Input glyph did not render with its matching atlas: " + character);
        }
    }

    static GlyphReport InspectGlyph(TMP_FontAsset font, char character)
    {
        var result = new GlyphReport { character = character.ToString(), present = font.HasCharacter(character) };
        if (!result.present) return result;
        Texture2D pixels = null;
        try
        {
            var glyph = font.characterLookupTable[character].glyph; var rect = glyph.glyphRect;
            result.glyph = glyph.index; result.atlas = (int)glyph.atlasIndex;
            result.x = rect.x; result.y = rect.y; result.width = rect.width; result.height = rect.height;
            pixels = ReadTexture(font.atlasTextures[result.atlas]);
            Color32[] colors = pixels.GetPixels32(); uint hash = 2166136261;
            for (int y = rect.y; y < rect.y + rect.height; y++) for (int x = rect.x; x < rect.x + rect.width; x++)
            {
                byte alpha = colors[y * pixels.width + x].a;
                if (alpha >= 128) result.inkPixels++;
                result.alphaSum += alpha; hash = unchecked((hash ^ alpha) * 16777619);
            }
            result.hash = hash.ToString("X8");
        }
        catch (Exception error) { result.error = error.ToString(); }
        finally { if (pixels != null) Object.DestroyImmediate(pixels); }
        return result;
    }

    static Texture2D ReadTexture(Texture2D texture)
    {
        RenderTexture target = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        RenderTexture previous = RenderTexture.active;
        try
        {
            Graphics.Blit(texture, target); RenderTexture.active = target;
            var result = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false, true);
            result.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0); result.Apply(); return result;
        }
        finally { RenderTexture.active = previous; RenderTexture.ReleaseTemporary(target); }
    }

    static void Capture(Camera camera, RenderTexture target, string path)
    {
        RenderTexture previous = RenderTexture.active; Texture2D capture = null;
        try
        {
            if (GraphicsSettings.currentRenderPipeline != null) RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
            else camera.Render();
            RenderTexture.active = target; capture = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            capture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); capture.Apply(); File.WriteAllBytes(path, capture.EncodeToPNG());
        }
        finally { RenderTexture.active = previous; if (capture != null) Object.DestroyImmediate(capture); }
    }

    static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
