using System.Collections;
using System.Collections.Generic;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class RecoveredFreeReelMotionTests
{
    private static List<int> Weights(int count){var values=new List<int>();for(int i=0;i<=count;i++)values.Add(i==count?1000000:0);return values;}
    private static RecoveredFreeSpinResult Result(int id)
    {
        var rules=new RecoveredGameplayRules(new GoldenDragonAutoGenConfig{Rrggiomg=new RrggiomgPoro{
            QoinOmoinrKgiitr=Weights(id==9?15:0),RollOmoinrKgiitr=Weights(id==11?15:0),RollRipgKgiitr=new List<int>{1,1,1}}});
        var result=new RecoveredFreeSpinResult(rules);result.Begin(new[]{3});
        while(result.IsGenerating)result.Step();return result;
    }
    [TestCase(3)] [TestCase(9)] [TestCase(11)]
    public void RealFreeLandingWaitsThenCreatesResultAndReturnsWithNativeOvershoot(int id)
    {
        var random=Random.state;var root=Object.Instantiate(Resources.Load<RecoveredFreeReels>("RecoveredSymbols/FreeReels"));
        try {
            Random.InitState(781);var result=Result(id);root.Initialize(Resources.Load<RecoveredSymbolCatalog>("RecoveredSymbols/OriginalSymbolCatalog"),result);
            var reel=root.At(2,1);var free=root.MotionAt(2,1);var motion=free.Movement;motion.enabled=false;
            var tail=new int[6];for(int i=0;i<6;i++)tail[i]=reel.SymbolId(i+1);
            var firstCoin=root.Specials.CoinAt(reel.SymbolAt(0));var firstBall=root.Specials.BallAt(reel.SymbolAt(0));
            Assert.IsTrue(free.StartSlotSpin(.1f));free.RequestStop();
            motion.AdvanceMotion(.05f);Assert.AreEqual(3535.534f,motion.CurrentSpeed,.001f);
            motion.AdvanceMotion(.05f);Assert.IsNull(root.Specials.StoppedAt(reel));
            Assert.AreEqual(-426.7767f,reel.OffsetPixels,.001f);
            motion.AdvanceMotion(.01f);
            for(int i=0;i<6;i++)Assert.AreEqual(tail[i],reel.SymbolId(i+1));
            if(id==9) {
                var coin=(RecoveredFreeCoin)root.Specials.StoppedAt(reel);Assert.AreSame(firstCoin,coin);
                Assert.IsTrue(coin.Art.Player.IsPlaying("zcjb_chuxian"));Assert.IsFalse(coin.Reward.gameObject.activeSelf);
                Assert.AreEqual(15,root.Specials.ActiveCoins);Assert.AreEqual(15,root.Specials.CreatedCoins);
            } else if(id==11) {
                var ball=(RecoveredFreeBall)root.Specials.StoppedAt(reel);Assert.AreSame(firstBall,ball);
                Assert.AreEqual(1,root.Specials.BallIndex);Assert.AreEqual(result.GetBall(0),ball.BallType);
                Assert.AreEqual(15,root.Specials.ActiveBalls);Assert.AreEqual(15,root.Specials.CreatedBalls);
            } else {
                Assert.AreEqual(3,reel.SymbolId(0));Assert.IsTrue(reel.SymbolAt(0).Cover.gameObject.activeSelf);
                Assert.IsNull(root.Specials.StoppedAt(reel));
            }
            Assert.AreEqual(.17071068f,motion.ReturnDuration,.00001f);
            motion.AdvanceMotion(motion.ReturnDuration*.5f);Assert.Greater(reel.OffsetPixels,0);
            Assert.IsTrue(motion.StopRequested);Assert.IsTrue(motion.IsSpinning);
            motion.AdvanceMotion(motion.ReturnDuration*.5f);Assert.AreEqual(0,reel.OffsetPixels);
            Assert.IsFalse(motion.IsSpinning);Assert.IsFalse(motion.StopRequested);
            // Clear the registered landed special before the next rolling pool cleanup.
            reel.Refresh(689,1,true);Assert.IsNull(root.Specials.StoppedAt(reel));
            if(id==9)Assert.AreEqual(14,root.Specials.ActiveCoins);
            if(id==11)Assert.AreEqual(14,root.Specials.ActiveBalls);
        } finally {Object.DestroyImmediate(root.gameObject);Random.state=random;}
    }
    private static void CaptureStoppedBoard()
    {
        var host=new GameObject("Current Free landing",typeof(Camera));var camera=host.GetComponent<Camera>();
        camera.orthographic=true;camera.orthographicSize=3.6f;camera.transform.position=new Vector3(0,0,-10);camera.cullingMask=33;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
        var target=new RenderTexture(1080,720,24);camera.targetTexture=target;var previous=RenderTexture.active;
        var capture=new Texture2D(1080,720,TextureFormat.RGB24,false);
        try {
            Canvas.ForceUpdateCanvases();
            UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,new UnityEngine.Rendering.RenderPipeline.StandardRequest{destination=target});
            RenderTexture.active=target;capture.ReadPixels(new Rect(0,0,1080,720),0,0);capture.Apply();
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(Application.dataPath,"../Artifacts/current-free-actual-stop.png"),capture.EncodeToPNG());
        } finally {RenderTexture.active=previous;camera.targetTexture=null;target.Release();Object.Destroy(target);Object.Destroy(capture);Object.Destroy(host);}
    }
    [UnityTest]
    public IEnumerator DelayedStopCallsBackOnlyAfterFreeLandingAndReturn()
    {
        var random=Random.state;var root=Object.Instantiate(Resources.Load<RecoveredFreeReels>("RecoveredSymbols/FreeReels"));
        float scale=Time.timeScale;
        try {
            Random.InitState(912);root.Initialize(Resources.Load<RecoveredSymbolCatalog>("RecoveredSymbols/OriginalSymbolCatalog"),Result(9));
            var reel=root.At(0,0);var motion=root.MotionAt(0,0);int callbacks=0;
            motion.StartSlotSpin(.1f);
            var operation=motion.SetStop(.12f,actual=>{Assert.AreSame(reel,actual);Assert.AreEqual(0,reel.OffsetPixels);Assert.IsFalse(motion.Movement.StopRequested);Assert.IsNotNull(root.Specials.StoppedAt(reel));callbacks++;});
            Time.timeScale=0;yield return null;yield return null;Assert.IsFalse(operation.IsCompleted);Assert.AreEqual(0,callbacks);
            Time.timeScale=1;
            float deadline=Time.realtimeSinceStartup+5;
            while(!operation.IsCompleted&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.IsTrue(operation.IsCompleted);operation.GetResult();Assert.AreEqual(1,callbacks);
            CaptureStoppedBoard();
        } finally {Time.timeScale=scale;Object.Destroy(root.gameObject);Random.state=random;}
    }
}
