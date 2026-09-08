using System.Collections;
using System.IO;
using DragonLegend.Whitebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class RecoveredCoinRewardTextTests
{
    [UnityTest]
    public IEnumerator BitmapRewardTextPreservesMetricsAndNativeShowScaleSequence()
    {
        var text = Object.Instantiate(Resources.Load<RecoveredCoinRewardText>("RecoveredUI/CoinRewardText"));
        var cameraHost = new GameObject("Reward text camera", typeof(Camera)); var camera = cameraHost.GetComponent<Camera>();
        camera.transform.position = new Vector3(0, 0, -10); camera.orthographic = true; camera.orthographicSize = 2;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black; camera.cullingMask = 32;
        var target = new RenderTexture(512, 512, 24); camera.targetTexture = target; text.GetComponent<Canvas>().worldCamera = camera;
        var previous = RenderTexture.active; Texture2D capture = null;
        float scale = Time.timeScale, delta = Time.captureDeltaTime;
        try {
            Time.timeScale = 1; Time.captureDeltaTime = .05f;
            var label = text.Label; Assert.AreEqual(Vector2.zero, label.rectTransform.sizeDelta);
            Assert.AreEqual(new Vector2(0, 4.6f), label.rectTransform.anchoredPosition);
            Assert.AreEqual(0, label.fontSize); Assert.IsFalse(label.font.dynamic);
            Assert.AreEqual(TextAnchor.MiddleCenter, label.alignment);
            Assert.AreEqual(HorizontalWrapMode.Overflow, label.horizontalOverflow);
            Assert.AreEqual(VerticalWrapMode.Truncate, label.verticalOverflow);
            Assert.IsTrue(label.font.GetCharacterInfo('0', out var zero, 0)); Assert.AreEqual(43, zero.advance);
            Assert.IsTrue(label.font.GetCharacterInfo('.', out var dot, 0)); Assert.AreEqual(21, dot.advance);
            Assert.IsNotNull(label.font.material.mainTexture);
            int completed = 0; text.PresentationFinished += () => completed++;
            text.Begin(1234, 0); float started = Time.time;
            Assert.IsFalse(label.gameObject.activeSelf);
            Time.timeScale = 0; for (int i = 0; i < 10; i++) yield return null;
            Assert.IsFalse(label.gameObject.activeSelf); Assert.AreEqual(0, completed); Time.timeScale = 1;
            for (int i = 0; i < 10 && !label.gameObject.activeSelf; i++) yield return null;
            Assert.IsTrue(label.gameObject.activeSelf); Assert.GreaterOrEqual(Time.time - started, .199f);
            Assert.AreEqual("$12.34", label.text); Assert.AreEqual(0, completed);
            bool sawPeak = label.transform.localScale.x >= 1.19f;
            Canvas.ForceUpdateCanvases();
            RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
            RenderTexture.active = target; capture = new Texture2D(512, 512, TextureFormat.RGB24, false);
            capture.ReadPixels(new Rect(0, 0, 512, 512), 0, 0); capture.Apply();
            int visible = 0; foreach (var p in capture.GetPixels32()) if (p.r > 80 || p.g > 80 || p.b > 80) visible++;
            Assert.Greater(visible, 1000, "Static bitmap font with size zero must render its authored glyphs.");
            File.WriteAllBytes(Path.GetFullPath(Path.Combine(Application.dataPath, "../Artifacts/current-coin-reward-text.png")), capture.EncodeToPNG());
            for (int i = 0; i < 15 && completed == 0; i++) {
                yield return null; sawPeak |= label.transform.localScale.x >= 1.19f;
            }
            Assert.AreEqual(1, completed); Assert.IsTrue(sawPeak); Assert.GreaterOrEqual(Time.time - started, .599f);
            Assert.AreEqual(1, label.transform.localScale.x, .00001f); Assert.IsTrue(label.gameObject.activeSelf);
            Assert.IsNull(text.Error);
            text.Begin(1234, 1); for (int i = 0; i < 10 && !label.gameObject.activeSelf; i++) yield return null;
            Assert.AreEqual("R$12,34", label.text);
            text.Hide(); for (int i = 0; i < 15; i++) yield return null;
            Assert.AreEqual(1, completed); Assert.IsFalse(label.gameObject.activeSelf);
            text.Begin(400, 0); text.gameObject.SetActive(false); text.gameObject.SetActive(true);
            for (int i = 0; i < 15; i++) yield return null;
            Assert.AreEqual(1, completed); Assert.IsFalse(label.gameObject.activeSelf);
        } finally {
            Time.timeScale = scale; Time.captureDeltaTime = delta; RenderTexture.active = previous;
            Object.Destroy(text.gameObject); Object.Destroy(cameraHost); if (capture != null) Object.Destroy(capture);
            target.Release(); Object.Destroy(target);
        }
    }
}
