using System.Collections;
using DragonLegend.Whitebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class RecoveredCoreRoundFlowTests
{
    [UnityTest]
    public IEnumerator SpinSettlesThenForcedFreeRoundReturnsThroughActualEndWindow()
    {
        string key=RecoveredPlayerStore.OriginalKey;bool had=PlayerPrefs.HasKey(key);string saved=PlayerPrefs.GetString(key);PlayerPrefs.DeleteKey(key);
        var random=Random.state;float scale=Time.timeScale,delta=Time.captureDeltaTime;Scene scene=default;AsyncOperation unload=null;
        try {
            Time.timeScale=1;Time.captureDeltaTime=.05f;Random.InitState(71);
            yield return SceneManager.LoadSceneAsync("GameEntry",LoadSceneMode.Additive);scene=SceneManager.GetSceneByName("GameEntry");
            GameEntry game=null;foreach(var root in scene.GetRootGameObjects()){var found=root.GetComponentInChildren<GameEntry>();if(found!=null)game=found;}
            float deadline=Time.realtimeSinceStartup+10;while(game.CoreRound==null&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.IsNotNull(game.CoreRound);var field=game.Playfield;var core=game.CoreRound;int completed=0;core.CoreRoundCompleted+=()=>completed++;
            int spins=game.PlayerProgress.SpinCount;field.SpinButton.Button.onClick.Invoke();field.SpinButton.Button.onClick.Invoke();
            Assert.AreEqual(spins-1,game.PlayerProgress.SpinCount);Assert.IsTrue(field.IsBusy);
            for(int frame=0;frame<1800&&completed==0;frame++){Claim(field.BigWinPopup.PlainButton);Claim(field.JackpotPopup.PlainButton);yield return null;}
            Assert.IsNull(core.Error);Assert.AreEqual(1,completed);Assert.IsFalse(field.IsBusy);Assert.IsFalse(field.AwaitingRewards);
            int seed=-1;var beforeSearch=Random.state;
            for(int candidate=0;candidate<10000;candidate++){Random.InitState(candidate);game.Rules.GetFreeCoinAmount();if(game.Rules.GetFreeBallAmount()==0){seed=candidate;break;}}
            Random.state=beforeSearch;Assert.GreaterOrEqual(seed,0,"Fixture needs one Free result without a ball branch.");
            // Keep this core-loop fixture short. Other suites cover all four ball routes.
            core.Entry.ChangeMusicRequested+=name=>{game.PlayerProgress.FreeSpinCount=1;Random.InitState(seed);};
            game.SpinResult.ForceFreeSpin=true;spins=game.PlayerProgress.SpinCount;field.SpinButton.Button.onClick.Invoke();
            Assert.AreEqual(spins-1,game.PlayerProgress.SpinCount);
            for(int frame=0;frame<1800&&!core.Entry.Window.gameObject.activeSelf;frame++){Claim(field.BigWinPopup.PlainButton);Claim(field.JackpotPopup.PlainButton);yield return null;}
            Assert.IsTrue(core.Entry.Window.gameObject.activeSelf);Assert.IsTrue(field.IsBusy);Assert.Greater(core.Entry.InitialSpinCount,0);
            for(int i=0;i<15;i++)yield return null;core.Entry.Window.PlainButton.onClick.Invoke();
            for(int frame=0;frame<1800&&!core.Exit.Window.IsShown;frame++)yield return null;
            Assert.IsNull(core.Error);Assert.IsNull(core.Exit.Error);Assert.IsNull(field.ModeView.FreeReels.Controller.Error);
            Assert.IsNull(field.ModeView.FreeReels.CoinScan.Error);Assert.IsNull(field.ModeView.FreeReels.BallScan.Error);Assert.IsNull(field.ModeView.FreeReels.RewardCollect.Error);
            Assert.IsTrue(core.Exit.Window.IsShown);Assert.AreEqual(0,game.PlayerProgress.FreeSpinCount);Assert.IsTrue(field.IsBusy);Assert.AreEqual(1,completed);
            for(int i=0;i<100&&!core.Exit.Window.ContinueButton.gameObject.activeInHierarchy;i++)yield return null;
            Assert.IsTrue(core.Exit.Window.ContinueButton.gameObject.activeInHierarchy);core.Exit.Window.ContinueButton.onClick.Invoke();
            Assert.IsTrue(field.IsBusy,"End-window close must not unlock Spin before return transition completes.");
            for(int frame=0;frame<150&&completed==1;frame++)yield return null;
            Assert.AreEqual(2,completed);Assert.IsFalse(core.Entry.IsFreeSpinEnd);Assert.IsFalse(field.IsBusy);
            Assert.AreEqual(RecoveredSlotType.Base,game.PlayerProgress.GameSlotType);Assert.IsTrue(field.ModeView.BaseRoll.activeSelf);Assert.IsFalse(field.ModeView.FreeRoll.activeSelf);
            spins=game.PlayerProgress.SpinCount;field.SpinButton.Button.onClick.Invoke();Assert.AreEqual(spins-1,game.PlayerProgress.SpinCount);Assert.IsTrue(field.Reels.IsRunning);
        } finally {
            Random.state=random;Time.timeScale=scale;Time.captureDeltaTime=delta;
            if(scene.IsValid()&&scene.isLoaded)unload=SceneManager.UnloadSceneAsync(scene);
            if(had)PlayerPrefs.SetString(key,saved);else PlayerPrefs.DeleteKey(key);
        }
        if(unload!=null)yield return unload;
    }
    private static void Claim(Button button){if(button!=null&&button.gameObject.activeInHierarchy&&button.IsInteractable())button.onClick.Invoke();}
}
