using System.Collections;
using DragonLegend.Whitebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class RecoveredInterruptedSpinLoadTests
{
    [UnityTest]
    public IEnumerator AcceptedFirstSpinReloadsSavedDebitWithoutInventingSettlement()
    {
        string key=RecoveredPlayerStore.OriginalKey;
        bool had=PlayerPrefs.HasKey(key);string saved=PlayerPrefs.GetString(key);
        var random=Random.state;Scene scene=default;AsyncOperation unload=null;
        try
        {
            PlayerPrefs.DeleteKey(key);Random.InitState(71);
            yield return SceneManager.LoadSceneAsync("GameEntry",LoadSceneMode.Additive);
            scene=SceneManager.GetSceneByName("GameEntry");var game=FindEntry(scene);
            float deadline=Time.realtimeSinceStartup+10;
            while(game.CoreRound==null&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.IsNotNull(game.CoreRound);
            int spins=game.PlayerProgress.SpinCount;float cash=game.PlayerStore.Data.GreenCount;
            var oldProgress=game.PlayerProgress;int completed=0;
            game.CoreRound.CoreRoundCompleted+=()=>completed++;
            game.Playfield.SpinButton.Button.onClick.Invoke();
            Assert.IsTrue(game.Playfield.IsBusy);Assert.AreEqual(0,completed);
            Assert.AreEqual(spins-1,game.PlayerProgress.SpinCount);
            Assert.AreEqual(2,game.PlayerStore.Data.GuideStep);
            Assert.AreEqual(cash,game.PlayerStore.Data.GreenCount);
            string accepted=PlayerPrefs.GetString(key);
            Assert.AreEqual(JsonUtility.ToJson(game.PlayerStore.Data),accepted);
            // Stop the actual scene before the next frame can settle the reels.
            foreach(var root in scene.GetRootGameObjects())root.SetActive(false);
            unload=SceneManager.UnloadSceneAsync(scene);yield return unload;unload=null;scene=default;
            Assert.IsTrue(game==null);Assert.AreEqual(0,completed);
            Assert.AreEqual(accepted,PlayerPrefs.GetString(key));
            yield return SceneManager.LoadSceneAsync("GameEntry",LoadSceneMode.Additive);
            scene=SceneManager.GetSceneByName("GameEntry");game=FindEntry(scene);
            deadline=Time.realtimeSinceStartup+10;
            while(game.CoreRound==null&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.IsNotNull(game.CoreRound);Assert.AreNotSame(oldProgress,game.PlayerProgress);
            Assert.AreEqual(accepted,JsonUtility.ToJson(game.PlayerStore.Data));
            Assert.AreEqual(accepted,PlayerPrefs.GetString(key));
            Assert.IsFalse(game.Playfield.IsBusy);Assert.IsFalse(game.Playfield.AwaitingRewards);
            Assert.AreEqual(RecoveredSlotType.Base,game.PlayerProgress.GameSlotType);
            Assert.IsFalse(game.CoreRound.FirstSpinGuide.gameObject.activeInHierarchy);
            Assert.AreEqual(cash,game.PlayerStore.Data.GreenCount);
            game.Playfield.SpinButton.Button.onClick.Invoke();
            Assert.IsTrue(game.Playfield.IsBusy);
            Assert.AreEqual(spins-2,game.PlayerProgress.SpinCount);
        }
        finally
        {
            if(scene.IsValid()){
                foreach(var root in scene.GetRootGameObjects())root.SetActive(false);
                unload=SceneManager.UnloadSceneAsync(scene);
            }
            Random.state=random;
            if(had)PlayerPrefs.SetString(key,saved);else PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }
        if(unload!=null)yield return unload;
    }
    private static GameEntry FindEntry(Scene scene)
    {
        foreach(var root in scene.GetRootGameObjects()){
            var entry=root.GetComponentInChildren<GameEntry>();if(entry!=null)return entry;
        }
        Assert.Fail("Production GameEntry missing.");return null;
    }
}
