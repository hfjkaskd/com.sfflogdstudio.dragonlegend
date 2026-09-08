using System.Collections;
using System.Collections.Generic;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

public sealed class RecoveredFreeSlotGameTests
{
    [UnityTest]
    public IEnumerator RoutedSlotRecordsTaskPulsesThenRunsActualWindowAndCreditsClaim()
    {
        string key=RecoveredPlayerStore.OriginalKey;bool had=PlayerPrefs.HasKey(key);string saved=PlayerPrefs.GetString(key);PlayerPrefs.DeleteKey(key);
        float scale=Time.timeScale,delta=Time.captureDeltaTime;var random=Random.state;
        Scene scene=default;AsyncOperation unload=null;RecoveredFreeSlotGame game=null;
        try {
            Time.timeScale=1;Time.captureDeltaTime=.05f;
            yield return SceneManager.LoadSceneAsync("GameEntry",LoadSceneMode.Additive);scene=SceneManager.GetSceneByName("GameEntry");
            GameEntry entry=null;foreach(var root in scene.GetRootGameObjects()){var value=root.GetComponentInChildren<GameEntry>();if(value!=null)entry=value;}
            Assert.IsNotNull(entry);float deadline=Time.realtimeSinceStartup+5;
            while(entry.CashFlight==null&&Time.realtimeSinceStartup<deadline)yield return null;Assert.IsNotNull(entry.CashFlight);
            game=Object.Instantiate(Resources.Load<RecoveredFreeSlotGame>("RecoveredUI/FreeSlotGame"),entry.transform,false);
            game.Bind(entry.PlayerProgress,entry.Rules,entry.Ads,entry.CashFlight,entry.transform,entry.CurrentProfile.isA,entry.CurrentProfile.languageType);
            var rules=new RecoveredGameplayRules(new GoldenDragonAutoGenConfig{Rrggiomg=new RrggiomgPoro{
                RollGlorg=new List<int>{1,1,1},RollKtggl=new List<int>{0,0,0},RollRrgogirg=new List<int>{0,0,0},RollLiqki=new List<int>{0,0,0}}});
            System.Action<Vector3,System.Action<float>> unexpected=(p,c)=>Assert.Fail("Expected the configured Slot branch.");
            var router=new RecoveredFreeSmallGameRouter(rules,entry.PlayerProgress,game.Begin,unexpected,unexpected,unexpected);
            float balance=entry.PlayerProgress.GreenCount;int callbacks=0;float paid=0;
            Vector3 source=entry.transform.TransformPoint(new Vector3(0,100,0));
            router.Open(0,source,value=>{callbacks++;paid=value;Assert.AreEqual(balance,entry.PlayerProgress.GreenCount);});
            Assert.AreEqual(source,game.Icon.position);Assert.AreEqual(new Vector2(244,186),game.Icon.sizeDelta);
            var stored=JsonUtility.FromJson<PlayerData>(PlayerPrefs.GetString(key));PlayerTaskData task=null;
            foreach(var record in stored.PlayerTaskDatas)if(record.id==4)task=record;
            Assert.IsNotNull(task);Assert.AreEqual(1,task.count);Assert.IsTrue(game.IsRunning);Assert.IsFalse(game.Window.Window.activeSelf);
            // No reward draw occurs on dispatch or while paused; it belongs after the icon delay.
            Random.InitState(719);int next=Random.Range(0,1000000);Random.InitState(719);
            Time.timeScale=0;for(int i=0;i<3;i++)yield return null;Assert.AreEqual(next,Random.Range(0,1000000));
            Random.InitState(37);float expected=entry.Rules.GetSlotReward();Random.InitState(37);
            Time.timeScale=1;for(int i=0;i<6;i++)yield return null;
            Assert.That(game.Icon.localScale.x,Is.EqualTo(1.5f).Within(.0001f));Assert.AreEqual(0,game.Icon.localScale.z);
            Assert.IsFalse(game.Window.Window.activeSelf);
            for(int i=0;i<8&&!game.Window.Window.activeSelf;i++)yield return null;
            Assert.IsNull(game.Error);Assert.IsTrue(game.Window.Window.activeSelf);Assert.IsFalse(game.Icon.gameObject.activeSelf);
            for(int i=0;i<160&&!game.Window.Popup.gameObject.activeSelf;i++)yield return null;
            Assert.IsNull(game.Window.Error);Assert.IsTrue(game.Window.Popup.gameObject.activeSelf);
            Assert.AreEqual(expected,game.Window.Popup.Claim.OriginalReward);Assert.AreEqual(0,callbacks);
            for(int i=0;i<50&&!game.Window.Popup.PlainButton.gameObject.activeInHierarchy;i++)yield return null;
            game.Window.Popup.PlainButton.onClick.Invoke();deadline=Time.realtimeSinceStartup+5;
            while(callbacks==0&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.AreEqual(1,callbacks);Assert.AreEqual(expected*.5f,paid);Assert.AreEqual(balance+paid,entry.PlayerProgress.GreenCount);
            Assert.IsFalse(game.IsRunning);Assert.IsFalse(game.Window.IsRunning);
        } finally {
            if(game!=null)Object.Destroy(game.gameObject);if(scene.IsValid())unload=SceneManager.UnloadSceneAsync(scene);
            Time.timeScale=scale;Time.captureDeltaTime=delta;Random.state=random;
            if(had)PlayerPrefs.SetString(key,saved);else PlayerPrefs.DeleteKey(key);
        }
        if(unload!=null)yield return unload;
    }
}
