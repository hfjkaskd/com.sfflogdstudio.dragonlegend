using System.Collections;
using System.Collections.Generic;
using System.IO;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

public sealed class RecoveredFreeColumnTests
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
    public IEnumerator FreeStartupCompletesOnStopFlagPredicateWhileStillSpinning()
    {
        var random=Random.state;var root=Create();
        try {
            int acc=0,stop=0;var free=root.MotionAt(3,1);
            var operation=free.StartSpin(1,index=>{Assert.AreEqual(3,index);acc++;},index=>{Assert.AreEqual(3,index);stop++;});
            Assert.AreEqual(1,acc);Assert.AreEqual(0,stop);Assert.IsFalse(operation.IsCompleted);
            yield return null;yield return null;
            Assert.IsTrue(operation.IsCompleted);operation.GetResult();Assert.AreEqual(1,stop);
            Assert.IsTrue(free.Movement.IsSpinning);Assert.IsFalse(free.Movement.StopRequested);
        } finally {Object.Destroy(root.gameObject);Random.state=random;}
    }
    [UnityTest]
    public IEnumerator EveryRowStopRepositionsTheEntireColumnsRegisteredEffects()
    {
        var random=Random.state;var root=Create();
        try {
            var column=root.ColumnAt(2);int completed=0;
            for(int row=0;row<3;row++){var free=root.MotionAt(2,row);free.Movement.enabled=false;free.StartSlotSpin(0);}
            column.StopSpin(0,index=>{Assert.AreEqual(2,index);completed++;});
            yield return null;yield return null;
            for(int row=0;row<3;row++) {
                var free=root.MotionAt(2,row);Assert.IsTrue(free.Movement.StopRequested);
                free.Movement.AdvanceMotion(0);free.Reel.SetOffsetPixels(-50*(row+1));free.Movement.AdvanceMotion(0);
            }
            root.MotionAt(2,0).Movement.AdvanceMotion(1);yield return null;
            Assert.AreEqual(0,completed);
            for(int row=0;row<3;row++) {
                var reel=root.At(2,row);var coin=(RecoveredFreeCoin)root.Specials.StoppedAt(reel);
                Assert.AreSame(column.ResultLayer,coin.transform.parent);Assert.IsNull(coin.Clipping.Boundary);
                Assert.That(Vector3.Distance(coin.transform.position,reel.SymbolAt(0).Symbol.transform.position),Is.LessThan(.00001f));
            }
            var moving=root.At(2,1);var effect=(RecoveredFreeCoin)root.Specials.StoppedAt(moving);var fixedPosition=effect.transform.position;
            moving.SetOffsetPixels(-20);Assert.AreEqual(fixedPosition,effect.transform.position);
            // Reparented result effects are not GetComponentInChildren hits for slot cleanup.
            root.Specials.ClearCoins(moving);Assert.IsTrue(effect.gameObject.activeInHierarchy);
            root.MotionAt(2,1).Movement.AdvanceMotion(1);yield return null;
            Assert.AreEqual(0,completed);Assert.That(Vector3.Distance(effect.transform.position,moving.SymbolAt(0).Symbol.transform.position),Is.LessThan(.00001f));
            root.MotionAt(2,2).Movement.AdvanceMotion(1);yield return null;Assert.AreEqual(1,completed);
            var lastScale=effect.transform.localScale;
            root.MotionAt(2,1).StartSlotSpin(1);
            Assert.AreSame(moving.SymbolAt(0).transform,effect.transform.parent);Assert.AreEqual(lastScale,effect.transform.localScale);
            Assert.AreSame(moving.SymbolAt(0).EffectClip,effect.Clipping.Boundary);
            moving.Refresh(689,1,true);Assert.IsFalse(effect.gameObject.activeSelf);Assert.IsNull(root.Specials.StoppedAt(moving));
        } finally {Object.Destroy(root.gameObject);Random.state=random;}
    }
    [UnityTest]
    public IEnumerator FiveColumnsScheduleNativeDelaysAndFinishOnceEach()
    {
        var random=Random.state;float captureDelta=Time.captureDeltaTime;var root=Create();
        try {
            Time.captureDeltaTime=.02f;int ignoredAcc=0;var callbacks=new int[5];var firstStopTime=new float[5];
            float start=Time.time;
            for(int col=0;col<5;col++)root.ColumnAt(col).StartSpin(.1f,index=>ignoredAcc++,index=>callbacks[index]++);
            float deadline=Time.realtimeSinceStartup+5;
            while(Time.realtimeSinceStartup<deadline) {
                bool done=true;
                for(int col=0;col<5;col++) {
                    if(firstStopTime[col]==0&&root.MotionAt(col,0).Movement.StopRequested)firstStopTime[col]=Time.time-start;
                    done&=callbacks[col]==1;
                }
                if(done)break;yield return null;
            }
            Assert.AreEqual(0,ignoredAcc);
            for(int col=0;col<5;col++) {
                Assert.AreEqual(1,callbacks[col]);Assert.That(firstStopTime[col],Is.InRange(.5f+col*.15f-.021f,.5f+col*.15f+.041f));
                for(int row=0;row<3;row++) {
                    Assert.IsFalse(root.MotionAt(col,row).Movement.IsSpinning);
                    Assert.AreSame(root.ColumnAt(col).ResultLayer,root.Specials.StoppedAt(root.At(col,row)).transform.parent);
                }
            }
            Capture();
        } finally {Time.captureDeltaTime=captureDelta;Object.Destroy(root.gameObject);Random.state=random;}
    }
    private static void Capture()
    {
        var host=new GameObject("Current Free columns",typeof(Camera));var camera=host.GetComponent<Camera>();
        camera.orthographic=true;camera.orthographicSize=3.6f;camera.transform.position=new Vector3(0,0,-10);camera.cullingMask=33;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
        var target=new RenderTexture(1080,720,24);camera.targetTexture=target;var previous=RenderTexture.active;
        var capture=new Texture2D(1080,720,TextureFormat.RGB24,false);
        try {
            Canvas.ForceUpdateCanvases();RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
            RenderTexture.active=target;capture.ReadPixels(new Rect(0,0,1080,720),0,0);capture.Apply();
            File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/current-free-columns-stopped.png"),capture.EncodeToPNG());
        } finally {RenderTexture.active=previous;camera.targetTexture=null;target.Release();Object.Destroy(target);Object.Destroy(capture);Object.Destroy(host);}
    }
}
