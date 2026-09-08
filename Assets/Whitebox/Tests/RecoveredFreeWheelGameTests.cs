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

public sealed class RecoveredFreeWheelGameTests
{
    [UnityTest]
    public IEnumerator EntryUsesOriginalIconAndDefersWheelSelectionUntilAfterItsPulse()
    {
        string key = RecoveredPlayerStore.OriginalKey; bool had = PlayerPrefs.HasKey(key); string saved = PlayerPrefs.GetString(key); PlayerPrefs.DeleteKey(key);
        float scale = Time.timeScale, delta = Time.captureDeltaTime; var random = Random.state;
        Scene scene = default; AsyncOperation unload = null; RecoveredFreeWheelGame game = null; Camera camera = null; RenderTexture texture = null;
        try
        {
            Time.timeScale = 1; Time.captureDeltaTime = .05f;
            yield return SceneManager.LoadSceneAsync("GameEntry", LoadSceneMode.Additive); scene = SceneManager.GetSceneByName("GameEntry");
            var entry = Entry(scene); float deadline = Time.realtimeSinceStartup + 5;
            while (entry.CashFlight == null && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsNotNull(entry.CashFlight);
            game = Object.Instantiate(Resources.Load<RecoveredFreeWheelGame>("RecoveredUI/FreeWheelGame"), entry.transform, false);
            game.Bind(entry.PlayerProgress, entry.Rules, entry.Ads, entry.CashFlight, entry.transform, entry.Playfield, false, 0);
            camera = entry.GetComponent<Canvas>().worldCamera; texture = new RenderTexture(1080, 1920, 24); camera.targetTexture = texture;
            var rules = BranchRules();
            Action<Vector3, Action<float>> unexpected = (p, c) => Assert.Fail("Expected Wheel branch.");
            var router = new RecoveredFreeSmallGameRouter(rules, entry.PlayerProgress, unexpected, game.Begin, unexpected, unexpected);
            int taskBefore = TaskCount(entry, 4), callbacks = 0; float balance = entry.PlayerProgress.GreenCount, paid = 0;
            var source = entry.transform.TransformPoint(new Vector3(0, 100, 0));
            Random.InitState(97); router.Open(0, source, value => { callbacks++; paid = value; Assert.AreEqual(balance, entry.PlayerProgress.GreenCount); });
            Assert.IsTrue(game.IsRunning); Assert.AreEqual(source, game.Icon.position);
            Assert.AreEqual(new Vector2(208, 217), game.Icon.sizeDelta); Assert.AreEqual(Vector3.one, game.Icon.localScale);
            Assert.AreEqual("mfyx_icon_zhuanpan", game.Icon.GetComponent<UnityEngine.UI.Image>().sprite.name);
            Assert.AreEqual(taskBefore, TaskCount(entry, 4), "Wheel does not run Slot's task mutation.");
            Random.InitState(719); int next = Random.Range(0, 1000000); Random.InitState(719);
            Time.timeScale = 0; for (int i = 0; i < 3; i++) yield return null;
            Assert.AreEqual(next, Random.Range(0, 1000000)); Assert.IsFalse(game.Window.Window.activeSelf);
            Assert.AreEqual(Vector3.one, game.Icon.localScale);
            int seed = SeedFor(entry.Rules, 1); Random.InitState(seed);
            Time.timeScale = 1;
            for (int i = 0; i < 3; i++) yield return null;
            Assert.That(game.Icon.localScale.x, Is.EqualTo(1.375f).Within(.0001f));
            Assert.That(game.Icon.localScale.z, Is.EqualTo(.25f).Within(.0001f));
            for (int i = 0; i < 3; i++) yield return null;
            Assert.That(game.Icon.localScale.x, Is.EqualTo(1.5f).Within(.0001f)); Assert.AreEqual(0, game.Icon.localScale.z);
            Assert.IsFalse(game.Window.Window.activeSelf); Capture(camera, texture);
            for (int i = 0; i < 10 && !game.Window.Window.activeSelf; i++) yield return null;
            Assert.IsNull(game.Error); Assert.IsTrue(game.Window.Window.activeSelf); Assert.IsFalse(game.Icon.gameObject.activeSelf);
            Assert.AreEqual(1, game.Window.Rotor.ResultIndex, "Weighted selection occurs when the real Wheel window opens.");
            Assert.That(game.Icon.localScale.x, Is.EqualTo(1).Within(.0001f)); Assert.AreEqual(0, game.Icon.localScale.z);
            for (int i = 0; i < 130 && !game.Window.CashPopup.gameObject.activeSelf; i++) yield return null;
            Assert.IsNull(game.Window.Error); Assert.IsTrue(game.Window.CashPopup.gameObject.activeSelf);
            float expected = entry.Rules.GetWheelReward(1) * game.Window.CashPopup.Claim.UnadvertisedMultiplier;
            for (int i = 0; i < 50 && !game.Window.CashPopup.PlainButton.gameObject.activeInHierarchy; i++) yield return null;
            game.Window.CashPopup.PlainButton.onClick.Invoke(); deadline = Time.realtimeSinceStartup + 5;
            while (callbacks == 0 && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.AreEqual(1, callbacks); Assert.AreEqual(expected, paid); Assert.AreEqual(balance + paid, entry.PlayerProgress.GreenCount);
            Assert.IsFalse(game.IsRunning); Assert.IsFalse(game.Window.IsRunning); Assert.AreEqual(taskBefore, TaskCount(entry, 4));
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
    public IEnumerator TwoStoppedBallsClaimCashThenGrandAndResumeTheActualFreeScan()
    {
        string key = RecoveredPlayerStore.OriginalKey; bool had = PlayerPrefs.HasKey(key); string saved = PlayerPrefs.GetString(key); PlayerPrefs.DeleteKey(key);
        float scale = Time.timeScale, delta = Time.captureDeltaTime; var random = Random.state;
        Scene scene = default; AsyncOperation unload = null; RecoveredFreeReels reels = null; RecoveredNpcPresentation npc = null;
        RecoveredFreeWheelGame game = null; GameObject target = null;
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
            target = new GameObject("Wheel ball destination"); target.transform.position = new Vector3(0, 3, 0);
            game = Object.Instantiate(Resources.Load<RecoveredFreeWheelGame>("RecoveredUI/FreeWheelGame"), entry.transform, false);
            game.Bind(entry.PlayerProgress, entry.Rules, entry.Ads, entry.CashFlight, entry.transform, entry.Playfield, false, 0);
            entry.PlayerProgress.GameSlotType = RecoveredSlotType.Free;
            float initialBalance = entry.PlayerProgress.GreenCount, initialTotal = entry.PlayerProgress.TotalFreeSpinWin, paid = 0;
            int requests = 0, done = 0, firstSeed = SeedFor(entry.Rules, 1), secondSeed = SeedFor(entry.Rules, 4);
            Exception npcError = null; npc.Failed += error => npcError = error;
            Action<Vector3, Action<float>> unexpected = (p, c) => Assert.Fail("Seeded fixture must select Wheel.");
            var router = new RecoveredFreeSmallGameRouter(rules, entry.PlayerProgress, unexpected, (position, callback) => {
                requests++; Assert.AreEqual(0, reels.Specials.FlightBallCount);
                Random.InitState(requests == 1 ? firstSeed : secondSeed);
                game.Begin(position, callback);
                Assert.AreEqual(position, game.Icon.position);
            }, unexpected, unexpected);
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
                while (!game.Window.CashPopup.gameObject.activeSelf && !game.Window.JackpotPopup.gameObject.activeSelf && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.IsNull(reels.BallScan.Error); Assert.IsNull(npcError); Assert.IsNull(game.Error); Assert.IsNull(game.Window.Error);
                Assert.AreEqual(ball + 1, requests); Assert.AreEqual(ball, reels.CoinScan.Rewards.Count);
                Assert.AreEqual(ball == 0 ? 1 : 4, game.Window.Rotor.ResultIndex);
                Assert.IsTrue(reels.BallScan.IsRunning); Assert.AreEqual(0, done);
                var button = ball == 0 ? game.Window.CashPopup.PlainButton : game.Window.JackpotPopup.PlainButton;
                for (int i = 0; i < 60 && !button.gameObject.activeInHierarchy; i++) yield return null;
                Assert.IsTrue(button.gameObject.activeInHierarchy);
                float reward = ball == 0 ? game.Window.CashPopup.Claim.OriginalReward * game.Window.CashPopup.Claim.UnadvertisedMultiplier :
                    game.Window.JackpotPopup.Claim.OriginalReward * game.Window.JackpotPopup.Claim.UnadvertisedMultiplier;
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
    private static int SeedFor(RecoveredGameplayRules rules, int index)
    {
        for (int seed = 0; seed < 100000; seed++) { Random.InitState(seed); if (rules.RandomWheelWeight() == index) return seed; }
        Assert.Fail("Native Wheel outcome is unreachable: " + index); return -1;
    }
    private static int TaskCount(GameEntry entry, int id)
    {
        foreach (var task in entry.PlayerStore.Data.PlayerTaskDatas) if (task.id == id) return task.count;
        return 0;
    }
    private static RecoveredGameplayRules BranchRules() => new RecoveredGameplayRules(new GoldenDragonAutoGenConfig { Rrggiomg = new RrggiomgPoro {
        QoinOmoinrKgiitr = new List<int> { 1000000 }, RollOmoinrKgiitr = new List<int> { 0, 0, 1000000 }, RollRipgKgiitr = new List<int> { 1000000 },
        RollGlorg = new List<int> { 0, 0, 0 }, RollKtggl = new List<int> { 1000000, 1000000, 1000000 },
        RollRrgogirg = new List<int> { 0, 0, 0 }, RollLiqki = new List<int> { 0, 0, 0 }
    } });
    private static void Capture(Camera camera, RenderTexture target)
    {
        var previous = RenderTexture.active; var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
        try
        {
            Canvas.ForceUpdateCanvases(); RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target }); RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); image.Apply(); File.WriteAllBytes(Path.Combine(Application.dataPath, "../Artifacts/current-free-wheel-entry.png"), image.EncodeToPNG());
        }
        finally { RenderTexture.active = previous; Object.Destroy(image); }
    }
}
