using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public sealed class RecoveredFreeTreasureGameTests
{
    [UnityTest]
    public IEnumerator EntrySavesBeforeShowRejectsStoredIdAndFliesIntoRealFlipAndClaim()
    {
        string key = RecoveredPlayerStore.OriginalKey; bool had = PlayerPrefs.HasKey(key); string saved = PlayerPrefs.GetString(key); PlayerPrefs.DeleteKey(key);
        float scale = Time.timeScale, delta = Time.captureDeltaTime; var random = Random.state;
        Scene scene = default; AsyncOperation unload = null; RecoveredFreeTreasureGame game = null; Camera camera = null; RenderTexture texture = null;
        try
        {
            Time.timeScale = 1; Time.captureDeltaTime = .025f;
            yield return SceneManager.LoadSceneAsync("GameEntry", LoadSceneMode.Additive); scene = SceneManager.GetSceneByName("GameEntry");
            var entry = Entry(scene); yield return null; Assert.IsNotNull(entry.CashFlight);
            game = Object.Instantiate(Resources.Load<RecoveredFreeTreasureGame>("RecoveredUI/FreeTreasureGame"), entry.transform, false);
            game.Bind(entry.PlayerProgress, entry.Rules, entry.Ads, entry.CashFlight, entry.transform, false, 0);
            camera = entry.GetComponent<Canvas>().worldCamera; texture = new RenderTexture(1080, 1920, 24); camera.targetTexture = texture;
            // Force one or more native rejection draws, and independently verify the remaining RNG stream.
            Random.InitState(97); entry.PlayerStore.Data.RandomIndex = entry.Rules.RandomCollectIndex();
            Random.InitState(97); int expectedId, draws = 0;
            do { expectedId = entry.Rules.RandomCollectIndex(); draws++; } while (expectedId == entry.PlayerProgress.RandomIndex);
            Assert.Greater(draws, 1); int expectedNext = Random.Range(0, 1000000);
            int shown = 0, callbacks = 0, taskBefore = TaskCount(entry, 4); float paid = 0, balance = entry.PlayerProgress.GreenCount;
            game.Window.SoundRequested += name => {
                if (name != "jump") return;
                shown++; var persisted = JsonUtility.FromJson<PlayerData>(PlayerPrefs.GetString(key));
                Assert.AreEqual(1, persisted.PlayerCollectDatas.Count);
                Assert.AreEqual(expectedId, persisted.PlayerCollectDatas[0].id);
                Assert.AreEqual(1, persisted.PlayerCollectDatas[0].count);
            };
            var source = entry.transform.TransformPoint(new Vector3(-280, -200, 0));
            Random.InitState(97); game.Begin(source, value => { callbacks++; paid = value; Assert.AreEqual(balance, entry.PlayerProgress.GreenCount); });
            Assert.AreEqual(expectedNext, Random.Range(0, 1000000)); Assert.AreEqual(expectedId, game.SelectedId);
            Assert.AreEqual(1, shown); Assert.IsTrue(game.Window.gameObject.activeSelf);
            Assert.IsFalse(game.Window.Card.gameObject.activeSelf); Assert.IsTrue(game.Icon.gameObject.activeSelf);
            Assert.AreEqual("CardPrefab", game.Icon.name); Assert.AreEqual(new Vector2(390, 459), game.Icon.sizeDelta);
            Assert.AreEqual("tc_sc_bak02", game.Icon.GetComponent<UnityEngine.UI.Image>().sprite.name);
            Assert.AreEqual(Vector3.one * .4f, game.Icon.localScale); Assert.AreEqual(source, game.Icon.position);
            var end = game.Window.EndPosition.position;
            var control = (source + end) * .5f + Vector3.up * (Vector3.Distance(source, end) * .3f);
            Time.timeScale = 0; for (int i = 0; i < 3; i++) yield return null;
            Assert.AreEqual(source, game.Icon.position); Assert.AreEqual(Vector3.one * .4f, game.Icon.localScale);
            Time.timeScale = 1;
            for (int frame = 1; frame <= 12; frame++)
            {
                yield return null;
                float t = (1 - Mathf.Cos(Mathf.PI * (frame * .025f / .6f))) * .5f;
                var point = (1 - t) * (1 - t) * source + 2 * (1 - t) * t * control + t * t * end;
                Assert.That(Vector3.Distance(point, game.Icon.position), Is.LessThan(.001f));
                Assert.That(Vector3.Distance(Vector3.one * (.4f + .6f * t), game.Icon.localScale), Is.LessThan(.0001f));
                Assert.IsFalse(game.Window.Card.gameObject.activeSelf);
            }
            Capture(camera, texture);
            for (int i = 0; i < 15 && game.Icon.gameObject.activeSelf; i++) yield return null;
            Assert.IsFalse(game.Icon.gameObject.activeSelf); Assert.IsTrue(game.Window.Card.IsFlipped);
            Assert.AreEqual(Vector3.zero, game.Icon.localPosition); Assert.AreEqual(Vector3.one, game.Icon.localScale);
            for (int i = 0; i < 60; i++) yield return null;
            Assert.IsTrue(game.Window.Content.Find("Title").gameObject.activeSelf);
            Assert.IsTrue(game.Window.CollectTip.gameObject.activeSelf); Assert.AreEqual("1/15", game.Window.CollectTip.ProgressText.text);
            Assert.AreEqual(taskBefore, TaskCount(entry, 4)); Assert.AreEqual(0, callbacks);
            float expectedReward = 0;
            foreach (var info in entry.Rules.GetCollectInfos()) if (info.id == expectedId) expectedReward = info.worth * game.Window.Claim.UnadvertisedMultiplier;
            UnityEngine.UI.Image departed = null; Vector3 departureStart = default, departureEnd = default;
            game.Window.CollectCardDepartureRequested += (position, sprite) => {
                Assert.Greater(entry.CashFlight.ActiveCashCount, 0); Assert.AreEqual(0, callbacks);
                Assert.AreEqual(1, game.Departure.ActiveCardCount); departed = game.Departure.ActiveCardAt(0);
                Assert.AreSame(entry.transform, departed.transform.parent); Assert.AreSame(sprite, departed.sprite);
                Assert.AreEqual(sprite.rect.size, departed.rectTransform.sizeDelta);
                Assert.AreEqual(Vector3.one, departed.transform.localScale); Assert.AreEqual(position, departed.transform.position);
                departureStart = position; departureEnd = game.Departure.Destination.position;
            };
            game.Window.PlainButton.onClick.Invoke(); float deadline = Time.realtimeSinceStartup + 5;
            while (departed == null && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsNotNull(departed); Assert.IsFalse(game.Window.gameObject.activeSelf);
            var destination = game.Departure.Destination;
            Assert.AreEqual(new Vector2(-9, -10), destination.anchoredPosition);
            Assert.AreEqual(new Vector2(.5113022f, .543063f), destination.pivot);
            Assert.IsNotNull(destination.GetComponent<RecoveredRegionAnimator>());
            var departureControl = (departureStart + departureEnd) * .5f + Vector3.up * (Vector3.Distance(departureStart, departureEnd) * .3f);
            Time.timeScale = 0; for (int i = 0; i < 3; i++) yield return null;
            Assert.AreEqual(departureStart, departed.transform.position); Assert.AreEqual(Vector3.one, departed.transform.localScale);
            Time.timeScale = 1;
            for (int frame = 1; frame <= 12; frame++) {
                yield return null; float t = frame * .025f / .6f, eased = (1 - Mathf.Cos(Mathf.PI * t)) * .5f;
                var point = (1 - eased) * (1 - eased) * departureStart + 2 * (1 - eased) * eased * departureControl + eased * eased * departureEnd;
                Assert.That(Vector3.Distance(point, departed.transform.position), Is.LessThan(.001f));
                Assert.That(departed.transform.localScale.x, Is.EqualTo(1 - .7f * (1 - (1 - t) * (1 - t))).Within(.0001f));
            }
            Capture(camera, texture, "current-treasure-departure.png");
            for (int i = 0; i < 15 && game.Departure.ActiveCardCount > 0; i++) yield return null;
            Assert.AreEqual(0, game.Departure.ActiveCardCount); Assert.IsFalse(departed.gameObject.activeSelf);
            Assert.AreEqual(Vector3.one * .3f, departed.transform.localScale);
            Assert.AreEqual(1, game.Departure.CreatedCardCount);
            deadline = Time.realtimeSinceStartup + 5;
            while (callbacks == 0 && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.AreEqual(1, callbacks); Assert.AreEqual(expectedReward, paid); Assert.AreEqual(balance + paid, entry.PlayerProgress.GreenCount);
            Assert.IsFalse(game.IsRunning); Assert.IsFalse(game.Window.IsRunning);
            // Reuse the returned image, then cancel while parented outside its owner as on Main teardown.
            game.Departure.Begin(departureStart, game.Window.CardImage.sprite);
            Assert.AreSame(departed, game.Departure.ActiveCardAt(0)); Assert.AreEqual(Vector3.one, departed.transform.localScale);
            game.Departure.Unbind(); Assert.AreEqual(0, game.Departure.ActiveCardCount);
            for (int i = 0; i < 30; i++) yield return null;
            Assert.IsTrue(departed == null); Assert.AreEqual(1, callbacks);
            Assert.AreEqual(balance + paid, entry.PlayerProgress.GreenCount);
        }
        finally
        {
            if (camera != null) camera.targetTexture = null; if (texture != null) Object.Destroy(texture); if (game != null) Object.Destroy(game.gameObject);
            if (scene.IsValid()) unload = SceneManager.UnloadSceneAsync(scene); Time.timeScale = scale; Time.captureDeltaTime = delta; Random.state = random;
            if (had) PlayerPrefs.SetString(key, saved); else PlayerPrefs.DeleteKey(key);
        }
        if (unload != null) yield return unload;
    }

    [UnityTest]
    public IEnumerator TwoStoppedBallsClaimTreasuresAndResumeTheActualFreeScan()
    {
        string key = RecoveredPlayerStore.OriginalKey; bool had = PlayerPrefs.HasKey(key); string saved = PlayerPrefs.GetString(key); PlayerPrefs.DeleteKey(key);
        float scale = Time.timeScale, delta = Time.captureDeltaTime; var random = Random.state;
        Scene scene = default; AsyncOperation unload = null; RecoveredFreeReels reels = null; RecoveredNpcPresentation npc = null;
        RecoveredFreeTreasureGame game = null; GameObject target = null;
        try
        {
            Time.timeScale = 1; Time.captureDeltaTime = .05f; Random.InitState(371);
            yield return SceneManager.LoadSceneAsync("GameEntry", LoadSceneMode.Additive); scene = SceneManager.GetSceneByName("GameEntry");
            var entry = Entry(scene); float deadline = Time.realtimeSinceStartup + 5;
            while (entry.CashFlight == null && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsNotNull(entry.CashFlight); entry.PlayerProgress.IsFirstFreeReward = false;
            var rules = BranchRules(); var result = new RecoveredFreeSpinResult(rules);
            result.Begin(new[] { 3 }); while (result.IsGenerating) result.Step();
            reels = Object.Instantiate(Resources.Load<RecoveredFreeReels>("RecoveredSymbols/FreeReels"));
            reels.Initialize(Resources.Load<RecoveredSymbolCatalog>("RecoveredSymbols/OriginalSymbolCatalog"), result);
            npc = Object.Instantiate(Resources.Load<RecoveredNpcPresentation>("RecoveredUI/Npc"));
            target = new GameObject("Treasure ball destination"); target.transform.position = new Vector3(0, 3, 0);
            game = Object.Instantiate(Resources.Load<RecoveredFreeTreasureGame>("RecoveredUI/FreeTreasureGame"), entry.transform, false);
            game.Bind(entry.PlayerProgress, entry.Rules, entry.Ads, entry.CashFlight, entry.transform, false, 0);
            entry.PlayerProgress.GameSlotType = RecoveredSlotType.Free;
            float initialBalance = entry.PlayerProgress.GreenCount, initialTotal = entry.PlayerProgress.TotalFreeSpinWin, paid = 0;
            int requests = 0, done = 0;
            Exception npcError = null; npc.Failed += error => npcError = error;
            Action<Vector3, Action<float>> unexpected = (p, c) => Assert.Fail("Seeded fixture must select Treasure.");
            var router = new RecoveredFreeSmallGameRouter(rules, entry.PlayerProgress, unexpected, unexpected, (position, callback) => {
                requests++; Assert.AreEqual(0, reels.Specials.FlightBallCount);

                game.Begin(position, callback);
                Assert.AreEqual(position, game.Icon.position);
            }, unexpected);
            var expected = new List<RecoveredReelView>();
            for (int column = 0; column < 5; column++)
            {
                for (int row = 0; row < 3; row++)
                {
                    var reel = reels.At(column, row); reels.Specials.ApplyStoppedResult(reel, column, row);
                    if (result.GetSymbol(column, row) == 11) expected.Add(reel);
                }
                reels.ColumnAt(column).ShowFreeEffects();
            }
            Assert.AreEqual(2, expected.Count);
            reels.CoinScan.Bind(entry.Rules, entry.PlayerProgress, result, entry.Playfield.BonusCollection, () => 0);
            reels.BallScan.Bind(result, entry.PlayerProgress, npc, target.transform, npc.transform.Find("PlayFire"), () => 0, router.Open);
            reels.BallScan.Completed += () => done++; reels.BallScan.Begin();
            for (int ball = 0; ball < 2; ball++)
            {
                deadline = Time.realtimeSinceStartup + 5;
                while ((!game.Window.gameObject.activeSelf || !game.Window.Content.Find("Title").gameObject.activeSelf) && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.IsNull(reels.BallScan.Error); Assert.IsNull(npcError);
                Assert.AreEqual(ball + 1, requests); Assert.AreEqual(ball, reels.CoinScan.Rewards.Count);
                Assert.AreNotEqual(entry.PlayerProgress.RandomIndex, game.SelectedId);
                Assert.IsTrue(reels.BallScan.IsRunning); Assert.AreEqual(0, done);
                var button = game.Window.PlainButton;
                for (int i = 0; i < 60 && !button.gameObject.activeInHierarchy; i++) yield return null;
                Assert.IsTrue(button.gameObject.activeInHierarchy);
                RecoveredCollectInfo info = null;
                foreach (var item in entry.Rules.GetCollectInfos()) if (item.id == game.SelectedId) info = item;
                Assert.IsNotNull(info);
                float reward = info.worth * game.Window.Claim.UnadvertisedMultiplier;
                button.onClick.Invoke(); deadline = Time.realtimeSinceStartup + 5;
                while (reels.CoinScan.Rewards.Count == ball && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.AreEqual(ball + 1, reels.CoinScan.Rewards.Count);
                Assert.AreEqual(reward, reels.CoinScan.Rewards[expected[ball].gameObject]); paid += reward;
                Assert.AreEqual(initialBalance + paid, entry.PlayerProgress.GreenCount);
                Assert.AreEqual(initialTotal + paid, entry.PlayerProgress.TotalFreeSpinWin);
                Assert.IsFalse(game.IsRunning); Assert.IsFalse(game.Window.IsRunning);
                Assert.IsTrue(reels.Specials.CurrentStoppedBall(expected[ball]).RewardPresentation.IsAnimating);
            }
            for (int i = 0; i < 25 && done == 0; i++) yield return null;
            Assert.AreEqual(1, done); Assert.IsFalse(reels.BallScan.IsRunning); Assert.IsNull(reels.BallScan.Error);
            Assert.AreEqual(RecoveredSlotType.Free, entry.PlayerProgress.GameSlotType);
        }
        finally
        {
            if (reels != null) Object.Destroy(reels.gameObject); if (npc != null) Object.Destroy(npc.gameObject);
            if (game != null) Object.Destroy(game.gameObject); if (target != null) Object.Destroy(target);
            if (scene.IsValid()) unload = SceneManager.UnloadSceneAsync(scene); Time.timeScale = scale; Time.captureDeltaTime = delta; Random.state = random;
            if (had) PlayerPrefs.SetString(key, saved); else PlayerPrefs.DeleteKey(key);
        }
        if (unload != null) yield return unload;
    }
    private static GameEntry Entry(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects()) { var entry = root.GetComponentInChildren<GameEntry>(); if (entry != null) return entry; }
        Assert.Fail("GameEntry scene did not instantiate its entry."); return null;
    }
    private static int TaskCount(GameEntry entry, int id)
    {
        foreach (var task in entry.PlayerStore.Data.PlayerTaskDatas) if (task.id == id) return task.count;
        return 0;
    }
    private static RecoveredGameplayRules BranchRules() => new RecoveredGameplayRules(new GoldenDragonAutoGenConfig { Rrggiomg = new RrggiomgPoro {
        QoinOmoinrKgiitr = new List<int> { 1000000 }, RollOmoinrKgiitr = new List<int> { 0, 0, 1000000 }, RollRipgKgiitr = new List<int> { 1000000 },
        RollGlorg = new List<int> { 0, 0, 0 }, RollKtggl = new List<int> { 0, 0, 0 },
        RollRrgogirg = new List<int> { 1000000, 1000000, 1000000 }, RollLiqki = new List<int> { 0, 0, 0 }
    } });
    private static void Capture(Camera camera, RenderTexture target, string name = "current-free-treasure-entry.png")
    {
        var previous = RenderTexture.active; var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
        try
        {
            Canvas.ForceUpdateCanvases(); RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target }); RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); image.Apply(); File.WriteAllBytes(Path.Combine(Application.dataPath, "../Artifacts/" + name), image.EncodeToPNG());
        }
        finally { RenderTexture.active = previous; Object.Destroy(image); }
    }
}
