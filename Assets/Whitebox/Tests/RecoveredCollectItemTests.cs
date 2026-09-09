using System.Collections;
using System.IO;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public sealed class RecoveredCollectItemTests
{
    [Test]
    public void LookupReturnsFirstRecordIncludingReceivedZeroAndNegativeCountsWithoutSaving()
    {
        var data = new PlayerData(); int saves = 0;
        var player = new RecoveredPlayerProgress(new RecoveredGameplayRules(new GoldenDragonAutoGenConfig()), () => saves++, data);
        Assert.IsNull(player.GetPlayerCollectData(7));
        var first = new PlayerCollectData { id = 7, count = 0, isRecieve = true };
        data.PlayerCollectDatas.Add(first); data.PlayerCollectDatas.Add(new PlayerCollectData { id = 7, count = 99 });
        Assert.AreSame(first, player.GetPlayerCollectData(7));
        first.count = -8; Assert.AreSame(first, player.GetPlayerCollectData(7));
        Assert.IsNull(player.GetPlayerCollectData(1));
        data.PlayerCollectDatas = null; Assert.IsNull(player.GetPlayerCollectData(7)); Assert.AreEqual(0, saves);
    }

    [UnityTest]
    public IEnumerator RealPrefabReusesAllFifteenSpritesAndPreservesNativeCountPresentation()
    {
        string key = RecoveredPlayerStore.OriginalKey; bool had = PlayerPrefs.HasKey(key); string saved = PlayerPrefs.GetString(key); PlayerPrefs.DeleteKey(key);
        var random = Random.state; Scene scene = default; AsyncOperation unload = null;
        var items = new RecoveredCollectItem[3]; Camera camera = null; RenderTexture texture = null;
        try {
            yield return SceneManager.LoadSceneAsync("GameEntry", LoadSceneMode.Additive); scene = SceneManager.GetSceneByName("GameEntry");
            GameEntry entry = null; foreach (var root in scene.GetRootGameObjects()) { var value = root.GetComponentInChildren<GameEntry>(); if (value != null) entry = value; }
            Assert.IsNotNull(entry); yield return null;
            var prefab = Resources.Load<RecoveredCollectItem>("RecoveredUI/CollectItem"); Assert.IsNotNull(prefab);
            for (int i = 0; i < items.Length; i++) {
                items[i] = Object.Instantiate(prefab, entry.transform, false);
                ((RectTransform)items[i].transform).anchoredPosition = new Vector2((i - 1) * 310, 0);
                Assert.AreEqual(new Vector2(279, 328), ((RectTransform)items[i].transform).sizeDelta);
                Assert.AreEqual(0, items[i].GetComponentsInChildren<UnityEngine.UI.Button>(true).Length);
            }
            var data = new PlayerData(); int saves = 0;
            var player = new RecoveredPlayerProgress(entry.Rules, () => saves++, data);
            var infos = entry.Rules.GetCollectInfos(); Assert.AreEqual(15, infos.Count);
            foreach (var info in infos) {
                items[0].PointText.text = "previous count";
                items[0].Initialize(info, player);
                Assert.IsNotNull(items[0].Icon.sprite);
                StringAssert.StartsWith("t_icon_" + info.id.ToString("D2") + "_", items[0].Icon.sprite.name);
                Assert.AreEqual(items[0].Icon.sprite.rect.size, items[0].Icon.rectTransform.sizeDelta);
                Assert.AreEqual(new Color(37f / 255, 37f / 255, 37f / 255, 1), items[0].Icon.color);
                Assert.IsFalse(items[0].RedPoint.activeSelf); Assert.AreEqual("previous count", items[0].PointText.text);
            }
            var selected = infos[0];
            items[0].Initialize(selected, player);
            var record = new PlayerCollectData { id = selected.id, count = 1 };
            data.PlayerCollectDatas.Add(record); data.PlayerCollectDatas.Add(new PlayerCollectData { id = selected.id, count = 999 });
            items[1].Initialize(selected, player); Assert.AreEqual("1", items[1].PointText.text);
            Assert.AreEqual(Color.white, items[1].Icon.color); Assert.IsTrue(items[1].RedPoint.activeSelf);
            record.isRecieve = true;
            foreach (int count in new[] { 0, -8, int.MaxValue, 5 }) {
                record.count = count; items[2].Initialize(selected, player);
                Assert.AreEqual(string.Format("{0}", count), items[2].PointText.text);
                Assert.AreEqual(Color.white, items[2].Icon.color); Assert.IsTrue(items[2].RedPoint.activeSelf);
            }
            Assert.AreEqual(0, saves); camera = entry.GetComponent<Canvas>().worldCamera;
            texture = new RenderTexture(1080, 1920, 24); camera.targetTexture = texture;
            yield return null; Capture(camera, texture);
            data.PlayerCollectDatas = null; items[2].Initialize(selected, player);
            Assert.IsFalse(items[2].RedPoint.activeSelf); Assert.AreEqual("5", items[2].PointText.text);
            Assert.AreEqual(37f / 255, items[2].Icon.color.r); Assert.AreEqual(0, saves);
        }
        finally {
            if (camera != null) camera.targetTexture = null; if (texture != null) Object.Destroy(texture);
            foreach (var item in items) if (item != null) Object.Destroy(item.gameObject);
            if (scene.IsValid()) unload = SceneManager.UnloadSceneAsync(scene); Random.state = random;
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
            File.WriteAllBytes(Path.Combine(Application.dataPath, "../Artifacts/current-collect-items.png"), image.EncodeToPNG());
        }
        finally { RenderTexture.active = previous; Object.Destroy(image); }
    }
}
