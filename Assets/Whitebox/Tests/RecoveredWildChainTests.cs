using System.Collections;
using System.Collections.Generic;
using System.IO;
using DragonLegend.Whitebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

public sealed class RecoveredWildChainTests
{
    [UnityTest]
    public IEnumerator ScanReadsLiveColumnsAcrossGapsAndWaitsAfterTheLastMatch()
    {
        float scale=Time.timeScale,delta=Time.captureDeltaTime;var sequence=new RecoveredWildSequence(.36f);
        var matches=new[]{true,false,true,false,false};var columns=new List<int>();var times=new List<float>();int reads=0,complete=0;
        try {
            Time.timeScale=1;Time.captureDeltaTime=.05f;
            sequence.Begin((c,r)=>{reads++;return matches[c]?7:0;},c=>{columns.Add(c);times.Add(Time.time);matches[4]=true;},count=>{Assert.AreEqual(3,count);complete++;times.Add(Time.time);});
            Assert.AreEqual(1,sequence.Count);Assert.AreEqual(3,reads);Assert.AreEqual(0,complete);
            Time.timeScale=0;for(int i=0;i<12;i++)yield return null;Assert.AreEqual(3,reads);
            Time.timeScale=1;for(int i=0;i<40&&sequence.IsRunning;i++)yield return null;
            CollectionAssert.AreEqual(new[]{0,2,4},columns);Assert.AreEqual(15,reads);Assert.AreEqual(1,complete);
            for(int i=1;i<times.Count;i++)Assert.GreaterOrEqual(times[i]-times[i-1],.36f-.0001f);
            sequence.Begin((c,r)=>0,c=>Assert.Fail("No Wild"),count=>{Assert.AreEqual(0,count);complete++;});
            Assert.AreEqual(2,complete);Assert.IsFalse(sequence.IsRunning);
            sequence.Begin((c,r)=>7,c=>sequence.Cancel(),count=>Assert.Fail("Cancelled presentation"));
            for(int i=0;i<12;i++)yield return null;Assert.AreEqual(1,sequence.Count);Assert.IsFalse(sequence.IsRunning);
        } finally {sequence.Cancel();Time.timeScale=scale;Time.captureDeltaTime=delta;}
    }

    [UnityTest]
    public IEnumerator ActualSpinRunsWildPresentationShakeIndependentLightsAndReelPoolOwnership()
    {
        string key=RecoveredPlayerStore.OriginalKey;bool had=PlayerPrefs.HasKey(key);string saved=PlayerPrefs.GetString(key);
        var random=Random.state;float scale=Time.timeScale,delta=Time.captureDeltaTime;PlayerPrefs.DeleteKey(key);
        var root=Object.Instantiate(Resources.Load<GameObject>("Whitebox/GameEntry"));var entry=root.GetComponent<GameEntry>();
        var cameraHost=new GameObject("Actual Wild chain camera",typeof(Camera));var camera=cameraHost.GetComponent<Camera>();
        camera.transform.position=new Vector3(100,0,-10);camera.orthographic=true;camera.orthographicSize=5;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.cullingMask=32;
        var target=new RenderTexture(1080,1920,24);camera.targetTexture=target;root.GetComponent<Canvas>().worldCamera=camera;
        var previous=RenderTexture.active;Texture2D capture=null;
        try {
            Time.timeScale=1;Time.captureDeltaTime=.05f;
            for(int i=0;i<200&&entry.Playfield==null;i++)yield return null;
            var field=entry.Playfield;Assert.IsNotNull(field);var presenter=field.Wilds;var shake=presenter.Shake;
            Assert.AreEqual("QiPan",shake.Target.name);Assert.AreEqual("Result",presenter.transform.parent.name);
            var result=(RectTransform)presenter.transform.parent;Assert.AreEqual(new Vector2(.003418f,335.14f),result.anchoredPosition);
            Assert.AreEqual(new Vector2(1080,1919.72f),result.sizeDelta);
            int presented=0,sounds=0,vibrations=0,jackpot=0,maxLights=0;bool scanCompleted=false;float began=0;
            var times=new List<float>();int before=entry.PlayerProgress.SpinCount;float balance=entry.PlayerProgress.GreenCount;
            field.WildColumnsCheckRequested+=()=>{began=Time.time;Assert.IsFalse(field.BonusCoins.IsRunning);};
            presenter.SoundRequested+=sound=>{Assert.AreEqual("change",sound);sounds++;};
            presenter.VibrationRequested+=duration=>{Assert.AreEqual(200,duration);vibrations++;};
            presenter.Presented+=column=>{
                if(scanCompleted)return;
                Assert.AreEqual(presented,column);presented++;times.Add(Time.time);
                Assert.AreEqual(presented,field.WildSequence.Count);Assert.AreEqual(presented,presenter.ActiveCount);
                Assert.IsTrue(shake.IsShaking);Assert.IsTrue(field.IsBusy);maxLights=Mathf.Max(maxLights,presenter.ActiveLightCount);
                var value=presenter.ColumnAt(column);Assert.IsNotNull(value);
                Assert.That(Vector3.Distance(value.transform.position,field.Reels.ReelAt(column).SymbolAt(1).Symbol.transform.position),Is.LessThan(.00001f));
                for(int row=0;row<3;row++)Assert.IsFalse(field.Reels.ReelAt(column).SymbolAt(row).Symbol.gameObject.activeSelf);
                float t=Time.deltaTime/.3f,weight=1-3*t*t+2*t*t*t;
                var expected=shake.OriginalPosition+new Vector2((Mathf.PerlinNoise(Time.time*20,0)*2-1)*20*weight,(Mathf.PerlinNoise(0,Time.time*20)*2-1)*20*weight);
                Assert.That(Vector2.Distance(expected,shake.Target.anchoredPosition),Is.LessThan(.001f));
            };
            field.JackpotCheckRequested+=count=>{
                Assert.AreEqual(5,count);Assert.AreEqual(5,presented);Assert.IsFalse(field.WildSequence.IsRunning);
                Assert.Greater(presenter.ActiveLightCount,0,"Scan must not await the independent .8 second light");
                Assert.GreaterOrEqual(Time.time-times[4],.36f-.0001f);jackpot++;scanCompleted=true;
            };
            field.SpinButton.Button.onClick.Invoke();Assert.IsTrue(field.IsBusy);Assert.AreEqual(before-1,entry.PlayerProgress.SpinCount);
            // Invoke the real guaranteed-Wild board stage before the moving reels read their final rows.
            entry.SpinResult.Board.ApplyGuaranteedWildIndex(2);
            for(int i=0;i<300&&presented<3;i++)yield return null;
            Assert.AreEqual(3,presented);Assert.IsNull(field.Error);Time.timeScale=0;
            for(int i=0;i<4;i++)yield return null;var pausedPosition=shake.Target.anchoredPosition;
            for(int i=0;i<10;i++)yield return null;
            Assert.AreEqual(3,presented);Assert.AreEqual(0,jackpot);Assert.AreEqual(pausedPosition,shake.Target.anchoredPosition);
            Canvas.ForceUpdateCanvases();RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
            RenderTexture.active=target;capture=new Texture2D(1080,1920,TextureFormat.RGB24,false);capture.ReadPixels(new Rect(0,0,1080,1920),0,0);capture.Apply();
            File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/current-wild-chain.png"),capture.EncodeToPNG());
            var pixels=capture.GetPixels32();int gold=0;
            for(int y=350;y<850;y++)for(int x=0;x<650;x++){var p=pixels[y*1080+x];if(p.r>120&&p.g>65&&p.b<150)gold++;}
            Assert.Greater(gold,10000,"The actual entry camera must render the native Wild chain");
            Time.timeScale=1;for(int i=0;i<80&&jackpot==0;i++)yield return null;
            Assert.AreEqual(1,jackpot);Assert.AreEqual(5,sounds);Assert.AreEqual(5,vibrations);Assert.LessOrEqual(maxLights,3);
            Assert.GreaterOrEqual(times[0],began);for(int i=1;i<5;i++)Assert.GreaterOrEqual(times[i]-times[i-1],.36f-.0001f);
            for(int i=0;i<30&&presenter.ActiveLightCount>0;i++)yield return null;
            Assert.AreEqual(0,presenter.ActiveLightCount);Assert.AreEqual(5,presenter.ActiveCount);Assert.IsFalse(shake.IsShaking);
            Assert.AreEqual(shake.OriginalPosition,shake.Target.anchoredPosition);Assert.AreEqual(balance,entry.PlayerProgress.GreenCount);
            Assert.IsTrue(field.IsBusy);Assert.IsTrue(field.AwaitingRewards);
            field.SpinButton.Button.onClick.Invoke();Assert.AreEqual(before-1,entry.PlayerProgress.SpinCount);
            var pooled=presenter.ColumnAt(0);var player=pooled.PlayerAt(0);float phase=player[player.clip.name].time;
            var reel=field.Reels.ReelAt(0);reel.Refresh(100000,.1f,false);Assert.IsFalse(pooled.gameObject.activeSelf);Assert.AreEqual(4,presenter.ActiveCount);
            reel.ApplyBaseColumn(new[]{7,7,7});presenter.Present(0);
            Assert.AreSame(pooled,presenter.ColumnAt(0));Assert.AreEqual(5,presenter.CreatedCount);
            Assert.AreEqual(phase,player[player.clip.name].time,.0001f,"Pool reuse retains the original idle phase");
            presenter.Present(0);Assert.AreEqual(6,sounds,"Repeated ShowSymbolEffect must not spawn twice");
            reel.ApplyBaseColumn(new[]{9,9,9});field.CoinStops.ShowColumn(0);
            Assert.AreEqual(0,field.CoinStops.ActiveCount,"Coin and Wild effects share the native shown-slot set");
            field.Unbind();Assert.AreEqual(0,presenter.ActiveCount);Assert.AreEqual(0,presenter.ActiveLightCount);
            Assert.AreEqual(shake.OriginalPosition,shake.Target.anchoredPosition);
            for(int i=0;i<30;i++)yield return null;Assert.AreEqual(1,jackpot);Assert.AreEqual(6,sounds);
        } finally {
            Time.timeScale=scale;Time.captureDeltaTime=delta;Random.state=random;
            Object.Destroy(root);Object.Destroy(cameraHost);RenderTexture.active=previous;target.Release();Object.Destroy(target);if(capture!=null)Object.Destroy(capture);
            if(had)PlayerPrefs.SetString(key,saved);else PlayerPrefs.DeleteKey(key);PlayerPrefs.Save();
        }
    }
}
