using System;
using System.Collections;
using DragonLegend.Whitebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class RecoveredOfflineSpinLoadTests
{
    [UnityTest]
    public IEnumerator LoadingRealDefaultProfileRecoversSavedIntervalsOnlyOnce()
    {
        string key=RecoveredPlayerStore.OriginalKey;bool had=PlayerPrefs.HasKey(key);string saved=PlayerPrefs.GetString(key);
        PlayerPrefs.DeleteKey(key);var random=UnityEngine.Random.state;Scene scene=default;AsyncOperation unload=null;
        try {
            yield return SceneManager.LoadSceneAsync("GameEntry",LoadSceneMode.Additive);scene=SceneManager.GetSceneByName("GameEntry");
            GameEntry game=null;foreach(var root in scene.GetRootGameObjects()){var value=root.GetComponentInChildren<GameEntry>();if(value!=null)game=value;}
            Assert.IsNotNull(game);float deadline=Time.realtimeSinceStartup+10;
            while(game.CoreRound==null&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.IsNotNull(game.CoreRound);
            var button=game.transform.Find("SelectBundledDefault").GetComponent<Button>();button.onClick.Invoke();
            deadline=Time.realtimeSinceStartup+10;while(game.CoreRound==null&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.IsNotNull(game.CoreRound);Assert.AreEqual("default",game.Rules.GetConfigType());
            int max=game.Rules.GetMaxSpinCount(),cd=game.Rules.GetSpinCD(game.PlayerProgress.Level);
            Assert.Greater(max,3);Assert.Greater(cd,10);
            game.PlayerProgress.SetSpinCount(max-3);
            int last=unchecked((int)DateTimeOffset.UtcNow.ToUnixTimeSeconds()-2*cd-5);
            game.PlayerStore.Data.LastSpinTime=last;game.PlayerStore.Save();
            var prior=game.PlayerProgress;
            button.onClick.Invoke();
            deadline=Time.realtimeSinceStartup+10;while(game.CoreRound==null&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.IsNotNull(game.CoreRound);Assert.AreNotSame(prior,game.PlayerProgress);
            Assert.AreEqual(max-1,game.PlayerProgress.SpinCount);
            Assert.AreEqual(last+2*cd,game.PlayerStore.Data.LastSpinTime);
            var timer=game.Playfield.SpinRecovery.Recovery;Assert.IsTrue(timer.IsRunning);
            Assert.That(timer.RemainingSeconds,Is.InRange(cd-10,cd-5));
            string recoveredSave=PlayerPrefs.GetString(key);
            var persisted=JsonUtility.FromJson<DragonLegend.Whitebox.Recovered.PlayerData>(recoveredSave);
            Assert.AreEqual(max-1,persisted.SpinCount);Assert.AreEqual(last+2*cd,persisted.LastSpinTime);
            button.onClick.Invoke();
            deadline=Time.realtimeSinceStartup+10;while(game.CoreRound==null&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.IsNotNull(game.CoreRound);Assert.IsFalse(timer.IsRunning);
            Assert.AreEqual(max-1,game.PlayerProgress.SpinCount,"A second load must not grant the same elapsed intervals again.");
            Assert.AreEqual(last+2*cd,game.PlayerStore.Data.LastSpinTime);
            Assert.AreEqual(recoveredSave,PlayerPrefs.GetString(key));
            game.PlayerStore.Data.LastSpinTime=unchecked((int)DateTimeOffset.UtcNow.ToUnixTimeSeconds()-2*cd-5);
            game.PlayerStore.Save();
            button.onClick.Invoke();
            deadline=Time.realtimeSinceStartup+10;while(game.CoreRound==null&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.IsNotNull(game.CoreRound);Assert.AreEqual(max,game.PlayerProgress.SpinCount);
            Assert.IsFalse(game.Playfield.SpinRecovery.Recovery.IsRunning);
            persisted=JsonUtility.FromJson<DragonLegend.Whitebox.Recovered.PlayerData>(PlayerPrefs.GetString(key));
            Assert.AreEqual(max,persisted.SpinCount,"Recovered count must persist at the configured cap.");
            TestContext.WriteLine("Default maximum: "+max+"; cooldown: "+cd+"; restored intervals: 2");
        } finally {
            UnityEngine.Random.state=random;
            if(scene.IsValid()&&scene.isLoaded)unload=SceneManager.UnloadSceneAsync(scene);
            if(had)PlayerPrefs.SetString(key,saved);else PlayerPrefs.DeleteKey(key);
        }
        if(unload!=null)yield return unload;
    }
}
