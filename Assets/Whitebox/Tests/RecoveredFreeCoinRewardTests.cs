using System.Collections;
using System.Collections.Generic;
using System.IO;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

public sealed class RecoveredFreeCoinRewardTests
{
    private static RecoveredFreeReels Create()
    {
        var weights=new List<int>();for(int i=0;i<=15;i++)weights.Add(i==15?1000000:0);
        var result=new RecoveredFreeSpinResult(new RecoveredGameplayRules(new GoldenDragonAutoGenConfig{Rrggiomg=new RrggiomgPoro{
            QoinOmoinrKgiitr=weights,RollOmoinrKgiitr=new List<int>{1000000},RollRipgKgiitr=new List<int>{1}}}));
        result.Begin(new[]{3});while(result.IsGenerating)result.Step();
        var root=Object.Instantiate(Resources.Load<RecoveredFreeReels>("RecoveredSymbols/FreeReels"));
        root.Initialize(Resources.Load<RecoveredSymbolCatalog>("RecoveredSymbols/OriginalSymbolCatalog"),result);return root;
    }
    [UnityTest]
    public IEnumerator NativeRevealTextThenPooledFlightLightsTargetWithoutDelayingCompletion()
    {
        var random=Random.state;float scale=Time.timeScale,delta=Time.captureDeltaTime;var root=Create();
        var collection=Object.Instantiate(Resources.Load<RecoveredBonusCollection>("RecoveredUI/BonusCollection"));
        var canvasHost=new GameObject("Collection test canvas",typeof(RectTransform),typeof(Canvas));
        canvasHost.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;
        collection.transform.SetParent(canvasHost.transform,false);
        collection.transform.localScale=Vector3.one*.01f;collection.transform.position=new Vector3(0,3,0);collection.Initialize(new[]{0,0,0,0,0});
        try {
            Time.timeScale=1;Time.captureDeltaTime=.02f;
            var reel=root.At(0,0);root.Specials.ApplyStoppedResult(reel,0,0);root.ColumnAt(0).ShowFreeEffects();
            var coin=(RecoveredFreeCoin)root.Specials.StoppedAt(reel);var reward=coin.RewardPresentation;var lamps=root.Specials.LampFlights;
            var sounds=new List<string>();root.Specials.SoundRequested+=sounds.Add;
            for(int pass=0;pass<2;pass++) {
                var target=collection.GetUnselectedTarget(0,pass+1);int completed=0;
                reward.Play(12.34f,0,target,()=>{Assert.AreEqual(1,lamps.ActiveFlights);Assert.IsFalse(target.GetChild(0).gameObject.activeSelf);completed++;});
                Assert.IsTrue(coin.Art.Player.IsPlaying("zcjb_b_chun"));Assert.AreEqual(3,coin.Art.Player["zcjb_b_chun"].speed);
                Assert.IsFalse(coin.Reward.gameObject.activeSelf);Assert.IsFalse(coin.Glow.gameObject.activeSelf);
                Time.timeScale=0;for(int i=0;i<3;i++)yield return null;Assert.IsFalse(coin.Reward.gameObject.activeSelf);
                Time.timeScale=1;
                for(int i=0;i<30&&reward.IsRevealing;i++)yield return null;
                Assert.IsFalse(reward.IsRevealing);Assert.IsTrue(coin.Art.Player.IsPlaying("idle_chun"));Assert.AreEqual(1,coin.Art.Player["idle_chun"].speed);
                Assert.IsTrue(coin.Glow.gameObject.activeSelf);Assert.IsTrue(coin.Reward.gameObject.activeSelf);
                Assert.AreEqual(RecoveredCurrency.Format(12.34f,0,2),coin.Reward.text);Assert.IsTrue(reward.IsPresenting);
                if(pass==0)Capture("current-free-coin-reward-reveal.png");
                for(int i=0;i<40&&reward.IsPresenting;i++)yield return null;
                Assert.AreEqual(1,completed);Assert.AreEqual(1,lamps.ActiveFlights);Assert.AreEqual(1,lamps.CreatedFlights);
                var flight=lamps.FlightAt(0);Assert.AreSame(coin.transform,flight.transform.parent);Assert.AreEqual(target.position,flight.EndPoint);
                Time.timeScale=0;var position=flight.transform.position;for(int i=0;i<3;i++)yield return null;Assert.AreEqual(position,flight.transform.position);
                Time.timeScale=1;for(int i=0;i<30&&lamps.ActiveFlights>0;i++)yield return null;
                Assert.AreEqual(0,lamps.ActiveFlights);Assert.IsTrue(target.GetChild(0).gameObject.activeSelf);Assert.AreEqual(1,lamps.ActiveFlashes);
                if(pass==0) {for(int i=0;i<5;i++)yield return null;Capture("current-free-coin-reward-arrival.png");}
                for(int i=0;i<40&&lamps.ActiveFlashes>0;i++)yield return null;
                Assert.AreEqual(0,lamps.ActiveFlashes);Assert.IsFalse(coin.Glow.gameObject.activeSelf);
            }
            CollectionAssert.AreEqual(new[]{"coinReveal","exp","coinReveal","exp"},sounds);
        } finally {Time.timeScale=scale;Time.captureDeltaTime=delta;Object.Destroy(root.gameObject);Object.Destroy(canvasHost);Random.state=random;}
    }
    [UnityTest]
    public IEnumerator NullLampTargetStillCompletesAndKeepsOriginalRewardFont()
    {
        float delta=Time.captureDeltaTime;var coin=Object.Instantiate(Resources.Load<RecoveredFreeCoin>("RecoveredSymbols/FreeCoin"));
        try {
            Time.captureDeltaTime=.02f;int completed=0,flight=0;coin.RewardPresentation.LampFlightRequested+=(owner,target)=>flight++;
            coin.RewardPresentation.Play(9.5f,0,null,()=>completed++);
            for(int i=0;i<50&&completed==0;i++)yield return null;
            Assert.AreEqual(1,completed);Assert.AreEqual(0,flight);
            Assert.AreSame(Resources.Load<Font>("RecoveredUI/CoinRewardText/Green"),coin.Reward.font);
            Assert.AreEqual(RecoveredCurrency.Format(9.5f,0,2),coin.Reward.text);
        } finally {Time.captureDeltaTime=delta;Object.Destroy(coin.gameObject);}
    }
    private static void Capture(string name)
    {
        var host=new GameObject("Free reward camera",typeof(Camera));var camera=host.GetComponent<Camera>();
        camera.orthographic=true;camera.orthographicSize=4.2f;camera.transform.position=new Vector3(0,0,-10);camera.cullingMask=33;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
        var target=new RenderTexture(1080,840,24);camera.targetTexture=target;var previous=RenderTexture.active;
        var capture=new Texture2D(1080,840,TextureFormat.RGB24,false);
        try {
            Canvas.ForceUpdateCanvases();RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
            RenderTexture.active=target;capture.ReadPixels(new Rect(0,0,1080,840),0,0);capture.Apply();
            File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/"+name),capture.EncodeToPNG());
        } finally {RenderTexture.active=previous;camera.targetTexture=null;target.Release();Object.Destroy(target);Object.Destroy(capture);Object.Destroy(host);}
    }
}
