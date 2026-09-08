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

public sealed class RecoveredBigWinIntegrationTests
{
    [UnityTest]
    public IEnumerator PaidSpinOpensBigWinAndCreditsOnlyThePopupCashFlight()
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
            for(int i=0;i<200&&entry.Playfield==null;i++)yield return null;
            var field=entry.Playfield;entry.PlayerStore.Data.GuideStep=2;
            float balance=entry.PlayerProgress.GreenCount,began=-1;int changes=0,completed=0,flights=0,bursts=0;bool found=false;
            field.SymbolAmountReady+=()=>began=Time.time;
            field.SymbolSequenceCompleted+=()=>completed++;
            entry.PlayerProgress.GreenCountChanged+=(before,after)=>{changes++;Assert.AreEqual(balance,before);};
            field.WinFlight.ArrivalEffectRequested+=()=>bursts++;
            field.FlyCoinRequested+=(value,callback)=>{
                flights++;Assert.IsNull(callback);Assert.IsFalse(field.BigWinSequence.AwaitingWindow);
                Assert.AreEqual(balance,entry.PlayerProgress.GreenCount);
                Assert.AreEqual(field.SymbolWin.TotalWin*entry.Rules.GetBigWinClaim(0),value);
            };
            field.SpinButton.Button.onClick.Invoke();
            for(int attempt=0;attempt<10000;attempt++) {
                Random.InitState(719+attempt);entry.SpinResult.Board.FillBase();entry.SpinResult.Board.Settle(entry.Settlement,field.Bet);
                float win=entry.Settlement.GetWinTotalLine();
                if(win>0&&entry.Rules.GetBigWin(win,field.Bet)!=RecoveredSlotWinType.None){found=true;break;}
            }
            Assert.IsTrue(found);
            for(int i=0;i<700&&!field.BigWinPopup.gameObject.activeSelf;i++)yield return null;
            Assert.IsNull(field.Error);Assert.IsTrue(field.BigWinPopup.gameObject.activeSelf);
            Assert.GreaterOrEqual(Time.time-began,1.1f-.001f);Assert.AreEqual(1,bursts);
            Assert.AreEqual(string.Empty,field.SymbolAmount.Label.text);Assert.AreEqual(0,completed);
            Assert.AreEqual(balance,entry.PlayerProgress.GreenCount);Assert.AreEqual(0,changes);
            Assert.AreEqual(0,entry.CashFlight.ActiveCashCount);Assert.IsFalse(field.JackpotPopup.gameObject.activeSelf);
            Assert.AreEqual(RecoveredCurrency.Format(field.SymbolWin.TotalWin,entry.CurrentProfile.languageType,2),field.DownWin.Label.text);
            Assert.AreEqual(0,field.DownWin.Total,"Pre-popup count is text-only");
            Assert.IsTrue(entry.PlayerStore.Data.PlayerTaskDatas.Exists(t=>t.id==1&&t.count==1));
            for(int i=0;i<32;i++)yield return null;
            capture=new Texture2D(1080,1920,TextureFormat.RGB24,false);Capture(camera,target,capture,"current-bigwin-spin-popup.png");
            field.BigWinPopup.ClaimButton.onClick.Invoke();Assert.IsTrue(entry.Ads.Pending);
            entry.AdControls.FailureButton.onClick.Invoke();Assert.IsFalse(field.BigWinPopup.Claim.IsClicked);
            Assert.AreEqual(0,flights);Assert.AreEqual(0,changes);
            field.BigWinPopup.ClaimButton.onClick.Invoke();entry.AdControls.RewardButton.onClick.Invoke();
            for(int i=0;i<40&&flights==0;i++)yield return null;
            Assert.AreEqual(1,flights);Assert.IsFalse(field.BigWinPopup.gameObject.activeSelf);
            Assert.IsTrue(field.BigWinSequence.IsRunning);Assert.AreEqual(0,completed);
            float award=field.SymbolWin.TotalWin*entry.Rules.GetBigWinClaim(0);
            for(int i=0;i<4;i++)yield return null;
            Capture(camera,target,capture,"current-bigwin-spin-adjustment.png");
            float deadline=Time.realtimeSinceStartup+4;
            while((completed==0||entry.CashFlight.ActiveCashCount>0||field.OrdinaryWin.IsCounting)&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.IsNull(field.Error);Assert.AreEqual(1,completed);Assert.IsFalse(field.BigWinSequence.IsRunning);
            Assert.AreEqual(1,changes);Assert.AreEqual(balance+award,entry.PlayerProgress.GreenCount);
            Assert.AreEqual(award,field.DownWin.TemporaryTotal);Assert.AreEqual(0,field.DownWin.Total);
            Assert.AreEqual(RecoveredCurrency.Format(award,entry.CurrentProfile.languageType,2),field.DownWin.Label.text);
            Assert.AreEqual(string.Empty,field.SymbolAmount.Label.text);Assert.IsTrue(field.IsBusy);
            Capture(camera,target,capture,"current-bigwin-spin-complete.png");
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
