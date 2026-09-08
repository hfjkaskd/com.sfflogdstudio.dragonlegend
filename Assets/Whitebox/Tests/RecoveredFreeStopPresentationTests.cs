using System.Collections;
using System.Collections.Generic;
using System.IO;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

public sealed class RecoveredFreeStopPresentationTests
{
    private static List<int> Weights(int count){var values=new List<int>();for(int i=0;i<=count;i++)values.Add(i==count?1000000:0);return values;}
    private static RecoveredFreeReels Create(out RecoveredFreeSpinResult result)
    {
        result=new RecoveredFreeSpinResult(new RecoveredGameplayRules(new GoldenDragonAutoGenConfig{Rrggiomg=new RrggiomgPoro{
            QoinOmoinrKgiitr=Weights(6),RollOmoinrKgiitr=Weights(7),RollRipgKgiitr=new List<int>{1,1,1}}}));
        result.Begin(new[]{3});while(result.IsGenerating)result.Step();
        var root=Object.Instantiate(Resources.Load<RecoveredFreeReels>("RecoveredSymbols/FreeReels"));
        root.Initialize(Resources.Load<RecoveredSymbolCatalog>("RecoveredSymbols/OriginalSymbolCatalog"),result);return root;
    }
    [Test]
    public void StopPresentationUsesOnlyLandedSpecialsAndEmitsSoundThenVibrationInRowOrder()
    {
        var random=Random.state;Random.InitState(414);var root=Create(out var result);
        try {
            var actual=new List<string>();var expected=new List<string>();
            root.Specials.SoundRequested+=sound=>actual.Add(sound);root.Specials.VibrationRequested+=ms=>actual.Add("v"+ms);
            for(int col=0;col<5;col++)root.ColumnAt(col).PlayStopAnimation();Assert.IsEmpty(actual);
            for(int col=0;col<5;col++)for(int row=0;row<3;row++)root.Specials.ApplyStoppedResult(root.At(col,row),col,row);
            for(int col=0;col<5;col++) {
                for(int row=0;row<3;row++) {
                    int id=result.GetSymbol(col,row);
                    if(id==9||id==11){expected.Add(id==9?"coinshow":"scatterShow");expected.Add("v200");}
                }
                root.ColumnAt(col).PlayStopAnimation();
                for(int row=0;row<3;row++) {
                    var effect=root.Specials.StoppedAt(root.At(col,row));
                    if(effect is RecoveredFreeCoin coin)Assert.IsTrue(coin.Art.Player.IsPlaying("zcjb_chuxian"));
                    else if(effect is RecoveredFreeBall ball)Assert.IsTrue(ball.Art.Player.IsPlaying("start_"+(ball.BallType==0?"zi":ball.BallType==1?"lan":"lv")));
                }
            }
            CollectionAssert.AreEqual(expected,actual);Assert.AreEqual(26,actual.Count);
        } finally {Object.DestroyImmediate(root.gameObject);Random.state=random;}
    }
    [Test]
    public void CoinReplayRetainsPriorScaleTweenAndDefersCallbackCreatedReturns()
    {
        var coin=Object.Instantiate(Resources.Load<RecoveredFreeCoin>("RecoveredSymbols/FreeCoin"));
        try {
            coin.enabled=false;coin.PlayShow();coin.AdvanceScale(.1f);Assert.AreEqual(.925f,coin.transform.localScale.x,.000001f);
            coin.PlayShow();coin.AdvanceScale(.1f);Assert.AreEqual(1,coin.transform.localScale.x,.000001f);
            coin.AdvanceScale(.1f);Assert.AreEqual(.775f,coin.transform.localScale.x,.000001f);
            coin.AdvanceScale(.1f);Assert.AreEqual(.7f,coin.transform.localScale.x,.000001f);Assert.IsTrue(coin.IsScaling);
            coin.AdvanceScale(.1f);Assert.IsFalse(coin.IsScaling);Assert.AreEqual(.7f,coin.transform.localScale.x,.000001f);
        } finally {Object.DestroyImmediate(coin.gameObject);}
    }
    [UnityTest]
    public IEnumerator ControllerStartsAllColumnsThenPresentsStopsBeforeRewardBoundary()
    {
        var random=Random.state;float delta=Time.captureDeltaTime;Random.InitState(414);var root=Create(out var result);
        try {
            Time.captureDeltaTime=.02f;var controller=root.Controller;int sounds=0,vibrations=0,reelSounds=0,shakes=0,done=0;
            var order=new List<string>();
            root.Specials.SoundRequested+=sound=>{Assert.That(sound,Is.EqualTo("coinshow").Or.EqualTo("scatterShow"));sounds++;order.Add(sound);};
            root.Specials.VibrationRequested+=ms=>{Assert.AreEqual(200,ms);vibrations++;order.Add("v");};
            controller.SoundRequested+=sound=>{Assert.AreEqual("reelstop",sound);reelSounds++;order.Add(sound);};
            controller.ShakeRequested+=()=>{Assert.AreEqual("reelstop",order[order.Count-1]);shakes++;order.Add("shake");};
            controller.ReelsStopped+=()=>{Assert.AreEqual(5,controller.StoppedCount);Assert.AreEqual(5,shakes);done++;};
            controller.Begin();
            for(int col=0;col<5;col++)for(int row=0;row<3;row++)Assert.IsTrue(root.MotionAt(col,row).Movement.IsSpinning);
            float deadline=Time.realtimeSinceStartup+5;
            while(controller.IsRunning&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.IsFalse(controller.IsRunning);Assert.IsNull(controller.Error);Assert.AreEqual(1,done);
            Assert.AreEqual(13,sounds);Assert.AreEqual(13,vibrations);Assert.AreEqual(5,reelSounds);
            Capture();
        } finally {Time.captureDeltaTime=delta;Object.Destroy(root.gameObject);Random.state=random;}
    }
    private static void Capture()
    {
        var host=new GameObject("Current Free stop presentation",typeof(Camera));var camera=host.GetComponent<Camera>();
        camera.orthographic=true;camera.orthographicSize=3.6f;camera.transform.position=new Vector3(0,0,-10);camera.cullingMask=33;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
        var target=new RenderTexture(1080,720,24);camera.targetTexture=target;var previous=RenderTexture.active;
        var capture=new Texture2D(1080,720,TextureFormat.RGB24,false);
        try {
            Canvas.ForceUpdateCanvases();RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
            RenderTexture.active=target;capture.ReadPixels(new Rect(0,0,1080,720),0,0);capture.Apply();
            File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/current-free-stop-presentation.png"),capture.EncodeToPNG());
        } finally {RenderTexture.active=previous;camera.targetTexture=null;target.Release();Object.Destroy(target);Object.Destroy(capture);Object.Destroy(host);}
    }
}
