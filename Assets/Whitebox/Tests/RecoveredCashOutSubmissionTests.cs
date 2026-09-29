using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public sealed class RecoveredCashOutSubmissionTests
{
    private static RecoveredGameplayRules Rules()=>new RecoveredGameplayRules(new GoldenDragonAutoGenConfig{
        Qonrii=new QonriiPoro{Ripg=new List<string>{"default"}},
        Rgpggm=new RgpggmPoro{Qogt=new List<int>{1},Rogk1roil=new List<int>{20},Rogk2roil=new List<int>{0},Rogk3roil=new List<int>{2},Rogk4roil=new List<int>{0},Rogk5roil=new List<int>{0},Rogk6roil=new List<int>{0},Rimgg1=new List<int>{7},Rimg3=new List<int>{7},Rimg7=new List<int>{7}},
        Qollgqr=new QollgqrPoro{Ip=new List<int>{0},Lgtgl=new List<int>{1},Ronpom=new List<int>{1},Korrt=new List<int>{1}}});
    private sealed class RetryFacade:ICashFacade
    {
        public int Calls;public string Request;private readonly LocalCashFacade local=new LocalCashFacade();
        public MockCashOrder Submit(string id,long amount)
        {
            Calls++;if(Request!=null)Assert.AreEqual(Request,id);Request=id;
            var result=local.Submit(id,amount);Assert.AreEqual(100,amount);
            if(Calls==1)throw new InvalidOperationException("Lost response after acceptance");return result;
        }
    }
    [Test]
    public void RetryAfterLostResponseReusesRequestAndPendingSurvivesReload()
    {
        var rules=Rules();var data=new PlayerData{GreenCount=1.17f};int saves=0;
        var player=new RecoveredPlayerProgress(rules,()=>saves++,data);var sdk=new RetryFacade();
        var flow=new RecoveredCashOutSubmission(rules,player,sdk);
        Assert.Throws<InvalidOperationException>(()=>flow.Submit(0,1,"Test","test@example.test",100));
        Assert.AreEqual("submitting",data.PlayerCashOutOrders[0].status);Assert.IsEmpty(data.PlayerCashOutDatas);
        flow.Submit(0,1,"Test","test@example.test",101);
        Assert.AreEqual(2,sdk.Calls);Assert.AreEqual(1,data.PlayerCashOutOrders.Count);Assert.AreEqual(1,data.PlayerCashOutDatas.Count);
        Assert.AreEqual(0,data.PlayerCashOutDatas[0].step);Assert.IsFalse(data.PlayerCashOutDatas[0].isCashout);
        Assert.AreEqual(101,data.PlayerCashOutDatas[0].time);Assert.AreEqual(20,player.GetCashOutConditions(0,101).TaskGoal);
        Assert.AreEqual(1.17f,data.GreenCount);Assert.Greater(saves,0);
        var restored=JsonUtility.FromJson<PlayerData>(JsonUtility.ToJson(data));
        var reloaded=new RecoveredCashOutSubmission(rules,new RecoveredPlayerProgress(rules,()=>Assert.Fail("Pending retry must not mutate"),restored),sdk);
        var again=reloaded.Submit(0,1,"Test","test@example.test",200);
        Assert.AreEqual("task_review",again.status);Assert.AreEqual(sdk.Request,again.orderId);Assert.AreEqual(2,sdk.Calls);
        Assert.AreEqual(101,restored.PlayerCashOutDatas[0].time,"Reopening must not restart the review timer.");
    }
    [Test]
    public void InvalidInputAndInsufficientBalanceDoNotCreateOrders()
    {
        var rules=Rules();var data=new PlayerData{GreenCount=.5f};var player=new RecoveredPlayerProgress(rules,()=>{},data);
        var flow=new RecoveredCashOutSubmission(rules,player,new LocalCashFacade());
        Assert.Throws<InvalidOperationException>(()=>flow.Submit(0,1,"Test","test@example.test",100));
        Assert.Throws<ArgumentException>(()=>player.SaveCashAccount(1," ","test@example.test"));
        Assert.IsEmpty(data.PlayerCashOutOrders);Assert.IsEmpty(data.PlayerAccount);Assert.IsEmpty(data.PlayerCashOutDatas);
        player.SaveCashAccount(1," Test ","test@example.test");player.SaveCashAccount(1,"Changed","changed@example.test");
        Assert.AreEqual(1,data.PlayerAccount.Count);Assert.AreEqual("Changed",data.PlayerAccount[0].accountName);
    }
    [Test]
    public void LegacyPendingReviewRestoresOnceWithoutChangingAccountBalanceOrStartTime()
    {
        var rules=Rules();var data=new PlayerData{GreenCount=12.69f};int saves=0;
        data.PlayerCashOutDatas.Add(new PlayerCashOutData{id=0,type=3,step=1000,time=123});
        data.PlayerCashOutOrders.Add(new PlayerCashOutOrder{index=0,orderId="legacy",status="pending"});
        var player=new RecoveredPlayerProgress(rules,()=>saves++,data);
        player.RestoreLegacyCashOutReviews();
        Assert.AreEqual(0,data.PlayerCashOutDatas[0].step);Assert.AreEqual(123,data.PlayerCashOutDatas[0].time);
        Assert.AreEqual(3,data.PlayerCashOutDatas[0].type);Assert.AreEqual(12.69f,data.GreenCount);
        Assert.AreEqual("task_review",data.PlayerCashOutOrders[0].status);
        player.RefreshCashOutTask(0,7);int before=saves;player.RestoreLegacyCashOutReviews();
        Assert.AreEqual(before,saves);Assert.AreEqual(7,data.PlayerCashOutDatas[0].count);
        data.PlayerCashOutDatas[0].step=1000;data.PlayerCashOutOrders[0].status="pending";
        player.RestoreLegacyCashOutReviews();Assert.AreEqual(1000,data.PlayerCashOutDatas[0].step,"Versioned final pending orders must not restart tasks.");
    }
    [UnityTest]
    public IEnumerator OriginalNonorganicConfigurationRunsAllReviewStagesAndSurvivesReload()
    {
        var loader=new ConfigSnapshotLoader();yield return loader.Load("RecoveredConfig/Remote/cp_test.json");
        var rules=new RecoveredGameplayRules(loader.Value);
        int[] spinGoals={20,40,60,80,100};
        for(int tier=0;tier<rules.GetCashOutCount();tier++)
        {
            var data=new PlayerData{GreenCount=rules.GetCashOutCash(tier)};
            var player=new RecoveredPlayerProgress(rules,()=>{},data);
            new RecoveredCashOutSubmission(rules,player,new LocalCashFacade()).Submit(tier,1,"Test","test@example.test",100);
            Assert.AreEqual(spinGoals[tier],player.GetCashOutConditions(tier,100).TaskGoal);
            for(int step=0;step<6;step++)
            {
                var record=data.PlayerCashOutDatas[0];Assert.AreEqual(step,record.step);
                int start=record.time;var state=player.GetCashOutConditions(tier,start);
                Assert.AreEqual(86400,state.RemainingSeconds);Assert.IsFalse(state.TaskComplete);Assert.IsFalse(state.WaitComplete);
                player.RefreshCashOutTask(step,state.TaskGoal);
                Assert.IsTrue(player.GetCashOutConditions(tier,start).TaskComplete);
                Assert.IsFalse(player.GetCashOutConditions(tier,start+86399).WaitComplete);
                data=JsonUtility.FromJson<PlayerData>(JsonUtility.ToJson(data));player=new RecoveredPlayerProgress(rules,()=>{},data);
                state=player.GetCashOutConditions(tier,start+86400);Assert.IsTrue(state.TaskComplete&&state.WaitComplete);
                record=data.PlayerCashOutDatas[0];player.ApplyCashOutTaskStep(record,rules.GetNextCashOutTaskStep(tier,step,false),start+86400);
            }
            Assert.AreEqual(6,data.PlayerCashOutDatas[0].step);
            Assert.AreEqual(rules.GetCollectInfoCount(),player.GetCashOutConditions(tier,data.PlayerCashOutDatas[0].time).TaskGoal);
                Assert.AreEqual("task_review",data.PlayerCashOutOrders[0].status,"Tasks do not synthesize a payment approval.");
        }
        var finalData=new PlayerData{GreenCount=rules.GetCashOutCash(0)};
        finalData.PlayerCashOutDatas.Add(new PlayerCashOutData{id=0,type=1,step=5,time=100,isCashout=true});
        var finalPlayer=new RecoveredPlayerProgress(rules,()=>{},finalData);
        var finalFlow=new RecoveredCashOutSubmission(rules,finalPlayer,new LocalCashFacade());
        Assert.Throws<InvalidOperationException>(()=>finalFlow.Submit(0,1,"Test","test@example.test",101));
        var pending=finalFlow.Submit(0,1,"Test","test@example.test",86500);
        Assert.AreEqual("pending",pending.status);Assert.AreEqual(1000,finalData.PlayerCashOutDatas[0].step);
    }
    [UnityTest]
    public IEnumerator RealWindowButtonsOpenValidateSubmitEditAndReopen()
    {
        var key=RecoveredPlayerStore.OriginalKey;bool had=PlayerPrefs.HasKey(key);string saved=PlayerPrefs.GetString(key);
        PlayerPrefs.DeleteKey(key);Scene scene=default;AsyncOperation unload=null;RecoveredCashOutWindow window=null;
        var random=UnityEngine.Random.state;float scale=Time.timeScale;Camera camera=null;RenderTexture target=null,prior=RenderTexture.active;Texture2D capture=null;
        GameObject portraitHost=null;
        try
        {
            Time.timeScale=1;yield return SceneManager.LoadSceneAsync("GameEntry",LoadSceneMode.Additive);scene=SceneManager.GetSceneByName("GameEntry");
            GameEntry game=null;foreach(var root in scene.GetRootGameObjects()){var found=root.GetComponentInChildren<GameEntry>();if(found!=null)game=found;}
            float deadline=Time.realtimeSinceStartup+10;while(game.CoreRound==null&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.IsNotNull(game.CoreRound);var rules=Rules();var data=new PlayerData{GreenCount=1.17f};var player=new RecoveredPlayerProgress(rules,()=>{},data);
            var canvas=game.GetComponent<Canvas>();camera=canvas.worldCamera;target=new RenderTexture(1080,1920,24);camera.targetTexture=target;
            // The batch test GameView is landscape; give the phone UI an explicit portrait viewport.
            portraitHost=new GameObject("Portrait withdrawal viewport",typeof(RectTransform));portraitHost.transform.SetParent(game.transform,false);
            ((RectTransform)portraitHost.transform).sizeDelta=new Vector2(1080,1920);
            window=Object.Instantiate(Resources.Load<RecoveredCashOutWindow>("RecoveredUI/CashOutWindow"),portraitHost.transform,false);
            window.Bind(rules,player,0,canvas,()=>100);window.Show();yield return null;
            var bottom=window.GetComponentInChildren<RecoveredCashOutBottom>(true);
            var task=new PlayerCashOutData{id=0,type=1,step=0,count=20,time=90};data.PlayerCashOutDatas.Add(task);
            bottom.Initialize(0,1,0);bottom.Button.onClick.Invoke();Assert.AreEqual(2,task.step);Assert.AreEqual(0,task.count);Assert.AreEqual(100,task.time);
            data.PlayerCashOutDatas.Clear();bottom.Initialize(0,1,0);
            bottom.Button.onClick.Invoke();var account=window.GetComponentInChildren<RecoveredAccountWindow>(true);
            Assert.IsNotNull(account);Assert.IsTrue(account.gameObject.activeSelf);
            foreach(var graphic in account.transform.Find("Content").GetComponentsInChildren<Image>(true))Assert.IsNotNull(graphic.sprite,graphic.name);
            Assert.AreEqual(0,account.SubmitButton.onClick.GetPersistentEventCount());
            account.SubmitButton.onClick.Invoke();Assert.IsTrue(account.gameObject.activeSelf);Assert.IsEmpty(data.PlayerCashOutOrders);
            account.NameInput.text="Test Player";account.EmailInput.text="test@example.test";
            yield return new WaitForSeconds(.4f);Canvas.ForceUpdateCanvases();
            Assert.Greater(window.List.Scroll.viewport.rect.height,0,"List must measure its active layout.");
            Assert.IsNotNull(window.List.ItemAt(0),"The withdrawal tier must be visible with its tasks.");
            RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});RenderTexture.active=target;
            capture=new Texture2D(1080,1920,TextureFormat.RGB24,false);capture.ReadPixels(new Rect(0,0,1080,1920),0,0);capture.Apply();
            File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/cash-account-connected.png"),capture.EncodeToPNG());
            account.SubmitButton.onClick.Invoke();Assert.IsFalse(account.gameObject.activeSelf);Assert.AreEqual(1,data.PlayerCashOutOrders.Count);
            Assert.AreEqual("Spin 0/20 times",bottom.TaskText.text);Assert.IsTrue(bottom.Button.gameObject.activeInHierarchy);Assert.AreEqual(1.17f,data.GreenCount);
            window.transform.Find("Content/Top/InputField (TMP)/AccountBtn").GetComponent<Button>().onClick.Invoke();
            Assert.IsTrue(account.gameObject.activeSelf);Assert.AreEqual("test@example.test",account.EmailInput.text);
            account.EmailInput.text="changed@example.test";account.SubmitButton.onClick.Invoke();Assert.AreEqual("changed@example.test",data.PlayerAccount[0].emailName);
            window.Cancel();window.Show();yield return null;
            Assert.IsFalse(account.gameObject.activeSelf);Assert.AreEqual("Spin 0/20 times",bottom.TaskText.text);
            Assert.IsTrue(bottom.Button.gameObject.activeInHierarchy);
            window.Unbind();Object.Destroy(window.gameObject);yield return null;
            var nativeLoader=new ConfigSnapshotLoader();yield return nativeLoader.Load("RecoveredConfig/Remote/cp_test.json");
            var nativeRules=new RecoveredGameplayRules(nativeLoader.Value);
            var nativePlayer=new RecoveredPlayerProgress(nativeRules,()=>{},new PlayerData{GreenCount=nativeRules.GetCashOutCash(0)});
            int started=unchecked((int)DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            new RecoveredCashOutSubmission(nativeRules,nativePlayer,new LocalCashFacade()).Submit(0,1,"Test Player","test@example.test",started);
            window=Object.Instantiate(Resources.Load<RecoveredCashOutWindow>("RecoveredUI/CashOutWindow"),portraitHost.transform,false);
            window.Bind(nativeRules,nativePlayer,0,canvas,()=>unchecked((int)DateTimeOffset.UtcNow.ToUnixTimeSeconds()));window.Show();
            bottom=window.GetComponentInChildren<RecoveredCashOutBottom>(true);
            yield return new WaitForSeconds(.4f);Canvas.ForceUpdateCanvases();
            RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});RenderTexture.active=target;
            capture.ReadPixels(new Rect(0,0,1080,1920),0,0);capture.Apply();
            File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/cash-withdrawal-tasks.png"),capture.EncodeToPNG());
            var spinTask=new PlayerCashOutData{id=0,step=0};var adTask=new PlayerCashOutData{id=1,step=1};
            game.PlayerStore.Data.PlayerCashOutDatas.Add(spinTask);game.PlayerStore.Data.PlayerCashOutDatas.Add(adTask);
            game.Ads.PlayRewardAd(()=>{},()=>{},"review-test","review-test");game.Ads.Complete(AdOutcome.Failed);
            Assert.AreEqual(0,adTask.count);
            game.Ads.PlayRewardAd(()=>{},()=>{},"review-test","review-test");game.Ads.Complete(AdOutcome.Rewarded);
            Assert.AreEqual(1,adTask.count);Assert.IsFalse(game.Ads.Complete(AdOutcome.Rewarded));Assert.AreEqual(1,adTask.count);
            var treasureTask=new PlayerCashOutData{id=2,step=4};game.PlayerStore.Data.PlayerCashOutDatas.Add(treasureTask);
            var treasureWindow=game.GetComponentInChildren<RecoveredTreasureWindow>(true);
            Assert.IsNotNull(treasureWindow);treasureWindow.Show(game.Rules.GetCollectInfos()[0].id,null,null);
            Assert.AreEqual(1,treasureTask.count);
            game.Playfield.SpinButton.Button.onClick.Invoke();Assert.AreEqual(1,spinTask.count);
            game.Playfield.SpinButton.Button.onClick.Invoke();Assert.AreEqual(1,spinTask.count,"Busy clicks must not count twice.");
            window.Unbind();Assert.IsFalse(window.gameObject.activeSelf);
        }
        finally
        {
            if(window!=null)Object.Destroy(window.gameObject);Time.timeScale=scale;UnityEngine.Random.state=random;RenderTexture.active=prior;
            if(portraitHost!=null)Object.Destroy(portraitHost);
            if(camera!=null)camera.targetTexture=null;if(capture!=null)Object.Destroy(capture);if(target!=null){target.Release();Object.Destroy(target);}
            if(scene.IsValid()&&scene.isLoaded)unload=SceneManager.UnloadSceneAsync(scene);
            if(had)PlayerPrefs.SetString(key,saved);else PlayerPrefs.DeleteKey(key);
        }
        if(unload!=null)yield return unload;
    }
}
