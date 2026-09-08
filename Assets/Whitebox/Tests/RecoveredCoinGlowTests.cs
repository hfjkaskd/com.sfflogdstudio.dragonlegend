using System.Collections;
using System.IO;
using DragonLegend.Whitebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public sealed class RecoveredCoinGlowTests
{
    [UnityTest]
    public IEnumerator GlowPreservesTrimmedMeshUvDeformationAndInstanceTint()
    {
        var prefab = Resources.Load<RecoveredCoinGlow>("RecoveredSymbols/CoinGlow");
        var glow = Object.Instantiate(prefab); var other = Object.Instantiate(prefab);
        var mesh = glow.GetComponentInChildren<SkinnedMeshRenderer>(); var baked = new Mesh();
        float scale = Time.timeScale, delta = Time.captureDeltaTime;
        try {
            Time.timeScale = 1; Time.captureDeltaTime = .02f;
            Assert.AreEqual(45, mesh.sharedMesh.vertexCount); Assert.AreEqual(186, mesh.sharedMesh.triangles.Length);
            Assert.AreEqual(7, glow.GetComponentsInChildren<SpriteRenderer>().Length);
            var uv = mesh.sharedMesh.uv[0];
            Assert.AreEqual((149f - 4 + .592243671f * 119) / 993, uv.x, .000001f);
            Assert.AreEqual(1 - (2f - 4 + .0766312853f * 119) / 898, uv.y, .000001f);
            var clip = glow.GetComponent<Animation>().GetClip("glow"); Assert.AreEqual(22f / 30f, clip.length, .00001f);
            clip.SampleAnimation(glow.gameObject, 0); Assert.IsFalse(mesh.enabled);
            Assert.AreEqual(0, mesh.GetBlendShapeWeight(0));
            clip.SampleAnimation(glow.gameObject, 1f / 6f); Assert.IsTrue(mesh.enabled);
            var properties = new MaterialPropertyBlock(); mesh.GetPropertyBlock(properties);
            var tint = properties.GetColor("_Color"); Assert.AreEqual(181f / 255f, tint.g, .00001f); Assert.AreEqual(1, tint.a);
            other.GetComponentInChildren<SkinnedMeshRenderer>().GetPropertyBlock(properties);
            Assert.AreEqual(Color.white, properties.GetColor("_Color"), "Animating one instance cannot recolor another.");
            float[] times = { 8f / 30f, 14f / 30f, 20f / 30f };
            float[] expected = { 47.0054398f, 54.6610508f, 62.3166618f };
            for (int i = 0; i < times.Length; i++) {
                clip.SampleAnimation(glow.gameObject, times[i]); mesh.BakeMesh(baked);
                Assert.AreEqual(expected[i] * .01f, baked.vertices[35].x, .00001f);
            }
            mesh.GetPropertyBlock(properties); Assert.AreEqual(0, properties.GetColor("_Color").a, .00001f);
            glow.Play(); Time.timeScale = 0; for (int i = 0; i < 10; i++) yield return null;
            Assert.IsTrue(glow.IsPlaying); Time.timeScale = 1;
            for (int i = 0; i < 50 && glow.IsPlaying; i++) yield return null;
            Assert.IsFalse(glow.gameObject.activeSelf);
            glow.Play(); Assert.IsTrue(glow.gameObject.activeSelf); Assert.IsTrue(glow.IsPlaying);
        } finally { Time.timeScale = scale; Time.captureDeltaTime = delta; Object.Destroy(glow.gameObject); Object.Destroy(other.gameObject); Object.Destroy(baked); }
    }

    [UnityTest]
    public IEnumerator ActualRewardScanRevealsStoppedCoinsThenRunsIndependentGlow()
    {
        string key = RecoveredPlayerStore.OriginalKey; bool had = PlayerPrefs.HasKey(key); string saved = PlayerPrefs.GetString(key);
        var random = Random.state; float scale = Time.timeScale, delta = Time.captureDeltaTime; PlayerPrefs.DeleteKey(key);
        var root = Object.Instantiate(Resources.Load<GameObject>("Whitebox/GameEntry")); var entry = root.GetComponent<GameEntry>();
        var cameraHost = new GameObject("Reward camera", typeof(Camera)); var camera = cameraHost.GetComponent<Camera>();
        camera.transform.position = new Vector3(100, 0, -10); camera.orthographic = true; camera.orthographicSize = 5;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black; camera.cullingMask = 32;
        var target = new RenderTexture(1080, 1920, 24); camera.targetTexture = target; root.GetComponent<Canvas>().worldCamera = camera;
        var previous = RenderTexture.active; Texture2D capture = null;
        try {
            Time.timeScale = 1; Time.captureDeltaTime = .05f;
            for (int i = 0; i < 200 && entry.Playfield == null; i++) yield return null;
            var field = entry.Playfield; Assert.IsNotNull(field); var board = entry.SpinResult.Board;
            board.BeginGuaranteedBonus(2, new[] { 0, 4 }, new System.Random(21), (min, max) => 0);
            while (board.IsPlacingSingleSymbol) board.StepSingleSymbol();
            var columns = new int[5][];
            for (int c = 0; c < 5; c++) { columns[c] = new int[3]; for (int r = 0; r < 3; r++) columns[c][r] = board.GetSymbol(c, r); }
            float balance = entry.PlayerProgress.GreenCount; int presentations = 0, sounds = 0, firstReward = 0;
            field.CoinStops.CoinRevealSoundRequested += () => sounds++;
            field.BonusCoinPresentationRequested += coin => {
                if(presentations==0)firstReward=coin.Reward;
                Assert.AreEqual(presentations == 0 ? 0 : 4, coin.Column); Assert.AreEqual(0, coin.Row);
                Assert.IsTrue(field.CoinStops.CoinAt(coin.Column, coin.Row).Reveal.IsRevealing); presentations++;
            };
            field.Reels.Begin(-1, c => columns[c]);
            for (int i = 0; i < 200 && presentations == 0; i++) yield return null;
            Assert.AreEqual(1, presentations); Assert.AreEqual(1, sounds);
            var first = field.CoinStops.CoinAt(0, 0); var second = field.CoinStops.CoinAt(4, 0);
            Assert.IsTrue(first.Reveal.IsRevealing); Assert.IsFalse(first.Glow.gameObject.activeSelf);
            Time.timeScale = 0; for (int i = 0; i < 10; i++) yield return null;
            Assert.IsTrue(first.Reveal.IsRevealing); Assert.AreEqual(1, presentations); Time.timeScale = 1;
            for (int i = 0; i < 20 && !first.Glow.IsPlaying; i++) yield return null;
            Assert.IsTrue(first.Glow.IsPlaying); Assert.IsTrue(first.Reveal.GetComponent<Animation>().IsPlaying("idle_chun"));
            yield return null; yield return null;
            Assert.IsTrue(first.RewardText.Label.gameObject.activeSelf);
            Assert.AreEqual(RecoveredCurrency.Format(firstReward,0),first.RewardText.Label.text);
            Canvas.ForceUpdateCanvases();
            RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
            RenderTexture.active = target; capture = new Texture2D(1080, 1920, TextureFormat.RGB24, false);
            capture.ReadPixels(new Rect(0, 0, 1080, 1920), 0, 0); capture.Apply();
            File.WriteAllBytes(Path.GetFullPath(Path.Combine(Application.dataPath, "../Artifacts/current-coin-reward-glow.png")), capture.EncodeToPNG());
            for (int i = 0; i < 80 && (field.BonusCoins.IsRunning || first.Glow.IsPlaying || second.Glow.IsPlaying); i++) yield return null;
            Assert.AreEqual(2, presentations); Assert.AreEqual(2, sounds); Assert.IsNull(field.BonusCoins.Error);
            Assert.IsFalse(first.Glow.gameObject.activeSelf); Assert.IsFalse(second.Glow.gameObject.activeSelf);
            Assert.AreEqual(balance, entry.PlayerProgress.GreenCount, "Reveal must not prematurely credit the missing flight stage.");
            Assert.IsFalse(field.BonusCollection.GetUnselectedTarget(0, 1).GetChild(0).gameObject.activeSelf);
            Assert.AreEqual(2, field.CoinStops.CreatedCount);
            first.PlayShow(); Assert.IsFalse(first.Reveal.gameObject.activeSelf); Assert.IsFalse(first.Glow.gameObject.activeSelf);
            Assert.IsFalse(first.RewardText.Label.gameObject.activeSelf);
            Assert.AreEqual(5, first.GetComponentsInChildren<SpriteRenderer>().Length);
            field.CoinStops.Unbind(); Assert.AreEqual(0, field.CoinStops.ActiveCount);
        } finally {
            Object.DestroyImmediate(root); Object.Destroy(cameraHost); RenderTexture.active = previous;
            if (capture != null) Object.Destroy(capture); target.Release(); Object.Destroy(target);
            if (had) PlayerPrefs.SetString(key, saved); else PlayerPrefs.DeleteKey(key); PlayerPrefs.Save();
            Random.state = random; Time.timeScale = scale; Time.captureDeltaTime = delta;
        }
    }
}
