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
            { float resourceDeadline=Time.realtimeSinceStartup+5; while(entry.Playfield==null&&Time.realtimeSinceStartup<resourceDeadline)yield return null; }
            var field = entry.Playfield; Assert.IsNotNull(field); Assert.AreEqual("GOOD LUCK",field.DownWin.Label.text); var board = entry.SpinResult.Board;
            // This fixture inspects the bonus-coin layer after all independent effects
            // finish. Stop the newly connected next stage so its popup does not cover
            // the glyph pixels under inspection; BigWinIntegrationTests covers that path.
            field.BigWinBranchEntered+=field.BigWinSequence.Cancel;
            board.BeginGuaranteedBonus(2, new[] { 0, 4 }, new System.Random(21), (min, max) => 0);
            while (board.IsPlacingSingleSymbol) board.StepSingleSymbol();
            var columns = new int[5][];
            for (int c = 0; c < 5; c++) { columns[c] = new int[3]; for (int r = 0; r < 3; r++) columns[c][r] = board.GetSymbol(c, r); }
            float balance = entry.PlayerProgress.GreenCount; int presentations = 0, sounds = 0, firstReward = 0;
            field.CoinStops.CoinRevealSoundRequested += () => sounds++;
            int winFlights=0,winArrivals=0,burstSounds=0,winVibrations=0;
            field.CoinStops.RewardPresentationFinished+=effect=>{
                Assert.IsTrue(effect.GetComponent<RecoveredCoinStopEffect>().IsScaling);
                Assert.AreEqual(1,field.WinFlight.ActiveCount);
                Assert.AreEqual(effect.transform.position,field.WinFlight.FlightAt(0).StartPoint);
                Assert.AreEqual(field.DownWin.transform.position,field.WinFlight.FlightAt(0).EndPoint);
                winFlights++;
            };
            field.WinFlight.ArrivalEffectRequested+=()=>{Assert.AreEqual(0,field.WinFlight.ActiveCount);Assert.Greater(field.WinFlight.ActiveBurstCount,0);winArrivals++;};
            field.WinFlight.CoinBurstSoundRequested+=()=>{Assert.AreEqual(burstSounds+1,winArrivals);burstSounds++;};
            field.WinFlight.VibrationRequested+=duration=>{Assert.AreEqual(200,duration);Assert.AreEqual(winVibrations+1,burstSounds);winVibrations++;};
            int arrivals=0, winUpdates=0;float displayedReward=0;
            field.DownWin.Changed+=value=>{winUpdates++;displayedReward=value;};
            field.CoinStops.ExpSoundRequested+=()=>{
                Assert.AreEqual(1,field.CoinStops.ActiveFlightCount);
                Assert.IsFalse(field.CoinStops.FlightAt(0).Destination.GetChild(0).gameObject.activeSelf);
            };
            field.CoinStops.LampLitEffectRequested+=lamp=>{
                Assert.AreEqual(0,field.CoinStops.ActiveFlightCount);Assert.IsTrue(lamp.GetChild(0).gameObject.activeSelf);arrivals++;
                Assert.Greater(field.CoinStops.ActiveFlashCount,0);
                var flash=field.CoinStops.FlashAt(field.CoinStops.ActiveFlashCount-1);
                Assert.AreSame(lamp,flash.transform.parent);Assert.IsTrue(flash.IsPlaying);
                Assert.AreEqual(Vector2.zero,((RectTransform)flash.transform).anchoredPosition);
            };
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
            for(int i=0;i<20&&field.CoinStops.ActiveFlightCount==0;i++)yield return null;
            Assert.AreEqual(1,field.CoinStops.ActiveFlightCount);
            var flying=field.CoinStops.FlightAt(0);
            Assert.IsFalse(flying.Destination.GetChild(0).gameObject.activeSelf);
            yield return null;yield return null;
            Assert.IsTrue(flying.IsFlying);Assert.Greater(Vector3.Distance(flying.StartPoint,flying.transform.position),.1f);
            RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});
            RenderTexture.active=target;capture.ReadPixels(new Rect(0,0,1080,1920),0,0);capture.Apply();
            File.WriteAllBytes(Path.GetFullPath(Path.Combine(Application.dataPath,"../Artifacts/current-lamp-flight.png")),capture.EncodeToPNG());
            Assert.AreEqual(1,field.WinFlight.ActiveCount);
            Assert.Greater(Vector3.Distance(field.WinFlight.FlightAt(0).StartPoint,field.WinFlight.FlightAt(0).transform.position),.01f);
            yield return null;yield return null;
            Assert.AreEqual(1,field.WinFlight.ActiveCount);
            Canvas.ForceUpdateCanvases();
            RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
            RenderTexture.active=target;capture.ReadPixels(new Rect(0,0,1080,1920),0,0);capture.Apply();
            File.WriteAllBytes(Path.GetFullPath(Path.Combine(Application.dataPath,"../Artifacts/current-win-flight.png")),capture.EncodeToPNG());
            for(int i=0;i<20&&field.CoinStops.ActiveFlashCount==0;i++)yield return null;
            Assert.Greater(field.CoinStops.ActiveFlashCount,0);
            var activeFlash=field.CoinStops.FlashAt(0);
            var animatedImages=activeFlash.GetComponentsInChildren<UnityEngine.UI.Image>();
            bool visibleFlash=false;
            for(int i=0;i<5&&!visibleFlash;i++) {
                yield return null;
                foreach(var image in animatedImages)if(image.enabled&&image.color.a>.1f)visibleFlash=true;
            }
            Assert.IsTrue(visibleFlash,"The arrival animation must sample a visible frame.");
            Canvas.ForceUpdateCanvases();
            RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});
            RenderTexture.active=target;capture.ReadPixels(new Rect(0,0,1080,1920),0,0);capture.Apply();
            var lampPoint=camera.WorldToScreenPoint(field.BonusCollection.GetUnselectedTarget(0,1).position);
            int bright=0;var pixels=capture.GetPixels32();
            for(int y=(int)lampPoint.y-40;y<(int)lampPoint.y+40;y++)for(int x=(int)lampPoint.x-40;x<(int)lampPoint.x+40;x++)
                if(pixels[y*1080+x].r>180&&pixels[y*1080+x].g>120)bright++;
            Assert.Greater(bright,300);
            File.WriteAllBytes(Path.GetFullPath(Path.Combine(Application.dataPath,"../Artifacts/current-lamp-flash.png")),capture.EncodeToPNG());
            // Render the same frame without the flash; the already-lit coin must not satisfy this check.
            var flashImages=field.CoinStops.FlashAt(0).GetComponentsInChildren<UnityEngine.UI.Image>();
            foreach(var image in flashImages)image.enabled=false;
            Canvas.ForceUpdateCanvases();
            RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});
            RenderTexture.active=target;capture.ReadPixels(new Rect(0,0,1080,1920),0,0);capture.Apply();
            var withoutFlash=capture.GetPixels32();int flashPixels=0;
            for(int y=(int)lampPoint.y-100;y<(int)lampPoint.y+100;y++)for(int x=(int)lampPoint.x-100;x<(int)lampPoint.x+100;x++) {
                int index=y*1080+x;
                if(pixels[index].r>withoutFlash[index].r+10 || pixels[index].g>withoutFlash[index].g+10)flashPixels++;
            }
            Assert.Greater(flashPixels,100,"The arrival flash must add visible pixels beyond the lit lamp itself.");
            foreach(var image in flashImages)image.enabled=true;

            Assert.Greater(field.WinFlight.ActiveBurstCount,0);
            var burst=field.WinFlight.BurstAt(0);Assert.IsTrue(burst.IsPlaying);
            Assert.AreEqual(Vector2.zero,((RectTransform)burst.transform).anchoredPosition);
            Assert.AreSame(field.DownWin.transform.parent,burst.transform.parent);
            Canvas.ForceUpdateCanvases();
            RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
            RenderTexture.active=target;capture.ReadPixels(new Rect(0,0,1080,1920),0,0);capture.Apply();
            File.WriteAllBytes(Path.GetFullPath(Path.Combine(Application.dataPath,"../Artifacts/current-win-burst.png")),capture.EncodeToPNG());
            var withBurst=capture.GetPixels32();var burstRig=burst.GetComponent<RecoveredRegionRig>();burstRig.enabled=false;
            Canvas.ForceUpdateCanvases();
            RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
            RenderTexture.active=target;capture.ReadPixels(new Rect(0,0,1080,1920),0,0);capture.Apply();
            var withoutBurst=capture.GetPixels32();int burstPixels=0;
            for(int i=0;i<1080*650;i++)if(System.Math.Abs(withBurst[i].r-withoutBurst[i].r)>10||System.Math.Abs(withBurst[i].g-withoutBurst[i].g)>10)burstPixels++;
            Assert.Greater(burstPixels,200,"The arrival burst must actually contribute visible pixels.");
            burstRig.enabled=true;

            for (int i = 0; i < 80 && (field.BonusCoins.IsRunning || first.Glow.IsPlaying || second.Glow.IsPlaying || field.CoinStops.ActiveFlashCount>0 || field.WinFlight.ActiveBurstCount>0); i++) yield return null;
            Assert.AreEqual(2, presentations); Assert.AreEqual(2, sounds); Assert.IsNull(field.BonusCoins.Error);
            Assert.AreEqual(2,winFlights);Assert.AreEqual(2,winArrivals);Assert.AreEqual(2,burstSounds);Assert.AreEqual(2,winVibrations);
            Assert.AreEqual(0,field.WinFlight.ActiveBurstCount);
            Assert.AreEqual(1,field.WinFlight.CreatedCount);Assert.AreEqual(0,field.WinFlight.ActiveCount);
            Assert.AreEqual(2,winUpdates);Assert.AreEqual(field.BonusCoins.TotalReward,displayedReward);
            Assert.AreEqual(RecoveredCurrency.Format(displayedReward,0),field.DownWin.Label.text);
            Canvas.ForceUpdateCanvases();
            RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});
            RenderTexture.active=target;capture.ReadPixels(new Rect(0,0,1080,1920),0,0);capture.Apply();
            File.WriteAllBytes(Path.GetFullPath(Path.Combine(Application.dataPath,"../Artifacts/current-down-win.png")),capture.EncodeToPNG());
            // Verify every rendered currency glyph over successive frozen frames. String equality
            // alone misses the partial text loss observed during offscreen rendering.
            Time.timeScale=0;
            for(int pass=0;pass<8;pass++) {
                yield return null;
                Canvas.ForceUpdateCanvases();
                RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
                RenderTexture.active=target;capture.ReadPixels(new Rect(0,0,1080,1920),0,0);capture.Apply();
                File.WriteAllBytes(Path.GetFullPath(Path.Combine(Application.dataPath,"../Artifacts/current-down-win.png")),capture.EncodeToPNG());
                var rendered=capture.GetPixels32();var label=field.DownWin.Label;
                for(int character=0;character<label.textInfo.characterCount;character++) {
                    var info=label.textInfo.characterInfo[character];if(!info.isVisible)continue;
                    var low=camera.WorldToScreenPoint(label.transform.TransformPoint(info.bottomLeft));
                    var high=camera.WorldToScreenPoint(label.transform.TransformPoint(info.topRight));
                    int greenPixels=0;
                    for(int y=Mathf.Max(0,(int)low.y);y<Mathf.Min(1920,Mathf.CeilToInt(high.y));y++)
                        for(int x=Mathf.Max(0,(int)low.x);x<Mathf.Min(1080,Mathf.CeilToInt(high.x));x++) {
                            var pixel=rendered[y*1080+x];if(pixel.g>100&&pixel.g>pixel.r*1.3f)greenPixels++;
                        }
                    Assert.Greater(greenPixels,20,"Missing rendered currency glyph "+info.character+" on frozen frame "+pass);
                }
            }
            Time.timeScale=1;

            Assert.IsFalse(first.Glow.gameObject.activeSelf); Assert.IsFalse(second.Glow.gameObject.activeSelf);
            Assert.AreEqual(balance, entry.PlayerProgress.GreenCount, "Reveal must not prematurely credit the missing flight stage.");
            Assert.IsTrue(field.BonusCollection.GetUnselectedTarget(0, 1).GetChild(0).gameObject.activeSelf);
            Assert.AreEqual(2,arrivals);Assert.AreEqual(1,field.CoinStops.CreatedFlightCount);Assert.AreEqual(0,field.CoinStops.ActiveFlightCount);
            Assert.AreEqual(0,field.CoinStops.ActiveFlashCount);Assert.That(field.CoinStops.CreatedFlashCount,Is.InRange(1,2));
            Assert.AreEqual(2, field.CoinStops.CreatedCount);
            first.PlayShow(); Assert.IsFalse(first.Reveal.gameObject.activeSelf); Assert.IsFalse(first.Glow.gameObject.activeSelf);
            Assert.IsFalse(first.RewardText.Label.gameObject.activeSelf);
            Assert.AreEqual(5, first.GetComponentsInChildren<SpriteRenderer>().Length);
            field.WinFlight.Play(first.transform);Assert.AreEqual(1,field.WinFlight.ActiveCount);
            yield return null;field.Unbind();
            Assert.AreEqual(0,field.WinFlight.ActiveCount);Assert.AreEqual(0,field.CoinStops.ActiveCount);
            for(int i=0;i<10;i++)yield return null;
            Assert.AreEqual(2,winArrivals,"Profile unbind must cancel the old transfer callback.");
        } finally {
            Object.DestroyImmediate(root); Object.Destroy(cameraHost); RenderTexture.active = previous;
            if (capture != null) Object.Destroy(capture); target.Release(); Object.Destroy(target);
            if (had) PlayerPrefs.SetString(key, saved); else PlayerPrefs.DeleteKey(key); PlayerPrefs.Save();
            Random.state = random; Time.timeScale = scale; Time.captureDeltaTime = delta;
        }
    }
}
