using System.Collections;
using System.Collections.Generic;
using System.IO;
using DragonLegend.Whitebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public sealed class RecoveredWheelWindowTests
{
    [UnityTest]
    public IEnumerator CashAndEveryJackpotUseActualAnimationsPopupsAndCashArrival()
    {
        string key = RecoveredPlayerStore.OriginalKey;
        bool had = PlayerPrefs.HasKey(key); string saved = PlayerPrefs.GetString(key); PlayerPrefs.DeleteKey(key);
        float scale = Time.timeScale, delta = Time.captureDeltaTime; var random = Random.state;
        Scene scene = default; AsyncOperation unload = null; RecoveredWheelWindow game = null; Camera camera = null; RenderTexture texture = null;
        try
        {
            Time.timeScale = 1; Time.captureDeltaTime = .025f;
            yield return SceneManager.LoadSceneAsync("GameEntry", LoadSceneMode.Additive); scene = SceneManager.GetSceneByName("GameEntry");
            GameEntry entry = null;
            foreach (var root in scene.GetRootGameObjects()) { var value = root.GetComponentInChildren<GameEntry>(); if (value != null) entry = value; }
            Assert.IsNotNull(entry); float deadline = Time.realtimeSinceStartup + 5;
            while (entry.CashFlight == null && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsNotNull(entry.CashFlight);
            game = Object.Instantiate(Resources.Load<RecoveredWheelWindow>("RecoveredUI/WheelWindow"), entry.transform, false);
            game.Bind(entry.PlayerProgress, entry.Rules, entry.Ads, entry.CashFlight, entry.transform, entry.Playfield, false, 0);
            camera = entry.GetComponent<Canvas>().worldCamera; texture = new RenderTexture(1080, 1920, 24); camera.targetTexture = texture;
            var events = new List<string>();
            game.SoundRequested += name => events.Add(name); game.Sound1Requested += name => events.Add("sound1:" + name);
            game.PauseMusicRequested += () => events.Add("pause"); game.ResumeMusicRequested += () => events.Add("resume");
            game.StopSound1Requested += () => events.Add("stop1");
            int[] selected = { 1, 4, 0, 6 }; // Cash, Grand, Major, Mini, using actual configured weights.
            for (int pass = 0; pass < selected.Length; pass++)
            {
                bool jackpot = pass != 0, freeClaim = pass == 1;
                entry.PlayerProgress.IsFirstFreeReward = freeClaim;
                int seed;
                for (seed = 0; seed < 100000; seed++) { Random.InitState(seed); if (entry.Rules.RandomWheelWeight() == selected[pass]) break; }
                Assert.Less(seed, 100000, "Each native configured branch must be reachable.");
                Random.InitState(seed); int callbacks = 0; float paid = 0, balance = entry.PlayerProgress.GreenCount;
                events.Clear(); float began = Time.time;
                game.Show(value => { callbacks++; paid = value; Assert.AreEqual(balance, entry.PlayerProgress.GreenCount, "Free caller is notified before the shared cash flight credits the balance."); });
                Assert.AreEqual(selected[pass], game.Rotor.ResultIndex);
                Assert.IsTrue(game.Window.activeSelf); Assert.IsFalse(game.CashPopup.gameObject.activeSelf); Assert.IsFalse(game.JackpotPopup.gameObject.activeSelf);
                Assert.IsNotNull(game.Window.transform.Find("Content/Title").GetComponent<RecoveredTitleShine>());
                Assert.AreEqual(0, game.Frame.Selected); Assert.AreEqual(0, game.Pointer.Selected);
                Time.timeScale = 0; for (int i = 0; i < 4; i++) yield return null;
                Assert.IsFalse(game.Rotor.IsSpinning); Time.timeScale = 1;
                for (int i = 0; i < 60 && !game.Rotor.IsSpinning; i++) yield return null;
                Assert.IsTrue(game.Rotor.IsSpinning); Assert.That(Time.time - began, Is.InRange(.799f, .91f));
                for (int i = 0; i < 100 && game.Rotor.IsSpinning; i++) yield return null;
                Assert.IsNull(game.Error); Assert.AreEqual(1, game.Frame.Selected); Assert.AreEqual(1, game.Pointer.Selected);
                float stopped = Time.time;
                int meterIndex = pass - 1;
                if (jackpot)
                {
                    Assert.IsTrue(game.Meter(meterIndex).Icon.IsWinning);
                    Assert.IsTrue(entry.Playfield.JackpotMeters.At(meterIndex).Icon.IsWinning, "Native event must reach Main as well as Wheel.");
                    CollectionAssert.AreEqual(new[] { "jump", "wheelSpin", "pause", "sound1:ring" }, events);
                }
                else CollectionAssert.AreEqual(new[] { "jump", "wheelSpin", "wheelWin" }, events);
                var item = game.Rotor.Item(selected[pass]).transform;
                for (int i = 0; i < 32; i++) yield return null;
                Assert.AreEqual(Vector3.one, item.localScale); Assert.IsFalse(game.CashPopup.gameObject.activeSelf); Assert.IsFalse(game.JackpotPopup.gameObject.activeSelf);
                for (int i = 0; i < 32 && item.localScale.x < 1.19999f; i++) yield return null;
                Assert.That(item.localScale.x, Is.EqualTo(1.2f).Within(.0001f));
                Assert.That(Time.time - stopped, Is.InRange(1.299f, 1.43f));
                float expected = entry.Rules.GetWheelReward(selected[pass]);
                if (jackpot)
                {
                    // Change the actual balance while the item returns to normal.
                    // Neither selection nor spin-end is the jackpot capture point.
                    expected = 12345 + pass * 100;
                    if (pass == 1) entry.PlayerProgress.GrandJackPotReward = expected;
                    else if (pass == 2) entry.PlayerProgress.MajorJackPotReward = expected;
                    else entry.PlayerProgress.MiniJackPotReward = expected;
                }
                if (pass == 0) Capture(camera, texture, "current-wheel-window.png");
                if (pass == 1) Capture(camera, texture, "current-wheel-grand-win.png");
                for (int i = 0; i < 30 && !game.CashPopup.gameObject.activeSelf && !game.JackpotPopup.gameObject.activeSelf; i++) yield return null;
                Assert.That(Time.time - stopped, Is.InRange(1.599f, 1.78f));
                Assert.That(item.localScale.x, Is.EqualTo(1).Within(.0001f));
                Assert.IsTrue(game.Window.activeSelf, "Show reward overlaps Wheel's exit animation.");
                Assert.AreEqual(0, callbacks); Assert.IsTrue(game.IsRunning);
                if (jackpot)
                {
                    Assert.IsTrue(game.JackpotPopup.gameObject.activeSelf); Assert.IsFalse(game.CashPopup.gameObject.activeSelf);
                    Assert.AreEqual(expected, game.JackpotPopup.Claim.OriginalReward);
                    Assert.AreEqual(meterIndex, game.JackpotPopup.Dragon.Selected);
                    CollectionAssert.AreEqual(new[] { "jump", "wheelSpin", "pause", "sound1:ring", "stop1", "pause", "sound1:jackpotBg", "jackpotm" }, events);
                }
                else { Assert.IsTrue(game.CashPopup.gameObject.activeSelf); Assert.AreEqual(expected, game.CashPopup.Claim.OriginalReward); }
                for (int i = 0; i < 75; i++) yield return null;
                Assert.IsFalse(game.Window.activeSelf);
                if (pass == 1) Capture(camera, texture, "current-wheel-grand-reward.png");
                float multiplier = freeClaim ? 1 : jackpot ? entry.Rules.GetJpClaim(1) : game.CashPopup.Claim.UnadvertisedMultiplier;
                if (!jackpot) game.CashPopup.PlainButton.onClick.Invoke();
                else if (freeClaim) game.JackpotPopup.ClaimButton.onClick.Invoke();
                else game.JackpotPopup.PlainButton.onClick.Invoke();
                deadline = Time.realtimeSinceStartup + 5;
                while (callbacks == 0 && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.IsNull(game.Error); Assert.AreEqual(1, callbacks); Assert.AreEqual(expected * multiplier, paid);
                Assert.AreEqual(balance + paid, entry.PlayerProgress.GreenCount); Assert.IsFalse(game.IsRunning);
                Assert.IsFalse(game.CashPopup.gameObject.activeSelf); Assert.IsFalse(game.JackpotPopup.gameObject.activeSelf);
                if (jackpot) Assert.Contains("resume", events);
            }
        }
        finally
        {
            if (camera != null) camera.targetTexture = null; if (texture != null) Object.Destroy(texture); if (game != null) Object.Destroy(game.gameObject);
            if (scene.IsValid()) unload = SceneManager.UnloadSceneAsync(scene); Time.timeScale = scale; Time.captureDeltaTime = delta; Random.state = random;
            if (had) PlayerPrefs.SetString(key, saved); else PlayerPrefs.DeleteKey(key);
        }
        if (unload != null) yield return unload;
    }
    private static void Capture(Camera camera, RenderTexture target, string name)
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
