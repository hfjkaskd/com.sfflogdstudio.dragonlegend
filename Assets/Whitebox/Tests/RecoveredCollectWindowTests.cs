using System.Collections;
using System.IO;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class RecoveredCollectWindowTests
{
    [Test]
    public void SafeAreaPreservesNativeLinearScalingAndInvalidInputLeavesLayout()
    {
        var root = new GameObject("Root scaler", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        var child = new GameObject("Adapt", typeof(RectTransform)); child.transform.SetParent(root.transform, false);
        try {
            var scaler = root.GetComponent<CanvasScaler>(); scaler.referenceResolution = new Vector2(1080, 1920);
            var adapt = child.AddComponent<RecoveredSafeArea>(); var rect = (RectTransform)child.transform;
            foreach (float match in new[] {0f, .5f, 1f}) {
                scaler.matchWidthOrHeight = match;
                adapt.Apply(1200, 2400, new Rect(40, 100, 1100, 2200));
                float factor = match == 0 ? .9f : match == 1 ? .8f : .85f;
                Assert.Less(Vector2.Distance(new Vector2(40, 100) * factor, rect.offsetMin), .001f);
                Assert.Less(Vector2.Distance(new Vector2(-60, -100) * factor, rect.offsetMax), .001f);
                Assert.AreEqual(Vector2.zero, rect.anchorMin); Assert.AreEqual(Vector2.one, rect.anchorMax);
            }
            var before = rect.offsetMin; adapt.Apply(1, 2400, new Rect(0, 0, 100, 100)); Assert.AreEqual(before, rect.offsetMin);
            adapt.Apply(1200, 2400, new Rect(0, 0, 1, 100)); Assert.AreEqual(before, rect.offsetMin);
            Object.DestroyImmediate(child); child = new GameObject("Detached Adapt", typeof(RectTransform));
            adapt = child.AddComponent<RecoveredSafeArea>(); adapt.BindRootScaler(scaler);
            adapt.Apply(1200, 2400, new Rect(40, 100, 1100, 2200));
            Assert.Less(Vector2.Distance(new Vector2(32, 80), ((RectTransform)child.transform).offsetMin), .001f);
        }
        finally { Object.DestroyImmediate(child); Object.DestroyImmediate(root); }
    }

    [UnityTest]
    public IEnumerator SourceWindowShowsRawProgressReopensSameListAndKeepsNativeABranch()
    {
        string key = RecoveredPlayerStore.OriginalKey; bool had = PlayerPrefs.HasKey(key); string saved = PlayerPrefs.GetString(key); PlayerPrefs.DeleteKey(key);
        float scale = Time.timeScale, delta = Time.captureDeltaTime; var random = Random.state;
        Scene scene = default; AsyncOperation unload = null; RecoveredCollectWindow window = null;
        Camera camera = null; RenderTexture target = null;
        try {
            Time.timeScale = 1; Time.captureDeltaTime = .025f;
            yield return SceneManager.LoadSceneAsync("GameEntry", LoadSceneMode.Additive); scene = SceneManager.GetSceneByName("GameEntry");
            GameEntry entry = null; foreach (var root in scene.GetRootGameObjects()) { var value = root.GetComponentInChildren<GameEntry>(); if (value != null) entry = value; }
            Assert.IsNotNull(entry); yield return null;
            camera = entry.GetComponent<Canvas>().worldCamera; target = new RenderTexture(1080, 1920, 24); camera.targetTexture = target;
            Canvas.ForceUpdateCanvases(); yield return null;
            var data = new PlayerData(); int saves = 0;
            var player = new RecoveredPlayerProgress(entry.Rules, () => saves++, data);
            int id = entry.Rules.GetCollectInfos()[0].id;
            data.PlayerCollectDatas.Add(new PlayerCollectData {id = id, count = 1});
            data.PlayerCollectDatas.Add(new PlayerCollectData {id = id, count = 0, isRecieve = true});
            data.PlayerCollectDatas.Add(new PlayerCollectData {id = -55, count = -3});
            window = Object.Instantiate(Resources.Load<RecoveredCollectWindow>("RecoveredUI/CollectWindow"), entry.transform, false);
            float width = window.Fill.rect.width;
            window.Bind(player, entry.Rules, entry.transform, false, 0);
            Assert.IsTrue(window.Green.activeSelf); Assert.IsFalse(window.Gold.activeSelf);
            Assert.AreEqual(entry.Rules.GetCollectReward(0), window.RewardText.text);
            Assert.AreEqual(new Vector4(22,0,23,0), window.Fill.GetComponent<Image>().sprite.border);
            int clicks = 0; window.SoundRequested += sound => { Assert.AreEqual("click", sound); clicks++; };
            window.Show(); Assert.AreEqual("3/15", window.ProgressText.text);
            Assert.AreEqual(width * 3 / 15, window.Fill.sizeDelta.x);
            Assert.AreEqual(Vector3.zero, window.Content.localScale);
            for (int i = 0; i < 6; i++) yield return null;
            Assert.Greater(window.Content.localScale.x, 1, "Original OutBack overshoots during entry.");
            Time.timeScale = 0; var paused = window.Content.localScale;
            yield return null; yield return null; Assert.AreEqual(paused, window.Content.localScale);
            Time.timeScale = 1; for (int i = 0; i < 9; i++) yield return null;
            Assert.AreEqual(Vector3.one, window.Content.localScale);
            var list = window.List; Assert.AreEqual(15, list.TotalCount);
            Assert.AreEqual(1176, ((RectTransform)list.transform).rect.height, .01f, "Initialize the native fixed viewport after establishing portrait output.");
            list.Scroll.enabled = false; list.Scroll.content.anchoredPosition = new Vector2(0, 150); yield return null;
            window.CloseButton.onClick.Invoke(); Assert.AreEqual(1, clicks); Assert.IsTrue(window.gameObject.activeSelf);
            for (int i = 0; i < 14; i++) yield return null;
            Assert.IsFalse(window.gameObject.activeSelf);
            for (int i = 0; i < 15; i++) data.PlayerCollectDatas.Add(new PlayerCollectData {id = id, count = i});
            data.PlayerCollectDatas[0].count = 7;
            window.Show(); Assert.AreSame(list, window.List);
            Assert.AreEqual(150, list.Scroll.content.anchoredPosition.y);
            Assert.AreEqual("18/15", window.ProgressText.text); Assert.AreEqual(width * 18 / 15, window.Fill.sizeDelta.x);
            for (int i = 0; i < 14; i++) yield return null;
            Assert.AreEqual("7", list.ItemAt(0).PointText.text); Assert.AreEqual(0, saves);
            Capture(camera, target, "current-collect-window.png");
            Object.Destroy(window.gameObject); yield return null;
            window = Object.Instantiate(Resources.Load<RecoveredCollectWindow>("RecoveredUI/CollectWindow"), entry.transform, false);
            window.Bind(player, entry.Rules, entry.transform, true, 4); window.Show();
            Assert.IsTrue(window.Gold.activeSelf); Assert.IsFalse(window.Green.activeSelf);
            Assert.AreEqual("18/15", window.ProgressText.text); Assert.AreEqual(0, window.Fill.sizeDelta.x);
            for (int i = 0; i < 14; i++) yield return null;
            Capture(camera, target, "current-collect-window-a.png"); Assert.AreEqual(0, saves);
        }
        finally {
            if (camera != null) camera.targetTexture = null; if (target != null) Object.Destroy(target); if (window != null) Object.Destroy(window.gameObject);
            if (scene.IsValid()) unload = SceneManager.UnloadSceneAsync(scene);
            Time.timeScale = scale; Time.captureDeltaTime = delta; Random.state = random;
            if (had) PlayerPrefs.SetString(key, saved); else PlayerPrefs.DeleteKey(key);
        }
        if (unload != null) yield return unload;
    }
    private static void Capture(Camera camera, RenderTexture target, string filename)
    {
        var previous = RenderTexture.active; var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
        try {
            Canvas.ForceUpdateCanvases(); RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest {destination = target}); RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); image.Apply();
            File.WriteAllBytes(Path.Combine(Application.dataPath, "../Artifacts/" + filename), image.EncodeToPNG());
        }
        finally { RenderTexture.active = previous; Object.Destroy(image); }
    }
}
