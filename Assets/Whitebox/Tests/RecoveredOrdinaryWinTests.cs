using System.Collections;
using System.Collections.Generic;
using System.IO;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
using Random=UnityEngine.Random;

public sealed class RecoveredOrdinaryWinTests
{
    [Test]
    public void ZeroRewardSkipsCreditAndNegativeRewardKeepsTheNativeNoFlightCondition()
    {
        var root=Object.Instantiate(Resources.Load<GameObject>("RecoveredUI/SpinPlayfield"));var field=root.GetComponent<RecoveredSpinPlayfield>();
        int saves=0;var data=new PlayerData{GreenCount=100};
        var player=new RecoveredPlayerProgress(new RecoveredGameplayRules(new GoldenDragonAutoGenConfig()),()=>saves++,data);
        try {
            field.SymbolAmount.Bind(0);field.SymbolAmount.Label.text="unchanged";bool completed=false;
            field.OrdinaryWin.Begin(0,()=>{Assert.Fail("No bonus read for zero total");return 0;},player,()=>completed=true);
            Assert.IsTrue(completed);Assert.AreEqual(0,saves);Assert.AreEqual(100,data.GreenCount);
            field.OrdinaryWin.Begin(-2,()=>5,player,()=>Assert.Fail("Must await .8 seconds"));
            Assert.AreEqual(98,data.GreenCount);Assert.AreEqual(2,saves);Assert.IsFalse(field.OrdinaryWin.IsFlying);
            Assert.AreEqual(-2,field.DownWin.TemporaryTotal);Assert.AreEqual("unchanged",field.SymbolAmount.Label.text);
            field.OrdinaryWin.Cancel();
        } finally {Object.DestroyImmediate(root);}
    }
    [UnityTest]
    public IEnumerator FlightUsesScaledBezierRestoresTextAndLeavesStoredDownWinCountUnchanged()
    {
        var root=Object.Instantiate(Resources.Load<GameObject>("RecoveredUI/SpinPlayfield"));var field=root.GetComponent<RecoveredSpinPlayfield>();
        var sequence=field.OrdinaryWin;float scale=Time.timeScale,delta=Time.captureDeltaTime;
        var data=new PlayerData{GreenCount=100};int saves=0;
        var player=new RecoveredPlayerProgress(new RecoveredGameplayRules(new GoldenDragonAutoGenConfig()),()=>saves++,data);
        var coin=new GameObject("Existing bonus award");
        try {
            Time.timeScale=1;Time.captureDeltaTime=.05f;field.SymbolAmount.Bind(0);field.DownWin.Bind(0);
            field.DownWin.Register(coin,5);field.DownWin.PresentationFinished(coin);
            for(int i=0;i<15&&field.DownWin.Total!=5;i++)yield return null;
            Assert.AreEqual(5,field.DownWin.Total);field.SymbolAmount.Label.text=RecoveredCurrency.Format(7,0,2);
            bool arrived=false;float finished=-1,began=Time.time;
            sequence.FlightArrived+=()=>{arrived=true;Assert.AreEqual(string.Empty,field.SymbolAmount.Label.text);};
            sequence.Begin(12,()=>{Assert.AreEqual(112,data.GreenCount);Assert.AreEqual(2,saves);return 5;},player,()=>finished=Time.time);
            Assert.AreEqual(112,data.GreenCount);Assert.IsTrue(sequence.IsFlying);Assert.AreEqual(12,field.DownWin.TemporaryTotal);
            Assert.AreEqual((sequence.Origin+sequence.Destination)*.5f+Vector3.up,sequence.Control);
            yield return null;yield return null;
            var paused=field.SymbolAmount.Label.transform.position;Time.timeScale=0;
            for(int i=0;i<15;i++)yield return null;
            Assert.AreEqual(paused,field.SymbolAmount.Label.transform.position);Assert.IsFalse(arrived);Assert.AreEqual(-1,finished);
            Time.timeScale=1;
            for(int i=0;i<30&&(finished<0||sequence.IsCounting);i++)yield return null;
            Assert.IsTrue(arrived);Assert.GreaterOrEqual(finished-began,.8f-.0001f);
            Assert.That(Vector3.Distance(sequence.Origin,field.SymbolAmount.Label.transform.position),Is.LessThan(.0001f));
            Assert.AreEqual(RecoveredCurrency.Format(12,0,2),field.DownWin.Label.text);
            Assert.AreEqual(5,field.DownWin.Total,"b__13 only formats text; it does not assign DownWinCount");
            Assert.AreEqual(12,field.DownWin.TemporaryTotal);Assert.AreEqual(2,saves);
            bool stale=false;sequence.Begin(4,()=>0,player,()=>stale=true);yield return null;sequence.Cancel();
            for(int i=0;i<25;i++)yield return null;
            Assert.IsFalse(stale);Assert.IsFalse(sequence.IsFlying);Assert.IsFalse(sequence.IsCounting);
        } finally {Time.timeScale=scale;Time.captureDeltaTime=delta;Object.Destroy(coin);Object.Destroy(root);}
    }
    [UnityTest]
    public IEnumerator PaidNormalSpinCreditsBeforeFlightAndCompletesOnlyTheSymbolStage()
    {
        string key=RecoveredPlayerStore.OriginalKey;bool had=PlayerPrefs.HasKey(key);string saved=PlayerPrefs.GetString(key);
        var random=Random.state;float scale=Time.timeScale,delta=Time.captureDeltaTime;PlayerPrefs.DeleteKey(key);
        var root=Object.Instantiate(Resources.Load<GameObject>("Whitebox/GameEntry"));var entry=root.GetComponent<GameEntry>();
        var host=new GameObject("Ordinary win camera",typeof(Camera));var camera=host.GetComponent<Camera>();
        camera.transform.position=new Vector3(100,0,-10);camera.orthographic=true;camera.orthographicSize=5;camera.cullingMask=32;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
        var target=new RenderTexture(1080,1920,24);camera.targetTexture=target;root.GetComponent<Canvas>().worldCamera=camera;
        var previous=RenderTexture.active;Texture2D capture=null;
        try {
            Time.timeScale=1;Time.captureDeltaTime=.05f;
            { float resourceDeadline=Time.realtimeSinceStartup+5; while(entry.Playfield==null&&Time.realtimeSinceStartup<resourceDeadline)yield return null; }
            var field=entry.Playfield;entry.PlayerStore.Data.GuideStep=2;
            float balance=entry.PlayerProgress.GreenCount,began=-1,finished=-1;int changes=0,completed=0;bool found=false;
            field.SymbolAmountReady+=()=>{began=Time.time;Assert.AreEqual(balance,entry.PlayerProgress.GreenCount);};
            field.SymbolSequenceCompleted+=()=>{completed++;finished=Time.time;};
            entry.PlayerProgress.GreenCountChanged+=(before,after)=>{changes++;Assert.AreEqual(balance,before);};
            field.SpinButton.Button.onClick.Invoke();
            for(int attempt=0;attempt<10000;attempt++) {
                Random.InitState(719+attempt);entry.SpinResult.Board.FillBase();entry.SpinResult.Board.Settle(entry.Settlement,field.Bet);
                float win=entry.Settlement.GetWinTotalLine();int wildColumns=0;
                for(int c=0;c<5;c++)if(entry.SpinResult.Board.GetSymbol(c,0)==7&&entry.SpinResult.Board.GetSymbol(c,1)==7&&entry.SpinResult.Board.GetSymbol(c,2)==7)wildColumns++;
                if(win>0&&entry.Rules.GetBigWin(win,field.Bet)==RecoveredSlotWinType.None&&wildColumns==0){found=true;break;}
            }
            Assert.IsTrue(found,"Original base weights must produce an ordinary winning board");
            for(int i=0;i<700&&!field.OrdinaryWin.IsFlying;i++)yield return null;
            Assert.IsTrue(field.OrdinaryWin.IsFlying);Assert.IsNull(field.Error);Assert.GreaterOrEqual(began,0);
            Assert.AreEqual(1,changes);Assert.AreEqual(balance+field.SymbolWin.TotalWin,entry.PlayerProgress.GreenCount);
            Assert.AreEqual(0,completed);Assert.AreEqual(0,entry.CashFlight.ActiveCashCount);Assert.IsFalse(entry.Ads.Pending);
            for(int i=0;i<2;i++)yield return null;
            capture=new Texture2D(1080,1920,TextureFormat.RGB24,false);Capture(camera,target,capture,"current-ordinary-win-flight.png");
            for(int i=0;i<30&&(completed==0||field.OrdinaryWin.IsCounting);i++)yield return null;
            Assert.AreEqual(1,completed);Assert.GreaterOrEqual(finished-began,.8f-.0001f);Assert.AreEqual(string.Empty,field.SymbolAmount.Label.text);
            Assert.AreEqual(RecoveredCurrency.Format(field.SymbolWin.TotalWin,entry.CurrentProfile.languageType,2),field.DownWin.Label.text);
            Assert.AreEqual(1,changes);Assert.IsFalse(field.IsBusy);Assert.IsFalse(field.AwaitingRewards);Assert.IsFalse(field.JackpotPopup.gameObject.activeSelf);
            Capture(camera,target,capture,"current-ordinary-win-complete.png");
        } finally {
            Time.timeScale=scale;Time.captureDeltaTime=delta;Random.state=random;RenderTexture.active=previous;
            camera.targetTexture=null;target.Release();Object.Destroy(target);if(capture!=null)Object.Destroy(capture);Object.Destroy(host);Object.Destroy(root);
            if(had)PlayerPrefs.SetString(key,saved);else PlayerPrefs.DeleteKey(key);
        }
    }
    private static void Capture(Camera camera,RenderTexture target,Texture2D capture,string name)
    {
        Canvas.ForceUpdateCanvases();RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});RenderTexture.active=target;
        capture.ReadPixels(new Rect(0,0,1080,1920),0,0);capture.Apply();File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/"+name),capture.EncodeToPNG());
    }
}
