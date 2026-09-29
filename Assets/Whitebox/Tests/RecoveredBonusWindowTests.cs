using System;
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

public sealed class RecoveredBonusWindowTests
{
    [UnityTest]
    public IEnumerator ShortRoundHidesUnusedSlotsAndReopeningRestoresThem()
    {
        var random=UnityEngine.Random.state;
        var loader=new ConfigSnapshotLoader();yield return loader.Load("RecoveredConfig/Remote/cp_test.json");
        var config=loader.Value;
        config.Ronig.Ltoo=new List<int>{1};config.Ronig.Qoi=new List<int>{1};
        config.Ronig.Jin=new List<int>{1};config.Ronig.Roo=new List<int>{4};config.Ronig.Rgkorp=new List<int>{2};
        var rules=new RecoveredGameplayRules(config);
        var player=new RecoveredPlayerProgress(rules,()=>{},new PlayerData());
        var window=Object.Instantiate(Resources.Load<RecoveredBonusWindow>("RecoveredUI/BonusWindow"));
        try {
            window.Show(player,rules,new LocalAdFacade(),null,()=>1,false,0);
            Assert.AreEqual(9,window.Selection.ActiveCardCount);
            for(int i=0;i<12;i++)Assert.AreEqual(i<9,window.Card(i).gameObject.activeSelf);
            window.Selection.Select(11);Assert.IsFalse(window.Selection.IsClicked);
            for(int i=0;i<9;i++)Assert.IsTrue(window.Selection.Round.TryReveal(i,out _));
            window.ShowFinger();Assert.IsNull(window.Finger,"Exhausted short pool must not hint at unavailable cards.");
            config.Ronig.Rgkorp[0]=5;
            window.Show(player,rules,new LocalAdFacade(),null,()=>1,false,0);
            Assert.AreEqual(12,window.Selection.ActiveCardCount);
            for(int i=0;i<12;i++){
                Assert.IsTrue(window.Card(i).gameObject.activeSelf);
                Assert.IsTrue(window.Card(i).Button.enabled);
            }
        }finally{Object.Destroy(window.gameObject);UnityEngine.Random.state=random;}
    }

    private static RecoveredGameplayRules CashRules(int jump,bool characters=false)=>new RecoveredGameplayRules(new GoldenDragonAutoGenConfig {
        Qonrii=new QonriiPoro {Joqkpor=new List<int>{10000,5000,1000},JpOpp=new List<int>{1},JpQloim=new List<int>{2000,500}},
        Rgpggm=new RgpggmPoro {Qogt=new List<int>{10000}},
        Ronig=new RonigPoro {Ltoo=new List<int>{characters?1:0},Qoi=new List<int>{characters?1:0},Jin=new List<int>{characters?1:0},Roo=new List<int>{characters?1:0},
            Rgkorp=new List<int>{characters?8:12},RrggRimgg=new List<int>{characters?12:1},RgkorpKgiitr=new List<int>{1},
            RgokrpMin=new List<int>{963},RgkorpMoj=new List<int>{963},Jimp=new List<int>{jump}}});

    [UnityTest]
    public IEnumerator OriginalWindowBindsCardsAndClaimsThroughTheSharedCashFlight()
    {
        string key=RecoveredPlayerStore.OriginalKey;bool had=PlayerPrefs.HasKey(key);string saved=PlayerPrefs.GetString(key);
        float scale=Time.timeScale,delta=Time.captureDeltaTime;var random=UnityEngine.Random.state;PlayerPrefs.DeleteKey(key);
        var entryRoot=Object.Instantiate(Resources.Load<GameObject>("Whitebox/GameEntry"));var entry=entryRoot.GetComponent<GameEntry>();
        var host=new GameObject("Current Bonus canvas",typeof(RectTransform),typeof(Canvas));host.layer=5;
        var cameraHost=new GameObject("Current Bonus camera",typeof(Camera));var camera=cameraHost.GetComponent<Camera>();
        camera.transform.position=new Vector3(0,0,-10);camera.orthographic=true;camera.orthographicSize=960;camera.cullingMask=33;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
        var canvas=host.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=camera;
        ((RectTransform)host.transform).sizeDelta=new Vector2(1080,1920);
        var target=new RenderTexture(1080,1920,24);camera.targetTexture=target;var previous=RenderTexture.active;
        var capture=new Texture2D(1080,1920,TextureFormat.RGB24,false);
        try {
            Time.timeScale=1;Time.captureDeltaTime=.025f;
            { float resourceDeadline=Time.realtimeSinceStartup+5; while(entry.CashFlight==null&&Time.realtimeSinceStartup<resourceDeadline)yield return null; }
            Assert.IsNotNull(entry.CashFlight);
            var prefab=Resources.Load<RecoveredBonusWindow>("RecoveredUI/BonusWindow");Assert.IsNotNull(prefab);
            var window=Object.Instantiate(prefab,host.transform);
            window.Show(entry.PlayerProgress,entry.Rules,entry.Ads,entry.CashFlight,()=>1,false,0);
            for(int i=0;i<25;i++)yield return null;
            Assert.IsNull(window.Finger,"Hand must wait for the native 1.83-second hint delay.");
            for(int i=0;i<100&&window.Finger==null;i++)yield return null;
            Assert.IsNotNull(window.Finger);Assert.IsTrue(window.Finger.gameObject.activeInHierarchy);
            var firstFinger=window.Finger;
            int fingerIndex=-1;for(int i=0;i<window.CardCount;i++)if(window.Card(i).transform==firstFinger.parent)fingerIndex=i;
            Assert.GreaterOrEqual(fingerIndex,0);Assert.IsFalse(window.Selection.Round.WasClicked(fingerIndex));
            Assert.AreEqual(Vector2.zero,firstFinger.anchoredPosition);Assert.AreEqual(Vector3.one,firstFinger.localScale);
            for(int i=0;i<20;i++)yield return null; // Inspect the animated pose, not its transparent opening frame.
            Assert.AreEqual(12,window.CardCount);Assert.AreEqual("Bonus (11)",window.Card(7).name);
            Assert.AreEqual(Math.Min(window.Selection.Round.Count,window.CardCount),window.Selection.ActiveCardCount);
            for(int i=0;i<window.CardCount;i++)Assert.AreEqual(i<window.Selection.ActiveCardCount,window.Card(i).gameObject.activeSelf);
            Assert.AreEqual("LUCKY DRAW CHANCES(<gradient=\"spin\">0/"+entry.Rules.GetBonusFreeTimes()+"</gradient>)",window.Chances.text);
            foreach(var component in window.GetComponentsInChildren<Component>(true))Assert.IsNotNull(component,"Missing native component binding");
            for(int i=0;i<12;i++){
                Assert.IsNotNull(window.Card(i).Button.targetGraphic);Assert.AreSame(window.Card(i).Button.gameObject,window.Card(i).Button.targetGraphic.gameObject);
                Assert.AreEqual(0,window.Card(i).Button.onClick.GetPersistentEventCount());
            }
            Canvas.ForceUpdateCanvases();RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
            RenderTexture.active=target;capture.ReadPixels(new Rect(0,0,1080,1920),0,0);capture.Apply();
            File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/current-bonus-window.png"),capture.EncodeToPNG());
            for(int i=0;i<90&&firstFinger.gameObject.activeSelf;i++)yield return null;
            Assert.IsFalse(firstFinger.gameObject.activeSelf);
            for(int i=0;i<90&&!firstFinger.gameObject.activeSelf;i++)yield return null;
            Assert.IsTrue(firstFinger.gameObject.activeSelf);Assert.AreSame(firstFinger,window.Finger);
            window.Card(fingerIndex).Button.onClick.Invoke();
            Assert.IsFalse(firstFinger.gameObject.activeSelf,"Selecting a card immediately hides its hint.");
            window.CancelFingerSequence();window.HideFinger();
            Object.Destroy(window.gameObject);yield return null;
            // Fixed source-shaped configs exercise both branches through actual Buttons,
            // child popup and the GameEntry flight/balance service. No payout stub.
            for(int jump=0;jump<2;jump++){
                window=Object.Instantiate(prefab,host.transform);Exception failure=null;window.Failed+=error=>failure=error;
                int exits=0;window.ExitRequested+=()=>exits++;
                window.Show(entry.PlayerProgress,CashRules(jump),entry.Ads,entry.CashFlight,()=>1,false,0);
                for(int i=0;i<20;i++)yield return null;
                var popup=window.GetComponentInChildren<RecoveredBonusRewardPopup>(true);
                int count=jump==0?12:1;
                for(int card=0;card<count;card++){
                    float before=entry.PlayerProgress.GreenCount;
                    window.Card(card).Button.onClick.Invoke();
                    if(card>0){Assert.IsTrue(entry.Ads.Pending);Assert.AreEqual("bonusCoin",entry.Ads.Placement);entry.Ads.Complete(AdOutcome.Rewarded);}
                    Assert.IsTrue(window.Selection.IsClicked);Assert.AreEqual(card+1,window.Selection.Round.ClickedCount);
                    Assert.AreEqual(before,entry.PlayerProgress.GreenCount);
                    if(jump!=0){
                        for(int i=0;i<80&&!popup.gameObject.activeSelf;i++)yield return null;
                        Assert.IsTrue(popup.gameObject.activeSelf);Assert.IsTrue(window.Selection.IsClicked);
                        Assert.AreEqual(before,entry.PlayerProgress.GreenCount);
                        for(int i=0;i<20;i++)yield return null;
                        popup.ClaimButton.onClick.Invoke();Assert.IsTrue(entry.Ads.Pending);Assert.AreEqual("lucky",entry.Ads.Placement);
                        entry.Ads.Complete(AdOutcome.Rewarded);
                    }
                    for(int i=0;i<100&&entry.CashFlight.ActiveCashCount==0;i++)yield return null;
                    Assert.IsNull(failure);Assert.AreEqual(10,entry.CashFlight.ActiveCashCount);
                    Assert.AreEqual(before,entry.PlayerProgress.GreenCount);
                    Assert.AreEqual(jump!=0,window.Selection.IsClicked,"Normal cash releases at turn; jump holds through payout");
                    Assert.AreSame(window.transform,entry.BalancePanel.transform.parent);
                    if(card==11){Assert.AreEqual(1,exits);Assert.IsTrue(window.Selection.IsEnd);}
                    float deadline=Time.realtimeSinceStartup+5;
                    while(entry.CashFlight.ActiveCashCount>0&&Time.realtimeSinceStartup<deadline)yield return null;
                    Assert.AreEqual(0,entry.CashFlight.ActiveCashCount);Assert.IsFalse(window.Selection.IsClicked);
                    Assert.AreEqual(before+(jump==0?963:1926),entry.PlayerProgress.GreenCount);
                    Assert.AreSame(entry.Playfield.transform,entry.BalancePanel.transform.parent);
                }
                Assert.IsNull(failure);Object.Destroy(window.gameObject);yield return null;
            }
            window=Object.Instantiate(prefab,host.transform);Exception characterFailure=null;window.Failed+=error=>characterFailure=error;
            window.Show(entry.PlayerProgress,CashRules(0,true),entry.Ads,entry.CashFlight,()=>1,false,0);
            for(int i=0;i<20;i++)yield return null;
            var jackpot=window.GetComponentInChildren<RecoveredJackpotPopup>(true);
            var characterSequence=window.GetComponentInChildren<RecoveredBonusCharacterRewards>();
            int selected=0;
            for(int i=0;i<12;i++){
                if(window.Selection.Round.GetCard(i)==RecoveredBonusType.Reward)continue;
                window.Card(i).Button.onClick.Invoke();selected++;
                if(selected<4){
                    for(int frame=0;frame<150&&characterSequence.PendingCount>0;frame++)yield return null;
                    Assert.IsNull(characterFailure);Assert.AreEqual(0,characterSequence.PendingCount);Assert.IsFalse(window.Selection.IsClicked);
                }else{
                    for(int frame=0;frame<200&&!jackpot.gameObject.activeSelf;frame++)yield return null;
                    Assert.IsNull(characterFailure);Assert.IsTrue(jackpot.gameObject.activeSelf);Assert.IsTrue(window.Selection.IsClicked);
                    for(int frame=0;frame<55;frame++)yield return null;
                    Canvas.ForceUpdateCanvases();RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
                    RenderTexture.active=target;capture.ReadPixels(new Rect(0,0,1080,1920),0,0);capture.Apply();
                    File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/current-bonus-window-jackpot.png"),capture.EncodeToPNG());
                    float before=entry.PlayerProgress.GreenCount,amount=entry.PlayerProgress.GrandJackPotReward;
                    jackpot.PlainButton.onClick.Invoke();
                    for(int frame=0;frame<100&&entry.CashFlight.ActiveCashCount==0;frame++)yield return null;
                    Assert.AreEqual(10,entry.CashFlight.ActiveCashCount);Assert.IsTrue(window.Selection.IsClicked);
                    float deadline=Time.realtimeSinceStartup+5;
                    while(entry.CashFlight.ActiveCashCount>0&&Time.realtimeSinceStartup<deadline)yield return null;
                    Assert.IsFalse(window.Selection.IsClicked);Assert.AreEqual(before+amount*.5f,entry.PlayerProgress.GreenCount);
                }
            }
            Assert.AreEqual(4,selected);Assert.IsNull(characterFailure);
        }finally{
            Time.timeScale=scale;Time.captureDeltaTime=delta;UnityEngine.Random.state=random;RenderTexture.active=previous;camera.targetTexture=null;
            Object.Destroy(capture);Object.Destroy(target);Object.Destroy(host);Object.Destroy(cameraHost);Object.Destroy(entryRoot);
            if(had)PlayerPrefs.SetString(key,saved);else PlayerPrefs.DeleteKey(key);
        }
    }
}
