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

public sealed class RecoveredCoinRevealTests
{
    [UnityTest]
    public IEnumerator RevealPreservesNativeMeshFramesAndThreeTimesSpeedBeforeIdle()
    {
        var prefab = Resources.Load<RecoveredCoinReveal>("RecoveredSymbols/CoinReveal");
        Assert.IsNotNull(prefab);
        var coin = Object.Instantiate(prefab);
        var cameraHost = new GameObject("Reveal camera", typeof(Camera)); var camera = cameraHost.GetComponent<Camera>();
        camera.transform.position = new Vector3(0, 0, -10); camera.orthographic = true; camera.orthographicSize = 2;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black; camera.cullingMask = 32;
        var target = new RenderTexture(512, 512, 24); camera.targetTexture = target;
        var baked = new Mesh(); Texture2D capture = null; var previous = RenderTexture.active;
        float scale = Time.timeScale, delta = Time.captureDeltaTime;
        try {
            Time.timeScale = 1; Time.captureDeltaTime = .02f;
            Assert.IsEmpty(coin.GetComponentsInChildren<Graphic>(true));
            var mesh = coin.GetComponentInChildren<SkinnedMeshRenderer>(true);
            Assert.AreEqual(70, mesh.sharedMesh.vertexCount); Assert.AreEqual(204, mesh.sharedMesh.triangles.Length);
            Assert.AreEqual(3, mesh.sharedMesh.blendShapeCount);
            Assert.AreEqual(13, coin.GetComponentsInChildren<SpriteRenderer>(true).Length);
            var player = coin.GetComponent<Animation>(); var clip = player.GetClip("zcjb_b_chun");
            Assert.AreEqual(2f / 3f, clip.length, .00001f);
            // Independent first vertex coordinates from the original 4.1.24 skeleton deform frames.
            float[] times = { 0, 1f / 6f, 1f / 3f, .5f, 2f / 3f };
            Vector2[] expected = {
                new Vector2(-10.1071701f, -114.495018f), new Vector2(1.44641495f, -118.007874f),
                new Vector2(13, -121.520729f), new Vector2(9.4544649f, -117.433544f),
                new Vector2(5.90892982f, -113.346359f)
            };
            SpriteRenderer face = null;
            foreach (var sprite in coin.GetComponentsInChildren<SpriteRenderer>()) if (sprite.transform.parent.name == "Slot23") face = sprite;
            Assert.IsNotNull(face);
            for (int i = 0; i < times.Length; i++) {
                clip.SampleAnimation(coin.gameObject, times[i]); mesh.BakeMesh(baked);
                Assert.AreEqual(expected[i].x * .01f, baked.vertices[0].x, .00001f);
                Assert.AreEqual(expected[i].y * .01f, baked.vertices[0].y, .00001f);
                Assert.AreEqual(i >= 2, face.enabled, "Face attachment begins at original 1/3 second.");
            }
            clip.SampleAnimation(coin.gameObject, 1f / 6f);
            RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
            RenderTexture.active = target; capture = new Texture2D(512, 512, TextureFormat.RGB24, false);
            capture.ReadPixels(new Rect(0, 0, 512, 512), 0, 0); capture.Apply();
            int gold = 0; foreach (var p in capture.GetPixels32()) if (p.r > 120 && p.g > 80 && p.b < p.r) gold++;
            Assert.Greater(gold, 1000);
            File.WriteAllBytes(Path.GetFullPath(Path.Combine(Application.dataPath, "../Artifacts/current-coin-reveal.png")), capture.EncodeToPNG());
            int completed = 0; coin.Revealed += () => completed++;
            coin.PlayReveal(); Assert.IsFalse(face.enabled, "Replaying restores setup before delayed attachment.");
            Assert.AreEqual(3, player["zcjb_b_chun"].speed);
            Time.timeScale = 0;
            for (int i = 0; i < 10; i++) yield return null;
            Assert.IsTrue(coin.IsRevealing); Assert.AreEqual(0, completed);
            Time.timeScale = 1; float start = Time.time; int frames = 0;
            while (coin.IsRevealing && frames++ < 30) yield return null;
            Assert.AreEqual(1, completed); Assert.That(Time.time - start, Is.InRange(.20f, .28f));
            Assert.IsTrue(player.IsPlaying("idle_chun")); Assert.AreEqual(1, player["idle_chun"].speed);
            yield return null; Assert.IsFalse(mesh.enabled); Assert.IsTrue(face.enabled);
            coin.PlayReveal(); coin.gameObject.SetActive(false); coin.gameObject.SetActive(true);
            yield return null; Assert.IsFalse(coin.IsRevealing); Assert.AreEqual(1, completed);
        } finally {
            Time.timeScale = scale; Time.captureDeltaTime = delta; RenderTexture.active = previous;
            Object.Destroy(coin.gameObject); Object.Destroy(cameraHost); Object.Destroy(baked);
            if (capture != null) Object.Destroy(capture); target.Release(); Object.Destroy(target);
        }
    }
}
