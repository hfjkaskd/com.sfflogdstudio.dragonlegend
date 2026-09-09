using System.Collections;
using DragonLegend.Whitebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class RecoveredCashFlightTeardownTests
{
    [UnityTest]
    public IEnumerator ClosingMainDuringScatterOrCollectionCancelsDetachedObjectsWithoutReparenting()
    {
        string key=RecoveredPlayerStore.OriginalKey;bool had=PlayerPrefs.HasKey(key);string saved=PlayerPrefs.GetString(key);PlayerPrefs.DeleteKey(key);
        var random=Random.state;float scale=Time.timeScale,delta=Time.captureDeltaTime;Scene scene=default;AsyncOperation unload=null;
        try {
            Time.timeScale=1;Time.captureDeltaTime=.025f;
            yield return SceneManager.LoadSceneAsync("GameEntry",LoadSceneMode.Additive);scene=SceneManager.GetSceneByName("GameEntry");
            GameEntry game=null;foreach(var root in scene.GetRootGameObjects()){var found=root.GetComponentInChildren<GameEntry>();if(found!=null)game=found;}
            for(int phase=0;phase<2;phase++)
            {
                float deadline=Time.realtimeSinceStartup+10;while(game.CashFlight==null&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.IsNotNull(game.CashFlight);var flight=game.CashFlight;var player=game.PlayerProgress;int callbacks=0;
                flight.Begin(75,()=>callbacks++,game.transform,true);
                Assert.AreEqual(10,flight.ActiveCashCount);
                if(phase==1)
                {
                    deadline=Time.realtimeSinceStartup+8;
                    while(flight.ActiveEffectCount==0&&Time.realtimeSinceStartup<deadline)yield return null;
                    Assert.Greater(flight.ActiveEffectCount,0,"Exercise collection effects parented outside the pool.");
                }
                var cash=game.GetComponentsInChildren<RecoveredCashFlightItem>();
                var effects=game.GetComponentsInChildren<RecoveredCashCollectionEffect>();
                float balance=player.GreenCount;int completed=callbacks;
                game.gameObject.SetActive(false);
                Assert.AreEqual(0,flight.ActiveCashCount);Assert.AreEqual(0,flight.ActiveEffectCount);
                foreach(var item in cash)Assert.IsFalse(item.gameObject.activeSelf);
                foreach(var effect in effects)Assert.IsFalse(effect.gameObject.activeSelf);
                for(int frame=0;frame<30;frame++)yield return null;
                Assert.AreEqual(completed,callbacks);Assert.AreEqual(balance,player.GreenCount);
                foreach(var item in cash)Assert.IsTrue(item==null);
                foreach(var effect in effects)Assert.IsTrue(effect==null);
                game.gameObject.SetActive(true);
                deadline=Time.realtimeSinceStartup+10;while(game.CoreRound==null&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.IsNotNull(game.CoreRound);Assert.AreNotSame(flight,game.CashFlight);
                Assert.AreEqual(balance,game.PlayerProgress.GreenCount);
                Assert.AreEqual(0,game.CashFlight.ActiveCashCount);Assert.AreEqual(0,game.CashFlight.ActiveEffectCount);
            }
        } finally {
            Random.state=random;Time.timeScale=scale;Time.captureDeltaTime=delta;
            if(scene.IsValid()&&scene.isLoaded)unload=SceneManager.UnloadSceneAsync(scene);
            if(had)PlayerPrefs.SetString(key,saved);else PlayerPrefs.DeleteKey(key);
        }
        if(unload!=null)yield return unload;
    }
}
