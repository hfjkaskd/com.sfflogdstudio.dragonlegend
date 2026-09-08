using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using System.IO;
using DragonLegend.Whitebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;
using Random=UnityEngine.Random;

public sealed class RecoveredJackpotIntegrationTests
{
    [UnityTest]
    public IEnumerator PaidSpinReachesPopupResetsMeterAndUsesManualAdResultWithoutPrematureContinuation()
    {
        string key=RecoveredPlayerStore.OriginalKey;bool had=PlayerPrefs.HasKey(key);string saved=PlayerPrefs.GetString(key);
        var random=Random.state;float scale=Time.timeScale,delta=Time.captureDeltaTime;PlayerPrefs.DeleteKey(key);
        var root=Object.Instantiate(Resources.Load<GameObject>("Whitebox/GameEntry"));var entry=root.GetComponent<GameEntry>();
        var cameraHost=new GameObject("Current jackpot main camera",typeof(Camera));var camera=cameraHost.GetComponent<Camera>();
        camera.transform.position=new Vector3(100,0,-10);camera.orthographic=true;camera.orthographicSize=5;camera.cullingMask=32;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
        var target=new RenderTexture(1080,1920,24);camera.targetTexture=target;root.GetComponent<Canvas>().worldCamera=camera;
        var previous=RenderTexture.active;Texture2D capture=null;
        try
        {
            Time.timeScale=1;Time.captureDeltaTime=.05f;
            for(int i=0;i<200&&entry.Playfield==null;i++)yield return null;
            var field=entry.Playfield;Assert.IsNotNull(field);var popup=field.JackpotPopup;Assert.IsNotNull(popup);
            Assert.IsFalse(popup.gameObject.activeSelf);Assert.IsNotNull(entry.AdControls);
            Assert.AreEqual(300,popup.GetComponent<Canvas>().sortingOrder);Assert.AreSame(camera,popup.GetComponent<Canvas>().worldCamera);
            var mask=popup.transform.Find("_WindowBg");Assert.AreEqual(0,mask.GetSiblingIndex());
            Assert.AreEqual(new Color(0,0,0,.65f),mask.GetComponent<Image>().color);
            Assert.AreSame(mask.GetComponent<Image>(),mask.GetComponent<Button>().targetGraphic);
            var data=entry.PlayerStore.Data;data.GuideStep=2;data.JpAddCount=8;
            entry.PlayerProgress.IsFirstFreeReward=true; // prove the next ordinary result clears it
            float beforeBalance=entry.PlayerProgress.GreenCount,winTime=-1,flightAmount=0;int symbols=0;Action flight=null;
            int amounts=0;float symbolTime=-1,amountTime=-1;
            field.SymbolAnimationsRequested+=()=>{symbols++;symbolTime=Time.time;};
            field.SymbolAmountReady+=()=>{amounts++;amountTime=Time.time;};
            field.JackpotSequence.WinRequested+=type=>{Assert.AreEqual(RecoveredJackpotType.Grand,type);winTime=Time.time;};
            field.FlyCoinRequested+=(amount,completed)=>{flightAmount=amount;flight=completed;};
            field.SpinButton.Button.onClick.Invoke();Assert.IsFalse(entry.PlayerProgress.IsFirstFreeReward);
            entry.SpinResult.Board.ApplyGuaranteedWildIndex(2);
            entry.SpinResult.Board.Settle(entry.Settlement,field.Bet);
            for(int i=0;i<500&&!popup.gameObject.activeSelf;i++)yield return null;
            Assert.IsNull(field.Error);Assert.IsTrue(popup.gameObject.activeSelf);Assert.GreaterOrEqual(winTime,0);
            Assert.GreaterOrEqual(Time.time-winTime,1.5f-.001f);
            float expected=entry.Rules.GetJackpotReward(entry.Rules.GetJackPot()[0],field.Bet,9);
            Assert.AreEqual(expected,field.JackpotSequence.CapturedReward);Assert.AreEqual(expected,popup.Claim.OriginalReward);
            Assert.AreEqual(2,data.PlayerTaskDatas[0].id);Assert.AreEqual(1,data.PlayerTaskDatas[0].count);
            Assert.IsTrue(field.JackpotMeters.At(0).Icon.IsWinning);Assert.AreEqual(9,data.JpAddCount);
            for(int i=0;i<22;i++)yield return null;
            Assert.AreEqual(0,data.JpAddCount);Assert.IsFalse(field.JackpotMeters.At(0).Icon.IsWinning);
            Assert.AreEqual(entry.Rules.GetJackpotReward(entry.Rules.GetJackPot()[0],field.Bet,0),entry.PlayerProgress.GrandJackPotReward);
            Assert.AreEqual(expected,popup.Claim.OriginalReward);Assert.AreEqual(0,symbols);
            capture=new Texture2D(1080,1920,TextureFormat.RGB24,false);
            Capture(camera,target,capture,"current-jackpot-main-flow.png");
            mask.GetComponent<Button>().onClick.Invoke();Assert.IsFalse(popup.Claim.IsClicked);
            popup.ClaimButton.onClick.Invoke();Assert.IsTrue(entry.Ads.Pending);
            yield return null;yield return null;
            Assert.IsTrue(entry.AdControls.RewardButton.gameObject.activeInHierarchy);
            Capture(camera,target,capture,"current-jackpot-gm-ad.png");
            entry.AdControls.FailureButton.onClick.Invoke();Assert.IsFalse(entry.Ads.Pending);Assert.IsFalse(popup.Claim.IsClicked);
            Assert.IsNull(flight);Assert.IsTrue(popup.gameObject.activeSelf);
            popup.ClaimButton.onClick.Invoke();entry.AdControls.RewardButton.onClick.Invoke();
            for(int i=0;i<30&&flight==null;i++)yield return null;
            Assert.IsNotNull(flight);Assert.IsFalse(popup.gameObject.activeSelf);
            Assert.AreEqual(expected*entry.Rules.GetJpClaim(0),flightAmount);
            Assert.AreEqual(0,symbols);Assert.IsTrue(field.JackpotSequence.IsRunning);
            Assert.AreEqual(beforeBalance,entry.PlayerProgress.GreenCount);
            Assert.AreEqual(10,entry.CashFlight.ActiveCashCount);
            for(int i=0;i<6;i++)yield return null;
            Capture(camera,target,capture,"current-jackpot-cash-scatter.png");
            float flightDeadline=Time.realtimeSinceStartup+3;
            while(symbols==0&&Time.realtimeSinceStartup<flightDeadline)yield return null;
            Assert.AreEqual(1,symbols);Assert.IsFalse(field.JackpotSequence.IsRunning);
            Assert.IsNotNull(field.SymbolWin);
            Assert.AreEqual(entry.Settlement.GetWinTotalLine()+field.BonusCoins.TotalReward,field.SymbolWin.TotalWin);
            if(field.SymbolWin.HasReward)
                Assert.AreEqual(entry.Rules.GetBigWin(field.SymbolWin.TotalWin,field.Bet),field.SymbolWin.BigWin);
            Assert.AreEqual(beforeBalance+flightAmount,entry.PlayerProgress.GreenCount);
            Assert.AreEqual(0,entry.CashFlight.ActiveCashCount);Assert.Greater(entry.CashFlight.ActiveEffectCount,0);
            Assert.AreEqual(2,entry.BalancePanel.transform.GetSiblingIndex());
            Capture(camera,target,capture,"current-jackpot-cash-arrival.png");
            for(int i=0;i<20;i++)yield return null;
            Assert.GreaterOrEqual(field.SymbolAmount.GetComponent<Canvas>().sortingOrder,field.Wilds.NextSortingOrder);
            Assert.AreEqual(1,amounts);Assert.GreaterOrEqual(amountTime-symbolTime,.5f-.0001f);
            Assert.AreEqual(RecoveredCurrency.Format(field.SymbolWin.LineWin,entry.CurrentProfile.languageType,2),field.SymbolAmount.Label.text);
            Assert.AreEqual(beforeBalance+flightAmount,entry.PlayerProgress.GreenCount,"The initial amount tween cannot credit the line award");
            var gm=root.transform.Find("SelectUS");Assert.IsTrue(gm.gameObject.activeInHierarchy);
            Assert.Greater(gm.GetComponent<Canvas>().sortingOrder,field.SymbolAmount.GetComponent<Canvas>().sortingOrder);
            Assert.IsNotNull(gm.GetComponent<GraphicRaycaster>());
            Canvas.ForceUpdateCanvases();var hits=new List<RaycastResult>();
            gm.GetComponent<GraphicRaycaster>().Raycast(new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(camera,gm.position)},hits);
            Assert.IsTrue(hits.Exists(hit=>hit.gameObject==gm.gameObject),"GM version Button must remain reachable through Unity UI raycasting");
            Capture(camera,target,capture,"current-symbol-amount-main.png");
            Assert.AreEqual(0,entry.CashFlight.ActiveEffectCount);
            Assert.AreEqual(RecoveredCurrency.Format(beforeBalance+flightAmount,entry.CurrentProfile.languageType),entry.BalancePanel.BalanceText);
            Assert.IsTrue(field.IsBusy);Assert.IsTrue(field.AwaitingRewards,"Only CheckBaseEnd can release Spin");
        }
        finally
        {
            Time.timeScale=scale;Time.captureDeltaTime=delta;Random.state=random;RenderTexture.active=previous;
            camera.targetTexture=null;if(capture!=null)Object.Destroy(capture);Object.Destroy(target);Object.Destroy(cameraHost);Object.Destroy(root);
            if(had)PlayerPrefs.SetString(key,saved);else PlayerPrefs.DeleteKey(key);
        }
    }
    private static void Capture(Camera camera,RenderTexture target,Texture2D capture,string name)
    {
        Canvas.ForceUpdateCanvases();RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
        RenderTexture.active=target;capture.ReadPixels(new Rect(0,0,1080,1920),0,0);capture.Apply();
        File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/"+name),capture.EncodeToPNG());
    }
}
