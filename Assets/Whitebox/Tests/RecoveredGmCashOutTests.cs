using System.Collections;
using System.IO;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.UI;

public sealed class RecoveredGmCashOutTests
{
    [UnityTest]
    public IEnumerator CommandsKeepProgressAndWaitIndependentForEveryTierAndBranch()
    {
        var loader=new ConfigSnapshotLoader();yield return loader.Load("RecoveredConfig/Remote/cp_test.json");
        var rules=new RecoveredGameplayRules(loader.Value);const int now=1800000000;
        for(int tier=0;tier<rules.GetCashOutCount();tier++)for(int branch=0;branch<2;branch++)for(int step=0;step<7;step++){
            var data=new PlayerData();var player=new RecoveredPlayerProgress(rules,()=>{},data);
            var gm=new RecoveredGmCashOutTools(rules,player,data,()=>{});gm.Prepare(tier,step,branch==1,now);
            var before=player.GetCashOutConditions(tier,now);
            Assert.AreEqual(Mathf.Max(0,before.TaskGoal-1),before.TaskCount);
            Assert.IsFalse(before.WaitComplete);Assert.GreaterOrEqual(player.GreenCount,rules.GetCashOutCash(tier));
            gm.AddProgress(tier,now,false);Assert.IsTrue(player.GetCashOutConditions(tier,now).TaskComplete);
            Assert.IsFalse(player.GetCashOutConditions(tier,now).WaitComplete);
            gm.SetWait(tier,now,true);Assert.IsTrue(player.GetCashOutConditions(tier,now).WaitComplete);
            Assert.AreEqual(step,gm.Record(tier).step,"GM must not bypass the real continuation button.");
            gm.SetNearGoal(tier,now);gm.AddProgress(tier,now,true);Assert.IsTrue(player.GetCashOutConditions(tier,now).TaskComplete);
            if(step==6)Assert.AreEqual(rules.GetCollectInfoCount(),data.PlayerCollectDatas.Count);
        }
    }

    [UnityTest]
    public IEnumerator ChinesePanelBacksUpEditsAndExercisesTheRealWithdrawalButton()
    {
        string key=RecoveredPlayerStore.OriginalKey,backupKey=RecoveredGmCashOutPanel.BackupKey;
        bool had=PlayerPrefs.HasKey(key),hadBackup=PlayerPrefs.HasKey(backupKey);
        string saved=PlayerPrefs.GetString(key),savedBackup=PlayerPrefs.GetString(backupKey);
        var random=Random.state;float oldDelta=Time.captureDeltaTime;Scene scene=default;AsyncOperation unload=null;
        RenderTexture target=null;Camera camera=null;var previous=RenderTexture.active;
        PlayerPrefs.SetString(key,JsonUtility.ToJson(new PlayerData{GuideStep=3,SpinCount=70}));PlayerPrefs.DeleteKey(backupKey);
        try{
            Time.captureDeltaTime=.05f;
            yield return SceneManager.LoadSceneAsync("GameEntry",LoadSceneMode.Additive);scene=SceneManager.GetSceneByName("GameEntry");
            GameEntry game=null;foreach(var root in scene.GetRootGameObjects()){var entry=root.GetComponentInChildren<GameEntry>();if(entry!=null)game=entry;}
            for(int i=0;i<300&&game.CoreRound==null;i++)yield return null;
            var panel=game.GetComponent<RecoveredGmCashOutPanel>();Assert.IsNotNull(panel);Assert.IsFalse(panel.IsOpen);
            var mainGm=game.GetComponent<RecoveredGmPanel>();mainGm.ToggleButton.onClick.Invoke();panel.OpenButton.onClick.Invoke();
            Assert.IsTrue(panel.IsOpen);StringAssert.Contains("提现任务验证",panel.Status.text);Assert.IsTrue(panel.Status.font.HasCharacter('提'));
            string before=JsonUtility.ToJson(game.PlayerStore.Data);
            panel.ActionButton(6).onClick.Invoke();Assert.AreEqual(before,PlayerPrefs.GetString(backupKey));
            var record=game.PlayerProgress.CashOutRecords[0];Assert.AreEqual(19,record.count);Assert.AreEqual(0,record.step);
            panel.ActionButton(8).onClick.Invoke();Assert.AreEqual(20,record.count);
            panel.ActionButton(13).onClick.Invoke();Assert.IsFalse(panel.IsOpen);
            for(int i=0;i<30;i++)yield return null;
            var bottom=game.CoreRound.CashOut.GetComponentInChildren<RecoveredCashOutBottom>(true);
            Assert.IsTrue(bottom.TaskComplete);Assert.IsFalse(bottom.WaitComplete);bottom.Button.onClick.Invoke();Assert.AreEqual(0,record.step);
            panel.Open();panel.ActionButton(10).onClick.Invoke();panel.ActionButton(13).onClick.Invoke();
            bottom.Button.onClick.Invoke();Assert.AreEqual(1,record.step);Assert.AreEqual(0,record.count);
            panel.Open();for(int i=0;i<6;i++)panel.ActionButton(4).onClick.Invoke();panel.ActionButton(6).onClick.Invoke();
            panel.ActionButton(9).onClick.Invoke();panel.ActionButton(10).onClick.Invoke();panel.ActionButton(13).onClick.Invoke();
            Assert.IsTrue(bottom.TaskComplete);Assert.IsTrue(bottom.WaitComplete);bottom.Button.onClick.Invoke();Assert.AreEqual(6,record.step,"Native collection terminal must remain blocked.");
            panel.Open();
            camera=game.GetComponent<Canvas>().worldCamera;target=new RenderTexture(1080,1920,24);camera.targetTexture=target;
            Canvas.ForceUpdateCanvases();yield return null;
            RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});RenderTexture.active=target;
            var image=new Texture2D(1080,1920,TextureFormat.RGB24,false);
            try{image.ReadPixels(new Rect(0,0,1080,1920),0,0);image.Apply();File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/gm-cashout-chinese.png"),image.EncodeToPNG());}finally{Object.Destroy(image);}
            panel.ActionButton(12).onClick.Invoke();Assert.IsFalse(PlayerPrefs.HasKey(backupKey));
            for(int i=0;i<300&&game.CoreRound==null;i++)yield return null;
            Assert.AreEqual(0,game.PlayerProgress.CashOutRecords.Count);Assert.AreEqual(70,game.PlayerProgress.SpinCount);
        }finally{
            if(camera!=null)camera.targetTexture=null;RenderTexture.active=previous;if(target!=null)Object.Destroy(target);
            if(scene.IsValid())unload=SceneManager.UnloadSceneAsync(scene);Random.state=random;Time.captureDeltaTime=oldDelta;
            if(had)PlayerPrefs.SetString(key,saved);else PlayerPrefs.DeleteKey(key);
            if(hadBackup)PlayerPrefs.SetString(backupKey,savedBackup);else PlayerPrefs.DeleteKey(backupKey);
        }
        if(unload!=null)yield return unload;
    }
}
