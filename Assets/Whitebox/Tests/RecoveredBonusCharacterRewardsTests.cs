using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

public sealed class RecoveredBonusCharacterRewardsTests
{
    [UnityTest]
    public IEnumerator ConcurrentFlightsReleaseAtNativeStagesAndSnapshotAfterTargetEffect()
    {
        var host=new GameObject("Bonus character test",typeof(RectTransform),typeof(Canvas));
        host.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;
        var cameraRoot=new GameObject("Bonus character current camera",typeof(Camera));var camera=cameraRoot.GetComponent<Camera>();
        camera.transform.position=new Vector3(0,0,-10);camera.orthographic=true;camera.orthographicSize=360;camera.cullingMask=33;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;host.GetComponent<Canvas>().worldCamera=camera;
        var texture=new RenderTexture(720,720,24);camera.targetTexture=texture;var previous=RenderTexture.active;
        var captureTexture=new Texture2D(720,720,TextureFormat.RGB24,false);
        Action<string> captureFrame=name=>{
            Canvas.ForceUpdateCanvases();RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=texture});
            RenderTexture.active=texture;captureTexture.ReadPixels(new Rect(0,0,720,720),0,0);captureTexture.Apply();
            File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/current-bonus-character-"+name+".png"),captureTexture.EncodeToPNG());
        };
        var effects=Object.Instantiate(Resources.Load<RecoveredBonusCharacterRewards>("RecoveredUI/BonusCharacterRewards"),host.transform);
        var first=Object.Instantiate(Resources.Load<RecoveredBonusItemTurn>("RecoveredUI/BonusItem"),host.transform);
        var second=Object.Instantiate(Resources.Load<RecoveredBonusItemTurn>("RecoveredUI/BonusItem"),host.transform);
        var targetA=new GameObject("Grand letter",typeof(RectTransform));targetA.transform.SetParent(host.transform,false);
        var targetB=new GameObject("Last Grand letter",typeof(RectTransform));targetB.transform.SetParent(host.transform,false);
        new GameObject("black",typeof(RectTransform)).transform.SetParent(targetA.transform,false);
        new GameObject("black",typeof(RectTransform)).transform.SetParent(targetB.transform,false);
        first.transform.localPosition=new Vector3(-180,-200,0);second.transform.localPosition=new Vector3(180,-200,0);
        targetA.transform.localPosition=new Vector3(-180,200,0);targetB.transform.localPosition=new Vector3(180,200,0);
        float timeScale=Time.timeScale,capture=Time.captureDeltaTime;
        try {
            Time.timeScale=1;Time.captureDeltaTime=.025f;yield return null;first.Initialize();second.Initialize();
            var rules=new RecoveredGameplayRules(new GoldenDragonAutoGenConfig {Rogk=new RogkPoro {
                Ip=new List<int>{2},RogkOmoinr=new List<int>{99},Rgkorp=new List<int>{10},Jimp=new List<int>{1} }});
            int saves=0,early=0,late=0,started=0,animated=0,vibrations=0,popups=0;
            var data=new PlayerData{GreenCount=42};var progress=new RecoveredPlayerProgress(rules,()=>saves++,data){GrandJackPotReward=100};
            Exception error=null;effects.Failed+=e=>error=e;first.Failed+=e=>error=e;second.Failed+=e=>error=e;
            var sounds=new List<string>();effects.SoundRequested+=s=>sounds.Add(s);
            effects.VibrationRequested+=ms=>{Assert.AreEqual(200,ms);vibrations++;};
            effects.JackpotAnimationRequested+=type=>{Assert.AreEqual(RecoveredJackpotType.Grand,type);animated++;progress.GrandJackPotReward=321;};
            effects.FlightStarted+=flight=>{
                started++;Assert.AreEqual(started<=2?1:2,early);Assert.AreEqual(started<=2?0:1,late);
                Assert.AreEqual(Vector3.one,flight.transform.localScale);
                Assert.That(Vector3.Distance(flight.StartPoint,flight.transform.parent.TransformPoint(new Vector3(-1.87f,-2.1f,0))),Is.LessThan(.0001f));
                var expected=(flight.StartPoint+flight.EndPoint)*.5f+Vector3.up*Vector3.Distance(flight.StartPoint,flight.EndPoint)*.3f;
                Assert.That(Vector3.Distance(expected,flight.ControlPoint),Is.LessThan(.0001f));
                foreach(var renderer in flight.GetComponentsInChildren<Renderer>())Assert.AreEqual(43,renderer.sortingOrder);
            };
            Action<float> claim=null;float shown=0;double poppedAt=0;
            effects.JackpotPopupRequested+=(type,amount,callback)=>{Assert.AreEqual(RecoveredJackpotType.Grand,type);popups++;shown=amount;claim=callback;poppedAt=Time.timeAsDouble;};
            effects.Begin(first,new RecoveredBonusRound.Reveal(RecoveredBonusType.Zhao,RecoveredJackpotType.Grand,0,false),targetA.transform,progress,rules,0,42,()=>early++);
            effects.Begin(second,new RecoveredBonusRound.Reveal(RecoveredBonusType.Bao,RecoveredJackpotType.Grand,3,true),targetB.transform,progress,rules,0,42,()=>late++);
            Assert.AreEqual(1,saves);Assert.AreEqual(1,data.PlayerTaskDatas[0].count);Assert.AreEqual(0,early);
            for(int i=0;i<20&&effects.ActiveFlightCount<2;i++)yield return null;
            Assert.IsNull(error);Assert.AreEqual(2,effects.ActiveFlightCount);Assert.AreEqual(2,started);Assert.AreEqual(2,vibrations);
            Assert.IsTrue(targetA.transform.GetChild(0).gameObject.activeSelf);
            for(int i=0;i<4;i++)yield return null;
            captureFrame("flight");
            Time.timeScale=0;for(int i=0;i<8;i++)yield return null;Assert.AreEqual(2,effects.ActiveFlightCount);Assert.AreEqual(0,animated);
            Time.timeScale=1;for(int i=0;i<20&&animated<2;i++)yield return null;
            Assert.AreEqual(2,animated);Assert.AreEqual(0,effects.ActiveFlightCount);Assert.AreEqual(2,effects.ActiveFlashCount);
            Assert.IsFalse(targetA.transform.GetChild(0).gameObject.activeSelf);Assert.IsFalse(targetB.transform.GetChild(0).gameObject.activeSelf);
            captureFrame("arrival");
            for(int i=0;i<40&&effects.ActiveFlashCount>0;i++)yield return null;
            double flashEnd=Time.timeAsDouble;Assert.AreEqual(0,effects.ActiveFlashCount);Assert.AreEqual(1,effects.PendingCount);
            progress.GrandJackPotReward=999;Time.timeScale=0;for(int i=0;i<10;i++)yield return null;Assert.AreEqual(0,popups);
            Time.timeScale=1;for(int i=0;i<70&&popups==0;i++)yield return null;
            Assert.IsNull(error);Assert.AreEqual(1,popups);Assert.AreEqual(321,shown);
            Assert.That(poppedAt-flashEnd,Is.InRange(1.47,1.56));Assert.AreEqual(0,late);Assert.AreEqual(42,data.GreenCount);
            claim(-100);Assert.AreEqual(1,late);Assert.AreEqual(0,effects.PendingCount);Assert.AreEqual(42,data.GreenCount);
            CollectionAssert.AreEqual(new[]{"coinReveal","coinReveal","exp","exp"},sounds);
            first.Initialize();effects.Begin(first,new RecoveredBonusRound.Reveal(RecoveredBonusType.Cai,RecoveredJackpotType.None,-1,false),null,progress,rules,0,42,()=>early++);
            for(int i=0;i<20&&effects.PendingCount>0;i++)yield return null;
            Assert.AreEqual(2,early);Assert.AreEqual(2,started);
            second.Initialize();effects.Begin(second,new RecoveredBonusRound.Reveal(RecoveredBonusType.Bao,RecoveredJackpotType.Grand,3,true),targetB.transform,progress,rules,0,42,()=>late++);
            for(int i=0;i<20&&effects.ActiveFlightCount==0;i++)yield return null;
            effects.Cancel();Assert.AreEqual(0,effects.ActiveFlightCount);Assert.AreEqual(0,effects.PendingCount);
            for(int i=0;i<100;i++)yield return null;Assert.AreEqual(1,late);Assert.AreEqual(1,popups);Assert.IsNull(error);
        }finally{Time.timeScale=timeScale;Time.captureDeltaTime=capture;RenderTexture.active=previous;camera.targetTexture=null;texture.Release();Object.Destroy(texture);Object.Destroy(captureTexture);Object.Destroy(cameraRoot);Object.Destroy(host);}
    }
}
