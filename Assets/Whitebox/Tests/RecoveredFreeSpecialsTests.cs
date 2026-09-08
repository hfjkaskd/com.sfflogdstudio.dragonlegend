using System.Collections;
using System.Collections.Generic;
using System.IO;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

public sealed class RecoveredFreeSpecialsTests
{
    private static List<int> Weights(int count){var w=new List<int>();for(int i=0;i<=count;i++)w.Add(i==count?1000000:0);return w;}
    private static RecoveredFreeSpinResult Result(int coins,int balls)
    {
        var rules=new RecoveredGameplayRules(new GoldenDragonAutoGenConfig {Rrggiomg=new RrggiomgPoro {
            QoinOmoinrKgiitr=Weights(coins),RollOmoinrKgiitr=Weights(balls),RollRipgKgiitr=new List<int>{1,1,1}}});
        var result=new RecoveredFreeSpinResult(rules);result.Begin(new[]{0,1,2,3,4,5,6});
        int tries=0;while(result.IsGenerating&&tries++<10000)result.Step();Assert.IsFalse(result.IsGenerating);return result;
    }
    [Test]
    public void ActualInitializationCreatesAndReusesSpecialsOnlyWithinRequestedReel()
    {
        var state=Random.state;var root=Object.Instantiate(Resources.Load<RecoveredFreeReels>("RecoveredSymbols/FreeReels"));
        try {
            Random.InitState(938);var result=Result(6,7);root.Initialize(Resources.Load<RecoveredSymbolCatalog>("RecoveredSymbols/OriginalSymbolCatalog"),result);
            var pool=root.Specials;Assert.AreEqual(6,pool.ActiveCoins);Assert.AreEqual(7,pool.ActiveBalls);Assert.AreEqual(7,pool.BallIndex);
            RecoveredReelView coinReel=null,ballReel=null;int ballIndex=0;
            for(int col=0;col<5;col++)for(int row=0;row<3;row++) {
                var reel=root.At(col,row);var slot=reel.SymbolAt(0);int id=result.GetSymbol(col,row);
                if(id==9) {
                    coinReel=reel;var coin=pool.CoinAt(slot);Assert.IsNotNull(coin);Assert.IsTrue(coin.Art.Player.IsPlaying("idle_chun"));
                    Assert.That(Vector3.Distance(slot.Symbol.transform.position,coin.transform.position),Is.LessThan(.00001f));
                } else if(id==11) {
                    ballReel=reel;var ball=pool.BallAt(slot);Assert.IsNotNull(ball);Assert.AreEqual(result.GetBall(ballIndex++),ball.BallType);
                    Assert.That(Vector3.Distance(slot.Symbol.transform.position,ball.transform.position),Is.LessThan(.00001f));
                }
                Assert.AreEqual(id!=9&&id!=11,slot.Symbol.gameObject.activeSelf);
            }
            var oldCoin=pool.CoinAt(coinReel.SymbolAt(0));pool.ClearCoins(coinReel);
            Assert.AreEqual(5,pool.ActiveCoins);Assert.AreEqual(7,pool.ActiveBalls);Assert.IsTrue(coinReel.SymbolAt(0).Symbol.gameObject.activeSelf);
            var reusedCoin=pool.CreateCoin(coinReel.SymbolAt(2),false);Assert.AreSame(oldCoin,reusedCoin);Assert.AreEqual(6,pool.CreatedCoins);
            Assert.IsTrue(reusedCoin.Art.Player.IsPlaying("zcjb_chuxian"));Assert.IsFalse(reusedCoin.Reward.gameObject.activeSelf);
            var oldBall=pool.BallAt(ballReel.SymbolAt(0));pool.ClearBalls(ballReel);Assert.AreEqual(6,pool.ActiveBalls);
            var reusedBall=pool.CreateBall(ballReel.SymbolAt(1),false);Assert.AreSame(oldBall,reusedBall);Assert.AreEqual(7,pool.CreatedBalls);
            Assert.AreEqual(1,pool.BallIndex);Assert.AreEqual(result.GetBall(0),reusedBall.BallType);
            Assert.AreEqual(6,pool.ActiveCoins);Assert.AreEqual(7,pool.ActiveBalls);
        } finally {Object.DestroyImmediate(root.gameObject);Random.state=state;}
    }
    [Test]
    public void SecondWrapUsesFreshDistributionThenExactlyOneSlotDrawAndReusesCoin()
    {
        var state=Random.state;var root=Object.Instantiate(Resources.Load<RecoveredFreeReels>("RecoveredSymbols/FreeReels"));
        try {
            Random.InitState(91);var result=Result(15,0);var catalog=Resources.Load<RecoveredSymbolCatalog>("RecoveredSymbols/OriginalSymbolCatalog");
            root.Initialize(catalog,result);var reel=root.At(0,0);var original=root.Specials.CoinAt(reel.SymbolAt(0));
            Random.InitState(971);
            for(int i=0;i<8;i++)Random.Range(0,catalog.ModeCount(RecoveredSlotType.Free));
            Random.Range(4,6);result.GetRandomEffectShow();int expectedSlot=Random.Range(0,4);int next=Random.Range(0,1000000);
            Random.InitState(971);reel.Refresh(689,1,true);
            Assert.AreEqual(14,root.Specials.ActiveCoins);Assert.IsFalse(original.gameObject.activeSelf);
            reel.Refresh(688,1,true);
            Assert.AreEqual(15,root.Specials.ActiveCoins);Assert.AreEqual(15,root.Specials.CreatedCoins);
            Assert.AreSame(original,root.Specials.CoinAt(reel.SymbolAt(expectedSlot)));
            Assert.AreEqual(next,Random.Range(0,1000000));
            Assert.IsTrue(original.Art.Player.IsPlaying("zcjb_chuxian"));
        } finally {Object.DestroyImmediate(root.gameObject);Random.state=state;}
    }
    [UnityTest]
    public IEnumerator MovingSpecialsAndLabelsStayInsideTheirOwnReels()
    {
        var state=Random.state;var root=Object.Instantiate(Resources.Load<RecoveredFreeReels>("RecoveredSymbols/FreeReels"));
        var host=new GameObject("Moving Free clipping",typeof(Camera));var camera=host.GetComponent<Camera>();
        camera.orthographic=true;camera.orthographicSize=5;camera.transform.position=new Vector3(0,0,-10);camera.cullingMask=33;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
        var target=new RenderTexture(1200,1000,24);camera.targetTexture=target;var previous=RenderTexture.active;
        var capture=new Texture2D(1200,1000,TextureFormat.RGB24,false);
        try {
            Random.InitState(938);root.Initialize(Resources.Load<RecoveredSymbolCatalog>("RecoveredSymbols/OriginalSymbolCatalog"),Result(6,7));
            foreach(var sprite in root.GetComponentsInChildren<SpriteRenderer>(true))sprite.enabled=false;
            foreach(var art in root.GetComponentsInChildren<RecoveredWorldAnimation>()) {art.Player.Stop();art.Rig.Sample(.173f);}
            yield return null;
            int initial=CaptureBright(camera,target,capture,"current-free-clipping-initial.png");Assert.Greater(initial,10000);
            for(int col=0;col<5;col++)for(int row=0;row<3;row++)root.At(col,row).SetOffsetPixels(86);
            yield return null;
            int partial=CaptureBright(camera,target,capture,"current-free-clipping-half.png");Assert.Greater(partial,1000);Assert.Less(partial,initial);
            // Every sibling SpriteMask stays enabled: none may reveal a neighbour's effect.
            for(int col=0;col<5;col++)for(int row=0;row<3;row++)root.At(col,row).SetOffsetPixels(344);
            yield return null;
            Assert.AreEqual(0,CaptureBright(camera,target,capture,"current-free-clipping-outside.png"));
            foreach(var mesh in root.GetComponentsInChildren<MeshRenderer>())mesh.enabled=false;
            for(int col=0;col<5;col++)for(int row=0;row<3;row++)root.At(col,row).SetOffsetPixels(0);
            yield return null;
            Assert.Greater(CaptureBright(camera,target,capture,"current-free-clipping-labels.png"),100);
            // Parent motion/scale must not leave a stale world-space clip rectangle.
            root.transform.localScale=new Vector3(.8f,1.1f,1);root.transform.rotation=Quaternion.Euler(0,0,13);
            root.transform.position=new Vector3(.2f,.3f,0);
            for(int col=0;col<5;col++)for(int row=0;row<3;row++)root.At(col,row).SetOffsetPixels(344);
            yield return null;
            Assert.AreEqual(0,CaptureBright(camera,target,capture,"current-free-clipping-labels-outside.png"));
            // Move a pooled effect between two independently transformed reels.
            RecoveredReelView source=null;
            for(int col=0;col<5;col++)for(int row=0;row<3;row++)if(root.Specials.CoinAt(root.At(col,row).SymbolAt(0))!=null)source=root.At(col,row);
            var coin=root.Specials.CoinAt(source.SymbolAt(0));root.Specials.ClearCoins(source);
            var destination=root.At(0,0);var reused=root.Specials.CreateCoin(destination.SymbolAt(2),false);
            Assert.AreSame(coin,reused);Assert.AreSame(destination.SymbolAt(0).EffectClip,reused.Clipping.Boundary);
        } finally {Random.state=state;RenderTexture.active=previous;camera.targetTexture=null;target.Release();Object.Destroy(target);Object.Destroy(capture);Object.Destroy(host);Object.Destroy(root.gameObject);}
    }
    private static int CaptureBright(Camera camera,RenderTexture target,Texture2D capture,string filename)
    {
        Canvas.ForceUpdateCanvases();
        RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});RenderTexture.active=target;
        capture.ReadPixels(new Rect(0,0,target.width,target.height),0,0);capture.Apply();
        File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/"+filename),capture.EncodeToPNG());
        int bright=0;foreach(var pixel in capture.GetPixels32())if(pixel.r>10||pixel.g>10||pixel.b>10)bright++;return bright;
    }
    [UnityTest]
    public IEnumerator CurrentInitializedFreeBoardRendersActualCoinsAndBalls()
    {
        var state=Random.state;var root=Object.Instantiate(Resources.Load<RecoveredFreeReels>("RecoveredSymbols/FreeReels"));
        var host=new GameObject("Current Free special board",typeof(Camera));var camera=host.GetComponent<Camera>();
        camera.orthographic=true;camera.orthographicSize=3.6f;camera.transform.position=new Vector3(0,0,-10);camera.cullingMask=33;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
        var target=new RenderTexture(1080,720,24);camera.targetTexture=target;var previous=RenderTexture.active;
        var capture=new Texture2D(1080,720,TextureFormat.RGB24,false);
        try {
            Random.InitState(938);root.Initialize(Resources.Load<RecoveredSymbolCatalog>("RecoveredSymbols/OriginalSymbolCatalog"),Result(6,7));
            yield return null;
            RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});RenderTexture.active=target;
            capture.ReadPixels(new Rect(0,0,1080,720),0,0);capture.Apply();
            File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/current-free-board-specials.png"),capture.EncodeToPNG());
            int bright=0;foreach(var pixel in capture.GetPixels32())if(pixel.r>80||pixel.g>80||pixel.b>80)bright++;Assert.Greater(bright,20000);
        } finally {Random.state=state;RenderTexture.active=previous;camera.targetTexture=null;target.Release();Object.Destroy(target);Object.Destroy(capture);Object.Destroy(host);Object.Destroy(root.gameObject);}
    }
}
