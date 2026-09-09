using System.Collections;
using System.IO;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class RecoveredCollectListTests
{
    [UnityTest]
    public IEnumerator NativeListUsesBoundaryVisibilityFifoDirtyRefreshAndActualScrollRectDrag()
    {
        string key = RecoveredPlayerStore.OriginalKey; bool had = PlayerPrefs.HasKey(key); string saved = PlayerPrefs.GetString(key); PlayerPrefs.DeleteKey(key);
        var random = Random.state; float timeScale = Time.timeScale;
        Scene scene = default; AsyncOperation unload = null; RecoveredCollectList list = null;
        Camera camera = null; RenderTexture texture = null;
        try {
            Time.timeScale = 1;
            yield return SceneManager.LoadSceneAsync("GameEntry", LoadSceneMode.Additive); scene = SceneManager.GetSceneByName("GameEntry");
            GameEntry entry = null; foreach (var root in scene.GetRootGameObjects()) { var value = root.GetComponentInChildren<GameEntry>(); if (value != null) entry = value; }
            Assert.IsNotNull(entry); yield return null; Time.timeScale = 0;
            var data = new PlayerData(); int saves = 0;
            var player = new RecoveredPlayerProgress(entry.Rules, () => saves++, data);
            data.PlayerCollectDatas.Add(new PlayerCollectData { id = entry.Rules.GetCollectInfos()[0].id, count = 1 });
            list = Object.Instantiate(Resources.Load<RecoveredCollectList>("RecoveredUI/CollectList"), entry.transform, false);
            var scroll = list.Scroll;
            Assert.IsFalse(scroll.horizontal); Assert.IsTrue(scroll.vertical); Assert.IsTrue(scroll.inertia);
            Assert.AreEqual(ScrollRect.MovementType.Elastic, scroll.movementType);
            Assert.AreEqual(.1f, scroll.elasticity); Assert.AreEqual(.135f, scroll.decelerationRate);
            Assert.AreEqual(1, scroll.scrollSensitivity); Assert.IsNull(scroll.verticalScrollbar);
            Assert.IsNotNull(scroll.viewport.GetComponent<RectMask2D>());
            // Disable only the ScrollRect integration while probing exact virtualization boundaries.
            scroll.enabled = false; list.Initialize(entry.Rules, player, new Vector2(930, 720));
            Assert.AreEqual(0, list.CreatedCount); Assert.AreEqual(15, list.TotalCount);
            Assert.AreEqual(new Vector2(930, 1800), scroll.content.sizeDelta);
            yield return null;
            Assert.AreEqual(9, list.CreatedCount, "Native inclusive edge also shows row 2 touching the viewport bottom.");
            for (int i = 0; i < 15; i++) Assert.AreEqual(i < 9, list.ItemAt(i) != null);
            var originalThird = list.ItemAt(3);
            for (int i = 0; i < 9; i++) {
                Assert.AreEqual(new Vector3(155 + (i % 3) * 310, -180 - (i / 3) * 360, 0), list.ItemAt(i).transform.localPosition);
                Assert.AreEqual(Vector3.one, list.ItemAt(i).transform.localScale);
            }
            data.PlayerCollectDatas[0].count = 7;
            yield return null; Assert.AreEqual("1", list.ItemAt(0).PointText.text, "No unsolicited refresh on unchanged frames.");
            list.RefreshData(); yield return null; Assert.AreEqual("7", list.ItemAt(0).PointText.text);
            scroll.content.localPosition = new Vector3(0, 720, 0); yield return null;
            Assert.AreEqual(12, list.CreatedCount); Assert.AreEqual(0, list.CachedCount);
            for (int i = 0; i < 15; i++) Assert.AreEqual(i >= 3, list.ItemAt(i) != null);
            scroll.content.localPosition = new Vector3(0, 721, 0); yield return null;
            Assert.IsNotNull(list.ItemAt(3), "Squared movement <=2 does not trigger a native refresh.");
            scroll.content.localPosition = new Vector3(0, 721.5f, 0); yield return null;
            Assert.AreEqual(3, list.CachedCount); Assert.IsNull(list.ItemAt(3));
            Assert.IsTrue(originalThird.gameObject.activeSelf); Assert.AreEqual(Vector3.one * 9999, originalThird.transform.localPosition);
            scroll.content.localPosition = Vector3.zero; yield return null;
            Assert.AreSame(originalThird, list.ItemAt(0), "FIFO cache uses the oldest returned item first.");
            Assert.AreEqual("7", list.ItemAt(0).PointText.text); Assert.AreEqual(12, list.CreatedCount);
            Assert.AreEqual(3, list.CachedCount); Assert.AreEqual(0, saves);
            camera = entry.GetComponent<Canvas>().worldCamera; texture = new RenderTexture(1080, 1920, 24); camera.targetTexture = texture;
            Canvas.ForceUpdateCanvases(); scroll.enabled = true;
            var start = RectTransformUtility.WorldToScreenPoint(camera, list.transform.position);
            var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left,
                position = start, pressPosition = start,
                pointerPressRaycast = new RaycastResult { module = entry.GetComponent<GraphicRaycaster>() } };
            scroll.OnInitializePotentialDrag(pointer); scroll.OnBeginDrag(pointer);
            pointer.position = start + new Vector2(0, 250); scroll.OnDrag(pointer);
            Assert.Greater(scroll.content.anchoredPosition.y, 0);
            yield return null; yield return null;
            float offset = scroll.content.anchoredPosition.y;
            list.RefreshData(); yield return null; Assert.AreEqual(offset, scroll.content.anchoredPosition.y);
            Capture(camera, texture); scroll.OnEndDrag(pointer);
            Assert.AreEqual(12, list.CreatedCount); Assert.AreEqual(0, saves);
        }
        finally {
            if (camera != null) camera.targetTexture = null; if (texture != null) Object.Destroy(texture); if (list != null) Object.Destroy(list.gameObject);
            if (scene.IsValid()) unload = SceneManager.UnloadSceneAsync(scene); Time.timeScale = timeScale; Random.state = random;
            if (had) PlayerPrefs.SetString(key, saved); else PlayerPrefs.DeleteKey(key);
        }
        if (unload != null) yield return unload;
    }
    private static void Capture(Camera camera, RenderTexture target)
    {
        var previous = RenderTexture.active; var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
        try {
            Canvas.ForceUpdateCanvases(); RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target }); RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); image.Apply();
            File.WriteAllBytes(Path.Combine(Application.dataPath, "../Artifacts/current-collect-list.png"), image.EncodeToPNG());
        }
        finally { RenderTexture.active = previous; Object.Destroy(image); }
    }
}
