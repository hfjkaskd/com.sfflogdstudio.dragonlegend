using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public sealed class RecoveredDailyTaskTests
{
    [TestCase("cp_test.json",true)]
    [TestCase("cp_default_1.json",false)]
    public void NativeStartupResetsOnlyDefaultAndReceiptPrecedesCash(string snapshot,bool resets)
    {
        var config=JsonUtility.FromJson<GoldenDragonAutoGenConfig>(File.ReadAllText(Path.Combine(Application.streamingAssetsPath,"RecoveredConfig/Remote/"+snapshot)));
        var rules=new RecoveredGameplayRules(config);var data=new PlayerData();string saved=null;
        data.LoginTime=(int)new DateTimeOffset(DateTime.Today.AddDays(-1)).ToUnixTimeSeconds();
        data.PlayerTaskDatas.Add(new PlayerTaskData{id=1,count=99,isRecieve=true});
        var player=new RecoveredPlayerProgress(rules,()=>saved=JsonUtility.ToJson(data),data);
        player.InitializeDailyTasks(DateTime.Now);Assert.AreEqual(resets,player.FindTask(1)==null);Assert.AreEqual(1,player.FindTask(6).count);
        if(!resets)player.ClearDailyTasks();
        player.SetTaskData(1,1);player.SetTaskData(1,99);
        float balance=player.GreenCount;Assert.IsTrue(player.TryClaimTask(1,out float reserved));Assert.Greater(reserved,0);
        Assert.AreEqual(balance,player.GreenCount,"ClaimTaskReward only reserves the task; the cash flight pays later.");
        Assert.IsFalse(player.TryClaimTask(1,out _));
        var restored=JsonUtility.FromJson<PlayerData>(saved);var again=new RecoveredPlayerProgress(rules,()=>{},restored);
        Assert.IsFalse(again.TryClaimTask(1,out _));
        player.ClearDailyTasks();Assert.IsNull(player.FindTask(6),"Window midnight clear does not regrant login.");
        Assert.IsFalse(player.CanClaimTask(1),"A missing progress record cannot be claimed.");
        player.SetTaskData(1,1);if(!resets)Assert.IsFalse(player.CanClaimTask(1));
    }
    [TestCase("cp_test.json")]
    [TestCase("cp_default_1.json")]
    [TestCase("cp_low_frequency_high_rewards.json")]
    public void DailyTasksRequireTheirConfiguredGoalAndPersistReceipts(string snapshot)
    {
        var config=JsonUtility.FromJson<GoldenDragonAutoGenConfig>(File.ReadAllText(Path.Combine(Application.streamingAssetsPath,"RecoveredConfig/Remote/"+snapshot)));
        var rules=new RecoveredGameplayRules(config);var data=new PlayerData();string saved=null;
        var player=new RecoveredPlayerProgress(rules,()=>saved=JsonUtility.ToJson(data),data);
        foreach(var info in rules.GetTaskInfos()) {
            Assert.IsFalse(player.CanClaimTask(info.id));Assert.IsFalse(player.TryClaimTask(info.id,out _));
            for(int count=1;count<=info.taskAmount;count++) {
                player.SetTaskData(info.id,1);
                Assert.AreEqual(count==info.taskAmount,player.CanClaimTask(info.id));
                if(count<info.taskAmount)Assert.IsFalse(player.TryClaimTask(info.id,out _));
                Assert.IsFalse(player.FindTask(info.id).isRecieve);
            }
            float balance=player.GreenCount;
            Assert.IsTrue(player.TryClaimTask(info.id,out float reward));Assert.AreEqual(info.reward,reward);
            Assert.AreEqual(balance,player.GreenCount);Assert.IsTrue(player.FindTask(info.id).isRecieve);
            Assert.IsFalse(player.CanClaimTask(info.id));Assert.IsFalse(player.TryClaimTask(info.id,out _));
            var restored=new RecoveredPlayerProgress(rules,()=>{},JsonUtility.FromJson<PlayerData>(saved));
            Assert.IsFalse(restored.CanClaimTask(info.id));Assert.IsFalse(restored.TryClaimTask(info.id,out _));
        }
        player.ClearDailyTasks();foreach(var info in rules.GetTaskInfos())Assert.IsFalse(player.CanClaimTask(info.id));
        Assert.IsFalse(player.CanClaimTask(999));
    }
    [UnityTest]
    public IEnumerator EntryOpensScrollableTasksAndLoginClaimPaysOnlyOnce()
    {
        string key=RecoveredPlayerStore.OriginalKey;bool had=PlayerPrefs.HasKey(key);string saved=PlayerPrefs.GetString(key);PlayerPrefs.DeleteKey(key);
        Scene scene=default;AsyncOperation unload=null;RenderTexture target=null;Texture2D capture=null;var prior=RenderTexture.active;Camera camera=null;
        try {
            yield return SceneManager.LoadSceneAsync("GameEntry",LoadSceneMode.Additive);scene=SceneManager.GetSceneByName("GameEntry");
            GameEntry game=null;foreach(var root in scene.GetRootGameObjects()){var found=root.GetComponentInChildren<GameEntry>();if(found!=null)game=found;}
            float deadline=Time.realtimeSinceStartup+15;while(game.CoreRound==null&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.IsNotNull(game.DailyTasks);Assert.AreEqual("default",game.Rules.GetConfigType());game.CoreRound.FirstSpinGuide.Hide();
            game.DailyTasks.Button.onClick.Invoke();var window=game.DailyTasks.Window;Assert.IsTrue(window.gameObject.activeSelf);Assert.AreEqual(6,window.Items.Length);
            yield return null;Canvas.ForceUpdateCanvases();var scroll=window.GetComponentInChildren<ScrollRect>();Assert.Greater(scroll.content.rect.height,scroll.viewport.rect.height);
            float balance=game.PlayerProgress.GreenCount;window.Items[5].ClaimButton.onClick.Invoke();window.Items[5].ClaimButton.onClick.Invoke();
            Assert.AreEqual(balance,game.PlayerProgress.GreenCount);yield return new WaitForSeconds(4);
            Assert.AreEqual(balance+500,game.PlayerProgress.GreenCount);Assert.IsTrue(game.PlayerProgress.FindTask(6).isRecieve);
            game.PlayerProgress.SetTaskData(1,1);
            var bigTask=game.Rules.GetTaskInfos()[0];
            AssertTaskPresentation(window.Items[0],bigTask.taskAmount<=1,false);
            if(bigTask.taskAmount>1) {
                float incompleteBalance=game.PlayerProgress.GreenCount;
                window.Claim(bigTask);Assert.IsFalse(game.PlayerProgress.FindTask(1).isRecieve);
                Assert.AreEqual(incompleteBalance,game.PlayerProgress.GreenCount);
            }
            while(game.PlayerProgress.FindTask(1).count<bigTask.taskAmount)game.PlayerProgress.SetTaskData(1,1);
            AssertTaskPresentation(window.Items[0],true,false);window.Items[0].ClaimButton.onClick.Invoke();
            var reward=window.GetComponentInChildren<RecoveredTaskRewardWindow>(true);Assert.IsTrue(reward.gameObject.activeSelf);
            balance=game.PlayerProgress.GreenCount;reward.AdButton.onClick.Invoke();Assert.IsTrue(game.Ads.Pending);game.Ads.Complete(AdOutcome.Failed);
            Assert.AreEqual("lucky",game.Ads.Placement);Assert.AreEqual(balance,game.PlayerProgress.GreenCount);Assert.IsTrue(game.PlayerProgress.FindTask(1).isRecieve);
            reward.AdButton.onClick.Invoke();game.Ads.Complete(AdOutcome.Rewarded);yield return new WaitForSeconds(5);Assert.AreEqual(balance+4000,game.PlayerProgress.GreenCount);
            Assert.IsTrue(game.PlayerProgress.FindTask(1).isRecieve);Assert.IsFalse(game.Ads.Complete(AdOutcome.Rewarded));
            game.PlayerProgress.SetTaskData(2,1);
            var jackpotTask=game.Rules.GetTaskInfos()[1];
            while(game.PlayerProgress.FindTask(2).count<jackpotTask.taskAmount)game.PlayerProgress.SetTaskData(2,1);
            balance=game.PlayerProgress.GreenCount;
            window.Items[1].ClaimButton.onClick.Invoke();Assert.IsTrue(game.PlayerProgress.FindTask(2).isRecieve);
            reward.PlainButton.onClick.Invoke();yield return new WaitForSeconds(5);
            Assert.AreEqual(balance+1500,game.PlayerProgress.GreenCount,"Native UIRewardView plain claim applies the half multiplier through cash flight.");
            window.CloseButton.onClick.Invoke();Assert.IsFalse(window.gameObject.activeSelf);game.DailyTasks.Button.onClick.Invoke();Assert.IsFalse(window.Items[5].ClaimButton.gameObject.activeSelf);AssertTaskPresentation(window.Items[0],false,true);
            camera=game.GetComponent<Canvas>().worldCamera;target=new RenderTexture(1080,1920,24);camera.targetTexture=target;
            yield return new WaitForSeconds(3.5f);Canvas.ForceUpdateCanvases();
            // Use actual raycasts to prove the popup does not cover its own controls.
            var rect=(RectTransform)window.CloseButton.transform;var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(rect.rect.center))};
            var hits=new List<RaycastResult>();Canvas.ForceUpdateCanvases();EventSystem.current.RaycastAll(pointer,hits);
            game.CoreRound.Tips.Cancel();game.Ads.CompleteInterstitial(false);
            RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});RenderTexture.active=target;
            capture=new Texture2D(1080,1920,TextureFormat.RGB24,false);capture.ReadPixels(new Rect(0,0,1080,1920),0,0);capture.Apply();
            File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/daily-tasks.png"),capture.EncodeToPNG());
            Assert.IsNotEmpty(hits,"Close button screen position: "+pointer.position);Assert.AreEqual(window.CloseButton,hits[0].gameObject.GetComponentInParent<Button>());
            window.CloseButton.onClick.Invoke();
            game.PlayerProgress.GameSlotType=RecoveredSlotType.Free;game.PlayerProgress.FreeSpinCount=7;
            game.FreeSpinResult.Begin(new[]{0,1,2,3,4,5,6});int steps=0;while(game.FreeSpinResult.IsGenerating&&steps++<10000)game.FreeSpinResult.Step();
            Assert.IsFalse(game.FreeSpinResult.IsGenerating);var mode=game.Playfield.ModeView;mode.ApplyCurrent();mode.InitializeFreeReels();mode.RefreshFreeCount();
            var ordinary=new List<SpriteRenderer>();
            for(int col=0;col<5;col++)for(int row=0;row<3;row++) {
                int id=game.FreeSpinResult.GetSymbol(col,row);if(id==9||id==11)continue;
                var reel=mode.FreeReels.At(col,row);reel.ApplyFreeStoppedSymbol(id);ordinary.Add(reel.SymbolAt(0).Symbol);
            }
            Assert.IsNotEmpty(ordinary);yield return null;Canvas.ForceUpdateCanvases();
            RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});RenderTexture.active=target;
            capture.ReadPixels(new Rect(0,0,1080,1920),0,0);capture.Apply();var visible=capture.GetPixels32();
            File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/free-symbols-full-scene.png"),capture.EncodeToPNG());
            var fireParent=game.Playfield.Npc.transform.Find("PlayFire");
            Assert.IsTrue(fireParent.GetComponent<Canvas>().overrideSorting);
            Assert.Greater(fireParent.GetComponent<Canvas>().sortingOrder,mode.FreeReels.At(0,0).GetComponent<UnityEngine.Rendering.SortingGroup>().sortingOrder);
            var fire=fireParent.GetComponentInChildren<RecoveredRegionAnimator>(true);game.Playfield.Npc.Show(1);yield return new WaitForSeconds(.95f);
            Assert.IsTrue(fire.gameObject.activeInHierarchy);
            Canvas.ForceUpdateCanvases();RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});RenderTexture.active=target;
            capture.ReadPixels(new Rect(0,0,1080,1920),0,0);capture.Apply();
            File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/free-fire-foreground.png"),capture.EncodeToPNG());fire.gameObject.SetActive(false);
            foreach(var symbol in ordinary)symbol.enabled=false;
            RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});RenderTexture.active=target;
            capture.ReadPixels(new Rect(0,0,1080,1920),0,0);capture.Apply();var hidden=capture.GetPixels32();int changed=0;
            for(int i=0;i<visible.Length;i++)if(Math.Abs(visible[i].r-hidden[i].r)+Math.Abs(visible[i].g-hidden[i].g)+Math.Abs(visible[i].b-hidden[i].b)>15)changed++;
            Assert.Greater(changed,2000,"Ordinary stopped symbols must render above the complete board UI, through the dim cover.");
            window.BeginTimer(DateTime.Today.AddDays(1).AddSeconds(-2));window.CloseButton.onClick.Invoke();
            window.AdvanceTimer(0);Assert.AreEqual(2,window.RemainingSeconds);Assert.IsNotNull(game.PlayerProgress.FindTask(6));
            window.AdvanceTimer(2);Assert.IsNull(game.PlayerProgress.FindTask(6));Assert.AreEqual(0,window.RemainingSeconds);AssertTaskPresentation(window.Items[0],false,false);
        } finally {
            RenderTexture.active=prior;if(camera!=null)camera.targetTexture=null;if(target!=null){target.Release();Object.Destroy(target);}if(capture!=null)Object.Destroy(capture);
            if(scene.IsValid()&&scene.isLoaded)unload=SceneManager.UnloadSceneAsync(scene);
            if(had)PlayerPrefs.SetString(key,saved);else PlayerPrefs.DeleteKey(key);
        }
        if(unload!=null)yield return unload;
    }
    static void AssertTaskPresentation(RecoveredDailyTaskItem item,bool claimable,bool received)
    {
        Assert.AreEqual(!received,item.ClaimButton.gameObject.activeSelf);
        Assert.AreEqual(claimable,item.ClaimButton.interactable);
        Image tick=null;foreach(var image in item.GetComponentsInChildren<Image>(true))
            if(image.sprite!=null&&image.sprite.name=="re_gou")tick=image;
        Assert.IsNotNull(tick);Assert.AreEqual(received,tick.gameObject.activeSelf);
        Assert.AreEqual(received,item.transform.Find("black").gameObject.activeSelf);
    }
}
